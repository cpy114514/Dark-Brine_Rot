"""Global settings and palette for the Dark Brine_Rot teaser."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "out"

# --- output ---------------------------------------------------------------
WIDTH = 1920
HEIGHT = 1080
FPS = 24
DURATION = 23.0

# --- palette (RGB 0-255) --------------------------------------------------
INK = (3, 6, 9)
DEEP = (5, 16, 22)
BRINE = (12, 42, 48)
TEAL = (44, 128, 128)
FOAM = (170, 216, 208)
BONE = (230, 234, 222)
RUST = (178, 72, 40)
EMBER = (255, 128, 48)
BLOOD = (120, 22, 18)
GOLD = (206, 172, 96)

# --- title ----------------------------------------------------------------
TITLE_TOP = "DARK"
TITLE_BOTTOM = "BRINE_ROT"
TAGLINE = "SURVIVE THE ISLAND."
