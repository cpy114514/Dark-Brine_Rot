"""Original offline synthesis for Capri's second phase (no third-party audio)."""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
OUT = Path('Assets/Resources/CapriPhase2')
OUT.mkdir(parents=True, exist_ok=True)


def write(name, duration, sample):
    rng = random.Random(773)
    data = bytearray()
    for i in range(round(duration * RATE)):
        t = i / RATE
        value = sample(t, rng)
        data.extend(struct.pack('<h', round(max(-.93, min(.93, value)) * 32767)))
    with wave.open(str(OUT / (name + '.wav')), 'wb') as out:
        out.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        out.writeframes(data)


def steam(t, rng):
    p = t / 1.15
    env = math.sin(math.pi * min(1, p)) ** 1.4
    return env * (.1 * rng.uniform(-1, 1) + .16 * math.sin(2 * math.pi * (125 * t + 95 * t * t)))


def impact(t, rng):
    return .48 * math.exp(-9 * t) * math.sin(2 * math.pi * (65 * t + 9 * (1-math.exp(-15*t)))) + .19 * rng.uniform(-1, 1) * math.exp(-18*t)


def blade(t, rng):
    env = math.sin(math.pi * min(1, t / .27)) ** 1.3
    return env * (.23 * rng.uniform(-1, 1) + .12 * math.sin(2 * math.pi * (1400*t-1700*t*t)))


BPM = 120
LENGTH = 8.0  # Four complete bars: seamless beat-aligned loop.


def rhythm(t, rng):
    beat = t % .5
    eighth = t % .25
    step = int(t / .5)
    kick = .24 * math.exp(-26 * beat) * math.sin(2*math.pi*(48*beat+3*(1-math.exp(-35*beat))))
    hat = .025 * rng.uniform(-1,1) * math.exp(-95*eighth)
    snare = .09 * rng.uniform(-1,1) * math.exp(-32*beat) if step % 2 else 0
    note = (73.416, 73.416, 87.307, 65.406)[(step//4)%4]
    envelope = math.sin(math.pi*beat/.5)**1.5
    bass = .1 * envelope * (math.sin(2*math.pi*note*t)+.22*math.sin(2*math.pi*note*2*t))
    return kick + hat + snare + bass


write('SteamCharge', 1.15, steam)
write('CremaImpact', .8, impact)
write('BladeAccent', .27, blade)
write('RistrettoRhythm', LENGTH, rhythm)
print('Built four original mono WAVs at 22050 Hz.')
