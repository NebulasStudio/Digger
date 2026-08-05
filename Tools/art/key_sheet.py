#!/usr/bin/env python3
"""Remove the magenta key from a sprite SHEET while preserving the frame grid.

Unlike key_magenta_sprite.py (which crops to a single sprite and resizes), this tool
only removes the magenta background and preserves the sheet's full dimensions, so the
NxM frame grid stays intact for later slicing in Unity (SpriteSheetImporter).

Usage:
  python3 key_sheet.py <source> <output> [--fill-glitch]
"""
from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


def is_key_magenta(red: int, green: int, blue: int) -> bool:
    return (
        red >= 145
        and blue >= 145
        and green <= 145
        and red > green + 35
        and blue > green + 35
    )


def convert(source: Path, output: Path, fill_glitch: bool) -> None:
    image = Image.open(source).convert("RGBA")
    pixels = image.load()

    # First pass: clear magenta to transparent; track a per-pixel original for glitch fill.
    original = image.copy().load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if is_key_magenta(r, g, b):
                pixels[x, y] = (r, g, b, 0)

    # Optional: fill fully-transparent "holes" (magenta pockets inside the subject) with the
    # nearest-opaque neighbor color so transparent-internal glitches don't punch through.
    if fill_glitch:
        _fill_interior_holes(image, pixels)

    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output, format="PNG", optimize=False)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    print(f"{output}: {image.width}x{image.height} sha256={digest}")


def _fill_interior_holes(image: Image.Image, px) -> None:
    """Replace transparent pixels fully surrounded by opaque pixels with the region average."""
    width, height = image.size
    rgba = image.convert("RGBA")
    src = rgba.load()
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            if src[x, y][3] != 0:
                continue
            # 4-neighborhood: if all opaque around a transparent pixel, sample the average.
            around = [src[x + dx, y + dy] for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))]
            if all(p[3] > 200 for p in around):
                r = sum(p[0] for p in around) // 4
                g = sum(p[1] for p in around) // 4
                b = sum(p[2] for p in around) // 4
                px[x, y] = (r, g, b, 255)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--fill-glitch", action="store_true",
                        help="Repair transparent pockets fully enclosed by opaque pixels.")
    args = parser.parse_args()
    convert(args.source, args.output, args.fill_glitch)


if __name__ == "__main__":
    main()