# I75 V02 — Surface R02

Second Blender variant: harder panel normals, matte paint, worn steel hardware,
four thin corner-angle sets (eight plates), two rails and eight hex fasteners.
Original V02 body positions retained. Total10983 polygons /18912 triangles.
Dimensions0.482m wide /0.750m long /0.4371m high. One mesh and one material.
Blender coordinates: Z up, front−Y. Four runtime image textures packed in the
`.blend` and `.glb`; seven2048² source/engine maps in `Textures`.

Authoring: portable Blender4.5.14LTS, `--background --factory-startup --threads 10
--python-exit-code 1 --python tools/TextureIndoorDroneI75R02.py` from repository
root. Reads the R01
stencil source. The floor, lights and camera are excluded from FBX/GLB exports.

Documentation: `docs/Innendrohne-I75-Textur-R02-2026-10-26.md`.
Preview only; the live Unity prefab and game build have not been replaced.
