"""Authored byte fixtures plus independent original IFF/DDS identities; no native-seeded expectations."""
import argparse,contextlib,hashlib,io,json,math,os,stat,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
ORIGINAL_PATH='shader/defaultemis.sht';ORIGINAL_PIN='BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7'
S='shader/synthetic.sht';T='texture/synthetic.dds';MAIN=0x4D41494E
DEFAULT=dict(MaximumReads=2,MaximumShaderBytes=1048576,MaximumTextureBytes=67108864,MaximumSourceBytes=68157440,MaximumDimension=8192,MaximumMips=16,MaximumDecodedBytes=134217728)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def chunk(t,b):return t+struct.pack('>I',len(b))+b
def form(t,b):return chunk(b'FORM',t+b)
def shader(bindings=None,values=None,version=b'0001',extra=b''):
 bindings=[(MAIN,0,T)] if bindings is None else bindings
 tx=b''.join(form(b'TXM ',form(b'0002',chunk(b'DATA',struct.pack('<IB7B',tag,flag,0,0,0,2,2,2,0))+(chunk(b'NAME',path.encode()+b'\0') if path is not None else b''))) for tag,flag,path in bindings)
 materials=b'' if values is None else form(b'MATS',form(b'0000',chunk(b'TAG ',struct.pack('<I',MAIN))+chunk(b'MATL',struct.pack('<17f',*values))))
 return form(b'SSHT',form(version,chunk(b'NAME',b'effect/unused.eft\0')+materials+form(b'TCSS',chunk(b'0000',struct.pack('<IB',MAIN,0)))+form(b'TFNS',chunk(b'0000',struct.pack('<II',0x44494646,0x80402010)))+form(b'TXMS',tx)+extra))
def dds(w=4,h=4,mips=3,dxgi=None):
 header=bytearray(148 if dxgi is not None else 128);header[:4]=b'DDS ';struct.pack_into('<7I',header,4,124,0x2100F,h,w,w*4,0,mips);struct.pack_into('<I',header,76,32);struct.pack_into('<I',header,108,0x401008)
 if dxgi is None:struct.pack_into('<7I',header,80,0x41,0,32,0xFF0000,0xFF00,0xFF,0xFF000000)
 else:struct.pack_into('<II',header,80,4,0x30315844);struct.pack_into('<5I',header,128,dxgi,3,0,1,1)
 payload=b'';images=[]
 for level in range(mips):
  mw=max(1,w>>level);mh=max(1,h>>level);rgba=bytes(v for i in range(mw*mh) for v in ((i*19+level*7)%256,(i*13+31)%256,(i*7+71)%256,(i*11+127)%256));images.append(dict(Width=mw,Height=mh,Bytes=len(rgba),Sha256=sha(rgba)))
  payload+=rgba if dxgi is not None else bytes(rgba[i+j] for i in range(0,len(rgba),4) for j in (2,1,0,3))
 return bytes(header)+payload,images
COLORS=[dict(Texel=list(map(bits,t)),Vertex=list(map(bits,v))) for t,v in [((1,1,1,1),(.25,.5,.75,1)),((.25,.5,.75,.125),(.8,.6,.4,.2)),((0,0,0,0),(1,1,1,1))]]
def metadata(raw):
 from verify_static_mesh_slice import nodes
 root,=nodes(raw);ver,=root[1];children=ver[1];name=next(v for t,v in children if t==b'NAME').rstrip(b'\0').decode();name=name if '/' in name else 'effect/'+name
 materials={};coords={};factors={};bindings=[]
 for tag,data in children:
  if tag==b'MATS':
   version,=data
   for i in range(0,len(version[1]),2):k=struct.unpack('<I',version[1][i][1])[0];materials[str(k)]=list(struct.unpack('<17I',version[1][i+1][1]))
  elif tag==b'TCSS':
   _,blob=data[0]
   for i in range(0,len(blob),5):coords[str(struct.unpack_from('<I',blob,i)[0])]=blob[i+4]
  elif tag==b'TFNS':
   _,blob=data[0]
   for i in range(0,len(blob),8):k,v=struct.unpack_from('<II',blob,i);factors[str(k)]=v
  elif tag==b'TXMS':
   for _,bv in data:
    vv,=bv;fields=dict(vv[1]);blob=fields[b'DATA'];version=vv[0].decode();k=struct.unpack_from('<I',blob,1 if version=='0000' else 0)[0];flag=blob[0 if version=='0000' else 4]
    bindings.append(dict(Tag=k,SourceFlag=flag,Version=version,AssetPath=fields.get(b'NAME',b'')[:-1].decode()))
 binding=next(v for v in bindings if v['Tag']==MAIN);authored=None
 if str(MAIN) in materials:
  v=materials[str(MAIN)];authored=[v[5],v[6],v[7],v[4]]
 return dict(Version=ver[0].decode(),EffectPath=name,Binding=binding,Materials=materials,Coordinates=coords,Factors=factors,Authored=authored)
def model(raw,tex,images,root=S,priority=77,texturePriority=78,policy=0):
 v=metadata(raw);diffuse=v['Authored'] if policy==1 and v['Authored'] is not None else list(map(bits,(1,1,1,1)))
 dxgi=struct.unpack_from('<I',tex,128)[0] if tex[84:88]==b'DX10' else None
 fmt={28:'Rgba32',29:'Rgba32',87:'Bgra32',91:'Bgra32'}.get(dxgi,'Bgra32')
 return dict(ShaderSource=dict(Path=root,Priority=priority,Bytes=len(raw),Sha256=sha(raw)),TextureSource=dict(Path=v['Binding']['AssetPath'],Priority=texturePriority,Bytes=len(tex),Sha256=sha(tex)),SourceBytes=len(raw)+len(tex),DecodedBytes=sum(i['Bytes'] for i in images),ExternalReads=2,Width=images[0]['Width'],Height=images[0]['Height'],MipCount=len(images),IsCompleteMipChain=len(images)==max(images[0]['Width'],images[0]['Height']).bit_length(),IsSingleMipOrCompleteChain=len(images)==1 or len(images)==max(images[0]['Width'],images[0]['Height']).bit_length(),Format=fmt,ColorSpace='Srgb' if dxgi in (29,91) else 'Linear' if dxgi is not None else 'Unspecified',AlphaMode='Straight' if dxgi is not None else 'Unknown',DxgiFormat=dxgi,Diffuse=diffuse,Mips=images,OriginalTerrainAssociation=False,OriginalShaderExecutionParity=False,**v)
def shade(colors,diffuse):
 d=[struct.unpack('<f',struct.pack('<I',x))[0] for x in diffuse];out=[]
 for c in colors:
  t=[struct.unpack('<f',struct.pack('<I',x))[0] for x in c['Texel']];v=[struct.unpack('<f',struct.pack('<I',x))[0] for x in c['Vertex']]
  out.append([bits(f32(f32(a*b)*f)) for a,b,f in zip(t,v,d)])
 return out

def fixtures():
 out=[];expected=[];base=shader();tex,im=dds();values=[1,.1,.2,.3,.8,.25,.5,.75,1,0,0,0,1,.4,.5,.6,16]
 def add(name,raw=base,texture=tex,images=im,policy=0,limits=None,reject=False,calls=None,pin=None,root=S,resources=None,**extra):
  l=dict(DEFAULT,**(limits or {}));rr=[dict(Path=S,Kind='Present',Hex=raw.hex(),Priority=77),dict(Path=T,Kind='Present',Hex=texture.hex(),Priority=78)] if resources is None else resources
  c=dict(Name=name,RootPath=root,Pin=sha(raw) if pin is None else pin,Policy=policy,Limits=l,Resources=rr,Colors=COLORS,**extra);out.append(c)
  e=dict(Name=name,Reject='Input' if reject else 'None',Calls=([S,T] if not reject else [S]) if calls is None else calls)
  if not reject:e['Material']=model(raw,texture,images,policy=policy);e['Colors']=shade(COLORS,e['Material']['Diffuse'])
  expected.append(e)
 add('default');add('authored-default-retains-only',raw=shader(values=values));add('authored-multiply',raw=shader(values=values),policy=1);add('missing-authored-explicit-white',policy=1)
 add('unrelated-dynamic-not-read',raw=shader([(MAIN,0,T),(0x4E524D4C,1,None)]));add('version0000',raw=shader(version=b'0000'))
 for dxgi in (28,29,87,91):
  t,mi=dds(dxgi=dxgi)
  if dxgi in (87,91):
   # BGRA storage for independently authored RGBA images.
   t=t[:148]+bytes(t[i+j] for i in range(148,len(t),4) for j in (2,1,0,3))
  add('dxgi-'+str(dxgi),texture=t,images=mi)
 t,mi=dds(4,2,1);add('single-mip-upload-eligible',texture=t,images=mi)
 t,mi=dds(4,4,2);add('partial-chain-cpu-accepted-host-reject',texture=t,images=mi)
 for tag in (b'CSHD',b'SWTS'):add(tag.decode(),raw=form(tag,form(b'0000',b'')),reject=True)
 for raw in (shader([]),shader([(MAIN,1,T)]),shader([(MAIN,0,None)]),shader([(MAIN,0,T)]*2),shader(version=b'0002'),base[:-1],base+b'x',base[:7]):add('invalid-ssht-'+str(len(out)),raw=raw,reject=True)
 for v in (math.nan,math.inf,-math.inf,1.01,-.01):a=values[:];a[5]=v;add('nonfinite-hdr-'+str(len(out)),raw=shader(values=a),reject=True)
 for raw in (shader([(0x4E524D4C,0,T)]),shader(extra=form(b'MATS',form(b'0001',b''))),shader(values=values,extra=form(b'MATS',form(b'0000',b''))),shader(extra=form(b'TFNS',chunk(b'0000',b''))),base.replace(form(b'TFNS',chunk(b'0000',struct.pack('<II',0x44494646,0x80402010))),form(b'TFNS',chunk(b'0000',b'1234567'))),base.replace(form(b'TCSS',chunk(b'0000',struct.pack('<IB',MAIN,0))),form(b'TCSS',chunk(b'0000',struct.pack('<IB',MAIN,16))))):add('invalid-parameter-factor-main-'+str(len(out)),raw=raw,reject=True)
 a=values[:];a[16]=math.nan;add('nonfinite-material-power',raw=shader(values=a),reject=True)
 add('wrong-pin',pin='0'*64,reject=True);add('short-pin',pin='0'*63,reject=True,calls=[]);add('wrong-policy',policy=99,reject=True,calls=[])
 for p in ('Shader/synthetic.sht','/shader/synthetic.sht','shader/../synthetic.sht','shader/a:synthetic.sht','shader//synthetic.sht'):add('path-'+str(len(out)),root=p,reject=True,calls=[])
 for p in ('texture/../synthetic.dds','texture/a:synthetic.dds','shader/a.sht'):add('binding-path-'+str(len(out)),raw=shader([(MAIN,0,p)]),reject=True)
 for kind in ('Missing','Masked'):
  for path,calls in ((S,[S]),(T,[S,T])):
   rr=[dict(Path=S,Kind='Present',Hex=base.hex(),Priority=77),dict(Path=T,Kind='Present',Hex=tex.hex(),Priority=78)];next(v for v in rr if v['Path']==path).update(Kind=kind,Hex=None);add(kind+path,reject=True,calls=calls,resources=rr)
 for path,calls in ((S,[S]),(T,[S,T])):
  rr=[dict(Path=S,Kind='Present',Hex=base.hex(),Priority=77),dict(Path=T,Kind='Present',Hex=tex.hex(),Priority=78)];next(v for v in rr if v['Path']==path)['Hex']=None;add('null-payload-'+path,reject=True,calls=calls,resources=rr)
 for raw in (b'BAD!'+tex[4:],tex[:127],tex[:-1],tex+b'x',dds(dxgi=95)[0]):add('invalid-dds-'+str(len(out)),texture=raw,reject=True,calls=[S,T])
 exact=dict(MaximumReads=2,MaximumShaderBytes=len(base),MaximumTextureBytes=len(tex),MaximumSourceBytes=len(base)+len(tex),MaximumDimension=4,MaximumMips=3,MaximumDecodedBytes=84);add('exact-all',limits=exact)
 for k,v in exact.items():add('one-less-'+k,limits=dict(exact,**{k:v-1}),reject=True,calls=[] if k=='MaximumReads' else [S] if k=='MaximumShaderBytes' else [S,T])
 for k in DEFAULT:
  add('negative-'+k,limits={k:-1},reject=True,calls=[])
 hard=dict(MaximumReads=2,MaximumShaderBytes=16777216,MaximumTextureBytes=67108864,MaximumSourceBytes=134217728,MaximumDimension=32768,MaximumMips=16,MaximumDecodedBytes=134217728)
 for k,v in hard.items():add('hard-plus-'+k,limits={k:v+1},reject=True,calls=[])
 add('precancel',PreCancel=True,reject=True,calls=[])
 for i in (1,2):add('cancel-resolve-'+str(i),CancelRead=i,reject=True,calls=[S] if i==1 else [S,T])
 return out,expected

def original(root,config):
 from cmpa_corpus_inventory import archives
 from verify_textured_mesh_bundle import Index
 from verify_dds_pixels import inspect
 assert sha(config.read_bytes())==CONFIG
 with contextlib.ExitStack() as stack:
  indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
  def read(path):
   for priority,index in indices:
    data=index.lookup(path)
    if data is not None:return data,priority
   raise ValueError('Missing pinned original diagnostic source')
  raw,p=read(ORIGINAL_PATH);assert sha(raw)==ORIGINAL_PIN;meta=metadata(raw);texturePath=meta['Binding']['AssetPath'];texture,tp=read(texturePath)
  with contextlib.redirect_stdout(io.StringIO()):hashes=inspect(texture)
  h,w=struct.unpack_from('<II',texture,12);images=[dict(Width=max(1,w>>i),Height=max(1,h>>i),Bytes=max(1,w>>i)*max(1,h>>i)*4,Sha256=d) for i,d in enumerate(hashes)]
  m=model(raw,texture,images,root=ORIGINAL_PATH,priority=p,texturePriority=tp)
  fourcc=texture[84:88];m['Format']={b'DXT1':'Bc1',b'DXT3':'Bc2',b'DXT5':'Bc3'}.get(fourcc,m['Format'])
  c=dict(Name='original-terminal-base-diagnostic',RootPath=ORIGINAL_PATH,Pin=ORIGINAL_PIN,Policy=0,Limits=DEFAULT,Resources=[dict(Path=ORIGINAL_PATH,Kind='Present',Hex=raw.hex(),Priority=p),dict(Path=texturePath,Kind='Present',Hex=texture.hex(),Priority=tp)],Colors=COLORS)
  return c,dict(Name=c['Name'],Reject='None',Calls=[ORIGINAL_PATH,texturePath],Material=m,Colors=shade(COLORS,m['Diffuse']))
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--exe',type=Path,required=True);parser.add_argument('--root',type=Path,required=True);parser.add_argument('--config',type=Path,required=True);parser.add_argument('--out',type=Path,required=True);parser.add_argument('--native',type=Path);a=parser.parse_args()
 assert sha(a.exe.read_bytes())==EXE and sha(a.config.read_bytes())==CONFIG
 a.out=Path(os.path.abspath(a.out));assert a.out.is_relative_to(ROOT/'local-private')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(info.st_mode) and not(getattr(info,'st_file_attributes',0)&0x400) and stat.S_ISDIR(info.st_mode)
 sys.path.insert(0,str(ROOT/'tools/python'));sys.path.insert(0,'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages')
 if a.native:
  got=json.loads(a.native.read_text());want=json.loads((a.out/'material-expected.json').read_text());assert len(got['Cases'])==len(want)
  for g,w in zip(got['Cases'],want):assert g==w,(w['Name'],g,w)
  o=want[-1]['Material'];receipt=dict(Passed=True,Cases=len(want),Accepted=sum(w['Reject']=='None' for w in want),Rejected=sum(w['Reject']=='Input' for w in want),OwnershipChecks=got['OwnershipChecks'],IndependentByteFixtures=True,NativeSeededExpectedValues=False,OriginalShaderPath=ORIGINAL_PATH,OriginalShaderSha256=ORIGINAL_PIN,OriginalTexture=o['TextureSource'],OriginalMips=o['Mips'],OriginalDecodedBytes=o['DecodedBytes'],OriginalSourceBytes=o['SourceBytes'],OriginalTerrainAssociation=False,OriginalShaderExecutionParity=False,ExternalReads=2,EffectReads=0,UnrelatedTextureReads=0,OriginalMaterial=o,Modulation='binary32 MAIN texel * vertex color * optional authored MAIN diffuse; missing diffuse white')
  (a.out/'material-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 cases,want=fixtures();c,e=original(a.root,a.config);cases.append(c);want.append(e)
 (a.out/'material-fixtures.json').write_text(json.dumps(cases),encoding='utf-8');(a.out/'material-expected.json').write_text(json.dumps(want),encoding='utf-8');print(json.dumps(dict(Cases=len(cases),OriginalShader=e['Material']['ShaderSource'],OriginalTexture=e['Material']['TextureSource'])))
if __name__=='__main__':main()
