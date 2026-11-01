"""Move stage two's palette between the cyan stage-one and violet stage-three art.

The scene pixels are left in place; this is a restrained HSV grade that shifts
the purple midtones toward blue-cyan while keeping bright energy markers luminous.
"""
from pathlib import Path
import colorsys
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Resources" / "Art"
SOURCE = ART / "Arena" / "arena-floor-stage2.png"
OUTPUT = ART / "Arena" / "arena-floor-stage2.png"


def grade(pixel):
    r, g, b, a = pixel
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    # Stage 2 should sit between stage 1's cyan (about 0.575) and stage 3's
    # violet (about 0.666). Shift darker/mid pixels more than bright accents.
    shift = -0.055 * (1.0 - 0.35 * v)
    h = (h + shift) % 1.0
    s = min(1.0, s * 0.94)
    v = min(1.0, v * 1.015)
    nr, ng, nb = colorsys.hsv_to_rgb(h, s, v)
    return round(nr * 255), round(ng * 255), round(nb * 255), a


def main():
    image = Image.open(SOURCE).convert("RGBA")
    image.putdata([grade(pixel) for pixel in image.getdata()])
    image.save(OUTPUT, optimize=True)
    print(f"Graded {OUTPUT} ({image.width}x{image.height})")


if __name__ == "__main__":
    main()
