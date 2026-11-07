"""Post-processing: camera move, bloom, glitch, flash, grain and letterbox."""
from __future__ import annotations

import numpy as np
from PIL import Image, ImageFilter


def camera(arr, zoom=1.0, cx=0.5, cy=0.5, sx=0.0, sy=0.0, chroma=0):
    """Crop-resize the frame to fake a camera push; optional RGB split."""
    h, w = arr.shape[:2]
    zoom = max(1.0, float(zoom))
    im = Image.fromarray(np.clip(arr, 0.0, 255.0).astype(np.uint8), "RGB")

    cw = w / zoom
    ch = h / zoom
    x = np.clip(cx * w - cw / 2.0 + sx * w, 0.0, w - cw)
    y = np.clip(cy * h - ch / 2.0 + sy * h, 0.0, h - ch)
    im = im.crop((int(x), int(y), int(x + cw), int(y + ch)))
    im = im.resize((w, h), Image.BICUBIC)

    a = np.asarray(im, dtype=np.uint8)
    if chroma > 0:
        a = a.copy()
        a[..., 0] = np.roll(a[..., 0], -int(chroma), axis=1)
        a[..., 2] = np.roll(a[..., 2], int(chroma), axis=1)
    return a.astype(np.float32)


def soft_bloom(arr, radius=18.0, strength=0.55, threshold=150.0):
    """Additive glow from the bright parts of the frame."""
    a = np.clip(arr, 0.0, 255.0)
    lum = a.max(axis=2)
    mask = np.clip((lum - threshold) / max(1.0, 255.0 - threshold), 0.0, 1.0)[..., None]
    if float(mask.max()) <= 0.0:
        return arr
    bright = (a * mask).astype(np.uint8)
    blur = Image.fromarray(bright, "RGB").filter(ImageFilter.GaussianBlur(float(radius)))
    return arr + np.asarray(blur, dtype=np.float32) * float(strength)


def glitch(arr, amount, seed=0):
    """Horizontal slice tearing; used in the first frames after an impact cut."""
    if amount <= 0.01:
        return arr
    rng = np.random.default_rng(int(seed) & 0xFFFFFFFF)
    out = arr
    h, w = out.shape[:2]
    for _ in range(int(4 + 6 * amount)):
        y0 = int(rng.integers(0, max(1, h - 8)))
        hgt = int(rng.integers(2, max(3, int(h * 0.035))))
        dx = int(rng.normal(0.0, 46.0 * amount))
        if dx:
            out[y0:y0 + hgt] = np.roll(out[y0:y0 + hgt], dx, axis=1)
    return out


def flash(arr, amount, color=(150, 220, 225)):
    if amount <= 0.001:
        return arr
    a = min(0.9, float(amount))
    return arr * (1.0 - a) + np.asarray(color, dtype=np.float32) * a


def finish(arr, grain_bank, vig, seed=0, grain=0.05, exposure=1.0,
           lift=0.0, contrast=1.0):
    """Highlight rolloff, contrast curve, vignette and film grain."""
    x = np.clip(arr * float(exposure) + float(lift), 0.0, 255.0)
    if contrast != 1.0:
        x = 255.0 * np.clip((x / 255.0 - 0.5) * float(contrast) + 0.5, 0.0, 1.0)
    x = x - 0.16 * x * (x / 255.0) ** 2
    x = np.clip(x, 0.0, 255.0)
    x *= vig
    if grain > 0.0 and grain_bank:
        g = grain_bank[int(seed) % len(grain_bank)]
        x += (g[..., None] - 127.5) * float(grain)
    return x


def letterbox(arr, bar):
    bar = int(bar)
    if bar > 0:
        arr[:bar] = 0.0
        arr[-bar:] = 0.0
    return arr
