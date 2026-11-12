"""Иконка установщика AnomalyPath: кадр постера вокруг фигуры сталкера -> .ico.

Запуск: python tools/installer/make-icon.py
Результат: generated/installer/icon.ico (16, 24, 32, 48, 64, 128, 256 px).
"""

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
POSTER = ROOT / "generated" / "poster_anomalypath_final_v2.png"
OUTPUT = ROOT / "generated" / "installer" / "icon.ico"
SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]

# Кадр постера (лево, верх, право, низ): голова в противогазе и зелёное свечение справа.
CROP = (470, 480, 760, 770)


def main() -> None:
    source = Image.open(POSTER).convert("RGB")
    icon = source.crop(CROP).resize((256, 256), Image.LANCZOS)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    icon.save(OUTPUT, format="ICO", sizes=SIZES)
    print(f"{OUTPUT.relative_to(ROOT)}: {OUTPUT.stat().st_size} bytes")


if __name__ == "__main__":
    main()
