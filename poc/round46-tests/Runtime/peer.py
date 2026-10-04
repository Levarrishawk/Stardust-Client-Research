"""Independent received-only literal world model over frozen45 Python scalar/store oracle."""
import json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
BASE3='05000C5FA76888776655443322F14F4552430381000000000000000080FFFF03000000668000443322F102006BFF0300000000D84100FFDC040302FEFFFF0300000000FE800300000000000000FFFFFFFF00000080FFFFFFFF332211F0FFFFFFFF4433228110325476A5FFFE08070605040302FE0000E0BF443322F18796A5B4C3D2E1F00300000000000000FEFFFFFFFFFFFF7FFEFFFFFF'
BASE6='05000C5FA76889776655443322F14F455243067800000016007600000002000000FFFFFFFF0000000000000000FFFFFFFFFFFFFFFF0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000100000007000000000000000000000000000000000000000000FF'
P=runpy.run_path(str(ROOT/'poc/round45-tests/Runtime/peer.py'))
def generate():
 cases=P['generate']()
 us=[[1,0x3F800000],[5,0x80000000],[10,0x7FC01234]];body=b''.join(struct.pack('<HI',f,b) for f,b in us)
 wire=(struct.pack('<HIQIBIH',5,0x12862153,1,0x4352454f,4,len(body)+2,0)+body).hex().upper()
 cases.insert(0,dict(Steps=[dict(Actor=0,Declared=0,Updates=us,Wire=wire),dict(Actor=1,Declared=65535,Updates=[[6,18446744073709551615]],Wire=(struct.pack('<HIQIBIH',5,0x12862153,2,0x4352454f,4,12,65535)+struct.pack('<HQ',6,18446744073709551615)).hex().upper())]))
 return dict(Cases=cases,CREO3=BASE3,CREO6=BASE6)
if __name__=='__main__':
 command,path,*rest=sys.argv[1:];fixture=generate()
 if command=='generate':Path(path).write_text(json.dumps(fixture,separators=(',',':'))+'\n',encoding='utf-8')
 else:
  assert json.loads(Path(path).read_text())==fixture
  got=json.loads(Path(rest[0]).read_text());wanted=P['expected'](fixture['Cases'])
  other=2*(len(bytes.fromhex(fixture['CREO3']))+len(bytes.fromhex(fixture['CREO6'])))
  for case,record in zip(fixture['Cases'],wanted):
   actors=set()
   for i,(source,row) in enumerate(zip(case['Steps'],record['Steps'])):
    n=len(source['Updates']);a=len(actors);world_changes=1 if i==0 else 2
    row['WorldWork']=row['WorkUsed']+1+2*n+world_changes+a
    row['WorldBytes']=other+row['StateBytes']+row['PendingBytes']+64
    actors.add(source['Actor'])
  assert got['Records']==wanted,'literal model/world journal/state/bytes/provenance/work'
  assert got['OpaqueCounts']==[4]*len(wanted),'CREO3/CREO6 remain separately opaque'
  print('PASS world cases',len(wanted),'steps',sum(len(c['Steps']) for c in fixture['Cases']),'events',sum(len(s['Updates']) for c in fixture['Cases'] for s in c['Steps']),'checks',got['Checks'])
