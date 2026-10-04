"""Independent synthetic supported-state and journal oracle; no native expectations."""
import copy,json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OLD=runpy.run_path(str(ROOT/'poc/round41-tests/Runtime/peer.py'))
def generate():return OLD['generate']()
def verify(fixture,receipt):
 data=json.loads(Path(fixture).read_text());assert data==generate();actual=json.loads(Path(receipt).read_text());assert len(actual['Records'])==len(data['Cases']);total=0
 for c,r in zip(data['Cases'],actual['Records']):
  raw,_=OLD['BASE']['parse'](bytes.fromhex(c['Baseline']),'Baseline');us,mc=OLD['parse'](c['Hex']);initial,current,events,sc,seedcost=OLD['oracle'](raw,us,mc)
  assert r['Name']==c['Name'] and r['Initial']==initial and r['Current']==current,(c['Name'],'owned states/raw bits/counters')
  assert r['StoreRevision']==2 and r['RetainedBytes']==current['DecodedBytes'] and r['PendingBytes']==sum(e['LogicalBytes'] for e in events),(c['Name'],'store accounting')
  assert r['Queue']==[dict(StoreRevision=2,Event=e) for e in events],(c['Name'],'ordered variable-byte journal')
  expected=copy.deepcopy(initial);expected['Revision']=3
  assert r['Reseed']==expected and r['ReseedStoreRevision']==3 and r['ReseedPendingBytes']==r['PendingBytes'],(c['Name'],'historical reseed journal')
  assert r['WorkUsed']==sc['Work'] and r['SeedWork']==seedcost['Work'],(c['Name'],'independent child work')
  total+=len(events)
 # Independent two-actor journal/accounting sequence, not native-derived expectations.
 m=dict(ObjectId=7,DeclaredCount=65535,FloatBits=[0]*10,ListenTo=(1<<64)-1,Encumbrances=dict(Counter=0,Values=[]),SkillMods=dict(Counter=0,Values=[]),SpaceMissionObjects=dict(Counter=0,Values=[]))
 u=dict(Index=3,Skill=dict(Extent=1,Counter=1,Commands=[dict(Opcode=0,Key='0080ff',Extended=False,BaseValue=-1,Bonus=-(1<<31))]))
 _,mc=OLD['parse'](OLD['encode']([u]));_,a,events,_,_=OLD['oracle'](m,[u],mc);a['Revision']=3
 b=dict(Model=copy.deepcopy(m),Revision=1,DecodedBytes=72);b['Model']['ObjectId']=8
 expected=dict(ActorA=a,ActorB=b,StoreRevision=4,ActorAStoreRevision=4,ActorBStoreRevision=2,RetainedBytes=163,PendingBytes=67,Queue=[dict(StoreRevision=3,Event=e) for e in events])
 assert actual['GlobalTrace']==expected,'independent multi-actor identity/model/variable journal trace'
 assert actual['Lifecycle']==['history-move-recreate-reset','capacity-foreign-owner-close','global-variable-history','intermediate-late-failure','stale-other-no-write-prune','revision-overflow','zero-event-ack','invalid-policies']
 print(json.dumps(dict(passed=True,sequences=len(data['Cases']),events=total,checks=actual['Checks'],budgets=actual['Budgets'],negatives=actual['Negatives'],lifecycle=len(actual['Lifecycle'])),indent=2))
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n');print('Generated122 independent supported store cases')
 else:verify(sys.argv[2],sys.argv[3])
