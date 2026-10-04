"""Mutated/truncated executable and config reject before decoder/archive/output."""
import argparse,json,subprocess,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--config',type=Path,required=True);p.add_argument('--work-root',type=Path,required=True);a=p.parse_args();root=a.work_root.resolve();assert 'local-private' in root.parts and not root.exists();root.mkdir(parents=True);results=[];tool=Path(__file__).resolve().parents[3]/'tools/python/verify_terrain_fractal_seed_noise.py'
for label,original in [('Executable',a.exe.read_bytes()),('Config',a.config.read_bytes())]:
 changed=bytearray(original);changed[0]^=1
 for name,data in [('mutated',changed),('truncated',original[:-1])]:
  case=label.lower()+'-'+name;inp=root/(case+'.bin');inp.write_bytes(data);output=root/(case+'-output')
  run=subprocess.run([sys.executable,str(tool),'--exe',str(inp if label=='Executable' else a.exe),'--config',str(inp if label=='Config' else a.config),'--packages',str(root/'absent-decoders'),'--asset-root',str(root/'absent-assets'),'--run-root',str(output)],capture_output=True,text=True)
  assert run.returncode!=0 and label+' identity mismatch; rejected before decoding' in run.stderr and not output.exists(),(case,run.stderr);results.append(dict(Case=case,RejectedBeforeDecoding=True,Exit=run.returncode))
(root/'negative.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps(dict(IdentityNegatives=len(results),Passed=True)))
