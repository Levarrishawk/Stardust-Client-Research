"""Independent conditional coordinates/Fraction height/index/topology/winding model."""
import argparse,importlib.util,itertools,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
# Reuse accepted independent functions without invoking its CLI or producing its fixtures.
path=ROOT/'poc/round38-tests/Rendering/verify_grid.py';producer={'__file__':str(path)};exec(compile(path.read_text().split('\np=argparse.ArgumentParser();')[0],str(path),'exec'),producer)
a=producer['math'];fb=producer['fb'];coord=producer['evaluate']
s=importlib.util.spec_from_file_location('padding_static',Path(__file__).with_name('static_padding.py'));st=importlib.util.module_from_spec(s);s.loader.exec_module(st)
OFFSETS=[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
RINGS=[[1,2,3,4,5,6,7,8,1],[1,2,4,5,6,7,8,1],[2,4,5,6,7,8,2],[2,3,4,5,6,7,8,2],[2,3,4,5,6,8,2],[1,2,3,4,5,6,8,1],[1,2,3,4,6,8,1],[1,2,3,4,6,7,8,1],[1,2,4,6,7,8,1]]
DEFAULT=dict(MaximumGridSamples=65536,MaximumPatches=64,MaximumSelectedSamples=576)
def pattern(n,c,r,m):
 if r==0 and m&4:return 4 if c==0 and m&2 else 6 if c==n-1 and m&8 else 5
 if c==0 and m&2:return 2 if r==n-1 and m&1 else 3
 if r==n-1 and m&1:return 8 if c==n-1 and m&8 else 1
 return 7 if c==n-1 and m&8 else 0

def expected(f):
 fail=lambda reject='Input':dict(Name=f['Name'],Reject=reject)
 grid=coord(dict(Name=f['Name'],Request=f['Request'],Profiles=f['Profiles'],Limits=producer['DEFAULT']))
 if grid['Reject']!='None':return fail(grid['Reject'])
 caps=f['Limits'];r=f['Request'];n=grid['Parameters']['Count'];t=r['Tiles'];m=r['Margin'];uv=f['ScaleBits']
 if not 1<=caps['MaximumGridSamples']<=1048576 or not 1<=caps['MaximumPatches']<=1024 or not 1<=caps['MaximumSelectedSamples']<=9216:return fail('Options')
 if f['Mask'] not in range(16) or not a.finite(uv) or not 0<a.scalar(uv)[0]<=16:return fail('Options')
 if not 1<=t<=32 or m<0 or m+2*t+1>n or n*n>caps['MaximumGridSamples'] or t*t>caps['MaximumPatches'] or 9*t*t>caps['MaximumSelectedSamples']:return fail()
 if any(len(f[k])!=n*n for k in ('Heights','Normals','Colors')) or not grid['Position']['HasStrictlyIncreasingDisplayAxes']:return fail()
 if any(abs(a.scalar(v)[0])>1000000 for k in ('First','Second') for v in grid['Position'][k]):return fail()
 heights=[];positions=[]
 for i,(raw,normal) in enumerate(zip(f['Heights'],f['Normals'])):
  if not a.finite(raw) or abs(a.scalar(raw)[0])>1000000 or any(not a.finite(x) or abs(a.scalar(x)[0])>4096 for x in normal):return fail()
  if sum(a.scalar(x)[0]**2 for x in normal)<a.Fraction(1,10**12):return fail()
  y=a.bits(a.precision(a.add(a.scalar(raw),a.scalar(0)),f['Profiles'][3]));heights.append(y)
  positions.append([grid['Position']['First'][i%n],y,grid['Position']['Second'][i//n]])
 patches=[]
 for row in range(t):
  for column in range(t):
   p=pattern(t,column,row,f['Mask']);source=[(m+2*row+dy)*n+m+2*column+dx for dx,dy in OFFSETS];ix=[v for l,r in zip(RINGS[p],RINGS[p][1:]) for v in (0,l,r)]
   pos=[positions[i] for i in source];normals=[f['Normals'][i] for i in source]
   for j in range(0,len(ix),3):
    aa,bb,cc=[[a.scalar(v)[0] for v in pos[ix[j+k]]] for k in range(3)];b=[y-x for x,y in zip(aa,bb)];c=[y-x for x,y in zip(aa,cc)]
    cross=[b[1]*c[2]-b[2]*c[1],b[2]*c[0]-b[0]*c[2],b[0]*c[1]-b[1]*c[0]]
    normal=[sum(a.scalar(normals[ix[j+k]][l])[0] for k in range(3)) for l in range(3)]
    if sum(v*v for v in cross)<a.Fraction(1,10**20) or abs(cross[1])<=a.Fraction(1,10**12) or sum(v*w for v,w in zip(cross,normal))<=0:return fail()
   patches.append(dict(Source=source,Pattern=p,Indices=ix,Positions=pos,Normals=normals,Colors=[f['Colors'][i] for i in source],LayerPairs=[dict(Column=i%n,Row=i//n,FirstBits=grid['Layer']['First'][i%n],SecondBits=grid['Layer']['Second'][i//n]) for i in source]))
 return dict(Name=f['Name'],Reject='None',GridWidth=n,CellsPerSide=t,SampleBorder=m,LeadingPadding=m,TrailingPadding=n-(m+2*t+1),InputHeights=f['Heights'],PositionHeights=heights,Colors=f['Colors'],Patches=patches)

def fixtures():
 fs=[]
 def emit(request,profiles=(24,24,24,24),mask=0,caps=None,kind=None):
  n=max(0,2*(request['Tiles']+request['Margin']));size=n*n
  h=[fb(((i%n)*2-(i//n))*.03125) for i in range(size)];normals=[[0,0x3f800000,0x80000000] for i in range(size)];colors=[(0x80402010+i*0x010307)&0xffffffff for i in range(size)]
  if size:h[0]=0x80000000
  f=dict(Name=str(len(fs)),Request=request,Profiles=list(profiles),Mask=mask,ScaleBits=0x3f800000,Heights=h,Normals=normals,Colors=colors,Limits=caps or DEFAULT)
  fs.append(f);return f
 def req(w=0x41000000,t=2,m=3,x=-1,z=0,s=1):return dict(ChunkWidthBits=w,Tiles=t,Margin=m,First=x,Second=z,Scale=s)
 for p in itertools.product((24,53,64),repeat=4):
  for mask in range(16):
   emit(req(),p,mask);emit(req(w=0x3f800001,t=1,m=1,x=-3,z=4,s=3),p,mask)
 for profiles in itertools.product((24,53,64),repeat=4):
  emit(req(x=0),profiles);emit(req(z=1),profiles)
 for t,m in ((2,1),(3,2),(4,3)):
  for mask in range(16):emit(req(t=t,m=m),mask=mask)
 for m in (-1,0):emit(req(m=m))
 for w in (1,0x00800001):emit(req(w=w))
 emit(req(w=fb(.000001)));
 emit(req(x=2147483647));emit(req(w=fb(.0000001),x=2147483647))
 for k in ('Heights','Normals','Colors'):
  f=emit(req());f[k]=f[k][:-1]
 for bad in (0x7f800000,0xff800000,0x7fc12345,fb(1000001)):
  for at in (0,99):f=emit(req());f['Heights'][at]=bad
 for normal in ([0,0,0],[0,0xbf800000,0],[0x7fc12345,0x3f800000,0],[fb(4097),0x3f800000,0]):
  f=emit(req());f['Normals']=[normal]*100
 for cap in [dict(MaximumGridSamples=99,MaximumPatches=4,MaximumSelectedSamples=36),dict(MaximumGridSamples=100,MaximumPatches=3,MaximumSelectedSamples=36),dict(MaximumGridSamples=100,MaximumPatches=4,MaximumSelectedSamples=35),dict(MaximumGridSamples=100,MaximumPatches=4,MaximumSelectedSamples=36),dict(MaximumGridSamples=0,MaximumPatches=4,MaximumSelectedSamples=36),dict(MaximumGridSamples=1048577,MaximumPatches=4,MaximumSelectedSamples=36),dict(MaximumGridSamples=100,MaximumPatches=1025,MaximumSelectedSamples=36),dict(MaximumGridSamples=100,MaximumPatches=4,MaximumSelectedSamples=9217)]:emit(req(),caps=cap)
 for mask in (-1,16):emit(req(),mask=mask)
 for uv in (0,0x7fc12345,fb(17)):
  f=emit(req());f['ScaleBits']=uv
 # Raw ARGB has no numeric-invalid bit pattern.
 f=emit(req());f['Colors']=[0,0xffffffff,0x80000000,0x7fc12345]*25
 emit(req(t=32,m=1),caps=dict(MaximumGridSamples=4356,MaximumPatches=1024,MaximumSelectedSamples=9216))
 return fs

p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
static=st.verify(args.exe,args.packages);fs=fixtures();args.fixtures.write_text(json.dumps(dict(Cases=fs),separators=(',',':')));ex=[expected(f) for f in fs]
receipt=dict(Static=static,ProfileCombinations=81,Cases=len(fs),Accepted=sum(f['Reject']=='None' for f in ex),Rejected=sum(f['Reject']!='None' for f in ex),SyntheticOnly=True)
if args.native:
 native=json.loads(args.native.read_text(encoding='utf-8-sig'));assert len(native['Cases'])==len(ex)
 for want,got in zip(ex,native['Cases']):assert want==got,(want['Name'],want,got)
 receipt['OwnershipChecks']=native['OwnershipChecks'];assert native['OwnershipChecks']>=35
 receipt['ExactPatches']=sum(len(f.get('Patches',[])) for f in ex);receipt['ExactSelectedSamples']=receipt['ExactPatches']*9
 receipt['Patterns']=sorted({p['Pattern'] for f in ex for p in f.get('Patches',[])});assert receipt['Patterns']==list(range(9))
args.receipt.write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
