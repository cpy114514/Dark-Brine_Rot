'''Vector figure library for Dark Brine_Rot.

Every character, boss and weapon is drawn from scratch as a layered vector
figure.  To keep the geometry predictable, each figure is authored in a
canonical 1000x1000 space and scaled down once at the end, so no figure can
drift out of frame because of a missed scale factor.

The look is a hard cinematic silhouette: the body is filled with a very dark
teal gradient, interior shading is soft and low-contrast, and the shape is
read almost entirely through a tight directional rim light plus a faint
outer halo.

Nothing here needs an external image file.
'''
from __future__ import annotations

import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from . import config as C

BASE = 1000
CANVAS = 1000  # canonical authoring space


# --------------------------------------------------------------- helpers

def _vgrad(size, top, bottom, y0=0.12, y1=0.92):
    """Vertical gradient image (RGB) covering the whole canvas."""
    w, h = size
    ys = np.linspace(0.0, 1.0, h, dtype=np.float32)
    t = np.clip((ys - y0) / max(1e-6, (y1 - y0)), 0.0, 1.0)[:, None]
    c = (np.array(top, np.float32)[None, :] * (1.0 - t)
         + np.array(bottom, np.float32)[None, :] * t)
    return Image.fromarray(np.repeat(c[:, None, :], w, axis=1).astype(np.uint8), "RGB")


class Body:
    """Accumulates silhouette polygons on an L mask, then shades them."""

    def __init__(self, size=CANVAS):
        self.size = (int(size), int(size))
        self.mask = Image.new("L", self.size, 0)
        self.d = ImageDraw.Draw(self.mask)

    def poly(self, pts, fill=255):
        self.d.polygon([(float(x), float(y)) for x, y in pts], fill=fill)
        return self

    def ell(self, box, fill=255):
        x0, y0, x1, y1 = (float(v) for v in box)
        self.d.ellipse([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], fill=fill)
        return self

    def rect(self, box, fill=255):
        x0, y0, x1, y1 = (float(v) for v in box)
        self.d.rectangle([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], fill=fill)
        return self

    def line(self, pts, width, fill=255):
        self.d.line([(float(x), float(y)) for x, y in pts], fill=fill, width=int(width))
        return self

    def circle(self, cx, cy, r, fill=255):
        return self.ell([cx - r, cy - r, cx + r, cy + r], fill)

    def build(self, top, bottom, y0=0.12, y1=0.92):
        """Return an RGBA layer with the body filled by a vertical gradient."""
        layer = Image.new("RGBA", self.size, (0, 0, 0, 0))
        layer.paste(_vgrad(self.size, top, bottom, y0, y1), (0, 0), self.mask)
        return layer


def _overlay(size, fn, blur=0.0):
    """Draw translucent interior shading onto a fresh RGBA layer."""
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    fn(ImageDraw.Draw(layer), layer)
    if blur > 0.0:
        layer = layer.filter(ImageFilter.GaussianBlur(float(blur)))
    return layer


def _finish(layer, edge=C.FOAM, glow=C.TEAL, inner=1.15, halo=0.45,
            inner_w=2.0, halo_w=7.0, dirx=3.0, diry=4.0):
    """Add a directional inner rim light and a restrained outer halo."""
    alpha = np.asarray(layer.split()[3], dtype=np.float32) / 255.0
    if alpha.max() <= 0.0:
        return layer

    blurred = np.asarray(
        Image.fromarray((alpha * 255).astype(np.uint8), "L")
        .filter(ImageFilter.GaussianBlur(float(halo_w))),
        dtype=np.float32) / 255.0

    shifted = np.roll(alpha, (int(diry), int(dirx)), axis=(0, 1))
    shifted[:int(diry), :] = alpha[:int(diry), :]
    shifted[-int(diry):, :] = alpha[-int(diry):, :]
    shifted[:, :int(dirx)] = alpha[:, :int(dirx)]
    shifted[:, -int(dirx):] = alpha[:, -int(dirx):]
    edge_band = np.clip(alpha - shifted, 0.0, 1.0)
    if inner_w > 0.0:
        edge_band = np.asarray(
            Image.fromarray((edge_band * 255).astype(np.uint8), "L")
            .filter(ImageFilter.GaussianBlur(float(inner_w))),
            dtype=np.float32) / 255.0

    rgb = np.asarray(layer.convert("RGB"), dtype=np.float32)
    rgb += edge_band[..., None] * np.array(edge, np.float32) * float(inner)
    # the halo mostly tints the air around the figure, not the figure itself
    rgb += (blurred[..., None] * np.array(glow, np.float32)
            * float(halo) * 0.55 * (1.0 - alpha * 0.88)[..., None])

    out = Image.fromarray(np.clip(rgb, 0.0, 255.0).astype(np.uint8), "RGB")
    a = np.clip(alpha + blurred * 0.42 * float(halo), 0.0, 1.0)
    out.putalpha(Image.fromarray((a * 255).astype(np.uint8), "L"))
    return out


def _tapered(p0, p1, bend=0.0, w0=10.0, w1=10.0, steps=40):
    """Polygon outline of a curved stroke that narrows toward p1."""
    x0, y0 = p0
    x1, y1 = p1
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    cx = (x0 + x1) / 2.0 - dy / length * bend
    cy = (y0 + y1) / 2.0 + dx / length * bend

    left, right = [], []
    for i in range(steps + 1):
        t = i / steps
        bx = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * cx + t * t * x1
        by = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * cy + t * t * y1
        tx = 2 * (1 - t) * (cx - x0) + 2 * t * (x1 - cx)
        ty = 2 * (1 - t) * (cy - y0) + 2 * t * (y1 - cy)
        norm = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / norm, tx / norm
        half = (w0 + (w1 - w0) * t) / 2.0
        left.append((bx + nx * half, by + ny * half))
        right.append((bx - nx * half, by - ny * half))
    return left + right[::-1]


def _blade(p0, p1, width=120.0, curve=0.07, steps=30):
    """Kukri-style blade: a spine with a deep curved belly (cutting edge)."""
    x0, y0 = p0
    x1, y1 = p1
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    ux, uy = dx / length, dy / length
    nx, ny = -uy, ux

    spine, belly = [], []
    for i in range(steps + 1):
        t = i / steps
        wob = math.sin(t * math.pi) * length * curve
        sx = x0 + dx * t + nx * wob
        sy = y0 + dy * t + ny * wob
        spine.append((sx, sy))
        shape = math.sin(math.pi * (0.16 + 0.84 * t)) ** 0.72
        off = width * shape
        belly.append((sx + nx * off, sy + ny * off))
    return spine, belly


def _smooth(points, per_seg=8, closed=True):
    """Catmull-Rom closed spline through the given points.

    Organic bodies built from straight polygons read as flat slabs; this
    rounds them into an actual silhouette.
    """
    n = len(points)
    if n < 3:
        return list(points)
    out = []
    for i in range(n if closed else n - 1):
        p0 = points[(i - 1) % n] if closed else points[max(i - 1, 0)]
        p1 = points[i % n]
        p2 = points[(i + 1) % n]
        p3 = points[(i + 2) % n] if closed else points[min(i + 2, n - 1)]
        for j in range(per_seg):
            t = j / per_seg
            t2 = t * t
            t3 = t2 * t
            x = 0.5 * (2 * p1[0] + (-p0[0] + p2[0]) * t
                       + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
                       + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * (2 * p1[1] + (-p0[1] + p2[1]) * t
                       + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
                       + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            out.append((x, y))
    return out


def _capsule(p0, p1, r, steps=14):
    """Stadium (rounded-rectangle) outline from p0 to p1 with radius r."""
    ang = math.atan2(p1[1] - p0[1], p1[0] - p0[0])
    pts = []
    for i in range(steps + 1):
        a = ang - math.pi / 2.0 + math.pi * i / steps
        pts.append((p1[0] + math.cos(a) * r, p1[1] + math.sin(a) * r))
    for i in range(steps + 1):
        a = ang + math.pi / 2.0 + math.pi * i / steps
        pts.append((p0[0] + math.cos(a) * r, p0[1] + math.sin(a) * r))
    return pts


def _octagon(cx, cy, rx, ry, rot=0.0):
    pts = []
    for i in range(8):
        a = rot + i * math.pi / 4.0
        pts.append((cx + rx * math.cos(a), cy + ry * math.sin(a)))
    return pts


def _blit(arr, layer, cx, cy, opacity=1.0, additive=False):
    """Composite an RGBA sprite onto a float RGB frame at (cx, cy)."""
    opacity = float(opacity)
    if opacity <= 0.002:
        return arr
    h, w = arr.shape[:2]
    lw, lh = layer.size
    x0 = int(round(cx - lw / 2.0))
    y0 = int(round(cy - lh / 2.0))
    ax0, ay0 = max(0, x0), max(0, y0)
    ax1, ay1 = min(w, x0 + lw), min(h, y0 + lh)
    if ax1 <= ax0 or ay1 <= ay0:
        return arr

    sx, sy = ax0 - x0, ay0 - y0
    sub = layer.crop((sx, sy, sx + (ax1 - ax0), sy + (ay1 - ay0)))
    a = np.asarray(sub.split()[3], dtype=np.float32) / 255.0 * opacity
    rgb = np.asarray(sub.convert("RGB"), dtype=np.float32)
    dst = arr[ay0:ay1, ax0:ax1]
    if additive:
        arr[ay0:ay1, ax0:ax1] = dst + rgb * a[..., None]
    else:
        arr[ay0:ay1, ax0:ax1] = dst * (1.0 - a[..., None]) + rgb * a[..., None]
    return arr


blit = _blit


def _fit(layer, size):
    size = max(32, int(round(size)))
    if layer.size == (size, size):
        return layer
    return layer.resize((size, size), Image.LANCZOS)


def ground_shadow(size=BASE, rx=0.22, ry=0.045, alpha=165):
    """Soft contact shadow to sit under a figure."""
    img = Image.new("RGBA", (int(size), int(size)), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([size * (0.5 - rx), size * (1.0 - ry * 2.0),
               size * (0.5 + rx), size * (1.0 - ry * 0.35)],
              fill=(0, 0, 0, int(alpha)))
    return img.filter(ImageFilter.GaussianBlur(size * 0.022))


# ---------------------------------------------------------------- weapons

def _club_head(b, cx, cy, scale=1.0, spikes=None):
    """Lumpy, gnarled club head: three overlapping masses plus spikes.

    Kept deliberately irregular — a smooth capsule reads as a cleaver
    blade, which is the wrong weapon entirely.
    """
    s = float(scale)
    b.ell([cx - 70 * s, cy - 100 * s, cx + 56 * s, cy + 4 * s])
    b.ell([cx - 44 * s, cy - 50 * s, cx + 84 * s, cy + 52 * s])
    b.ell([cx - 60 * s, cy - 4 * s, cx + 46 * s, cy + 92 * s])
    if spikes is None:
        spikes = ((-1.95, 92), (-1.05, 104), (-0.15, 96), (0.75, 84), (1.85, 70))
    for ang, ln in spikes:
        bx = cx + math.cos(ang) * 74.0 * s
        by = cy + math.sin(ang) * 74.0 * s
        tx = cx + math.cos(ang) * (74.0 + ln) * s
        ty = cy + math.sin(ang) * (74.0 + ln) * s
        px, py = -math.sin(ang) * 23.0 * s, math.cos(ang) * 23.0 * s
        b.poly([(bx + px, by + py), (bx - px, by - py), (tx, ty)])


def club(size=BASE, glow_color=C.TEAL):
    '''Brutal gnarled club: long shaft, knotted head, radiating spikes.'''
    b = Body(CANVAS)

    grip_a = (470, 934)
    grip_b = (452, 742)
    b.poly(_tapered(grip_a, grip_b, 10.0, 70.0, 60.0))

    shaft_b = (462, 742)
    head_c = (586, 470)
    b.poly(_tapered(shaft_b, head_c, 22.0, 60.0, 96.0))

    _club_head(b, 600, 388, 1.0)

    layer = b.build((26, 44, 48), (3, 8, 10), 0.20, 0.94)

    def detail(d, lay):
        for i in range(6):
            t = i / 5.0
            y = 758 + t * 156
            d.line([(438, y), (506, y + 13)], fill=(44, 58, 60, 130),
                   width=8)
        d.polygon([(516, 366), (676, 330), (692, 396), (530, 430)],
                  fill=(84, 22, 14, 115))
        d.ellipse([540, 300, 650, 384], fill=(150, 168, 164, 40))
        for ang, ln in ((-1.95, 92), (-1.05, 104), (-0.15, 96), (0.75, 84)):
            bx = 600 + math.cos(ang) * 66.0
            by = 388 + math.sin(ang) * 66.0
            tx = 600 + math.cos(ang) * (74.0 + ln * 0.9)
            ty = 388 + math.sin(ang) * (74.0 + ln * 0.9)
            d.line([(bx, by), (tx, ty)], fill=(168, 186, 182, 100), width=5)

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=3.0))
    return _fit(_finish(layer, edge=(186, 214, 210), glow=glow_color,
                        inner=1.1, halo=0.5), size)


def blades(size=BASE, glow_color=C.TEAL):
    '''Twin curved kukri blades with guards, wrapped grips and pommels.'''
    b = Body(CANVAS)

    specs = (((318, 792), (700, 286), -1.0), ((682, 792), (300, 286), 1.0))
    edges = []
    for p0, p1, side in specs:
        spine, belly = _blade(p0, p1, width=104.0, curve=0.05)
        b.poly(spine + belly[::-1])
        edges.append((spine, belly))

    for (p0, p1, side), (spine, belly) in zip(specs, edges):
        gx, gy = p0
        ang = math.atan2(p1[1] - p0[1], p1[0] - p0[0])
        nx, ny = -math.sin(ang), math.cos(ang)
        d = 58.0
        b.poly([(gx - nx * d, gy - ny * d), (gx + nx * d, gy + ny * d),
                (gx + nx * (d - 22) + (p1[0] - p0[0]) * 0.07,
                 gy + ny * (d - 22) + (p1[1] - p0[1]) * 0.07),
                (gx - nx * (d - 22) + (p1[0] - p0[0]) * 0.07,
                 gy - ny * (d - 22) + (p1[1] - p0[1]) * 0.07)])
        b.poly(_tapered((gx, gy), (gx - side * 16, gy + 122), 6.0, 48.0, 42.0))
        b.circle(gx - side * 19, gy + 138, 30)

    layer = b.build((30, 52, 56), (3, 9, 11), 0.14, 0.94)

    def detail(d, lay):
        for (p0, p1, side), (spine, belly) in zip(specs, edges):
            d.line(belly, fill=(204, 232, 228, 175), width=8, joint="curve")
            d.line(spine, fill=(64, 96, 98, 55), width=4, joint="curve")
            gx, gy = p0
            for i in range(4):
                t = i / 3.0
                yy = gy + 26 + t * 96
                xx = gx - side * (10 + t * 22)
                d.line([(xx - 24, yy), (xx + 24, yy + 9)],
                       fill=(104, 78, 48, 120), width=11)

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=1.6))
    return _fit(_finish(layer, edge=(206, 232, 228), glow=glow_color,
                        inner=1.2, halo=0.5), size)


def rifle(size=BASE, glow_color=C.TEAL):
    '''M4A1: flat-top rail, railed handguard, curved magazine, flash hider.'''
    b = Body(CANVAS)

    b.poly([(120, 556), (236, 548), (250, 618), (196, 706), (128, 700),
            (150, 618)])                                     # butt stock
    b.poly([(236, 546), (352, 552), (352, 596), (240, 598)])   # buffer tube
    b.poly([(340, 496), (640, 488), (640, 560), (352, 572)])   # upper receiver
    b.poly([(340, 468), (640, 460), (640, 492), (340, 500)])   # flat top rail
    b.poly([(636, 480), (812, 486), (812, 540), (636, 542)])   # handguard
    b.poly([(806, 498), (906, 502), (906, 526), (806, 528)])   # barrel
    b.poly([(902, 492), (958, 494), (958, 534), (902, 532)])   # flash hider
    b.poly([(356, 566), (476, 562), (476, 622), (356, 626)])   # magwell
    b.poly([(430, 618), (506, 612), (496, 720), (444, 728)])   # pistol grip
    b.poly([(474, 556), (600, 560), (640, 646), (588, 706), (506, 664)])
    b.poly([(676, 470), (760, 470), (760, 496), (676, 494)])   # rear sight block
    b.poly([(880, 440), (912, 440), (912, 492), (880, 492)])   # front sight
    b.poly([(700, 432), (762, 432), (762, 462), (700, 462)])   # optic
    b.poly([(588, 452), (664, 452), (664, 468), (588, 468)])   # charging handle
    b.poly([(452, 556), (452, 600), (478, 600), (478, 556)])   # trigger

    layer = b.build((30, 44, 48), (3, 8, 10), 0.32, 0.94)

    def detail(d, lay):
        for i in range(6):
            x = 650 + i * 28
            d.rectangle([x, 490, x + 14, 534], fill=(12, 20, 22, 180))
        d.rectangle([352, 474, 636, 484], fill=(118, 138, 136, 80))
        d.rectangle([640, 496, 808, 508], fill=(104, 124, 122, 70))
        for i in range(5):
            t = i / 4.0
            d.polygon([(484 + t * 96, 588), (532 + t * 104, 590),
                       (540 + t * 96, 660), (494 + t * 90, 654)],
                      fill=(20, 30, 32, 140))
        d.rectangle([706, 438, 756, 456], fill=(132, 154, 150, 120))
        d.ellipse([678, 468, 700, 490], fill=(0, 0, 0, 210))
        d.rectangle([906, 500, 952, 512], fill=(16, 24, 26, 200))
        d.rectangle([150, 566, 234, 582], fill=(96, 112, 112, 70))

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=2.0))
    return _fit(_finish(layer, edge=(178, 210, 206), glow=glow_color,
                        inner=1.05, halo=0.45), size)


# ------------------------------------------------------------- character

def sahur(weapon="club", size=BASE, glow_color=C.TEAL):
    '''Sahur: a lone hunter in a wind-torn coat, weapon held low.'''
    b = Body(CANVAS)

    # legs and boots
    b.poly(_tapered((462, 566), (438, 742), 10.0, 104.0, 74.0))
    b.poly(_tapered((538, 566), (566, 744), -10.0, 102.0, 72.0))
    b.poly(_tapered((438, 742), (428, 916), 6.0, 74.0, 56.0))
    b.poly(_tapered((566, 744), (578, 916), -6.0, 72.0, 54.0))
    b.poly([(372, 906), (466, 906), (486, 962), (350, 962)])
    b.poly([(524, 908), (618, 908), (642, 964), (506, 964)])

    # coat body, flaring and blown to the right
    b.poly([(420, 372), (582, 368), (614, 520), (598, 700), (628, 840),
            (500, 878), (372, 840), (398, 676), (396, 512)])
    # wind-blown coat tail on the right
    b.poly([(598, 540), (692, 596), (716, 726), (648, 700), (606, 664)])
    # inner torso column
    b.poly([(452, 356), (552, 352), (566, 520), (548, 640), (456, 644),
            (432, 520)])

    # shoulders
    b.poly([(384, 372), (446, 318), (556, 314), (620, 372), (588, 412),
            (416, 416)])

    # arms — the near arm reaches out so the club stays clear of the coat
    b.poly(_tapered((420, 396), (338, 494), -26.0, 78.0, 52.0))
    b.poly(_tapered((338, 494), (304, 588), 6.0, 52.0, 40.0))
    b.poly(_tapered((586, 396), (626, 502), 16.0, 76.0, 50.0))
    b.poly(_tapered((626, 502), (612, 596), -8.0, 50.0, 38.0))
    b.circle(300, 600, 30)
    b.circle(610, 610, 28)

    # neck, head, hood
    b.poly(_tapered((500, 330), (500, 288), 0.0, 58.0, 48.0))
    b.ell([452, 196, 548, 292])
    b.poly([(438, 268), (452, 146), (500, 112), (550, 148), (566, 270),
            (500, 232)])
    b.poly([(438, 258), (406, 330), (452, 300)])   # hood drape

    if weapon == "club":
        b.poly(_tapered((238, 916), (292, 500), -12.0, 56.0, 44.0))
        _club_head(b, 282, 380, 0.86,
                   spikes=((-2.0, 62), (-1.1, 70), (-0.2, 64), (0.7, 56)))
    elif weapon == "blades":
        for p0, p1, side in (((392, 604), (700, 236), -1.0),
                             ((596, 596), (286, 246), 1.0)):
            spine, belly = _blade(p0, p1, width=74.0, curve=0.05)
            b.poly(spine + belly[::-1])
    elif weapon == "rifle":
        b.poly([(268, 520), (452, 496), (470, 560), (286, 584)])
        b.poly([(452, 494), (640, 486), (640, 548), (452, 558)])
        b.poly([(352, 560), (410, 556), (404, 646), (362, 650)])
        b.poly([(486, 560), (560, 566), (566, 620), (500, 626)])

    layer = b.build((26, 46, 50), (3, 8, 10), 0.10, 0.94)

    def detail(d, lay):
        d.polygon([(472, 330), (536, 318), (540, 386), (474, 396)],
                  fill=(6, 12, 14, 120))
        d.ellipse([478, 226, 496, 240], fill=(226, 208, 148, 215))
        d.ellipse([504, 226, 522, 240], fill=(226, 208, 148, 215))
        d.polygon([(430, 350), (500, 312), (576, 350), (568, 368),
                   (500, 332), (438, 368)], fill=(74, 104, 104, 95))
        d.line([(420, 520), (432, 690)], fill=(8, 16, 18, 110), width=9)
        d.line([(566, 520), (578, 660)], fill=(8, 16, 18, 90), width=8)
        d.polygon([(470, 646), (546, 640), (556, 760), (486, 766)],
                  fill=(6, 12, 14, 90))

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=4.0))
    return _fit(_finish(layer, edge=(178, 216, 212), glow=glow_color,
                        inner=1.1, halo=0.5), size)


# ----------------------------------------------------------------- bosses

def cappuccino(size=BASE, glow_color=C.EMBER):
    '''Cappuccino Assassino: porcelain cup, cream crown, hooded killer.

    Built in four layers so the cup stays the brightest thing in the
    frame while the cowl and cape stay dark: hood/arms, porcelain cup,
    foam, then the cape that overlaps the rim.
    '''
    por_top, por_bot = (152, 156, 148), (26, 32, 34)
    drk_top, drk_bot = (34, 38, 38), (4, 7, 9)

    bd = Body(CANVAS)
    bd.poly([(420, 492), (436, 336), (500, 286), (564, 336), (580, 492),
             (500, 452)])
    bd.poly(_tapered((268, 570), (170, 664), -26.0, 42.0, 22.0))
    bd.poly(_tapered((732, 570), (818, 646), 22.0, 42.0, 24.0))
    bd.poly(_tapered((168, 668), (104, 764), 14.0, 24.0, 4.0))
    bd.poly(_tapered((404, 842), (386, 930), 6.0, 56.0, 38.0))
    bd.poly(_tapered((596, 842), (614, 930), -6.0, 56.0, 38.0))
    layer = bd.build(drk_top, drk_bot, 0.20, 0.92)

    bc = Body(CANVAS)
    bc.ell([262, 818, 738, 902])
    bc.poly([(268, 566), (732, 566), (664, 826), (336, 826)])
    bc.ell([248, 516, 752, 614])
    bc.poly(_tapered((714, 596), (836, 626), 34.0, 40.0, 36.0))
    bc.poly(_tapered((836, 626), (716, 706), -30.0, 36.0, 40.0))
    layer = Image.alpha_composite(layer, bc.build(por_top, por_bot, 0.56, 0.84))

    bm = Body(CANVAS)
    bm.ell([272, 530, 728, 598])
    bm.ell([316, 438, 684, 548])
    bm.ell([382, 396, 560, 476])
    bm.ell([454, 356, 592, 424])
    layer = Image.alpha_composite(layer, bm.build((182, 176, 160), (58, 58, 52), 0.28, 0.58))

    bp = Body(CANVAS)
    bp.poly([(258, 566), (338, 520), (500, 502), (662, 520), (742, 566),
             (716, 594), (500, 574), (284, 594)])
    bp.poly([(254, 558), (318, 516), (312, 590), (262, 598)])
    bp.poly([(746, 558), (682, 516), (688, 590), (738, 598)])
    layer = Image.alpha_composite(layer, bp.build(drk_top, drk_bot, 0.48, 0.62))

    def detail(d, lay):
        d.ellipse([466, 318, 534, 386], fill=(2, 5, 7, 245))
        d.ellipse([478, 344, 496, 358], fill=tuple(C.EMBER) + (255,))
        d.ellipse([508, 344, 526, 358], fill=tuple(C.EMBER) + (255,))
        for i in range(3):
            d.arc([346 + i * 52, 398, 462 + i * 52, 510], start=196, end=344,
                  fill=(206, 200, 186, 110), width=8)
        d.ellipse([282, 534, 718, 594], fill=(26, 18, 14, 165))
        d.rectangle([248, 560, 752, 600], fill=(238, 242, 234, 95))
        d.polygon([(296, 600), (350, 600), (386, 826), (352, 826)],
                  fill=(238, 242, 236, 55))
        d.ellipse([330, 610, 670, 706], fill=(30, 36, 38, 55))
        d.polygon([(346, 620), (404, 614), (396, 700), (354, 706)],
                  fill=(10, 14, 16, 80))
        d.ellipse([148, 656, 182, 690], fill=(214, 226, 220, 95))
        d.line([(272, 574), (338, 830)], fill=(226, 232, 226, 70), width=6)
        d.rectangle([268, 586, 732, 600], fill=(0, 0, 0, 55))
        d.line([(266, 560), (336, 524)], fill=(120, 130, 126, 70), width=5)
        d.line([(734, 560), (664, 524)], fill=(120, 130, 126, 70), width=5)

    layer = Image.alpha_composite(layer, _overlay(bd.size, detail, blur=2.6))
    return _fit(_finish(layer, edge=(228, 236, 228), glow=glow_color,
                        inner=1.0, halo=0.55), size)


def nailong(size=BASE, glow_color=C.TEAL):
    '''Nailong: horned noodle-dragon, coiling body, finned spine.'''
    b = Body(CANVAS)

    pts = []
    n = 54
    for i in range(n + 1):
        t = i / n
        x = 96 + 640 * t
        y = 858 - 524 * t + 176 * math.sin(t * 5.4 + 0.35) - 44 * math.sin(t * 10.5)
        r = 24 + 66 * math.sin(math.pi * min(1.0, t * 1.04)) ** 0.75
        pts.append((x, y, r))

    for x, y, r in pts:
        b.ell([x - r, y - r, x + r, y + r])

    for i in range(4, n - 6, 4):
        x, y, r = pts[i]
        b.poly([(x - 18, y - r * 0.74), (x + 18, y - r * 0.74),
                (x, y - r - 56)])

    hx, hy, hr = pts[-1]
    b.ell([hx - 58, hy - 70, hx + 106, hy + 64])
    b.poly([(hx + 46, hy - 42), (hx + 176, hy - 8), (hx + 164, hy + 32),
            (hx + 42, hy + 40)])
    b.poly([(hx + 38, hy + 28), (hx + 158, hy + 28), (hx + 136, hy + 76),
            (hx + 42, hy + 66)])
    b.poly(_tapered((hx + 2, hy - 60), (hx - 96, hy - 200), 36.0, 36.0, 6.0))
    b.poly(_tapered((hx + 60, hy - 62), (hx + 44, hy - 216), -30.0, 34.0, 6.0))
    for i in range(3):
        b.poly(_tapered((hx + 120, hy + 6 + i * 13), (hx + 206, hy - 40 + i * 38),
                        22.0, 12.0, 3.0))
    b.poly(_tapered((hx + 28, hy + 56), (hx + 100, hy + 132), 20.0, 28.0, 8.0))

    layer = b.build((24, 48, 52), (3, 9, 11), 0.12, 0.94)

    def detail(d, lay):
        for i in range(2, n - 10, 2):
            x, y, r = pts[i]
            d.arc([x - r * 0.85, y - r * 0.95, x + r * 0.85, y + r * 0.95],
                  start=188, end=352, fill=(72, 108, 110, 60), width=5)
        for i in range(4):
            tx = hx + 52 + i * 30
            d.polygon([(tx, hy + 26), (tx + 20, hy + 26), (tx + 10, hy + 60)],
                      fill=(206, 218, 206, 215))
        d.ellipse([hx + 26, hy - 46, hx + 68, hy - 8], fill=tuple(C.EMBER) + (255,))
        d.ellipse([hx + 40, hy - 40, hx + 58, hy - 20], fill=(12, 7, 4, 255))
        for i in range(3):
            d.line([(hx + 150, hy + 8 + i * 14), (hx + 220, hy - 36 + i * 32)],
                   fill=(146, 188, 186, 140), width=4)
        d.ellipse([hx - 24, hy - 34, hx + 42, hy + 12], fill=(14, 26, 28, 100))

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=2.4))
    return _fit(_finish(layer, edge=(176, 226, 220), glow=glow_color,
                        inner=1.15, halo=0.55), size)


def crocodilo(size=BASE, glow_color=C.EMBER):
    '''Bombardino Crocodilo: armoured gator hauling a lit bomb on its back.'''
    b = Body(CANVAS)

    b.poly(_tapered((272, 590), (104, 686), 62.0, 124.0, 26.0))
    b.poly(_tapered((150, 640), (74, 748), 26.0, 56.0, 14.0))

    b.poly(_smooth([(256, 456), (330, 418), (452, 398), (576, 410),
                    (660, 452), (690, 518), (652, 584), (536, 626),
                    (396, 628), (284, 590), (242, 518)], 6))
    for i in range(5):
        x = 296 + i * 74
        b.ell([x, 556 + i * 3, x + 62, 622 + i * 3])
    for lx, ly, s in ((300, 612, -1), (560, 620, 1)):
        b.poly(_tapered((lx, ly), (lx + 26 * s, ly + 84), 14.0, 84.0, 54.0))
        b.poly(_tapered((lx + 26 * s, ly + 84), (lx + 10 * s, ly + 126),
                        -6.0, 54.0, 40.0))
        b.ell([lx - 6 * s, ly + 120, lx + 74 * s, ly + 158])

    b.poly(_smooth([(612, 446), (740, 452), (856, 470), (898, 504),
                    (852, 544), (740, 556), (612, 558)], 6))
    b.poly(_smooth([(616, 580), (744, 588), (846, 598), (826, 634),
                    (744, 626), (616, 618)], 6))
    for i in range(6):
        x = 250 + i * 62
        b.poly([(x, 436 - i * 3), (x + 44, 436 - i * 3), (x + 22, 364 - i * 4)])
    b.poly(_smooth([(596, 452), (654, 418), (716, 414), (766, 452),
                    (700, 488), (626, 484)], 6))
    b.ell([432, 298, 602, 452])
    b.poly(_tapered((514, 306), (546, 248), 14.0, 24.0, 16.0))
    b.poly(_tapered((546, 248), (588, 214), -12.0, 16.0, 12.0))
    b.circle(600, 202, 20)

    layer = b.build((28, 40, 38), (4, 9, 9), 0.24, 0.94)

    def detail(d, lay):
        for i in range(8):
            x = 636 + i * 30
            d.polygon([(x, 542), (x + 20, 542), (x + 10, 584)],
                      fill=(212, 222, 210, 230))
        d.ellipse([668, 426, 714, 460], fill=tuple(C.EMBER) + (255,))
        d.ellipse([681, 435, 701, 453], fill=(14, 7, 4, 255))
        d.ellipse([452, 324, 496, 364], fill=(88, 96, 88, 130))
        d.ellipse([578, 180, 622, 218], fill=tuple(C.EMBER) + (235,))
        d.ellipse([568, 170, 632, 230], fill=(255, 168, 76, 60))
        for i in range(6):
            x = 258 + i * 58
            d.line([(x, 428), (x + 22, 370)], fill=(88, 100, 96, 70), width=4)
        d.polygon([(250, 500), (332, 508), (326, 566), (258, 558)],
                  fill=(10, 16, 16, 120))
        for i in range(4):
            d.line([(700 + i * 34, 450), (712 + i * 34, 412)],
                   fill=(16, 24, 24, 140), width=3)
        d.ellipse([448, 316, 508, 372], fill=(140, 152, 148, 90))
        d.line([(520, 300), (548, 250)], fill=(150, 166, 162, 90), width=4)

    layer = Image.alpha_composite(layer, _overlay(b.size, detail, blur=2.4))
    return _fit(_finish(layer, edge=(210, 222, 208), glow=glow_color,
                        inner=1.05, halo=0.55), size)


BOSSES = {
    "cappuccino": cappuccino,
    "nailong": nailong,
    "crocodilo": crocodilo,
}

WEAPONS = {
    "club": club,
    "blades": blades,
    "rifle": rifle,
}
