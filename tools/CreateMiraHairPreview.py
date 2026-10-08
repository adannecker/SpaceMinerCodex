from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]
paths=sorted((root/'work/MiraHairV07Frames').glob('frame-*.png'))
assert len(paths)==24, f'Expected 24 frames, found {len(paths)}'
frames=[Image.open(p).convert('RGB') for p in paths]
# Use one palette throughout to avoid palette changes between frames.
palette=frames[0].quantize(colors=256)
frames=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
out=root/'docs/Art/Mira3D/Mira-Hair-v07-Loop.gif'
frames[0].save(out,save_all=True,append_images=frames[1:],duration=80,loop=0,optimize=False)
check=Image.open(out);assert check.n_frames==24
print(out,check.n_frames)
