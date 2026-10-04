"""Explicit authored versus received selection peer; no fallback/default or original effective-value claim."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('movement_join_received',ROOT/'poc/round46-tests/Integration/creature4_partial_host_peer.py');received=importlib.util.module_from_spec(spec);spec.loader.exec_module(received)
FIELDS=[0,1,4,5,7,8,9,10,11,12]
SLOTS=['speed[0]','speed[1]','slopeModAngle','slopeModPercent','waterModPercent']
def scalar(bits):return struct.unpack('<f',struct.pack('<I',bits))[0]
def word(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def finite(bits):return bits is not None and bits&0x7f800000!=0x7f800000
def original(oracle):
    d=json.loads(Path(oracle).read_text());assert d['SourceBytes']==8290 and len(d['Chain'])==5
    values={}
    for name in SLOTS:
      value=d['Results'][name];p=value['Parameter'];raw=bytes.fromhex(p['RawHex']);assert raw[:2]==b'\1 ' and len(raw)==6
      bits=struct.unpack('<I',raw[2:])[0];assert finite(bits)
      values[name]=dict(Bits=bits,SourcePath=value['SourcePath'])
    return values

def generate(oracle):
    profile=original(oracle);base=received.generate();states=[base['Seed']]+base['Steps'];cases=[]
    directions=[[word(1),0],[0,word(-1)],[word(.6),word(.8)]]
    for phase in ['PendingBaselines','Finalized']:
      for i,state in enumerate(states):
       for index,slot in [(7,'speed[0]'),(11,'speed[1]')]:
        for direction in directions:
          # Raw wire is explicitly relabelled to this synthetic world's object1.
          data=bytearray.fromhex(state['Hex']);struct.pack_into('<Q',data,6,1)
          cases.append(dict(Name=f'{phase}-{i}-{index}-{direction}',Phase=phase,StateIndex=i,Field=index,AuthoredSlot=slot,DirectionBits=direction,Wire=data.hex().upper(),PrefixWires=[(bytes.fromhex(previous['Hex'])[:6]+struct.pack('<Q',1)+bytes.fromhex(previous['Hex'])[14:]).hex().upper() for previous in states[:i+1]],KnownFields=state['Fields']))
    return dict(Version=1,Synthetic=True,OriginalAuthoredOnly=True,OriginalEffectiveValues=False,AuthoredInputs=profile,Cases=cases)

def velocity(bits,direction):return [word(scalar(direction[0])*scalar(bits)),0,word(scalar(direction[1])*scalar(bits))]
def oracle(c,profile):
    known={x['Index']:x for x in c['KnownFields']};bits=known.get(c['Field'],{}).get('Bits');f=finite(bits)
    ready=c['Phase']=='Finalized' and f and scalar(bits)>=0
    authored=profile[c['AuthoredSlot']];ab=authored['Bits'];assert finite(ab) and scalar(ab)>=0
    raw=[dict(Index=index,Bits=known.get(index,{}).get('Bits'),Provenance=known.get(index,{}).get('Provenance')) for index in FIELDS]
    return dict(Name=c['Name'],Phase=c['Phase'],ReceivedFields=raw,ReceivedBits=bits,ReceivedReady=bool(ready),ReceivedVelocityBits=velocity(bits,c['DirectionBits']) if ready else None,AuthoredBits=ab,AuthoredSourcePath=authored['SourcePath'],AuthoredSelected=True,AuthoredVelocityBits=velocity(ab,c['DirectionBits']),AuthoredPhaseEligible=c['Phase']=='Finalized')

def verify(fixture,native,original_oracle,receipt):
    d=json.loads(Path(fixture).read_text());assert d==generate(original_oracle);rows=json.loads(Path(native).read_text());assert len(rows)==len(d['Cases'])
    for c,r in zip(d['Cases'],rows):assert r==oracle(c,d['AuthoredInputs']),(c['Name'],r,oracle(c,d['AuthoredInputs']))
    missing=sum(r['ReceivedBits'] is None for r in rows);pending=sum(r['Phase']=='PendingBaselines' and not r['ReceivedReady'] for r in rows)
    assert missing>0 and pending==36
    result=dict(Passed=True,Cases=len(rows),MissingReceivedSpeedCases=missing,PendingNonOperationalCases=pending,AuthoredSources=5,OriginalAuthoredOnly=True,NoDefaultSeeds=True,NoReceivedFallback=True,OriginalEffectiveValues=False)
    Path(receipt).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':
    if sys.argv[1]=='generate':
      d=generate(sys.argv[2]);Path(sys.argv[3]).write_text(json.dumps(d,indent=2)+'\n');print(json.dumps(dict(Cases=len(d['Cases']),OriginalAuthoredOnly=True)))
    else:verify(*sys.argv[2:6])
