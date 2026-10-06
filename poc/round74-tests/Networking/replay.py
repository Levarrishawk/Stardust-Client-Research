"""Independent synthetic root/datagram/ACK oracle and offline single-frame drain replay."""
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
    cases=[dict(X=-0.0 if i==0 else i*.25,Y=2.,Z=-i*.5,Speed=float(i%3),Flags=[0,0xffffffff,5][i%3],Counter=(0xfffffffc+i)&0xffffffff,Timestamp=(0xfffffff0+i*10)&0xffffffff) for i in range(8)]
    expected=[];datagrams={};acks={}
    for i,c in enumerate(cases):
        application=struct.pack('<HIIIQII',5,0x80ce5e46,c['Flags'],0x71,prior.H,c['Timestamp'],c['Counter'])+struct.pack('<8f',0,0,0,1,c['X'],c['Z'],c['Y'],c['Speed'])
        wire=prior.wire.frame(struct.pack('>HH',9,i)+application).hex().upper();ack=prior.wire.frame(struct.pack('>HH',0x15,i)).hex().upper()
        datagrams['ack'+str(i)]=ack
        expected.append(dict(Sequence=i,ApplicationHex=application.hex().upper(),DatagramHex=wire,AckHex=ack,AcceptedCount=i+1))
    bad=bytearray.fromhex(datagrams['ack0']);bad[-1]^=1
    datagrams.update(badCrc=bad.hex().upper(),wrongSequence=datagrams['ack1'],wrongChannel=prior.wire.frame(struct.pack('>HH',0x16,0)).hex().upper(),wrongOpcode=prior.wire.frame(struct.pack('>HH',0x11,0)).hex().upper(),short='0015',otherAck=prior.wire.frame(struct.pack('>HH',0x17,0)).hex().upper(),smallResponse=struct.pack('>HII3BI',2,0x12345678,prior.wire.SEED,2,1,4,32).hex().upper())
    # Isolated independent model seed override; restore baseline before native.
    originalSeed=prior.wire.SEED
    try:
        prior.wire.SEED=0xDEADBEEF
        alternate=prior.wire.frame(struct.pack('>HH',0x15,0))
    finally:prior.wire.SEED=originalSeed
    datagrams['alternateSeedAck']=alternate.hex().upper()
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Datagrams=datagrams,Acks=acks,Request=struct.pack('>HIII',1,2,0x12345678,496).hex().upper(),Response=struct.pack('>HII3BI',2,0x12345678,prior.wire.SEED,2,1,4,496).hex().upper(),Cases=cases)
    (out/'expected.json').write_text(json.dumps(expected),encoding='utf-8')
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round74-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert native['Receipts']==expected,(native['Receipts'],expected)
    assert native['Failures']==[dict(Kind=k,Retained=3 if k=='fragmented' else 1,BridgeAccepted=1) for k in ['sequenceOrigin','seedMismatch','fragmented']]
    assert native['OriginalAssets']==0
    receipt=dict(Status='PASS',Synthetic=True,NativeChecks=native['Checks'],ExactRoots=8,ExactProtectedFrames=8,ExactAcks=8,PostTransferFailures=3,OracleSha256=hashlib.sha256((out/'expected.json').read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
