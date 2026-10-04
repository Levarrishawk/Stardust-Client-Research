"""Explicit captured speed -> actual planar ticks, with independent original authored inputs."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
old=load('movement_join_original','poc/round47-tests/Integration/movement_input_join_peer.py')
wire=load('movement_join_wires','poc/round15-tests/Networking/peer_model.py')
def f32(v):return old.scalar(old.word(v))
def generate(original):
 profile=old.original(original);apps=wire.produce()['Applications'];oid=wire.H
 create=struct.pack('<HIQ7IIB',5,0xfe89ddea,oid,0,0,0,0x3f800000,0,0,0,2937962913,0).hex().upper()
 states=[[],[(7,old.word(2)),(11,old.word(1))],[(7,0),(11,0x80000000)],[(7,old.word(-1)),(11,0x7fc00001)]]
 def received(items):
  body=b''.join(struct.pack('<HI',i,b) for i,b in items)
  return (struct.pack('<HIQIBIH',5,0x12862153,oid,0x4352454f,4,len(body)+2,len(items))+body).hex().upper()
 cases=[]
 for source in ['AuthoredSpeed0','AuthoredSpeed1','ReceivedField7','ReceivedField11']:
  for state,items in enumerate(states):
   for db in [[old.word(1),0],[0,old.word(-1)],[old.word(.6),old.word(.8)]]:
    for partition in [5000,17000,30000]:
     cases.append(dict(Name=f'{source}-state{state}-d{db}-p{partition}',Source=source,ReceivedBits={str(i):b for i,b in items},ReceivedWire=received(items) if items else None,DirectionBits=db,Partition=partition))
 return dict(Version=1,SyntheticStateAndClock=True,OriginalAuthoredOnly=True,OriginalEffectiveValues=False,AuthoredInputs=profile,Applications=dict(Start=apps['start'],Create=create,Baseline=apps['humanBaseline'],End=apps['humanEnd']),ChangedSpeedWire=received([(7,old.word(9)),(11,old.word(9))]),Cases=cases)
def oracle(c,profile):
 src=c['Source'];authored=src.startswith('Authored');slot='speed[0]' if src=='AuthoredSpeed0' else 'speed[1]';field='7' if src=='ReceivedField7' else '11'
 bits=profile[slot]['Bits'] if authored else c['ReceivedBits'].get(field)
 accepted=old.finite(bits) and old.scalar(bits)>=0
 velocity=old.velocity(bits,c['DirectionBits']) if accepted else [0,0,0]
 if accepted and any(abs(old.scalar(b))>10 for b in velocity):accepted=False;velocity=[0,0,0]
 pos=[0.,0.,0.]
 for _ in range(10):pos=[f32(a+f32(old.scalar(v)*f32(.01))) for a,v in zip(pos,velocity)]
 return dict(Name=c['Name'],Accepted=bool(accepted),CapturedBits=bits if accepted else None,VelocityBits=velocity if accepted else None,PositionBits=[old.word(x) for x in pos],Timestamp=100000,Dropped=0,Fault=None,SourceKind=src,AuthoredSourcePath=profile[slot]['SourcePath'] if authored and accepted else None,CapturedPolicy=True)
if __name__=='__main__':
 action,original,fixture=sys.argv[1:4];d=generate(original);e=[oracle(c,d['AuthoredInputs']) for c in d['Cases']]
 if action=='generate':Path(fixture).write_text(json.dumps(d,indent=2)+'\n');print(json.dumps(dict(Cases=len(e),Accepted=sum(r['Accepted'] for r in e),Rejected=sum(not r['Accepted'] for r in e))))
 elif action=='verify':
  assert json.loads(Path(fixture).read_text())==d
  actual=json.loads(Path(sys.argv[4]).read_text());assert actual==e,'Explicit motion composition differs'
  r=dict(Passed=True,Cases=len(e),Accepted=sum(x['Accepted'] for x in e),Rejected=sum(not x['Accepted'] for x in e),TicksPerCase=10,CapturedVelocity=True,NoFallback=True,OriginalEffectiveValues=False)
  Path(sys.argv[5]).write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
 else:raise ValueError('generate/verify')
