"""Independent Fraction/isqrt supplied target oracle; fixtures are synthetic."""
import argparse,json,math,random,runpy
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
f=runpy.run_path(str(ROOT/'poc/round56-tests/Runtime/verify_transition.py'))
decode=f['decode'];mul=f['multiply'];add=f['add'];store=f['stored'];text=f['text'];power=f['power'];floor2=f['floor2']
def sqrt_value(a,p):
    if a[0]<0:raise ValueError('Negative radicand')
    if not a[0]:return a
    quantum=floor2(a[0])//2-p+1;scaled=a[0]/power(2*quantum);n,d=scaled.numerator,scaled.denominator
    q=math.isqrt(n//d);comparison=4*n-d*(2*q+1)**2
    if comparison>0 or (comparison==0 and q&1):q+=1
    return (Fraction(q)*power(quantum),False)
def state(c,reload=False,other_order=False):
    p=c['Precision'];run,rm,re=decode(c['Run']);walk,wm,we=decode(c['Walk']);dirs=[decode(b) for b in c['Direction']]
    selected=run if c['Flag']!=0 and run[0]>0 else walk
    squares=[mul(d[0],d[0],p) for d in dirs]
    summed=add(squares[0],add(squares[1],squares[2],p),p) if other_order else add(add(squares[0],squares[1],p),squares[2],p)
    mag=sqrt_value(summed,p);intermediate=mul(mag,selected,p);snapshot=store(intermediate)
    ff=c['Factor'];m,e,n=ff['Magnitude'],ff['Exponent'],ff['Negative'];factor=(-Fraction(m)*power(e) if n else Fraction(m)*power(e),n if not m else False)
    target=mul(decode(snapshot)[0] if reload else intermediate,factor,p)
    return summed,mag,intermediate,snapshot,target,store(target)
def evaluate(c):
    name=c['Name']
    if c.get('Cancel'):return dict(Name=name,Reject='Canceled')
    try:
        p=c['Precision'];mb=c.get('MagnitudeBits',64);ec=c.get('ExponentCap',512);wc=c.get('WorkCap',65536)
        if p not in (24,53,64) or not 0<=mb<=64 or not 0<=ec<=512 or not 0<=wc<=65536 or not 0<=c['Flag']<=255 or len(c['Direction'])!=3:raise ValueError()
        run,rm,re=decode(c['Run']);walk,wm,we=decode(c['Walk']);dirs=[decode(b) for b in c['Direction']];factor=c['Factor'];m,e,n=factor['Magnitude'],factor['Exponent'],factor['Negative']
        if not 0<=m<1<<64 or m.bit_length()>mb or not -ec<=e<=ec:raise ValueError()
        isrun=c['Flag']!=0 and run[0]>0;selected=c['Run'] if isrun else c['Walk'];_,sm,se=decode(selected)
        low=min(2*d[2] for d in dirs);high=max(2*d[2]+2*d[1].bit_length() for d in dirs)+2
        work=1024+16*(high-low+abs(se)+sm.bit_length()+2*abs(e)+m.bit_length()+p+128)
        if work>wc:raise ValueError()
        s,mag,intermediate,snapshot,target,bits=state(c)
        return dict(Name=name,Reject='None',Selection='Run' if isrun else 'Walk',Selected=selected,Sum=text(s),Magnitude=text(mag),Intermediate=text(intermediate),IntermediateBits=snapshot,RetainedTarget=text(target),TargetBits=bits,Work=work,Counts=[2,3,1,1,8,2],Provenance='SuppliedFiniteInputs')
    except (ValueError,OverflowError):return dict(Name=name,Reject='Input')
def generate():
    cases=[]
    def put(**changes):
        c=dict(Name=str(len(cases)),Run=0x40000000,Walk=0x3f800000,Flag=1,Direction=[0x3f800000,0,0],Factor=dict(Magnitude=1,Exponent=0,Negative=False),Precision=64);c.update(changes);cases.append(c);return c
    values=[0,0x80000000,0x3f800000,0xbf800000,1,0x7f7fffff]
    directions=[[0,0,0],[0x80000000,0,0x80000000],[0x3f800000,0,0],[0x3f800000,0x40000000,0x40400000],[1,1,1],[0x7f7fffff]*3,[0x3f800001,0x3eaaaaab,0x3dcccccd]]
    for p in (24,53,64):
        for flag in (0,1,255):
            for run in values:
                for walk in (0x3f800000,0xbf800000,0x80000000):put(Precision=p,Flag=flag,Run=run,Walk=walk)
        for direction in directions:
            for m,e,n in [(1,0,False),(1,0,True),(0,512,True),((1<<64)-1,-64,False),(1,-512,False)]:put(Precision=p,Direction=direction,Factor=dict(Magnitude=m,Exponent=e,Negative=n))
        put(Precision=p,Run=1,Walk=1,Direction=[1,0,0],Factor=dict(Magnitude=1,Exponent=298,Negative=False),StoreWitness=True)
        put(Precision=p,Run=0,Walk=0x80000001,Direction=[1,0,0],Factor=dict(Magnitude=1,Exponent=298,Negative=True),StoreWitness=True)
        half={24:0x39800000,53:0x323504f3,64:0x2f800000}[p]
        put(Precision=p,Direction=[0x3f800000,half,half],OrderWitness=True)
        put(Precision=p,Run=0,Walk=0,Direction=[0x7f7fffff]*3,Factor=dict(Magnitude=1,Exponent=512,Negative=False))
        put(Precision=p,Direction=[0x7f7fffff]*3,Factor=dict(Magnitude=0,Exponent=0,Negative=False))
        put(Precision=p,Factor=dict(Magnitude=1,Exponent=512,Negative=False))
    rng=random.Random(57)
    def finiteword():
        b=rng.getrandbits(32)
        return b^0x00800000 if (b>>23)&255==255 else b
    for p in (24,53,64):
        for _ in range(160):put(Precision=p,Run=finiteword(),Walk=finiteword(),Flag=rng.choice((0,1,255)),Direction=[finiteword() for _ in range(3)],Factor=dict(Magnitude=rng.getrandbits(64),Exponent=rng.randint(-170,150),Negative=bool(rng.getrandbits(1))))
        # Moderate values probe double-rounding at +154 without forced underflow.
        for _ in range(80):put(Precision=p,Run=0x3f000000+rng.randrange(0x1000000),Walk=0x3f800000,Direction=[0x3d000000+rng.randrange(0x3000000) for _ in range(3)],Factor=dict(Magnitude=rng.getrandbits(64),Exponent=-64,Negative=False))
    base=put();work=evaluate(base)['Work'];put(WorkCap=work,Boundary='work-exact');put(WorkCap=work-1,Boundary='work-one-below')
    put(Factor=dict(Magnitude=(1<<64)-1,Exponent=-64,Negative=False),MagnitudeBits=64,Boundary='magnitude-exact');put(Factor=dict(Magnitude=(1<<64)-1,Exponent=-64,Negative=False),MagnitudeBits=63,Boundary='magnitude-one-below')
    put(Factor=dict(Magnitude=1,Exponent=-512,Negative=False),ExponentCap=512,Boundary='exponent-exact');put(Factor=dict(Magnitude=1,Exponent=-512,Negative=False),ExponentCap=511,Boundary='exponent-one-below')
    for changes in [dict(Run=0x7f800000),dict(Walk=0xff800000),dict(Direction=[0x7fc12345,0,0]),dict(Direction=[0,0]),dict(Direction=[0,0,0,0]),dict(Precision=23),dict(Flag=256),dict(Flag=-1),dict(MagnitudeBits=-1),dict(MagnitudeBits=65),dict(ExponentCap=-1),dict(ExponentCap=513),dict(WorkCap=-1),dict(WorkCap=65537),dict(WorkCap=0),dict(Cancel=True),dict(Factor=dict(Magnitude=1,Exponent=513,Negative=False)),dict(Factor=dict(Magnitude=1,Exponent=-513,Negative=False)),dict(Run=0x7f800000,Flag=0),dict(Walk=0x7f800000,Flag=1)]:put(**changes)
    return cases

def main():
    p=argparse.ArgumentParser();sub=p.add_subparsers(dest='mode',required=True);g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True);v=sub.add_parser('verify')
    for name in ('cases','native','receipt'):v.add_argument('--'+name,type=Path,required=True)
    a=p.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    if a.mode=='generate':
        out=guard(a.out,ROOT);cases=generate();out.write_text(json.dumps(cases,indent=2)+'\n',encoding='utf-8',newline='\n');print(f'GENERATED {len(cases)} synthetic target cases');return
    out=guard(a.receipt,ROOT);cases=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());expected=[evaluate(c) for c in cases]
    if len(native['Cases'])!=len(expected):raise ValueError('Case count')
    storewitness=orderwitness=0
    for c,w,g in zip(cases,expected,native['Cases']):
        if w!=g:raise ValueError(f"Mismatch {c['Name']}: expected{w} actual{g}")
        if w['Reject']=='None':
            if state(c,reload=True)[5]!=w['TargetBits']:storewitness+=1
            if text(state(c,other_order=True)[0])!=w['Sum']:orderwitness+=1
    assert storewitness>=6 and orderwitness>=3
    caps=[(c,w) for c,w in zip(cases,expected) if 'Boundary' in c]
    assert len(caps)==6 and all(w['Reject']==('None' if c['Boundary'].endswith('exact') else 'Input') for c,w in caps)
    positive=sum(w['Reject']=='None' for w in expected)
    assert native['OwnershipChecks']==positive and native['Repeated']==100 and native['Threaded']==64 and native['PriorResultPreserved'] and native['NoMutableResultProperties']
    report=dict(Cases=len(cases),Positive=positive,Rejected=sum(w['Reject']=='Input' for w in expected),Canceled=sum(w['Reject']=='Canceled' for w in expected),RetainedStoreWitnesses=storewitness,SumOrderWitnesses=orderwitness,ExactCapCases=3,OneBelowCapCases=3,OwnershipChecks=positive,Repeated=100,Threaded=64,PriorResultPreserved=True,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('FRACTION_ISQRT_PASS '+json.dumps(report))
if __name__=='__main__':main()
