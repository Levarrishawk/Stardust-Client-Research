"""Independent primitive completion/fault reports and small offline root proof."""
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
    requestHex=struct.pack('>HIII',1,2,0xf00d00db,496).hex().upper()
    responseHex=struct.pack('>HII3BI',2,0xf00d00db,0xa1b2c3d4,2,1,4,496).hex().upper()
    def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
    expected=[];z=0.;counter=0;sequence=0
    for i in range(1,4):
        speed=2 if i<=2 else 0
        if speed:z=f32(z+f32(f32(speed)*f32(.01)))
        clock=i*10000;due=i==3;drained=None
        if due:
            counter=1;app=struct.pack('<HIIIQII',5,0x80ce5e46,5,0x71,prior.H,30,1)+struct.pack('<8f',0,0,0,1,0,z,2,0)
            drained=dict(Sequence=0,ApplicationHex=app.hex().upper(),DatagramHex=prior.wire.frame(struct.pack('>HH',9,0)+app).hex().upper(),AckHex=prior.wire.frame(struct.pack('>HH',0x15,0)).hex().upper(),AcceptedCount=1)
            sequence=1
        expected.append(dict(Cadence=dict(Status=1 if due else 0,ElapsedMicroseconds=clock,Samples=i,MovementCounter=counter,Timestamp=clock//1000),Drain=drained,QueueCountAfterCadence=1 if due else 0,QueueBytesAfterCadence=62 if due else 0))
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams={},Acks={},Request='',Response='',Cases=[])
    def report(samples,queued,drained,sequence,fault,state,count=0,bytes_=0,pending=0,retained=0):
        return dict(Samples=samples,Queued=queued,Drained=drained,NextSequence=sequence,SessionFaulted=fault,TransportState=state,QueueCount=count,QueueBytes=bytes_,PendingMessages=pending,RetainedPackets=retained,CleanDrain=not fault and count==bytes_==pending==retained==0)
    reports=dict(initial=report(0,0,0,0,False,'Established'),normal=report(3,1,1,1,False,'Established'),retained=report(3,1,1,1,False,'Established',1,62,1,1),ownerDisposed=report(3,1,1,1,False,'Closed',1,62),callerQueueDisposed=report(3,1,1,1,False,'Closed'),fault=report(3,1,0,0,True,'Established',1,62),faultDisposed=report(3,1,0,0,True,'Closed',1,62),faultCleared=report(3,1,0,0,True,'Closed'))
    (out/'expected.json').write_text(json.dumps(dict(RequestHex=requestHex,ResponseHex=responseHex,Receipts=expected,Reports=reports)),encoding='utf-8')
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round79-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['RequestHex']==requestHex and native['ResponseHex']==responseHex
    assert native['Receipts']==expected
    assert native['Reports']==reports,(native['Reports'],reports)
    assert native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,NativeChecks=native['Checks'],ExactHandshakes=1,PointDriverSteps=6,ExactSessionReceipts=3,ExactRootFrames=sequence,ExactPrimitiveReports=8,OracleSha256=hashlib.sha256((out/'expected.json').read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
