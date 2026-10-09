"""Measure existing atlas artwork; does not modify the PNGs. Requires Pillow."""
import json
from pathlib import Path
from PIL import Image, ImageChops

root = Path(__file__).parent
atlases = []
for number, columns in [(1, 10), (2, 10), (3, 9)]:
    file = f"assets/realistic-v3-{number}.png"
    image = Image.open(root / file).convert("RGB")
    width, height = image.size
    # Find each atlas's actual empty horizontal gutters, including its bottom margin.
    red, green, blue = image.split()
    mask = ImageChops.lighter(ImageChops.lighter(red, green), blue).point(lambda value: 255 if value > 60 else 0)
    gaps, start = [], None
    for y in range(height + 1):
        empty = y < height and mask.crop((0, y, width, y + 1)).getbbox() is None
        if empty and start is None:
            start = y
        if not empty and start is not None:
            if start > 0 and y < height and y - start >= 2:
                gaps.append((start, y))
            start = None
    if len(gaps) != 5:
        raise ValueError(f"Expected five empty row gutters: {file}, found {gaps}")
    row_edges = [0] + [(start + end) // 2 for start, end in gaps] + [height]
    crops = []
    for row in range(6):
        for column in range(columns):
            left, right = round(column * width / columns), round((column + 1) * width / columns)
            # One generated hologram table spans two nominal cells. Keep it whole.
            if number == 2 and row == 2 and column == 6:
                right = round(8 * width / columns)
            top, bottom = row_edges[row:row + 2]
            # Navy background remains below 60; metal, glass and accents exceed it.
            bounds = mask.crop((left, top, right, bottom)).getbbox()
            if bounds is None:
                raise ValueError(f"Empty cell: {file}, {row}, {column}")
            x0, y0, x1, y1 = bounds
            x0, y0 = max(left, left + x0 - 4), max(top, top + y0 - 4)
            x1, y1 = min(right, left + x1 + 4), min(bottom, top + y1 + 4)
            crops.append(dict(cropX=x0, cropY=y0, cropWidth=x1-x0, cropHeight=y1-y0))
    atlases.append(dict(file=file, imageWidth=width, imageHeight=height, rowEdges=row_edges, crops=crops))
(root / "assets/realistic-v3-crops.json").write_text(json.dumps(dict(atlases=atlases), indent=2) + "\n", encoding="utf-8")
print(f"Measured {sum(len(a['crops']) for a in atlases)} atlas cells; PNGs unchanged.")
