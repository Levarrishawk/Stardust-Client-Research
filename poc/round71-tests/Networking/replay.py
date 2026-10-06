"""Independent synthetic offline SOE-to-pump ingress and correction oracle and offline native replay."""
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
    rootframe=struct.pack('<HIIIQII',5,0x80ce5e46,0x12345678,0x71,prior.H,0,33)+struct.pack('<8f',.5,.5,.5,.5,1.25,-3.75,2,4)
    assert len(rootframe)==62
    def reliable(sequence,payload):return prior.wire.frame(struct.pack('>HH',9,sequence)+payload).hex().upper()
    multi=b'\x00\x19'+bytes([62])+rootframe+bytes([62])+rootframe
    datagrams=dict(correction=reliable(0,rootframe),duplicateApplication=reliable(1,rootframe),multi=reliable(0,multi),malformed=reliable(1,rootframe[:-1]),suffix=reliable(2,rootframe))
    bad=bytearray.fromhex(datagrams['correction']);bad[-1]^=1;datagrams['badCrc']=bad.hex().upper();datagrams['shortFraming']='000900'
    disconnect=struct.pack('>HIH',5,0x12345678,6)
    datagrams.update(disconnect=prior.wire.frame(disconnect).hex().upper(),wrongConnection=prior.wire.frame(struct.pack('>HIH',5,0x87654321,6)).hex().upper(),shortPacket=prior.wire.frame(disconnect[:-1]).hex().upper(),alternateReason=prior.wire.frame(disconnect[:-2]+b'\xFE\xDC').hex().upper())
    bad=bytearray.fromhex(datagrams['disconnect']);bad[-1]^=1;datagrams['badCrc']=bad.hex().upper()
    acks={key:prior.wire.frame(struct.pack('>HH',0x15,sequence)).hex().upper() for key,sequence in [('correction',0),('duplicateSoe',0),('duplicateApplication',1),('malformed',1),('suffix',2)]}
    request=struct.pack('>HIII',1,2,0x12345678,496).hex().upper();response=struct.pack('>HII3BI',2,0x12345678,prior.wire.SEED,2,1,4,496).hex().upper()
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams=datagrams,Acks=acks,Request=request,Response=response)
    def f32(value):return struct.unpack('<f',struct.pack('<f',value))[0]
    def bits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
    expectedMotion=dict(RawHex=rootframe.hex().upper(),Accepted=1,Dropped=1,AuthoritativeRevision=3,LocalRevision=4,PositionBits=[bits(f32(1.25+f32(f32(2)*f32(.01)))),bits(2),bits(-3.75)],SpeedBits=bits(2),Rows=[0,0,1,0,1,0,-1,0,0])
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round71-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['DisconnectHex']==datagrams['disconnect']
    assert native['ExactControls']==1 and native['DriverSteps']==1 and native['NegativePackets']==4 and native['PendingRootPackets']==2 and native['JobsDrained']==2 and native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,Native=native,RawDisconnectHex=disconnect.hex().upper(),OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
