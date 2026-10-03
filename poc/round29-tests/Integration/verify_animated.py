"""Independent supplied-time/main-host animation oracle; original scheduler parity unclaimed."""
import argparse,json,struct,subprocess,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('log',type=Path);p.add_argument('frame_native',type=Path);p.add_argument('tangible_stage',type=Path);p.add_argument('named_peer',type=Path);a=p.parse_args()
lines=a.log.read_text(encoding='utf-8-sig').splitlines()
def records(tag):return [json.loads(line.split('=',1)[1]) for line in lines if line.startswith(tag+'=')]
frame_lines=a.frame_native.read_text(encoding='utf-8-sig').splitlines()
bundle=json.loads(next(line.split('=',1)[1] for line in frame_lines if line.startswith('frame_bundle_original=')))['Bundle']
assert bundle['FrameCount']==8 and bundle['RawDtstWord1']==0x3dcccccd
resources=records('terminal_animation_resource');assert len(resources)==2
for r in resources:
 assert r['ExternalReads']==34 and r['SourceBytes']==1479410 and r['ReadsOnce'] and r['ArchivesClosed']
 assert r['BundleSources']==10 and r['BundleDecodedBytes']==bundle['DecodedBytes']==349536 and r['BundleMips']==64
 assert r['RetainedDecodedBytes']==2490372 and r['PendingWeight']==3969782
 assert r['DefaultFrame0DecodedSeparately'] is True
rows=records('terminal_animation_state');names=['initial']+['frame'+str(i) for i in range(1,9)]+['multi','zero','reset','invalid_retained']
assert [r['Name'] for r in rows]==names
f32=lambda bits:struct.unpack('<f',struct.pack('<I',bits))[0]
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
interval=f32(bundle['RawDtstWord1']);index=0;residual=0.0;ids={};structure=rows[0]['Structure'];pose=rows[0]['Pose'];publication=rows[0]['Publication'];revision=rows[0]['Revision'];advances=rows[0]['Advances']
for n,row in enumerate(rows):
 if row['Name']=='reset':index=0;residual=0.0;revision+=1
 elif n and row['Name']!='invalid_retained':
  delta=f32(row['DeltaBits']);remaining=residual+delta;steps=0
  assert delta>=0
  while remaining>=interval:remaining-=interval;steps+=1;assert steps<=4096
  index=(index+steps)%8;residual=f32(bits(remaining));revision+=1;advances+=steps
 assert row['Index']==index and row['ResidualBits']==bits(residual) and row['Revision']==revision and row['Advances']==advances
 path=bundle['Frames'][index]['TexturePath'];expected=bundle['Images'][path]
 assert row['Path']==path and row['MipHashes']==[m['Sha256'] for m in expected['Mips']]
 assert row['Structure']==structure and row['Pose']==pose and row['Publication']==publication
 assert row['Calls']==3 and row['Closed']==3 and row['Copies']==8 and row['Uploads']==8 and row['ScreenBindings']==2
 assert row['GpuVerified']==row['Gpu'] and row['CaptionRetained'] and row['TypedStateCurrent']
 assert row['NumericPolicy']=='binary64-intermediate-float32-persistent' and row['OriginalParity'] is False
 if index in ids:assert ids[index]==row['TextureId']
 else:ids[index]=row['TextureId']
assert len(ids)==8 and len(set(ids.values()))==8
cleanup=records('terminal_animation_cleanup');assert len(cleanup)==1 and cleanup[0]['Retired'] and cleanup[0]['CachesReleased'] and cleanup[0]['Copies']==8 and cleanup[0]['Uploads']==8
assert any(line=='PASS: pumped terminal_animation=true frames=8 bounded=true no_update_io=true drained=true' for line in lines)
subprocess.run([sys.executable,str(Path(__file__).resolve().parents[2]/'round27-tests/Integration/verify_tangible.py'),str(a.log),str(a.tangible_stage),str(a.named_peer)],check=True)
print('PASS main animated terminal:13 supplied-time states/all8mip identities/retained geometry+typed caption/no update I/O/cleanup; modern policy')
