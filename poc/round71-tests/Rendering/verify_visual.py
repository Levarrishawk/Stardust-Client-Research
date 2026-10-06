"""Independent accepted-clock arithmetic and eight original body shader vertices before native execution."""
import argparse,contextlib,hashlib,importlib.util,json,math,os,stat,struct,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_skeleton_bind_pose import skeletons,pose,quaternion,rotation,multiply
from ckat_float32_overshoot_probe import build_table,f32
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
STEPS=[dict(Label=n,Advance=advance,Seconds=s,X=x,Z=z,TickSeconds=t,Apply=ap) for n,advance,s,x,z,t,ap in
 [('idle',True,.1,0,0,.25,False),('walk',True,.1,.2,0,60,False),('run',True,.1,0,.6,1,True),
  ('held-run',False,0,0,0,60,False),('idle-again',True,.1,0,0,0,True),('walk-again',True,.1,0,.15,.01,False)]]
CLIPS=[('idle','appearance/animation/all_b_idl_ambient_01.ans','798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B'),
 ('walk','appearance/animation/all_b_loc_walk_male.ans','FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6'),
 ('run','appearance/animation/all_b_loc_run.ans','72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4')]
def decode(raw,table,frame):
 root,=nodes(raw);fields=dict(root[1][0][1])
 def unpack(word,selectors):
  values=[]
  for axis,(field,selector) in enumerate(zip(((word>>21)&2047,(word>>10)&2047,word&1023),selectors)):
   basis,s11,s10=table[selector];sign=512 if axis==2 else 1024;offset=(field&(sign-1))*(s10 if axis==2 else s11)
   values.append(f32(basis-offset if field&sign else basis+offset))
  values.append(f32(math.sqrt(max(0,1-sum(v*v for v in values)))));norm=math.sqrt(sum(v*v for v in values))
  return [f32(v/norm) for v in values]
 rotations=[[(t,unpack(word,data[2:5])) for t,word in struct.iter_unpack('<HI',data[5:])] for _,data in fields[b'AROT']]
 static=[unpack(word,(x,y,z)) for x,y,z,word in struct.iter_unpack('<BBBI',fields[b'SROT'])]
 translations=[list(struct.iter_unpack('<Hf',data[2:])) for _,data in fields[b'ATRN']]
 constants=[v for v, in struct.iter_unpack('<f',fields[b'STRN'])]
 def interval(channel):
  if frame<=channel[0][0]:return channel[0][1],channel[0][1],0
  if frame>=channel[-1][0]:return channel[-1][1],channel[-1][1],0
  low=max(i for i,(t,_) in enumerate(channel) if t<=frame);a,b=channel[low:low+2]
  return a[1],b[1],f32((frame-a[0])/(b[0]-a[0]))
 def sampleq(channel):
  a,b,t=interval(channel);dot=sum(x*y for x,y in zip(a,b))
  if dot<0:b=[-v for v in b];dot=-dot
  dot=min(1,dot)
  if dot>.999999:q=[(1-t)*x+t*y for x,y in zip(a,b)]
  else:
   angle=math.acos(dot);q=[(math.sin((1-t)*angle)*x+math.sin(t*angle)*y)/math.sin(angle) for x,y in zip(a,b)]
  norm=math.sqrt(sum(v*v for v in q));return [v/norm for v in q]
 result={}
 for _,data in fields[b'XFRM']:
  stop=data.index(0);animated,r,mask,*indices=struct.unpack('<BHB3H',data[stop+1:]);q=sampleq(rotations[r]) if animated else static[r]
  p=[]
  for axis,index in enumerate(indices):
   if mask&(8<<axis):a,b,t=interval(translations[index]);p.append((1-t)*a+t*b)
   else:p.append(constants[index])
  result[data[:stop].decode('ascii').lower()]=(q,p)
 return result
def vertices(raw,skraw,samples):
 names,parents,arrays=skeletons(skraw)[0];inv=pose((names,parents,arrays));worlds=[]
 for i,name in enumerate(names):
  if name.decode('ascii').lower() in samples:
   xyzw,p=samples[name.decode('ascii').lower()];clip=[xyzw[3],*xyzw[:3]]
   q=quaternion(arrays[b'RPST'][i],quaternion(quaternion(clip,arrays[b'BPRO'][i]),arrays[b'RPRE'][i]));norm=math.sqrt(sum(v*v for v in q));q=[v/norm for v in q]
   local=rotation(q,[a+b for a,b in zip(arrays[b'BPTR'][i],p)])
  else:local=rotation(quaternion(quaternion(arrays[b'RPST'][i],arrays[b'BPRO'][i]),arrays[b'RPRE'][i]),arrays[b'BPTR'][i])
  worlds.append(local if parents[i]<0 else multiply(worlds[parents[i]],local))
 root,=nodes(raw);records=root[1][0][1];fields=dict(records[:6]);transforms=fields[b'XFNM'].split(b'\0')[:-1]
 lookup={name.lower():i for i,name in enumerate(names)};palette=[multiply(worlds[lookup[n.lower()]],inv[lookup[n.lower()]]) for n in transforms]
 points=list(struct.iter_unpack('<3f',fields[b'POSN']));counts=list(struct.iter_unpack('<i',fields[b'TWHD']));weights=list(struct.iter_unpack('<If',fields[b'TWDT']))
 starts=[];cursor=0
 for count, in counts:starts.append(cursor);cursor+=count
 shader=next(dict(r) for tag,r in records if tag==b'PSDT');indices=[v for v, in struct.iter_unpack('<I',shader[b'PIDX'][4:])];result=[]
 for point in indices[:8]:
  group=weights[starts[point]:starts[point]+counts[point][0]];v=points[point]
  p=[sum(w*(sum(palette[j][axis][k]*v[k] for k in range(3))+palette[j][axis][3]) for j,w in group) for axis in range(3)]
  result.append([p[0],p[1],-p[2]])
 return result
def generate(exe):
 assert hashlib.sha256(Path('C:/Stardust-DEV/swgemu_live.cfg').read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
 with contextlib.ExitStack() as stack:
  indexes=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(Path('C:/Stardust-DEV'),Path('C:/Stardust-DEV/swgemu_live.cfg'))];identities=[]
  def lookup(path,pin=None):
   for priority,index in indexes:
    raw=index.lookup(path)
    if raw is not None:break
   else:raise ValueError(path)
   h=hashlib.sha256(raw).hexdigest().upper()
   if pin:assert h==pin
   identities.append(dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=h));return raw
  clips={};metadata={}
  for key,path,pin in CLIPS:
   raw=lookup(path,pin);clips[key]=raw;root,=nodes(raw);fields=dict(root[1][0][1]);rate,end,*_=struct.unpack('<f6h',fields[b'INFO']);speed=struct.unpack_from('<f',fields[b'LOCT'])[0] if b'LOCT' in fields else 0;metadata[key]=(rate,end,speed)
  mesh=lookup('appearance/mesh/hum_m_body_l0.mgn','93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0');root,=nodes(mesh);fields=dict(root[1][0][1][:6]);skeleton=lookup(fields[b'SKTM'].split(b'\0')[0].decode('ascii'),'7F3EB762EB97B27C2170014BE66BFCA0C232E35B2F8CDBEFEA912736FB681CC3');table=build_table(exe.read_bytes())
  phase=distance=0;mode='idle';switches=0;heading=[0,0,1];expected=[]
  for step in STEPS:
   if step['Advance']:
    x,z=f32(step['X']),f32(step['Z']);d=f32(math.sqrt(f32(f32(x*x)+f32(z*z))));new='idle' if d<=1e-6 else 'run' if d/step['Seconds']>=4 else 'walk';rate,end,speed=metadata[new]
    phase=(phase+(step['Seconds']*rate/end if new=='idle' else d*rate/(speed*end)))%1;distance+=d;switches+=new!=mode;mode=new
    if d>1e-6:heading=[f32(x/d),0,f32(z/d)]
   rate,end,_=metadata[mode];frame=phase*end
   expected.append(dict(Label=step['Label'],Mode=mode,Frame=frame,NormalizedPhase=phase,AcceptedDistance=distance,Switches=switches,Heading=heading,Vertices=vertices(mesh,skeleton,decode(clips[mode],table,frame))))
 return expected,identities
def stage(out):
 frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round71/frozen');raw=(frozen/'baseline.json').read_bytes();assert hashlib.sha256(raw).hexdigest()=='12bb3453e94a2041068db521c9cdcea6486a009baedab72a7e2f6e73cf814c37'
 archive=frozen/'baseline.zip';assert hashlib.sha256(archive.read_bytes()).hexdigest()=='eb66e5727b76f79969738c26c5ac89da1cb16f7422612920cacd4d26b9f7428d';rows=json.loads(raw)['files'];assert len(rows)==4297;selected=[]
 with zipfile.ZipFile(archive) as z:
  assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4297 and set(z.namelist())=={r['path'] for r in rows}
  for row in rows:
   name=row['path'];assert ':' not in name and '\\' not in name and all(v not in ('','.','..') for v in name.split('/'))
   if not(name.startswith('poc/Stardust.Core/') or name.startswith('poc/Stardust.Godot/') and name.endswith('.cs')):continue
   entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000
   raw=z.read(name);assert len(raw)==row['bytes'] and hashlib.sha256(raw).hexdigest()==row['sha256']
   dest=out/('frozen-core' if name.startswith('poc/Stardust.Core/') else 'frozen-godot')/Path(name).relative_to('poc/Stardust.Core' if name.startswith('poc/Stardust.Core/') else 'poc/Stardust.Godot');dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw);selected.append(row)
 (out/'frozen-source-identity.json').write_text(json.dumps(selected,indent=2)+'\n')
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  stage(out);expected,identities=generate(a.exe);(out/'steps.json').write_text(json.dumps(STEPS,indent=2)+'\n');(out/'expected.json').write_text(json.dumps(expected,indent=2)+'\n');(out/'original-identity.json').write_text(json.dumps(identities,indent=2)+'\n')
  spec=importlib.util.spec_from_file_location('peer13',ROOT/'poc/round13-tests/Rendering/Generate-Fixture.py');sys.argv=[str(spec.origin),str(out/'peer.json')];module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
  peer=json.loads((out/'peer.json').read_text());peer['a_height']=(struct.pack('<HIQIBIH',5,0x12862153,0xF122334455667788,0x4352454F,3,8,1)+struct.pack('<Hf',14,1.4)).hex().upper();(out/'peer.json').write_text(json.dumps(peer,indent=2)+'\n')
  print('Independent expectations generated before native');return
 n=json.loads(a.native.read_text());expected=json.loads((out/'expected.json').read_text());assert n['Passed'] and n['NoSecondClock'] and len(n['Negatives'])==23 and n['OriginalSources']>0;maximum=0
 assert len(n['Samples'])==len(expected)==6
 for got,want in zip(n['Samples'],expected):
  for key in ('Label','Mode','Switches'):assert got[key]==want[key],(key,got,want)
  for key in ('Frame','NormalizedPhase','AcceptedDistance'):assert abs(got[key]-want[key])<1e-6,(key,got[key],want[key])
  assert max(abs(a-b) for a,b in zip(got['Heading'],want['Heading']))<1e-6
  error=max(abs(a-b) for gv,wv in zip(got['Vertices'],want['Vertices']) for a,b in zip(gv,wv));assert error<2e-5,(got['Label'],error);maximum=max(maximum,error)
 receipt=dict(Passed=True,Checks=n['Checks'],IndependentStates=6,IndependentOriginalVertices=48,MaximumVertexError=maximum,Negatives=23,Gpu=n['Gpu'],ExpectedBeforeNative=True,NoNativeSeededExpected=True,NoSecondClock=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
