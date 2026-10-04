"""Early-identity rejection with absent decoder packages and absent output roots."""
import argparse,json,subprocess,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--work-root',type=Path,required=True);args=p.parse_args()
root=Path(__file__).resolve().parents[3];args.work_root.mkdir(parents=True,exist_ok=False);data=args.exe.read_bytes();changed=bytearray(data);changed[len(data)//2]^=1
cases={'mutated':bytes(changed),'truncated':data[:-1],'appended':data+b'\0','empty':b''};result=[]
for name,content in cases.items():
 candidate=args.work_root/(name+'.bin');candidate.write_bytes(content);output=args.work_root/(name+'-output')
 run=subprocess.run([sys.executable,str(root/'tools/python/verify_terrain_fractal_sampler.py'),'--exe',str(candidate),'--packages',str(args.work_root/'absent-packages'),'--run-root',str(output)],capture_output=True,text=True)
 assert run.returncode!=0 and 'Executable identity mismatch; rejected before decoding' in run.stderr and not output.exists(),(name,run.stdout,run.stderr)
 result.append(dict(Case=name,RejectedBeforeDecoding=True,OutputAbsent=True))
(args.work_root/'negative.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(IdentityNegatives=len(result),Passed=True)))
