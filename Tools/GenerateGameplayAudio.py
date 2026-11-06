"""Render Dark Brine's original gameplay Foley and cue previews (requires NumPy)."""

from pathlib import Path
import wave

import numpy as np


RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
STAGING = ROOT / "Temp" / "GameplayAudio"
BANK = "Assets/Resources/GameplayAudio/"
STORY = "Assets/Resources/StoryBattle/Audio/"
SWISHES = "Assets/Game/Prefabs/Characters/Sahur/Audio/"
COMBAT = "Assets/Resources/Combat/"


class Sound:
    def __init__(self, duration, seed):
        self.samples = np.zeros(round(duration * RATE))
        self.random = np.random.default_rng(seed)

    def time(self, duration):
        return np.arange(round(duration * RATE)) / RATE

    def add(self, start, samples):
        offset = round(start * RATE)
        length = min(len(samples), len(self.samples) - offset)
        self.samples[offset:offset + length] += samples[:length]

    def noise(self, t, low, high):
        frequencies = np.fft.rfftfreq(len(t), 1 / RATE)
        band = (frequencies / (frequencies + low)) ** 3
        band /= 1 + (frequencies / high) ** 4
        spectrum = np.fft.rfft(self.random.standard_normal(len(t)))
        samples = np.fft.irfft(spectrum * band, n=len(t))
        return samples / np.std(samples)

    def texture(self, start, duration, low, high, gain, attack, decay, flutter=0):
        t = self.time(duration)
        envelope = (1 - np.exp(-t / attack)) * np.exp(-t / decay)
        if flutter:
            envelope *= .69 + .31 * np.sin(2 * np.pi * flutter * t)
        fade = np.minimum(1, (duration - t) / .012)
        self.add(start, gain * self.noise(t, low, high) * envelope * fade)

    def ring(self, start, duration, frequency, end, gain, decay, attack=.0015):
        t = self.time(duration)
        phase = 2 * np.pi * (frequency * t + (end - frequency) * t ** 2 / (2 * duration))
        envelope = (1 - np.exp(-t / attack)) * np.exp(-t / decay)
        fade = np.minimum(1, (duration - t) / .01)
        self.add(start, gain * np.sin(phase) * envelope * fade)

    def tick(self, start, gain=1, pitch=1):
        self.texture(start, .05, 650, 5700, .27 * gain, .0006, .009)
        for frequency, amplitude, decay in [(575, .27, .018), (1460, .22, .015), (2680, .14, .009)]:
            self.ring(start, .07, frequency * pitch, frequency * pitch, amplitude * gain, decay)

    def sweep(self, start, duration, weight, brightness):
        t = self.time(duration)
        x = t / duration
        air = self.noise(t, 360, 1900) * (1 - x) + self.noise(t, 1250, brightness) * x
        self.add(start, weight * air * np.sin(np.pi * x) ** 3 * (.5 + x))

    def voice(self, start, duration, f0, end, formants, gain):
        t = self.time(duration)
        contour = np.interp(t / duration, [0, .22, .6, 1], [f0 * .82, f0, f0 * .9, end])
        contour *= 1 + .045 * np.sin(2 * np.pi * 21 * t)
        phase = 2 * np.pi * np.cumsum(contour) / RATE
        signal = np.zeros(len(t))
        for harmonic in range(1, 28):
            frequency = harmonic * contour
            resonance = sum(np.exp(-.5 * ((frequency - center) / width) ** 2) * level
                            for center, width, level in formants)
            signal += np.sin(harmonic * phase) * (.22 + resonance) / np.sqrt(harmonic)
        breath = self.noise(t, 380, 2400) * (.12 + .08 * np.sin(phase))
        self.add(start, gain * (signal + breath) * np.sin(np.pi * t / duration) ** .78)


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    assert np.max(np.abs(samples)) <= 1, path
    with wave.open(str(path), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(np.rint(samples * 32767).astype("<i2").tobytes())


def render():
    clips = {}

    def save(path, sound, peak):
        samples = sound.samples - np.mean(sound.samples)
        if path != BANK + "GrassRustle":
            fade = np.linspace(0, 1, 132)
            samples[:len(fade)] *= fade
            samples[-len(fade):] *= fade[::-1]
        samples = samples * (peak / np.max(np.abs(samples)))
        clips[path] = samples
        write_wav(STAGING / (path + ".wav"), samples)

    for index, (path, duration, brightness, peak) in enumerate([
        (BANK + "Swing", .21, 4100, .53),
        (SWISHES + "swish-1", .19, 4350, .47),
        (SWISHES + "swish-5", .22, 5150, .51),
        (SWISHES + "swish-9", .28, 3750, .59),
        (SWISHES + "swish-12", .34, 3250, .69),
        (BANK + "CapriSweep", .22, 6900, .66),
    ]):
        sound = Sound(duration, 220 + index)
        sound.sweep(.003, duration * .74, .34, brightness)
        sound.texture(.023, duration - .023, 125, 730, .17 + index * .013, .021, .075, 27)
        if index == 5:
            sound.ring(.052, .153, 2100, 1080, .21, .065, .013)
        save(path, sound, peak)

    for index, (path, duration, low, high, modes, peak) in enumerate([
        (STORY + "StickHit", .13, 900, 5200, [(490, .021), (1010, .018), (1740, .012)], .57),
        (STORY + "Guard", .31, 780, 7100, [(735, .062), (1577, .054), (2437, .071)], .63),
        (COMBAT + "ClubSurface", .14, 280, 1700, [(162, .041), (289, .029)], .47),
        (COMBAT + "ClubBody", .24, 65, 510, [(98, .058), (174, .032)], .59),
        (BANK + "CapriImpact", .25, 980, 7400, [(616, .046), (1852, .05), (2997, .043)], .74),
        (COMBAT + "ClubWood", .17, 330, 3300, [(307, .047), (684, .034), (1258, .026)], .54),
    ]):
        sound = Sound(duration, 320 + index)
        sound.texture(0, duration, low, high, .27, .001, .018)
        for order, (frequency, decay) in enumerate(modes):
            sound.ring(0, duration, frequency, frequency * (.83 if index in (2, 3) else .98),
                       .42 / (order + 1), decay)
        sound.texture(.026, duration - .026, low, high * .62, .08, .004, .027)
        if path == COMBAT + "ClubWood":
            sound.tick(.025, .22, .63)
        save(path, sound, peak)

    sound = Sound(.24, 440)
    sound.texture(0, .15, 260, 2500, .28, .002, .023)
    sound.voice(.014, .19, 157, 98, [(520, 180, .8), (1150, 270, .55)], .26)
    save(BANK + "Hurt", sound, .35)

    sound = Sound(.32, 441)
    sound.sweep(0, .23, .32, 3600)
    sound.texture(.032, .26, 180, 1850, .22, .021, .071, 40)
    sound.texture(.141, .17, 560, 4100, .15, .006, .028, 58)
    save(BANK + "Dodge", sound, .57)

    sound = Sound(.44, 442)
    sound.texture(0, .17, 1350, 11300, .68, .00065, .008)
    sound.texture(.001, .24, 185, 2900, .51, .0007, .033)
    sound.ring(.001, .19, 166, 59, .56, .031)
    for time, gain in [(.052, .15), (.094, .10), (.157, .055), (.228, .037)]:
        sound.texture(time, .18, 210, 2150, gain, .007, .043)
    sound.tick(.048, .12, 1.2)
    save(BANK + "Gunshot", sound, .94)

    sound = Sound(.58, 443)
    for time, gain, pitch in [(0, .64, 1.24), (.083, .84, .71), (.286, 1, .86), (.427, .69, 1.46), (.468, .74, 1.09)]:
        sound.tick(time, gain, pitch)
        sound.texture(time, .072, 350, 2400, .16 * gain, .004, .019)
    save(BANK + "Reload", sound, .69)

    sound = Sound(.32, 444)
    for time, frequency, gain in [(0, 523.25, .35), (.047, 783.99, .5), (.105, 1046.5, .47)]:
        sound.ring(time, .21, frequency, frequency, gain, .051, .004)
        sound.ring(time, .15, frequency * 2.42, frequency * 2.4, gain * .32, .025)
    save(BANK + "Pickup", sound, .56)

    sound = Sound(.078, 445)
    sound.tick(0, .8, .9)
    sound.tick(.016, .22, .72)
    save(BANK + "Click", sound, .27)

    for index, path in enumerate([BANK + "Warning", BANK + "CapriWarning", BANK + "NailongSonar"]):
        sound = Sound(.39, 448 + index)
        base = [360, 518, 743][index]
        for time, pitch, level in [(0, 1, .58), (.084, 1.25, .42)]:
            sound.ring(time, .27, base * pitch, base * pitch * .81, level, .058, .003)
            sound.ring(time, .22, base * pitch * 2.63, base * pitch * 2.32, .23, .044, .006)
        sound.texture(.02, .3, 180, 900, .13, .017, .1)
        save(path, sound, .54 if index == 2 else .64)

    for index, (path, duration, calls, formants, peak) in enumerate([
        ("GooseHonk", .43, [(0, .28, 395, 290), (.285, .13, 330, 234)],
         [(895, 210, 1.1), (1590, 260, .8), (2770, 420, .55)], .62),
        ("GooseDefeat", .62, [(0, .22, 352, 259), (.247, .35, 296, 146)],
         [(710, 185, 1.15), (1280, 260, .68)], .6),
        ("NailongRoar", .86, [(0, .82, 118, 59)],
         [(290, 115, 1.3), (630, 230, 1.05), (1430, 380, .51)], .83),
        ("NailongScold", .76, [(0, .22, 215, 149), (.23, .18, 268, 172), (.425, .31, 204, 112)],
         [(560, 165, 1.1), (1370, 250, .9), (2380, 410, .54)], .67),
    ]):
        sound = Sound(duration, 490 + index)
        for time, length, frequency, end in calls:
            sound.voice(time, length, frequency, end, formants, .57)
        if index == 2:
            sound.texture(.02, .81, 75, 820, .26, .047, .23, 31)
        save(BANK + path, sound, peak)

    for index, (path, duration, peak) in enumerate([
        (BANK + "NailongSpit", .23, .65), (STORY + "TidalHit", .68, .79),
        (STORY + "EncounterStart", 2.4, .83),
    ]):
        sound = Sound(duration, 502 + index)
        start = .43 if index == 2 else 0
        if index == 2:
            sound.sweep(0, 1.58, .32, 2900)
            sound.ring(.11, 2.26, 78, 51, .32, .77, .19)
            sound.ring(.48, 1.8, 193, 170, .15, .46, .015)
            for time in [.46, .488, .59, .73]:
                sound.tick(time, .35, .57)
        sound.texture(start, duration - start, 275, 4850, .42, .004, .071 + index * .12, 29)
        sound.texture(start, duration - start, 90, 730, .42, .003, .065 + index * .1)
        sound.ring(start, min(.31, duration), 212 if index == 0 else 117, 60, .4, .041 + index * .026)
        for drop in range(9 + index * 4):
            time = start + sound.random.uniform(.018, (duration - start) * .7)
            frequency = sound.random.uniform(390, 1620)
            sound.ring(time, .09, frequency, frequency * 1.49, .14 * np.exp(-(time - start)), .019)
        save(path, sound, peak)

    for index, (path, duration, frequency) in enumerate([
        ("CottonPress", .26, 132), ("CottonLift", .28, 225), ("CottonLand", .36, 105),
    ]):
        sound = Sound(duration, 520 + index)
        sound.texture(0, duration, 150, 1700, .22, .012, .061, 44)
        sound.ring(.006, duration - .006, frequency, frequency * (1.38 if index == 1 else .53),
                   .41, .065, .01)
        save(BANK + path, sound, .44 + .06 * index)

    sound = Sound(2.4, 540)
    t = sound.time(2.4)
    sound.samples = sound.noise(t, 450, 4100) * (.65 + .2 * np.sin(2 * np.pi * 5 * t / 2.4))
    sound.samples += .3 * sound.noise(t, 950, 6400) * (1 + np.sin(2 * np.pi * 13 * t / 2.4))
    save(BANK + "GrassRustle", sound, .59)

    return clips


def preview(clips):
    # Match the in-game order and approximate the gains of the impact layers.
    sequence = Sound(15.9, 0)
    for index, (time, swish, surface, weight) in enumerate([
        (.25, 1, COMBAT + "ClubSurface", .32), (.88, 5, COMBAT + "ClubSurface", .32),
        (1.49, 9, COMBAT + "ClubSurface", .8), (2.39, 12, COMBAT + "ClubSurface", .8),
        (3.34, 1, STORY + "Guard", 0),
    ]):
        sequence.add(time, .58 * clips[SWISHES + f"swish-{swish}"])
        hit = time + (.175 if swish == 12 else .095)
        sequence.add(hit, (.6 if index == 4 else .85) * .7 * clips[STORY + "StickHit"])
        sequence.add(hit, .7 * weight * clips[COMBAT + "ClubBody"])
        sequence.add(hit, .245 * clips[surface])
    for time, path, gain in [
        (4.25, BANK + "Dodge", .63), (4.85, BANK + "Hurt", .72),
        (5.42, BANK + "Gunshot", .7), (5.52, BANK + "Gunshot", .7), (5.62, BANK + "Gunshot", .7),
        (6.40, BANK + "Reload", .68), (7.26, BANK + "Click", .62), (7.63, BANK + "Pickup", .61),
        (8.39, BANK + "CapriWarning", .63), (8.89, BANK + "CapriSweep", .61),
        (9.03, BANK + "CapriImpact", .67), (9.67, BANK + "GooseHonk", .65),
        (10.26, BANK + "NailongRoar", .7), (11.31, BANK + "NailongSpit", .66),
        (11.61, BANK + "NailongSonar", .63), (12.12, STORY + "TidalHit", .65),
        (13.13, STORY + "EncounterStart", .85),
    ]:
        sequence.add(time, gain * clips[path])
    write_wav(ROOT / ".codex" / "Dark Brine - Gameplay SFX.wav", sequence.samples)


if __name__ == "__main__":
    preview(render())
