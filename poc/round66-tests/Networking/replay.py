"""Independent synthetic combined motion wire routing and pump oracle and offline native replay."""
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
    apps=lifecycle['Applications'].copy()
    def full(counter,kind=0x71):
        frame=struct.pack('<HIIIQI',5,0x80ce5e46,0xf1234567,kind,prior.H,0x80000000)+struct.pack('<I',counter)
        if kind==0xf1:frame+=struct.pack('<Q',0)
        return frame+struct.pack('<8f',1,-2,3,-4,5,7,-6,2)
    def compact(counter,parent=False):
        head=struct.pack('<HI',8,0xc867ab5a if parent else 0x1b24f808)
        return head+(struct.pack('<Q',0) if parent else b'')+struct.pack('<QhhhIbb',prior.H,-32768,9,-7,counter,-128,127)
    for key,b in [('fullFirst',full(0xffffffff)),('compactWrap',compact(0)),('fullEqual',full(0)),('compactOlder',compact(0xffffffff)),('fullNext',full(1)),('fullParent',full(2,0xf1)),('compactParent',compact(2,True))]:apps[key]=b.hex().upper()
    base=full(1)
    apps['malformed']=base[:-1].hex().upper();apps['extra']=(base+b'X').hex().upper();apps['shortOc']=base[:10].hex().upper()
    bad=bytearray(base);struct.pack_into('<H',bad,0,6);apps['badCount']=bad.hex().upper()
    bad=bytearray(base);struct.pack_into('<I',bad,30,0x7fc00001);apps['nonfinite']=bad.hex().upper()
    bad=bytearray(base);struct.pack_into('<I',bad,10,0x99);apps['unknownOc']=bad.hex().upper()
    sequence=['fullFirst','compactWrap','fullEqual','compactOlder','fullNext','fullParent','compactParent','heightA']
    peer=dict(Applications=apps,Initial=lifecycle['Initial'],Sequence=sequence)
    def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
    q=[v/math.hypot(1,-2,3,-4) for v in (1,-2,3,-4)];columns=[]
    for axis in ([1,0,0],[0,1,0],[0,0,1]):
        first=cross(q[:3],axis);second=cross(q[:3],first);columns.append([axis[i]+2*(q[3]*first[i]+second[i]) for i in range(3)])
    fullrows=[columns[c][row] for row in range(3) for c in range(3)]
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round66-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    expected=[('FullTransform',True,'Applied',1,0xffffffff),('Compact',True,None,2,0),('FullTransform',False,'CounterRejected',2,0),('Compact',False,None,None,None),('FullTransform',True,'Applied',3,1),('FullTransform',False,'UnsupportedParent',None,None),('Compact',False,None,None,None),('World',True,None,None,None)]
    assert len(native['Observed'])==len(sequence)
    for key,want,row in zip(sequence,expected,native['Observed']):
        assert row['Key']==key and tuple(row[k] for k in ('Route','Accepted','FullStatus','Revision','Counter'))==want
        if row['Rows'] is not None:
            matrix=fullrows if row['FullStatus']=='Applied' else [1,0,0,0,1,0,0,0,1]
            position=[5,-6,7] if row['FullStatus']=='Applied' else [-8192,-1.75,2.25]
            assert max(abs(a-b) for a,b in zip(matrix,row['Rows']))<2e-7 and row['Position']==position
    assert native['Steps']==[dict(AcceptedMessages=2,DroppedMessages=1,PendingMessages=5),dict(AcceptedMessages=1,DroppedMessages=2,PendingMessages=2),dict(AcceptedMessages=1,DroppedMessages=1,PendingMessages=0)]
    assert len(native['Failures'])==6 and all(row['Closed'] and row['QueueCleared'] and row['PriorRevision']==1 and row['PriorCounter']==0xffffffff for row in native['Failures'])
    assert native['OriginalAssetReads']==0
    receipt=dict(Status='PASS',Synthetic=True,RouteReceipts=len(sequence),NativeChecks=native['Checks'],CleanupCases=6,BasisTolerance=2e-7,OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
