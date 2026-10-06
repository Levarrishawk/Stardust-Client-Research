"""Independent synthetic full-transform byte oracle and offline native replay."""
import argparse, hashlib, json, os, runpy, struct, subprocess
from pathlib import Path

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output-dir', required=True)
    args = parser.parse_args()
    root = Path(__file__).absolute().parents[3]
    guard = runpy.run_path(str(root / 'tools/python/verify_creature_controller_speed_inputs.py'))
    out = guard['fresh_private_path'](Path(args.output_dir), root, directory=True)
    groups = [
        [0x3f800000,0xc0000000,0x40400000,0xc0800000,0x40a00000,0xc0c00000,0x40e00000,0xc1000000],
        [0x80000000,0,0x80000000,0,0x80000000,0,0x80000000,0x80000000],
        [0x7f7fffff,0xff7fffff,1,0x80000001,0x007fffff,0x807fffff,0x00800000,0x80800000],
        [0,0,0,0,0,0,0,0],
    ]
    cases=[]
    for direction in range(2):
        for kind in (0x71,0xf1):
            for index in range(8):
                bits=groups[index % 4]
                outer=([0,0xffffffff,0x7fc00001,0x7f800000,0xff800000,0x80000000,1,0x12345678] if direction==0
                       else [0,0x80000000,0x7f7fffff,0xff7fffff,1,0x80000001,0x3f800000,0xbf800000])[index]
                flags=[0,0xffffffff,0x12345678,0x80000000][index%4]
                oid=[0,0xffffffffffffffff,0x0123456789abcdef,0x8000000000000000][index%4]
                counter=[0,0xffffffff,0x12345678,0x80000000][index%4]
                frame=struct.pack('<HIIIQII',5,0x80ce5e46,flags,kind,oid,outer,counter)
                if kind==0xf1:frame+=struct.pack('<Q',[0,0xffffffffffffffff,0xfedcba9876543210,1][index%4])
                frame+=struct.pack('<8I',*bits)
                assert len(frame)==(62 if kind==0x71 else 70)
                cases.append(dict(Name=f'{direction}-{kind:02x}-{index}',Direction=direction,Hex=frame.hex().upper(),Bits=bits))
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(cases),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round64-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert len(native['Cases'])==len(cases)
    for expected,actual in zip(cases,native['Cases']):
        assert all(expected[key]==actual[key] for key in ('Name','Hex','Bits'))
    receipt=dict(Status='PASS',Synthetic=True,Cases=len(cases),Rejected=native['Rejected'],OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
