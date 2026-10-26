"""Derive the in-game R04 blue paint palette without changing the approved mesh.

The original authored atlas supplies brushing, scratches, seams and wear.  Only
the sixteen manufactured material swatches change hue; original model/atlas,
normal and per-swatch metal/smoothness maps remain untouched.  Python + Pillow
and NumPy; no external services.  Run from any working directory.
"""
from pathlib import Path
import hashlib
import re
import uuid

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_Albedo.png"
TARGET = ROOT / "Assets/SpaceMiner/Resources/TouchConsoleR04/TouchConsole_BlueAlbedo.png"

# Bottom-to-top UV atlas ordering, identical to CreateTouchConsole.SWATCHES.
# Blue metallic paint, cool titanium cheeks, dark seals and discrete fasteners.
PALETTE = (
    (24, 53, 83), (39, 68, 100), (110, 145, 179), (91, 126, 161),
    (149, 177, 199), (3, 7, 12), (4, 7, 10), (10, 20, 30),
    (11, 20, 29), (100, 128, 151), (18, 36, 55), (26, 51, 77),
    (54, 82, 108), (7, 19, 32), (85, 116, 145), (6, 13, 22),
)


def main():
    original_hash = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
    original = np.asarray(Image.open(SOURCE).convert("RGBA"), dtype=np.float32)
    height, width = original.shape[:2]
    if width != height or width % 4:
        raise ValueError("Expected the authored square 4 x 4 material atlas")
    result = original.copy()
    tile = width // 4
    for index, color in enumerate(PALETTE):
        row, column = divmod(index, 4)
        region = np.s_[height - (row + 1) * tile:height - row * tile,
                       column * tile:(column + 1) * tile]
        rgb = original[region][..., :3]
        # Preserve the source's machined grain and additive wear rather than
        # tinting pale structural metal into the same flat blue as the cabinet.
        luminance = np.mean(rgb, axis=2)
        detail = luminance - np.median(luminance)
        result[region][..., :3] = np.clip(np.asarray(color) + detail[..., None], 0, 255)
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.rint(result).astype(np.uint8), "RGBA").save(TARGET, optimize=True)
    metadata = Path(str(TARGET) + ".meta")
    if not metadata.exists():
        template = Path(str(SOURCE) + ".meta").read_text(encoding="utf-8")
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "SpaceMiner/TouchConsole/R04/BlueAlbedo").hex
        metadata.write_text(re.sub(r"^guid: [a-f0-9]+", "guid: " + guid, template,
                                   count=1, flags=re.MULTILINE), encoding="utf-8")
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != original_hash:
        raise RuntimeError("Original atlas changed")
    print(f"R04 BLUE PALETTE: {width} x {height}; original preserved; {TARGET.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
