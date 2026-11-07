"""Brainstick score for the looping ad, v2 — 132 BPM.

Shorter and busier than `loop_score.py`: the earworm motif fires on the same
two beats of every bar, a blip answers it half a beat later, and a rising
sweep breathes in front of the drop. Every repetition sits on the same grid
point, which is the whole point — a listener can predict it, and predicting it
is what makes it stick.
"""
from __future__ import annotations

import wave
from pathlib import Path

import numpy as np

from . import audio as A

SR = A.SR
BPM = 132.0
BEAT = 60.0 / BPM          # 0.4545 s
BAR = BEAT * 4             # 1.8182 s
BARS = 8
DURATION = BAR * BARS      # 14.55 s


def _kick(dur=0.32, freq=160.0, seed=3) -> np.ndarray:
    t = A._t(dur)
    env = np.exp(-t * 15.0)
    body = np.sin(2 * np.pi * freq * t * np.exp(-t * 9.5)) * env
    click = A._norm(A._filter(A._noise(dur, seed), hi=2000), 1.0) * np.exp(-t * 95.0) * 0.55
    return (body + click).astype(np.float32)


def _hat(dur=0.07, seed=7) -> np.ndarray:
    t = A._t(dur)
    env = np.exp(-t * 60.0)
    return (A._norm(A._filter(A._noise(dur, seed), lo=6800), 1.0) * env * 0.32).astype(np.float32)


def _clap(dur=0.26, seed=11) -> np.ndarray:
    t = A._t(dur)
    burst = (np.exp(-t * 28.0) + 0.6 * np.exp(-t * 66.0) + 0.35 * np.exp(-t * 118.0))
    noise = A._norm(A._filter(A._noise(dur, seed), lo=1500, hi=7200), 1.0)
    return (noise * burst * 0.52).astype(np.float32)


def _bell(dur=0.58, root=392.0, seed=17) -> np.ndarray:
    """The motif voice: a short detuned bell."""
    t = A._t(dur)
    env = np.exp(-t * 5.6)
    sig = np.zeros(t.size, dtype=np.float32)
    for mul, gain in ((1.0, 0.50), (1.5, 0.26), (2.0, 0.17), (2.9966, 0.10), (4.0, 0.05)):
        sig += (gain * np.sin(2 * np.pi * root * mul * t)).astype(np.float32)
    shimmer = A._norm(A._filter(A._noise(dur, seed), hi=5600), 1.0) * np.exp(-t * 13.0) * 0.12
    return A._fade(((sig * env) + shimmer).astype(np.float32), 0.004, 0.11)


def _blip(dur=0.16, f0=880.0, f1=1320.0, seed=29) -> np.ndarray:
    """The answer: a short upward blip, always the same pitch."""
    t = A._t(dur)
    sweep = f0 + (f1 - f0) * (t / dur)
    sig = np.sin(2 * np.pi * sweep * t)
    env = np.exp(-t * 22.0)
    air = A._norm(A._filter(A._noise(dur, seed), hi=7000), 1.0) * env * 0.10
    return A._fade(((sig * env * 0.30) + air).astype(np.float32), 0.003, 0.05)


def _boing(dur=0.34, f0=196.0, f1=98.0, seed=31) -> np.ndarray:
    """A low falling synth note under the motif."""
    t = A._t(dur)
    sweep = f0 * (f1 / f0) ** (t / dur)
    sig = np.sin(2 * np.pi * sweep * t) + 0.3 * np.sin(4 * np.pi * sweep * t)
    env = np.exp(-t * 8.0)
    return A._fade((sig * env * 0.28).astype(np.float32), 0.006, 0.09)


def _riser(dur=BAR, seed=23) -> np.ndarray:
    n = A._n(dur)
    w = np.linspace(0.0, 1.0, n, dtype=np.float32)
    noise = A._noise(dur, seed)
    a = A._norm(A._filter(noise, hi=400), 1.0)
    b = A._norm(A._filter(noise, lo=1400, hi=4600), 1.0)
    blend = a * (1 - w) ** 2 + b * (2 * w * (1 - w))
    sweep = np.sin(2 * np.pi * (240.0 + 1100.0 * w ** 2) * np.arange(n) / SR) * 0.16 * (w ** 2)
    return A._fade(((blend * 0.55 + sweep) * (w ** 2.2)).astype(np.float32), 0.04, 0.18)


def _sub(dur=DURATION) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    sig = np.sin(2 * np.pi * 49.0 * t) * 0.7 + np.sin(2 * np.pi * 73.5 * t) * 0.25
    pulse = 0.55 + 0.45 * np.sin(2 * np.pi * t / BAR)
    return (sig * pulse * 0.30).astype(np.float32)


def build(duration: float = DURATION) -> np.ndarray:
    n = A._n(duration)
    mix = _sub(duration)

    def add(sig, at, gain=1.0):
        i = int(round(float(at) * SR))
        if i >= n or sig.size == 0:
            return
        seg = sig[:n - i]
        mix[i:i + seg.size] += seg * float(gain)

    beats = int(duration / BEAT) + 2

    for b in range(beats):
        down = (b % 4 == 0)
        add(_kick(0.34 if down else 0.24, 165.0 if down else 132.0, 30 + b),
            b * BEAT, 1.0 if down else 0.68)

    for b in range(beats):
        if b % 4 in (1, 3):
            add(_clap(0.26, 60 + b), b * BEAT, 0.40)

    for b in range(beats):
        add(_hat(0.07, 90 + b), b * BEAT + BEAT * 0.5, 0.30)
        if b % 2 == 1:
            add(_hat(0.05, 140 + b), b * BEAT + BEAT * 0.25, 0.16)

    # the motif: same two points of every bar, all the way through
    for bar in range(BARS):
        add(_bell(0.60, 392.0, 40 + bar), bar * BAR, 0.90)
        add(_boing(0.34, 196.0, 98.0, 80 + bar), bar * BAR, 0.55)
        add(_blip(0.16, 880.0, 1320.0, 120 + bar), bar * BAR + BEAT * 2.5, 0.60)
        add(_bell(0.48, 587.33, 60 + bar), bar * BAR + BEAT * 2, 0.42)

    # breath into the drop, impact on its downbeat
    add(_riser(BAR, 23), 4 * BAR, 0.70)
    add(A._boom(1.5, 56.0, 77, 1.0), 5 * BAR, 0.60)

    # tail: the motif once more over the end card
    add(_bell(1.1, 392.0, 200), 7 * BAR, 0.75)
    add(_bell(1.1, 293.66, 201), 7 * BAR + BEAT * 2, 0.45)

    mix = np.tanh(mix * 1.15).astype(np.float32)
    return A._norm(mix, 0.90)


def write_score(path, duration: float = DURATION):
    mono = build(duration)
    n = mono.size

    delay = int(0.008 * SR)
    left = mono.copy()
    right = np.empty_like(mono)
    right[:delay] = mono[:delay]
    right[delay:] = mono[:n - delay]
    right *= 0.95

    stereo = np.stack([left, right], axis=1)
    data = (np.clip(stereo, -1.0, 1.0) * 32767.0).astype("<i2")

    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(data.tobytes())
    return path


if __name__ == "__main__":
    out = Path(__file__).resolve().parents[1] / "out" / "ad_loop_score_v2.wav"
    write_score(out)
    print(f"wrote {out} ({DURATION:.2f}s, {BPM:.0f} BPM)")
