"""Before-native bounded long-session trajectory, recorded-event metadata and pivot oracle."""
import argparse,hashlib,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21';TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd';LOCOMOTION='9cebf47f94457a5604c1ab4054e270024a4d231b9ebbe70ca1a4bea8361dddc2'
def encode(record):
    def chunk(tag,data):return tag.encode()+struct.pack('>I',len(data))+data
    def form(tag,*children):return chunk('FORM',tag.encode()+b''.join(children))
    groups=record['Messages'];end=record['EndFrame'];speed=record['AuthoredSpeed'];children=[chunk('INFO',struct.pack('<H',len(groups)))]
    for g in groups:children.append(chunk('MESG',struct.pack('<H',len(g['Frames']))+g['Name'].encode()+b'\0'+struct.pack('<'+'H'*len(g['Frames']),*g['Frames'])))
    info=struct.pack('<f6h',record['FrameRate'],end,0,0,0,0,0);loct=struct.pack('<fhHfffHfff',speed,2,0,0,0,0,end,0,0,0)
    raw=form('CKAT',form('0001',chunk('INFO',info),form('XFRM'),form('MSGS',*children),chunk('LOCT',loct)))
    return dict(FrameRate=record['FrameRate'],EndFrame=end,IdleTimeScale=record['IdleTimeScale'],AuthoredSpeed=speed,Groups=groups,Hex=raw.hex(),SyntheticContainer=True,ReencodedSelectedMessageMetadata=True,OriginalAssetBytes=False,OriginalRecordMetadata=record)
def fixture(runtime,terrain,records):
    targets=[dict(At=i,X=8 if n%2 else 8.5,Z=8 if n%2 else 8.5) for n,i in enumerate(range(600,1000,20))]
    targets.insert(0,dict(At=500,X=8.5,Z=8.5))
    # Twenty successful point admissions, including the first handoff.
    targets=targets[:20]
    c=dict(Name='long-1200-sustained-accepted-session',Start=[7.8,7.8],Targets=targets,Manual=[dict(At=i,X=x,Z=z) for i,x,z in [(0,1,0),(100,-1,0),(200,0,1),(300,0,-1),(400,1,0),(550,0,0)]],Clears=[],Pauses=[dict(At=400,Value=True),dict(At=440,Value=False)],Correction=dict(At=450,Position=[8.1,7.65]),Updates=1200,Speed=3,Radius=.01,Step=10000,Sparse=False,Speeds=[dict(At=200,Value=4),dict(At=500,Value=3)])
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=[c],Clips=[encode(r) for r in records])
def compute(source):
    movement=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));wanted,_=movement['compute'](source)
    decorate(source['Cases'][0],wanted[0]['Rows'],source['Clips'])
    follow=runpy.run_path(str(ROOT/'poc/round72-tests/Runtime/verify_follow.py'));camera=follow['compute'](source|dict(Pure=[],Driver=source['Cases'][0],DriverOffset=[0,2,0]))
    ticks=[r for r in camera['Driver'] if r['Kind']=='Advance'];assert len(ticks)==1200
    for row,c in zip(wanted[0]['Rows'],ticks):row['Follow']=c['Follow']
    return wanted
def decorate(c,rows,clips):
    numeric=runpy.run_path(str(ROOT/'poc/round71-tests/Runtime/verify_observer.py'));f,bits,value=numeric['f'],numeric['bits'],numeric['value']
    px,pz=map(f,c['Start']);mode='Idle';phase=distance=0.0;switches=cycles=0;generation=1;cursorphase=timestamp=0.0;initial=True;heading=[0,0,bits(1)];revision=2
    for i,row in enumerate(rows):
        if c.get('Correction',{}).get('At')==i:px,pz=map(f,c['Correction']['Position']);revision+=1
        x,z=value(row['Position'][0]),value(row['Position'][2]);dx,dz=f(x-px),f(z-pz);length=f(math.sqrt(f(f(dx*dx)+f(dz*dz))));seconds=c['Step']/1000000
        nextmode='Idle' if length<=1e-6 else 'Run' if length/seconds>=3.5 else 'Walk';descriptor=clips[['Idle','Walk','Run'].index(nextmode)];end=descriptor['EndFrame']
        # Frozen C# float*ushort denominator rounds BEFORE the double distance division.
        denominator=f(f(descriptor['AuthoredSpeed'])*end)
        inc=seconds*descriptor['FrameRate']*descriptor['IdleTimeScale']/end if nextmode=='Idle' else length*descriptor['FrameRate']/denominator
        if nextmode!=mode:generation+=1;switches+=1;cycles=0;cursorphase=phase*end;timestamp=0;initial=False
        cycles+=math.floor(phase+inc);nextphase=(phase+inc)%1;frame=nextphase*end;targetphase=cycles*end+frame;timestamp+=seconds;events=[]
        if targetphase!=cursorphase:
            for cy in range(max(0,math.floor(cursorphase/end)-1),math.floor(targetphase/end)+1):
                group_events=[]
                for gi,g in enumerate(descriptor['Groups']):
                    for oi,fr in enumerate(g['Frames']):
                        at=cy*end+fr
                        if (at>cursorphase or initial and cy==0 and at==cursorphase) and at<=targetphase:group_events.append(dict(Cycle=cy,GroupIndex=gi,OccurrenceIndex=oi,Name=g['Name'],Frame=fr))
                events.extend(sorted(group_events,key=lambda e:(e['Frame'],e['GroupIndex'],e['OccurrenceIndex'])))
            initial=False
        if length>1e-6:heading=[bits(f(dx/length)),0,bits(f(dz/length))]
        distance+=length;revision+=1
        row['Observer']=dict(Observations=i+1,TimestampMicroseconds=(i+1)*c['Step'],MotionRevision=revision,Generation=generation,Mode=nextmode,Frame=frame,NormalizedPhase=nextphase,AcceptedDistance=distance,Switches=switches,Heading=heading,UnwrappedPhase=targetphase,GenerationSeconds=timestamp,CompletedCycles=cycles,Events=events)
        px,pz=x,z;mode=nextmode;phase=nextphase;cursorphase=targetphase
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','locomotion','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN and hashlib.sha256(a.locomotion.read_bytes()).hexdigest()==LOCOMOTION
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()),json.loads(a.locomotion.read_text()));wanted=compute(source)
    if a.mode=='generate':guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Cases=wanted),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('LONG_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text());runpy.run_path(str(ROOT/'poc/round71-tests/Runtime/verify_observer.py'))['compare'](actual['Cases'],wanted)
    rows=wanted[0]['Rows'];events=sum(len(r['Observer']['Events']) for r in rows);assert actual['EventCount']==events and len(rows)==1200 and rows[-1]['SuccessfulTargets']==20 and rows[-1]['Follow']['Rebases']==1
    assert rows[255]['Observer']['AcceptedDistance']>rows[127]['Observer']['AcceptedDistance'] and actual['DefaultProbeUpdates']==128
    report=dict(Passed=True,ComparedJoinedSteps=1200,ElapsedMicroseconds=12000000,SuccessfulTargets=20,Corrections=1,Events=events,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],DefaultBudgetNoCountChangeProbe=128,ExpectedGeneratedBeforeNative=True,IndependentF32GroundedTrajectoryObserverEventsAndPivot=True,Comparison='F32/integer/event identities exact, phase2e-13 rel/abs',PersistentAcross128=True,RecordedMessageMetadataReencodedSyntheticContainer=True,OriginalAssetBytes=False,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN,LocomotionMetadataSha256=LOCOMOTION)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('LONG_SESSION_PASS',json.dumps(report))
if __name__=='__main__':main()
