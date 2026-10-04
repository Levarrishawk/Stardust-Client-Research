"""Synthetic explicit-speed inputs and independent binary32 planar tick oracle."""
import hashlib, json, math, runpy, struct, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def f(x): return struct.unpack('<f',struct.pack('<f',x))[0]
def word(x): return struct.unpack('<I',struct.pack('<f',x))[0]
def generate(path):
    template=runpy.run_path(str(ROOT/'poc/round45-tests/Networking/research_checks.py'))['template']
    baseline=bytearray.fromhex(json.loads((ROOT/'poc/round39-tests/Runtime/ordered-fixtures.json').read_text())['Peer']['baseline'])
    struct.pack_into('<Q',baseline,6,42)
    profiles=[]
    for name,words in [('regular',[word(2),word(6)]),('negative',[word(-1),word(3)]),('signed-zero',[0x80000000,word(4)]),('short-list',[word(5)]),('absent',[]),('unsupported-range',[None,word(2)]),('component-edge',[word(10),word(12)])]:
        parameters={} if not words else {'speed':struct.pack('<i',len(words))+b''.join((b'\x03 '+struct.pack('<2f',1,4) if v is None else b'\x01 '+struct.pack('<I',v)) for v in words)}
        raw=template(parameters)
        profiles.append(dict(Name=name,RootPath='object/root.iff',RootSha256=hashlib.sha256(raw).hexdigest().upper(),Hex=raw.hex().upper(),SpeedWords=words))
    cases=[]
    for pi,p in enumerate(profiles):
        for source in range(1,5):
            for direction in [(1,0),(0,-1),(.6,.8),(0,0),(1,1)]:
                for received in [word(3),0x80000000,word(-2),0x7FC01234,0x7F800000,None]:
                    entries=[] if received is None else [(7,received),(11,received)]
                    if not entries: entries=[(1,word(9))]
                    payload=struct.pack('<H',len(entries))+b''.join(struct.pack('<HI',i,b) for i,b in entries)
                    wire=struct.pack('<HIQIBI',5,0x12862153,42,0x4352454f,4,len(payload))+payload
                    bits=(p['SpeedWords'][source-1] if source<=2 and len(p['SpeedWords'])>=source else None) if source<=2 else received
                    speed=None if bits is None else struct.unpack('<f',struct.pack('<I',bits))[0]
                    d=[f(v) for v in direction]
                    eligible=speed is not None and math.isfinite(speed) and speed>=0 and abs(sum(v*v for v in d)-1)<=.000001
                    v=[f(d[0]*speed),f(d[1]*speed)] if eligible else None
                    eligible=eligible and all(math.isfinite(x) and abs(x)<=10 for x in v)
                    cases.append(dict(Profile=pi,Source=source,Direction=d,Wire=wire.hex().upper(),Bits=bits,ExpectedEligible=eligible))
    Path(path).write_text(json.dumps(dict(Synthetic=True,Baseline=baseline.hex().upper(),Profiles=profiles,Cases=cases),separators=(',',':'))+'\n',encoding='utf-8',newline='\n')
    print('R48_FIXTURES',len(cases))
def verify(fixture,native):
    fixture=json.loads(Path(fixture).read_text());native=json.loads(Path(native).read_text());assert len(native['Cases'])==len(fixture['Cases']); checks=0
    for c,r in zip(fixture['Cases'],native['Cases']):
        assert r['Eligible']==c['ExpectedEligible'],(c,r); checks+=1
        if not r['Eligible']: assert r['Position']==[0,0,0] and r['RawBits'] is None;checks+=2;continue
        assert r['RawBits']==c['Bits'];checks+=1
        speed=struct.unpack('<f',struct.pack('<I',c['Bits']))[0];vx=f(f(c['Direction'][0])*speed);vz=f(f(c['Direction'][1])*speed)
        assert list(map(f,r['Velocity']))==[vx,0,vz];checks+=1
        x=z=f(0)
        for _ in range(4): x=f(x+f(vx*f(.01)));z=f(z+f(vz*f(.01)))
        assert list(map(f,r['Position']))==[x,0,z],(c,r,(x,z));checks+=1
        assert r['SourceCurrent'];checks+=1
    print('R48_INDEPENDENT_PASS cases=%s comparisons=%s nativechecks=%s'%(len(native['Cases']),checks,native['Checks']))
if __name__=='__main__':
    if sys.argv[1]=='generate': generate(sys.argv[2])
    else: verify(sys.argv[2],sys.argv[3])
