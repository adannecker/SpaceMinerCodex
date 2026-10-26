"""Snapshot project files and Git before a same-volume directory relocation."""
from pathlib import Path
import hashlib,json,subprocess

root=Path(__file__).resolve().parents[1]
def git(*args): return subprocess.check_output(['git','-C',str(root),*args])
status=git('status','--porcelain=v1','--untracked-files=all','-z').decode('utf-8')
names=sorted(set(p for p in git('ls-files','--cached','--others','--exclude-standard','-z').decode('utf-8').split('\0') if p))
files={}
for name in names:
    path=root/name
    if path.is_file(): files[name]=hashlib.sha256(path.read_bytes()).hexdigest().upper()
for path in (root/'.git').rglob('*'):
    if path.is_file(): files[path.relative_to(root).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest().upper()
report={'source':str(root),'target':r'C:\temp\SpaceMinerCodex','head':git('rev-parse','HEAD').decode().strip(),
        'branch':git('branch','--show-current').decode().strip(),'status':status,'files':files,
        'scope':'Tracked/untracked non-ignored project files and complete .git. Ignored builds/caches move with the same physical directory; no copy/delete.'}
folder=root/'Logs/ProjectRoot-20261026'; folder.mkdir(exist_ok=True)
(folder/'before.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'{len(files)} project/Git files SHA256 recorded; HEAD {report["head"]}')
