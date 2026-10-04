"""Original-resource sourced host: independent authored, received, and binary32 motion checks."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
movement=load('sourced_host_original','poc/round47-tests/Integration/movement_input_join_peer.py')
partial=load('sourced_host_partial','poc/round46-tests/Integration/creature4_partial_host_peer.py')
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def verify(original,partialfixture,mixedfixture,log,source,receipt):
 authored=movement.original(original);fixture=json.loads(Path(partialfixture).read_text(encoding='utf-8'));assert fixture==partial.generate()
 create=bytes.fromhex(json.loads(Path(mixedfixture).read_text(encoding='utf-8'))['human_create'])
 pose=struct.unpack_from('<7f',create,14);start=[pose[4],pose[6],pose[5]]
 received={r['Index']:r['Bits'] for r in fixture['Steps'][-1]['Fields']}
 index={'AuthoredSpeed0':7,'AuthoredSpeed1':11,'ReceivedField7':7,'ReceivedField11':11,'CurrentField7':7,'CurrentField11':11}[source]
 slot='speed[0]' if index==7 else 'speed[1]'
 speedbits=authored[slot]['Bits'] if source.startswith('Authored') else received[index]
 assert movement.finite(speedbits) and 0<=movement.scalar(speedbits)<=10,'This positive host proof requires an explicitly eligible selected source'
 speed=movement.scalar(speedbits);text=Path(log).read_text(encoding='utf-8-sig')
 assert 'PASS: pumped sourced_input=true explicit_source=true original_resource_chain=true stopped=true no_update_io=true drained=true' in text
 assert 'pump_failure_cleanup=' not in text
 rows=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('sourced_input_state=')]
 assert [r['Name'] for r in rows]==['moving1','moving2','moving3','released','focus_suspended','focus_resumed_idle']
 position=start.copy();positions=[]
 for _ in range(3):position=[position[0],position[1],f(position[2]+f(speed*f(.01)))];positions.append(position.copy())
 for i,r in enumerate(rows):
  assert r['Source']==source and r['RawSpeedBits']==speedbits and r['Start']==start
  if source.startswith('Current'):
   assert r['CurrentSourceCurrent']==(i<3) and r['CurrentOrigin']==('Received' if i<3 else None)
  expected=positions[min(i,2)]
  assert list(map(f,r['Native']))==expected,(i,r['Native'],expected)
  assert list(map(f,r['Engine']))==[expected[0],expected[1],-expected[2]]
  assert f(r['Speed'])==(speed if i<3 else 0) and r['Stop']==(i>=3) and not r['SourceUnavailable']
  assert r['Keys']==(1 if i<3 else 0) and r['Active']==(i!=4) and r['Ticks']==min(i+1,3)
  assert r['calls']==r['closed']==3 and r['PublicationRetained'] and r['SourceCount']==5
  assert r['RootPath']=='object/creature/player/shared_human_male.iff' and not r['OriginalEffectiveMovementParity']
 assert all(rows[i]['TimestampMicroseconds']-rows[i-1]['TimestampMicroseconds']==10000 for i in range(1,6))
 views=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('sourced_current_inputs=')];assert len(views)==1
 view=views[0];assert view['Current'] and view['OutputBytes']==1632 and view['Scope']=='CallerVerifiedConcreteClientCreatureRawInputs'
 assert view['RootSha256']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243' and not view['RawReceivedStoreSeeded'] and not view['MovementApplied']
 slots={7:'speed[0]',11:'speed[1]',8:'slopeModAngle',9:'slopeModPercent',12:'waterModPercent'}
 stores={0:0x0042F14C,1:0x0042F122,4:0x0042F093,5:0x0042F071,10:0x0042F176}
 assert [r['Index'] for r in view['Fields']]==[0,1,4,5,7,8,9,10,11,12]
 for row in view['Fields']:
  index=row['Index'];bits=received[index]
  assert row['RawBits']==bits and row['Origin']=='Received'
  assert row['Eligibility']==('NonFinite' if bits&0x7f800000==0x7f800000 else 'FiniteCurrent')
  assert row['AuthoredBits']==(authored[slots[index]]['Bits'] if index in slots else None)
  assert row['ConstructorLiteralStoreVA']==stores.get(index) and row['ConstructorLiteralBits']==(0x3f800000 if index in stores else None)
 result=dict(Passed=True,Source=source,States=6,CurrentFields=10,OriginalResourceSourceCount=5,OriginalRecipeChain=True,IndependentOriginalAndReceivedBits=True,NativeAndEnginePositionVerified=True,ExplicitStop=True,FocusNoCatchup=True,NoUpdateIO=True,OriginalEffectiveMovementParity=False)
 out=Path(receipt).resolve();assert out.is_relative_to(ROOT/'local-private');out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
if __name__=='__main__':verify(*sys.argv[1:7])
