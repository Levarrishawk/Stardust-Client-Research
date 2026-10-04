"""Reject wrong input identity before decoding or fixture/receipt creation."""
import argparse,json,subprocess,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--work-root',type=Path,required=True);a=p.parse_args()
root=a.work_root.resolve();assert 'local-private' in root.parts and not root.exists();root.mkdir(parents=True)
raw=a.exe.read_bytes();changed=bytearray(raw);changed[0]^=1;results=[]
for name,data in [('wrong-identity',changed),('truncated',raw[:-1])]:
 inp=root/(name+'.bin');inp.write_bytes(data);fixture=root/(name+'-fixtures.json');receipt=root/(name+'-receipt.json')
 run=subprocess.run([sys.executable,str(Path(__file__).with_name('verify_layer.py')),'--exe',str(inp),'--packages',str(root/'absent-decoder-packages'),'--fixtures',str(fixture),'--receipt',str(receipt)],capture_output=True,text=True)
 assert run.returncode!=0 and 'Executable identity mismatch; rejected before decoding' in run.stderr and not fixture.exists() and not receipt.exists()
 results.append(dict(Case=name,Exit=run.returncode,RejectedBeforeDecode=True))
(root/'negative.json').write_text(json.dumps(results,indent=2));print(json.dumps(results))

