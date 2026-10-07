"""Offline pump replay with independently authored transcript and private build artifacts."""
import argparse
import json
import os
import runpy
import subprocess
from pathlib import Path
import transcript

ROOT=Path(__file__).resolve().parents[3]


def main():
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True)
    p.add_argument('--dotnet',type=Path,default=Path('C:/Program Files/dotnet/dotnet.exe'))
    p.add_argument('--packages',type=Path,default=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'))
    a=p.parse_args();out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output,ROOT,directory=True)
    oracle=transcript.generate(out/'peer',0x11223344,0x1122334455667788,'object/creature/player/shared_authored.iff','terrain/authored.trn')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),NUGET_PACKAGES=str(a.packages),
        DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    commands=[[str(a.dotnet),'build',str(ROOT/'poc/round84-tests/Networking/Networking.csproj'),'--artifacts-path',str(out/'artifacts'),
        '--configfile',str(config),'-p:NuGetAudit=false','-p:UseSharedCompilation=false','-p:UseAppHost=false','--nologo'],
        [str(a.dotnet),str(out/'artifacts/bin/Networking/debug/Networking.dll'),str(out/'peer/transcript.json'),str(out/'captured.json'),str(out/'native-receipt.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8',timeout=120)
        (out/f'step-{i}.log').write_text(result.stdout)
        if result.returncode:raise RuntimeError(result.stdout)
        if i==0:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    boundary=transcript.verify(oracle,json.loads((out/'captured.json').read_text()))
    captured=json.loads((out/'captured.json').read_text());python_negatives=0
    for key in ('AuthWire','SelectWire','ReadyWire'):
        bad=dict(captured);bad[key]='FF'+bad[key][2:]
        try:transcript.verify(oracle,bad)
        except ValueError:python_negatives+=1
        else:raise ValueError('Corrupted captured boundary accepted')
    for crc,oid in ((-1,1),(0x100000000,1),(1,0),(1,True)):
        target=out/f'bad-metadata-{python_negatives}'
        try:transcript.generate(target,crc,oid,'object/authored.iff','terrain/authored.trn')
        except ValueError:python_negatives+=1;assert not target.exists()
        else:raise ValueError('Invalid explicit metadata accepted')
    native=json.loads((out/'native-receipt.json').read_text());assert native['Passed'] and not native['SocketCreated']
    receipt=dict(Native=native,IndependentBoundary=boundary,PythonRejections=python_negatives,Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))


if __name__=='__main__':main()
