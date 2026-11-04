"""Synthesize a continuous thunder-drum death impact (Python stdlib only).

A struck membrane drops from a midrange attack into deep bass. Smooth, filtered
noise supplies the thunder tail; there are no chopped samples, distortion stages,
or discrete echo taps. --demo-music renders a local, unpitched ducking preview.
"""
import argparse
from array import array
import json
import math
from pathlib import Path
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
RATE = 48000
DURATION = 2.4
TAU = math.tau


def biquad(samples, frequency, highpass=False):
    omega = TAU * frequency / RATE
    cosine = math.cos(omega)
    alpha = math.sin(omega) / math.sqrt(2)
    if highpass:
        b0, b1, b2 = (1 + cosine) / 2, -(1 + cosine), (1 + cosine) / 2
    else:
        b0, b1, b2 = (1 - cosine) / 2, 1 - cosine, (1 - cosine) / 2
    a0, a1, a2 = 1 + alpha, -2 * cosine, 1 - alpha
    b0, b1, b2, a1, a2 = (x / a0 for x in (b0, b1, b2, a1, a2))
    x1 = x2 = y1 = y2 = 0.0
    result = []
    for x in samples:
        y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        result.append(y)
        x2, x1, y2, y1 = x1, x, y1, y
    return result


def falling_noise(rng, length, start_cutoff, end_cutoff, fall_seconds):
    """Continuous double-pole filtering; the cutoff never steps between blocks."""
    first = second = 0.0
    result = []
    for i in range(length):
        t = i / RATE
        cutoff = end_cutoff + (start_cutoff - end_cutoff) * math.exp(-t / fall_seconds)
        coefficient = 1 - math.exp(-TAU * cutoff / RATE)
        first += coefficient * (rng.gauss(0, 1) - first)
        second += coefficient * (first - second)
        result.append(second * math.sqrt(start_cutoff / cutoff))
    result = biquad(result, 27, highpass=True)
    rms = math.sqrt(sum(x * x for x in result[:RATE // 2]) / (RATE // 2))
    return [x / max(0.001, rms) for x in result]


def render():
    rng = random.Random(20261104)
    length = round(RATE * DURATION)
    thunder = falling_noise(rng, length, 1750, 85, 0.30)
    room_left = falling_noise(rng, length, 1200, 180, 0.36)
    room_right = falling_noise(rng, length, 1200, 180, 0.36)
    contact = biquad([rng.gauss(0, 1) for _ in range(length)], 1700)
    # Membrane modes give a drum skin its body without a distorted explosion layer.
    modes = [(1.0, 1.0, 0.90), (1.593, 0.50, 0.45), (2.136, 0.29, 0.30),
             (2.296, 0.20, 0.23), (2.655, 0.13, 0.16), (2.917, 0.09, 0.12),
             (3.157, 0.065, 0.085), (3.50, 0.04, 0.065)]
    center, left_space, right_space = [], [], []
    for i in range(length):
        t = i / RATE
        # Integrated frequency sweep: 265 Hz on contact, settling smoothly at 42 Hz.
        phase = TAU * (42 * t + 158 * 0.13 * (1 - math.exp(-t / 0.13))
                       + 65 * 0.025 * (1 - math.exp(-t / 0.025)))
        skin = sum(weight * math.sin(phase * ratio) * math.exp(-t / decay)
                   for ratio, weight, decay in modes)
        strike = contact[i] * 0.30 * math.exp(-t / 0.018)
        roll = thunder[i] * 0.58 * math.exp(-t / 1.10)
        center.append(skin * 0.93 + strike + roll)
        room_envelope = (1 - math.exp(-t / 0.035)) * math.exp(-t / 0.64)
        left_space.append(room_left[i] * 0.15 * room_envelope)
        right_space.append(room_right[i] * 0.15 * room_envelope)
    # Only the upper tail spreads into stereo; the drum and bass stay centered.
    left_space = biquad(left_space, 155, highpass=True)
    right_space = biquad(right_space, 155, highpass=True)
    center = biquad(center, 24, highpass=True)
    left = [x + y for x, y in zip(center, left_space)]
    right = [x + y for x, y in zip(center, right_space)]
    for i in range(length):
        t = i / RATE
        # Only the final tail fades. The foreground strike has no fade-in envelope.
        envelope = min(1.0, (DURATION - t) / 0.18) ** 2
        left[i] *= envelope
        right[i] *= envelope
    peak = max(max(abs(x) for x in left), max(abs(x) for x in right))
    gain = 10 ** (-0.7 / 20) / peak
    return [[x * gain for x in channel] for channel in (left, right)]


def write_wav(path, channels):
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = bytearray(len(channels[0]) * len(channels) * 2)
    for i, frame in enumerate(zip(*channels)):
        for c, sample in enumerate(frame):
            struct.pack_into('<h', pcm, (i * len(channels) + c) * 2,
                             round(max(-1, min(1, sample)) * 32767))
    with wave.open(str(path), 'wb') as stream:
        stream.setparams((len(channels), 2, RATE, len(channels[0]), 'NONE', 'not compressed'))
        stream.writeframes(pcm)


def render_demo(music_path, impact, output):
    # The event starts at 2 s. BGM keeps advancing silently until the 3 s slow-mo ends.
    with wave.open(str(music_path), 'rb') as stream:
        if stream.getsampwidth() != 2 or stream.getnchannels() != 2 or stream.getframerate() != RATE:
            raise ValueError('The demo input must be stereo PCM16 at 48 kHz.')
        stream.setpos(min(20 * RATE, max(0, stream.getnframes() - 8 * RATE)))
        pcm = array('h', stream.readframes(8 * RATE))
    channels = [[0.0] * (8 * RATE) for _ in range(2)]
    for i in range(len(channels[0])):
        t = i / RATE
        music_gain = 1 if t < 2 else max(0, 1 - (t - 2) / 0.4) if t < 2.4 else 0 if t < 5 else min(1, (t - 5) / 0.4)
        impact_i = i - 2 * RATE
        edge = min(1, t / 0.03, (8 - t) / 0.05)
        for c in range(2):
            music = pcm[i * 2 + c] / 32768 * 0.30 if i * 2 + c < len(pcm) else 0
            hit = impact[c][impact_i] * 0.95 if 0 <= impact_i < len(impact[c]) else 0
            channels[c][i] = (music * music_gain + hit) * edge
    write_wav(output, channels)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT / 'Assets/Resources/SFX/BattleDeathImpact.wav')
    parser.add_argument('--demo-music', type=Path)
    args = parser.parse_args()
    channels = render()
    write_wav(args.output, channels)
    preview = ROOT / 'outputs/agent-battle/death-audio-20261104'
    write_wav(preview / 'death-thunder-drum-v3.wav', channels)
    mono = [(a + b) * 0.5 for a, b in zip(*channels)]
    low = biquad(mono, 180)
    energy = sum(x * x for x in mono)
    report = dict(durationSeconds=DURATION, sampleRate=RATE, channels=2,
                  peakDbFS=20 * math.log10(max(abs(x) for channel in channels for x in channel)),
                  rmsDbFS=10 * math.log10(energy / len(mono)),
                  below180HzEnergyFraction=sum(x * x for x in low) / energy,
                  first50msRmsDbFS=10 * math.log10(sum(x * x for x in mono[:RATE // 20]) / (RATE // 20)),
                  leadingSample=channels[0][0], trailingSample=channels[0][-1],
                  attackFadeSeconds=0, sources=[], synthesizedSubBassHz=42,
                  startingDrumFrequencyHz=265, texture='continuous thunder and struck membrane',
                  distortion=False, discreteEchoes=False)
    (preview / 'synthesis-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    if args.demo_music:
        render_demo(args.demo_music, channels, preview / 'death-music-ducking-v3.wav')
    print(json.dumps(dict(asset=str(args.output), audition=str(preview / 'death-thunder-drum-v3.wav'), **report)))


if __name__ == '__main__':
    main()
