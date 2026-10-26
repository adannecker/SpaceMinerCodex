"""Record actual local acceptance and verify copied player, no Unity mutation."""
from pathlib import Path
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/Pruefungen/Touchpulte-R04-Spielintegration-2026-10-26';OUT.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
tests={}
for name,folder in [('consoles','TouchConsole'),('power','StationPowerConsole'),('planner','CommandRoomPlannerCheck')]:
    p=ROOT/f'Logs/{folder}/result.json';data=json.loads(p.read_text());assert data['passed'];tests[name]={'checks':data['checks'],'passed':True};shutil.copy2(p,OUT/f'{name}-result.json')
for p in (ROOT/'Logs/TouchConsole').glob('*.png'):shutil.copy2(p,OUT/p.name)
files={p.relative_to(ROOT/'Builds/TouchConsoleR04').as_posix():sha(p) for p in (ROOT/'Builds/TouchConsoleR04').rglob('*') if p.is_file()}
for rel,digest in files.items():assert sha(ROOT/'Builds/Windows'/rel)==digest,rel
(OUT/'player-hashes.json').write_text(json.dumps(files,indent=2))
sources={}
for p in list((ROOT/'Assets/SpaceMiner/Scripts').glob('TouchConsole*'))+list((ROOT/'Assets/SpaceMiner/Editor').glob('TouchConsole*'))+list((ROOT/'Assets/SpaceMiner/Resources/TouchConsoleR04').rglob('*')):
    if p.is_file():sources[p.relative_to(ROOT).as_posix()]=sha(p)
for rel in ['Assets/SpaceMiner/Scripts/StationOperations.Geometry.cs','Assets/SpaceMiner/Scripts/StationOperations.RoomPlan.cs','Assets/SpaceMiner/Scripts/PlayerSettings.cs','Assets/SpaceMiner/Scripts/CommandRoomPlannerCheck.cs','Assets/SpaceMiner/Art/TouchConsoleR04/geometry.json','tools/ExportTouchConsoleRuntime.py']:
    sources[rel]=sha(ROOT/rel)
(OUT/'source-hashes.json').write_text(json.dumps(sources,indent=2))
report={'date':'2026-10-26','unity':'6000.6.5f1','build':'Development, existing AsteroidBelt scene, no Setup','buildLog':'Logs/console-r04-game-acceptance-build.log','buildExit':0,'tests':tests,'totalChecks':sum(v['checks'] for v in tests.values()),'playerFilesVerified':len(files),'deployment':'Builds/Windows','previousPlayer':'Builds/Windows-before-touch-console-r04-20261026','model':'R04 clean housing, 23772 triangles, 3 mesh/material groups','widthM':{'standard':1.5,'main':4.4},'limitations':['No Release/headset/FPS or natural full campaign acceptance','World screens show compact genuine summaries, not complete duplicated interactive UI','Old plan reservation sizes retained; physical hardware dimensions fixed','Early technically passing screenshot run had giant text; corrected and rebuilt before final acceptance'],'picturesVisuallyChecked':['main.png','Power.png'],'noCommitPush':True}
(OUT.parent/'Touchpulte-R04-Spielintegration-2026-10-26.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'checks':report['totalChecks'],'playerFiles':len(files)}))
