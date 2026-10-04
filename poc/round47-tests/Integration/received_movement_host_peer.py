"""Independent received movement inspection on the accepted original-resource partial host."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('rx_host46',ROOT/'poc/round46-tests/Integration/creature4_partial_host_peer.py');peer=importlib.util.module_from_spec(s);s.loader.exec_module(peer)
def verify(fixture,log):
 data=json.loads(Path(fixture).read_text());assert data==peer.generate();text=Path(log).read_text(encoding='utf-8-sig');rows=[json.loads(x.split('=',1)[1]) for x in text.splitlines() if x.startswith('received_movement_state=')];expected=[data['Seed']]+data['Steps'];assert len(rows)==len(expected)==6
 for index,(row,model) in enumerate(zip(rows,expected)):
  known={f['Index']:f for f in model['Fields']};fields=[]
  for f in [0,1,4,5,7,8,9,10,11,12]:
   prior=known.get(f);bits=prior['Bits'] if prior else None;kind=0 if bits is None else 2 if bits&0x7f800000==0x7f800000 else 1
   fields.append(dict(Index=f,RawBits=bits,Provenance=prior['Provenance'] if prior else None,Kind=kind,Eligibility=0 if kind==0 else 1 if kind==2 else 3))
  assert row==dict(Index=index,Phase='Finalized',Generation=1,PublicationRevision=index+1,StateRevision=index+1,DecodedBytes=544,Fields=fields,Current=True,NoDefaultSeeds=True,MotionApplied=False,Archives=3),index
 assert rows[0]['Fields'][4]['RawBits'] is None and rows[0]['Fields'][8]['RawBits'] is None
 peer.verify(fixture,log)
 print(json.dumps(dict(Passed=True,ReceivedModels=6,FixedSlotObservations=60,OriginalResources=True,NoDefaultSeeds=True,MotionApplied=False)))
if __name__=='__main__':verify(*sys.argv[1:3])
