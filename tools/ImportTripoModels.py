"""Verify and import the two user-provided Tripo exports; originals stay untouched."""
import hashlib
import argparse
import io
import json
from pathlib import Path
import shutil
import zipfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]

def digest(data):
    return hashlib.sha256(data).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--drone", type=Path, default=ROOT / "Imports/Tripo/industrial+maintenance+drone+3d+model.zip")
    parser.add_argument("--cradle", type=Path, default=ROOT / "Imports/Tripo/drone+charging+cradle+3d+model.zip")
    args = parser.parse_args()
    models = {"MaintenanceDrone": args.drone, "ChargingCradle": args.cradle}
    report = {"date": "2026-10-11", "archiveCheckPassed": False, "models": []}
    for name, source in models.items():
        record = {"name": name, "sourceArchive": str(source), "archiveBytes": source.stat().st_size,
                  "sha256": digest(source.read_bytes()), "entries": []}
        destination = ROOT / "Assets/SpaceMiner/Art/Tripo" / name
        with zipfile.ZipFile(source) as archive:
            assert archive.testzip() is None, "Archive CRC failure"
            assert len(archive.infolist()) <= 30, "Unexpected export contents"
            file_data = []
            for entry in archive.infolist():
                if entry.is_dir():
                    continue
                target = (destination / entry.filename).resolve()
                assert target.is_relative_to(destination.resolve()), "Unsafe archive path"
                suffix = target.suffix.lower()
                assert suffix in (".fbx", ".jpeg", ".jpg", ".png"), "Unexpected file type"
                assert entry.file_size < 100_000_000, "Unexpectedly large entry"
                data = archive.read(entry)
                info = {"path": entry.filename, "bytes": len(data), "sha256": digest(data)}
                if suffix == ".fbx":
                    assert data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00"), "Not a binary FBX"
                    info["fbxVersion"] = int.from_bytes(data[23:27], "little")
                else:
                    with Image.open(io.BytesIO(data)) as texture:
                        info["dimensions"] = list(texture.size)
                        info["mode"] = texture.mode
                        texture.verify()
                    assert info["dimensions"] == [4096, 4096], "Expected 4K textures"
                record["entries"].append(info)
                file_data.append((target, data))
            assert sum(p.suffix.lower() == ".fbx" for p, _ in file_data) == 1
            for texture_type in ("basecolor", "normal", "metallic", "roughness", "rm"):
                assert any(texture_type + "." in p.name for p, _ in file_data), texture_type
            for target, data in file_data:
                if target.exists():
                    assert target.read_bytes() == data, "Refusing to overwrite different existing file"
                else:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(data)
        backup = ROOT / "Imports/Tripo" / source.name
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists():
            shutil.copy2(source, backup)
        assert digest(backup.read_bytes()) == record["sha256"]
        record["assetDirectory"] = destination.relative_to(ROOT).as_posix()
        report["models"].append(record)
    report["archiveCheckPassed"] = True
    path = ROOT / "docs/Pruefungen/Tripo-Archive-2026-10-11.json"
    path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"passed": True, "models": [{"name": m["name"], "files": len(m["entries"]),
          "fbxVersion": next(e["fbxVersion"] for e in m["entries"] if "fbxVersion" in e)} for m in report["models"]]}))

if __name__ == "__main__":
    main()
