"""Wrong identities reject before optional decoder imports, archive lookup and receipt creation."""
import argparse,importlib.util,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--config',type=Path,required=True);p.add_argument('--work-root',type=Path,required=True);a=p.parse_args()
root=a.work_root.resolve();assert 'local-private' in root.parts and not root.exists();root.mkdir(parents=True)
exe=a.exe.read_bytes();cfg=a.config.read_bytes();tool=ROOT/'tools/python/verify_terrain_height_layer_order.py';results=[]
for label,original in [('Executable',exe),('Config',cfg)]:
 changed=bytearray(original);changed[0]^=1
 for name,data in [('mutated',changed),('truncated',original[:-1])]:
  case=label.lower()+'-'+name;inp=root/(case+'.bin');inp.write_bytes(data);receipt=root/(case+'-receipt.json')
  run=subprocess.run([sys.executable,str(tool),'--exe',str(inp if label=='Executable' else a.exe),'--config',str(inp if label=='Config' else a.config),'--packages',str(root/'absent-decoders'),'--asset-root',str(root/'absent-assets'),'--receipt',str(receipt)],capture_output=True,text=True)
  assert run.returncode!=0 and label+' identity mismatch; rejected before decoding' in run.stderr and not receipt.exists(),(case,run.stderr)
  results.append(dict(Case=case,RejectedBeforeDecoding=True,Exit=run.returncode))
spec=importlib.util.spec_from_file_location('identity_only_order',tool);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
# Explicit synthetic wrong bytes test the source/embedded-generator gates without reading another original occurrence.
for label,expected,size in [('Terrain source',m.SOURCE,1188446),('Generator',m.GENERATOR,139608)]:
 for case,data in [('wrong-bytes',bytes(size)),('wrong-size',bytes(size-1))]:
  try:m.identity(data,expected,label,size)
  except ValueError as e:assert str(e)==label+' identity mismatch; rejected before decoding'
  else:raise AssertionError('Source identity gate accepted '+case)
  results.append(dict(Case=label+'-'+case,RejectedBeforeDecoding=True,Synthetic=True))
(root/'negative.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps(dict(IdentityNegatives=len(results),Passed=True)))
