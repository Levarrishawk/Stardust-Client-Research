"""Independent accepted37 all18 raw model/counter/event/work/peak oracle with full-store publication export."""
import json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];peer=runpy.run_path(str(ROOT/'poc/round37-tests/Networking/peer.py'))
def generate():return dict(Synthetic=True,Cases=[{k:c[k] for k in ('Name','Baseline','Hex','CodecCost','StateCost')} for c in peer['generate']()['Cases']])
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n')
 else:
  f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());assert f==generate();n=json.loads(Path(sys.argv[2]).read_text());assert n['Checks']==2773 and n['BudgetChecks']==1453 and len(n['Cases'])==15;assert {'global-output-history','global-event-history','generation-reseed-retirement','stale-other-capacity-retirement'}.issubset(set(n['Cases']));rows={r['Name']:r for r in n['Records']};assert len(rows)==len(n['Records'])==len(f['Cases'])==97;events=0
  for c in f['Cases']:
   seed,_=peer['b']['parse'](bytes.fromhex(c['Baseline']),'Baseline');plan,cost=peer['parse'](bytes.fromhex(c['Hex']));updates=[]
   for u in plan['Updates']:
    if u['Scalar'] is not None:updates.append(u['Scalar'])
    else:
     child=next(u[k] for k in ('Defender','Ham','Wearable') if u[k] is not None);updates.append(dict(Index=u['Index'],Extent=child['Extent'],Counter=child['Counter'],Commands=child['Commands']))
   expected,descriptions,peak=peer['apply'](seed,updates,cost);r=rows[c['Name']]
   assert r['Initial']==dict(Model=seed,Revision=1,DecodedBytes=peer['measure'](seed)['StateBytes']);assert r['State']==expected and r['StoreRevision']==2 and r['WorkUsed']==peak['Work'] and cost==c['CodecCost'] and peak==c['StateCost'];assert r['Queue']==[dict(StoreRevision=2,**e) for e in descriptions];events+=len(descriptions)
  print(json.dumps(dict(passed=True,native_checks=n['Checks'],budget_checks=n['BudgetChecks'],sequences=97,ordered_events=events,store_cases=n['Cases'],live_or_original_execution=False),indent=2))
