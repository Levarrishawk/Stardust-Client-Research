"""Independent synthetic locomotion phase/events joined to frozen F32 terrain trajectory reference."""
import argparse,hashlib,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21';TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def value(v):return struct.unpack('<f',struct.pack('<I',v))[0]
def f(v):return value(bits(v))
def clip(end,speed,groups):
    def chunk(tag,data):return tag.encode()+struct.pack('>I',len(data))+data
    def form(tag,*children):return chunk('FORM',tag.encode()+b''.join(children))
    children=[chunk('INFO',struct.pack('<H',len(groups)))]
    for name,frames in groups:children.append(chunk('MESG',struct.pack('<H',len(frames))+name.encode()+b'\0'+struct.pack('<'+'H'*len(frames),*frames)))
    info=struct.pack('<f6h',16,end,0,0,0,0,0);loct=struct.pack('<fhHfffHfff',speed,2,0,0,0,0,end,0,0,0)
    raw=form('CKAT',form('0001',chunk('INFO',info),form('XFRM'),form('MSGS',*children),chunk('LOCT',loct)))
    return dict(FrameRate=16,EndFrame=end,IdleTimeScale=1,AuthoredSpeed=speed,Groups=[dict(Name=n,Frames=fr) for n,fr in groups],Hex=raw.hex(),Synthetic=True)
def fixture(runtime,terrain):
    cases=[]
    def put(name,manual,targets=[],pauses=[],correction=None):
        c=dict(Name=name,Start=[7.8,7.8],Targets=[dict(At=a,X=x,Z=z) for a,x,z in targets],Manual=[dict(At=a,X=x,Z=z) for a,x,z in manual],Clears=[],Pauses=[dict(At=a,Value=v) for a,v in pauses],Updates=20,Speed=3,Radius=.01,Step=100000,Sparse=False,Speeds=[dict(At=6,Value=4),dict(At=14,Value=3)])
        if correction:c['Correction']=correction
        cases.append(c)
    put('manual-walk-run-idle',[(0,1,0),(8,1,1),(16,0,0)])
    put('pause-correction-rebase',[(0,1,1),(11,-1,1)],pauses=[(3,True),(9,False)],correction=dict(At=5,Position=[8.1,7.65]))
    put('manual-point-stop',[(0,1,0)],targets=[(7,8.5,8.5)])
    put('point-correction-manual',[(13,.5,0),(18,0,0)],targets=[(0,8.5,8.5)],correction=dict(At=2,Position=[8.1,7.65]))
    clips=[clip(8,0,[]),clip(8,2,[('step',[0,2,2,5,8]),('second',[2])]),clip(4,4,[('run_step',[0,1,3,4])])]
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=cases,Clips=clips)
def decorate(c,rows,clips):
    px,pz=map(f,c['Start']);mode='Idle';phase=distance=0.0;switches=cycles=0;generation=1;cursorphase=timestamp=0.0;initial=True;heading=[0,0,bits(1)];revision=2
    for i,row in enumerate(rows):
        if c.get('Correction',{}).get('At')==i:px,pz=map(f,c['Correction']['Position']);revision+=1
        x,z=value(row['Position'][0]),value(row['Position'][2]);dx,dz=f(x-px),f(z-pz);length=f(math.sqrt(f(f(dx*dx)+f(dz*dz))));seconds=c['Step']/1000000
        nextmode='Idle' if length<=1e-6 else 'Run' if length/seconds>=3.5 else 'Walk';descriptor=clips[['Idle','Walk','Run'].index(nextmode)];end=descriptor['EndFrame']
        inc=seconds*descriptor['FrameRate']/end if nextmode=='Idle' else length*descriptor['FrameRate']/(descriptor['AuthoredSpeed']*end)
        changed=nextmode!=mode
        if changed:generation+=1;switches+=1;cycles=0;cursorphase=phase*end;timestamp=0;initial=False
        cycles+=math.floor(phase+inc);nextphase=(phase+inc)%1;frame=nextphase*end;targetphase=cycles*end+frame;timestamp+=seconds
        events=[]
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
    return rows
def compute(source):
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));wanted,stats=peer['compute'](source)
    for c,w in zip(source['Cases'],wanted):decorate(c,w['Rows'],source['Clips'])
    return wanted,stats
def compare(got,want,path=''):
    if isinstance(want,float):assert math.isclose(got,want,rel_tol=2e-13,abs_tol=2e-13),(path,got,want);return
    if isinstance(want,dict):
        assert set(got)==set(want),(path,set(got),set(want))
        for k in want:compare(got[k],want[k],path+'/'+k)
    elif isinstance(want,list):
        assert len(got)==len(want),(path,len(got),len(want))
        for i,(a,b) in enumerate(zip(got,want)):compare(a,b,path+'/'+str(i))
    else:assert got==want,(path,got,want)
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted,stats=compute(source)
    if a.mode=='generate':guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Cases=wanted),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('OBSERVER_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text());compare(actual['Cases'],wanted);count=sum(len(c['Rows']) for c in wanted);assert count==80
    events=sum(len(r['Observer']['Events']) for c in wanted for r in c['Rows']);assert events>0 and actual['NegativeChecks']>=300
    report=dict(Passed=True,Cases=4,ComparedUpdates=count,Events=events,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentF32TerrainPoseSpeedHeadingClock=True,IndependentPhaseAndOrderedEvents=True,PhaseTolerance='2e-13 relative/absolute; F32 bits and event identities exact',CorrectionTeleportExcluded=True,OutputSeedUsed=False,OriginalExecuted=False,SyntheticMessages=True,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('TERRAIN_OBSERVER_PASS',json.dumps(report))
if __name__=='__main__':main()
