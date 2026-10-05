"""Independent Fraction nominal-transition oracle; all fixtures are synthetic."""
import argparse,json,random,runpy
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def power(e):return Fraction(1<<e) if e>=0 else Fraction(1,1<<-e)
def decode(bits):
    e=(bits>>23)&255
    if e==255:raise ValueError('Nonfinite F32')
    m=(bits&0x7fffff)|(0x800000 if e else 0);ex=e-150 if e else -149
    return (-(Fraction(m)*power(ex)) if bits>>31 else Fraction(m)*power(ex),bool(bits>>31) if m==0 else False),m,ex
def negative(v):return v[0]<0 or (v[0]==0 and v[1])
def floor2(v):
    n,d=v.numerator,v.denominator;k=n.bit_length()-d.bit_length()
    return k-1 if v<power(k) else k
def nearest(v,quantum):
    scaled=abs(v)/power(quantum);q,r=divmod(scaled.numerator,scaled.denominator)
    if 2*r>scaled.denominator or (2*r==scaled.denominator and q&1):q+=1
    return q
def rounded(v,p):
    if not v[0]:return v
    q=floor2(abs(v[0]))-p+1;n=nearest(v[0],q)
    return (-Fraction(n)*power(q) if v[0]<0 else Fraction(n)*power(q),False)
def multiply(a,b,p):
    z=a[0]*b[0];return rounded((z,negative(a)^negative(b) if not z else False),p)
def add(a,b,p):
    z=a[0]+b[0];return rounded((z,not a[0] and not b[0] and negative(a) and negative(b) if not z else False),p)
def stored(v):
    sign=0x80000000 if negative(v) else 0
    if not v[0]:return sign
    quantum=max(-149,floor2(abs(v[0]))-23);q=nearest(v[0],quantum)
    if not q:return sign
    length=q.bit_length();top=quantum+length-1
    if top>127:raise ValueError('Modern finite candidate overflow rejection')
    if top< -126:return sign|q
    m=q>>(length-24) if length>24 else q<<(24-length)
    return sign|((top+127)<<23)|(m&0x7fffff)
def text(v):return ('-0/1' if v[1] else '0/1') if not v[0] else f'{v[0].numerator}/{v[0].denominator}'
def evaluate(c):
    name=c['Name']
    if c.get('Cancel'):return dict(Name=name,Reject='Canceled')
    try:
        p=c['Precision'];mb=c.get('MagnitudeBits',64);ec=c.get('ExponentCap',512);wc=c.get('WorkCap',8192)
        if p not in (24,53,64) or not 0<=mb<=64 or not 0<=ec<=512 or not 0<=wc<=8192:raise ValueError()
        current,cm,ce=decode(c['Current']);target,tm,te=decode(c['Target']);seconds,sm,se=decode(c['Seconds']);rate=c.get('Rate')
        if rate is not None:
            mag,ex,neg=rate['Magnitude'],rate['Exponent'],rate['Negative']
            if not 0<=mag<1<<64 or mag.bit_length()>mb or not -ec<=ex<=ec:raise ValueError()
        order=(target[0]>current[0])-(target[0]<current[0])
        if order==0:return dict(Name=name,Reject='None',StoredBits=c['Current'],CandidateBits=c['Current'],Retained=text(current),Step=None,Branch='Equal',Clamped=False,Work=0,RatesUsed=0,Provenance='NotUsed')
        if rate is None:raise ValueError()
        product_ex=ex+se;low=min(ce,te,product_ex);high=max(ce+cm.bit_length(),te+tm.bit_length(),product_ex+mag.bit_length()+sm.bit_length()+2)
        work=64+8*(high-low+2)
        if work>wc:raise ValueError()
        rawrate=(-Fraction(mag)*power(ex) if neg else Fraction(mag)*power(ex),neg if mag==0 else False)
        if order<0:rawrate=(-rawrate[0],not rawrate[1] if not rawrate[0] else False)
        step=multiply(rawrate,seconds,p);candidate=add(step,current,p);bits=stored(candidate)
        clamp=candidate[0]>target[0] if order>0 else candidate[0]<target[0]
        return dict(Name=name,Reject='None',StoredBits=c['Target'] if clamp else bits,CandidateBits=bits,Retained=text(candidate),Step=text(step),Branch='Rise' if order>0 else 'Fall',Clamped=clamp,Work=work,RatesUsed=1,Provenance='SuppliedNominal')
    except (ValueError,OverflowError):return dict(Name=name,Reject='Input')
def generate():
    cases=[]
    def put(**changes):
        c=dict(Name=str(len(cases)),Current=0,Target=0x3f800000,Seconds=0x3c23d70a,Precision=64,Rate=dict(Magnitude=1,Exponent=0,Negative=False));c.update(changes);cases.append(c);return c
    pairs=[(0,0x3f800000),(0x40000000,0x3f800000),(0xbf800000,0x80000000),(0x80000000,0xbf800000),(0,0x80000000),(0x80000000,0),(0x3f800000,0x3f800000),(0xbf800000,0xc0000000),(0x7f7fffff,0),(0xff7fffff,0),(1,0),(0,1)]
    times=[0,0x80000000,0x3f800000,0xbf800000,0x3f000000,0x3c23d70a,1,0x7f7fffff]
    rates=[(1,0,False),(1,0,True),(0,0,True),((1<<64)-1,-64,False),((1<<53)+1,-53,False)]
    for p in (24,53,64):
        for current,target in pairs:
            for secs in times:
                for m,e,n in rates:put(Precision=p,Current=current,Target=target,Seconds=secs,Rate=dict(Magnitude=m,Exponent=e,Negative=n))
        put(Precision=p,Current=0x3f800000,Target=0x40000000,Seconds=0x3f800000,Rate=dict(Magnitude=(1<<64)-1,Exponent=-64,Negative=False),U64Carry=True)
        for current,target in ((0,0x3f800000),(0x40000000,0x3f800000)):
            for powerbits in (24,26,53):put(Precision=p,Current=current,Target=target,Seconds=0x3f800000,Rate=dict(Magnitude=(1<<powerbits)+1,Exponent=-powerbits,Negative=False),RetainedWitness=True)
        put(Precision=p,Current=0xbf800000,Target=0x80000000,Seconds=0x3f800000,Rate=dict(Magnitude=1,Exponent=0,Negative=False))
        put(Precision=p,Current=0,Target=0x80000000,Rate=None,WorkCap=0)
        put(Precision=p,Current=0x80000000,Target=0,Rate=None,WorkCap=0)
        for ex in (-512,512):put(Precision=p,Rate=dict(Magnitude=0 if ex>0 else 1,Exponent=ex,Negative=False),ExponentCap=512)
    rng=random.Random(56)
    def finiteword():
        b=rng.getrandbits(32)
        return b^0x00800000 if (b>>23)&255==255 else b
    for p in (24,53,64):
        for _ in range(160):put(Precision=p,Current=finiteword(),Target=finiteword(),Seconds=finiteword(),Rate=dict(Magnitude=rng.getrandbits(64),Exponent=rng.randint(-170,150),Negative=bool(rng.getrandbits(1))))
    base=put();work=evaluate(base)['Work'];put(WorkCap=work,Boundary="work-exact");put(WorkCap=work-1,Boundary="work-one-below")
    put(Rate=dict(Magnitude=(1<<64)-1,Exponent=-64,Negative=False),MagnitudeBits=64,Boundary="magnitude-exact");put(Rate=dict(Magnitude=(1<<64)-1,Exponent=-64,Negative=False),MagnitudeBits=63,Boundary="magnitude-one-below")
    put(Rate=dict(Magnitude=1,Exponent=-512,Negative=False),ExponentCap=512,Boundary="exponent-exact");put(Rate=dict(Magnitude=1,Exponent=-512,Negative=False),ExponentCap=511,Boundary="exponent-one-below")
    for changes in [dict(Current=0x7f800000),dict(Target=0xff800000),dict(Seconds=0x7fc12345),dict(Precision=23),dict(MagnitudeBits=-1),dict(MagnitudeBits=65),dict(ExponentCap=-1),dict(ExponentCap=513),dict(WorkCap=-1),dict(WorkCap=8193),dict(WorkCap=0),dict(Rate=None),dict(Cancel=True),dict(Rate=dict(Magnitude=1,Exponent=513,Negative=False)),dict(Rate=dict(Magnitude=1,Exponent=-513,Negative=False)),dict(Rate=dict(Magnitude=1,Exponent=200,Negative=False),Seconds=0x3f800000),dict(Current=0,Target=0,Rate=dict(Magnitude=1,Exponent=513,Negative=False)),dict(Current=0,Target=0,Seconds=0x7f800000)]:put(**changes)
    # Supplied-rate32, synthetic lead route; no physical original getter is involved.
    current=0
    for i in range(60):
        c=put(Current=current,Target=0x40000000 if i<20 else 0x40c00000 if i<40 else 0,Rate=dict(Magnitude=1,Exponent=5,Negative=False),LeadSyntheticStep=i)
        current=evaluate(c)['StoredBits']
    return cases

def main():
    p=argparse.ArgumentParser();sub=p.add_subparsers(dest='mode',required=True)
    g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True)
    v=sub.add_parser('verify')
    for k in ('cases','native','receipt'):v.add_argument('--'+k,type=Path,required=True)
    a=p.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    if a.mode=='generate':
        dest=guard(a.out,ROOT);cases=generate();dest.write_text(json.dumps(cases,indent=2)+'\n',encoding='utf-8',newline='\n');print(f'GENERATED {len(cases)} synthetic nominal transition cases');return
    dest=guard(a.receipt,ROOT);cases=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());expected=[evaluate(c) for c in cases]
    if len(native['Cases'])!=len(expected):raise ValueError('Case count')
    for c,want,got in zip(cases,expected,native['Cases']):
        if want!=got:raise ValueError(f"Mismatch {c['Name']}: expected{want} actual{got}")
    assert native['Repeated']==100 and native['Threaded']==64 and native['PriorResultPreserved'] and native['NoMutableResultProperties']
    retained=[c['Name'] for c,w in zip(cases,expected) if w['Reject']=='None' and w['Clamped'] and w['CandidateBits']==c['Target']]
    assert len(retained)>=3
    carry=[(c,w) for c,w in zip(cases,expected) if c.get('U64Carry')]
    assert len(carry)==3 and all(w['Retained']=='2/1' and w['StoredBits']==0x40000000 for c,w in carry)
    capcases=[(c,w) for c,w in zip(cases,expected) if 'Boundary' in c]
    assert len(capcases)==6 and all(w['Reject']==('None' if c['Boundary'].endswith('exact') else 'Input') for c,w in capcases)
    report=dict(U64CarryCases=3,ExactCapCases=3,OneBelowCapCases=3,Cases=len(cases),Positive=sum(e['Reject']=='None' for e in expected),Rejected=sum(e['Reject']=='Input' for e in expected),Canceled=sum(e['Reject']=='Canceled' for e in expected),RetainedStoreClampWitnesses=len(retained),Precisions=[24,53,64],Repeated=100,Threaded=64,PriorResultPreserved=True,SyntheticOnly=True,OriginalExecuted=False,LeadSyntheticSteps=60)
    dest.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('FRACTION_PASS '+json.dumps(report))
if __name__=='__main__':main()
