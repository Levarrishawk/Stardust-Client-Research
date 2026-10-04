"""Independent Fraction oracle for bounded supplied retained samples; no native outputs generate fixtures."""
import importlib.util,json,sys,random
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
ah=load('retained_ah','tools/python/verify_terrain_height_fractal.py');a=ah.a
ff=load('retained_ff','tools/python/verify_terrain_filter_fractal.py');ff.setup()
def sample(s):
 k=s['Kind']
 if k=='null':return None
 if k in ('f32','cache'):
  if not a.finite(s['Bits']):raise ValueError('nonfinite')
  return a.scalar(s['Bits']),0 if k=='f32' else 2,s['Bits']
 m=s['Significand'];e=s['Exponent'];z=s['NegativeZero']
 if not -(1<<63)<=m<(1<<63) or not -512<=e<=512 or (m and z):raise ValueError('bounds')
 v=(Fraction(m)*Fraction(2)**e,z);bits=a.bits(v)
 if not a.finite(bits):raise ValueError('projection')
 return v,1,bits
def oracle(c):
 try:s=sample(c['Sample'])
 except ValueError:return dict(Reject='Input')
 p=c['Precision']
 if p not in (24,53,64):return dict(Reject='Options')
 add=lambda x,y:a.precision(a.add(x,y),p)
 sub=lambda x,y:add(x,a.neg(y))
 mul=lambda x,y:a.precision(a.mul(x,y),p)
 empty=dict(Projection=None,Provenance=None)
 if c['Consumer']=='ahfr':
  h,w=c['Height'],c['Influence']
  if not c['Enabled']:return dict(Reject='None',Output=h,Outcome=1,**empty)
  if not a.finite(w):return dict(Reject='Input')
  if a.scalar(w)[0]<=0:return dict(Reject='None',Output=h,Outcome=2,**empty)
  if s is None or not a.finite(h) or not a.finite(c['Scale']):return dict(Reject='Input')
  sv,prov,projection=s;t=mul(sv,a.scalar(c['Scale']));hh,ww=map(a.scalar,[h,w]);op=c['Operation']
  if op==4:return dict(Reject='None',Output=h,Outcome=3,Projection=projection,Provenance=prov)
  if op==1:r=add(hh,mul(t,ww))
  elif op==2:r=sub(hh,mul(t,ww))
  elif op==3:r=add(hh,mul(sub(mul(t,hh),hh),ww))
  else:r=add(hh,mul(sub(t,hh),ww))
  out=a.bits(r)
  return dict(Reject='None',Output=out,Outcome=0,Projection=projection,Provenance=prov) if a.finite(out) else dict(Reject='Input')
 if not c['Enabled']:return dict(Reject='None',Scaled=None,Query=None,Influence=None,Carrier=None,Branch=None,Work=0,OwnedBytes=None,**empty)
 if s is None or not all(a.finite(c[x]) for x in ['Scale','Lower','Upper','Amount']):return dict(Reject='Input')
 sv,prov,projection=s;scaled=a.bits(mul(sv,a.scalar(c['Scale'])))
 if not a.finite(scaled):return dict(Reject='Input')
 f=dict(Version='0005',Limits=ff.QUERY,Action='evaluate',Enabled=c['Enabled'],Precision=p,Combine=p,Sample=scaled,Scale=0x3f800000,Lower=c['Lower'],Upper=c['Upper'],Amount=c['Amount'],Selector=c['Selector'])
 # Independent frozen Fraction filter graph consumes the mandatory scaled F32 store.
 r=ff.evaluate(f)
 if r['Reject']!='None':return dict(Reject='Input')
 return dict(Reject='None',Scaled=scaled,Query=r['Query'],Influence=r['Influence'],Carrier=r['CarrierBits'],Branch=r['Branch'],Projection=projection,Provenance=prov,Work=3,OwnedBytes=32)
def wide(m,e,z=False):return dict(Kind='wide',Significand=m,Exponent=e,NegativeZero=z)
def generate():
 rows=[]
 def add(c):
  c['Name']=f"case-{len(rows)}";c['Expected']=oracle(c)
  c['Hex']=(ah.encoded(family=1,op=c['Operation'],scale=c['Scale'],enabled=c['Enabled']) if c['Consumer']=='ahfr' else ff.encoded(lo=c['Lower'],hi=c['Upper'],selector=c['Selector'],amount=c['Amount'],scale=c['Scale'],family=1,enabled=c['Enabled'])).hex();rows.append(c)
 def pair(c,s):
  c=dict(c,Sample=s);add(c)
  try:hit=dict(Kind='cache',Bits=sample(s)[2])
  except ValueError:return
  add(dict(c,Sample=hit))
 supplied=[wide(16777217,-24),wide(33554431,-25),wide(16777217,-26),wide(33554431,-27),wide(9007199254740993,-53),wide(4503599627370497,-52),wide((1<<62)+(1<<38)+1,-62),wide(-16777217,-24),wide(0,0),wide(0,-512,True),wide(1,-512),wide(-(1<<63),-63),wide(16777215,104)]
 for p in (24,53,64):
  for s in supplied:
   for scale in (0x3f800000,0x3f800001,0xbf800000):
    for op in (0,1,2,3,4,0xffffffff):
     for h,w in [(0xbf800000,0x3f800000),(0x3f800000,0x3f000000),(0x00800001,0x3eaaaaab)]:
      pair(dict(Consumer='ahfr',Precision=p,Height=h,Influence=w,Scale=scale,Operation=op,Enabled=1),s)
    for selector in (0,1,2,3,99):
     for lo,hi,amount in [(0,0x40000000,0x3f000000),(0xbf800000,0x40000000,0x3f800000),(0x3f800000,0x40000000,0x3f800000)]:
      pair(dict(Consumer='ffra',Precision=p,Scale=scale,Lower=lo,Upper=hi,Amount=amount,Selector=selector,Enabled=1),s)
 # Invalid and unused consumer operands. Null is legal only behind an established gate.
 base=dict(Consumer='ahfr',Precision=64,Height=0x7fc00001,Influence=0x3f800000,Scale=0x7fc00001,Operation=0,Enabled=0,Sample=dict(Kind='null'))
 add(base);add(dict(base,Enabled=1,Influence=0));add(dict(base,Enabled=1,Influence=0xbf800000));add(dict(base,Enabled=1,Influence=0x80000000));add(dict(base,Enabled=1,Influence=0x7fc00001));add(dict(base,Enabled=1,Height=0,Scale=0x3f800000));add(dict(base,Precision=32));
 fb=dict(Consumer='ffra',Precision=64,Scale=0x7fc00001,Lower=0x7fc00001,Upper=0,Amount=0x7fc00001,Selector=0,Enabled=0,Sample=dict(Kind='null'))
 add(fb);add(dict(fb,Enabled=1));add(dict(fb,Precision=32))
 for kind in ('f32','cache'):
  for bits in (0,0x80000000,1,0x80000001,0x3f800000,0x7f7fffff):
   add(dict(base,Enabled=1,Height=0,Influence=0x3f800000,Scale=0x3f800000,Sample=dict(Kind=kind,Bits=bits)))
 negatives=[dict(Kind=k,Bits=b) for k in ('f32','cache') for b in (0x7f800000,0xff800000,0x7fc00001,0x7f800001)]
 negatives += [wide(1,e) for e in (-2147483648,-513,513,2147483647)]+[wide(1,0,True),wide(-1,0,True),wide(1,128),wide(1,512),wide(33554431,103),wide(-33554431,103)]
 for s in negatives:add(dict(base,Enabled=1,Height=0,Scale=0x3f800000,Sample=s))
 return dict(Cases=rows,FactoryNegatives=negatives)
def verify(fixture,native):
 fixture=json.loads(Path(fixture).read_text());assert fixture==generate(),'fixture differs from independently generated source'
 actual=json.loads(Path(native).read_text());assert len(actual['Cases'])==len(fixture['Cases'])
 rejects=0;witness={'AHFR':0,'FFRAUnit':0,'FFRAGeneral':0}
 for c,r in zip(fixture['Cases'],actual['Cases']):
  assert r['Name']==c['Name']
  expected=dict(Name=c['Name'],**c['Expected']);assert r==expected,(c['Name'],expected,r)
  rejects+=r['Reject']!='None'
 for left,right in zip(fixture['Cases'],fixture['Cases'][1:]):
  if left['Sample']['Kind']!='wide' or right['Sample']['Kind']!='cache':continue
  a0,b0=left['Expected'],right['Expected']
  if a0['Reject']!='None' or b0['Reject']!='None':continue
  if left['Consumer']=='ahfr' and a0['Output']!=b0['Output']:witness['AHFR']+=1
  if left['Consumer']=='ffra' and a0['Scaled']!=b0['Scaled']:witness['FFRAUnit' if left['Scale']==0x3f800000 else 'FFRAGeneral']+=1
 assert min(witness.values())>0,witness
 assert actual['OwnershipChecks']>=10 and actual['BudgetChecks']>=15 and actual['FactoryNegatives']==len(fixture['FactoryNegatives'])
 print(json.dumps(dict(Cases=len(fixture['Cases']),Rejects=rejects,Witnesses=witness,Ownership=actual['OwnershipChecks'],Budgets=actual['BudgetChecks'],FactoryNegatives=actual['FactoryNegatives'])))
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n');print('Generated bounded retained sample fixtures')
 else:verify(*sys.argv[2:4])
