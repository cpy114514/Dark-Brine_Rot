"""Procedural oceanic atmosphere for the Dark Brine_Rot teaser.

Every layer is generated from noise, gradients and vector silhouettes, so
the teaser needs no external art. ``OceanBackdrop.render`` returns a
float32 RGB array in 0..255; the caller keeps stacking effects on it and
quantises once at the end.

The module is split in two halves:

``_build``
    One-off setup -- tileable noise fields, the perspective wave table,
    the island and reef silhouettes and the rain sprites.  Nothing that
    is stored here ever moves.

``render``
    Per-frame work -- a handful of vector passes over the frame.  Every
    scratch buffer is allocated once in ``_build`` and reused, so a whole
    teaser never grows the process.

The look is a night storm on open water: cold teal, near black, high
contrast, nothing cheerful.  The frame is built back to front -- sky and
storm deck, moon, distant islands, perspective swell, drifting fog,
rain, lightning, and finally the near black reef that sits in front of
the camera.
"""
from __future__ import annotations

import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from . import config as C
from .util import add_glow, fbm, smoothstep, vertical_gradient


# ---------------------------------------------------------------------------
# generic procedural helpers
# ---------------------------------------------------------------------------

def ridge(width, seed, peaks=5, sharp=1.0):
    """A jagged 1D skyline in 0..1, used for island silhouettes."""
    rng = np.random.default_rng(int(seed))
    x = np.linspace(0.0, 1.0, int(width), dtype=np.float32)
    r = np.zeros_like(x)
    for _ in range(int(peaks)):
        c = float(rng.uniform(0.02, 0.98))
        s = float(rng.uniform(0.05, 0.18))
        a = float(rng.uniform(0.35, 1.0))
        r += a * np.exp(-((x - c) / s) ** 2)
    r /= max(1e-6, float(r.max()))
    return np.power(r, float(sharp)).astype(np.float32)


def silhouette(w, h, r, baseline, height, feather=2.0, window=None):
    """Filled alpha mask for a landmass sitting on ``baseline``."""
    w, h = int(w), int(h)
    ys = np.arange(h, dtype=np.float32)[:, None]
    top = float(baseline) - r[None, :w] * float(height)
    a = (ys >= top).astype(np.float32)
    if window is not None:
        a *= window[None, :w]
    img = Image.fromarray((a * 255.0).astype(np.uint8), "L")
    if feather > 0:
        img = img.filter(ImageFilter.GaussianBlur(float(feather)))
    return np.asarray(img, dtype=np.float32) / 255.0


def _periodic_noise(h, w, seed=0, cells_x=4, cells_y=4):
    """Seamless value noise in 0..1.

    The lattice wraps, so the field can be scrolled forever without ever
    showing the vertical seam that a plain ``fbm`` would produce.
    """
    h, w = int(h), int(w)
    gh = max(2, int(cells_y))
    gw = max(2, int(cells_x))
    grid = np.random.default_rng(int(seed)).random((gh, gw)).astype(np.float32)

    ys = (np.arange(h, dtype=np.float32) + 0.5) * (gh / max(1.0, float(h)))
    xs = (np.arange(w, dtype=np.float32) + 0.5) * (gw / max(1.0, float(w)))
    y0 = np.floor(ys).astype(np.int32) % gh
    x0 = np.floor(xs).astype(np.int32) % gw
    fy = (ys - np.floor(ys))[:, None]
    fx = (xs - np.floor(xs))[None, :]
    fy = fy * fy * (3.0 - 2.0 * fy)          # smoothstep weights -> C1 continuity
    fx = fx * fx * (3.0 - 2.0 * fx)
    y1 = (y0 + 1) % gh
    x1 = (x0 + 1) % gw
    top = grid[y0[:, None], x0[None, :]] * (1.0 - fx) + grid[y0[:, None], x1[None, :]] * fx
    bot = grid[y1[:, None], x0[None, :]] * (1.0 - fx) + grid[y1[:, None], x1[None, :]] * fx
    return top + (bot - top) * fy


def _periodic_fbm(h, w, seed=0, octaves=4, cells_x=4, cells_y=4, persistence=0.5):
    """Seamless fbm built on top of ``_periodic_noise``."""
    total = np.zeros((int(h), int(w)), dtype=np.float32)
    amp, norm = 1.0, 0.0
    cx, cy = int(cells_x), int(cells_y)
    for o in range(int(octaves)):
        total += amp * _periodic_noise(h, w, int(seed) + o * 17, cx, cy)
        norm += amp
        amp *= float(persistence)
        cx *= 2
        cy *= 2
    return total / max(1e-6, norm)


def _shift_x(src, dst, off):
    """Copy ``src`` into ``dst`` shifted right by ``off`` columns, wrapping."""
    w = src.shape[1]
    off %= w
    if off == 0:
        dst[:, :] = src
        return dst
    n = w - off
    dst[:, :n] = src[:, off:]
    dst[:, n:] = src[:, :off]
    return dst


def _shift_y(src, dst, off):
    """Copy ``src`` into ``dst`` shifted down by ``off`` rows, wrapping."""
    h = src.shape[0]
    off %= h
    if off == 0:
        dst[:, :] = src
        return dst
    n = h - off
    dst[:n] = src[off:]
    dst[n:] = src[:off]
    return dst


def _radial(radius, falloff):
    """Soft radial falloff in 0..1 on a (2r, 2r) canvas."""
    r = int(max(2, radius))
    y, x = np.mgrid[0:2 * r, 0:2 * r].astype(np.float32)
    c = r - 0.5
    d = np.sqrt((x - c) ** 2 + (y - c) ** 2) / max(1.0, c)
    return (np.clip(1.0 - d, 0.0, 1.0).astype(np.float32) ** float(falloff))


def _disc(radius, softness=0.30):
    """A nearly solid disc with a soft rim, on a (2r, 2r) canvas."""
    r = int(max(3, radius))
    y, x = np.mgrid[0:2 * r, 0:2 * r].astype(np.float32)
    c = r - 0.5
    d = np.sqrt((x - c) ** 2 + (y - c) ** 2) / max(1.0, c)
    return np.clip((1.0 - d) / max(0.05, float(softness)), 0.0, 1.0).astype(np.float32)


# ---------------------------------------------------------------------------
# palette used by the backdrop only
# ---------------------------------------------------------------------------

_HAZE = (48, 104, 104)        # colour the sky and the mist fade into
_ISLAND_FAR = (11, 26, 29)    # distant headland, still hazed out
_ISLAND_NEAR = (3, 8, 10)     # closer headland, almost a cut out
_REEF = (2, 5, 7)             # the near camera reef: a true silhouette
_FOAM_RGB = (166, 214, 206)   # crest tips and rain
_MOON_RGB = (208, 228, 222)   # the disc itself


class OceanBackdrop:
    """Night ocean backdrop: storm sky, moon, islands, swell, fog, rain.

    ``render`` is the only animated entry point; everything else is
    prepared once in ``__init__`` and reused for the whole teaser.
    """

    def __init__(self, w=C.WIDTH, h=C.HEIGHT, seed=3, horizon=0.545):
        self.w, self.h = int(w), int(h)
        self.seed = int(seed)
        self.horizon = float(horizon)
        self.hy = max(1, min(self.h - 1, int(self.h * self.horizon)))
        self.hs = self.h - self.hy
        self._build()

    # ------------------------------------------------------------------
    # static setup
    # ------------------------------------------------------------------
    def _build(self):
        w, h, hy, hs = self.w, self.h, self.hy, self.hs
        s = h / 1080.0
        hz = hy / float(h)                     # horizon in frame coordinates

        # ---- base gradients -------------------------------------------
        # The sky already carries the top of the haze band, so the
        # waterline never reads as a cut edge.
        sky = vertical_gradient(w, hy, [
            (0.00, (2, 4, 7)),
            (0.30, (4, 11, 16)),
            (0.60, (8, 27, 33)),
            (0.84, (17, 48, 54)),
            (1.00, (33, 74, 78)),
        ])
        v = np.linspace(0.0, 1.0, hy, dtype=np.float32)
        fade = smoothstep(0.86, 1.0, v)[:, None, None]
        sky = sky * (1.0 - fade) + np.asarray(_HAZE, np.float32) * fade
        self.sky_r = np.ascontiguousarray(sky[..., 0])
        self.sky_g = np.ascontiguousarray(sky[..., 1])
        self.sky_b = np.ascontiguousarray(sky[..., 2])

        sea = vertical_gradient(w, hs, [
            (0.000, (46, 100, 100)),
            (0.015, (34, 76, 78)),
            (0.050, (20, 50, 54)),
            (0.120, (11, 32, 37)),
            (0.300, (7, 21, 26)),
            (0.700, (5, 14, 18)),
            (1.000, (4, 10, 13)),
        ])
        self.sea_r = np.ascontiguousarray(sea[..., 0])
        self.sea_g = np.ascontiguousarray(sea[..., 1])
        self.sea_b = np.ascontiguousarray(sea[..., 2])

        # ---- storm deck -----------------------------------------------
        # Two stretched fbm layers: a heavy deck overhead and a lower
        # bank of wisps.  Both are seamless, so they can drift forever.
        self.cloud_hi = np.clip(
            (_periodic_fbm(hy, w, self.seed + 11, 4, 6, 5, 0.55) - 0.46) * 3.0, 0.0, 1.0)
        self.cloud_lo = np.clip(
            (_periodic_fbm(hy, w, self.seed + 23, 3, 11, 7, 0.50) - 0.48) * 3.2, 0.0, 1.0)
        cw1 = (0.28 + 0.72 * (1.0 - v) ** 0.8).astype(np.float32)
        cw2 = (0.80 * np.exp(-((v - 0.60) / 0.30) ** 2)).astype(np.float32)
        self.cloud_w1 = cw1
        self.cloud_w2 = cw2

        # ---- fog -------------------------------------------------------
        # Half resolution is plenty for soft volumes; it is upsampled
        # once per frame with a cheap 2x2 splat.
        h2, w2 = (h + 1) // 2, (w + 1) // 2
        self.h2, self.w2 = h2, w2
        vv = (np.arange(h2, dtype=np.float32) * 2.0 + 0.5) / float(h)
        self.fog_far = np.clip(
            (_periodic_fbm(h2, w2, self.seed + 57, 3, 4, 5, 0.60) - 0.30) * 1.9, 0.0, 1.0)
        self.fog_mid = np.clip(
            (_periodic_fbm(h2, w2, self.seed + 59, 3, 7, 6, 0.55) - 0.32) * 1.9, 0.0, 1.0)
        self.fog_low = np.clip(
            (_periodic_fbm(h2, w2, self.seed + 61, 2, 12, 9, 0.50) - 0.36) * 2.2, 0.0, 1.0)
        self.fog_p_far = (np.exp(-((vv - hz) / 0.048) ** 2) * 0.50).astype(np.float32)
        self.fog_p_mid = (np.exp(-((vv - 0.395) / 0.185) ** 2) * 0.26).astype(np.float32)
        self.fog_p_low = (np.exp(-((vv - (hz + 0.018)) / 0.022) ** 2) * 0.42).astype(np.float32)
        self.fog_p_line = (np.exp(-((vv - hz) / 0.007) ** 2) * 0.34).astype(np.float32)

        # ---- foam speckle ---------------------------------------------
        # Sparse, high contrast noise: crests are broken into flecks
        # instead of a clean ribbon of white.
        self.foam_tex = np.clip(
            (_periodic_noise(hs, w, self.seed + 71, 320, 150) - 0.74) * 5.0, 0.0, 1.0)

        # ---- wave table -----------------------------------------------
        # Each train is a sinusoid in world space.  The screen phase is
        #   k * (X * cos(t) + Z * sin(t)) / depth
        # with X the lateral world position and Z the depth, so the
        # apparent wavelength shrinks towards the horizon and the crest
        # lines curve with the perspective.  ``focal`` converts the
        # heading into screen pixels, which is what keeps the crests
        # roughly horizontal instead of radiating from a vanishing point.
        d0 = 10.0 * s
        focal = 1.20 * w
        rows = np.arange(1, hs + 1, dtype=np.float32)
        xcol = (np.arange(w, dtype=np.float32) - w * 0.5)
        inv = 1.0 / (rows + d0)
        self.xcol = xcol
        self.waves = []
        specs = (
            # wavelength at the bottom, heading, drift, height, mod gain, mod base
            (760.0, 4.0, 0.40, 0.80, 0.30, 0.68),
            (360.0, -7.0, 0.62, 0.55, 0.20, 0.62),
            (165.0, 11.0, 1.00, 0.40, 0.14, 0.58),
            (74.0, -15.0, 1.55, 0.26, 0.10, 0.54),
        )
        for lam, deg, omega, amp, gain, base in specs:
            th = math.radians(deg)
            ct, st = math.cos(th), math.sin(th)
            a = 2.0 * math.pi * (hs + d0) / (lam * max(0.25, abs(ct)))
            kx = (a * inv * ct)[:, None] * xcol[None, :]
            kz = a * inv * st * focal
            # a train only exists once its wavelength is a few pixels wide
            row_min = max(0.0, 3.2 * (hs + d0) / lam - d0)
            env = smoothstep(row_min, row_min * 1.8 + 8.0, rows)
            env = env * ((rows + d0) / (hs + d0)) ** 1.35 * float(amp)
            self.waves.append({
                "kx": kx.astype(np.float32),
                "kz": kz.astype(np.float32),
                "env": env.astype(np.float32),
                "omega": float(omega),
                "gain": float(gain),
                "base": float(base),
            })

        # foam only where the water is close enough to resolve it
        self.foam_depth = smoothstep(0.20 * hs, 0.55 * hs, rows).astype(np.float32)
        # moonlight path on the water
        self.refl_depth = np.exp(-((rows / (0.55 * hs)) ** 2)).astype(np.float32)

        # ---- islands ---------------------------------------------------
        far = ridge(w, self.seed + 21, peaks=7, sharp=1.6)
        far = far * (0.42 + 0.58 * _periodic_noise(1, w, self.seed + 22, 70, 2)[0])
        self.island_far = silhouette(w, hy, far, baseline=hy + 1.0,
                                     height=hy * 0.105, feather=1.5)
        near = ridge(w, self.seed + 37, peaks=3, sharp=1.15)
        near = near * (0.35 + 0.65 * _periodic_noise(1, w, self.seed + 38, 120, 2)[0])
        self.island_near = silhouette(w, hy, near, baseline=hy + 1.0,
                                      height=hy * 0.052, feather=1.0)
        self.island_rows = max(1, int(hy * 0.13))

        # ---- moon sprites ---------------------------------------------
        self.halo = _radial(int(hy * 0.34), 2.2)
        self.disc = _disc(int(hy * 0.045), 0.52)

        # ---- rain ------------------------------------------------------
        self.rain_near = self._make_rain(430, (26.0, 64.0), (7.0, 16.0),
                                         (85.0, 210.0), self.seed + 91)
        self.rain_far = self._make_rain(1050, (9.0, 26.0), (3.0, 8.0),
                                        (42.0, 120.0), self.seed + 97)

        # ---- foreground reef -------------------------------------------
        self.fg_mask, self.fg_rim = self._build_foreground()
        self.fg_row0 = self._first_row(self.fg_mask)
        self.fg_rim_row0 = self._first_row(self.fg_rim)

        # ---- reusable buffers -----------------------------------------
        self._r = np.empty((h, w), dtype=np.float32)
        self._g = np.empty((h, w), dtype=np.float32)
        self._b = np.empty((h, w), dtype=np.float32)
        self._cf = np.empty((hy, w), dtype=np.float32)
        self._c1 = np.empty((hy, w), dtype=np.float32)
        self._c2 = np.empty((hy, w), dtype=np.float32)
        self._tmp = np.empty((hy, w), dtype=np.float32)
        self._wacc = np.empty((hs, w), dtype=np.float32)
        self._wmod = np.empty((hs, w), dtype=np.float32)
        self._wph = np.empty((hs, w), dtype=np.float32)
        self._wtmp = np.empty((hs, w), dtype=np.float32)
        self._fogk = np.empty((h2, w2), dtype=np.float32)
        self._fg1 = np.empty((h2, w2), dtype=np.float32)
        self._fg2 = np.empty((h2, w2), dtype=np.float32)
        self._fogfull = np.empty((h, w), dtype=np.float32)
        self._rainf = np.empty((h, w), dtype=np.float32)
        self._rain2 = np.empty((h, w), dtype=np.float32)
        # scratch used by the full frame composites (no per frame allocs)
        self._s1 = np.empty((h, w), dtype=np.float32)
        self._s2 = np.empty((h, w), dtype=np.float32)
        # small pool of output frames, rotated so consecutive calls do not
        # hand back the very same buffer
        self._pool = [np.empty((h, w, 3), dtype=np.float32) for _ in range(2)]
        self._pool_i = 0
        # moon patch scratch
        mh = self.halo.shape[0]
        self._mp1 = np.empty((mh, mh), dtype=np.float32)
        self._mp2 = np.empty((mh, mh), dtype=np.float32)

    # ------------------------------------------------------------------
    # static art
    # ------------------------------------------------------------------
    def _make_rain(self, count, length, slant, value, seed):
        """A seamless sheet of slanted streaks (wraps in both axes)."""
        w, h = self.w, self.h
        img = Image.new("L", (w, h), 0)
        draw = ImageDraw.Draw(img)
        rng = np.random.default_rng(int(seed))
        for _ in range(int(count)):
            x0 = float(rng.uniform(0.0, w))
            y0 = float(rng.uniform(0.0, h))
            ln = float(rng.uniform(*length))
            sl = float(rng.uniform(*slant))
            val = int(rng.uniform(*value))
            for dx in (-w, 0.0, w):
                for dy in (-h, 0.0, h):
                    draw.line([(x0 + dx, y0 + dy), (x0 + dx + sl, y0 + dy + ln)],
                              fill=val, width=1)
        img = img.filter(ImageFilter.GaussianBlur(0.55))
        return np.asarray(img, dtype=np.float32) / 255.0

    def _build_foreground(self):
        """Near camera reef: jagged rocks, dead branches and a leaning post.

        Returns the silhouette mask plus a thin rim mask that catches a
        little moonlight, so the shape stays readable against the black
        water.
        """
        w, h = self.w, self.h
        img = Image.new("L", (w, h), 0)
        draw = ImageDraw.Draw(img)
        rng = np.random.default_rng(self.seed + 131)

        # --- low reef ridge with a few taller rocks
        n = 320
        x = np.linspace(0.0, 1.0, n, dtype=np.float32)
        bumps = np.zeros(n, dtype=np.float32)
        for _ in range(6):
            c = float(rng.uniform(-0.06, 1.06))
            sd = float(rng.uniform(0.03, 0.10))
            a = float(rng.uniform(0.30, 1.0))
            bumps += a * np.exp(-((x - c) / sd) ** 2)
        bumps /= max(1e-6, float(bumps.max()))
        rough = 0.28 + 0.72 * _periodic_noise(1, n, self.seed + 7, 80, 2)[0]
        heights = (0.020 + 0.110 * bumps) * rough
        py = h * (1.0 - np.interp(np.linspace(0.0, 1.0, w), x, heights))
        draw.polygon([(i, float(py[i])) for i in range(w)]
                     + [(w - 1, h + 20), (0, h + 20)], fill=255)

        # --- a handful of jagged rock stacks standing out of the reef
        for _ in range(5):
            cx = float(rng.uniform(0.03, 0.97)) * w
            rw = float(rng.uniform(0.020, 0.055)) * w
            rh = float(rng.uniform(0.045, 0.145)) * h
            base = h * 1.01
            pts = [(cx - rw, base)]
            # a few angular shoulders and a broken top
            for k in range(1, 6):
                f = k / 5.0
                px_ = cx - rw + 2.0 * rw * f
                py_ = base - rh * (1.0 - abs(f - float(rng.uniform(0.30, 0.70))) * 1.6)
                py_ = min(py_, base - rh * 0.25)
                pts.append((px_, py_))
            pts.append((cx + rw, base))
            draw.polygon(pts, fill=255)

        # --- bare dead branch leaning in from the left
        def branch(x0, y0, ang, length, wid, depth):
            if depth <= 0 or length < 10.0:
                return
            x1 = x0 + math.cos(ang) * length
            y1 = y0 + math.sin(ang) * length
            draw.line([(x0, y0), (x1, y1)], fill=255, width=max(1, int(round(wid))))
            for da in (-0.52, 0.30):
                branch(x1, y1, ang + da + float(rng.uniform(-0.18, 0.18)),
                       length * float(rng.uniform(0.55, 0.78)), wid * 0.62, depth - 1)

        branch(w * 0.05, h * 1.02, math.radians(-74.0), h * 0.21, 9.0, 4)
        branch(w * 0.19, h * 1.04, math.radians(-98.0), h * 0.13, 6.0, 3)

        # --- a leaning mooring post on the right
        px = w * 0.94
        draw.polygon([(px - 16.0, h + 20), (px + 16.0, h + 20),
                      (px - 4.0, h * 0.655), (px - 13.0, h * 0.672)], fill=255)

        img = img.filter(ImageFilter.GaussianBlur(1.7))
        mask = np.asarray(img, dtype=np.float32) / 255.0

        # the top edge of the reef, used for the wet rim light
        shifted = np.zeros_like(mask)
        shifted[1:] = mask[:-1]
        edge = np.clip(mask - shifted, 0.0, 1.0)
        rim = np.zeros_like(mask)
        rim[:-1] = edge[1:]
        return mask, rim

    @staticmethod
    def _first_row(field, eps=0.004):
        """First row of ``field`` that holds something (for cheap slicing)."""
        rows = np.nonzero(field.max(axis=1) > eps)[0]
        return int(rows[0]) if rows.size else int(field.shape[0])

    # ------------------------------------------------------------------
    # animated layers
    # ------------------------------------------------------------------
    def _moon(self, r, g, b, glow, moon_x, moon_y, cf):
        """Soft moon disc, veiled by whatever cloud sits in front of it."""
        w, hy = self.w, self.hy
        mx = float(moon_x) * w
        my = float(moon_y) * hy
        m1, m2 = self._mp1, self._mp2

        # broad halo
        rad = self.halo.shape[0] // 2
        x0 = int(round(mx - rad))
        y0 = int(round(my - rad))
        ax0, ay0 = max(0, x0), max(0, y0)
        ax1, ay1 = min(w, x0 + 2 * rad), min(hy, y0 + 2 * rad)
        if ax1 > ax0 and ay1 > ay0:
            rows, cols = ay1 - ay0, ax1 - ax0
            fall = self.halo[ay0 - y0:ay1 - y0, ax0 - x0:ax1 - x0]
            veil = m1[:rows, :cols]
            np.multiply(cf[ay0:ay1, ax0:ax1], -0.70, out=veil)
            veil += 1.0
            k = m2[:rows, :cols]
            np.multiply(fall, veil, out=k)
            k *= 0.26 * glow
            sl = (slice(ay0, ay1), slice(ax0, ax1))
            np.multiply(k, 52.0, out=veil)
            r[sl] += veil
            np.multiply(k, 146.0, out=veil)
            g[sl] += veil
            np.multiply(k, 148.0, out=veil)
            b[sl] += veil

        # the disc itself
        rad = self.disc.shape[0] // 2
        x0 = int(round(mx - rad))
        y0 = int(round(my - rad))
        ax0, ay0 = max(0, x0), max(0, y0)
        ax1, ay1 = min(w, x0 + 2 * rad), min(hy, y0 + 2 * rad)
        if ax1 > ax0 and ay1 > ay0:
            rows, cols = ay1 - ay0, ax1 - ax0
            disc = self.disc[ay0 - y0:ay1 - y0, ax0 - x0:ax1 - x0]
            veil = m1[:rows, :cols]
            np.multiply(cf[ay0:ay1, ax0:ax1], -0.88, out=veil)
            veil += 1.0
            k = m2[:rows, :cols]
            np.multiply(disc, veil, out=k)
            k *= 0.78 * glow
            sl = (slice(ay0, ay1), slice(ax0, ax1))
            np.multiply(k, _MOON_RGB[0], out=veil)
            r[sl] += veil
            np.multiply(k, _MOON_RGB[1], out=veil)
            g[sl] += veil
            np.multiply(k, _MOON_RGB[2], out=veil)
            b[sl] += veil

    def _islands(self, r, g, b):
        """Distant headlands sitting on the waterline."""
        hy = self.hy
        y0 = max(0, hy - self.island_rows)
        reg = slice(y0, hy)
        s1, s2 = self._s1, self._s2
        for mask, color in ((self.island_far[y0:], _ISLAND_FAR),
                            (self.island_near[y0:], _ISLAND_NEAR)):
            rows = mask.shape[0]
            inv = s1[:rows]
            np.multiply(mask, -1.0, out=inv)
            inv += 1.0
            r[reg] *= inv
            g[reg] *= inv
            b[reg] *= inv
            np.multiply(mask, color[0], out=s2[:rows])
            r[reg] += s2[:rows]
            np.multiply(mask, color[1], out=s2[:rows])
            g[reg] += s2[:rows]
            np.multiply(mask, color[2], out=s2[:rows])
            b[reg] += s2[:rows]

    def _water(self, r, g, b, t, swell, moon_x):
        """Perspective swell, foam on the crests and the moon path."""
        hy = self.hy
        acc = self._wacc
        mod = self._wmod
        ph = self._wph
        tmp = self._wtmp

        # --- wave trains, slowest first; every train is amplitude
        # modulated by the slower ones so crests break up instead of
        # running as continuous ribbons across the frame.
        wv = self.waves
        rowv = wv[0]["kz"] + wv[0]["omega"] * t
        np.add(wv[0]["kx"], rowv[:, None], out=ph)
        np.sin(ph, out=ph)
        np.multiply(ph, wv[0]["env"][:, None], out=ph)
        acc[:] = ph

        for i in range(1, len(wv)):
            np.multiply(acc, wv[i]["gain"], out=mod)
            mod += wv[i]["base"]
            rowv = wv[i]["kz"] + wv[i]["omega"] * t
            np.add(wv[i]["kx"], rowv[:, None], out=ph)
            np.sin(ph, out=ph)
            np.multiply(ph, wv[i]["env"][:, None], out=ph)
            np.multiply(ph, mod, out=ph)
            np.add(acc, ph, out=acc)

        # --- normalise the surface into a signed height
        np.multiply(acc, 0.62, out=acc)
        np.clip(acc, -0.45, 1.0, out=acc)

        # --- light the crests and sink the troughs
        np.multiply(acc, 13.0, out=tmp)
        r[hy:] += tmp
        np.multiply(acc, 42.0, out=tmp)
        g[hy:] += tmp
        np.multiply(acc, 40.0, out=tmp)
        b[hy:] += tmp

        # --- foam: only the very tips of the crests, broken into flecks
        foam = self._wph
        np.subtract(acc, 0.55, out=foam)
        np.multiply(foam, 3.2, out=foam)
        np.clip(foam, 0.0, 1.0, out=foam)
        np.multiply(foam, self.foam_tex, out=foam)
        np.multiply(foam, self.foam_depth[:, None], out=foam)

        # --- moonlight scattered back off the chop
        glint = self._wmod
        np.maximum(acc, 0.0, out=glint)
        np.multiply(glint, glint, out=glint)
        # the glitter path sits under the moon
        sigma = 0.055 * self.w
        off = (float(moon_x) - 0.5) * self.w
        refl = np.exp(-(((self.xcol - off) / sigma) ** 2)).astype(np.float32)
        np.multiply(glint, refl[None, :], out=glint)
        np.multiply(glint, self.refl_depth[:, None], out=glint)
        np.multiply(glint, 0.65, out=glint)
        foam += glint

        np.clip(foam, 0.0, 1.0, out=foam)
        np.multiply(foam, 1.15 * float(swell), out=foam)
        np.multiply(foam, _FOAM_RGB[0], out=tmp)
        r[hy:] += tmp
        np.multiply(foam, _FOAM_RGB[1], out=tmp)
        g[hy:] += tmp
        np.multiply(foam, _FOAM_RGB[2], out=tmp)
        b[hy:] += tmp

    def _fog(self, r, g, b, t, amount):
        """Three drifting depth layers of fog, flattened into one veil."""
        k = self._fogk
        _shift_x(self.fog_far, self._fg1, int(t * 3.5))
        np.multiply(self._fg1, self.fog_p_far[:, None], out=k)
        _shift_x(self.fog_mid, self._fg2, int(t * 8.0))
        np.multiply(self._fg2, self.fog_p_mid[:, None], out=self._fg1)
        k += self._fg1
        _shift_x(self.fog_low, self._fg2, int(t * 17.0))
        np.multiply(self._fg2, self.fog_p_low[:, None], out=self._fg1)
        k += self._fg1
        np.multiply(self._fg1, self.fog_p_line[:, None], out=self._fg2)
        k += self._fg2
        np.multiply(k, 1.15 * float(amount), out=k)
        np.clip(k, 0.0, 0.88, out=k)

        # cheap 2x upsample of the half resolution volume
        big = self._fogfull
        big[0::2, 0::2] = k
        big[1::2, 0::2] = k
        big[0::2, 1::2] = k
        big[1::2, 1::2] = k

        inv = self._s1
        np.multiply(big, -1.0, out=inv)
        inv += 1.0
        r *= inv
        g *= inv
        b *= inv
        s2 = self._s2
        np.multiply(big, _HAZE[0], out=s2)
        r += s2
        np.multiply(big, _HAZE[1], out=s2)
        g += s2
        np.multiply(big, _HAZE[2], out=s2)
        b += s2

    def _rain(self, r, g, b, t, amount):
        """Two depth layers of slanted streaks."""
        near = self._rainf
        far = self._rain2
        _shift_y(self.rain_near, near, int(t * 420.0))
        _shift_y(self.rain_far, far, int(t * 190.0))
        np.multiply(far, 0.45, out=far)
        near += far
        np.multiply(near, 0.34 * float(amount), out=near)

        s2 = self._s2
        np.multiply(near, _FOAM_RGB[0], out=s2)
        r += s2
        np.multiply(near, _FOAM_RGB[1], out=s2)
        g += s2
        np.multiply(near, _FOAM_RGB[2], out=s2)
        b += s2

    def _foreground(self, r, g, b, rise):
        """The near camera reef, pushed down and away by ``rise``."""
        mask = self.fg_mask
        h = self.h
        shift = max(0, int(round(float(rise) * h)))
        if shift >= h:
            return
        s1, s2 = self._s1, self._s2

        # only the rows that actually carry rock have to be touched; the
        # reef simply slides down, it never wraps around
        row0 = self.fg_row0
        if row0 + shift >= h:
            return
        reg = slice(row0 + shift, h)
        m = mask[row0:h - shift]
        rows = m.shape[0]
        inv = s1[:rows]
        np.multiply(m, -1.0, out=inv)
        inv += 1.0
        r[reg] *= inv
        g[reg] *= inv
        b[reg] *= inv
        np.multiply(m, _REEF[0], out=s2[:rows])
        r[reg] += s2[:rows]
        np.multiply(m, _REEF[1], out=s2[:rows])
        g[reg] += s2[:rows]
        np.multiply(m, _REEF[2], out=s2[:rows])
        b[reg] += s2[:rows]

        # faint wet rim light along the top edge of the rock
        rim0 = self.fg_rim_row0
        if rim0 + shift < h:
            rreg = slice(rim0 + shift, h)
            rim = self.fg_rim[rim0:h - shift]
            rows = rim.shape[0]
            np.multiply(rim, 13.0, out=s2[:rows])
            r[rreg] += s2[:rows]
            np.multiply(rim, 36.0, out=s2[:rows])
            g[rreg] += s2[:rows]
            np.multiply(rim, 38.0, out=s2[:rows])
            b[rreg] += s2[:rows]

    # ------------------------------------------------------------------
    # public API
    # ------------------------------------------------------------------
    def render(self, t, *, swell=1.0, fog=1.0, rain=0.0, flash=0.0,
               rise=0.0, glow=1.0, moon_x=0.72, moon_y=0.30, tint=0.0):
        """Render one frame of the backdrop.

        ``t``          seconds, drives every drift.
        ``swell``      wave height, 0 flattens the sea.
        ``fog``        density of the three drifting fog layers.
        ``rain``       density of the two rain layers.
        ``flash``      lightning wash, 0..1.
        ``rise``       pushes the foreground reef down and away, 0..1.
        ``glow``       brightness of the moon and its halo.
        ``moon_x/y``   moon position, normalised inside the sky.
        ``tint``       palette drift used for the different beats.
        """
        t = float(t)
        hy = self.hy
        r, g, b = self._r, self._g, self._b

        # --- base wash: sky above, water below
        r[:hy] = self.sky_r
        r[hy:] = self.sea_r
        g[:hy] = self.sky_g
        g[hy:] = self.sea_g
        b[:hy] = self.sky_b
        b[hy:] = self.sea_b

        # --- storm deck drifting across the sky
        cf = self._cf
        _shift_x(self.cloud_hi, self._c1, int(t * 4.0))
        _shift_x(self.cloud_lo, self._c2, int(t * 7.5))
        np.multiply(self._c1, self.cloud_w1[:, None], out=cf)
        np.multiply(self._c2, self.cloud_w2[:, None], out=self._tmp)
        cf += self._tmp
        np.clip(cf, 0.0, 1.0, out=cf)
        # thick cloud swallows what little light there is
        np.multiply(cf, -0.45, out=self._tmp)
        self._tmp += 1.0
        r[:hy] *= self._tmp
        g[:hy] *= self._tmp
        b[:hy] *= self._tmp

        # --- moon
        if glow > 0.001:
            self._moon(r, g, b, float(glow), float(moon_x), float(moon_y), cf)

        # --- distant islands
        self._islands(r, g, b)

        # --- sea
        if swell > 0.001:
            self._water(r, g, b, t, float(swell), float(moon_x))

        # --- drifting fog
        if fog > 0.001:
            self._fog(r, g, b, t, float(fog))

        # --- rain
        if rain > 0.001:
            self._rain(r, g, b, t, float(rain))

        # --- lightning
        if flash > 0.001:
            f = float(flash)
            r += 96.0 * f
            g += 148.0 * f
            b += 158.0 * f

        # --- near camera reef
        self._foreground(r, g, b, float(rise))

        # --- palette drift
        if tint > 0.001:
            a = 0.18 * float(tint)
            r *= (1.0 - a)
            r += 10.0 * a
            g *= (1.0 - a)
            g += 30.0 * a
            b *= (1.0 - a)
            b += 34.0 * a

        np.clip(r, 0.0, 255.0, out=r)
        np.clip(g, 0.0, 255.0, out=g)
        np.clip(b, 0.0, 255.0, out=b)

        out = self._pool[self._pool_i]
        self._pool_i = 1 - self._pool_i
        out[..., 0] = r
        out[..., 1] = g
        out[..., 2] = b
        return out
