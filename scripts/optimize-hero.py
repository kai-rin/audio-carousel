"""Turn promo's Hero stills into the README hero images.

Usage (from the repo root):
    cd promo && npm run hero          # renders promo/out/Hero{En,Ja,ZhHans}.png
    python scripts/optimize-hero.py   # writes docs/images/hero-*.png

256-colour MEDIANCUT quantization (LIBIMAGEQUANT is not in the Windows Pillow
wheel), then Pillow's PNG optimizer. If `oxipng` is on PATH it is run as a
final lossless pass.
"""

import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SOURCES = {
    "HeroEn.png": "hero-en.png",
    "HeroJa.png": "hero-ja.png",
    "HeroZhHans.png": "hero-zh-Hans.png",
}


def main() -> int:
    out_dir = ROOT / "docs" / "images"
    oxipng = shutil.which("oxipng")
    for src_name, dst_name in SOURCES.items():
        src = ROOT / "promo" / "out" / src_name
        if not src.exists():
            print(f"missing {src}; run `npm run hero` in promo/ first", file=sys.stderr)
            return 1
        img = Image.open(src).convert("RGB")
        quantized = img.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        dst = out_dir / dst_name
        quantized.save(dst, optimize=True)
        if oxipng:
            subprocess.run([oxipng, "-o", "4", "--strip", "safe", str(dst)], check=True)
        print(f"wrote {dst.relative_to(ROOT)} ({dst.stat().st_size / 1024:.0f} KiB)")
    if not oxipng:
        print("note: oxipng not found; skipped the extra lossless pass")
    return 0


if __name__ == "__main__":
    sys.exit(main())
