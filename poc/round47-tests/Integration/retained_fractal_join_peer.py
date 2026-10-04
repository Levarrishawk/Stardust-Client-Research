"""Independent supplied retained-sample join; no original noise/cache execution."""
import importlib.util,json,random,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('retained_join_ah',ROOT/'tools/python/verify_terrain_height_fractal.py');ah=importlib.util.module_from_spec(spec);spec.loader.exec_module(ah)
spec=importlib.util.spec_from_file_location('retained_join_ff',ROOT/'tools/python/verify_terrain_filter_fractal.py');ff=importlib.util.module_from_spec(spec);spec.loader.exec_module(ff);ff.setup()
a=ah.a;l=ff.l

def word(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def raw(sample):
    v=Fraction(sample['Magnitude'])*Fraction(2)**sample['Exponent']
    return (-v if sample['Negative'] else v,sample['Magnitude']==0 and sample['Negative'])
def source(v):
    q,negzero=v
    return dict(Magnitude=abs(q.numerator),Exponent=-(q.denominator.bit_length()-1),Negative=q<0 or negzero)
def height(c,sample,height_bits,influence,op,scale):
    p=c['Precision'];h=a.scalar(height_bits);w=a.scalar(influence)
    if w[0]<=0:return height_bits
    add=lambda u,v:a.precision(a.add(u,v),p)
    sub=lambda u,v:add(u,a.neg(v))
    mul=lambda u,v:a.precision(a.mul(u,v),p)
    t=mul(sample,a.scalar(scale))
    if op==1:r=add(h,mul(t,w))
    elif op==2:r=sub(h,mul(t,w))
    elif op==3:r=add(h,mul(sub(mul(t,h),h),w))
    elif op==4:r=h
    else:r=add(h,mul(sub(t,h),w))
    return a.bits(r)
def query(c,sample):
    scaled=a.bits(a.precision(a.mul(sample,a.scalar(c['FilterScale'])),c['QueryPrecision']))
    f=dict(Version='0002',Lower=0,Upper=word(2),Amount=word(.75),Height=scaled,Precision=c['QueryPrecision'],Limits=ff.fh.QUERY,Action='query',Selector=c['Selector'],Enabled=1)
    v=ff.old.query_number(f);assert not isinstance(v,dict)
    return v,scaled

def oracle(c,cache=False):
    v=raw(c['Sample']);cache_bits=a.bits(v)
    if cache:v=a.scalar(cache_bits)
    first=height(c,v,c['Initial'],word(1),c['Operation'],c['Scale'])
    u,scaled=query(c,v)
    candidate=l.shape(u,c['Selector'],c['CombinePrecision'])
    local=a.scalar(word(1))
    if candidate[0]<local[0]:local=a.scalar(a.bits(candidate))
    product=a.bits(a.precision(a.mul(local,a.scalar(c['Weight'])),c['ProductPrecision']))
    final=height(c,v,first,product,1,word(.5)) if local[0]>0 else first
    return dict(Name=c['Name'],CacheBits=cache_bits,First=first,Scaled=scaled,Query=a.bits(u),Local=a.bits(local),Product=product,Final=final)

def generate():
    rng=random.Random(470046);pairs=[(0x3f800001,0x3f800001),(0x3eaaaaab,0x3faaaaab),(0x3f000001,0x3f000001)]+[(rng.randrange(0x3e800000,0x3f800000),rng.randrange(0x3e800000,0x3f800000)) for _ in range(125)]
    cases=[]
    for p in (24,53,64):
      for i,(x,y) in enumerate(pairs):
       v=a.precision(a.mul(a.scalar(x),a.scalar(y)),p)
       for fs in (word(1),0x3f800001):
        c=dict(Name=f'p{p}-n{i}-scale{fs}',Sample=source(v),Precision=p,QueryPrecision=53,CombinePrecision=64,ProductPrecision=24,Initial=word(-.25),Operation=i%5,Scale=word(15),FilterScale=fs,Selector=i%4,Weight=word(.75))
        c.update(RootHex=ah.encoded(1,c['Operation'],c['Scale']).hex(),FilterHex=ff.encoded(scale=fs,selector=c['Selector'],amount=word(.75)).hex(),ChildHex=ah.encoded(1,1,word(.5)).hex());cases.append(c)
    # Supplied values straddling an F32 midpoint at precision wider than binary53.
    # Unit scale can double-round here; the earlier 48-bit products cannot show it.
    for p in (24,53,64):
      for e in (-54,-55,-62):
       v=(Fraction(2)**(e+54)+Fraction(2)**(e+30)+Fraction(2)**e,False)
       for fs in (word(1),0x3f800001):
        c=dict(Name=f'wide-midpoint-{p}-{e}-{fs}',Sample=source(v),Precision=p,QueryPrecision=53,CombinePrecision=64,ProductPrecision=24,Initial=word(-.25),Operation=0,Scale=word(15),FilterScale=fs,Selector=0,Weight=word(.75))
        c.update(RootHex=ah.encoded(1,c['Operation'],c['Scale']).hex(),FilterHex=ff.encoded(scale=fs,selector=0,amount=word(.75)).hex(),ChildHex=ah.encoded(1,1,word(.5)).hex());cases.append(c)
    return dict(Version=1,Synthetic=True,OriginalNoise=False,Cases=cases)

def verify(fixture,native,receipt):
    d=json.loads(Path(fixture).read_text());assert d==generate();rows=json.loads(Path(native).read_text());assert len(rows)==len(d['Cases'])
    ahw=ffw=final=0;scale1=0
    for c,r in zip(d['Cases'],rows):
      miss=oracle(c);hit=oracle(c,True);assert r==dict(Miss=miss,Hit=hit),(c['Name'],r,miss,hit)
      ahw+=miss['First']!=hit['First'];ffw+=miss['Scaled']!=hit['Scaled'];final+=miss['Final']!=hit['Final']
      if c['FilterScale']==word(1):scale1+=miss['Scaled']!=hit['Scaled']
    assert ahw>0 and ffw>0 and final>0 and scale1>0
    r=dict(Passed=True,Cases=len(rows),AhfrMissHitWitnesses=ahw,FfraMissHitWitnesses=ffw,FinalMissHitWitnesses=final,SuppliedUnitScaleSpillWitnesses=scale1,OriginalNoise=False,OriginalTerrainParity=False)
    Path(receipt).write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':
    if sys.argv[1]=='generate':
      d=generate();Path(sys.argv[2]).write_text(json.dumps(d,indent=2)+'\n');expected=[dict(Miss=oracle(c),Hit=oracle(c,True)) for c in d['Cases']];print(json.dumps(dict(Cases=len(expected),Ahfr=sum(x['Miss']['First']!=x['Hit']['First'] for x in expected),Ffra=sum(x['Miss']['Scaled']!=x['Hit']['Scaled'] for x in expected),Final=sum(x['Miss']['Final']!=x['Hit']['Final'] for x in expected))))
    else:verify(*sys.argv[2:5])
