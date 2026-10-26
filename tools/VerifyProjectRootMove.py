"""Verify the relocated checkout before intentional documentation updates."""
from pathlib import Path
import hashlib,json,subprocess,sys

root=Path(sys.argv[1]).resolve()
assert root==Path(r'C:\temp\SpaceMinerCodex').resolve(), 'Expected new physical root'
assert Path(r'C:\Users\achim.dannecker\source\repos\SpaceMinerCodex').resolve()==root, 'Compatibility path does not resolve to new root'
folder=root/'Logs/ProjectRoot-20261026'
before=json.loads((folder/'before.json').read_text(encoding='utf-8-sig'))
bad=[name for name,h in before['files'].items() if not (root/name).is_file()
     or hashlib.sha256((root/name).read_bytes()).hexdigest().upper()!=h]
assert not bad, f'File verification failed: {bad[:10]}'
def git(*args): return subprocess.check_output(['git','-C',str(root),*args]).decode('utf-8')
assert git('rev-parse','HEAD').strip()==before['head'], 'HEAD changed'
assert git('status','--porcelain=v1','--untracked-files=all','-z')==before['status'], 'Working tree changed'
subprocess.run(['git','-C',str(root),'fsck','--connectivity-only'],check=True,stdout=subprocess.DEVNULL)
report={'passed':True,'physicalRoot':str(root),'filesSha256Identical':len(before['files']),
        'head':before['head'],'branch':before['branch'],'workingTreePreserved':True,
        'gitConnectivityPassed':True,'scope':'Before subsequent explicit root documentation updates; ignored folders included in directory rename.'}
(folder/'after.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
