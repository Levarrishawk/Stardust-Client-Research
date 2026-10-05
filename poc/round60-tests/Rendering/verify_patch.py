"""Independent Fraction/Decimal normal-patch composition; authored nonzero-origin metadata only."""
import argparse,importlib.util,json,hashlib,struct,os,stat
from decimal import Decimal
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def clone(c):return json.loads(json.dumps(c))
def account(w,p):
 n=w*w;own=[36*n,256+36*n,128+4*w+16*n+4*p];normal=[8*w+4*n,256+8*w+16*n,128+4*w+80*n];final=[20*n+1024*p,256+36*n+2048*p,256+96*n+4096*p]
 return own,normal,final,[sum(v[i] for v in (own,normal,final)) for i in range(3)]
def prepare(f):
 m=f.setup();original=m['expected']
 def source(c):
  result=original(c)
  if result['Reject']!='None':return result
  origin=c.get('VerticalOriginBits',0);a=m['a'];stored=[a.bits(a.precision(a.add(a.scalar(v),a.scalar(origin)),c['Profiles'][3])) for v in c['Heights']]
  if any(not a.finite(v) or abs(a.scalar(v)[0])>1000000 for v in stored):return dict(Name=c['Name'],Reject='Input')
  result['PositionHeights']=stored
  for patch in result['Patches']:
   for j,index in enumerate(patch['Source']):patch['Positions'][j][1]=stored[index]
  return result
 m['expected']=source;return m
UV=[(.5,.5),(0,.5),(0,0),(.5,0),(1,0),(1,.5),(1,1),(.5,1),(0,1)]
def expected(c,f,m):
 try:
  source=m['expected'](c)
  if source['Reject']!='None':raise ValueError('Source')
  w=source['GridWidth'];n=w*w;p=len(source['Patches']);selected=9*p
  spec=[('MaximumAxisSamples',32,32),('MaximumCells',1024,1024),('MaximumPatches',64,256),('MaximumSelectedSamples',576,2304),('MaximumCopyBytes',1048576,4194304),('MaximumArrayBytes',2097152,16777216),('MaximumWork',4194304,33554432)]
  for k,d,maximum in spec:
   if not 0<=c.get(k,d)<=maximum:raise ValueError('Parent limit')
  if not 2<=w<=32 or w>c.get('MaximumAxisSamples',32) or n>c.get('MaximumCells',1024) or p>c.get('MaximumPatches',64) or selected>c.get('MaximumSelectedSamples',576):raise ValueError('Shape')
  own,normal,final,total=account(w,p)
  for (k,d,_),v in zip(spec[4:],total):
   if v>c.get(k,d):raise ValueError('Aggregate preflight')
  pl=dict(MaximumGridSamples=65536,MaximumPatches=64,MaximumSelectedSamples=576);pl.update(c.get('PatchLimits',{}))
  for k,lo,hi,need in (('MaximumGridSamples',1,1048576,n),('MaximumPatches',1,1024,p),('MaximumSelectedSamples',1,9216,selected)):
   if not lo<=pl[k]<=hi or need>pl[k]:raise ValueError('Final child preflight')
  nc=clone(c)
  for key in [x[0] for x in spec]:nc.pop(key,None)
  nc.update(c.get('NormalLimits',{}));norm=f.expected(nc,m)
  if norm['Reject']!='None':raise ValueError('Normal child')
  nb=[[f.word(float(Decimal(v))) for v in vector] for vector in norm['Normals']];patches=[];a=m['a']
  for patch in source['Patches']:
   ids=patch['Source'];vectors=[nb[i] for i in ids];pos=patch['Positions'];ix=patch['Indices']
   for j in range(0,len(ix),3):
    aa,bb,cc=[[a.scalar(v)[0] for v in pos[ix[j+k]]] for k in range(3)];b=[y-x for x,y in zip(aa,bb)];d=[y-x for x,y in zip(aa,cc)];cross=[b[1]*d[2]-b[2]*d[1],b[2]*d[0]-b[0]*d[2],b[0]*d[1]-b[1]*d[0]]
    summed=[]
    for k in range(3):
     v=a.bits(a.add(a.scalar(vectors[ix[j]][k]),a.scalar(vectors[ix[j+1]][k])));v=a.bits(a.add(a.scalar(v),a.scalar(vectors[ix[j+2]][k])));summed.append(a.scalar(v)[0])
    if sum(x*y for x,y in zip(cross,summed))<=0:raise ValueError('Final winding')
   colors=[c['Colors'][i] for i in ids]
   rgba=[[a.bits((Fraction((color>>shift)&255,255),False)) for shift in (16,8,0,24)] for color in colors]
   scale=a.scalar(c['ScaleBits'])[0];uv=[[a.bits((Fraction(x)*scale,False)),a.bits((Fraction(y)*scale,False))] for x,y in UV]
   patches.append(dict(Source=ids,Pattern=patch['Pattern'],Positions=pos,Normals=[norm['Normals'][i] for i in ids],Colors=colors,Rgba=rgba,Uvs=uv,Indices=ix))
  names=('CopyBytes','ArrayBytes','Work');out=dict(Name=c['Name'],Reject='None',Width=w,Count=n,PatchCount=p,SelectedSamples=selected,Raw=c['Heights'],Stored=source['PositionHeights'],Colors=c['Colors'],Normals=norm['Normals'],Patches=patches)
  for prefix,values in (('Own',own),('Normal',normal),('FinalReserved',final),('Aggregate',total)):
   for key,value in zip(names,values):out[prefix+key]=value
  return out
 except (ValueError,IndexError,OverflowError,ZeroDivisionError):return dict(Name=c['Name'],Reject='Input')
def generate(f,m):
 cases=f.generate(m)
 for c in cases:
  nl={}
  for key in ('MaximumAxisSamples','MaximumCells','MaximumCopyBytes','MaximumArrayBytes','MaximumWork'):
   if key in c:nl[key]=c.pop(key)
  if nl:c['NormalLimits']=nl
  c['Colors']=[(0x80402010+i*0x010307)&0xffffffff for i in range(len(c['Heights']))]
 for i in range(18):
  c=clone(cases[i]);c['VerticalOriginBits']=f.word((1.25,-3,65536)[i%3]);c['ScaleBits']=f.word((.5,1.25,16)[i%3]);cases.append(c)
 c=clone(cases[2]);c['VerticalOriginBits']=0x80000000;cases.append(c)
 template=clone(cases[0]);w=4;p=1;tot=account(w,p)[3]
 c=clone(template);c.update(dict(zip(('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'),tot)));c.update(MaximumAxisSamples=4,MaximumCells=16,MaximumPatches=1,MaximumSelectedSamples=9,PatchLimits=dict(MaximumGridSamples=16,MaximumPatches=1,MaximumSelectedSamples=9));cases.append(c)
 for key in ('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'):
  c=clone(template);c.update(dict(zip(('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'),tot)));c[key]-=1;cases.append(c)
 for key,value in (('MaximumAxisSamples',3),('MaximumAxisSamples',33),('MaximumAxisSamples',-1),('MaximumCells',15),('MaximumCells',1025),('MaximumCells',-1),('MaximumPatches',-1),('MaximumSelectedSamples',-1),('MaximumCopyBytes',-1),('MaximumArrayBytes',-1),('MaximumWork',-1),('MaximumPatches',0),('MaximumPatches',257),('MaximumSelectedSamples',8),('MaximumSelectedSamples',2305),('MaximumCopyBytes',4194305),('MaximumArrayBytes',16777217),('MaximumWork',33554433)):
  c=clone(template);c[key]=value;cases.append(c)
 for pl in (dict(MaximumGridSamples=15),dict(MaximumPatches=0),dict(MaximumSelectedSamples=8),dict(MaximumSelectedSamples=9217)):
  c=clone(template);c['PatchLimits']=pl;cases.append(c)
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases
def vectors(got,want):
 assert len(got)==len(want)
 for bits,reference in zip(got,want):
  for b,r in zip(bits,reference):assert abs(float(struct.unpack('<f',struct.pack('<I',b))[0])-float(Decimal(r)))<=3e-6,(b,r)
def compare(native,e):
 assert len(native['Cases'])==len(e);samples=patches=selected=0
 for got,want in zip(native['Cases'],e):
  assert got['Name']==want['Name'] and got['Reject']==want['Reject'],(got['Name'],got['Reject'],want['Reject'])
  if want['Reject']!='None':continue
  for k in want:
   if k not in ('Normals','Patches'):assert got[k]==want[k],(got['Name'],k)
  vectors(got['Normals'],want['Normals']);samples+=want['Count'];patches+=want['PatchCount'];selected+=want['SelectedSamples']
  for g,p in zip(got['Patches'],want['Patches']):
   for k in p:
    if k!='Normals':assert g[k]==p[k],(got['Name'],k)
   vectors(g['Normals'],p['Normals'])
 assert native['DoubleOriginWitnesses']>=18 and native['ActivePaddedSeamChecks']==5
 return dict(Samples=samples,Patches=patches,SelectedSamples=selected)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE:raise ValueError('Executable identity before imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output rejected')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 f=load('frozen59_composition','poc/round59-tests/Rendering/verify_normals.py');m=prepare(f)
 if a.native:
  e=json.loads((a.out/'patch-expected.json').read_text());native=json.loads(a.native.read_text());stats=compare(native,e)
  receipt=dict(Passed=True,Cases=len(e),Accepted=sum(c['Reject']=='None' for c in e),Rejected=sum(c['Reject']!='None' for c in e),DecimalPrecision=100,NormalTolerance='3e-6',OwnershipChecks=native['OwnershipChecks'],DoubleOriginWitnesses=native['DoubleOriginWitnesses'],ActivePaddedSeamChecks=native['ActivePaddedSeamChecks'],OriginalNormalParity=False,OriginalMaterialParity=False,**stats)
  (a.out/'patch-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 cases=generate(f,m);e=[expected(c,f,m) for c in cases];assert e[0]['Reject']=='None'
 (a.out/'patch-fixtures.json').write_text(json.dumps(dict(Cases=cases)),encoding='utf-8');(a.out/'patch-expected.json').write_text(json.dumps(e),encoding='utf-8');print(json.dumps(dict(Cases=len(e))))
if __name__=='__main__':main()
