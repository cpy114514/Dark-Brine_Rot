"""40-second score for the Dark Brine_Rot trailer.

Reuses the synthesis primitives from ``promo.audio`` (ocean bed, sub booms,
metal hits, risers, heartbeat, choir pad) but lays them out on the trailer's
own timeline instead of the 23-second teaser timeline.
"""
from __future__ import annotations

import wave
from pathlib import Path

import numpy as np

from . import audio as A

SR = A.SR
DURATION = 40.0

# --- trailer timeline (seconds) -------------------------------------------
OPEN = (0.0, 4.5)        # black sea, a whisper of a voice
WRECK = (4.5, 9.0)       # the ship you were not supposed to see
SAHUR = (9.0, 13.0)      # a raft, a heartbeat, no way back
SHARK = (13.0, 17.5)     # something follows the raft
ISLAND = (17.5, 22.0)    # landfall
COMBAT = (22.0, 27.0)    # blades, combos, counters
BOSS = (27.0, 31.5)      # the three that guard the island
CARDS = (31.5, 36.0)     # weapon cards
TITLE = (36.0, 40.0)     # title, link, silence

IMPACTS = [13.0, 17.5, 19.4, 21.0, 22.4, 23.6, 24.8, 26.0, 27.0, 28.4, 30.0, 31.5, 33.0, 34.4, 36.0]


def _bed(duration: float) -> np.ndarray:
    """Ocean surface, rain hiss and a slow sub drone."""
    n = A._n(duration)
    t = np.arange(n, dtype=np.float32) / SR

    surf = A._norm(A._filter(A._noise(duration, 7), hi=420), 1.0)
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * 0.085 * t - 0.9) ** 2
    surf = surf * swell * 0.30

    rain = A._norm(A._filter(A._noise(duration, 11), lo=2600), 1.0) * 0.05

    drone = (0.20 * np.sin(2 * np.pi * 36.0 * t)
             + 0.13 * np.sin(2 * np.pi * 54.0 * t + 1.1)
             + 0.08 * np.sin(2 * np.pi * 81.0 * t + 2.4))
    drone = drone * (0.55 + 0.45 * np.sin(2 * np.pi * 0.05 * t))
    drone = A._filter(drone.astype(np.float32), hi=130, order=1) * 0.55

    return (surf + rain + drone).astype(np.float32)


def build(duration: float = DURATION) -> np.ndarray:
    n = A._n(duration)
    mix = _bed(duration)
    if mix.size >= n:
        mix = mix[:n]

    def add(sig, at, gain=1.0):
        i = int(round(float(at) * SR))
        if i >= n or sig.size == 0:
            return
        seg = sig[:n - i]
        mix[i:i + seg.size] += seg * float(gain)

    # Opening: one long sub drop, then the sea alone.
    add(A._boom(4.0, 40.0, 3, 1.0), 0.0, 0.55)

    # Wreck: a slow swell that never resolves.
    add(A._riser(4.4, 41), 4.6, 0.42)
    add(A._pad(3.4, 98.0), 6.2, 0.55)

    # Sahur: heartbeat under the raft.
    for i in range(7):
        add(A._heart(0.45, 50 + i), 9.3 + i * 0.46, 0.42)

    # Shark: the first hard hit, then the trail of impacts.
    for k, at in enumerate(IMPACTS):
        if at < 13.0:
            continue
        heavy = 0.80 if at < 22.0 else (1.15 if at < 31.5 else 0.85)
        add(A._boom(2.2, 62.0 - 1.4 * k, 20 + k, 1.0), at, heavy)
        add(A._metal(0.9, 740.0 + 55.0 * k, 30 + k), at + 0.012, 0.18)

    # Riser into the boss reveal.
    add(A._riser(4.0, 63), 26.6, 0.70)

    # Boss: a heavier pad stack under the big beat.
    add(A._pad(4.0, 73.42), 27.0, 0.95)
    add(A._pad(4.0, 110.0), 27.05, 0.55)
    add(A._boom(3.0, 34.0, 77, 1.0), 27.0, 0.75)

    # Title: choir opens up, everything else falls away.
    add(A._pad(4.2, 110.0), 35.95, 1.05)
    add(A._pad(4.2, 164.81), 36.0, 0.70)
    add(A._pad(4.2, 220.0), 36.05, 0.42)

    mix = np.tanh(mix * 1.15).astype(np.float32)
    return A._norm(mix, 0.92)


def write_score(path, duration: float = DURATION):
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
    out = Path(__file__).resolve().parents[1] / "out" / "score_40s.wav"
    write_score(out)
    print(f"wrote {out}")
