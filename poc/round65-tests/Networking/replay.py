"""Independent synthetic full-transform application and geometry oracle and offline native replay."""
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
    groups=[
        [0x3f800000,0xc0000000,0x40400000,0xc0800000],
        [0,0,0,0],
        [1,0x80000001,1,1],
        [0x7f7fffff,0xff7fffff,0x7f7fffff,0xff7fffff],
        [0,0x3f800000,0,0],
        [0x80000000,0,0x80000000,0x3f800000],
        [0x00800000,0x80800000,0x007fffff,0x807fffff],
        [0x3e800000,0xbf000000,0x3f400000,0x40000000],
    ]
    cases=[];expected=[]
    def f(bits):return struct.unpack('<f',struct.pack('<I',bits))[0]
    def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
    for index,qbits in enumerate(groups):
        position=[0x80000000,0x7f7fffff,1] if index==3 else [0x40a00000,0xc0c00000,0x40e00000]
        speed=0x80000000 if index==5 else 0x7f7fffff if index==6 else 0x40000000
        frame=struct.pack('<HIIIQII',5,0x80ce5e46,0xf1234567,0x71,prior.H,0x80000000,index)+struct.pack('<8I',*(qbits+[position[0],position[2],position[1],speed]))
        cases.append(dict(Name=f'pose-{index}',Hex=frame.hex().upper()))
        q=[f(b) for b in qbits];norm=math.hypot(*q);zero=norm==0;q=[0,0,0,1] if zero else [v/norm for v in q]
        columns=[]
        for axis in ([1,0,0],[0,1,0],[0,0,1]):
            first=cross(q[:3],axis);second=cross(q[:3],first)
            columns.append([axis[i]+2*(q[3]*first[i]+second[i]) for i in range(3)])
        expected.append(dict(Name=f'pose-{index}',Rows=[columns[c][row] for row in range(3) for c in range(3)],PositionBits=position,SpeedBits=speed,UsedZeroIdentity=zero))
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Cases=cases)
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round65-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert len(native['Cases'])==len(cases)+1
    for source,want,actual in zip(cases,expected,native['Cases']):
        assert actual['Name']==want['Name'] and actual['RawHex']==source['Hex']
        assert all(actual[key]==want[key] for key in ('PositionBits','SpeedBits','UsedZeroIdentity'))
        assert max(abs(a-b) for a,b in zip(actual['Rows'],want['Rows']))<2e-7
    serial=native['Cases'][-1]['Serial'];last=None;revision=len(cases)
    for row in serial:
        incoming=row['Counter'];admit=last is None or 0<((incoming-last)&0xffffffff)<0x80000000
        if admit:last=incoming;revision+=1
        assert row['Accepted']==admit and row['Revision']==revision
    assert native['ResourceLoads']==0
    receipt=dict(Status='PASS',Synthetic=True,Cases=len(cases),NativeChecks=native['Checks'],SerialCases=len(serial),BasisTolerance=2e-7,OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
