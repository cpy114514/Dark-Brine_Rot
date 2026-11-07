'''Procedural soundtrack: ocean bed, drones, impacts, riser and heartbeat.

No samples, no external files: every sound is synthesized with numpy and
written as a 16-bit stereo WAV that the encoder muxes into the MP4.
'''
from __future__ import annotations

import wave
from pathlib import Path

import numpy as np

SR = 44100
DURATION = 23.0

# Timeline cues (seconds) shared with promo.render.
OPEN = (0.0, 2.8)
ISLAND = (2.8, 5.4)
SAHUR = (5.4, 7.2)
WEAPONS = [(7.2, 8.6), (8.6, 10.0), (10.0, 11.4)]
BOSSES = [(11.4, 13.0), (13.0, 14.6), (14.6, 16.2)]
HYPE = (16.2, 18.6)
TITLE = (18.6, 21.2)
END = (21.2, 23.0)

IMPACTS = [7.2, 8.6, 10.0, 11.4, 13.0, 14.6, 16.2, 18.6]


def _n(dur):
    return int(round(float(dur) * SR))


def _t(dur):
    return np.arange(_n(dur), dtype=np.float32) / SR


def _noise(dur, seed=0):
    return np.random.default_rng(int(seed)).standard_normal(_n(dur)).astype(np.float32)


def _filter(x, lo=None, hi=None, order=2):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(x.size, 1.0 / SR)
    gain = np.ones_like(f)
    if lo is not None:
        gain = gain / (1.0 + (lo / np.maximum(f, 0.5)) ** order)
    if hi is not None:
        gain = gain / (1.0 + (f / hi) ** order)
    return np.fft.irfft(spec * gain, n=x.size).astype(np.float32)


def _norm(x, peak=1.0):
    m = float(np.abs(x).max())
    return x if m <= 1e-9 else (x / m * peak).astype(np.float32)


def _fade(x, attack=0.01, release=0.2):
    n = x.size
    a = min(n, max(1, int(attack * SR)))
    r = min(n, max(1, int(release * SR)))
    env = np.ones(n, dtype=np.float32)
    env[:a] = np.linspace(0.0, 1.0, a, dtype=np.float32)
    env[n - r:] *= np.linspace(1.0, 0.0, r, dtype=np.float32)
    return (x * env).astype(np.float32)


def _bed():
    n = _n(DURATION)
    t = np.arange(n, dtype=np.float32) / SR

    surf = _norm(_filter(_noise(DURATION, 7), hi=420), 1.0)
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * 0.085 * t - 0.9) ** 2
    surf = surf * swell * 0.30

    rain = _norm(_filter(_noise(DURATION, 11), lo=2600), 1.0) * 0.05

    drone = (0.20 * np.sin(2 * np.pi * 36.0 * t)
             + 0.13 * np.sin(2 * np.pi * 54.0 * t + 1.1)
             + 0.08 * np.sin(2 * np.pi * 81.0 * t + 2.4))
    drone = drone * (0.55 + 0.45 * np.sin(2 * np.pi * 0.05 * t))
    drone = _filter(drone.astype(np.float32), hi=130, order=1) * 0.55

    return (surf + rain + drone).astype(np.float32)


def _boom(dur=2.2, freq=64.0, seed=1, power=1.0):
    t = _t(dur)
    env = np.exp(-t * 3.2)
    body = np.sin(2 * np.pi * freq * t * np.exp(-t * 0.35)) * env
    sub = np.sin(2 * np.pi * (freq * 0.5) * t) * np.exp(-t * 2.2) * 0.8
    crack = _norm(_filter(_noise(dur, seed), hi=260), 1.0) * np.exp(-t * 9.0) * 1.5

    out = (body + sub + crack).astype(np.float32)
    click_n = min(out.size, _n(0.05))
    click = _noise(0.05, seed + 9)[:click_n] * np.exp(-np.arange(click_n) / SR * 60.0)
    out[:click_n] += click * 0.35
    return _fade(out * float(power), 0.002, 0.28)


def _metal(dur=1.1, base=820.0, seed=2):
    t = _t(dur)
    env = np.exp(-t * 6.5)
    tone = (np.sin(2 * np.pi * base * t)
            + 0.55 * np.sin(2 * np.pi * base * 2.71 * t)
            + 0.32 * np.sin(2 * np.pi * base * 4.13 * t))
    tick = _norm(_filter(_noise(dur, seed), lo=2200), 1.0) * np.exp(-t * 24.0)
    return _fade(((tone * 0.45 * env) + tick * 0.5).astype(np.float32), 0.001, 0.18)


def _riser(dur=3.2, seed=5):
    n = _n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    w = np.linspace(0.0, 1.0, n, dtype=np.float32)
    noise = _noise(dur, seed)

    a = _norm(_filter(noise, hi=320), 1.0)
    b = _norm(_filter(noise, lo=900, hi=3200), 1.0)
    c = _norm(_filter(noise, lo=3400), 1.0)
    blend = a * (1 - w) ** 2 + b * (2 * w * (1 - w)) + c * w ** 2

    sweep = np.sin(2 * np.pi * (170.0 + 620.0 * w ** 2) * t) * 0.20 * (w ** 2)
    env = w ** 2.3
    return _fade((blend * 0.55 + sweep) * env, 0.05, 0.35)


def _heart(dur=0.45, seed=8):
    t = _t(dur)
    env = np.exp(-t * 24.0)
    return (np.sin(2 * np.pi * 58.0 * t) * env * 0.9).astype(np.float32)


def _pad(dur=2.6, root=110.0):
    t = _t(dur)
    parts = ((1.0, 0.50), (1.5, 0.30), (2.0, 0.20), (3.0, 0.11))
    sig = np.zeros(t.size, dtype=np.float32)
    for i, (mul, gain) in enumerate(parts):
        sig += (gain * np.sin(2 * np.pi * root * mul * t + i * 0.7)).astype(np.float32)
    env = np.minimum(1.0, t / 0.7) * np.exp(-np.maximum(0.0, t - 1.0) * 1.7)
    return (sig * env * 0.22).astype(np.float32)


def build(duration=DURATION):
    n = _n(duration)
    mix = _bed()
    if mix.size >= n:
        mix = mix[:n]

    def add(sig, at, gain=1.0):
        i = int(round(float(at) * SR))
        if i >= n or sig.size == 0:
            return
        seg = sig[:n - i]
        mix[i:i + seg.size] += seg * float(gain)

    add(_boom(3.4, 44.0, 3, 1.0), 0.0, 0.55)

    for k, at in enumerate(IMPACTS):
        heavy = 0.85 if k < 6 else 1.25
        add(_boom(2.2, 60.0 - 2.0 * k, 20 + k, 1.0), at, heavy)
        add(_metal(0.9, 760.0 + 70.0 * k, 30 + k), at + 0.012, 0.20)

    add(_riser(3.5, 41), 15.2, 0.65)
    add(_pad(2.6, 110.0), 18.55, 0.90)
    add(_pad(2.6, 164.81), 18.60, 0.55)

    for i in range(6):
        add(_heart(0.45, 50 + i), 5.45 + i * 0.42, 0.45)
    for i in range(4):
        add(_heart(0.45, 60 + i), 11.45 + i * 0.60, 0.50)

    add(_boom(2.4, 42.0, 77, 0.8), 21.2, 0.45)

    mix = np.tanh(mix * 1.15).astype(np.float32)
    return _norm(mix, 0.92)


def write_soundtrack(path, duration=DURATION):
    mono = build(duration)
    n = mono.size

    delay = int(0.010 * SR)
    left = mono.copy()
    right = np.empty_like(mono)
    right[:delay] = mono[:delay]
    right[delay:] = mono[:n - delay]
    right *= 0.96

    left = np.tanh(left * 1.02).astype(np.float32)
    right = np.tanh(right * 1.02).astype(np.float32)

    stereo = np.stack([left, right], axis=1)
    data = (np.clip(stereo, -1.0, 1.0) * 32767.0).astype('<i2')

    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), 'wb') as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(data.tobytes())
    return path
