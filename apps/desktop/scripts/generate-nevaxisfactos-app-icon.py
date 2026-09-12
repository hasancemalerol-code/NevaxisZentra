"""Generate canonical NevaxisFactOS desktop app icon (rounded blue square + white CF)."""

from __future__ import annotations

import math
import struct
import zlib
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

REPO_ROOT = Path(__file__).resolve().parents[3]
BRAND_DIR = REPO_ROOT / "apps" / "desktop" / "NevaxisFactOS.Desktop" / "Assets" / "Brand"
PNG_PATH = BRAND_DIR / "NevaxisFactOS-app-icon.png"
ICO_PATH = BRAND_DIR / "NevaxisFactOS-app-icon.ico"

BRAND_TOP = (58, 126, 196)
BRAND_BOTTOM = (36, 99, 167)  # #2463a7 — matches web --color-accent
BORDER = (185, 205, 225)  # #b9cde1
WHITE = (255, 255, 255)
ICO_SIZES = (16, 24, 32, 48, 64, 128, 256)


def _lerp(a: int, b: int, t: float) -> int:
    return int(round(a + (b - a) * t))


def _rounded_rect_mask(size: int, radius: int, inset: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle(
        (inset, inset, size - inset - 1, size - inset - 1),
        radius=radius,
        fill=255,
    )
    return mask


def _gradient_square(size: int) -> Image.Image:
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    inset = max(1, round(size * 0.06))
    radius = max(2, round(size * 0.22))
    mask = _rounded_rect_mask(size, radius, inset)

    gradient = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = gradient.load()
    for y in range(size):
        t = y / max(1, size - 1)
        color = (
            _lerp(BRAND_TOP[0], BRAND_BOTTOM[0], t),
            _lerp(BRAND_TOP[1], BRAND_BOTTOM[1], t),
            _lerp(BRAND_TOP[2], BRAND_BOTTOM[2], t),
            255,
        )
        for x in range(size):
            pixels[x, y] = color

    image = Image.composite(gradient, image, mask)

    border = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    border_draw = ImageDraw.Draw(border)
    border_draw.rounded_rectangle(
        (inset, inset, size - inset - 1, size - inset - 1),
        radius=radius,
        outline=BORDER + (255,),
        width=max(1, round(size / 38)),
    )
    return Image.alpha_composite(image, border)


def _load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (
        "C:/Windows/Fonts/segoeuib.ttf",
        "C:/Windows/Fonts/arialbd.ttf",
        "C:/Windows/Fonts/calibrib.ttf",
    )
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


def _draw_monogram(image: Image.Image, size: int) -> None:
    draw = ImageDraw.Draw(image)
    text = "CF"
    font_size = max(7, round(size * (0.34 if size >= 48 else 0.42)))
    font = _load_font(font_size)
    bbox = draw.textbbox((0, 0), text, font=font)
    text_w = bbox[2] - bbox[0]
    text_h = bbox[3] - bbox[1]
    x = (size - text_w) / 2 - bbox[0]
    y = (size - text_h) / 2 - bbox[1] - max(0, round(size * 0.01))
    draw.text((x, y), text, font=font, fill=WHITE + (255,))


def render_icon(size: int) -> Image.Image:
    image = _gradient_square(size)
    _draw_monogram(image, size)
    return image


def write_png(path: Path, image: Image.Image) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=True)


def write_ico(path: Path, images: dict[int, Image.Image]) -> None:
    """Write a multi-resolution ICO without relying on Pillow ICO quirks."""
    entries: list[tuple[int, bytes]] = []
    for size in sorted(images):
        rgba = images[size].convert("RGBA")
        bmp = _rgba_to_bmp(rgba)
        entries.append((size, bmp))

    count = len(entries)
    header = struct.pack("<HHH", 0, 1, count)
    offset = 6 + (16 * count)
    directory = bytearray()
    image_data = bytearray()

    for size, bmp in entries:
        width = 0 if size >= 256 else size
        height = 0 if size >= 256 else size
        directory.extend(
            struct.pack(
                "<BBBBHHII",
                width,
                height,
                0,
                0,
                1,
                32,
                len(bmp),
                offset,
            )
        )
        image_data.extend(bmp)
        offset += len(bmp)

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(header + directory + image_data)


def _rgba_to_bmp(rgba: Image.Image) -> bytes:
    width, height = rgba.size
    pixels = rgba.tobytes("raw", "BGRA")
    row_bytes = width * 4
    and_mask_stride = ((width + 31) // 32) * 4
    and_mask = bytes(and_mask_stride * height)
    header = struct.pack(
        "<IIIHHIIIIII",
        40,
        width,
        height * 2,
        1,
        32,
        0,
        0,
        0,
        0,
        0,
        0,
    )
    bottom_up = bytearray()
    for y in range(height - 1, -1, -1):
        start = y * row_bytes
        bottom_up.extend(pixels[start : start + row_bytes])
    return header + bytes(bottom_up) + and_mask


def main() -> None:
    master = render_icon(512)
    write_png(PNG_PATH, master)

    ico_images = {size: render_icon(size) for size in ICO_SIZES}
    write_ico(ICO_PATH, ico_images)

    print(f"Wrote {PNG_PATH}")
    print(f"Wrote {ICO_PATH} ({', '.join(str(s) for s in ICO_SIZES)})")


if __name__ == "__main__":
    main()
