"""Fresh static replay plus external identity/output guards; no original execution or toolchain downloads."""
import argparse,json,os,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
TOOL=ROOT/'tools/python/verify_terrain_fractal_curve_finish.py'
p=argparse.ArgumentParser()
for k in ('exe','packages','asset-root','config','run-root'):p.add_argument('--'+k,type=Path,required=True)
a=p.parse_args()
sys.path.insert(0,str(TOOL.parent));import verify_terrain_fractal_curve_finish as v
out=v.private_root(a.run_root);out.mkdir(parents=True);env=dict(os.environ,PYTHONUTF8='1',PYTHONDONTWRITEBYTECODE='1')
def run(exe,root,packages):
 return subprocess.run([sys.executable,str(TOOL),'--exe',str(exe),'--packages',str(packages),'--asset-root',str(a.asset_root),'--config',str(a.config),'--run-root',str(root)],capture_output=True,text=True,encoding='utf-8',env=env)
positive=run(a.exe,out/'positive',a.packages);(out/'positive.log').write_text(positive.stdout+positive.stderr,encoding='utf-8');assert positive.returncode==0,positive.stderr
checks=[]
for i,data in enumerate((b'bad',a.exe.read_bytes()[:-1],bytes(64),b'')):
 f=out/f'wrong-{i}.bin';f.write_bytes(data);root=out/f'wrong-output-{i}';result=run(f,root,out/'absent-packages');assert result.returncode!=0 and 'Executable identity rejected before decoding' in result.stderr and not root.exists();checks.append('wrong-identity-'+str(i))
for name,root,message in [('nonprivate',ROOT/'docs/round56-rejected','Private output root required'),('existing',out/'positive','Fresh output root required')]:
 result=run(a.exe,root,out/'absent-packages');assert result.returncode!=0 and message in result.stderr;assert name=='existing' or not root.exists();checks.append(name)
target=out/'guard-target';target.mkdir();junction=out/'guard-junction'
def quote_ps(value):return "'"+str(value).replace("'","''")+"'"
command='New-Item -ItemType Junction -Path '+quote_ps(junction)+' -Target '+quote_ps(target)+' | Out-Null'
created=subprocess.run(['powershell.exe','-NoProfile','-NonInteractive','-Command',command],capture_output=True,text=True,encoding='utf-8',creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
assert created.returncode==0 and junction.lstat().st_file_attributes&0x400,'Own private junction fixture creation failed'
root=junction/'round56-rejected';result=run(a.exe,root,out/'absent-packages');assert result.returncode!=0 and 'Linked output ancestor rejected' in result.stderr and not root.exists();checks.append('linked-ancestor')
prior=(out/'positive/curve-receipt.json').read_bytes();result=run(a.exe,out/'positive',a.packages);assert result.returncode!=0 and (out/'positive/curve-receipt.json').read_bytes()==prior;checks.append('prior-output-preserved')
receipt=json.loads(prior);receipt.update(ExternalGuardChecks=len(checks),ExternalGuards=checks);(out/'research-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
