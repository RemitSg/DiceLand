"""Build DiceLand's death impact from project-owned effects and synthesized bass.

Run with Python 3 (stdlib only). Source layers come from the existing Epic Toon FX
pack and battle hit in this repository; no external audio is downloaded. The bass
stays mono. --demo-music also renders a local, unpitched ducking preview.
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
SOURCES = [
    ('Assets/Assets/ResourcePack/Epic Toon FX/Sound/etfx_explosion_dark01.wav', 0.60, 1450, 0.80),
    ('Assets/Assets/ResourcePack/Epic Toon FX/Sound/etfx_explosion_nuke.wav', 0.56, 850, 0.74),
    ('Assets/Assets/ResourcePack/Epic Toon FX/Sound/etfx_explosion_soul.wav', 0.72, 2100, 0.42),
    ('Assets/Resources/SFX/Hit.wav', 0.68, 2400, 0.32),
]


def read_audio(path):
    """Read PCM or IEEE float RIFF audio, including the pack's float32 WAVs."""
    data = path.read_bytes()
    if data[:4] != b'RIFF' or data[8:12] != b'WAVE':
        raise ValueError(f'Not a RIFF/WAVE asset: {path}')
    offset, fmt, payload = 12, None, None
    while offset + 8 <= len(data):
        tag, size = struct.unpack_from('<4sI', data, offset)
        chunk = data[offset + 8:offset + 8 + size]
        if tag == b'fmt ':
            fmt = struct.unpack_from('<HHIIHH', chunk)
            if fmt[0] == 65534:
                fmt = (struct.unpack_from('<H', chunk, 24)[0], *fmt[1:])
        if tag == b'data':
            payload = chunk
        offset += 8 + size + size % 2
    if fmt is None or payload is None:
        raise ValueError(f'Missing WAVE chunks: {path}')
    encoding, channels, rate, _, _, bits = fmt
    width = bits // 8
    if encoding == 3:
        values = [x[0] for x in struct.iter_unpack('<f' if width == 4 else '<d', payload)]
    elif encoding == 1:
        values = [int.from_bytes(payload[i:i + width], 'little', signed=True) / (2 ** (bits - 1))
                  for i in range(0, len(payload), width)]
    else:
        raise ValueError(f'Unsupported WAVE format {encoding}: {path}')
    return [[values[i] for i in range(c, len(values), channels)] for c in range(channels)], rate


def source_layer(path, speed, cutoff, length):
    channels, rate = read_audio(path)
    mono = [sum(frame) / len(frame) for frame in zip(*channels)]
    peak = max(abs(x) for x in mono)
    # Cut directly to the impact. No swell, pre-roll, or gradual attack.
    start = max(0, next(i for i, x in enumerate(mono) if abs(x) >= peak * 0.18) - 3)
    result = []
    for i in range(length):
        position = start + i * rate / RATE * speed
        k = int(position)
        sample = mono[k] + (mono[k + 1] - mono[k]) * (position - k) if k + 1 < len(mono) else 0.0
        result.append(sample)
    result = biquad(biquad(result, cutoff), 38, highpass=True)
    rms = math.sqrt(sum(x * x for x in result[:RATE // 2]) / (RATE // 2))
    return [x * (0.32 / max(rms, 0.001)) for x in result]


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


def room_tail(excitation, delays):
    result = [0.0] * len(excitation)
    for milliseconds, feedback in delays:
        delay = round(milliseconds * RATE / 1000)
        buffer = [0.0] * delay
        filtered = 0.0
        for i, sample in enumerate(excitation):
            j = i % delay
            echo = buffer[j]
            filtered += 0.075 * (echo - filtered)
            buffer[j] = sample + filtered * feedback
            result[i] += echo / len(delays)
    return result


def render():
    rng = random.Random(20261104)
    length = round(RATE * DURATION)
    noise = [rng.uniform(-1, 1) for _ in range(length)]
    rumble = biquad(biquad(noise, 155), 28, highpass=True)
    air = biquad(biquad(noise, 1100), 180, highpass=True)
    layers = [(source_layer(ROOT / path, speed, cutoff, length), gain)
              for path, speed, cutoff, gain in SOURCES]
    dry, room_input = [], []
    for i in range(length):
        t = i / RATE
        phase = TAU * (36 * t + 82 * 0.037 * (1 - math.exp(-t / 0.037)))
        body = (math.cos(phase) + 0.38 * math.cos(2 * phase) + 0.17 * math.cos(3 * phase))
        body *= 0.85 * math.exp(-t / 0.66)
        pressure = math.cos(TAU * (63 * t + 57 * 0.018 * (1 - math.exp(-t / 0.018))))
        pressure *= 0.66 * math.exp(-t / 0.075)
        grit = rumble[i] * 3.0 * math.exp(-t / 0.82)
        transient = air[i] * 0.35 * math.exp(-t / 0.045)
        texture = sum(samples[i] * gain for samples, gain in layers)
        dry.append(math.tanh((body + pressure + grit + transient + texture) * 1.65))
        room_input.append((texture * 0.40 + pressure * 0.15 + rumble[i] * 0.32) * math.exp(-t / 0.55))
    left_tail = room_tail(room_input, [(53, 0.76), (97, 0.70), (149, 0.65)])
    right_tail = room_tail(room_input, [(67, 0.76), (113, 0.70), (173, 0.65)])
    left = biquad([math.tanh(x * 1.45 + y * 0.70) for x, y in zip(dry, left_tail)], 25, highpass=True)
    right = biquad([math.tanh(x * 1.45 + y * 0.70) for x, y in zip(dry, right_tail)], 25, highpass=True)
    for i in range(length):
        t = i / RATE
        # Only the final tail fades. The first sample contains the full pressure hit.
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
    write_wav(preview / 'death-impact-v2.wav', channels)
    mono = [(a + b) * 0.5 for a, b in zip(*channels)]
    low = biquad(mono, 180)
    energy = sum(x * x for x in mono)
    report = dict(durationSeconds=DURATION, sampleRate=RATE, channels=2,
                  peakDbFS=20 * math.log10(max(abs(x) for channel in channels for x in channel)),
                  rmsDbFS=10 * math.log10(energy / len(mono)),
                  below180HzEnergyFraction=sum(x * x for x in low) / energy,
                  first50msRmsDbFS=10 * math.log10(sum(x * x for x in mono[:RATE // 20]) / (RATE // 20)),
                  leadingSample=channels[0][0], trailingSample=channels[0][-1],
                  attackFadeSeconds=0, sources=[source[0] for source in SOURCES], synthesizedSubBassHz=36)
    (preview / 'synthesis-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    if args.demo_music:
        render_demo(args.demo_music, channels, preview / 'death-music-ducking-v2.wav')
    print(json.dumps(dict(asset=str(args.output), audition=str(preview / 'death-impact-v2.wav'), **report)))


if __name__ == '__main__':
    main()
