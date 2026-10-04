"""Independent full CREO6 model assignment and intermediate logical budget oracle; synthetic accepted32 wires."""
import copy,json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
peer=runpy.run_path(str(ROOT/'poc/round32-tests/Networking/peer.py'))
FIELDS={0:'Passive',2:'Level',3:'PerformanceAnimation',4:'MoodString',5:'WeaponId',6:'GroupId',8:'Guild',9:'TargetId',10:'MoodId',11:'PerformanceStart',12:'PerformanceType',16:'AlternateAppearance',17:'Frozen'}
def metrics(m):
 _,c=peer['parse'](peer['envelope'](peer['baseline_fields'](m),'Baseline',m['DeclaredCount']),'Baseline')
 return dict(StateBytes=c['DecodedBytes'],StringBytes=c['StringBytes'],TotalStringBytes=c['TotalStringBytes'],Elements=c['Elements'],TotalElements=c['TotalElements'],Work=c['Work'])
def apply(model,delta):
 m=copy.deepcopy(model);events=[];cost=dict(StateBytes=0,OutputBytes=0,Work=0,StringBytes=0,TotalStringBytes=0,Elements=0,TotalElements=0,Updates=len(delta['Updates']),Events=len(delta['Updates']))
 for n,u in enumerate(delta['Updates']):
  i=u['Index']
  if i==7:m['InviterId'],m['InviteCounter']=u['Bits'],u['SecondId']
  elif i in (3,4,16):m[FIELDS[i]]=copy.deepcopy(u['Text'])
  else:m[FIELDS[i]]=u['Bits']-(1<<32) if i==12 and u['Bits']>=1<<31 else u['Bits']
  c=metrics(m);cost['Work']+=1+c['Work'];events.append(dict(Index=i,Ordinal=n))
  for k in ('StateBytes','StringBytes','TotalStringBytes','Elements','TotalElements'):cost[k]=max(cost[k],c[k])
  cost['OutputBytes']=max(cost['OutputBytes'],c['StateBytes']+8*(n+1))
 return m,events,cost
def generate():
 original=peer['generate']();cases=[{k:c[k] for k in ('Name','Kind','Hex','Cost')} for c in original['Cases']];seq=[]
 for b in cases:
  if b['Kind']!='Baseline':continue
  m,_=peer['parse'](bytes.fromhex(b['Hex']),'Baseline');steps=[]
  for d in cases:
   if d['Kind']!='Delta':continue
   delta,_=peer['parse'](bytes.fromhex(d['Hex']),'Delta');m,e,cost=apply(m,delta);steps.append(dict(Name=d['Name'],Cost=cost))
  seq.append(dict(Name=b['Name'],Steps=steps))
 return dict(Synthetic=True,Version=1,Cases=cases,Sequences=seq)
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n')
 else:
  fixture=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());assert fixture==generate();native=json.loads(Path(sys.argv[2]).read_text());assert native['Checks']==836 and len(native['Cases'])==30;assert {'creature-constructor-coexist','generic-creo3-coexist','mixed-tano3-tano6-creo3','world-journal','world-revision','history-move-recreate-generation','callback-ack','callback-router-reentry','cancel-drain'}.issubset(set(native['Cases']));rows={r['Name']:r for r in native['Records']};assert len(rows)==len(native['Records'])==len(fixture['Sequences'])==7;wires={c['Name']:c for c in fixture['Cases']};transitions=events=0
  for seq in fixture['Sequences']:
   m,_=peer['parse'](bytes.fromhex(wires[seq['Name']]['Hex']),'Baseline');record=rows[seq['Name']];assert record['Initial']['Model']==m and record['Initial']['DecodedBytes']==metrics(m)['StateBytes'];queue=[]
   for i,(step,actual) in enumerate(zip(seq['Steps'],record['Steps']),2):
    d,_=peer['parse'](bytes.fromhex(wires[step['Name']]['Hex']),'Delta');m,e,cost=apply(m,d);assert cost==step['Cost'];queue += [dict(StoreRevision=i,**v) for v in e];assert actual['State']['Model']==m and actual['State']['Revision']==i and actual['State']['DecodedBytes']==metrics(m)['StateBytes'];assert actual['StoreRevision']==i and actual['Queue']==queue and actual['Work']==cost['Work'];transitions+=1;events+=len(e)
   assert len(record['Steps'])==len(seq['Steps'])
  print(json.dumps(dict(passed=True,native_checks=native['Checks'],sequences=len(rows),transitions=transitions,ordered_events=events,world_cases=native['Cases'],live_or_original_execution=False),indent=2))
