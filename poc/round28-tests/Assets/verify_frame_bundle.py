"""Independent SWTS frame-bundle fixtures and original source/mip oracle."""
import contextlib,hashlib,io,json,math,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_dds_pixels import inspect as pixels
SWTS='shader/anim_mission_terminal.sht';SWTS_PIN='B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62'
BASE='shader/defaultemis.sht';BASE_PIN='BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
def original(root,config):
 assert hashlib.sha256(config.read_bytes()).hexdigest().upper()==CONFIG
 sources={};images={}
 with contextlib.ExitStack() as stack:
  indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
  def read(path):
   assert path not in sources
   for priority,index in indices:
    raw=index.lookup(path)
    if raw is not None:break
   else:raise AssertionError(path)
   assert len(raw)<=1048576;sources[path]=dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper());return raw
  raw=read(SWTS);assert sources[SWTS]['Sha256']==SWTS_PIN
  node,=nodes(raw);version,=node[1];assert node[0]==b'SWTS' and version[0]==b'0000';fields=version[1];assert len(fields)==10
  assert fields[0]==(b'NAME',BASE.encode()+b'\0');state,=fields[1][1];assert fields[1][0]==b'DTST' and state[0]==b'0000' and struct.unpack('<II',state[1])==(8,0x3DCCCCCD)
  frames=[]
  for index,(tag,payload) in enumerate(fields[2:]):
   textureTag,=struct.unpack_from('<I',payload);path=payload[4:-1].decode('ascii');assert tag==b'TEXT' and payload[-1]==0 and textureTag==0x4D41494E
   assert path==f'texture/anim_mission_terminal_{index+1:02d}.dds';frames.append(dict(Index=index,TextureTag=textureTag,TexturePath=path))
  raw=read(BASE);assert sources[BASE]['Sha256']==BASE_PIN;node,=nodes(raw);v,=node[1];assert node[0]==b'SSHT' and v[0]==b'0001';bindings=dict(v[1]);assert bindings[b'NAME']==b'effect/a_emis_full.eft\0'
  txm,=bindings[b'TXMS'];bindingVersion,=txm[1];binding=dict(bindingVersion[1]);assert txm[0]==b'TXM ' and bindingVersion[0]==b'0002' and binding[b'DATA']==bytes.fromhex('4E49414D0000000002020200') and binding[b'NAME']==b'texture/defaulttexture.dds\0'
  for frame in frames:
   path=frame['TexturePath'];raw=read(path)
   with contextlib.redirect_stdout(io.StringIO()):hashes=pixels(raw)
   height,width=struct.unpack_from('<II',raw,12);mips=[]
   for level,digest in enumerate(hashes):
    w,h=max(1,width>>level),max(1,height>>level);mips.append(dict(Width=w,Height=h,Bytes=w*h*4,Sha256=digest))
   format_name={b'DXT1':'Bc1',b'DXT3':'Bc2',b'DXT5':'Bc3'}[raw[84:88]];images[path]=dict(Format=format_name,ColorSpace='Unspecified',AlphaMode='Unknown',DxgiFormat=None,Mips=mips,RetainedTerminalBytes=0)
 return dict(ShaderPath=SWTS,BaseShaderPath=BASE,RawDtstWord0=8,RawDtstWord1=0x3DCCCCCD,FrameCount=8,IntervalBits=0x3DCCCCCD,Frames=frames,Images=images,Sources=sources,SourceBytes=sum(s['Bytes'] for s in sources.values()),DecodedBytes=sum(m['Bytes'] for i in images.values() for m in i['Mips']),MipCount=sum(len(i['Mips']) for i in images.values()))
if sys.argv[1]=='probe':
 data=original(Path(sys.argv[2]),Path(sys.argv[3]));Path(sys.argv[4]).write_text(json.dumps(data,indent=2),encoding='utf-8')
 print(json.dumps(dict(sources=len(data['Sources']),frames=data['FrameCount'],source_bytes=data['SourceBytes'],decoded_bytes=data['DecodedBytes'],mips=data['MipCount'],source_identities=list(data['Sources'].values())),indent=2));sys.exit()

FRAME_PINS=['6BA1EC54844D2614DCAF9C37441C7373D63786F2F3B0D43B953F9A0F15BCC938','226A16D33CCFFE97EA9BA07B13F13D4B08C973006FC99C9959C670CCDE382ABD','87D602AF5538E04B55B71570F3736AAE4782C953A6E6F7D2CD2BBD3CE60C7B95','347572A7A980F2A29775300C2D68AD72986C7E3264E6347B2E769D49B807C897','829D371B0AFE48C607F2CA4AD0CD0A9B1E46372364896637072491A47B1A5886','698E795675D992628CB5F92441E5508057AA7F479EF9BA2DA5DBDBE0CD27716C','913AB9059C0F1EEF20F5EC4573F564490418ADAE517742B117AC52736F588E95','828EB4C1CB4590F9BDFD45992BA117B63D323791AC3DE1E8560A3FB032B36652']
def chunk(tag,payload):return tag+struct.pack('>I',len(payload))+payload
def form(tag,payload):return chunk(b'FORM',tag+payload)
S='shader/root.sht';T='shader/base.sht';A='texture/a.dds';B='texture/b.dds';MAIN=0x4D41494E

def swts(frames=None,count=None,interval=0x3E000000,base=T,version=b'0000',state=None):
 frames=[(MAIN,A),(MAIN,B)] if frames is None else frames
 fields=chunk(b'NAME',base.encode('ascii')+b'\0')+form(b'DTST',chunk(b'0000',struct.pack('<II',len(frames) if count is None else count,interval) if state is None else state))
 fields+=b''.join(chunk(b'TEXT',struct.pack('<I',tag)+(path.encode('ascii')+b'\0' if isinstance(path,str) else path)) for tag,path in frames)
 return form(b'SWTS',form(version,fields))
def ssht(bindings=None):
 bindings=[(MAIN,0,'texture/defaulttexture.dds')] if bindings is None else bindings
 rows=[]
 for tag,flag,path in bindings:
  binding=chunk(b'DATA',struct.pack('<I',tag)+bytes([flag])+bytes(7))
  if path is not None:binding+=chunk(b'NAME',path.encode('ascii')+b'\0')
  rows.append(form(b'TXM ',form(b'0002',binding)))
 return form(b'SSHT',form(b'0001',chunk(b'NAME',b'effect/synthetic.eft\0')+form(b'TXMS',b''.join(rows))))
def dds(width=2,height=1,count=2):
 data=bytearray(128);data[:4]=b'DDS ';struct.pack_into('<7I',data,4,124,0x2100F,height,width,width*4,0,count);struct.pack_into('<8I',data,76,32,0x41,0,32,0xFF0000,0xFF00,0xFF,0xFF000000);struct.pack_into('<I',data,108,0x1000)
 for level in range(count):
  w,h=max(1,width>>level),max(1,height>>level)
  for i in range(w*h):data+=bytes([i+3,level+5,i+9,255])
 return bytes(data)
def tail_dds():
 data=bytearray(128);data[:4]=b'DDS ';struct.pack_into('<7I',data,4,124,0xA1007,4,8,32,0,3);struct.pack_into('<8I',data,76,32,4,int.from_bytes(b'DXT3','little'),0,0,0,0,0);struct.pack_into('<I',data,108,0x1000)
 block=bytes([255]*8)+struct.pack('<HHI',0xF800,0x001F,0)
 return bytes(data)+block*2+block+block+block
RA=dds();RB=dds(1,1,1);RBASE=ssht();RSHADER=swts()
def limits():return dict(MaximumFrames=8,MaximumSources=10,MaximumSourceBytes=1048576,MaximumDecodedBytes=1024,MaximumMips=32,MaximumShaderBytes=4096,DdsLimits=dict(MaximumDimension=16,MaximumInputBytes=1024,MaximumDecodedBytes=1024,AllowLegacyTerminalBlockTail=False))
def image(raw,tail=False):
 body=raw[:-16] if tail else raw
 with contextlib.redirect_stdout(io.StringIO()):hashes=pixels(body)
 height,width=struct.unpack_from('<II',body,12);format_name={b'DXT1':'Bc1',b'DXT3':'Bc2',b'DXT5':'Bc3'}.get(body[84:88],'Bgra32')
 return dict(Format=format_name,ColorSpace='Unspecified',AlphaMode='Unknown',DxgiFormat=None,Mips=[dict(Width=max(1,width>>i),Height=max(1,height>>i),Bytes=max(1,width>>i)*max(1,height>>i)*4,Sha256=digest) for i,digest in enumerate(hashes)],RetainedTerminalBytes=16 if tail else 0)
def fixtures():
 result=[]
 def add(name,shader=RSHADER,base=RBASE,resources=None,budget=None,root=S,reject='None',calls=None,pin=None):
  sources={root:shader,T:base,A:RA,B:RB} if resources is None else resources
  rows=[dict(Path=p,Kind=v[0] if isinstance(v,tuple) else 'Missing' if v is None else 'Present',Hex=None if v is None else (v[1] if isinstance(v,tuple) else v).hex().upper()) for p,v in sources.items()]
  result.append(dict(Name=name,RootPath=root,RootSha256=hashlib.sha256(shader).hexdigest().upper() if pin is None else pin,Resources=rows,Limits=limits() if budget is None else budget,Reject=reject,ExpectedCalls=[root,T,A,B] if calls is None else calls))
 exact=limits();exact.update(MaximumFrames=2,MaximumSources=4,MaximumSourceBytes=len(RSHADER)+len(RBASE)+len(RA)+len(RB),MaximumDecodedBytes=16,MaximumMips=3,MaximumShaderBytes=max(len(RSHADER),len(RBASE)));exact['DdsLimits'].update(MaximumDimension=2,MaximumInputBytes=140,MaximumDecodedBytes=12)
 add('exact-limits',budget=exact);add('alias-path',shader=swts([(MAIN,A),(MAIN,B),(MAIN,A)]));add('equal-bytes-distinct-paths',resources={S:RSHADER,T:RBASE,A:RA,B:RA})
 add('different-frame-tags',shader=swts([(MAIN,A),(0x4E4F524D,B)]),base=ssht([(MAIN,0,'texture/defaulttexture.dds'),(0x4E4F524D,0,'texture/normal.dds')]))
 add('positive-subnormal-interval',shader=swts(interval=1));add('single-frame',shader=swts([(MAIN,A)]),calls=[S,T,A])
 long='texture/'+('a'*1012)+'.dds';add('path-length-limit',shader=swts([(MAIN,long)]),resources={S:swts([(MAIN,long)]),T:RBASE,long:RA},calls=[S,T,long])
 l=limits();l['MaximumDecodedBytes']=184;l['DdsLimits']['AllowLegacyTerminalBlockTail']=True;add('retained-terminal-tail',shader=swts([(MAIN,A)]),resources={S:swts([(MAIN,A)]),T:RBASE,A:tail_dds()},budget=l,calls=[S,T,A])
 l=limits();l['MaximumDecodedBytes']=183;l['DdsLimits']['AllowLegacyTerminalBlockTail']=True;add('retained-tail-one-less',shader=swts([(MAIN,A)]),resources={S:swts([(MAIN,A)]),T:RBASE,A:tail_dds()},budget=l,reject='Input',calls=[S,T,A])
 for field,value,calls in [('MaximumFrames',1,[S]),('MaximumSources',3,[S]),('MaximumSourceBytes',exact['MaximumSourceBytes']-1,[S,T,A,B]),('MaximumDecodedBytes',15,[S,T,A,B]),('MaximumMips',2,[S,T,A,B]),('MaximumShaderBytes',exact['MaximumShaderBytes']-1,[S] if len(RSHADER)>=len(RBASE) else [S,T])]:
  l=json.loads(json.dumps(exact));l[field]=value;add('one-less-'+field,budget=l,reject='Input',calls=calls)
 for field,value in [('MaximumDimension',1),('MaximumInputBytes',139),('MaximumDecodedBytes',11)]:
  l=json.loads(json.dumps(exact));l['DdsLimits'][field]=value;add('dds-one-less-'+field,budget=l,reject='Input',calls=[S,T,A])
 for count in (0,1,3,0xFFFFFFFF,0x80000000):add('bad-count-'+str(count),shader=swts(count=count),reject='Input',calls=[S])
 for interval in (0,0x80000000,0xBF800000,0x7F800000,0x7FC00000):add('bad-interval-'+str(interval),shader=swts(interval=interval),reject='Input',calls=[S])
 add('wrong-pin',pin='0'*64,reject='Input',calls=[S])
 for path in ('/shader/root.sht','shader/../root.sht','shader/Root.sht','shader/a\\b.sht','shader/a:bad.sht','shader//root.sht','shader/.sht','shader/root.sht/'):
  add('unsafe-root-'+path,root=path,reject='Input',calls=[])
 for path in ('/shader/base.sht','shader/Base.sht','shader/a\\base.sht','shader/a:base.sht','shader//base.sht','shader/../base.sht'):
  add('unsafe-base-'+path,shader=swts(base=path),reject='Input',calls=[S])
 for path in ('/texture/a.dds','texture/A.dds','texture/a\\b.dds','texture/a:b.dds','texture//a.dds','texture/../a.dds','texture/a.dds/','texture/'+('a'*1013)+'.dds'):
  add('unsafe-frame-'+path,shader=swts([(MAIN,path)]),reject='Input',calls=[S])
 for raw in (b'',b'texture/a.dds',b'texture/a.dds\0\0',b'texture/\x80.dds\0'):
  add('bad-frame-string-'+raw.hex(),shader=swts([(MAIN,raw)]),reject='Input',calls=[S])
 for base in (ssht([]),ssht([(MAIN,1,'texture/defaulttexture.dds')]),ssht([(MAIN,0,None)]),ssht([(MAIN,0,'texture/defaulttexture.dds')]*2),ssht([(MAIN,0,'/texture/defaulttexture.dds')]),RSHADER):
  add('bad-binding-'+hashlib.sha256(base).hexdigest()[:8],base=base,reject='Input',calls=[S,T])
 for path in (S,T,A,B):
  for kind in ('Missing','Masked'):
   resources={S:RSHADER,T:RBASE,A:RA,B:RB};resources[path]=None if kind=='Missing' else (kind,b'');calls=[S,T,A,B][:[S,T,A,B].index(path)+1];add('absent-'+path+kind,resources=resources,reject='Input',calls=calls)
 for n in range(len(RSHADER)):add('truncate-shader-'+str(n),shader=RSHADER[:n],reject='Input',calls=[S])
 for raw in (RA[:127],RA[:-1],RA+b'\0',bytes(140)):
  add('bad-dds-'+hashlib.sha256(raw).hexdigest()[:8],resources={S:RSHADER,T:RBASE,A:raw,B:RB},reject='Input',calls=[S,T,A])
 for raw in (RBASE[:-1],RBASE+b'\0'):
  add('bad-base-'+hashlib.sha256(raw).hexdigest()[:8],base=raw,reject='Input',calls=[S,T])
 for field,value in [('MaximumFrames',0),('MaximumFrames',257),('MaximumSources',259),('MaximumSourceBytes',268435457),('MaximumDecodedBytes',-1),('MaximumMips',4097),('MaximumShaderBytes',1048577)]:
  l=limits();l[field]=value;add('bad-option-'+field+str(value),budget=l,reject='Options',calls=[])
 return result

def synthetic(f):
 resources={r['Path']:r for r in f['Resources']};shader,=nodes(bytes.fromhex(resources[f['RootPath']]['Hex']));version,=shader[1];fields=version[1];base=fields[0][1][:-1].decode();state,=fields[1][1];word0,word1=struct.unpack('<II',state[1]);frames=[];images={};sources={}
 for path in f['ExpectedCalls']:
  raw=bytes.fromhex(resources[path]['Hex']);sources[path]=dict(Path=path,Priority=88,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper())
 for i,(tag,raw) in enumerate(fields[2:]):
  textureTag,=struct.unpack_from('<I',raw);path=raw[4:-1].decode();frames.append(dict(Index=i,TextureTag=textureTag,TexturePath=path))
  if path not in images:images[path]=image(bytes.fromhex(resources[path]['Hex']),f['Name']=='retained-terminal-tail')
 return dict(ShaderPath=f['RootPath'],BaseShaderPath=base,RawDtstWord0=word0,RawDtstWord1=word1,FrameCount=word0,IntervalBits=word1,Frames=frames,Images=images,Sources=sources,SourceBytes=sum(s['Bytes'] for s in sources.values()),DecodedBytes=sum(m['Bytes'] for t in images.values() for m in t['Mips'])+sum(t['RetainedTerminalBytes'] for t in images.values()),MipCount=sum(len(t['Mips']) for t in images.values()))
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Frame bundle fixtures:',len(fixtures()));sys.exit()
if sys.argv[1]=='verify':
 root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]};s=r['frame_bundle_synthetic'];assert len(s['Cases'])==len(fixtures())
 for f,a in zip(fixtures(),s['Cases']):
  assert a['Name']==f['Name'] and a['Reject']==f['Reject'] and a['Calls']==f['ExpectedCalls'],f['Name']
  if f['Reject']=='None':assert a['Bundle']==synthetic(f),f['Name']
 assert s['ControlledChecks']==14 and s['ConcurrentLoads']==32 and s['Immutable']
 expected=original(root,config);o=r['frame_bundle_original'];assert o['Bundle']==expected and o['Calls']==list(expected['Sources']) and o['ArchivesClosed']
 for i,pin in enumerate(FRAME_PINS):assert expected['Sources'][f'texture/anim_mission_terminal_{i+1:02d}.dds']['Sha256']==pin
 assert 'texture/defaulttexture.dds' not in expected['Sources'] and not any(p.startswith('effect/') for p in expected['Sources'])
 print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),rejected=sum(f['Reject']!='None' for f in fixtures()),controlled_checks=14,concurrent_loads=32,original_sources=len(expected['Sources']),original_frames=8,frame_dimensions_and_bytes=[dict(index=f['Index'],path=f['TexturePath'],width=expected['Images'][f['TexturePath']]['Mips'][0]['Width'],height=expected['Images'][f['TexturePath']]['Mips'][0]['Height'],mips=len(expected['Images'][f['TexturePath']]['Mips']),rgba_bytes=sum(m['Bytes'] for m in expected['Images'][f['TexturePath']]['Mips'])) for f in expected['Frames']],mip_hashes=expected['MipCount'],source_bytes=expected['SourceBytes'],decoded_bytes=expected['DecodedBytes'],default_and_effect_reads=0,world_or_cursor=False),indent=2))
