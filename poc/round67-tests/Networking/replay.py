"""Independent synthetic offline outbound current local motion oracle and offline native replay."""
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
    def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
    def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
    def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
    def rows(q):
        q=[v/math.hypot(*q) for v in q];columns=[]
        for axis in ([1,0,0],[0,1,0],[0,0,1]):
            first=cross(q[:3],axis);second=cross(q[:3],first);columns.append([axis[i]+2*(q[3]*first[i]+second[i]) for i in range(3)])
        return [columns[c][row] for row in range(3) for c in range(3)]
    # Independent symmetric eigenvector solve, not native trace/max-diagonal conversion.
    def eigen_quaternion(m):
        a,b,c,d,e,f,g,h,i=m
        k=[[a-e-i,b+d,c+g,h-f],[b+d,e-a-i,f+h,c-g],[c+g,f+h,i-a-e,d-b],[h-f,c-g,d-b,a+e+i]]
        v=[[float(row==col) for col in range(4)] for row in range(4)]
        for iteration in range(80):
            p,q=max(((p,q) for p in range(4) for q in range(p+1,4)),key=lambda pair:abs(k[pair[0]][pair[1]]))
            if abs(k[p][q])<1e-15:break
            angle=.5*math.atan2(2*k[p][q],k[q][q]-k[p][p]);cs=math.cos(angle);sn=math.sin(angle)
            # Orthogonal two-column Jacobi similarity.
            for row in range(4):k[row][p],k[row][q]=cs*k[row][p]-sn*k[row][q],sn*k[row][p]+cs*k[row][q]
            for col in range(4):k[p][col],k[q][col]=cs*k[p][col]-sn*k[q][col],sn*k[p][col]+cs*k[q][col]
            for row in range(4):v[row][p],v[row][q]=cs*v[row][p]-sn*v[row][q],sn*v[row][p]+cs*v[row][q]
        col=max(range(4),key=lambda j:k[j][j]);quat=[v[row][col] for row in range(4)]
        selector=3 if a+e+i>0 else max(range(3),key=lambda j:m[j*4])
        if quat[selector]<0:quat=[-value for value in quat]
        return [f32(value/math.hypot(*quat)) for value in quat]
    definitions=[('identity',[0,0,0,1]),('yaw90',[0,-math.sqrt(.5),0,math.sqrt(.5)]),('x180',[1,0,0,0]),('y180',[0,1,0,0]),('z180',[0,0,1,0]),('asymmetric',[.5,.5,.5,.5]),('near180',[1,0,0,2**-12])]
    cases=[];expected={}
    def expected_frame(name,matrix,position,speed,flags,counter,timestamp):
        quat=eigen_quaternion(matrix)
        frame=struct.pack('<HIIIQII',5,0x80ce5e46,flags,0x71,prior.H,timestamp,counter)+struct.pack('<8f',*(quat+[position[0],position[2],position[1],speed]))
        expected[name]=dict(Hex=frame.hex().upper(),Rows=matrix,Quaternion=quat)
    for index,(name,q) in enumerate(definitions):
        matrix=[f32(v) for v in rows(q)]
        # Axis fixtures use exact integer matrices; independently authored near-axis trigonometry otherwise.
        if name=='yaw90':matrix=[0,0,-1,0,1,0,1,0,0]
        position=[-0.0,5,-7] if index==0 else [1.25,-2.5,3.75]
        speed=-0.0 if index==0 else 2.0
        flags=[0,0xffffffff,0x89abcdef][index%3];counter=[0xffffffff,0,0x80000000][index%3];timestamp=[0x7fc00001,0x7f800000,0xff800000,0xffffffff,0,1,0x12345678][index]
        cases.append(dict(Name=name,RowBits=[bits(v) for v in matrix],PositionBits=[bits(v) for v in position],SpeedBits=bits(speed),Flags=flags,Counter=counter,Timestamp=timestamp))
        expected_frame(name,matrix,position,speed,flags,counter,timestamp)
    expected_frame('local-driver',[0,0,1,0,1,0,-1,0,0],[f32(f32(2)*f32(.01)),0,0],2,0x89abcdef,0xffffffff,0x7fc00001)
    expected_frame('terrain-driver',[1,0,0,0,1,0,0,0,1],[0,2,f32(f32(2)*f32(.01))],2,0xfedcba98,0,0xff800000)
    peer=dict(Applications=lifecycle['Applications'],Initial=lifecycle['Initial'],Cases=cases)
    fixture=out/'synthetic-oracle.json';fixture.write_text(json.dumps(peer),encoding='utf-8')
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=os.environ.copy();env.update(APPDATA=str(out/'appdata'),DOTNET_CLI_HOME=str(out/'dotnet-home'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false',NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages')
    dotnet='C:/Program Files/dotnet/dotnet.exe';project=root/'poc/round67-tests/Networking/Networking.csproj'
    commands=[[dotnet,'restore',str(project),'--configfile',str(config),'-p:NuGetAudit=false'],
              [dotnet,'build',str(project),'--no-restore','-p:UseSharedCompilation=false','-p:UseAppHost=false','-p:NuGetAudit=false'],
              [dotnet,str(project.parent/'bin/Debug/net8.0/Networking.dll'),str(fixture),str(out/'native.json')]]
    for i,cmd in enumerate(commands):
        result=subprocess.run(cmd,cwd=root,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,encoding='utf-8')
        (out/f'step-{i}.txt').write_text(result.stdout,encoding='utf-8')
        if result.returncode:raise RuntimeError(result.stdout)
        if i==1:assert '0 Warning(s)' in result.stdout and '0 Error(s)' in result.stdout
    native=json.loads((out/'native.json').read_text())
    assert len(native['Cases'])==len(expected)
    for actual in native['Cases']:
        want=expected[actual['Name']];frame=bytes.fromhex(actual['Hex']);anticipated=bytes.fromhex(want['Hex'])
        assert frame[:30]==anticipated[:30] and frame[46:]==anticipated[46:],(actual['Name'],'Exact header/position/speed')
        q=struct.unpack_from('<4f',frame,30);assert [bits(v) for v in q]==actual['QuaternionBits'];basis=rows(q)
        assert max(abs(a-b) for a,b in zip(basis,want['Rows']))<2e-7,(actual['Name'],basis,want['Rows'])
        # No arbitrary sign choice: compare basis; exact whole-frame pins cover stable authored axis/asymmetric cases.
        if actual['Name']!='near180':
            assert frame==anticipated,(actual['Name'],actual['Hex'],want['Hex'])
    assert native['ResourceLoads']==0
    receipt=dict(Status='PASS',Synthetic=True,Cases=len(expected),ExactWholeFrames=len(expected)-1,NativeChecks=native['Checks'],BasisTolerance=2e-7,OracleSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),Commands=commands)
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
if __name__=='__main__':main()
