"""Real private-file/junction negatives for round-copy preflight and exact-byte guards."""
import contextlib,importlib.util,io,json,subprocess,sys
from pathlib import Path
from unittest.mock import patch

repo=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('round_copy_guard',repo/'tools/python/parallel_round_workspace.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
root=Path(sys.argv[1]).resolve()
assert root.is_relative_to(repo/'local-private') and not root.exists(),'Fresh private test root required'
root.mkdir(parents=True);checks=0
def reject(operation):
    global checks
    try:operation()
    except ValueError:checks+=1
    else:raise AssertionError('Required preflight rejection absent')
for invalid in ['../x','a/../x','/x','C:/x','a\\b','.git/config','.GIT/config','a//b','a/./b','']:
    reject(lambda relative=invalid:m.safe(root,relative))
with patch.object(m.subprocess,'check_output',return_value=b'A.cs\0a.cs\0'):
    reject(lambda:m.listed(root))

def prepare(name):
    directory=root/name;main=directory/'main';worker=directory/'worker';baseline=directory/'baseline'
    for target in [main/'late',worker/'late',baseline]:target.mkdir(parents=True)
    rows=[]
    for relative in ['a.cs','late/b.cs']:
        (main/relative).write_bytes(b'old');(worker/relative).write_bytes(b'new')
        rows.append(dict(path=relative,sha256=m.digest(b'old'),bytes=3))
    (baseline/'baseline.json').write_text(json.dumps(dict(head='synthetic',files=rows)))
    allowed=directory/'allowed.json';allowed.write_text(json.dumps(['a.cs','late/b.cs']))
    return main,worker,baseline,allowed
def integrate(parts):
    main,worker,baseline,allowed=parts
    args=['guard','integrate','--root',str(main),'--worker',str(worker),'--baseline',str(baseline),'--allowed',str(allowed)]
    with patch.object(sys,'argv',args),patch.object(m.subprocess,'check_output',return_value=b'a.cs\0late/b.cs\0'),contextlib.redirect_stdout(io.StringIO()):m.main()

positive=prepare('positive');integrate(positive)
assert all((positive[0]/relative).read_bytes()==b'new' for relative in ['a.cs','late/b.cs']);checks+=1
conflict=prepare('late-conflict');(conflict[0]/'late/b.cs').write_bytes(b'other')
reject(lambda:integrate(conflict));assert (conflict[0]/'a.cs').read_bytes()==b'old';checks+=1

source=prepare('source-change');real_safe=m.safe;source_calls=0
def changed_source(base,relative):
    global source_calls
    result=real_safe(base,relative)
    if base==source[1] and relative=='a.cs':
        source_calls+=1
        if source_calls==2:result.write_bytes(b'changed before copy')
    return result
with patch.object(m,'safe',side_effect=changed_source):reject(lambda:integrate(source))
assert (source[0]/'a.cs').read_bytes()==b'old' and (source[0]/'late/b.cs').read_bytes()==b'old';checks+=1

# A real NTFS junction stays inside the synthetic root; no original files or Git repository are touched.
junction=prepare('late-junction');main=junction[0];(main/'actual').mkdir();(main/'actual/b.cs').write_bytes(b'old')
(main/'late/b.cs').unlink();(main/'late').rmdir()  # Exact synthetic files/empty directory only, not recursive.
quote=lambda value:str(value).replace("'","''")
command=f"New-Item -ItemType Junction -Path '{quote(main/'late')}' -Target '{quote(main/'actual')}' | Out-Null"
subprocess.run(['pwsh','-NoProfile','-Command',command],check=True,capture_output=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
reject(lambda:m.safe(main,'late/b.cs'));reject(lambda:m.safe(main/'late','b.cs'));reject(lambda:integrate(junction))
assert (main/'a.cs').read_bytes()==b'old' and (main/'actual/b.cs').read_bytes()==b'old';checks+=1
result=dict(Passed=True,Checks=checks,ActualWindowsJunction=True,BeforeFirstWrite=True,ExactSourceHash=True,PrivateSyntheticOnly=True)
(root/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
