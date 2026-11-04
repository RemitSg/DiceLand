# /// script
# requires-python = ">=3.12"
# dependencies = ["numpy==2.5.4", "scipy==1.18.1"]
# ///
"""Generate an original pressure-impact death cue using coarse reference features.

Run: uv run tools/audio/Generate-BattleDeathAudio.py
The confirmed Library of Ruina reference informs octave balance, stereo width,
and broad decay timing. Every sound sample is synthesized from new noise and
resonators; no reference recording is read or embedded by this generator.
--demo-music renders the existing 0.4-second BGM ducking behavior for audition.
"""
import argparse
from array import array
import json
import math
from pathlib import Path
import struct
import wave

import numpy as np
from scipy import interpolate, signal

ROOT = Path(__file__).resolve().parents[2]
RATE = 48000
DURATION = 2.4
TAU = math.tau
REFERENCE_URL = 'https://tuna.voicemod.net/sound/1ccd942e-bce6-4546-9bc0-f7ccda71901b'
# Coarse energy bands: sustained bass pressure plus the shorter upper impact.
BANDS = [(15, 25), (25, 48), (48, 96), (96, 192), (192, 384),
         (384, 768), (768, 1536), (1536, 3072), (3072, 6144), (6144, 12000)]
TIMES = [0, 0.12, 0.35, 0.75, 1.25, 1.75, 2.2, 2.4]
LEVELS_DB = [-7.5, -6.5, -7.5, -11.5, -20, -30, -40, -55]
BAND_ENERGY = np.array([
    [.10, .16, .24, .43, .018, .010, .015, .012, .010, .005],
    [.09, .30, .48, .06, .012, .006, .014, .017, .014, .007],
    [.04, .06, .69, .05, .03, .022, .055, .025, .022, .006],
    [.009, .15, .64, .10, .020, .015, .033, .018, .013, .002],
    [.001, .16, .64, .11, .06, .020, .002, .001, .001, .001],
    [.001, .06, .69, .11, .10, .035, .002, .001, .001, .0005],
    [.001, .07, .85, .06, .01, .003, .001, .0003, .0003, .0001],
    [.001, .07, .85, .06, .01, .003, .001, .0003, .0003, .0001],
])


def band_noise(rng, length, low, high):
    # Warm the filters before taking the impact so the onset is already full-bodied.
    warmup = RATE
    noise = rng.standard_normal((3, length + warmup))
    sos = signal.butter(3, [low, high], btype='bandpass', fs=RATE, output='sos')
    filtered = signal.sosfilt(sos, noise, axis=-1)[:, warmup:]
    rms = np.sqrt(np.mean(filtered[:, :RATE] ** 2, axis=-1, keepdims=True))
    return filtered / np.maximum(1e-8, rms)


def limit_peaks(channels, ceiling):
    """Look-ahead gain control, without clipping or waveshaping the bass waveform."""
    allowed = np.minimum(1, ceiling / np.maximum(1e-9, np.max(np.abs(channels), axis=0)))
    gain = np.empty_like(allowed)
    release = 1 - math.exp(-1 / (RATE * 0.070))
    attack = 1 - math.exp(-1 / (RATE * 0.001))
    current = 1.0
    for i in range(len(gain)):
        current = min(allowed[i], current + (1 - current) * release)
        gain[i] = current
    current = 1.0
    for i in range(len(gain) - 1, -1, -1):
        current = min(gain[i], current + (1 - current) * attack)
        gain[i] = current
    return channels * gain


def render():
    rng = np.random.default_rng(2026110419)
    length = round(RATE * DURATION)
    t = np.arange(length) / RATE
    rows = BAND_ENERGY / BAND_ENERGY.sum(axis=1, keepdims=True)
    amplitudes = np.sqrt(rows) * (10 ** (np.array(LEVELS_DB)[:, None] / 20))
    envelopes = interpolate.PchipInterpolator(TIMES, amplitudes, axis=0)(t)
    channels = np.zeros((2, length))
    widths = [0.04, 0.32, 0.65, 0.68, 0.72, 0.74, 0.78, 0.80, 0.82, 0.85]
    for band, ((low, high), width) in enumerate(zip(BANDS, widths)):
        center, side_left, side_right = band_noise(rng, length, low, high)
        normalization = math.sqrt(1 + width * width)
        channels[0] += (center + width * side_left) / normalization * envelopes[:, band]
        channels[1] += (center + width * side_right) / normalization * envelopes[:, band]
    # A single pressure front gives an immediate hit, with no audible pitched tom sweep.
    phase = TAU * (59 * t + 42 * 0.026 * (1 - np.exp(-t / 0.026)))
    pressure = 0.28 * np.cos(phase) * np.exp(-t / 0.075)
    channels += pressure
    # Sustain the first half-second, then let the large low-end space collapse naturally.
    channels *= 10 ** (1.2 / 20)
    channels = limit_peaks(channels, 10 ** (-0.7 / 20))
    channels *= np.minimum(1, (DURATION - t) / 0.09) ** 2
    return channels


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
    write_wav(preview / 'death-pressure-impact-v4.wav', channels)
    mono = channels.mean(axis=0)
    low = signal.sosfilt(signal.butter(2, 180, fs=RATE, output='sos'), mono)
    energy = float(np.sum(mono * mono))
    report = dict(durationSeconds=DURATION, sampleRate=RATE, channels=2,
                  peakDbFS=20 * math.log10(max(abs(x) for channel in channels for x in channel)),
                  rmsDbFS=10 * math.log10(energy / len(mono)),
                  below180HzEnergyFraction=sum(x * x for x in low) / energy,
                  first50msRmsDbFS=10 * math.log10(sum(x * x for x in mono[:RATE // 20]) / (RATE // 20)),
                  leadingSample=float(channels[0][0]), trailingSample=float(channels[0][-1]),
                  attackFadeSeconds=0, reference=REFERENCE_URL, recordedSamplesUsed=False,
                  bodyBandHz=[48, 96], texture='broadband pressure front and dense bass decay',
                  distortion=False, discreteEchoes=False)
    (preview / 'synthesis-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    if args.demo_music:
        render_demo(args.demo_music, channels, preview / 'death-music-ducking-v4.wav')
    print(json.dumps(dict(asset=str(args.output), audition=str(preview / 'death-pressure-impact-v4.wav'), **report)))


if __name__ == '__main__':
    main()
