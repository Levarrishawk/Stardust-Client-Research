"""Independent accepted scalar/HAM wire/model oracle with world ownership exports; synthetic fixtures."""
import json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p35=runpy.run_path(str(ROOT/'poc/round35-tests/Runtime/peer.py'));peer=p35['peer']
def generate():
 f=p35['generate']();baseline=next(c for c in peer['b']['generate']()['Cases'] if c['Name']=='empty_server22');return dict(**f,EmptyBaseline=baseline['Hex'])
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n')
 else:
  f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());assert f==generate();n=json.loads(Path(sys.argv[2]).read_text());assert n['Checks']==816 and len(n['Cases'])==40;assert {'exclusive-creo6-policies','global-output-journal','global-queue','late-ham-intermediate','noop-and-late-wire','callback-event-drain','callback-event-ack','callback-world-attempt','mixed-tano3-tano6-creo3','cancel-drain'}.issubset(set(n['Cases']));rows={r['Name']:r for r in n['Records']};assert len(rows)==len(n['Records'])==len(f['Cases'])==34;events=0
  for c in f['Cases']:
   seed,_=peer['b']['parse'](bytes.fromhex(c['Baseline']),'Baseline');plan,_=peer['parse'](bytes.fromhex(c['Hex']));updates=[u['Scalar'] if u['Scalar'] is not None else dict(Index=u['Index'],Extent=u['Ham']['Extent'],Counter=u['Ham']['Counter'],Commands=u['Ham']['Commands']) for u in plan['Updates']];expected,descriptions,peak=peer['apply'](seed,updates);r=rows[c['Name']]
   assert r['Initial']==dict(Model=seed,Revision=1,DecodedBytes=peer['measure'](seed)['StateBytes']);assert r['State']==expected and r['StoreRevision']==2 and r['WorkUsed']==peak['Work'] and peak==c['StateCost'];assert r['Queue']==[dict(StoreRevision=2,**e) for e in descriptions];events+=len(descriptions)
  print(json.dumps(dict(passed=True,native_checks=n['Checks'],sequences=34,ordered_events=events,world_cases=n['Cases'],live_or_original_execution=False),indent=2))
