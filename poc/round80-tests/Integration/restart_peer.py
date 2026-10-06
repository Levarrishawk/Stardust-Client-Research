"""Independent pre-native four-generation motion, completion and offline wire expectations."""
import argparse,hashlib,importlib.util,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def model(runtime,terrain):
    source=dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4])
    cases=[dict(Name='first-budget',Start=[7.8,7.8],Maximum=3,Fault=False),dict(Name='second-budget',Start=[7.8,7.8],Maximum=3,Fault=False),dict(Name='terrain-fault',Start=[15.95,4],Maximum=4,Fault=True),dict(Name='recovered-budget',Start=[7.8,7.8],Maximum=3,Fault=False)]
    models=[dict(Name=c['Name'],Start=c['Start'],Targets=[],Manual=[dict(At=0,X=1 if c['Fault'] else 0,Z=0 if c['Fault'] else 1)],Pauses=[],Clears=[] if c['Fault'] else [2],Speeds=[],Updates=2 if c['Fault'] else 3,Speed=3,Radius=.001,Step=10000,Sparse=False) for c in cases]
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));traces,_=peer['compute'](dict(Terrain=source['Terrain'],Cases=models))
    spec=importlib.util.spec_from_file_location('wire80',ROOT/'poc/round14-tests/Networking/peer.py');wire=importlib.util.module_from_spec(spec);spec.loader.exec_module(wire)
    wanted=[]
    f=lambda b:struct.unpack('<f',struct.pack('<I',b))[0]
    for index,(c,t) in enumerate(zip(cases,traces),1):
        row=t['Rows'][-1];fault=c['Fault'];seed=0xA1B2C3D4+index;id_=0xF00D00DB+index;wire.SEED=seed;count=row['CompletedUpdates']
        report=dict(Reason=2 if fault else 0,MaximumUpdates=c['Maximum'],CompletedUpdates=count,AttemptedUpdates=row['AttemptedUpdates'],Stopped=not fault,Faulted=fault,Mode=row['Mode'],Speed=row['Speed'],TargetActive=False,FinishRequested=False)
        completion=dict(Samples=count,Queued=0 if fault else 1,Drained=0 if fault else 1,NextSequence=0 if fault else 1,SessionFaulted=False,TransportState='Established',QueueCount=0,QueueBytes=0,PendingMessages=0,RetainedPackets=0,CleanDrain=True)
        drain=None
        if not fault:
            x,y,z=map(f,row['Position']);app=struct.pack('<HIIIQII8f',5,0x80CE5E46,5,0x71,42,30,1,0,0,0,1,x,z,y,0)
            drain=dict(Sequence=0,ApplicationHex=app.hex().upper(),DatagramHex=wire.frame(struct.pack('>HH',9,0)+app).hex().upper(),AckHex=wire.frame(struct.pack('>HH',0x15,0)).hex().upper(),AcceptedCount=1)
        wanted.append(dict(Name=c['Name'],Generation=index,Completion=report,RootCompletion=completion,Position=row['Position'],Drain=drain,RequestHex=struct.pack('>HIII',1,2,id_,496).hex().upper(),ResponseHex=struct.pack('>HII3BI',2,id_,seed,2,1,4,496).hex().upper()))
        c.update(ConnectionId=id_,Seed=seed,AckHex=wire.frame(struct.pack('>HH',0x15,0)).hex().upper())
    source['Cases']=cases
    return source,wanted
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('--runtime',type=Path,required=True);p.add_argument('--terrain',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    source,wanted=model(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()))
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    if a.mode=='generate':
        out.write_text(json.dumps(source)+'\n');guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(wanted)+'\n');print('FOUR_GENERATIONS_EXPECTED_BEFORE_NATIVE');return
    expected=a.native.parent/'fixtures.expected.json'
    assert json.loads(expected.read_text())==wanted and expected.stat().st_mtime<a.native.stat().st_mtime
    actual=json.loads(a.native.read_text())
    for row in actual['Runs']:row['Completion']['Speed']=struct.unpack('<I',struct.pack('<f',row['Completion']['Speed']))[0]
    assert actual['Runs']==wanted,(actual['Runs'],wanted)
    assert actual['RejectedFactoryRequests']==1 and actual['AcceptedRestarts']==3 and actual['NativeChecks']>=40
    assert actual['HudRetirements']==4 and actual['WorldSurvived'] and actual['QueuesEmpty'] and actual['NoBorrowedActorsCreated'] and actual['PartialAdmissionClosed']
    receipt=dict(Passed=True,Generations=4,CompletedRuns=3,FaultedRuns=1,ActualFaultCompleted=1,ActualFaultAttempted=2,AcceptedRestarts=3,RejectedFactoryRequests=1,ExactHandshakes=4,ExactProtectedRootFrames=3,NativeChecks=actual['NativeChecks'],PersistentOutcome=True,ExpectedBeforeNative=True,LiveServer=False,FullPlaygroundWired=False)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
