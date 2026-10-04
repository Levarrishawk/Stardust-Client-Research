"""Independent supplied analytic surfaces, binary32 motion and integer schedule oracle."""
import json, math, runpy, struct, sys, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def f(v): return struct.unpack('<f',struct.pack('<f',v))[0]
def plane(lo=-4,hi=4,a=0,b=0,c=0,bad=False):
    return dict(Lo=lo,Hi=hi,A=a,B=b,C=c,BadNormal=bad)
def generate(path):
    with tempfile.TemporaryDirectory() as tmp:
        old=sys.argv;sys.argv=['Generate-Peer.py',str(Path(tmp)/'peer.json')]
        try: runpy.run_path(str(ROOT/'poc/round14-tests/Runtime/Generate-Peer.py'),run_name='__main__')
        finally: sys.argv=old
        peer=json.loads((Path(tmp)/'peer.json').read_text())
    create=bytearray.fromhex(peer['create']);struct.pack_into('<7f',create,14,0,0,0,1,0,0,0);peer['create']=create.hex().upper()
    cases=[]
    transitions=[[0,2,0],[15500,0,4],[65001,-3,0],[100003,3,0],[100003,3,4],[211111,0,0],[271234,0,-2],[350000,0,0]]
    def case(name,surfaces,inputs=transitions,initial=[0,0,0],offset=.25,updates=None,maxsteps=8,budget=512):
        cases.append(dict(Name=name,Surfaces=surfaces,Initial=initial,Offset=offset,Inputs=inputs,
            Updates=updates or [dict(Time=400000,Active=True)],MaximumSteps=maxsteps,Budget=budget))
    for slopes in [(0,0,2),(.5,-.25,1),(-.25,.5,-2)]:
        for partition in [1000,27000,31000]:
            updates=[dict(Time=t,Active=True) for t in range(partition,400000,partition)]+[dict(Time=400000,Active=True)]
            case(f'plane-{slopes}-{partition}',[plane(a=slopes[0],b=slopes[1],c=slopes[2])],updates=updates)
    case('seam',[plane(-2,0,c=1),plane(0,2,a=.5,c=3)],[[0,10,0]],[-.25,0,0],updates=[dict(Time=t,Active=True) for t in range(10000,80001,10000)])
    case('closed-seam-first',[plane(-2,0,c=1),plane(0,2,c=99)],[],updates=[dict(Time=10000,Active=True)])
    case('overlap-first',[plane(c=7),plane(c=99)],[[0,1,1]])
    case('negative-offset',[plane(a=.5,b=-.25,c=2)],offset=-1,updates=[dict(Time=t,Active=True) for t in [15000,20000,100000]])
    case('paused',[plane(a=.5,b=.25,c=1)],[[0,2,0],[15500,0,4],[25000,-3,0]],updates=[dict(Time=t,Active=a) for t,a in [(15000,True),(20000,False),(100000,False),(110000,True),(120000,True)]])
    case('stall',[plane(a=.5,b=-.25)],[[0,1,0],[45000,0,2]],updates=[dict(Time=t,Active=True) for t in [200007,210000]],maxsteps=2)
    for partition in [1000,27000,31000]:
        updates=[dict(Time=t,Active=True) for t in range(partition,40000,partition)]
        updates += [dict(Time=40000,Active=True),dict(Time=40000,Active=True,Correction=[1,77,1,7])]
        updates += [dict(Time=t,Active=True) for t in range(40000+partition,100000,partition)]+[dict(Time=100000,Active=True)]
        case(f'correction-{partition}',[plane(a=.5,b=-.25,c=1)],[[0,1,0],[45001,0,2]],updates=updates)
    case('paused-correction',[plane(a=.5)],[[0,1,0],[50000,0,2]],updates=[dict(Time=15000,Active=True),dict(Time=20000,Active=False),dict(Time=100000,Active=False,Correction=[1,77,1,8]),dict(Time=110000,Active=True),dict(Time=120000,Active=True)])
    for failure in ['outside','budget','normal']:
        surfaces=[plane(-1,.025)]+([] if failure=='outside' else [plane(.025,1,bad=failure=='normal')])
        case(f'late-{failure}',surfaces,[[0,1,0]],offset=0,updates=[dict(Time=80000,Active=True),dict(Time=90000,Active=True)],budget=8 if failure=='budget' else 16)
    result=dict(Peer=peer,Cases=cases)
    Path(path).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Generated',len(cases),'independent analytic/schedule sequences')
def oracle(case):
    x,y,z=map(f,case['Initial']);offset=f(case['Offset']);rowx=[1,0,0];rowz=[0,0,1];speed=0
    def ground(x,y,z):
        reserved=0
        for p in case['Surfaces']:
            reserved+=8
            if reserved>case['Budget']:raise ValueError('budget')
            if f(p['Lo'])<=x<=f(p['Hi']) and -4<=z<=4:
                if p['BadNormal']:raise ValueError('normal')
                # Independent analytic plane; does not call native barycentric query or use its receipts.
                return f(p['A']*x+p['B']*z+p['C']+offset)
        raise ValueError('outside')
    y=ground(x,y,z);rev=2;authoritative=False;time=0;pending=0;dropped=0;suspended=0;active=True;fault=None;index=0;vx=vz=0
    def select(t):
        nonlocal index,vx,vz
        while index<len(case['Inputs']) and case['Inputs'][index][0]<=t:
            _,vx,vz=case['Inputs'][index];vx=f(vx);vz=f(vz);index+=1
    def record(steps): return dict(X=x,Y=y,Z=z,RowX=rowx[:],RowY=[0,1,0],RowZ=rowz[:],Speed=speed,TimestampMicroseconds=time,StepsThisUpdate=steps,DroppedMicroseconds=dropped,SuspendedMicroseconds=suspended,Active=active and fault is None,FaultType=fault,MotionRevision=rev,Authoritative=authoritative)
    records=[record(0)]
    for update in case['Updates']:
        now=update['Time'];requested=update['Active'] and fault is None
        if 'Correction' in update and fault is None:
            x,y,z,speed=map(f,update['Correction']);rowx=[1,0,0];rowz=[0,0,1];rev+=1;authoritative=True
        available=pending+now-time;completed=0
        if not requested or not active or fault:
            suspended+=available;pending=0;active=requested
        else:
            planned=min(available//10000,case['MaximumSteps']);remaining=available-planned*10000
            for ordinal in range(planned):
                select(time-pending+ordinal*10000)
                nx=f(x+f(vx*f(.01)));nz=f(z+f(vz*f(.01)))
                try:ny=ground(nx,y,nz)
                except ValueError:
                    fault='InvalidDataException';dropped+=available-completed*10000;pending=0;break
                x,y,z=nx,ny,nz;speed=f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
                if speed>0:
                    sine=f(vx/speed);cosine=f(vz/speed);rowx=[cosine,0,sine];rowz=[-sine,0,cosine]
                rev+=1;authoritative=False;completed+=1
            else:
                drop=remaining//10000*10000;dropped+=drop;pending=remaining-drop
        time=now;select(now-pending);records.append(record(completed))
    return records
def verify(fixture,receipt):
    cases=json.loads(Path(fixture).read_text())['Cases'];actual=json.loads(Path(receipt).read_text(),parse_int=lambda s:-0.0 if s=='-0' else int(s));assert actual['Result']=='PASS' and actual['ResourceLoads']==actual['AssetReads']==0
    assert len(cases)==len(actual['Cases']);comparisons=0
    for case,entry in zip(cases,actual['Cases']):
        assert case['Name']==entry['Name'];expected=oracle(case);assert len(expected)==len(entry['Records'])
        for exp,got in zip(expected,entry['Records']):
            for key,value in exp.items():
                if key in ['X','Y','Z','Speed']:
                    assert math.isclose(value,got[key],rel_tol=2e-6,abs_tol=2e-6),(case['Name'],key,value,got[key])
                elif key.startswith('Row'):
                    assert all(math.isclose(a,b,rel_tol=2e-6,abs_tol=2e-6) for a,b in zip(value,got[key])),(case['Name'],key,value,got[key])
                else: assert value==got[key],(case['Name'],key,value,got[key])
                comparisons+=1
    # Same tick-start histories and correction boundaries must produce identical exported poses.
    for prefix in ['plane-(0, 0, 2)','plane-(0.5, -0.25, 1)','plane-(-0.25, 0.5, -2)','correction']:
        finals=[e['Records'][-1] for e in actual['Cases'] if e['Name'].startswith(prefix)]
        assert len(finals)==3
        for key in ['X','Y','Z','RowX','RowY','RowZ','Speed','MotionRevision']:
            def bits(value):
                if key=='MotionRevision':return value
                return b''.join(struct.pack('<f',component) for component in value) if isinstance(value,list) else struct.pack('<f',value)
            assert all(bits(v[key])==bits(finals[0][key]) for v in finals),(prefix,key)
    print(json.dumps(dict(passed=True,sequences=len(cases),comparisons=comparisons,native_checks=actual['Checks']),indent=2))
if __name__=='__main__':
    if sys.argv[1]=='generate':generate(sys.argv[2])
    elif sys.argv[1]=='verify':verify(sys.argv[2],sys.argv[3])
    else:raise SystemExit('generate fixture | verify fixture receipt')
