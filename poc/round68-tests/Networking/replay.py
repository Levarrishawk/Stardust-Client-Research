"""Independent synthetic owned offline outbound queue byte and FIFO oracle and offline native replay."""
import argparse, hashlib, json, os, runpy, struct, subprocess, importlib.util, math
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
    def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
    def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
    cases=[]
    definitions=[('zero',[0,0,0,1],[-0.0,5,-7],-0.0,0xffffffff,0xffffffff,0x7fc00001),
                 ('halfturn',[1,0,0,0],[1.25,-2.5,3.75],2,0x89abcdef,0,0xff800000),
                 ('cyclic',[.5,.5,.5,.5],[-8,4,2],3,0,0x80000000,0xffffffff)]
    for name,q,position,speed,flags,counter,timestamp in definitions:
        columns=[]
        for axis in ([1,0,0],[0,1,0],[0,0,1]):
            first=cross(q[:3],axis);second=cross(q[:3],first);columns.append([axis[i]+2*(q[3]*first[i]+second[i]) for i in range(3)])
        matrix=[columns[c][row] for row in range(3) for c in range(3)]
        frame=struct.pack('<HIIIQII',5,0x80ce5e46,flags,0x71,prior.H,timestamp,counter)+struct.pack('<8f',*(q+[position[0],position[2],position[1],speed]))
        assert len(frame)==62
        cases.append(dict(Name=name,RowBits=[bits(v) for v in matrix],PositionBits=[bits(v) for v in position],SpeedBits=bits(speed),Flags=flags,Counter=counter,Timestamp=timestamp,Hex=frame.hex().upper()))
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Cases=cases)
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round68-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['Fifo']==[dict(Name=item['Name'],Hex=item['Hex']) for item in cases]
    assert native['Gates']==dict(HistoricalSameIncarnation=True,ByteCapAndDispose=True,ReincarnationDiscard=True,DestroyedDiscard=True,GenerationDiscard=True,FailurePrefix=True)
    assert native['ResourceLoads']==0
    receipt=dict(Status='PASS',Synthetic=True,ExactFifoFrames=len(cases),NativeChecks=native['Checks'],LifecycleGates=len(native['Gates']),OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
