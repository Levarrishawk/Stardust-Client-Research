"""Verify bounded replacement logs against the independent original frame oracle."""
import argparse,json,re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--headless-log',type=Path,required=True);p.add_argument('--gpu-log',type=Path,required=True);p.add_argument('--oracle',type=Path,required=True);p.add_argument('--capture-prefix',type=Path);a=p.parse_args()
expected=json.loads(a.oracle.read_text(encoding='utf-8-sig'))
for path,gpu in [(a.headless_log,False),(a.gpu_log,True)]:
 lines=path.read_text(encoding='utf-8-sig').splitlines();frames=[];validation=None;started=False;certificate=0
 for n,line in enumerate(lines):
  if line.startswith('switch_frame='):started=True;frames.append(json.loads(line.split('=',1)[1]))
  elif line.startswith('switch_validation='):validation=json.loads(line.split('=',1)[1])
  elif re.match(r'\s*(ERROR:|WARNING:|SCRIPT ERROR:)',line):
   assert not started and line=='ERROR: Failed to read the root certificate store.' and certificate==0,(path,line)
   assert n+1<len(lines) and lines[n+1]=='   at: get_system_ca_certificates (platform/windows/os_windows.cpp:2582)';certificate+=1
 assert len(frames)==8 and [f['index'] for f in frames]==list(range(8))
 assert len({f['texture'] for f in frames})==8 and len({f['structure'] for f in frames})==1
 assert all(f['gpu']==gpu and f['pose']==frames[0]['pose'] for f in frames)
 for f,authored in zip(frames,expected['Frames']):
  assert f['path']==authored['TexturePath'] and f['mips']==[m['Sha256'] for m in expected['Images'][f['path']]['Mips']]
 assert validation and validation['frames']==8 and validation['lodSelections']==32 and validation['negatives']>=11
 for key,value in dict(uniqueUploads=8,imageCopies=8,uploadedBytes=349536,archiveReads=37,syntheticAliasUploads=2,standalonePreloads=True,modernSeconds=True,cursorsSynchronized=True,structureFixed=True,otherFixed=True,originalParity=False,preclaimFailure=True,engineFailureRetired=True,lateStaleRetired=True).items():assert validation[key]==value,(key,validation)
 assert any(line=='PASS: switch visual teardown archives_closed=true worker_observed=true wrapper_cache_released=true borrowed_inner_retired=true' for line in lines)
if a.capture_prefix:
 import struct,hashlib
 hashes=[]
 for frame in [0,3,7]:
  path=a.capture_prefix.with_name(a.capture_prefix.stem+f'-frame{frame}.png');raw=path.read_bytes();assert raw[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',raw[16:24])==(1280,720);hashes.append(hashlib.sha256(raw).digest())
 assert len(set(hashes))==3
print('PASS: CPU/GPU 8 frames x 8 independent mip hashes; strict diagnostics; identities/lifetime/cleanup; three captures')
