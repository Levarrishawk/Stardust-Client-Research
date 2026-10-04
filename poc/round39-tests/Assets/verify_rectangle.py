"""Independent Fraction oracle for fresh/untransformed BREC cache, query and shaping."""
import importlib.util,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('circle_reference',ROOT/'poc/round36-tests/Rendering/verify_circle.py');c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
a=c.a
def form(tag,children=b''):return b'FORM'+struct.pack('>I',len(children)+4)+tag.encode()+children
def chunk(tag,b):return tag.encode()+struct.pack('>I',len(b))+b
def encode(f):
 h=form('IHDR',form('0001',chunk('DATA',struct.pack('<I',f['Enabled'])+b'synthetic\0')))
 w=list(f['Geometry']);v=f['Version']
 if v=='0001':w=[0xdeadbeef]+w
 elif v in ('0002','0003'):w+=[f['EncodedSelector'],f['Amount']]
 if v=='0003':w+=[0xffffffff,0x80000000,0x7f812345,0x40000000]
 raw=struct.pack('<'+'I'*len(w),*w)+(b'opaque-extension\0' if v=='0003' else b'')
 return form('BREC',form(v,h+chunk('DATA',raw))).hex()
def evaluate(f,spill_before_shape=False):
 cp,p=f['CachePrecision'],f['Precision']
 if cp not in (24,53,64) or p not in (24,53,64):return dict(Reject='Options')
 if f['Version'] not in ('0000','0001','0002','0003'):return dict(Reject='Input')
 empty=dict(Reject='None',Skipped=True,Influence=None,Query=None,Amount=None,Branch=None,CacheOuter=None,CacheInner=None,CacheExtents=None,QueryEdges=None,QueryExtents=None)
 if f['Action']=='evaluate' and not f['Enabled']:return empty
 g=list(f['Geometry']);xy=f['Coordinates'];amount=f['Amount'] if f['Version'] in ('0002','0003') else 0
 if not all(a.finite(x) for x in g+xy+[amount]):return dict(Reject='Input')
 if a.scalar(amount)[0]<0:amount=0
 elif a.scalar(amount)[0]>1:amount=0x3f800000
 av=a.scalar(amount)
 if a.scalar(g[0])[0]>a.scalar(g[2])[0]:g[0],g[2]=g[2],g[0]
 if a.scalar(g[1])[0]>a.scalar(g[3])[0]:g[1],g[3]=g[3],g[1]
 def add(x,y,p):return a.precision(a.add(x,y),p)
 def sub(x,y,p):return add(x,a.neg(y),p)
 def mul(x,y,p):return a.precision(a.mul(x,y),p)
 def store(x):
  b=a.bits(x)
  if not a.finite(b):raise ValueError('finite spill')
  return b
 def extents(p):
  first=store(sub(a.scalar(g[2]),a.scalar(g[0]),p));us=sub(a.scalar(g[3]),a.scalar(g[1]),p);second=store(us)
  return [first,second,second if us[0]<a.scalar(first)[0] else first]
 def inset(ext,p):return mul(mul(av,a.scalar(ext[2]),p),a.scalar(0x3f000000),p)
 def contains(bounds):
  return all(min(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0])<=a.scalar(xy[j])[0]<=max(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0]) for j in (0,1))
 try:
  ce=extents(cp);t=inset(ce,cp)
  inner=[store(add(t,a.scalar(g[0]),cp)),store(add(t,a.scalar(g[1]),cp)),store(sub(a.scalar(g[2]),t,cp)),store(sub(a.scalar(g[3]),t,cp))]
  edges=qe=None
  if not contains(g):u=a.scalar(0);branch=0
  elif not av[0]:u=a.scalar(0x3f800000);branch=1
  elif contains(inner):u=a.scalar(0x3f800000);branch=2
  else:
   edges=[store(sub(a.scalar(xy[0]),a.scalar(g[0]),p)),store(sub(a.scalar(g[2]),a.scalar(xy[0]),p)),store(sub(a.scalar(xy[1]),a.scalar(g[1]),p)),store(sub(a.scalar(g[3]),a.scalar(xy[1]),p))]
   qe=extents(p);den=inset(qe,p);edge=den
   for b in edges:
    if a.scalar(b)[0]<edge[0]:edge=a.scalar(b)
   if not den[0]:raise ValueError('division')
   u=a.precision((edge[0]/den[0],edge[1] if not edge[0] else False),p);branch=3
  projection=store(u)
  if spill_before_shape:u=a.scalar(projection)
  sel=f['Selector']
  if sel==0:shaped=u
  elif sel==1:shaped=mul(u,u,p)
  elif sel==2:shaped=c.sqrt_round(u,p)
  elif sel==3:shaped=mul(mul(sub(a.scalar(0x40400000),add(u,u,p),p),u,p),u,p)
  else:shaped=a.scalar(0)
  return dict(Reject='None',Skipped=False,Influence=store(shaped),Query=projection,Amount=amount,Branch=branch,CacheOuter=g,CacheInner=inner,CacheExtents=ce,QueryEdges=edges,QueryExtents=qe)
 except ValueError:return dict(Reject='Input')
def fbits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def fixtures():
 out=[]
 def emit(g,xy,amount=0,selector=0,enabled=1,version='0002',action='evaluate',cp=24,p=24):
  f=dict(Name=str(len(out)),Geometry=list(g),Coordinates=list(xy),Amount=amount,Selector=selector,EncodedSelector=selector,Enabled=enabled,Version=version,Action=action,Precision=p,CachePrecision=cp)
  if action=='query':f['EncodedSelector']=0
  if version in ('0000','0001') and action=='evaluate':f['Selector']=0
  f['RuleHex']=encode(f);f['Expected']=evaluate(f);out.append(f)
 geometries=[(0,0,fbits(2),fbits(4)),(fbits(2),fbits(4),0,0),(0,0,0,0),(0,0,0,fbits(2)),(0x80000000,0,0,0x80000000),(0,0,1,2),(fbits(-1),fbits(-3),fbits(1),fbits(3)),(0xff7fffff,0,0x7f7fffff,fbits(2))]
 coords=[(0,0),(0x80000000,0x80000000),(1,1),(fbits(.25),fbits(.25)),(fbits(1),fbits(2)),(fbits(2),fbits(4)),(fbits(2)+1,fbits(2)),(fbits(-1),0)]
 for p in (24,53,64):
  for g in geometries:
   for xy in coords:
    for amt in (0,0x80000000,fbits(.25),fbits(.5),fbits(1),fbits(-1),fbits(2)):
     for sel in (0,1,2,3,0xffffffff):emit(g,xy,amt,sel,cp=p,p=p)
  for v in ('0000','0001','0002','0003','0099'):
   for enabled in (0,1,0xffffffff):
    emit((0,0,fbits(2),fbits(4)),(fbits(.125),fbits(.25)),fbits(.5),3,enabled,v,cp=p,p=p)
  for bad in (0x7f800000,0xff800000,0x7fc12345,0x7f812345):
   for enabled in (0,1):
    for position in range(7):
     g=[0,0,fbits(2),fbits(4)];xy=[0,0];amt=fbits(.5)
     if position<4:g[position]=bad
     elif position<6:xy[position-4]=bad
     else:amt=bad
     emit(g,xy,amt,2,enabled,cp=p,p=p)
   emit((bad,0,fbits(2),fbits(4)),(0,0),0,2,0,action='query',cp=p,p=p)
 rng=random.Random(3901)
 for _ in range(90):
  size=2.0**rng.randint(-125,110);lo=[rng.uniform(-2,2)*size for j in (0,1)];hi=[lo[j]+rng.uniform(.25,4)*size for j in (0,1)]
  g=list(map(fbits,lo+hi));xy=[fbits(lo[j]+rng.uniform(0,.5)*(hi[j]-lo[j])) for j in (0,1)];amt=fbits(rng.uniform(.1,1))
  for cp in (24,53,64):
   for p in (24,53,64):
    for sel in (0,1,2,3):emit(g,xy,amt,sel,cp=cp,p=p)
 for bad in (0,23,25,52,54,63,65):
  emit((0,0,fbits(2),fbits(4)),(0,0),enabled=0,p=bad)
  emit((0,0,fbits(2),fbits(4)),(0,0),enabled=0,cp=bad)
 for cp in (24,53,64):
  for p in (24,53,64):
   for sel in (0,1,2,3,0xffffffff):emit((0,0,fbits(2),fbits(4)),(fbits(.1),fbits(.2)),fbits(.7),sel,0,action='query',cp=cp,p=p)
 return out
def static(exe,packages):
 import hashlib
 raw=Path(exe).read_bytes();assert hashlib.sha256(raw).hexdigest()==c.EXPECTED
 sys.path.insert(0,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 anchors={0xb72989:('mov','byte ptr [esi + 0x3c], al'),0xb7299d:('mov','dword ptr [esi + 0x40], ecx'),0xb729a0:('mov','dword ptr [esi + 0x50], ecx'),0xb63333:('mov','dword ptr [edx + 0x14], eax'),0xb63336:('mov','dword ptr [edx + 0x18], eax'),0xb72aaa:('call','0xb65fc0'),0xb72ab1:('call','0xb72ac0'),0xb72ad1:('jne','0xb72adf'),0xb72adc:('fstp','dword ptr [ecx + 0x24]'),0xb72aea:('jne','0xb72af8'),0xb72af5:('fstp','dword ptr [ecx + 0x28]'),0xb72afb:('fsub','dword ptr [ecx + 0x1c]'),0xb72afe:('fstp','dword ptr [ebp - 4]'),0xb72b04:('fsub','dword ptr [ecx + 0x20]'),0xb72b07:('fst','dword ptr [ebp - 8]'),0xb72b0a:('fcomp','dword ptr [ebp - 4]'),0xb72b15:('jnp','0xb72b1a'),0xb72b21:('fmul','dword ptr [eax]'),0xb72b28:('fmul','dword ptr [0x15dc5e4]'),0xb72b47:('fstp','dword ptr [eax]'),0xb72b4e:('fstp','dword ptr [ecx + 0x30]'),0xb72b54:('fsub','st(1)'),0xb72b56:('fstp','dword ptr [ecx + 0x34]'),0xb72b5c:('fsub','st(1)'),0xb72b5e:('fstp','dword ptr [ecx + 0x38]'),0xb72bce:('je','0xb72bf8'),0xb72be7:('call','0xabb330'),0xb72c1d:('jne','0xb72c91'),0xb72c4e:('jp','0xb72c91'),0xb72c5b:('jne','0xb72c91'),0xb72c8f:('jnp','0xb72c9e'),0xb72c91:('fld','dword ptr [0x15dc6a0]'),0xb72ca1:('fcomp','dword ptr [0x15dc6a0]'),0xb72cac:('jp','0xb72cbb'),0xb72cae:('fld','dword ptr [0x15dc6f8]'),0xb72d27:('fstp','dword ptr [ebp - 4]'),0xb72d30:('fstp','dword ptr [ebp - 8]'),0xb72d39:('fstp','dword ptr [ebp - 0xc]'),0xb72d42:('fstp','dword ptr [ebp + 0xc]'),0xb72d4b:('fstp','dword ptr [ebp + 8]'),0xb72d54:('fst','dword ptr [ebp - 0x14]'),0xb72d57:('fcomp','dword ptr [ebp + 8]'),0xb72d62:('jnp','0xb72d67'),0xb72d6a:('fmul','dword ptr [eax]'),0xb72d6c:('fmul','dword ptr [0x15dc5e4]'),0xb72d77:('fcomp','st(2)'),0xb72d7e:('jp','0xb72d85'),0xb72d8f:('jp','0xb72d96'),0xb72da0:('jp','0xb72da7'),0xb72db1:('jp','0xb72db8'),0xb72db8:('fdivrp','st(1)'),0xb72e3d:('fld','dword ptr [0x15dc6f8]'),0xb72fe6:('mov','byte ptr [ecx + 0x3c], 1'),0xb73003:('mov','byte ptr [ecx + 0x3c], 1'),0xb63f8e:('call','0xb62f20'),0xb63faf:('call','dword ptr [edx + 0x24]'),0xb63fb2:('cmp','edi, 3'),0xb63fb5:('ja','0xb63fd8'),0xb63fc0:('fmulp','st(1)'),0xb63fc4:('fsqrt',''),0xb63fca:('fadd','st(1)'),0xb63fcc:('fsubr','dword ptr [0x15dd850]'),0xb63fd2:('fmul','st(1)'),0xb63fd4:('fmulp','st(1)'),0xb63fe0:('fst','dword ptr [ebp - 0x34]')}
 anchors.update({0xb733f6:('fcomp','dword ptr [0x15dc6a0]'),0xb7341c:('mov','dword ptr [ebp + 8], 0x3f800000'),0xb73506:('fcomp','dword ptr [0x15dc6a0]'),0xb73511:('jp','0xb7351c'),0xb73513:('mov','dword ptr [ebp + 8], 0'),0xb7351f:('fcomp','dword ptr [0x15dc6f8]'),0xb7352a:('jne','0xb73535'),0xb7352c:('mov','dword ptr [ebp + 8], 0x3f800000')})
 for va,pair in anchors.items():
  i=next(md.disasm(b[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair,(hex(va),pair,(i.mnemonic,i.op_str))
 assert struct.unpack_from('<4I',b,0xb642a0-base)==(0xb63fe0,0xb63fbe,0xb63fc4,0xb63fc8)
 for va,value in [(0x15dc5e4,0x3f000000),(0x15dc6a0,0),(0x15dc6f8,0x3f800000),(0x15dd850,0x40400000)]:assert struct.unpack_from('<I',b,va-base)[0]==value
 assert [struct.unpack_from('<I',b,va-base)[0] for va in (0x15ed3c8,0x15ed3dc)]==[0xb72aa0,0xb72bc0]
 for start,end in [(0xb73200,0xb7329e),(0xb732a0,0xb7334b),(0xb73350,0xb73456),(0xb73460,0xb735c6)]:
  ii=list(md.disasm(b[start-base:end-base],start))
  assert not any(i.mnemonic in ('mov','fst','fstp') and i.op_str.startswith(('byte ptr [edi + 0x3c]','dword ptr [edi + 0x40]','dword ptr [edi + 0x44]','dword ptr [edi + 0x48]','dword ptr [edi + 0x4c]','dword ptr [edi + 0x50]','dword ptr [edi + 0x54]')) for i in ii)
  assert not any(i.mnemonic=='call' and i.op_str in ('0xb72fe0','0xb73000') for i in ii)
 q=list(md.disasm(b[0xb72bc0-base:0xb72e48-base],0xb72bc0));assert [i.op_str for i in q if i.mnemonic=='call']==['0xabb330']
 return dict(sha256=c.EXPECTED,anchors=len(anchors),constants=4,selectorTable=4,activeOriginalProfiles='Unknown',domain='Fresh untransformed finite input')
if __name__=='__main__':
 action=sys.argv[1]
 if action=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(dict(Cases=fs),separators=(',',':')));print('Rectangle vectors',len(fs))
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures();assert len(native['Cases'])==len(fs)
  for f,n in zip(fs,native['Cases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['BatchChecks']==32 and native['ArgumentNegatives']==6
  spec=importlib.util.spec_from_file_location('brec_reference',ROOT/'poc/round36-tests/Assets/verify_brec_input.py');brec=importlib.util.module_from_spec(spec);spec.loader.exec_module(brec)
  original=brec.original(Path(sys.argv[5]),Path(sys.argv[6]));first=original['Rectangles'][0];md=first['Metadata']
  expected_original=dict(ConfigSha256=original['ConfigSha256'],SourceSha256=original['SourceSha256'],GeneratorSha256=original['GeneratorSha256'],Priority=42,Count=29,First=dict(LayerIndex=first['LayerIndex'],ChildOrdinal=first['ChildOrdinal'],Offset=first['Offset'],Version=md['Version'],HeaderVersion=md['Header']['Version'],EnabledBits=md['Header']['EnabledBits'],DataWordBits=md['DataWordBits'],EncodedBytes=md['EncodedBytes'],Sha256=md['Sha256']))
  assert native['Original']==expected_original
  sys.path.insert(0,sys.argv[4])
  raw_static=brec.static(Path(sys.argv[3]))
  valid=[f for f in fs if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']]
  groups={};phases={}
  for f in valid:
   key=(tuple(f['Geometry']),tuple(f['Coordinates']),f['Amount'],f['Selector'],f['Version'],f['Action'],f['Enabled'],f['EncodedSelector'],f['CachePrecision']);groups.setdefault(key,set()).add(f['Expected']['Influence'])
   key=(tuple(f['Geometry']),tuple(f['Coordinates']),f['Amount'],f['Selector'],f['Version'],f['Action'],f['Enabled'],f['EncodedSelector'],f['Precision']);phases.setdefault(key,set()).add(tuple(f['Expected']['CacheInner']))
  result=dict(passed=True,vectors=len(fs),rejections=sum(f['Expected']['Reject']!='None' for f in fs),BatchChecks=native['BatchChecks'],ArgumentNegatives=native['ArgumentNegatives'],static=static(sys.argv[3],sys.argv[4]),queryPrecisionSensitiveInputs=sum(len(x)>1 for x in groups.values()),cachePrecisionSensitiveInputs=sum(len(x)>1 for x in phases.values()),unspilledShapingSensitiveCases=sum(evaluate(f,True)['Influence']!=f['Expected']['Influence'] for f in valid))
  result['rawInputStatic']=raw_static;result['originalRawOccurrences']=1
  assert result['queryPrecisionSensitiveInputs']>0 and result['cachePrecisionSensitiveInputs']>0 and result['unspilledShapingSensitiveCases']>0
  print(json.dumps(result,indent=2))
