from pathlib import Path
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/Pruefungen/Adaptive-Innenbeleuchtung-2026-10-26';OUT.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
tests={}
for name,folder in [('lighting','AdaptiveLighting'),('power','StationPowerConsole')]:
    p=ROOT/f'Logs/{folder}/result.json';data=json.loads(p.read_text());assert data['passed'];tests[name]=data;shutil.copy2(p,OUT/f'{name}-result.json')
for p in (ROOT/'Logs/AdaptiveLighting').glob('*.png'):shutil.copy2(p,OUT/p.name)
files={p.relative_to(ROOT/'Builds/AdaptiveLighting').as_posix():sha(p) for p in (ROOT/'Builds/AdaptiveLighting').rglob('*') if p.is_file()}
for rel,digest in files.items():assert sha(ROOT/'Builds/Windows'/rel)==digest,rel
(OUT/'player-hashes.json').write_text(json.dumps(files,indent=2))
plans=json.loads((ROOT/'Logs/adaptive-lighting-user-plans-before.json').read_text(encoding='utf-8-sig'))
for p in plans:assert sha(Path(p['Path'])).upper()==p['Hash'],p['Path']
sources={}
for rel in ['Assets/SpaceMiner/Scripts/StationOperations.Lighting.cs','Assets/SpaceMiner/Scripts/StationOperations.Geometry.cs','Assets/SpaceMiner/Scripts/StationOperations.Ui.cs','Assets/SpaceMiner/Scripts/StationOperations.cs','Assets/SpaceMiner/Scripts/StationOperations.PowerTelemetry.cs','Assets/SpaceMiner/Scripts/AdaptiveLightingCheck.cs','Assets/SpaceMiner/Editor/AdaptiveLightingBuild.cs','Assets/SpaceMiner/Scripts/PlayerSettings.cs']:
    sources[rel]=sha(ROOT/rel)
(OUT/'source-hashes.json').write_text(json.dumps(sources,indent=2))
report={'date':'2026-10-26','unity':'6000.6.5f1','developmentBuildExit':0,'buildLog':'Logs/adaptive-lighting-verified-retry-build.log','testLogs':['Logs/adaptive-lighting-verified-check.log','Logs/adaptive-lighting-verified-power-check.log'],'totalChecks':sum(t['checks'] for t in tests.values()),'tests':tests,'playerFilesVerified':len(files),'userRoomPlansHashUnchanged':len(plans),'deployment':'Builds/Windows','previousPlayer':'Builds/Windows-before-adaptive-lighting-20261026','lightingBudget':'Prototype split of existing180W systems allowance:90W other systems +90W maximum lighting, not an engineered fixture specification','failuresPreserved':['First lighting flag blocked by existing Live gate','Early active rendering too dim, raised before final build','Power-off render update delayed; TogglePower now refreshes lights synchronously','One Unity batch launch ended with1 before import; retry succeeded'],'limits':['No physical lux/watt, FPS, headset, Release or natural full-campaign acceptance','Fixed radial occupancy zones, not automatic wall/door or drone occupancy detection'],'noCommitPush':True}
(OUT.parent/'Adaptive-Innenbeleuchtung-2026-10-26.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'checks':report['totalChecks'],'playerFiles':len(files),'roomPlansUnchanged':len(plans)}))
