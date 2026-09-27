"""Cut promo's IconSheet render into the app's .ico files.

Usage (from the repo root):
    cd promo && npx remotion still IconSheet out/icon-sheet.png --image-format=png
    python scripts/build-icons.py

The sheet layout (sizes, gap, row order) must match promo/src/icon/IconSheet.tsx.
Frames up to 64 px are stored as 32-bit BMP (what the shell and LoadImage
handle everywhere); 256 px is stored as PNG, the standard for large frames.
"""

import io
import struct
import sys
from pathlib import Path

from PIL import Image

SIZES = [16, 20, 24, 32, 40, 48, 64, 256]
GAP = 10
ROW_HEIGHT = 256 + GAP
VARIANTS = {"app": 0, "tray-light": 1, "tray-dark": 2}

ROOT = Path(__file__).resolve().parent.parent
SHEET = ROOT / "promo" / "out" / "icon-sheet.png"
OUT_DIR = ROOT / "src" / "AudioCarousel" / "Resources"


def crop_frames(sheet: Image.Image, row: int) -> list[Image.Image]:
    frames, x = [], 0
    for size in SIZES:
        top = row * ROW_HEIGHT
        frames.append(sheet.crop((x, top, x + size, top + size)))
        x += size + GAP
    return frames


def bmp_frame(img: Image.Image) -> bytes:
    w, h = img.size
    header = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    # BGRA rows, bottom-up.
    rows = []
    px = img.load()
    for y in range(h - 1, -1, -1):
        rows.append(b"".join(struct.pack("BBBB", px[x, y][2], px[x, y][1], px[x, y][0], px[x, y][3]) for x in range(w)))
    xor = b"".join(rows)
    # AND mask: all zero (alpha carries transparency), rows padded to 32 bits.
    and_row = ((w + 31) // 32) * 4
    return header + xor + b"\x00" * (and_row * h)


def png_frame(img: Image.Image) -> bytes:
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


def write_ico(frames: list[Image.Image], path: Path) -> None:
    blobs = [png_frame(f) if f.width >= 256 else bmp_frame(f) for f in frames]
    offset = 6 + 16 * len(frames)
    out = bytearray(struct.pack("<HHH", 0, 1, len(frames)))
    for f, blob in zip(frames, blobs):
        dim = 0 if f.width >= 256 else f.width
        out += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(blob), offset)
        offset += len(blob)
    for blob in blobs:
        out += blob
    path.write_bytes(bytes(out))


def main() -> int:
    if not SHEET.exists():
        print(f"missing {SHEET}; render it first (see module docstring)", file=sys.stderr)
        return 1
    sheet = Image.open(SHEET).convert("RGBA")
    for name, row in VARIANTS.items():
        path = OUT_DIR / f"{name}.ico"
        write_ico(crop_frames(sheet, row), path)
        print(f"wrote {path.relative_to(ROOT)} ({path.stat().st_size} bytes)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
