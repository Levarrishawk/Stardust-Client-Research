"""Independent complete CREO4 host fixture/model/event/accounting oracle."""
import copy,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('accepted42_full4',ROOT/'poc/round42-tests/Runtime/peer.py')
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
OID=0xDABC001122334455

def envelope(body,baseline=False,count=65535):
    return struct.pack('<HIQIBIH',5,0x68a75f0c if baseline else 0x12862153,OID,0x4352454f,4,len(body)+2,count)+body
def snapshot(model):return dict(ModelHex=envelope(p.BASE['baseline_fields'](model),True,model['DeclaredCount']).hex().upper(),StateBytes=p.semantic(model))
def skill(model,op,key,a=0,b=0):return dict(Index=3,Skill=dict(Extent=1,Counter=(model['SkillMods']['Counter']+1)&0xffffffff,Commands=[dict(Opcode=op,Key=key,Extended=True,BaseValue=a,Bonus=b)]))
def mission(model,op,pair=None):return dict(Index=13,Mission=dict(Extent=1,Counter=(model['SpaceMissionObjects']['Counter']+1)&0xffffffff,Commands=[dict(Opcode=op,Pair=pair)]))
def enc(model,op,index=0,values=None,extent=1):return dict(Index=2,Extent=extent,Counter=(model['Encumbrances']['Counter']+extent)&0xffffffff,Commands=[dict(Opcode=op,Index=index,Values=values or [])])
def generate():
    case=p.generate()['Cases'][0];raw,_=p.BASE['parse'](bytes.fromhex(case['Baseline']),'Baseline');raw['ObjectId']=OID
    first,cost=p.parse(case['Hex']);initial,_,_,_,_=p.oracle(raw,first,cost);model=initial['Model'];seed=snapshot(model)
    baseline=envelope(p.BASE['baseline_fields'](raw),True,raw['DeclaredCount']).hex().upper();steps=[]
    makers=[('all14_ordered',lambda m:first),
        ('cross_container_growth',lambda m:[skill(m,2,'0080ff',-1,-2147483648),mission(m,1,dict(OwnerId=0x8000000000000001,MissionId=(1<<64)-1)),enc(m,1,1,[-7]),dict(Index=6,Bits=(1<<64)-1),dict(Index=0,Bits=0x80000000)]),
        ('prior_events_then_shrink',lambda m:[skill(m,1,'0080ff'),mission(m,2),enc(m,4)]),
        ('assignment_overshoot',lambda m:[enc(m,3,0,[0,-1,2147483647]),skill(m,0,'78'),mission(m,1,dict(OwnerId=0,MissionId=0)),dict(Index=9,Bits=0x7f800001)]),
        ('equal_and_counter_only',lambda m:[dict(Index=0,Bits=m['FloatBits'][0]),skill(m,2,'78'),mission(m,1,dict(OwnerId=0,MissionId=0)),enc(m,2,0,[0])])]
    for name,make in makers:
        updates=make(model);wire=p.encode(updates);parsed,cost=p.parse(wire);_,current,events,_,_=p.oracle(model,parsed,cost);model=current['Model']
        steps.append(dict(Name=name,Hex=envelope(wire[25:]).hex().upper(),Events=events,**snapshot(model)))
    return dict(Version=1,Synthetic=True,ObjectId=OID,Baseline=baseline,Seed=seed,Steps=steps)
def verify(fixture,log):
    data=json.loads(Path(fixture).read_text());assert data==generate();text=Path(log).read_text(encoding='utf-8-sig')
    rows=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('creature4_full_state=')]
    expected=[dict(Name='seed',Events=[],**data['Seed'])]+data['Steps'];assert len(rows)==len(expected)==6
    events=0
    for index,(row,e) in enumerate(zip(rows,expected)):
        assert row['Index']==index and row['Name']==e['Name'] and row['ObjectId']==OID
        assert row['ModelHex']==e['ModelHex'] and row['Events']==e['Events'] and row['RetainedBytes']==e['StateBytes']
        assert row['StateRevision']==row['StoreRevision']==index+1
        assert row['PendingBytesBeforeAck']==e['StateBytes']+sum(v['LogicalBytes'] for v in e['Events'])
        assert row['Current'] and row['AcknowledgedOnce'] and row['VisualRetained'] and row['Archives']==3
        events+=len(e['Events'])
    assert 'PASS: pumped creature_view4_full=true' in text and 'creature4_full_cleanup={"StateAndJournalBytes":0,"Drained":true}' in text
    return dict(Passed=True,FullModels=6,OrderedEvents=events,ExactVariableJournal=True,OriginalResources=True,WorldIngress=True,Live=False)
if __name__=='__main__':
    if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated complete CREO4 original-host fixture')
    else:print(json.dumps(verify(*sys.argv[2:4])))
