"""Frame composition and the render loop for the Dark Brine_Rot teaser."""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from . import effects as fx
from . import entities
from . import typography as ty
from . import util
from .background import OceanBackdrop
from .encode import encode_video


CUTS = [2.8, 5.4, 7.2, 8.6, 10.0, 11.4, 13.0, 14.6, 16.2, 16.78, 17.34, 17.94, 18.6, 21.2]

WEAPON_BEATS = [
    ("club", "CLUB", "BLUNT FORCE // CLOSE RANGE", "01"),
    ("blades", "DUAL BLADES", "BLEED FAST // NEVER STOP", "02"),
    ("rifle", "M4A1", "MAKE IT LOUD // MAKE IT COUNT", "03"),
]

BOSS_BEATS = [
    ("cappuccino", "CAPPUCCINO ASSASSINO", "BOSS 01 // BREWED IN THE DARK", C.EMBER),
    ("nailong", "NAILONG", "BOSS 02 // IT COILS BENEATH", C.TEAL),
    ("crocodilo", "BOMBARDINO CROCODILO", "BOSS 03 // THE TIDE EXPLODES", C.EMBER),
]

# Figures are authored in a 1000-unit square, so each entry is
# (sprite size as a fraction of frame height, vertical centre as a
# fraction of frame height).  The values keep every figure's feet just
# above the lower title band and its head below the upper text band.
FIGURE_LAYOUT = {
    "sahur": (0.58, 0.394),
    "club": (0.52, 0.420),
    "blades": (0.52, 0.528),
    "rifle": (0.86, 0.445),
    "cappuccino": (0.54, 0.415),
    "nailong": (0.56, 0.400),
    "crocodilo": (0.58, 0.391),
}

# Name cards sit below every figure's feet and above the letterbox.
CARD_Y = 0.775
CARD_DY = 0.056

# The foreground shore every figure stands on, as a fraction of height.
SHORE_Y = 0.665

HYPE_BEATS = [
    (16.20, "HUNT."),
    (16.78, "BLEED."),
    (17.34, "SURVIVE."),
    (17.94, "ROT."),
]


def _env(t, a, b, fade_in=0.25, fade_out=0.25):
    return util.smoothstep(a, a + fade_in, t) * (1.0 - util.smoothstep(b - fade_out, b, t))


class _Dust:
    """Slow drifting motes that give the frame depth."""

    def __init__(self, w, h, count=110, seed=12):
        rng = np.random.default_rng(int(seed))
        self.x = rng.random(count)
        self.y = rng.random(count)
        self.v = rng.uniform(0.5, 2.4, count)
        self.ph = rng.uniform(0.0, math.tau, count)
        self.r = rng.uniform(1.2, 3.4, count)

    def draw(self, arr, t, amount=1.0, color=C.FOAM):
        h, w = arr.shape[:2]
        s = h / 1080.0
        for x, y, v, ph, r in zip(self.x, self.y, self.v, self.ph, self.r):
            px = (float(x) * w + math.sin(t * 0.4 + float(ph)) * 40.0) % w
            py = (float(y) * h - t * float(v) * 16.0) % h
            arr = util.add_glow(arr, px, py, max(2.5, float(r) * 3.0 * s), color, 0.05 * amount, 2.0)
        return arr


class Renderer:
    def __init__(self, width=C.WIDTH, height=C.HEIGHT, fps=C.FPS, seed=3):
        self.w = int(width)
        self.h = int(height)
        self.fps = int(fps)
        self.s = self.h / 1080.0
        self.aspect = 2.39
        self.bar = max(0, int((self.h - self.w / self.aspect) / 2))

        self.backdrop = OceanBackdrop(self.w, self.h, seed=seed)
        self.grain = util.grain_bank(10, self.w, self.h, seed=seed + 5)
        self.vig = util.vignette_mask(self.w, self.h, 0.66, 1.9)
        self.dust = _Dust(self.w, self.h)

        self._sprites = {}
        self._cache = {}

    # ------------------------------------------------------------ sprites
    def _sprite(self, key, maker, size):
        size = max(80, int(round(float(size) / 32.0)) * 32)
        ck = (key, size)
        sp = self._sprites.get(ck)
        if sp is None:
            sp = maker(size)
            if len(self._sprites) > 48:
                self._sprites.pop(next(iter(self._sprites)))
            self._sprites[ck] = sp
        return sp

    def _rotated(self, key, sprite, deg):
        q = round(float(deg) * 2.0) / 2.0
        ck = ("rot", key, q)
        out = self._cache.get(ck)
        if out is None:
            out = sprite.rotate(q, expand=True, resample=Image.BICUBIC)
            if len(self._cache) > 120:
                self._cache.pop(next(iter(self._cache)))
            self._cache[ck] = out
        return out

    def _name_card(self, arr, name, sub, p, y=0.80, size=70.0, color=C.BONE, glow=10.0):
        s = self.s
        arr = ty.caption(arr, name, y=y, size=size * s, tracking=16.0 * s, color=color,
                         opacity=util.smoothstep(0.0, 0.35, p) * 0.98, glow=glow)
        if sub:
            arr = ty.caption(arr, sub, y=y + CARD_DY, size=22.0 * s, tracking=9.0 * s,
                             color=C.FOAM, opacity=util.smoothstep(0.22, 0.75, p) * 0.86)
        return arr

    # --------------------------------------------------------------- shots
    def _open(self, t):
        w, h, s = self.w, self.h, self.s
        arr = self.backdrop.render(t, glow=0.15 + 0.85 * util.smoothstep(0.2, 2.2, t),
                                   fog=0.45, rain=0.04, swell=0.7, moon_x=0.67, moon_y=0.33)
        arr *= util.smoothstep(0.0, 1.5, t)

        a = _env(t, 1.05, 2.75, 0.55, 0.45)
        arr = ty.caption(arr, "SOMETHING IS ROTTING BENEATH THE WAVES", y=0.80,
                         size=30.0 * s, tracking=13.0 * s, color=C.FOAM, opacity=0.85 * a)
        b = _env(t, 1.95, 2.80, 0.45, 0.30)
        arr = ty.caption(arr, "DARK BRINE_ROT", y=0.175, size=22.0 * s,
                         tracking=18.0 * s, color=C.TEAL, opacity=0.55 * b)
        arr += np.asarray(C.TEAL, np.float32) * (0.10 * util.smoothstep(2.50, 2.80, t))
        return arr

    def _island(self, t):
        w, h, s = self.w, self.h, self.s
        p = util.clamp((t - 2.8) / 2.6)
        zap = 0.45 * util.pulse(t, 3.02, 0.055) + 0.20 * util.pulse(t, 4.60, 0.050)
        arr = self.backdrop.render(t, glow=0.50, fog=0.90, rain=0.30, swell=1.15,
                                   rise=0.05 * util.smoothstep(0.0, 0.7, p),
                                   moon_x=0.26, moon_y=0.30, flash=zap)
        arr *= 0.92

        arr = ty.caption(arr, "NO MAP. NO MERCY.", y=0.55, size=64.0 * s, tracking=16.0 * s,
                         color=C.BONE, opacity=_env(t, 3.25, 4.18, 0.30, 0.30), glow=8.0)
        arr = ty.caption(arr, "THE ISLAND NEVER SLEEPS", y=0.655, size=26.0 * s,
                         tracking=11.0 * s, color=C.FOAM,
                         opacity=0.80 * _env(t, 4.35, 5.32, 0.35, 0.25))
        return arr

    def _shore(self, arr, top_y=None):
        """Dark foreground shore the figures stand on.

        Without a ground plane the figures float over the water and the
        shot falls apart; this puts a near-black bank under them with a
        faint teal rim and a band of mist along its edge.
        """
        h, w = arr.shape[:2]
        top = h * SHORE_Y if top_y is None else float(top_y)

        xs = np.arange(w, dtype=np.float32)
        edge = (top
                - 22.0 * np.sin(xs / 260.0 + 0.7)
                - 11.0 * np.sin(xs / 91.0 + 2.1)
                - 6.0 * np.sin(xs / 37.0))
        yy = np.arange(h, dtype=np.float32)[:, None]
        mask = (yy >= edge[None, :]).astype(np.float32)

        soft = Image.fromarray((mask * 255).astype(np.uint8), "L")
        soft = soft.filter(ImageFilter.GaussianBlur(2.2))
        mask = np.asarray(soft, dtype=np.float32) / 255.0

        base = np.asarray((7, 13, 15), np.float32)[None, None, :]
        depth = np.clip((yy - edge[None, :]) / max(1.0, h * 0.30), 0.0, 1.0)[..., None]
        arr = arr * (1.0 - mask[..., None]) + (base * (1.0 - 0.45 * depth)) * mask[..., None]

        rim = np.clip(mask - np.roll(mask, 3, axis=0), 0.0, 1.0)
        arr += rim[..., None] * np.asarray((20, 48, 50), np.float32)

        mist = np.exp(-((yy - edge[None, :]) / (h * 0.020)) ** 2)
        mist *= (yy < edge[None, :])
        arr += mist[..., None] * np.asarray(C.FOAM, np.float32) * 0.075
        return arr

    def _figure(self, arr, key, maker, t, p, cx=0.5, rise=0.0, shadow=False):
        """Blit a figure using the shared layout table, with a contact shadow."""
        layout_key = key[2:] if key.startswith("b_") else key
        size_f, cy_f = FIGURE_LAYOUT[layout_key]
        size = size_f * self.h
        sp = self._sprite(key, maker, size)
        cx_px = self.w * cx
        cy_px = self.h * cy_f + rise

        if shadow:
            sh = self._sprite("shadow", lambda sz: entities.ground_shadow(sz), size)
            arr = entities.blit(arr, sh, cx_px, cy_px + size * 0.44,
                                opacity=0.55 * util.smoothstep(0.0, 0.35, p))

        arr = entities.blit(arr, sp, cx_px, cy_px,
                            opacity=util.smoothstep(0.0, 0.26, p))
        return arr

    def _sahur(self, t):
        w, h, s = self.w, self.h, self.s
        p = util.clamp((t - 5.4) / 1.8)
        arr = self.backdrop.render(t, glow=0.30, fog=0.75, rain=0.55, swell=1.0,
                                   moon_x=0.5, moon_y=0.26, tint=0.45)
        arr = util.add_glow(arr, w * 0.5, h * 0.86, 340.0 * s, C.TEAL, 0.24)
        arr = self._shore(arr)

        rise = (1.0 - util.ease_out_cubic(p)) * 240.0 * s
        arr = self._figure(arr, "sahur",
                           lambda sz: entities.sahur("club", sz, C.TEAL),
                           t, p, cx=0.5, rise=rise, shadow=True)
        arr = self._name_card(arr, "SAHUR", "THE TIDE SENT HER BACK",
                              util.smoothstep(0.05, 0.55, p), y=CARD_Y, size=64.0)
        return arr

    def _weapon(self, t, idx, p):
        w, h, s = self.w, self.h, self.s
        arr = self.backdrop.render(t, glow=0.40, fog=0.55, rain=0.20, swell=0.85,
                                   moon_x=0.5, tint=0.45) * 0.95
        arr = util.add_glow(arr, w * 0.5, h * 0.47, 560.0 * s, C.TEAL, 0.34)
        arr = util.add_glow(arr, w * 0.5, h * 0.52, 300.0 * s, C.FOAM, 0.14)
        arr = self._shore(arr)

        key, name, desc, num = WEAPON_BEATS[idx]
        size_f, cy_f = FIGURE_LAYOUT[key]
        size = size_f * h
        sp = self._sprite("w_" + key,
                          lambda sz, k=key: entities.WEAPONS[k](sz, C.TEAL), size)
        deg = (1.0 - util.ease_out_cubic(p)) * 7.0 + math.sin(t * 1.1) * 1.0
        sp = self._rotated(key, sp, deg)

        cx = w * (0.5 - 0.06 * util.ease_out_cubic(p))
        cy = h * cy_f + (1.0 - util.ease_out_cubic(p)) * 90.0 * s
        arr = entities.blit(arr, sp, cx, cy, opacity=util.smoothstep(0.0, 0.18, p))
        arr = self._name_card(arr, name, desc, util.smoothstep(0.10, 0.50, p),
                              y=CARD_Y, size=58.0)
        arr = ty.caption(arr, num + " / 03", y=0.178, size=20.0 * s, tracking=10.0 * s,
                         color=C.RUST, opacity=0.85 * util.smoothstep(0.15, 0.50, p))
        return arr

    def _boss(self, t, idx, p):
        w, h, s = self.w, self.h, self.s
        key, name, desc, glow = BOSS_BEATS[idx]
        arr = self.backdrop.render(t, glow=0.25, fog=0.80, rain=0.60, swell=1.20,
                                   moon_x=0.5, moon_y=0.24, tint=0.50)
        arr = util.add_glow(arr, w * 0.5, h * 0.40, 460.0 * s, glow, 0.24)
        arr = self._shore(arr)

        rise = (1.0 - util.ease_out_cubic(p)) * 170.0 * s
        arr = self._figure(arr, "b_" + key,
                           lambda sz, k=key, g=glow: entities.BOSSES[k](sz, g),
                           t, p, cx=0.5, rise=rise, shadow=True)
        arr = self._name_card(arr, name, desc, util.smoothstep(0.08, 0.50, p),
                              y=CARD_Y, size=52.0)
        flicker = 0.02 + 0.02 * math.sin(t * 27.0)
        arr += np.asarray(glow, np.float32) * flicker
        return arr

    def _hype(self, t):
        w, h, s = self.w, self.h, self.s
        idx = 0
        for i, (bt, _word) in enumerate(HYPE_BEATS):
            if t >= bt:
                idx = i
        bt, word = HYPE_BEATS[idx]
        p = util.clamp((t - bt) / 0.55)

        arr = self.backdrop.render(t, glow=0.20, fog=0.60, rain=0.35, swell=1.40,
                                   moon_x=0.5, tint=0.65) * 0.50
        arr = util.add_glow(arr, w * 0.5, h * 0.50, 620.0 * s, C.RUST, 0.10)

        e = util.ease_out_cubic(min(1.0, p * 1.5))
        size = 170.0 * s * (1.0 + 0.30 * (1.0 - e))
        arr = ty.caption(arr, word, y=0.50, size=size, tracking=util.lerp(30.0, 8.0, e) * s,
                         color=C.BONE, opacity=util.smoothstep(0.0, 0.16, p), glow=16.0,
                         glow_color=C.RUST)
        arr = ty.caption(arr, "DARK BRINE_ROT", y=0.826, size=20.0 * s, tracking=14.0 * s,
                         color=C.FOAM, opacity=0.42)
        return arr

    def _title(self, t):
        w, h, s = self.w, self.h, self.s
        p = util.clamp((t - 18.6) / 1.7)
        arr = self.backdrop.render(t, glow=1.0, fog=0.50, rain=0.12, swell=0.90,
                                   moon_x=0.5, moon_y=0.30) * 0.90
        arr = util.add_glow(arr, w * 0.5, h * 0.50, 700.0 * s, C.TEAL, 0.14)
        arr = ty.main_title(arr, p, y=0.50, scale=s)
        arr = ty.caption(arr, "A CINEMATIC TEASER", y=0.798, size=21.0 * s,
                         tracking=12.0 * s, color=C.FOAM,
                         opacity=0.62 * util.smoothstep(0.35, 0.90, p))
        return arr

    def _end(self, t):
        w, h, s = self.w, self.h, self.s
        p = util.clamp((t - 21.2) / 1.8)
        base = self.backdrop.render(t, glow=0.30, fog=0.55, rain=0.10, swell=0.60,
                                    moon_x=0.5) * 0.34
        arr = util.add_glow(base, w * 0.5, h * 0.44, 640.0 * s, C.TEAL, 0.10)
        arr = ty.end_card(arr, p, tagline=C.TAGLINE, cta="COMING SOON", scale=s)
        arr = ty.caption(arr, "PC  \u00b7  CONSOLE", y=0.712, size=19.0 * s, tracking=10.0 * s,
                         color=C.FOAM, opacity=0.62 * util.smoothstep(0.70, 1.00, p))
        return arr

    # ------------------------------------------------------------ dynamics
    def _camera(self, t):
        zoom, cx, cy = 1.0, 0.5, 0.5
        if t < 2.8:
            p = t / 2.8
            zoom = 1.03 - 0.03 * util.ease_in_out(p)
            cx = 0.50 + 0.012 * p
            cy = 0.520
        elif t < 5.4:
            p = (t - 2.8) / 2.6
            zoom = 1.10 - 0.08 * util.ease_in_out(p)
            cx = 0.50 + 0.020 * math.sin(p * math.pi)
            cy = 0.500
        elif t < 7.2:
            p = (t - 5.4) / 1.8
            zoom = 1.06 - 0.03 * util.ease_out_cubic(p)
            cy = 0.520
        elif t < 11.4:
            p = ((t - 7.2) % 1.4) / 1.4
            zoom = 1.12 - 0.06 * util.ease_out_cubic(p)
            cx = 0.515
        elif t < 16.2:
            p = ((t - 11.4) % 1.6) / 1.6
            zoom = 1.10 - 0.05 * util.ease_out_cubic(p)
            cy = 0.500
        elif t < 18.6:
            zoom = 1.03 + 0.02 * ((t - 16.2) / 2.4)
        elif t < 21.2:
            p = (t - 18.6) / 2.6
            zoom = 1.08 - 0.05 * util.ease_in_out(p)
        else:
            p = (t - 21.2) / 1.8
            zoom = 1.00 + 0.02 * p

        amp = 0.0
        for c in CUTS:
            if t >= c:
                amp += math.exp(-(t - c) / 0.10)
        amp = min(amp * 0.0035, 0.012)
        sx = math.sin(t * 61.0) * amp
        sy = math.cos(t * 47.0) * amp
        return zoom, cx, cy, sx, sy

    def _flash(self, t):
        a = 0.0
        for c in CUTS:
            if t >= c:
                a += 0.75 * math.exp(-(t - c) / 0.075)
        return min(0.85, a)

    def _glitch(self, t):
        g = 0.0
        for c in CUTS:
            d = t - c
            if 0.0 <= d < 0.14:
                g = max(g, 1.0 - d / 0.14)
        return g * 0.9

    # ------------------------------------------------------------- output
    def frame(self, t):
        t = float(t)
        if t < 2.8:
            arr = self._open(t)
        elif t < 5.4:
            arr = self._island(t)
        elif t < 7.2:
            arr = self._sahur(t)
        elif t < 11.4:
            arr = self._weapon(t, int((t - 7.2) // 1.4), ((t - 7.2) % 1.4) / 1.4)
        elif t < 16.2:
            arr = self._boss(t, int((t - 11.4) // 1.6), ((t - 11.4) % 1.6) / 1.6)
        elif t < 18.6:
            arr = self._hype(t)
        elif t < 21.2:
            arr = self._title(t)
        else:
            arr = self._end(t)

        s = self.s
        arr = self.dust.draw(arr, t, 1.0)
        arr = fx.soft_bloom(arr, radius=18.0 * s, strength=0.55)
        zoom, cx, cy, sx, sy = self._camera(t)
        arr = fx.camera(arr, zoom, cx, cy, sx, sy, chroma=int(round(2 * s)))
        arr = fx.glitch(arr, self._glitch(t), seed=int(t * 100) + 7)
        arr = fx.flash(arr, self._flash(t))
        arr = fx.finish(arr, self.grain, self.vig, seed=int(round(t * self.fps)),
                        exposure=1.16, lift=4.0, contrast=1.06)
        fx.letterbox(arr, self.bar)

        return Image.fromarray(np.clip(arr, 0.0, 255.0).astype(np.uint8), "RGB")

    def iter_frames(self, start=0.0, end=C.DURATION):
        n0 = max(0, int(round(float(start) * self.fps)))
        n1 = int(round(float(end) * self.fps))
        for i in range(n0, n1):
            yield self.frame(i / float(self.fps))

    def render_video(self, out_path, audio_path=None, start=0.0, end=C.DURATION,
                     crf=18, preset="medium"):
        total = int(round((float(end) - float(start)) * self.fps))
        return encode_video(self.iter_frames(start, end), Path(out_path), self.w, self.h,
                            self.fps, audio_path=audio_path, crf=crf, preset=preset,
                            total_frames=total)

    def save_stills(self, times, outdir):
        outdir = Path(outdir)
        outdir.mkdir(parents=True, exist_ok=True)
        paths = []
        for t in times:
            t = float(t)
            path = outdir / ("still_%05.2f.png" % t)
            self.frame(t).save(path)
            paths.append(path)
        return paths
