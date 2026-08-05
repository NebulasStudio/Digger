#!/usr/bin/env python3
"""Create a deterministic, pixel-safe runtime sprite from a magenta-keyed source."""

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


def convert(source: Path, output: Path, target_height: int, padding: int) -> None:
    image = Image.open(source).convert("RGBA")
    pixels = image.load()
    minimum_x = image.width
    minimum_y = image.height
    maximum_x = -1
    maximum_y = -1

    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, _ = pixels[x, y]
            if is_key_magenta(red, green, blue):
                pixels[x, y] = (red, green, blue, 0)
                continue

            pixels[x, y] = (red, green, blue, 255)
            minimum_x = min(minimum_x, x)
            minimum_y = min(minimum_y, y)
            maximum_x = max(maximum_x, x)
            maximum_y = max(maximum_y, y)

    if maximum_x < minimum_x or maximum_y < minimum_y:
        raise ValueError(f"No opaque subject remained after keying {source}")

    minimum_x = max(0, minimum_x - padding)
    minimum_y = max(0, minimum_y - padding)
    maximum_x = min(image.width - 1, maximum_x + padding)
    maximum_y = min(image.height - 1, maximum_y + padding)
    cropped = image.crop((minimum_x, minimum_y, maximum_x + 1, maximum_y + 1))

    if cropped.height > target_height:
        target_width = max(1, round(cropped.width * target_height / cropped.height))
        cropped = cropped.resize((target_width, target_height), Image.Resampling.NEAREST)

    output.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(output, format="PNG", optimize=False)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    print(f"{output}: {cropped.width}x{cropped.height} sha256={digest}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--target-height", type=int, required=True)
    parser.add_argument("--padding", type=int, default=8)
    args = parser.parse_args()

    if args.target_height <= 0:
        parser.error("--target-height must be positive")
    if args.padding < 0:
        parser.error("--padding cannot be negative")

    convert(args.source, args.output, args.target_height, args.padding)


if __name__ == "__main__":
    main()
