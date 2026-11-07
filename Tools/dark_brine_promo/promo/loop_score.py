"""15-second loop score for the short ad.

Eight bars of 124 BPM: a kick on every beat, a hook stab that repeats on the
same bar of every bar, and a riser into the end card. The last bar lands back
on the first beat so the loop is seamless.
"""
from __future__ import annotations

import wave
from pathlib import Path

import numpy as np

from . import audio as A

SR = A.SR
BPM = 124.0
BEAT = 60.0 / BPM          # 0.4839 s
BAR = BEAT * 4             # 1.9355 s
BARS = 8
DURATION = BAR * BARS      # 15.48 s


def _kick(dur=0.34, freq=150.0, seed=3) -> np.ndarray:
    t = A._t(dur)
    env = np.exp(-t * 14.0)
    body = np.sin(2 * np.pi * freq * t * np.exp(-t * 9.0)) * env
    click = A._norm(A._filter(A._noise(dur, seed), hi=1800), 1.0) * np.exp(-t * 90.0) * 0.5
    return (body + click).astype(np.float32)


def _hat(dur=0.09, seed=7) -> np.ndarray:
    t = A._t(dur)
    env = np.exp(-t * 55.0)
    return (A._norm(A._filter(A._noise(dur, seed), lo=6000), 1.0) * env * 0.35).astype(np.float32)


def _clap(dur=0.30, seed=11) -> np.ndarray:
    t = A._t(dur)
    burst = (np.exp(-t * 26.0) + 0.6 * np.exp(-t * 62.0) + 0.35 * np.exp(-t * 110.0))
    noise = A._norm(A._filter(A._noise(dur, seed), lo=1400, hi=7000), 1.0)
    return (noise * burst * 0.55).astype(np.float32)


def _hook_stab(dur=0.62, root=392.0, seed=17) -> np.ndarray:
    """The repeating earworm: a short detuned bell phrase."""
    t = A._t(dur)
    env = np.exp(-t * 5.2)
    sig = np.zeros(t.size, dtype=np.float32)
    for mul, gain in ((1.0, 0.50), (1.5, 0.26), (2.0, 0.16), (2.9966, 0.10)):
        sig += (gain * np.sin(2 * np.pi * root * mul * t)).astype(np.float32)
    shimmer = A._norm(A._filter(A._noise(dur, seed), hi=5200), 1.0) * np.exp(-t * 12.0) * 0.12
    return A._fade(((sig * env) + shimmer).astype(np.float32), 0.004, 0.12)


def _riser(dur=BAR, seed=23) -> np.ndarray:
    n = A._n(dur)
    w = np.linspace(0.0, 1.0, n, dtype=np.float32)
    noise = A._noise(dur, seed)
    a = A._norm(A._filter(noise, hi=400), 1.0)
    b = A._norm(A._filter(noise, lo=1200, hi=4200), 1.0)
    blend = a * (1 - w) ** 2 + b * (2 * w * (1 - w))
    sweep = np.sin(2 * np.pi * (200.0 + 900.0 * w ** 2) * np.arange(n) / SR) * 0.18 * (w ** 2)
    return A._fade(((blend * 0.6 + sweep) * (w ** 2.2)).astype(np.float32), 0.04, 0.20)


def _sub(dur=DURATION) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    sig = np.sin(2 * np.pi * 49.0 * t) * 0.7 + np.sin(2 * np.pi * 73.5 * t) * 0.25
    pulse = 0.55 + 0.45 * np.sin(2 * np.pi * t / BAR)
    return (sig * pulse * 0.32).astype(np.float32)


def build(duration: float = DURATION) -> np.ndarray:
    n = A._n(duration)
    mix = _sub(duration)

    def add(sig, at, gain=1.0):
        i = int(round(float(at) * SR))
        if i >= n or sig.size == 0:
            return
        seg = sig[:n - i]
        mix[i:i + seg.size] += seg * float(gain)

    # kick on every beat, heavier on the downbeat
    beats = int(duration / BEAT) + 2
    for b in range(beats):
        at = b * BEAT
        down = (b % 4 == 0)
        add(_kick(0.36 if down else 0.26, 155.0 if down else 128.0, 30 + b),
            at, 1.0 if down else 0.72)

    # backbeat
    for b in range(beats):
        if b % 4 in (1, 3):
            add(_clap(0.28, 60 + b), b * BEAT, 0.42)

    # hats
    for b in range(beats):
        add(_hat(0.08, 90 + b), b * BEAT + BEAT * 0.5, 0.30)

    # the hook: same bar of every bar, so it repeats relentlessly
    for bar in range(BARS):
        add(_hook_stab(0.66, 392.0, 40 + bar), bar * BAR + BEAT * 2, 0.85)
        add(_hook_stab(0.50, 587.33, 60 + bar), bar * BAR + BEAT * 3.25, 0.45)

    # riser into the end card
    add(_riser(BAR, 23), (BARS - 1) * BAR, 0.75)

    # impact on the downbeat of bar 5 (the ad's middle beat)
    add(A._boom(1.4, 58.0, 77, 1.0), 4 * BAR, 0.55)

    mix = np.tanh(mix * 1.12).astype(np.float32)
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
    out = Path(__file__).resolve().parents[1] / "out" / "ad_loop_score.wav"
    write_score(out)
    print(f"wrote {out} ({DURATION:.2f}s, {BPM:.0f} BPM)")
