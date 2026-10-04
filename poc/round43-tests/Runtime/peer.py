"""Independent full-state fixture/oracle inherited from accepted42; public world receipts only."""
import runpy,json,sys,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];P=runpy.run_path(str(ROOT/'poc/round42-tests/Runtime/peer.py'))
def generate():
 d=P['generate']();d['Cases']=[c for c in d['Cases'] if len(bytes.fromhex(c['Baseline']))-23<=1048576 and len(bytes.fromhex(c['Hex']))-23<=1048576]
 d['Full6Baseline']=json.loads((ROOT/'poc/round38-tests/Runtime/ordered-fixtures.json').read_text())['Cases'][0]['Baseline'];return d
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(generate()['Cases']))
else:
 d=json.loads(Path(sys.argv[2]).read_text());assert d==generate();r=json.loads(Path(sys.argv[3]).read_text());assert len(r['Records'])==len(d['Cases']);events=0
 for c,a in zip(d['Cases'],r['Records']):
  raw,_=P['BASE']['parse'](bytes.fromhex(c['Baseline']),'Baseline');us,mc=P['parse'](c['Hex']);initial,current,es,cost,_=P['oracle'](raw,us,mc)
  assert a['Name']==c['Name'] and a['Initial']==initial and a['Current']==current,(c['Name'],'models')
  assert a['Events']==es and a['Retained']==current['DecodedBytes']+sum(e['LogicalBytes'] for e in es) and a['AfterAck']==current['DecodedBytes'],(c['Name'],'events/bytes')
  assert a['StoreRevision']==2 and a['OpaqueCount']==0,(c['Name'],'store/opaque');events+=len(es)
 print('PASS independent world',len(d['Cases']),'events',events,'checks',r['Checks'])
