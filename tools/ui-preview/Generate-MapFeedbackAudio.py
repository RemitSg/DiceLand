"""Reproduce DiceLand's original short UI sounds. Stdlib only; no recorded samples."""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
OUT = Path(__file__).resolve().parents[2] / "Assets/Resources/SFX"


def tone(t, frequency, decay):
    return math.sin(math.tau * frequency * t) * math.exp(-decay * t)


def metal(t, frequency):
    return sum(weight * tone(t, frequency * multiple, decay)
               for multiple, weight, decay in [(1, 0.6, 22), (2.76, 0.23, 36), (4.07, 0.1, 55)])


def render(name, duration, sample):
    rng = random.Random(75219)
    values = []
    for i in range(round(duration * RATE)):
        t = i / RATE
        # Quiet, click-free attack and an explicit fade to silence at the boundary.
        envelope = min(1, t / 0.004) * min(1, (duration - t) / 0.025)
        values.append(sample(t, rng) * envelope)
    peak = max(abs(v) for v in values)
    gain = 0.62 / max(peak, 0.001)
    pcm = b"".join(struct.pack("<h", round(v * gain * 32767)) for v in values)
    path = OUT / ("Map" + name + ".wav")
    with wave.open(str(path), "wb") as stream:
        stream.setparams((1, 2, RATE, len(values), "NONE", "not compressed"))
        stream.writeframes(pcm)
    print(f"{path.name}: {duration:.2f}s, peak=0.62, mono 44.1kHz PCM16")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    render("CoinRelease", 0.24, lambda t, rng: metal(t, 1450) * 0.5 +
           (metal(t - 0.055, 1800) * 0.42 if t >= 0.055 else 0))
    render("CoinArrival", 0.18, lambda t, rng: metal(t, 2080) + tone(t, 1040, 34) * 0.08)
    render("CoinSpend", 0.3, lambda t, rng: sum(metal(t - start, frequency) * gain
           for start, frequency, gain in [(0, 1800, 0.6), (0.065, 1350, 0.4), (0.13, 980, 0.3)] if t >= start))
    render("Damage", 0.22, lambda t, rng: 0.6 * math.sin(math.tau * (110 * t - 85 * t * t)) * math.exp(-25 * t)
           + (rng.random() * 2 - 1) * math.exp(-65 * t) * 0.13)
    render("Heal", 0.52, lambda t, rng: sum(tone(t - start, frequency, 9) * gain
           for start, frequency, gain in [(0, 660, 0.42), (0.06, 880, 0.3), (0.13, 1320, 0.18)] if t >= start))
