"""Identity/truncation negative cases for the hash-first static verifier."""
import argparse,json,subprocess,sys
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--exe',required=True);ap.add_argument('--work-root',required=True);args=ap.parse_args()
repo=Path(__file__).resolve().parents[3];work=Path(args.work_root).resolve();assert work.is_relative_to((repo/'local-private').resolve())
work.mkdir(parents=True,exist_ok=True);source=Path(args.exe).read_bytes();assert len(source)>1
cases={'wrong-identity':bytes([source[0]^1])+source[1:],'truncated':source[:-1]};results=[]
for name,blob in cases.items():
 path=work/(name+'.bin');path.write_bytes(blob);receipt=work/(name+'-receipt.json');assert not receipt.exists()
 p=subprocess.run([sys.executable,str(Path(__file__).with_name('verify_circle.py')),'--exe',str(path),'--packages','deliberately-unavailable','--receipt',str(receipt),'--fixtures',str(work/(name+'-fixtures.json'))],capture_output=True,text=True)
 assert p.returncode!=0 and p.stderr.strip()=='Executable identity mismatch; decoding not started.' and not receipt.exists(),(name,p.returncode,p.stderr)
 results.append({'case':name,'exit':p.returncode,'rejectedBeforeDecode':True})
(work/'negative.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps(results))
