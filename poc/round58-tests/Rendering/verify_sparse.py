"""Independent Fraction kernel/base, 100-digit Decimal curve, sparse ordering and zero-work empty model."""
import argparse,hashlib,importlib.util,json,os,stat,struct,sys
from decimal import Decimal,localcontext
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
def copy(c):return json.loads(json.dumps(c))
def account(c):
 shape=c['Width']*c['Rows'];count=len(c['Requests'])
 return [696+176*count,10088+32*shape+1600*count if count else 440,20424+shape+8288*count if count else 168,16*shape+1440*count if count else 0,shape+8256*count if count else 0]
def expected(c,mode,f):
 try:
  width,rows=c['Width'],c['Rows'];shape=width*rows;count=len(c['Requests']);a=account(c)
  spec=[('MaximumCells',1024,1024),('MaximumRequests',256,1024),('MaximumInputBytes',84,65536),('MaximumCopyBytes',1048576,4194304),('MaximumRouteBytes',4194304,16777216),('MaximumWork',16777216,67108864),('MaximumQueryBytes',2097152,2097152),('MaximumQueryWork',16777216,16777216)]
  for k,d,m in spec:
   if not 0<=c.get(k,d)<=m:raise ValueError('Limit range')
  if not(1<=width<=128 and 1<=rows<=128 and shape<=c.get('MaximumCells',1024) and count<=shape and count<=c.get('MaximumRequests',256)):raise ValueError('Shape/subset count')
  raw=bytes.fromhex(c['Hex'])
  if len(raw)!=84 or len(raw)>c.get('MaximumInputBytes',84):raise ValueError('Raw extent')
  for (key,default,_),required in zip(spec[3:],a):
   if required>c.get(key,default):raise ValueError('Preflight')
  if c.get('Cancel') or c.get('Policy',0)!=0 or c.get('NullControls'):raise ValueError('Policy/controls/cancel')
  for key in ('Table','Construction','Query'):mode.n.arithmetic(c[key])
  seen=set()
  for r in c['Requests']:
   if not(0<=r['Column']<width and 0<=r['Row']<rows):raise ValueError('Index')
   index=r['Row']*width+r['Column']
   if index in seen:raise ValueError('Duplicate cell')
   seen.add(index);mode.n.finite_scalar(r['XBits']);mode.n.finite_scalar(r['ZBits'])
  words=list(struct.unpack_from('<13I',raw,32))
  if raw!=mode.b.frame(words):raise ValueError('Envelope')
  parameter=mode.n.finite_scalar(words[2])[0]
  if not(words[1] and not words[3] and 0<parameter<1):raise ValueError('Curve raw/domain')
  if not(words[12]<=5 and 1<=words[5]<=16 and words[0]!=0x7fffffff):raise ValueError('Kernel raw options')
  for i in range(6,12):mode.n.finite_scalar(words[i])
  derived=bytearray(raw);derived[36:40]=bytes(4);cells=[];powers=0
  if count:
   model=mode.Model(dict(Hex=derived.hex(),Width=width,Rows=rows,TablePrecision=c['Table'],ConstructionPrecision=c['Construction']))
   with localcontext() as context:
    context.prec=100;exponent=f.dec(parameter).ln()/Decimal('.5').ln()
    for i,r in enumerate(c['Requests']):
     retained=model.miss(r,c['Query']);basis=mode.b.stored(retained);base=mode.n.finite_scalar(basis)[0]
     if not 0<=base<=1:raise ValueError('Base domain')
     if base==0:out=Decimal(0)
     elif base==1:out=Decimal(1)
     else:out=(f.dec(base).ln()*exponent).exp();powers+=1
     bits=struct.unpack('<Q',struct.pack('<d',float(out)))[0];_,power=f.dyadic(f.f64(bits))
     if not -512<=power<=512:raise ValueError('Frozen exact-export exponent')
     cells.append(dict(RequestIndex=i,Index=r['Row']*width+r['Column'],**r,Kernel=str(retained[0]),KernelNegative=retained[1],Base=basis,ExpectedExponent=str(exponent),ExpectedOutput=str(out)))
  octaves=words[5];has=int(count>0)
  return dict(Name=c['Name'],Reject='None',Width=width,Rows=rows,ShapeCells=shape,SuppliedRequests=count,OriginalSha256=f.sha(raw),DerivedSha256=f.sha(derived),OriginalHex=raw.hex().upper(),DerivedHex=derived.hex().upper(),Cells=cells,Counts=[has,has,has,count,count*octaves,count,2*has,powers,count-powers],LogicalCopyBytes=a[0],ReservedRouteBytes=a[1],ReservedWork=a[2],ReservedQueryBytes=a[3],ReservedQueryWork=a[4],AccountedRouteBytes=10088+32*shape+count*(320+80*octaves) if count else 440,AccountedWork=20424+shape+count*(96+512*octaves) if count else 168)
 except (ValueError,ZeroDivisionError,OverflowError,IndexError) as error:return dict(Name=c['Name'],Reject='Input',Reason=str(error))
def generate(mode,f,private):
 dense=f.generate(mode,private);cases=[]
 for old in dense:
  c=copy(old);c['Requests'].reverse()
  if 'MaximumSamples' in c:c['MaximumRequests']=c.pop('MaximumSamples')
  cases.append(c)
 # All six modes/all27 profiles: sparse, shuffled complete, and empty counterparts.
 for original in cases[:162]:
  c=copy(original);c['Requests']=[c['Requests'][i] for i in (1,3,0)];cases.append(c)
  c=copy(original);c['Requests']=[c['Requests'][i] for i in (2,0,3,1)];cases.append(c)
  c=copy(original);c['Requests']=[];cases.append(c)
 template=copy(cases[0]);empty=copy(template);empty['Requests']=[]
 for old in dense[193:245]:
  c=copy(old);c['Requests']=[];cases.append(c)
 for c0 in (template,empty):
  c=copy(c0);c.update(dict(zip(('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumQueryBytes','MaximumQueryWork'),account(c))));cases.append(c)
  for key in ('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumQueryBytes','MaximumQueryWork'):
   if c[key]>0:bad=copy(c);bad[key]-=1;cases.append(bad)
 for key,value in (('MaximumCells',0),('MaximumCells',1025),('MaximumRequests',0),('MaximumRequests',1025),('NullControls',True)):
  for c0 in (template,empty):c=copy(c0);c[key]=value;cases.append(c)
 for column,row in ((-1,0),(4,0),(0,-1),(0,4)):
  c=copy(template);c['Requests'][-1].update(Column=column,Row=row);cases.append(c)
 c=copy(template);c['Requests'][-1]=dict(c['Requests'][0],XBits=f.word(.125));cases.append(c)
 # Full-cache reservations differ from subset costs; empty still validates the largest shape.
 for points in ([dict(Column=127,Row=7,XBits=f.word(.25),ZBits=f.word(.375))],[]):
  c=copy(template);c.update(Width=128,Rows=8,Requests=points);cases.append(c)
 # Empty avoids runtime-only normalization/seed-table work without inventing success samples.
 for index,value in ((0,0),(7,f.word(-1))):
  c=copy(empty);w=list(struct.unpack_from('<13I',bytes.fromhex(c['Hex']),32));w[5]=2;w[index]=value;c['Hex']=mode.b.frame(w).hex();cases.append(c)
 # Preserve prior result after actual last-cell domain and exact-export failures.
 for old in dense:
  result=f.expected(old,mode)
  if result.get('Reason')=='Base domain':c=copy(old);c['LateCurve']=True;cases.append(c)
  elif result.get('Reason')=='Frozen exact-export exponent':
   raw=bytearray.fromhex(old['Hex']);raw[36:40]=bytes(4);model=mode.Model(dict(Hex=raw.hex(),Width=2,Rows=2,TablePrecision=old['Table'],ConstructionPrecision=old['Construction']))
   words=struct.unpack_from('<13I',raw,32)
   with localcontext() as context:
    context.prec=100;exponent=f.dec(mode.n.finite_scalar(words[2])[0]).ln()/Decimal('.5').ln()
    for r in old['Requests']:
     base=mode.n.finite_scalar(mode.b.stored(model.miss(r,old['Query'])))[0]
     if not 0<base<1:continue
     v=(f.dec(base).ln()*exponent).exp();bits=struct.unpack('<Q',struct.pack('<d',float(v)))[0]
     if f.dyadic(f.f64(bits))[1]<-512:
      c=copy(old);c['Requests']=[dict(Column=0,Row=0,XBits=0,ZBits=0),dict(r,Column=1,Row=1)];c['LateCurve']=True;cases.append(c);break
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases
def compare(native,e,mode,f):
 assert len(native['Cases'])==len(e);outputs=endpoints=underflows=empties=0
 for got,want in zip(native['Cases'],e):
  assert got['Name']==want['Name'] and got['Reject']==want['Reject'],(got['Name'],got['Reject'],want['Reject'])
  if want['Reject']!='None':continue
  for key in want:
   if key!='Cells':assert got[key]==want[key],(got['Name'],key,got[key],want[key])
  assert len(got['Cells'])==len(want['Cells'])
  if not want['Cells']:empties+=1;assert got['Counts']==[0]*9
  for a,b in zip(got['Cells'],want['Cells']):
   for key in ('RequestIndex','Index','Column','Row','XBits','ZBits','KernelNegative','Base'):assert a[key]==b[key],(got['Name'],key)
   assert Fraction(a['Kernel'])==Fraction(b['Kernel']) and a['Policy']==f.POLICY and not a['OriginalNumericParity'] and a['Provenance']=='SuppliedWideMiss'
   out=f.f64(a['OutputBits']);f.close(out,b['ExpectedOutput']);f.close(f.f64(a['ExponentBits']),b['ExpectedExponent'])
   if out==0:assert a['OutputBits']==0
   magnitude,power=f.dyadic(out);assert [a['Magnitude'],a['Exponent']]==[magnitude,power] and Fraction(a['Converted'])==out and a['Projection']==mode.b.stored((out,False))
   if a['Base']&0x7fffffff in (0,f.word(1)):assert out==mode.n.finite_scalar(a['Base'])[0];endpoints+=1
   elif out==0:assert float(Decimal(b['ExpectedOutput']))==0;underflows+=1
   outputs+=1
 assert native['FullEquivalentOutputs']==outputs and native['EmptyReceipts']==empties
 return dict(Outputs=outputs,EndpointOutputs=endpoints,ModernTrueUnderflows=underflows,EmptyReceipts=empties,FullEquivalentOutputs=outputs)
def main():
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE:raise ValueError('Executable identity rejected before imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output rejected')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 if not a.out.resolve().is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Private containment required')
 f=load('frozen_curve57_sparse58','poc/round57-tests/Rendering/verify_curve.py');mode=load('frozen_modes50_sparse58','poc/round50-tests/Rendering/verify_modes.py');mode.n.setup()
 if a.native:
  d=json.loads((a.out/'sparse-fixtures.json').read_text());e=json.loads((a.out/'sparse-expected.json').read_text());native=json.loads(a.native.read_text());counts=compare(native,e,mode,f);reasons=[x.get('Reason','') for x in e]
  assert counts['ModernTrueUnderflows']>0 and reasons.count('Frozen exact-export exponent')>=2 and reasons.count('Base domain')>=2
  accepted=[x for x in e if x['Reject']=='None']
  sparse=sum(0<x['SuppliedRequests']<x['ShapeCells'] for x in accepted)
  arbitrary=sum([c['Index'] for c in x['Cells']]!=sorted(c['Index'] for c in x['Cells']) for x in accepted)
  original=sum(len(e[i]['Cells']) for i,c in enumerate(d['Cases']) if c.get('OriginalOccurrence') and e[i]['Reject']=='None')
  assert sparse>0 and arbitrary>0 and original==4
  receipt=dict(Passed=True,Cases=len(e),Accepted=len(accepted),Rejected=len(e)-len(accepted),OwnershipChecks=native['OwnershipChecks'],SparseReceipts=sparse,ArbitraryOrderReceipts=arbitrary,OriginalApproximateOutputs=original,ExactExportRejections=reasons.count('Frozen exact-export exponent'),BaseDomainRejections=reasons.count('Base domain'),DecimalPrecision=100,RelativeTolerance='1e-12',AbsoluteTolerance='1e-15',Policy=f.POLICY,OriginalNumericParity=False,Selected=d['Selected'],**counts)
  (a.out/'sparse-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 selector=load('frozen56_sparse_selector','tools/python/verify_terrain_fractal_curve_finish.py');meta,private=selector.selected(a.asset_root,a.config)
 cases=generate(mode,f,private);e=[expected(c,mode,f) for c in cases];assert e[0]['Reject']=='None' and e[192]['Reject']=='None'
 (a.out/'sparse-fixtures.json').write_text(json.dumps(dict(Selected=meta,Cases=cases)),encoding='utf-8');(a.out/'sparse-expected.json').write_text(json.dumps(e),encoding='utf-8');print(json.dumps(dict(Cases=len(e),Rejected=sum(c['Reject']!='None' for c in e),Selected=meta)))
if __name__=='__main__':main()
