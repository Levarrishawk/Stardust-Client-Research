"""Independent full CREO6 world expectations from accepted37 synthetic oracle."""
import json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
peer=runpy.run_path(str(ROOT/'poc/round37-tests/Networking/peer.py'))
def generate():
 f=peer['generate']()
 empty=next(c for c in peer['b']['generate']()['Cases'] if c['Name']=='empty_server22')
 return dict(Synthetic=True,EmptyBaseline=empty['Hex'],Cases=[{k:c[k] for k in ('Name','Baseline','Hex','CodecCost','StateCost')} for c in f['Cases']])
def verify(native):
 f=generate();n=json.loads(Path(native).read_text());rows={r['Name']:r for r in n['Records']}
 assert len(rows)==len(n['Records'])==len(f['Cases'])==97
 assert {'exclusive-creo6-policies','global-output-journal','global-queue','callback-event-drain','callback-event-ack','callback-world-attempt','mixed-tano3-tano6-creo3','cancel-drain'}.issubset(set(n['Cases']))
 events=0
 for c in f['Cases']:
  seed,_=peer['b']['parse'](bytes.fromhex(c['Baseline']),'Baseline');plan,cost=peer['parse'](bytes.fromhex(c['Hex']));updates=[]
  for u in plan['Updates']:
   if u['Scalar'] is not None:updates.append(u['Scalar'])
   else:
    child=next(u[k] for k in ('Defender','Ham','Wearable') if u[k] is not None)
    updates.append(dict(Index=u['Index'],Extent=child['Extent'],Counter=child['Counter'],Commands=child['Commands']))
  expected,descriptions,peak=peer['apply'](seed,updates,cost);r=rows[c['Name']]
  assert r['Initial']==dict(Model=seed,Revision=1,DecodedBytes=peer['measure'](seed)['StateBytes'])
  assert r['State']==expected and r['StoreRevision']==2 and r['WorkUsed']==peak['Work'] and peak==c['StateCost']
  assert r['Queue']==[dict(StoreRevision=2,**e) for e in descriptions];events+=len(descriptions)
 return dict(Passed=True,Checks=n['Checks'],Sequences=97,Events=events,WorldGroups=len(n['Cases']),Synthetic=True,OriginalExecution=False)
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8')
 else:print(json.dumps(verify(sys.argv[2]),indent=2))
