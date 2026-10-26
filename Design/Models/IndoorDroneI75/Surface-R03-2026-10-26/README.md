# I75 R03 — sealed chassis and service/tool interface

New review revision from the unchanged R02 Blender source. Includes closed inner
chassis, belly/rear closures, side backing, restrained nine-material PBR finish,
geometric lettering and a central magnetic-tool mating face. Original decorative
skin remains separate; its remaining non-manifold intersections are documented.

Blender axes: X width, Z up, front -Y. Envelope 0.482 x 0.783 x 0.437103m,
including the new front coupling. 87564 triangles, 60 mesh objects, 9 materials.
No bitmap lettering or inherited noise/emissive texture maps. Native .blend,
FBX and embedded GLB are supplied; existing game prefab is not changed.

Generate with Blender4.5.14 and `tools/RepairIndoorDroneI75R03.py` from repo root.
Optional `-- --skip-render`. Each run recreates this R03 revision only.
Details: `docs/Innendrohne-I75-R03-2026-10-26.md`.
