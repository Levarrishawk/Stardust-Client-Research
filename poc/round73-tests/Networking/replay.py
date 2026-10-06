"""Independent synthetic outer-multipacket ordering oracle and offline native replay."""
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
    z=0.;last=0;has=False;lastz=0;lastspeed=0;counter=0xfffffffe;pending=[];frames=[];statuses=[];previous=2
    for i in range(1,1001):
        speed=2 if i<=300 or 501<=i<=700 else 0
        if speed:z=f32(z+f32(f32(speed)*f32(.01)))
        clock=i*10000;stop=previous>0 and speed==0
        due=stop or ((not has or z!=lastz or speed!=lastspeed) and clock-last>=100000) or (not speed and has and clock-last>=1000000)
        status='NotDue'
        if due:
            if len(pending)>=2:status='DeclinedBackpressure'
            else:
                status='Queued';counter=(counter+1)&0xffffffff;timestamp=(0xfffffff0+clock//1000)&0xffffffff
                raw=struct.pack('<HIIIQII',5,0x80ce5e46,5,0x71,prior.H,timestamp,counter)+struct.pack('<8f',0,0,0,1,0,z,2,speed)
                pending.append(raw);last=clock;has=True;lastz=z;lastspeed=speed
        statuses.append(status);previous=speed
        if i<20 or i>=45:
            frames.extend(dict(At=i,Hex=p.hex().upper()) for p in pending);pending=[]
    expectedWire=prior.wire.frame(struct.pack('>HH',9,0)+bytes.fromhex(frames[0]['Hex'])).hex().upper()
    acks=dict(correction=prior.wire.frame(struct.pack('>HH',0x15,0)).hex().upper())
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams={},Acks=acks,Request=struct.pack('>HIII',1,2,0x12345678,496).hex().upper(),Response=struct.pack('>HII3BI',2,0x12345678,prior.wire.SEED,2,1,4,496).hex().upper())
    # Expected cadence/raw/protected bytes are complete before starting native.
    (out/'expected.json').write_text(json.dumps(dict(Frames=frames,Statuses=statuses,WireHex=expectedWire)),encoding='utf-8')
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round73-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['Frames']==frames,(native['Frames'][:3],frames[:3])
    assert native['Statuses']==statuses
    assert native['WireHex']==expectedWire and native['DriverSteps']==1000 and native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,NativeChecks=native['Checks'],DriverSteps=1000,ExactFrames=len(frames),BackpressureSamples=statuses.count('DeclinedBackpressure'),ExactProtectedFrames=1,OracleSha256=hashlib.sha256((out/'expected.json').read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
