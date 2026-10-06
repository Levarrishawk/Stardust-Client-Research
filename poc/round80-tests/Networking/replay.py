"""Independent serialized restart generations, completion prefixes and protected root bytes."""
import argparse, hashlib, json, os, runpy, struct, subprocess, importlib.util
from pathlib import Path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output-dir', required=True)
    args = parser.parse_args()
    root = Path(__file__).absolute().parents[3]
    guard = runpy.run_path(str(root / 'tools/python/verify_creature_controller_speed_inputs.py'))
    out = guard['fresh_private_path'](Path(args.output_dir), root, directory=True)
    spec=importlib.util.spec_from_file_location('frozen15',root/'poc/round15-tests/Networking/peer_model.py')
    prior=importlib.util.module_from_spec(spec);spec.loader.exec_module(prior);lifecycle=prior.produce()
    cases=[dict(ConnectionId=0xf00d00db,Seed=0xa1b2c3d4,Fault=False),dict(ConnectionId=0x11223344,Seed=0xffffffff,Fault=True),dict(ConnectionId=0x55667788,Seed=0,Fault=False)]
    def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
    expected=[];baselineSeed=prior.wire.SEED
    try:
        for generation,c in enumerate(cases,1):
            prior.wire.SEED=c['Seed'];z=0.;receipts=[]
            for i in range(1,4):
                if i<3:z=f32(z+f32(f32(2)*f32(.01)))
                if i==3 and c['Fault']:continue
                due=i==3;drained=None
                if due:
                    app=struct.pack('<HIIIQII',5,0x80ce5e46,5,0x71,prior.H,30,1)+struct.pack('<8f',0,0,0,1,0,z,2,0)
                    drained=dict(Sequence=0,ApplicationHex=app.hex().upper(),DatagramHex=prior.wire.frame(struct.pack('>HH',9,0)+app).hex().upper(),AckHex=prior.wire.frame(struct.pack('>HH',0x15,0)).hex().upper(),AcceptedCount=1)
                receipts.append(dict(Cadence=dict(Status=1 if due else 0,ElapsedMicroseconds=i*10000,Samples=i,MovementCounter=1 if due else 0,Timestamp=i*10),Drain=drained,QueueCountAfterCadence=1 if due else 0,QueueBytesAfterCadence=62 if due else 0))
            fault=c['Fault'];report=dict(Samples=3,Queued=1,Drained=0 if fault else 1,NextSequence=0 if fault else 1,SessionFaulted=fault,TransportState='Established',QueueCount=1 if fault else 0,QueueBytes=62 if fault else 0,PendingMessages=0,RetainedPackets=0,CleanDrain=not fault)
            expected.append(dict(Generation=generation,ConnectionId=c['ConnectionId'],Seed=c['Seed'],RequestHex=struct.pack('>HIII',1,2,c['ConnectionId'],496).hex().upper(),ResponseHex=struct.pack('>HII3BI',2,c['ConnectionId'],c['Seed'],2,1,4,496).hex().upper(),Receipts=receipts,Completion=report,QueueAtRetirement=1 if fault else 0,TransportAtRetirement='Closed'))
    finally:prior.wire.SEED=baselineSeed
    datagrams=dict(otherAck=prior.wire.frame(struct.pack('>HH',0x17,0)).hex().upper())
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams=datagrams,Acks={},Request='',Response='',Cases=cases)
    (out/'expected.json').write_text(json.dumps(expected),encoding='utf-8')
    expectedCleanup=dict(Samples=0,Queued=0,Drained=0,NextSequence=0,SessionFaulted=False,TransportState='Established',QueueCount=0,QueueBytes=0,PendingMessages=0,RetainedPackets=0,CleanDrain=True)
    (out/'expected-cleanup.json').write_text(json.dumps(dict(CleanupFailureType='InvalidOperationException',CleanupPreservedReport=expectedCleanup)),encoding='utf-8')
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round80-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['Runs']==expected,(native['Runs'],expected)
    assert native['CleanupFailureType']=='InvalidOperationException'
    assert native['CleanupPreservedReport']==expectedCleanup
    assert native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,NativeChecks=native['Checks'],SuccessfulGenerations=3,HealthyRuns=2,FaultRuns=1,PointDriverSteps=9,ExactHandshakes=3,ExactRootFrames=2,ExactCompletionReports=4,ExecutedCleanupFailure=1,OracleSha256=hashlib.sha256((out/'expected.json').read_bytes()).hexdigest(),CleanupOracleSha256=hashlib.sha256((out/'expected-cleanup.json').read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
