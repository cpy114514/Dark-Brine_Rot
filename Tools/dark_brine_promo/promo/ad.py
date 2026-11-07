"""Beat-locked ad engine for Dark Brine_Rot.

A brainstick ad is not a mood piece, it is a metronome. Everything here is
addressed in *beats* rather than seconds, so the picture and the chant
cannot drift apart: change the BPM and both sides move together.

The recipe (which shot lands on which beat, where the chant syllables sit)
lives in one place and is consumed by both this module and
:mod:`promo.adscore`, which guarantees frame-exact audio/visual sync.
"""
from __future__ import annotations

import math
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from . import entities
from . import util
from .background import OceanBackdrop

# Glow colour per figure: the two "hot" bosses burn, everything else is cold.
FIGURE_GLOW = {
    "sahur": C.TEAL,
    "club": C.TEAL,
    "blades": C.TEAL,
    "rifle": C.TEAL,
    "cappuccino": C.EMBER,
    "nailong": C.TEAL,
    "crocodilo": C.EMBER,
}


@dataclass
class Shot:
    """One block of beats on the timeline."""
    kind: str
    beats: int
    payload: dict = field(default_factory=dict)


@dataclass
class Ad:
    """A complete ad: grid, shots, chant placement and look."""
    key: str
    title: str
    width: int
    height: int
    fps: int
    bpm: float
    bars: int
    shots: list = field(default_factory=list)
    chant: list = field(default_factory=list)   # (bar, beat, word, vowel)
    hits: list = field(default_factory=list)    # (bar, beat, kind)
    root: float = 55.0
    flash_color: tuple = (176, 232, 232)
    exposure: float = 1.14
    lift: float = 3.0
    contrast: float = 1.08
    horizon: float = 0.545
    letterbox: float = 0.0
    bloom: float = 0.55
    grain: float = 0.055
    seed: int = 7
    chant_y: float = 0.50
    chant_size: float = 168.0

    # ------------------------------------------------------------- grid
    @property
    def spb(self) -> float:
        """Seconds per beat."""
        return 60.0 / float(self.bpm)

    @property
    def duration(self) -> float:
        return self.bars * 4 * self.spb

    @property
    def frames(self) -> int:
        return int(round(self.duration * self.fps))

    def t_of(self, bar: float, beat: float = 0.0) -> float:
        return (float(bar) * 4.0 + float(beat)) * self.spb

    def pos(self, t: float):
        """-> (bar, beat, phase) with phase in [0, 1)."""
        beats = float(t) / self.spb
        bar = int(beats // 4)
        beat = int(beats % 4)
        phase = beats - math.floor(beats)
        return bar, beat, phase

    def shot_at(self, t: float):
        """-> (shot, index, beats_into_shot, phase)."""
        beats = float(t) / self.spb
        acc = 0
        for i, sh in enumerate(self.shots):
            if beats < acc + sh.beats:
                return sh, i, beats - acc, (beats - acc) % 1.0
            acc += sh.beats
        sh = self.shots[-1]
        return sh, len(self.shots) - 1, 0.0, 0.0

    def shot_start_beat(self, index: int) -> int:
        return sum(s.beats for s in self.shots[:index])


# --------------------------------------------------------------------- utils
def _over(arr, im: Image.Image, x: int, y: int, opacity: float = 1.0):
    """Alpha-composite an RGBA image onto a float RGB array at (x, y)."""
    h, w = arr.shape[:2]
    iw, ih = im.size
    x0, y0 = int(round(x)), int(round(y))
    sx0, sy0 = max(0, -x0), max(0, -y0)
    dx0, dy0 = max(0, x0), max(0, y0)
    cw = min(iw - sx0, w - dx0)
    ch = min(ih - sy0, h - dy0)
    if cw <= 0 or ch <= 0:
        return arr
    src = im.crop((sx0, sy0, sx0 + cw, sy0 + ch))
    a = np.asarray(src, dtype=np.float32)
    al = (a[..., 3:4] * float(opacity)) / 255.0
    dst = arr[dy0:dy0 + ch, dx0:dx0 + cw]
    arr[dy0:dy0 + ch, dx0:dx0 + cw] = dst * (1.0 - al) + a[..., :3] * al
    return arr


def _rgb_split(arr, px: int):
    """Cheap chromatic aberration - two channel rolls instead of a resample."""
    if px <= 0:
        return arr
    out = arr.copy()
    out[..., 0] = np.roll(arr[..., 0], -int(px), axis=1)
    out[..., 2] = np.roll(arr[..., 2], int(px), axis=1)
    return out


class AdRenderer:
    """Draws one ad frame by frame from a beat grid."""

    def __init__(self, ad: Ad):
        self.ad = ad
        self.w = int(ad.width)
        self.h = int(ad.height)
        self.s = self.h / 1080.0
        self.backdrop = OceanBackdrop(self.w, self.h, seed=ad.seed, horizon=ad.horizon)
        self.grain_bank = util.grain_bank(16, self.w, self.h, seed=ad.seed + 3)
        self.vig = util.vignette_mask(self.w, self.h, 0.58, 1.85)
        self._sprites = {}
        self._texts = {}
        self._scales = {}

    # ------------------------------------------------------------ sprites
    def _sprite(self, key: str, size: int):
        size = max(96, int(round(float(size) / 32.0)) * 32)
        ck = (key, size)
        sp = self._sprites.get(ck)
        if sp is None:
            glow = FIGURE_GLOW.get(key, C.TEAL)
            if key.startswith("sahur_"):
                # a weapon only reads as a weapon in someone's hands
                sp = entities.sahur(key[6:], size, glow)
            elif key == "sahur":
                sp = entities.sahur("club", size, glow)
            elif key in entities.WEAPONS:
                sp = entities.WEAPONS[key](size, glow)
            elif key in entities.BOSSES:
                sp = entities.BOSSES[key](size, glow)
            else:
                sp = entities.sahur("club", size, glow)
            self._sprites[ck] = sp
        return sp

    def _foot_offset(self, key: str, size: int):
        """How far below the sprite centre its lowest ink sits.

        Measuring the sprite beats guessing: a tall humanoid and a wide
        crocodile have their feet in very different places, and one fixed
        fraction leaves the other floating.
        """
        ck = ("__foot", key, int(size))
        off = self._sprites.get(ck)
        if off is None:
            sp = self._sprite(key, size)
            bbox = sp.getbbox()
            off = (bbox[3] - sp.height / 2.0) if bbox else (sp.height * 0.46)
            self._sprites[ck] = off
        return off

    def _sprite_scaled(self, key: str, size: int, scale: float):
        """A sprite resized by the punch factor, cached on a coarse grid."""
        q = round(float(scale) * 20.0) / 20.0
        ck = (key, int(size), q)
        out = self._scales.get(ck)
        if out is None:
            sp = self._sprite(key, size)
            if abs(q - 1.0) < 0.01:
                out = sp
            else:
                out = sp.resize((max(8, int(sp.width * q)), max(8, int(sp.height * q))),
                                Image.BILINEAR)
            if len(self._scales) > 160:
                self._scales.pop(next(iter(self._scales)))
            self._scales[ck] = out
        return out

    def _shadow(self, size: int):
        ck = ("__shadow", int(size))
        out = self._sprites.get(ck)
        if out is None:
            out = entities.ground_shadow(int(size))
            self._sprites[ck] = out
        return out

    # -------------------------------------------------------------- text
    def _text(self, text: str, size: int, kind: str = "display",
              color=C.BONE, tracking: float = 12.0, glow: float = 1.0):
        ck = (text, int(size), kind, tuple(color), float(tracking), round(float(glow), 2))
        lay = self._texts.get(ck)
        if lay is None:
            probe = ImageDraw.Draw(Image.new("RGBA", (8, 8)))
            # never let a line run off the frame - shrink it to fit instead
            fnt = util.fit_font(probe, text, kind, self.w * 0.90, int(size),
                                tracking, min_size=max(10, int(size * 0.30)))
            size = fnt.size
            wdt = int(util.text_width(probe, text, fnt, tracking)) + int(size) * 2
            hgt = int(size * 2.1)
            lay = Image.new("RGBA", (max(16, wdt), hgt), (0, 0, 0, 0))
            d = ImageDraw.Draw(lay)
            util.draw_tracked(d, (lay.width / 2, lay.height / 2), text, fnt,
                             tuple(color) + (255,), tracking, anchor="mm")
            if glow > 0.0:
                # a tight halo only - a wide one fuses adjacent letters
                g = lay.filter(ImageFilter.GaussianBlur(max(3.0, size * 0.042)))
                ga = np.asarray(g, dtype=np.float32)
                ga[..., :3] = np.minimum(255.0, ga[..., :3] * 1.18 + 16.0)
                ga[..., 3] = (ga[..., 3] * 0.85).astype(np.float32)
                lay = Image.alpha_composite(Image.fromarray(ga.astype(np.uint8), "RGBA"), lay)
            if len(self._texts) > 90:
                self._texts.pop(next(iter(self._texts)))
            self._texts[ck] = lay
        return lay

    def _paste_text(self, arr, text, size, cx, cy, scale=1.0, opacity=1.0,
                    kind="display", color=C.BONE, tracking=12.0, glow=1.0):
        lay = self._text(text, size, kind, color, tracking, glow)
        if abs(scale - 1.0) > 0.005:
            lay = lay.resize((max(8, int(lay.width * scale)), max(8, int(lay.height * scale))),
                             Image.BILINEAR)
        if opacity < 0.999:
            a = lay.getchannel("A").point(lambda v: int(v * max(0.0, min(1.0, opacity))))
            lay = lay.copy()
            lay.putalpha(a)
        return _over(arr, lay, cx - lay.width / 2, cy - lay.height / 2, 1.0)

    # ---------------------------------------------------------- backdrop
    def _bg(self, t, p):
        """Ocean backdrop tuned per shot.

        The backdrop hands back a pooled buffer that it reuses every other
        call, so it has to be copied before anything composites onto it
        in place.
        """
        out = self.backdrop.render(
            t,
            glow=p.get("glow", 0.45),
            fog=p.get("fog", 0.85),
            rain=p.get("rain", 0.20),
            swell=p.get("swell", 1.0),
            rise=p.get("rise", 0.0),
            flash=p.get("flash", 0.0),
            moon_x=p.get("moon_x", 0.68),
            moon_y=p.get("moon_y", 0.28),
            tint=p.get("tint", 0.35),
        )
        return np.array(out, dtype=np.float32, copy=True)

    def _shore(self, arr, top_y=None):
        """Dark foreground bank so the figures are not floating on the water.

        ``top_y`` is a fraction of the frame height, like every other
        vertical position in this engine.
        """
        h, w = arr.shape[:2]
        top = h * 0.66 if top_y is None else h * float(top_y)
        xs = np.arange(w, dtype=np.float32)
        edge = (top - 22.0 * np.sin(xs / 260.0 + 0.7)
                - 11.0 * np.sin(xs / 91.0 + 2.1) - 6.0 * np.sin(xs / 37.0))
        yy = np.arange(h, dtype=np.float32)[:, None]
        mask = (yy >= edge[None, :]).astype(np.float32)
        soft = Image.fromarray((mask * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(2.2))
        mask = np.asarray(soft, dtype=np.float32) / 255.0
        base = np.asarray((7, 13, 15), np.float32)[None, None, :]
        depth = np.clip((yy - edge[None, :]) / max(1.0, h * 0.30), 0.0, 1.0)[..., None]
        arr = arr * (1.0 - mask[..., None]) + (base * (1.0 - 0.45 * depth)) * mask[..., None]
        rim = np.clip(mask - np.roll(mask, 3, axis=0), 0.0, 1.0)
        arr += rim[..., None] * np.asarray((20, 48, 50), np.float32)
        mist = np.exp(-((yy - edge[None, :]) / (h * 0.020)) ** 2) * (yy < edge[None, :])
        arr += mist[..., None] * np.asarray(C.FOAM, np.float32) * 0.075
        return arr

    # -------------------------------------------------------------- shots
    def _draw_figure(self, arr, key, cx, cy, size_frac, punch, opacity=1.0, shadow=True):
        size = size_frac * self.h
        sp = self._sprite_scaled(key, size, punch)
        if shadow:
            sh = self._shadow(int(size * 0.9))
            arr = entities.blit(arr, sh, self.w * cx, self.h * cy + size * 0.44,
                                opacity=0.55 * opacity)
        return entities.blit(arr, sp, self.w * cx, self.h * cy, opacity=opacity)

    def _shot_black(self, arr, shot, ctx):
        """Near-black with a breathing glow; the chant lands on the beat."""
        t, phase, beat = ctx["t"], ctx["phase"], ctx["beat"]
        p = 0.5 + 0.5 * math.cos(2 * math.pi * phase)
        arr = arr * 0.06
        arr = util.add_glow(arr, self.w * 0.5, self.h * 0.5,
                            self.h * 0.42, C.TEAL, 0.10 + 0.14 * p)
        return arr

    def _shot_words(self, arr, shot, ctx):
        """One word per beat, slamming in and holding."""
        words = shot.payload.get("words") or ["DARK"]
        b = ctx["beat"]
        text = words[b % len(words)]
        p = ctx["phase"]
        # punch: big on the beat, settles within a third of a beat
        k = min(1.0, p / 0.30)
        scale = 1.0 + 0.48 * (1.0 - k) ** 2
        op = 1.0 if p < 0.86 else max(0.0, 1.0 - (p - 0.86) / 0.14)
        size = shot.payload.get("size", 150.0) * self.s
        cy = self.h * shot.payload.get("y", 0.44)
        color = shot.payload.get("color", C.BONE)
        tracking = shot.payload.get("tracking", 16.0) * self.s
        arr = self._paste_text(arr, text, size, self.w * 0.5, cy, scale, op,
                               kind=shot.payload.get("kind", "display"),
                               color=color, tracking=tracking)
        return arr

    def _place_figure(self, shot, size_frac, key="sahur"):
        """Sprite size plus the y that lands its feet on the shore line.

        Figures that float over the water read as cut-outs; deriving the
        centre from the shore keeps them planted whatever their size.
        """
        size = size_frac * self.h
        cy = shot.payload.get("cy")
        if cy is None:
            shore = shot.payload.get("shore_y", 0.66)
            off = self._foot_offset(key, size)
            cy = (self.h * shore - off) / self.h
        return size, cy

    def _shot_figure(self, arr, shot, ctx):
        """A vector figure, one cut per beat, punching in on every hit."""
        keys = shot.payload.get("keys") or ["sahur"]
        b = ctx["beat"]
        key = keys[b % len(keys)]
        p = ctx["phase"]
        k = min(1.0, p / 0.26)
        punch = 1.0 + 0.16 * (1.0 - k) ** 2
        size_frac = shot.payload.get("size", 0.52)
        _size, cy = self._place_figure(shot, size_frac, key)
        cx = shot.payload.get("cx", 0.5)
        arr = self._draw_figure(arr, key, cx, cy, size_frac, punch)
        label = shot.payload.get("labels")
        if label:
            txt = label[b % len(label)]
            arr = self._paste_text(arr, txt, 54.0 * self.s, self.w * 0.5,
                                   self.h * shot.payload.get("label_y", 0.80),
                                   1.0, 0.95, kind="condensed", color=C.FOAM,
                                   tracking=14.0 * self.s)
        return arr

    def _shot_boss(self, arr, shot, ctx):
        """Boss card: the creature plus a slamming index number."""
        keys = shot.payload.get("keys") or ["cappuccino"]
        b = ctx["beat"]
        key = keys[b % len(keys)]
        p = ctx["phase"]
        k = min(1.0, p / 0.30)
        punch = 1.0 + 0.14 * (1.0 - k) ** 2
        size_frac = shot.payload.get("size", 0.50)
        _size, cy = self._place_figure(shot, size_frac, key)
        arr = self._draw_figure(arr, key, shot.payload.get("cx", 0.5), cy,
                                size_frac, punch)
        nums = shot.payload.get("numbers", ["01"])
        num = nums[b % len(nums)]
        # the number is sized off the frame width, not the height, so it
        # cannot run off the side of a vertical frame
        num_size = min(shot.payload.get("num_size", 210.0) * self.s, self.w * 0.19)
        arr = self._paste_text(arr, num, num_size,
                               self.w * shot.payload.get("num_x", 0.18),
                               self.h * shot.payload.get("num_y", 0.26),
                               1.0 + 0.35 * (1.0 - k) ** 2, 0.85,
                               kind="display", color=C.EMBER, tracking=8.0 * self.s)
        return arr

    def _shot_title(self, arr, shot, ctx):
        """The title lockup; it grows a couple of percent on every beat."""
        p = ctx["phase"]
        k = min(1.0, p / 0.25)
        punch = 1.0 + 0.05 * (1.0 - k) ** 2
        big = shot.payload.get("size", 150.0) * self.s
        cy = shot.payload.get("cy", 0.42)
        arr = self._paste_text(arr, "DARK", 52.0 * self.s, self.w * 0.5,
                               self.h * (cy - 0.13), punch, 1.0,
                               kind="condensed", color=C.TEAL, tracking=34.0 * self.s)
        arr = self._paste_text(arr, "BRINE_ROT", big, self.w * 0.5, self.h * cy,
                               punch, 1.0, kind="display", color=C.BONE,
                               tracking=10.0 * self.s)
        return arr

    def _shot_tagline(self, arr, shot, ctx):
        text = shot.payload.get("text", "SURVIVE THE ISLAND.")
        p = ctx["phase"]
        k = min(1.0, p / 0.30)
        scale = 1.0 + 0.30 * (1.0 - k) ** 2
        arr = self._paste_text(arr, text, shot.payload.get("size", 78.0) * self.s,
                               self.w * 0.5, self.h * shot.payload.get("y", 0.62),
                               scale, 1.0, kind="condensed", color=C.FOAM,
                               tracking=16.0 * self.s)
        return arr

    def _shot_shards(self, arr, shot, ctx):
        """Teal light streaks on the beat - the repeated visual motif."""
        p = ctx["phase"]
        life = max(0.0, 1.0 - p / 0.45)
        if life <= 0.0:
            return arr
        rng = np.random.default_rng(int(ctx["shot_index"] * 977 + ctx["beat"]))
        n = int(shot.payload.get("count", 4))
        for i in range(n):
            x = rng.uniform(0.08, 0.92) * self.w
            y = rng.uniform(0.12, 0.88) * self.h
            ln = rng.uniform(0.18, 0.55) * self.h
            ang = rng.uniform(-0.5, 0.5)
            dx, dy = math.sin(ang) * ln, math.cos(ang) * ln
            a = life * rng.uniform(0.25, 0.75)
            layer = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
            d = ImageDraw.Draw(layer)
            d.line([(x, y), (x + dx, y + dy)], fill=tuple(C.FOAM) + (int(150 * a),),
                   width=int(rng.uniform(2, 7)))
            arr = _over(arr, layer, 0, 0, 1.0)
        return arr

    # -------------------------------------------------------------- frame
    def _bloom(self, arr):
        """Cheap bloom: threshold, downsample, blur small, upsample.

        A full-resolution Gaussian blur costs more than the rest of the
        frame put together; at quarter scale it is visually identical.
        """
        strength = float(self.ad.bloom)
        if strength <= 0.0:
            return arr
        a = np.clip(arr, 0.0, 255.0)
        lum = a.max(axis=2)
        mask = np.clip((lum - 150.0) / 105.0, 0.0, 1.0)[..., None]
        if float(mask.max()) <= 0.0:
            return arr
        bright = (a * mask).astype(np.uint8)
        small = Image.fromarray(bright, "RGB").resize((max(4, self.w // 4),
                                                      max(4, self.h // 4)),
                                                     Image.BILINEAR)
        small = small.filter(ImageFilter.GaussianBlur(5))
        blur = np.asarray(small.resize((self.w, self.h), Image.BILINEAR),
                           dtype=np.float32)
        return arr + blur * strength

    def frame(self, t: float):
        ad = self.ad
        shot, idx, into, phase = ad.shot_at(t)
        bar, beat, gphase = ad.pos(t)
        ctx = {"t": t, "phase": phase, "beat": beat, "bar": bar,
               "shot_index": idx, "into": into}

        p = dict(shot.payload.get("bg", {}))
        arr = self._bg(t, p)

        # background darkness per shot kind
        if shot.kind == "black":
            arr = self._shot_black(arr, shot, ctx)
        elif shot.kind == "words":
            arr = self._shot_words(arr, shot, ctx)
        elif shot.kind == "figure":
            arr = self._draw_figure_ground(arr, shot, ctx)
        elif shot.kind == "boss":
            arr = self._draw_figure_ground(arr, shot, ctx)
        elif shot.kind == "title":
            arr = self._shot_title(arr, shot, ctx)
        elif shot.kind == "tagline":
            arr = self._shot_tagline(arr, shot, ctx)
        elif shot.kind == "shards":
            arr = self._shot_shards(arr, shot, ctx)

        # the chant words, always in the same place so sound and word fuse
        arr = self._draw_chant(arr, t, shot)

        # beat decoration: white flash, RGB split
        fl = 0.55 * max(0.0, 1.0 - phase / 0.18) ** 2
        if shot.payload.get("flash", True):
            arr = arr + np.asarray(ad.flash_color, np.float32) * fl
        arr = _rgb_split(arr, int(round(3.0 * max(0.0, 1.0 - phase / 0.30))))

        # grade
        arr = self._bloom(arr)
        arr = np.clip(arr * ad.exposure + ad.lift, 0.0, 255.0)
        arr = 255.0 * np.clip((arr / 255.0 - 0.5) * ad.contrast + 0.5, 0.0, 1.0)
        arr *= self.vig
        if ad.grain > 0.0:
            g = self.grain_bank[int(t * self.ad.fps) % len(self.grain_bank)]
            arr += (g[..., None] - 127.5) * ad.grain

        if ad.letterbox > 0.0:
            bar_px = int(self.h * ad.letterbox)
            arr[:bar_px] = 0.0
            arr[-bar_px:] = 0.0
        return arr

    def _draw_figure_ground(self, arr, shot, ctx):
        if shot.payload.get("shore", True):
            arr = self._shore(arr, shot.payload.get("shore_y"))
        if shot.kind == "figure":
            arr = self._shot_figure(arr, shot, ctx)
        else:
            arr = self._shot_boss(arr, shot, ctx)
        return arr

    def _draw_chant(self, arr, t, shot):
        """Draw the chant syllable that is sounding right now.

        A "words" shot already owns the centre of the frame, so the chant
        layer stands down there - otherwise the same word would be drawn
        twice, on top of itself.
        """
        if shot.kind == "words":
            return arr
        if shot.payload.get("hide_chant"):
            return arr
        ad = self.ad
        cy = self.h * shot.payload.get("chant_y", ad.chant_y)
        size = shot.payload.get("chant_size", ad.chant_size) * self.s
        for bar, beat, word, _vowel in ad.chant:
            at = ad.t_of(bar, beat)
            dt = t - at
            dur = ad.spb * 0.88
            if dt < -0.02 or dt > dur:
                continue
            # a syllable lasts most of a beat; the word holds then fades
            life = 1.0 if dt < dur * 0.70 else max(0.0, 1.0 - (dt - dur * 0.70) / (dur * 0.30))
            k = min(1.0, max(0.0, dt) / (ad.spb * 0.22))
            scale = 1.0 + 0.34 * (1.0 - k) ** 2
            arr = self._paste_text(arr, word, size, self.w * 0.5, cy, scale, life,
                                   kind="display", color=C.BONE, tracking=22.0 * self.s)
        return arr

    # -------------------------------------------------------------- export
    def iter_frames(self):
        """Yield RGB frames as PIL images, the way the encoder wants them."""
        ad = self.ad
        n = ad.frames
        for i in range(n):
            arr = self.frame(i / float(ad.fps))
            yield Image.fromarray(np.clip(arr, 0.0, 255.0).astype(np.uint8), "RGB")


# ==========================================================================
# The five ads.
# ==========================================================================
def _chant_words(start_bar: int, bars: int, bpm: float):
    """DARK / BRINE / ROT on beats 0-2 of every bar, one beat of rest."""
    out = []
    for b in range(start_bar, start_bar + bars):
        for k, word in enumerate(("DARK", "BRINE", "ROT")):
            out.append((b, k, word, "ah" if k != 1 else "eh"))
    return out


def recipe_chant_landscape() -> Ad:
    """The hook: DARK / BRINE / ROT, eight bars, 120 BPM, 16 seconds."""
    bpm = 120.0
    ad = Ad(
        key="ad1_chant",
        title="CHANT // 16:9",
        width=1920, height=1080, fps=24, bpm=bpm, bars=8,
        root=55.0, exposure=1.16, contrast=1.10, letterbox=0.055,
    )
    ad.shots = [
        # the hook arrives out of the dark, one word per beat
        Shot("black", 4, {"bg": {"glow": 0.05, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.50, "chant_size": 172.0}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", "DARK"],
                          "size": 165.0, "y": 0.42, "tracking": 22.0,
                          "bg": {"glow": 0.18, "fog": 0.95, "rain": 0.05}}),
        Shot("figure", 4, {"keys": ["sahur"], "size": 0.50,
                           "labels": ["THE TIDE SENT HER BACK"], "label_y": 0.17,
                           "chant_y": 0.80, "chant_size": 84.0,
                           "bg": {"glow": 0.30, "fog": 0.80, "rain": 0.45},
                           "shore_y": 0.66}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", "DARK"],
                          "size": 185.0, "y": 0.40, "tracking": 26.0,
                          "bg": {"glow": 0.22, "fog": 0.95, "rain": 0.10}}),
        Shot("figure", 4, {"keys": ["sahur"], "size": 0.58,
                           "labels": ["SURVIVE THE ISLAND."], "label_y": 0.17,
                           "chant_y": 0.81, "chant_size": 84.0,
                           "bg": {"glow": 0.34, "fog": 0.75, "rain": 0.55},
                           "shore_y": 0.66}),
        Shot("shards", 4, {"count": 6, "chant_y": 0.50, "chant_size": 172.0,
                           "bg": {"glow": 0.40, "fog": 0.60, "rain": 0.30}}),
        Shot("title", 4, {"size": 150.0, "cy": 0.44,
                          "hide_chant": True,
                          "bg": {"glow": 0.45, "fog": 0.70, "rain": 0.20}}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", ""],
                          "size": 165.0, "y": 0.42, "tracking": 22.0,
                          "bg": {"glow": 0.30, "fog": 0.85, "rain": 0.35}}),
    ]
    ad.chant = _chant_words(0, 8, bpm)
    ad.hits = ([(b, 0, "boom") for b in range(0, 8)]
               + [(b, 2, "stab") for b in range(0, 8)])
    return ad


def recipe_chant_vertical() -> Ad:
    """The same hook, re-composed for a phone instead of cropped."""
    bpm = 120.0
    ad = Ad(
        key="ad2_chant_v",
        title="CHANT // 9:16",
        width=1080, height=1920, fps=24, bpm=bpm, bars=8,
        root=55.0, exposure=1.18, contrast=1.10, horizon=0.46, seed=11,
    )
    ad.shots = [
        Shot("black", 4, {"bg": {"glow": 0.05, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.44, "chant_size": 96.0}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", "DARK"],
                          "size": 96.0, "y": 0.26, "tracking": 13.0,
                          "bg": {"glow": 0.18, "fog": 0.95, "rain": 0.05}}),
        Shot("figure", 4, {"keys": ["sahur"], "size": 0.40,
                           "labels": ["THE TIDE SENT HER BACK"], "label_y": 0.17,
                           "chant_y": 0.83, "chant_size": 52.0,
                           "bg": {"glow": 0.30, "fog": 0.80, "rain": 0.45},
                           "shore_y": 0.66}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", "DARK"],
                          "size": 104.0, "y": 0.25, "tracking": 14.0,
                          "bg": {"glow": 0.22, "fog": 0.95, "rain": 0.10}}),
        Shot("figure", 4, {"keys": ["sahur"], "size": 0.46,
                           "labels": ["SURVIVE THE ISLAND."], "label_y": 0.17,
                           "chant_y": 0.84, "chant_size": 52.0,
                           "bg": {"glow": 0.34, "fog": 0.75, "rain": 0.55},
                           "shore_y": 0.66}),
        Shot("shards", 4, {"count": 6, "chant_y": 0.44, "chant_size": 96.0,
                           "bg": {"glow": 0.40, "fog": 0.60, "rain": 0.30}}),
        Shot("title", 4, {"size": 62.0, "cy": 0.46,
                          "hide_chant": True,
                          "bg": {"glow": 0.45, "fog": 0.70, "rain": 0.20}}),
        Shot("words", 4, {"words": ["DARK", "BRINE", "ROT", ""],
                          "size": 96.0, "y": 0.26, "tracking": 13.0,
                          "bg": {"glow": 0.30, "fog": 0.85, "rain": 0.35}}),
    ]
    ad.chant = _chant_words(0, 8, bpm)
    ad.hits = ([(b, 0, "boom") for b in range(0, 8)]
               + [(b, 2, "stab") for b in range(0, 8)])
    return ad


def recipe_weapons() -> Ad:
    """One weapon per beat at 140 BPM - a showroom that slams."""
    bpm = 140.0
    ad = Ad(
        key="ad3_weapons",
        title="WEAPONS // 16:9",
        width=1920, height=1080, fps=24, bpm=bpm, bars=8,
        root=62.0, exposure=1.20, contrast=1.14, letterbox=0.055,
        flash_color=(232, 214, 186), seed=23,
    )
    ad.shots = [
        Shot("black", 2, {"bg": {"glow": 0.05, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.50, "chant_size": 172.0}),
        Shot("figure", 6, {"keys": ["sahur_club", "sahur_blades", "sahur_rifle",
                                  "sahur_club", "sahur_blades", "sahur_rifle"],
                           "size": 0.46,
                           "labels": ["CLUB", "DUAL BLADES", "M4A1",
                                      "CLUB", "DUAL BLADES", "M4A1"],
                           "label_y": 0.17, "chant_y": 0.80, "chant_size": 84.0,
                           "bg": {"glow": 0.34, "fog": 0.80, "rain": 0.40},
                           "shore_y": 0.66}),
        Shot("words", 4, {"words": ["MELEE", "FAST", "LOUD", "MELEE"],
                          "size": 150.0, "y": 0.44, "tracking": 20.0,
                          "bg": {"glow": 0.25, "fog": 0.90, "rain": 0.20}}),
        Shot("figure", 4, {"keys": ["cappuccino", "nailong", "crocodilo", "sahur"],
                           "size": 0.44,
                           "labels": ["THREE GUARD THE ISLAND."], "label_y": 0.17,
                           "chant_y": 0.80, "chant_size": 84.0,
                           "bg": {"glow": 0.38, "fog": 0.70, "rain": 0.50},
                           "shore_y": 0.66}),
        Shot("shards", 4, {"count": 7, "chant_y": 0.50, "chant_size": 172.0,
                           "bg": {"glow": 0.42, "fog": 0.55, "rain": 0.35}}),
        Shot("title", 4, {"size": 150.0, "cy": 0.44,
                          "hide_chant": True,
                          "bg": {"glow": 0.45, "fog": 0.70, "rain": 0.20}}),
        Shot("tagline", 4, {"text": "SURVIVE THE ISLAND.", "size": 82.0, "y": 0.66,
                            "chant_y": 0.15, "chant_size": 72.0,
                            "bg": {"glow": 0.35, "fog": 0.80, "rain": 0.30}}),
        Shot("black", 4, {"bg": {"glow": 0.05, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.50, "chant_size": 172.0}),
    ]
    ad.chant = [(b, 0, "DARK", "ah") for b in range(0, 8)]
    ad.hits = []
    for b in range(0, 8):
        for k in range(4):
            ad.hits.append((b, k, "metal" if b % 2 == 0 else "boom"))
    return ad


def recipe_bosses() -> Ad:
    """Three dossiers at 100 BPM - slow, heavy, numbered."""
    bpm = 100.0
    ad = Ad(
        key="ad4_bosses",
        title="BOSS FILES // 16:9",
        width=1920, height=1080, fps=24, bpm=bpm, bars=8,
        root=49.0, exposure=1.14, contrast=1.12, letterbox=0.055,
        seed=31,
    )
    ad.shots = [
        Shot("black", 2, {"bg": {"glow": 0.04, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.50, "chant_size": 172.0}),
        Shot("boss", 4, {"keys": ["cappuccino"], "numbers": ["01"],
                         "size": 0.44, "chant_y": 0.80, "chant_size": 84.0,
                         "bg": {"glow": 0.42, "fog": 0.72, "rain": 0.42, "tint": 0.55},
                         "shore_y": 0.66}),
        Shot("boss", 4, {"keys": ["nailong"], "numbers": ["02"],
                         "size": 0.50, "chant_y": 0.80, "chant_size": 84.0,
                         "bg": {"glow": 0.34, "fog": 0.78, "rain": 0.48},
                         "shore_y": 0.66}),
        Shot("boss", 4, {"keys": ["crocodilo"], "numbers": ["03"],
                         "size": 0.46, "chant_y": 0.80, "chant_size": 84.0,
                         "bg": {"glow": 0.40, "fog": 0.70, "rain": 0.50, "tint": 0.55},
                         "shore_y": 0.66}),
        Shot("words", 4, {"words": ["THREE", "GUARD", "THE", "ISLAND"],
                          "size": 150.0, "y": 0.44, "tracking": 20.0,
                          "bg": {"glow": 0.25, "fog": 0.92, "rain": 0.20}}),
        Shot("title", 4, {"size": 150.0, "cy": 0.44,
                          "hide_chant": True,
                          "bg": {"glow": 0.45, "fog": 0.70, "rain": 0.20}}),
        Shot("tagline", 4, {"text": "SURVIVE THE ISLAND.", "size": 82.0, "y": 0.66,
                            "chant_y": 0.15, "chant_size": 72.0,
                            "bg": {"glow": 0.35, "fog": 0.80, "rain": 0.30}}),
        Shot("black", 6, {"bg": {"glow": 0.04, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.50, "chant_size": 172.0}),
    ]
    ad.chant = [(b, 0, "DARK", "oh") for b in range(0, 8)]
    ad.hits = ([(b, 0, "boom") for b in range(0, 8)]
               + [(b, 2, "boom") for b in range(0, 8)])
    return ad


def recipe_story_vertical() -> Ad:
    """The vertical story: island, survivor, arsenal, bosses, title."""
    bpm = 128.0
    ad = Ad(
        key="ad5_story_v",
        title="STORY // 9:16",
        width=1080, height=1920, fps=24, bpm=bpm, bars=10,
        root=58.0, exposure=1.16, contrast=1.10, horizon=0.46, seed=43,
    )
    ad.shots = [
        Shot("black", 4, {"bg": {"glow": 0.04, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.44, "chant_size": 96.0}),
        Shot("words", 4, {"words": ["THE", "ISLAND", "REMEMBERS", "YOU"],
                          "size": 60.0, "y": 0.24, "tracking": 9.0,
                          "bg": {"glow": 0.18, "fog": 0.95, "rain": 0.08}}),
        Shot("figure", 4, {"keys": ["sahur"], "size": 0.40,
                           "labels": ["SAHUR"], "label_y": 0.17,
                           "chant_y": 0.83, "chant_size": 52.0,
                           "bg": {"glow": 0.30, "fog": 0.80, "rain": 0.45},
                           "shore_y": 0.66}),
        Shot("figure", 4, {"keys": ["sahur_club", "sahur_blades", "sahur_rifle",
                                   "sahur_club"],
                           "size": 0.38,
                           "labels": ["CLUB", "DUAL BLADES", "M4A1", "CLUB"],
                           "label_y": 0.17, "chant_y": 0.83, "chant_size": 52.0,
                           "bg": {"glow": 0.36, "fog": 0.75, "rain": 0.45},
                           "shore_y": 0.66}),
        Shot("boss", 4, {"keys": ["cappuccino", "nailong", "crocodilo", "sahur"],
                         "numbers": ["01", "02", "03", "?"],
                         "num_size": 58.0, "num_x": 0.16, "num_y": 0.15,
                         "size": 0.38, "cx": 0.60,
                         "chant_y": 0.83, "chant_size": 52.0,
                         "bg": {"glow": 0.40, "fog": 0.70, "rain": 0.50},
                         "shore_y": 0.66}),
        Shot("words", 4, {"words": ["SURVIVE", "THE", "ISLAND", "NOW"],
                          "size": 60.0, "y": 0.24, "tracking": 9.0,
                          "bg": {"glow": 0.22, "fog": 0.92, "rain": 0.15}}),
        Shot("shards", 4, {"count": 6, "chant_y": 0.44, "chant_size": 96.0,
                           "bg": {"glow": 0.42, "fog": 0.55, "rain": 0.35}}),
        Shot("title", 4, {"size": 62.0, "cy": 0.46,
                          "hide_chant": True,
                          "bg": {"glow": 0.45, "fog": 0.70, "rain": 0.20}}),
        Shot("tagline", 4, {"text": "SURVIVE THE ISLAND.", "size": 44.0, "y": 0.70,
                            "chant_y": 0.12, "chant_size": 42.0,
                            "bg": {"glow": 0.35, "fog": 0.80, "rain": 0.30}}),
        Shot("black", 4, {"bg": {"glow": 0.04, "fog": 0.2, "rain": 0.0},
                          "chant_y": 0.44, "chant_size": 96.0}),
    ]
    ad.chant = []
    for b in range(0, 10):
        for k, word in enumerate(("DARK", "BRINE", "ROT")):
            if (b + k) % 2 == 0:
                ad.chant.append((b, k, word, "ah"))
    ad.hits = [(b, 0, "boom") for b in range(0, 10)]
    return ad


RECIPES = [
    recipe_chant_landscape,
    recipe_chant_vertical,
    recipe_weapons,
    recipe_bosses,
    recipe_story_vertical,
]


def get_recipes():
    return [fn() for fn in RECIPES]
