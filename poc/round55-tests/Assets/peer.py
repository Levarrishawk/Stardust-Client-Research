"""Independent one-layer frame/field/index oracle; no sampler or numeric evaluation."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
t=load('slice_tgen','poc/round33-tests/Assets/verify_tgen_input.py');f=load('slice_families','poc/round50-tests/Assets/peer.py')
b=load('slice_bcir','poc/round35-tests/Assets/verify_bcir_input.py');ah=load('slice_ahfr','tools/python/verify_terrain_height_fractal.py');ff=load('slice_ffra','tools/python/verify_terrain_filter_fractal.py');ad=load('slice_adta','tools/python/verify_terrain_layer_adta.py')
sha=lambda v:hashlib.sha256(v).hexdigest().upper()
finite=lambda v:v&0x7f800000!=0x7f800000
FAMILY=[47,0,0,0,0,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
def familygroup(entries):
 return t.form(b'MGRP',t.form(b'0000',b''.join(t.form(b'MFAM',t.chunk(b'DATA',struct.pack('<i',id)+b'\0')+t.form(b'MFRC',t.form(b'0001',t.chunk(b'DATA',t.words(w))))) for id,w in entries)))
def rule(kind,version,words,name=b'synthetic',enabled=1,header=None,data=None):
 raw=t.words(words) if data is None else data
 return t.form(kind,t.form(version,(header or t.header(name=name,enabled=enabled))+(t.chunk(b'DATA',raw) if kind==b'BCIR' else t.form(b'DATA',t.chunk(b'PARM',raw)))))
def circle(**kw):return rule(b'BCIR',b'0002',[0x3f800000,0x40000000,0xbf800000,0xffffffff,0x40000000],**kw)
def filter(words=None,**kw):return rule(b'FFRA',b'0005',words or [0xfffffff9,0xffffffff,0x3f000000,0,0x40000000,0x3f800000],**kw)
def height(words=None,**kw):return rule(b'AHFR',b'0003',words or [0xfffffff9,1,0x41700000],**kw)
def generator(children=None,h=None,adta=None,version=b'0003',entries=None,last=None):
 groups=[t.form(tag,t.form(v)) for tag,v in zip(t.TAGS[:4],t.VERS[:4])]+[familygroup(entries or [(-7,FAMILY)])]
 layer=t.layer(version,h or t.header(enabled=1),adta,children if children is not None else circle()+filter()+height())
 return t.encode(groups,t.form(b'LYRS',layer) if last is None else last)
def header(blob):
 root,=t.records(blob,0,len(blob));assert root[0]==b'FORM' and root[1][:4]==b'IHDR'
 v,=t.records(blob,12,len(blob));version=v[1][:4].decode();assert version in ('0000','0001')
 d,=t.records(blob,v[2]+12,v[3]);assert d[0]==b'DATA';raw=d[1];end=raw.index(0,4);name=raw[4:end];legacy=raw[end+1:];assert len(legacy)==(3 if version=='0000' else 0)
 return dict(Version=version,EnabledBits=struct.unpack_from('<I',raw)[0],NameSha=sha(name),LegacySha=sha(legacy)),len(name)+len(legacy)
def decoded(blob,kind,version,count):
 root,=t.records(blob,0,len(blob));assert root[0]==b'FORM' and root[1][:4]==kind.encode()
 v,=t.records(blob,12,len(blob));assert v[1][:4].decode()==version
 h,d=list(t.records(blob,v[2]+12,v[3]));hh,names=header(blob[h[2]:h[3]])
 if kind=='BCIR':assert d[0]==b'DATA';raw=d[1]
 else:assert d[0]==b'FORM' and d[1][:4]==b'DATA';p,=t.records(blob,d[2]+12,d[3]);assert p[0]==b'PARM';raw=p[1]
 words=list(struct.unpack('<'+'I'*count,raw));return hh,words,4*count+names

def supported_family(w):return w[12]<=5 and w[1]==w[3]==0 and 1<=w[5]<=16 and w[0]!=0x7fffffff and all(finite(v) for v in w[6:12])
def expected(c):
 fail=dict(Name=c['Name'],Reject='Input')
 try:
  g=bytes.fromhex(c['GeneratorHex']);meta=t.metadata(g);index=c.get('Index',0);policy=c.get('Policy',1)
  if c.get('Cancel') or policy not in (0,1) or index<0 or index>=len(meta['Layers']) or ('BindingHex' in c and bytes.fromhex(c['BindingHex'])!=g):return fail
  layer=meta['Layers'][index];ls=layer['Section'];children=layer['Children'];length=ls['EncodedBytes'];blob=g[ls['Offset']:ls['Offset']+length]
  if ls['FormType']!='LAYR' or ls['Version']!='0003' or layer['Header'] is None or len(children)!=3 or len(layer['AdtaWordBits'])!=3:return fail
  kinds=['BCIR','FFRA','AHFR'];versions=['0002','0005','0003'];counts=[5,6,3]
  if any(s['Tag']!='FORM' or s['FormType']!=kind or s['Version']!=ver for s,kind,ver in zip(children,kinds,versions)):return fail
  rows=[next((j,r) for j,r in enumerate(meta['Rules']) if r['LayerIndex']==index and r['ChildOrdinal']==i+2) for i in range(3)]
  group=next(x for x in meta['Groups'] if x['Role']=='FractalMap')['Section'];gb=g[group['Offset']:group['Offset']+group['EncodedBytes']];entries=f.inspect(gb,f.LIMITS)['Entries']
  lv,=t.records(blob,12,len(blob));lh,adta,*_=t.records(blob,lv[2]+12,lv[3]);head,layerNames=header(blob[lh[2]:lh[3]]);rawadta=adta[1];assert adta[0]==b'ADTA';description=rawadta[12:];assert description[-1]==0;description=description[:-1];layerDecoded=12+layerNames+len(description)
  childblobs=[g[s['Offset']:s['Offset']+s['EncodedBytes']] for s in children];data=[decoded(v,k,ver,n) for v,k,ver,n in zip(childblobs,kinds,versions,counts)]
  ruledata=[];oper=[]
  for i in (1,2):
   h,w,dc=data[i];key=struct.unpack('<i',struct.pack('<I',w[0]))[0];family=next((e for e in entries if e['Id']==key),None)
   if family is None:return fail
   eligible=supported_family(family['Words']) and (all(finite(w[j]) for j in (2,3,4,5)) if i==1 else w[1]<=3 and finite(w[2]));oper.append(h['EnabledBits']!=0 and eligible);s=children[i]
   ruledata.append(dict(GeneratorSha256=sha(g),GroupSha256=sha(gb),RuleIndex=rows[i][0],LayerIndex=index,ChildOrdinal=i+2,RuleOffset=s['Offset'],RuleBytes=s['EncodedBytes'],RuleSha256=sha(childblobs[i]),FamilyId=key,FamilyGeneratorOffset=group['Offset']+family['Offset'],FamilyBytes=family['EncodedBytes'],FamilyRawSha=sha(bytes.fromhex(family['RawHex'])),IsEnabled=h['EnabledBits']!=0,HasOperationalInputs=eligible,Header=h,Words=w))
  circleHeader,circleWords,circleDecoded=data[0];supported=circleHeader['EnabledBits']!=0 and all(finite(circleWords[i]) for i in (0,1,2,4)) and all(oper)
  if policy==1 and not(head['EnabledBits']!=0 and supported):return fail
  childbytes=sum(map(len,childblobs));reservedDecoded=length+12+childbytes;reserved=2*length+12+4*childbytes;metadataWork=3*len(meta['Rules'])+len(meta['Groups'])
  defaults=dict(MaximumLayerBytes=1048576,MaximumChildBytes=65536,MaximumNameBytes=65536,MaximumDecodedCopyBytes=1048576,MaximumSnapshotBytes=8388608,MaximumWork=8388608)
  hard=dict(MaximumLayerBytes=(24,16777216),MaximumChildBytes=(24,4194304),MaximumNameBytes=(0,65536),MaximumDecodedCopyBytes=(0,16777216),MaximumSnapshotBytes=(0,134217728),MaximumWork=(0,134217728));limits={k:c.get(k,v) for k,v in defaults.items()}
  if any(not hard[k][0]<=v<=hard[k][1] for k,v in limits.items()):return fail
  if length>limits['MaximumLayerBytes'] or length>limits['MaximumNameBytes'] or max(map(len,childblobs))>limits['MaximumChildBytes'] or reservedDecoded>limits['MaximumDecodedCopyBytes'] or reserved>limits['MaximumSnapshotBytes'] or reserved+metadataWork>limits['MaximumWork']:return fail
  totalDecoded=layerDecoded+sum(row[2] for row in data);snapshot=length+layerDecoded+3*childbytes+sum(row[2] for row in data)+circleDecoded-20
  cc=[dict(ChildOrdinal=i+2,GlobalRuleIndex=rows[i][0],FormType=kinds[i],Version=versions[i],Offset=s['Offset'],EncodedBytes=s['EncodedBytes'],Sha256=sha(childblobs[i])) for i,s in enumerate(children)]
  out=dict(GeneratorSha256=sha(g),GeneratorBytes=len(g),GroupSha256=sha(gb),GroupOffset=group['Offset'],GroupBytes=len(gb),LayerIndex=index,ParentIndex=layer['ParentIndex'],Depth=layer['Depth'],LayerOffset=ls['Offset'],LayerBytes=length,LayerSha256=sha(blob),IsLocallyEnabled=head['EnabledBits']!=0,BoundaryInverted=layer['AdtaWordBits'][0]!=0,FilterInverted=layer['AdtaWordBits'][1]!=0,AdtaWord2Bits=layer['AdtaWordBits'][2],HasSupportedInputs=supported,Policy=policy,Adta=layer['AdtaWordBits'],DescriptionSha=sha(description),Header=head,Children=cc,Circle=dict(Version='0002',Header=circleHeader,Words=circleWords,Sha256=sha(childblobs[0])),Filter=ruledata[0],Height=ruledata[1],DecodedCopyBytes=totalDecoded,ReservedDecodedCopyBytes=reservedDecoded,SnapshotBytes=snapshot,ReservedSnapshotBytes=reserved,Work=snapshot+metadataWork,ReservedWork=reserved+metadataWork)
  return dict(Name=c['Name'],Reject='None',Snapshot=out)
 except (ValueError,AssertionError,struct.error,StopIteration,IndexError):return fail

def original(root,cfg):
 assert sha(cfg.read_bytes())==t.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in t.archives(root,cfg):
   source=t.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if source is not None:break
 assert priority==42 and len(source)==1188446 and sha(source)==t.SOURCE
 v,=t.records(source,12,len(source));_,s,_=t.records(source,v[2]+12,v[3]);g=source[s[2]:s[3]];assert s[2]==210 and len(g)==139608 and sha(g)==t.GENERATOR
 meta=t.metadata(g);layer=meta['Layers'][3];assert (layer['ParentIndex'],layer['Depth'],layer['Section']['Offset'],[c['FormType'] for c in layer['Children']])==(0,1,13364,['BCIR','FFRA','AHFR'])
 assert layer['Section']['EncodedBytes']==422 and layer['Section']['Sha256']=='E0F7CC6190A9A25BCBE51D6FF4ED304D357667B37521DBF7DF5771EBD9EBD067'
 assert layer['Children'][2]['Offset']==13672 and layer['Children'][2]['Sha256']=='B85D90EC684322F07798476A55A665DCDE050D5B355D86CB3AD5ACB41883F674'
 assert layer['Children'][1]['Offset']==13554 and layer['Children'][1]['Sha256']=='038FA063AD5C2B8A10562A0040748B9C0CB247645418F38B5ACAE682409A1136'
 return g

def generate(exe,packages,root,cfg,out):
 assert sha(exe.read_bytes())==t.EXE;sys.path.insert(0,str(packages));static=dict(Circle=b.static(exe),Adta=ad.static(exe,packages),Height=ah.static(exe,str(packages)),Filter=ff.static(exe,packages));g=original(root,cfg);cases=[];base=generator()
 def add(name,encoded=None,**kw):cases.append(dict(Name=name,GeneratorHex=(encoded or base).hex(),**kw))
 add('owned-synthetic');add('original-layer3-evidence',g,Index=3,Policy=0,Original=True);add('original-layer3-operational-reject',g,Index=3)
 for idx in (-1,1,2147483647):add('bad-index-'+str(idx),Index=idx)
 add('wrong-binding',BindingHex=generator(adta=t.words([1,2,3])+b'x\0').hex());add('bad-policy',Policy=2);add('cancel',Cancel=True)
 for children in [filter()+circle()+height(),circle()+height()+filter(),circle()+filter(),circle()+filter()+height()+t.UNKNOWN,circle()+filter()+t.layer(children=height()),t.ahcn()+filter()+height()]:add('order-type-extra-'+str(len(cases)),generator(children))
 for version in (b'0001',b'0002',b'0004',b'0099'):add('layer-version-'+version.decode(),generator(version=version))
 for which in range(4):
  hdr=t.header(b'0099');parts=[circle(),filter(),height()]
  if which==0:add('opaque-layer-header',generator(h=hdr))
  else:parts[which-1]=[circle,filter,height][which-1](header=hdr);add('opaque-child-header-'+str(which),generator(b''.join(parts)))
 for kind,version,words in [(b'BCIR',b'0001',[0,0,0,0]),(b'FFRA',b'0004',[0]*6),(b'AHFR',b'0002',[0]*3)]:
  parts=[circle(),filter(),height()];parts[[b'BCIR',b'FFRA',b'AHFR'].index(kind)]=rule(kind,version,words);add('child-version-'+kind.decode(),generator(b''.join(parts)))
 for which in range(4):
  parts=[circle(),filter(),height()]
  if which==0:data=generator(h=t.header(enabled=0))
  else:parts[which-1]=[circle,filter,height][which-1](enabled=0);data=generator(b''.join(parts))
  add('disabled-operational-'+str(which),data);add('disabled-evidence-'+str(which),data,Policy=0)
 for at in (0,1,2,4):
  w=[0x3f800000,0x40000000,0xbf800000,0xffffffff,0x40000000];w[at]=0x7fc12345;data=generator(rule(b'BCIR',b'0002',w)+filter()+height());add('circle-finite-'+str(at),data);add('circle-evidence-'+str(at),data,Policy=0)
 for label,part in [('filter-nonfinite',filter(words=[0xfffffff9,0,0x7f800000,0,0x40000000,0x3f800000])),('height-operation',height(words=[0xfffffff9,4,0x3f800000])),('height-scale',height(words=[0xfffffff9,0,0x7fc12345]))]:
  parts=[circle(),filter(),height()];parts[1 if label.startswith('filter') else 2]=part;data=generator(b''.join(parts));add(label,data);add(label+'-evidence',data,Policy=0)
 for which in (1,2):
  parts=[circle(),filter(),height()];parts[which]=filter(words=[0xffffffff,0,0,0,0x40000000,0x3f800000]) if which==1 else height(words=[0xffffffff,1,0x41700000]);add('missing-family-'+str(which),generator(b''.join(parts)))
 changed=list(FAMILY);changed[12]=6;bad=generator(entries=[(-7,changed)]);add('family-ineligible',bad);add('family-ineligible-evidence',bad,Policy=0)
 add('two-signed-families',generator(circle()+filter()+height(words=[0xffffffff,1,0x41700000]),entries=[(-7,FAMILY),(-1,FAMILY)]))
 add('finite-unusual-filter',generator(circle()+filter(words=[0xfffffff9,99,0xbf800000,0x40000000,0,0])+height()))
 add('adta-nonzero-raw',generator(adta=t.words([0x80000000,0x7fc12345,0xffffffff])+b'raw\0'))
 nested=t.layer(h=t.header(enabled=0),children=t.layer(h=t.header(enabled=1),children=circle()+filter()+height()));add('explicit-child-disabled-parent',generator(last=t.form(b'LYRS',nested)),Index=1)
 add('malformed-circle',generator(circle(data=bytes(19))+filter()+height()))
 add('malformed-filter',generator(circle()+filter(data=bytes(23))+height()))
 add('malformed-height',generator(circle()+filter()+height(data=bytes(11))))
 add('hard-option-composition',MaximumLayerBytes=16777216,MaximumChildBytes=4194304,MaximumNameBytes=65536,MaximumDecodedCopyBytes=16777216,MaximumSnapshotBytes=134217728,MaximumWork=134217728)
 add('precopy-large-name',generator(circle(name=b's'*60000)+filter()+height()),MaximumSnapshotBytes=0)
 good=expected(cases[0])['Snapshot'];length=good['LayerBytes'];maxchild=max(x['EncodedBytes'] for x in good['Children']);exact=dict(MaximumLayerBytes=length,MaximumChildBytes=maxchild,MaximumNameBytes=length,MaximumDecodedCopyBytes=good['ReservedDecodedCopyBytes'],MaximumSnapshotBytes=good['ReservedSnapshotBytes'],MaximumWork=good['ReservedWork']);add('exact-reservations',**exact)
 for key,value in exact.items():add('one-less-'+key,**{**exact,key:value-1})
 for key,value in [('MaximumLayerBytes',16777217),('MaximumChildBytes',23),('MaximumNameBytes',65537),('MaximumDecodedCopyBytes',-1),('MaximumSnapshotBytes',134217729),('MaximumWork',-1)]:add('bad-cap-'+key,**{key:value})
 e=[expected(c) for c in cases];assert e[0]['Reject']==e[1]['Reject']=='None'
 out.joinpath('fixtures.json').write_text(json.dumps(cases),encoding='utf-8');out.joinpath('expected.json').write_text(json.dumps(e),encoding='utf-8');summary=dict(Cases=len(cases),Rejected=sum(x['Reject']!='None' for x in e),OriginalLayersInspected=1,OriginalLayerIndex=3,OriginalSamples=0,Static=static)
 out.joinpath('producer-receipt.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary))
def verify(out):
 n=json.loads(out.joinpath('native.json').read_text());e=json.loads(out.joinpath('expected.json').read_text());assert n['Cases']==e,'Slice raw field/identity/accounting comparison failed';assert n['OwnershipChecks']==15
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=15);out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Operation')
