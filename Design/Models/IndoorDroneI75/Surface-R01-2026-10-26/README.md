# I75 V02 — Surface R01

Actual Tripo Smart Mesh P2.0 V02 geometry with authored UVs and PBR surface.
75cm long; original10659 polygons /18312 triangles; no mesh repair or rig.
Blender coordinates: X width, Z up, nose−Y. Four runtime image textures packed
in the `.blend` and `.glb`; seven external PNG source/engine maps in `Textures`.
The studio floor, cameras and lights are excluded from FBX/GLB exports.

Authoring: run `python tools/MakeI75Stencils.py`, then portable Blender4.5.14LTS
with `--background --factory-startup --threads 10 --python-exit-code 1
--python tools/TextureIndoorDroneI75.py` from the repository root.

Full documentation: `docs/Innendrohne-I75-Textur-R01-2026-10-26.md`.
Preview images: `docs/Art/Drohnen/I75-Textur-R01-2026-10-26/`.
This preview has not replaced the existing Unity traffic prefab or build.
