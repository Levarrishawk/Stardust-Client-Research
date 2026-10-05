"""Independent composed Fraction/isqrt oracle. All inputs are synthetic."""
import argparse, copy, json, random, runpy
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
target = runpy.run_path(str(ROOT/'poc/round57-tests/Runtime/verify_target.py'))
transition = target['f']
decode = transition['decode']

def target_work(s, p):
    run, rm, re = decode(s['Run']); walk, wm, we = decode(s['Walk'])
    ds = [decode(b) for b in s['Direction']]
    f = s['Factor']; m, e = f['Magnitude'], f['Exponent']
    if len(ds) != 3 or not 0 <= s['Flag'] <= 255 or not 0 <= m < 1 << 64 or abs(e) > 512: raise ValueError()
    _, sm, se = decode(s['Run'] if s['Flag'] and run[0] > 0 else s['Walk'])
    low = min(2*d[2] for d in ds); high = max(2*d[2]+2*d[1].bit_length() for d in ds)+2
    return 1024+16*(high-low+abs(se)+sm.bit_length()+2*abs(e)+m.bit_length()+p+128)

def evaluate(c):
    name = c['Name']
    if c.get('Cancel'): return dict(Name=name, Reject='Canceled')
    try:
        p = c['Precision']; ss = c['Steps']; n = len(ss)
        caps = [c.get('MaximumSteps',4096),c.get('MaximumCopyBytes',4194304),c.get('MaximumWork',268435456)]
        if p not in (24,53,64) or any(not 0 <= v <= h for v,h in zip(caps,[65536,67108864,4294967296])): raise ValueError()
        decode(c['Initial'])
        if n > caps[0] or 640*n > caps[1]: raise ValueError()
        reserve = 0
        for s in ss:
            decode(s['Seconds']); r = s.get('Rate')
            if r is not None and (not 0 <= r['Magnitude'] < 1 << 64 or abs(r['Exponent']) > 512): raise ValueError()
            w = target_work(s,p)
            if w > 65536: raise ValueError()
            reserve += w+8192
        if reserve > caps[2]: raise ValueError()
        current=c['Initial']; rows=[]; tw=rw=rates=equals=clamps=0
        for i,s in enumerate(ss):
            t=target['evaluate'](dict(s,Name=name,Precision=p))
            if t['Reject']!='None': raise ValueError()
            r=transition['evaluate'](dict(Name=name,Precision=p,Current=current,Target=t['TargetBits'],Seconds=s['Seconds'],Rate=s.get('Rate')))
            if r['Reject']!='None': raise ValueError()
            for v in (t,r): v.pop('Name'); v.pop('Reject')
            rows.append(dict(Index=i,CurrentBeforeBits=current,SourceMatches=True,Target=t,Transition=r))
            current=r['StoredBits']; tw+=t['Work']; rw+=r['Work']; rates+=r['RatesUsed']; equals+=r['Branch']=='Equal'; clamps+=r['Clamped']
        assert tw+rw <= reserve
        return dict(Name=name,Reject='None',Initial=c['Initial'],Final=current,Rows=rows,Copy=640*n,Reserved=reserve,ActualTarget=tw,ActualTransition=rw,Counts=[n,n,n,rates,equals,clamps,2*n,n-equals+clamps])
    except (ValueError,OverflowError): return dict(Name=name,Reject='Input')

def generate():
    cases=[]
    def step(**kw):
        s=dict(Run=0x40000000,Walk=0x3f800000,Flag=1,Direction=[0x3f800000,0,0],Factor=dict(Magnitude=1,Exponent=0,Negative=False),Seconds=0x3f000000,Rate=dict(Magnitude=1,Exponent=0,Negative=False));s.update(kw);return s
    def put(**kw):
        c=dict(Name=str(len(cases)),Initial=0,Precision=64,Steps=[step(),step(Run=0x3f800000),step(Run=0,Walk=0)]);c.update(kw);cases.append(c);return c
    rng=random.Random(58)
    for p in (24,53,64):
        put(Precision=p,Steps=[]);put(Precision=p,Initial=0x80000000,Steps=[],MaximumSteps=0,MaximumCopyBytes=0,MaximumWork=0)
        put(Precision=p,Steps=[step(Run=0,Walk=0,Rate=None),step(),step(Run=0x3f000000,Rate=None)])
        put(Precision=p,Steps=[step(Run=1,Direction=[1,0,0],Factor=dict(Magnitude=1,Exponent=298,Negative=False),Seconds=0x3f800000),step(Run=0x3f800000,Rate=None)])
        put(Precision=p,Steps=[step(Run=0x3f800000,Seconds=0x3f800000,Rate=dict(Magnitude=(1<<53)+1,Exponent=-53,Negative=False)),step(Run=0x3f800000,Rate=None)])
        put(Precision=p,Steps=[step(Run=0x40000000 if i<20 else 0x40c00000 if i<40 else 0,Walk=0,Seconds=0x3c23d70a,Rate=dict(Magnitude=1,Exponent=5,Negative=False)) for i in range(60)])
        for _ in range(100):
            ss=[]
            for i in range(rng.randint(2,12)):
                ss.append(step(Run=rng.choice([0,1,0x3f800000,0xbf800000,0x40000000,0x40c00000]),Walk=rng.choice([0,0x80000000,0x3f800000,0xbf800000]),Flag=rng.choice([0,1,255]),Direction=rng.choice([[0,0,0],[1,1,1],[0x3f800001,0x3eaaaaab,0x3dcccccd],[0x3f800000,0x40000000,0x40400000]]),Factor=dict(Magnitude=rng.choice([0,1,(1<<64)-1]),Exponent=rng.choice([-64,0,1]),Negative=bool(rng.getrandbits(1))),Seconds=rng.choice([0,0x80000000,0x3f800000,0xbf800000,0x3c23d70a]),Rate=dict(Magnitude=rng.choice([0,1,(1<<53)+1]),Exponent=rng.choice([-53,0,5]),Negative=bool(rng.getrandbits(1)))))
            put(Precision=p,Steps=ss)
    base=put(); work=evaluate(base)['Reserved']
    for k,v in [('MaximumSteps',3),('MaximumCopyBytes',1920),('MaximumWork',work)]:
        put(**{k:v},Boundary='exact');put(**{k:v-1},Boundary='below')
    for k,h in [('MaximumSteps',65536),('MaximumCopyBytes',67108864),('MaximumWork',4294967296)]:
        put(**{k:-1});put(**{k:h+1})
    for kw in [dict(Seconds=0x7f800000),dict(Direction=[0,0x7fc12345,0]),dict(Run=0x7f800000),dict(Walk=0xff800000),dict(Factor=dict(Magnitude=1,Exponent=513,Negative=False)),dict(Rate=dict(Magnitude=1,Exponent=-513,Negative=False)),dict(Rate=None),dict(Factor=dict(Magnitude=1,Exponent=512,Negative=False)),dict(Seconds=0x7f7fffff,Rate=dict(Magnitude=1,Exponent=512,Negative=False))]:
        put(Steps=[step(),step(),step(**kw)],LateFailure=True)
    put(Initial=0x7f800000,Steps=[]);put(Precision=23);put(Cancel=True)
    return cases

def main():
    parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='mode',required=True)
    g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True)
    v=sub.add_parser('verify')
    for key in ('cases','native','receipt'):v.add_argument('--'+key,type=Path,required=True)
    a=parser.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    if a.mode=='generate':
        out=guard(a.out,ROOT);cs=generate();out.write_text(json.dumps(cs,indent=2)+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(cs));return
    out=guard(a.receipt,ROOT);cs=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());expected=[evaluate(c) for c in cs]
    for c,w,g in zip(cs,expected,native['Cases']):
        if w!=g: raise ValueError(f"Mismatch {c['Name']}: expected {w} actual {g}")
    assert len(native['Cases'])==len(expected)
    positive=sum(w['Reject']=='None' for w in expected);steps=sum(len(w.get('Rows',[])) for w in expected)
    assert native['OwnershipChecks']==positive and native['ReadOnlyChecks']==positive and native['ActualReceiptChecks']==steps
    assert native['Repeated']==100 and native['Threaded']==64 and native['PriorResultPreserved']
    boundaries=[(c,w) for c,w in zip(cs,expected) if 'Boundary' in c]
    assert len(boundaries)==6 and all(w['Reject']==('None' if c['Boundary']=='exact' else 'Input') for c,w in boundaries)
    assert all(w['Reject']=='Input' for c,w in zip(cs,expected) if c.get('LateFailure'))
    report=dict(Cases=len(cs),Positive=positive,Rejected=sum(w['Reject']=='Input' for w in expected),Canceled=sum(w['Reject']=='Canceled' for w in expected),AcceptedSteps=steps,OwnershipChecks=positive,ReadOnlyChecks=positive,ActualReceiptChecks=steps,ExactCapCases=3,OneBelowCapCases=3,LateFailures=9,Repeated=100,Threaded=64,PriorResultPreserved=True,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('FRACTION_ISQRT_PASS '+json.dumps(report))
if __name__=='__main__':main()
