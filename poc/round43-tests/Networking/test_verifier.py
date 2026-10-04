"""Fresh-process identity negatives; mutations remain in a new ignored private directory."""
import argparse,json,runpy,subprocess,sys,uuid
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--server-root',type=Path,required=True);p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--temp-root',type=Path,required=True);a=p.parse_args();repo=Path(__file__).resolve().parents[3];private=(repo/'local-private').resolve();assert a.temp_root.resolve().is_relative_to(private);work=a.temp_root/('identity-'+str(uuid.uuid4()));work.mkdir(parents=True);tool=repo/'tools/python/verify_creature_view4_delivery.py';pins=runpy.run_path(str(tool))['SERVER_PINS'];copy=work/'server';checks=0
for n in pins:
 target=copy/n;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((a.server_root/n).read_bytes())
def reject(exe,server,expected):
 global checks
 receipt=work/f'rejected-{checks}.json';r=subprocess.run([sys.executable,str(tool),'--server-root',str(server),'--exe',str(exe),'--packages',str(work/'missing-packages'),'--receipt',str(receipt)],capture_output=True,text=True)
 assert r.returncode!=0 and expected in r.stdout+r.stderr and not receipt.exists(),(r.returncode,r.stdout,r.stderr);checks+=1
wrong=work/'wrong.bin';wrong.write_bytes(b'');reject(wrong,a.server_root,'Wrong executable SHA256');wrong.write_bytes(b'MZ'+bytes(1024));reject(wrong,a.server_root,'Wrong executable SHA256')
for n in pins:
 target=copy/n;raw=target.read_bytes();target.write_bytes(raw+b'\n');reject(a.exe,copy,'Changed server source identity: '+n);target.write_bytes(raw)
reject(a.exe,work/'absent-server','No such file')
# A public receipt path rejects before any input identity/VA work, without writing it.
public=repo/'poc/round43-tests/Networking/rejected-public.json';assert not public.exists();r=subprocess.run([sys.executable,str(tool),'--server-root',str(a.server_root),'--exe',str(wrong),'--packages',str(a.packages),'--receipt',str(public)],capture_output=True,text=True);assert r.returncode!=0 and 'Receipt must remain under ignored local-private' in r.stderr and not public.exists();checks+=1
result=dict(Checks=checks,WrongExe=2,ChangedSourceFiles=len(pins),MissingRoot=1,PublicReceipt=1,NoRejectedReceiptWritten=True,AllRejectedBeforeOptionalPEImports=True);(a.temp_root/'identity-negatives.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result,indent=2))
