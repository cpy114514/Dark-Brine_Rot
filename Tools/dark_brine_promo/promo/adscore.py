"""Beat-locked score for the Dark Brine_Rot ads.

Everything is synthesized from scratch on the same beat grid the picture
uses (:class:`promo.ad.Ad`), so the chant, the drums and the cuts cannot
drift apart.

The chant is a formant synthesizer: a band-limited saw glottal source
pushed through three resonant peaks. The vowels are deliberately flat and
mechanical - a "broadcast" voice, which is what makes a repeated phrase
stick.
"""
from __future__ import annotations

import math
import wave
from pathlib import Path

import numpy as np

from . import audio as A

SR = A.SR

# Three formants per vowel: (frequency Hz, Q)
VOWELS = {
    "ah": ((730.0, 9.0), (1090.0, 11.0), (2440.0, 13.0)),
    "eh": ((530.0, 8.0), (1840.0, 11.0), (2480.0, 13.0)),
    "oh": ((570.0, 8.0), (840.0, 10.0), (2410.0, 13.0)),
}


def _saw(f0, dur: float, harmonics: int = 24) -> np.ndarray:
    """Band-limited saw whose instantaneous pitch follows the curve f0."""
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    phase = np.cumsum(np.asarray(f0, dtype=np.float32)) / SR
    sig = np.zeros(n, dtype=np.float32)
    for h in range(1, harmonics + 1):
        sig += np.sin(2 * np.pi * h * phase + h * 0.37) / h
    return (sig * (2.0 / harmonics)).astype(np.float32)


def _formant(x: np.ndarray, freq: float, q: float) -> np.ndarray:
    """Resonant peak at freq, approximated in the frequency domain."""
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(x.size, 1.0 / SR)
    gain = 1.0 / (1.0 + ((f - freq) / (freq / q)) ** 2)
    return np.fft.irfft(spec * gain, n=x.size).astype(np.float32)


def _syllable(dur: float, f0: float, vowel: str = "ah", gain: float = 1.0,
              seed: int = 0) -> np.ndarray:
    """One chant syllable: consonant transient, vowel body, sung envelope."""
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    # a small scoop into the note, then a slow settle
    f0c = f0 * (1.0 + 0.055 * np.exp(-t * 42.0) - 0.028 * np.exp(-t * 8.0))
    src = _saw(f0c, dur)

    out = np.zeros(n, dtype=np.float32)
    for i, (fc, q) in enumerate(VOWELS.get(vowel, VOWELS["ah"])):
        out += _formant(src, fc, q) * (1.0 / (i + 1.7))

    # consonant burst in front of the vowel
    cons = A._filter(A._noise(dur, seed), lo=1900, hi=7000) * np.exp(-t * 48.0) * 0.22
    out += cons

    env = np.minimum(1.0, t / 0.026) * np.exp(-t * 4.6)
    out = np.tanh(out * 1.6) * 0.55
    return (out * env * float(gain)).astype(np.float32)


def _kick(dur: float = 0.46, f0: float = 132.0, drop: float = 44.0) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    f = drop + (f0 - drop) * np.exp(-t * 24.0)
    ph = np.cumsum(f) / SR
    body = np.sin(2 * np.pi * ph) * np.exp(-t * 6.5)
    click = A._filter(A._noise(dur, 3), hi=2200) * np.exp(-t * 95.0) * 0.45
    return (body + click).astype(np.float32)


def _snare(dur: float = 0.26, seed: int = 7) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    noise = A._filter(A._noise(dur, seed), lo=1700) * np.exp(-t * 20.0)
    tone = np.sin(2 * np.pi * 188.0 * t) * np.exp(-t * 28.0) * 0.42
    return (noise * 0.85 + tone).astype(np.float32)


def _hat(dur: float = 0.085, seed: int = 5, bright: float = 8600.0) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    x = A._filter(A._noise(dur, seed), lo=5400, hi=bright)
    return (x * np.exp(-t * 72.0)).astype(np.float32)


def _sub(dur: float, f: float, gain: float = 1.0) -> np.ndarray:
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    sig = np.sin(2 * np.pi * f * t) + 0.22 * np.sin(4 * np.pi * f * t)
    return (sig * np.exp(-t * 1.5) * 0.30 * float(gain)).astype(np.float32)


def _stab(dur: float, root: float, gain: float = 1.0, seed: int = 13) -> np.ndarray:
    """Detuned synth stab on the root - the chord under the chant."""
    n = A._n(dur)
    t = np.arange(n, dtype=np.float32) / SR
    sig = np.zeros(n, dtype=np.float32)
    for mul, g in ((1.0, 0.5), (1.5, 0.26), (2.0, 0.18), (3.0, 0.09)):
        det = 1.0 + 0.004 * np.sin(2 * np.pi * 5.5 * t)
        sig += g * _saw(root * mul * det, dur, harmonics=14)
    env = np.exp(-t * 7.0) * np.minimum(1.0, t / 0.008)
    return (np.tanh(sig * 1.4) * env * 0.55 * float(gain)).astype(np.float32)


def _sea_bed(duration: float, seed: int = 17) -> np.ndarray:
    """Thin ocean/air layer so the ads are not dry."""
    n = A._n(duration)
    t = np.arange(n, dtype=np.float32) / SR
    surf = A._norm(A._filter(A._noise(duration, seed), hi=420), 1.0) * 0.22
    swell = 0.55 + 0.45 * np.sin(2 * math.pi * 0.09 * t - 0.9) ** 2
    surf = surf * swell
    drone = (np.sin(2 * np.pi * 36.0 * t) * 0.16
             + np.sin(2 * np.pi * 54.0 * t + 1.1) * 0.10)
    drone = A._filter(drone.astype(np.float32), hi=140, order=1) * 0.5
    return (surf + drone).astype(np.float32)


def _chant_pitch(ad, bar: int) -> float:
    """The hook climbs for the second half, then the ad loops back down."""
    semis = 0 if bar < ad.bars / 2 else 2
    return ad.root * 2.0 * (2.0 ** (semis / 12.0))


def build(ad) -> np.ndarray:
    """Render the whole ad score onto the ad's own beat grid."""
    duration = ad.duration
    n = A._n(duration)
    mix = _sea_bed(duration)

    def add(sig, at, gain=1.0):
        i = int(round(float(at) * SR))
        if i >= n or sig.size == 0:
            return
        seg = sig[:n - i]
        mix[i:i + seg.size] += seg * float(gain)

    # ------------------------------------------------------------- drums
    for bar in range(ad.bars):
        for beat in range(4):
            at = ad.t_of(bar, beat)
            if beat in (0, 2):
                add(_kick(0.46, 138.0 if beat == 0 else 124.0), at,
                    1.00 if beat == 0 else 0.78)
            else:
                add(_snare(0.26, seed=7 + bar * 4 + beat), at, 0.52)
            # eighth-note hats
            add(_hat(0.085, seed=5 + bar * 8 + beat * 2), at, 0.30)
            add(_hat(0.06, seed=90 + bar * 8 + beat * 2), at + ad.spb * 0.5, 0.17)
        add(_sub(ad.spb * 3.6, ad.root, 0.9), ad.t_of(bar, 0), 1.0)

    # ------------------------------------------------------------- chant
    for bar, beat, word, vowel in ad.chant:
        at = ad.t_of(bar, beat)
        f0 = _chant_pitch(ad, bar)
        dur = ad.spb * 0.80
        add(_syllable(dur, f0, vowel, 0.95, seed=bar * 7 + beat), at, 1.0)
        # a short stab under the syllable gives the hook its body
        add(_stab(min(dur, ad.spb * 0.55), f0, 0.55, seed=bar + beat), at, 1.0)

    # -------------------------------------------------------------- hits
    for bar, beat, kind in ad.hits:
        at = ad.t_of(bar, beat)
        if kind == "metal":
            add(A._metal(0.55, 900.0 + 70.0 * (bar % 5), 30 + bar), at, 0.30)
            add(_kick(0.34, 150.0), at, 0.55)
        elif kind == "boom":
            add(A._boom(1.8, 58.0, 20 + bar, 1.0), at, 0.62)
        elif kind == "stab":
            add(_stab(ad.spb * 0.7, _chant_pitch(ad, bar) * 2.0, 0.8, seed=bar), at, 1.0)

    # riser into the final bar, then a hard stop on the last beat
    add(A._riser(ad.spb * 3.0, 41), ad.t_of(ad.bars - 1, 0), 0.42)

    mix = np.tanh(mix * 1.18).astype(np.float32)
    mix = A._norm(mix, 0.94)

    # the last 0.12s is silence: a gap is more catchy than a fade
    tail = int(0.12 * SR)
    if tail < mix.size:
        mix[-tail:] *= np.linspace(1.0, 0.0, tail, dtype=np.float32)
    return mix


def write_score(path, ad) -> Path:
    mono = build(ad)
    n = mono.size

    delay = int(0.011 * SR)
    left = mono.copy()
    right = np.empty_like(mono)
    right[:delay] = mono[:delay]
    right[delay:] = mono[:n - delay]
    right *= 0.95

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
