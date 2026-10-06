"""Independent synthetic point-driver cadence/frontier and composed offline session replay."""
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
    def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
    expected=[];datagrams={};z=0.;last=0;lastz=0;lastspeed=0;has=False;previous=2;counter=0xfffffffe;sequence=0
    for i in range(32):datagrams['ack'+str(i)]=prior.wire.frame(struct.pack('>HH',0x15,i)).hex().upper()
    bad=bytearray.fromhex(datagrams['ack0']);bad[-1]^=1
    datagrams.update(badAck=bad.hex().upper(),otherAck=prior.wire.frame(struct.pack('>HH',0x17,0)).hex().upper())
    for i in range(1,121):
        speed=2 if i<=30 or 41<=i<=60 else 0
        if speed:z=f32(z+f32(f32(speed)*f32(.01)))
        clock=i*10000;due=(previous>0 and not speed) or ((not has or z!=lastz or speed!=lastspeed) and clock-last>=100000) or (not speed and has and clock-last>=1000000)
        timestamp=(0xfffffff0+clock//1000)&0xffffffff;drained=None
        if due:
            counter=(counter+1)&0xffffffff
            app=struct.pack('<HIIIQII',5,0x80ce5e46,5,0x71,prior.H,timestamp,counter)+struct.pack('<8f',0,0,0,1,0,z,2,speed)
            drained=dict(Sequence=sequence,ApplicationHex=app.hex().upper(),DatagramHex=prior.wire.frame(struct.pack('>HH',9,sequence)+app).hex().upper(),AckHex=datagrams['ack'+str(sequence)],AcceptedCount=sequence+1)
            sequence+=1;last=clock;lastz=z;lastspeed=speed;has=True
        expected.append(dict(Cadence=dict(Status=1 if due else 0,ElapsedMicroseconds=clock,Samples=i,MovementCounter=counter,Timestamp=timestamp),Drain=drained,QueueCountAfterCadence=1 if due else 0,QueueBytesAfterCadence=62 if due else 0));previous=speed
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams=datagrams,Acks={},Request=struct.pack('>HIII',1,2,0x12345678,496).hex().upper(),Response=struct.pack('>HII3BI',2,0x12345678,prior.wire.SEED,2,1,4,496).hex().upper())
    (out/'expected.json').write_text(json.dumps(expected),encoding='utf-8')
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round75-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['Receipts']==expected,(next((i for i,(a,b) in enumerate(zip(native['Receipts'],expected)) if a!=b),None))
    assert native['Failures']==[dict(Kind=k,Samples=10,Queued=1 if k in ['badAck','closed'] else 0,Drained=1 if k=='backpressure' else 0,Faulted=k in ['badAck','enqueue'],QueueCount=0 if k=='backpressure' else 1) for k in ['badAck','enqueue','backpressure','closed']]
    assert native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,NativeChecks=native['Checks'],PointDriverSteps=120,ExactReceipts=120,ExactFrames=sequence,FailurePolicies=4,OracleSha256=hashlib.sha256((out/'expected.json').read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
