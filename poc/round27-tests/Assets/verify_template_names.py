"""Bounded shared-template localized-name fixtures and independent original-chain oracle."""
import contextlib,hashlib,json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
ROOTS={'object/creature/player/shared_human_male.iff':'E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243','object/tangible/terminal/shared_terminal_mission.iff':'E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281'}
def pair(raw,maximum=1024):
 if raw==b'\0':return None
 assert raw[:1]==b'\1';cursor=1;parts=[]
 for _ in range(2):
  assert raw[cursor:cursor+1]==b'\1';cursor+=1;end=raw.index(0,cursor);assert end-cursor<=maximum;parts.append(raw[cursor:end]);cursor=end+1
 assert cursor==len(raw);return tuple(parts)
def stf(raw):
 magic,reserved,variant,next_id,count=struct.unpack_from('<HHBII',raw);assert magic==0xABCD and variant==1 and count<=65536;cursor=13;values={};order=[]
 for _ in range(count):
  ident,crc,n=struct.unpack_from('<III',raw,cursor);cursor+=12;assert n<=65536;units=list(struct.unpack_from('<'+'H'*n,raw,cursor));cursor+=n*2;assert ident not in values;values[ident]=units
 for _ in range(count):
  ident,n=struct.unpack_from('<II',raw,cursor);cursor+=8;assert n<=4096 and ident in values;key=raw[cursor:cursor+n];cursor+=n;order.append((ident,key))
 assert cursor==len(raw);return order

def original(root,config):
 assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
 sources={};profiles=[];tables={}
 with contextlib.ExitStack() as stack:
  indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
  def lookup(path):
   for priority,index in indices:
    raw=index.lookup(path)
    if raw is not None:return priority,raw
   raise AssertionError(path)
  for rootpath,pin in ROOTS.items():
   path=rootpath;chain=[];nearest={};rows=[]
   while path:
    assert path not in chain and len(chain)<16
    if path not in sources:
     priority,raw=lookup(path);assert len(raw)<=1048576 and sum(s['Bytes'] for s in sources.values())+len(raw)<=1048576
     sources[path]=dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper(),Raw=raw)
    source=sources[path];raw=source['Raw'];assert path!=rootpath or source['Sha256']==pin
    node,=nodes(raw);parent,layers=parse(node);version,fields=layers['SHOT'];assert version in ('0007','0008','0009','0010');chain.append(path)
    for field in ('objectName','detailedDescription'):
     encoded=fields.get(field);value=None if encoded is None else pair(encoded)
     rows.append(dict(Path=path,Version=version,Field=field,Hex=None if encoded is None else encoded.hex().upper(),Kind='Absent' if encoded is None else 'Inherited' if value is None else 'Explicit'))
     if value is not None and field not in nearest:nearest[field]=dict(TableHex=value[0].hex().upper(),KeyHex=value[1].hex().upper(),SourcePath=path,Family='SHOT',Version=version)
    path=parent
   name=nearest['objectName'];tablename=bytes.fromhex(name['TableHex']);key=bytes.fromhex(name['KeyHex']);tablepath='string/en/'+tablename.decode('ascii')+'.stf'
   assert tablepath in ('string/en/species.stf','string/en/terminal_name.stf')
   if tablepath not in tables:
    priority,raw=lookup(tablepath);assert len(raw)<=1048576;records=stf(raw);tables[tablepath]=dict(Path=tablepath,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper(),Records=len(records),IdsAndKeys=records)
   matches=[ident for ident,k in tables[tablepath]['IdsAndKeys'] if k==key];assert len(matches)==1
   profiles.append(dict(RootPath=rootpath,Chain=chain,Rows=rows,Nearest=nearest,StfPath=tablepath,MatchedId=matches[0]))
 return dict(Sources=[{k:v for k,v in s.items() if k!='Raw'} for s in sources.values()],Profiles=profiles,Tables=[{k:v for k,v in t.items() if k!='IdsAndKeys'} for t in tables.values()])
if sys.argv[1]=='probe':
 result=original(Path(sys.argv[2]),Path(sys.argv[3]));Path(sys.argv[4]).write_text(json.dumps(result,indent=2),encoding='utf-8')
 print(json.dumps(dict(unique_sources=len(result['Sources']),source_bytes=sum(s['Bytes'] for s in result['Sources']),chains=[dict(root=p['RootPath'],length=len(p['Chain']),name_table_bytes=len(bytes.fromhex(p['Nearest']['objectName']['TableHex'])),name_key_bytes=len(bytes.fromhex(p['Nearest']['objectName']['KeyHex'])),description_table_bytes=len(bytes.fromhex(p['Nearest']['detailedDescription']['TableHex'])),description_key_bytes=len(bytes.fromhex(p['Nearest']['detailedDescription']['KeyHex'])),name_key_found=True) for p in result['Profiles']],stf_tables=result['Tables']),indent=2));sys.exit()

def chunk(tag,payload):return tag+struct.pack('>I',len(payload))+payload
def form(tag,payload):return chunk(b'FORM',tag+payload)
def encoded(table=b'tab',key=b'value'):return b'\1\1'+table+b'\0\1'+key+b'\0'
def template(name=None,description=None,parent=None,version=b'0007',family=b'SHOT',extra=None,count_delta=0):
 fields=[]
 if name is not None:fields.append((b'objectName',name))
 if description is not None:fields.append((b'detailedDescription',description))
 if extra:fields+=extra
 version_node=form(version,chunk(b'PCNT',struct.pack('<I',len(fields)+count_delta))+b''.join(chunk(b'XXXX',n+b'\0'+raw) for n,raw in fields))
 deriv=b'' if parent is None else form(b'DERV',chunk(b'XXXX',parent.encode('ascii')+b'\0'))
 shot=form(b'SHOT',deriv+version_node)
 if family==b'SHOT':return shot
 stot=form(b'STOT',deriv+form(b'0009',chunk(b'PCNT',struct.pack('<I',0)))+shot)
 return stot if family==b'STOT' else form(b'SCOT',deriv+form(b'0012',chunk(b'PCNT',struct.pack('<I',0)))+stot)
A='object/a.iff';B='object/b.iff';C='object/c.iff'
def limits():return dict(MaximumDepth=16,MaximumTemplateBytes=1048576,MaximumSourceBytes=1048576,MaximumStringBytes=1024)
def fixtures():
 result=[]
 def add(name,resources,root=A,budget=None,follow=True,reject='None',calls=None,pin=None):
  first=resources.get(root);first=first[1] if isinstance(first,tuple) else first
  digest=hashlib.sha256(first).hexdigest().upper() if first is not None else '0'*64
  rows=[dict(Path=p,Kind=v[0] if isinstance(v,tuple) else 'Missing' if v is None else 'Present',Hex=None if v is None else (v[1] if isinstance(v,tuple) else v).hex().upper()) for p,v in resources.items()]
  result.append(dict(Name=name,RootPath=root,RootSha256=digest if pin is None else pin,Resources=rows,Limits=limits() if budget is None else budget,FollowParents=follow,Reject=reject,ExpectedCalls=calls if calls is not None else [root]))
 complete=template(encoded(),encoded(b'desc',b'info'))
 inherited=template(b'\0',encoded(b'desc',b'info'),B);base=template(encoded(),b'\0')
 add('root-complete-stop',{A:template(encoded(),encoded(b'desc',b'info'),B),B:b'bad'})
 add('inherited-name',{A:inherited,B:base},calls=[A,B])
 add('nearest-description',{A:template(encoded(),b'\0',B),B:template(encoded(b'far',b'old'),encoded(b'desc',b'info'))},calls=[A,B])
 add('explicit-empty-stops',{A:template(encoded(b'',b''),encoded(b'',b''),A)})
 add('empty-name-retained-through-parent',{A:template(encoded(b'',b''),b'\0',B),B:template(encoded(),encoded(b'desc',b'info'))},calls=[A,B])
 add('absent-end',{A:template()});add('inherited-end',{A:template(b'\0',b'\0')})
 add('parent-disabled',{A:inherited,B:base},follow=False)
 add('raw-narrow-high-space',{A:template(encoded(b'\x80\xff',b'A \\'),encoded(b'',b''))})
 add('empty-key-explicit',{A:template(encoded(b'tab',b''),encoded(b'',b''))})
 for family in (b'STOT',b'SCOT'):add('nested-'+family.decode(),{A:template(encoded(),encoded(b'',b''),family=family)})
 for version in (b'0007',b'0008',b'0009',b'0010'):add('version-'+version.decode(),{A:template(encoded(),encoded(b'',b''),version=version)})
 exact=limits();exact.update(MaximumDepth=2,MaximumTemplateBytes=max(len(inherited),len(base)),MaximumSourceBytes=len(inherited)+len(base),MaximumStringBytes=5)
 add('exact-limits',{A:inherited,B:base},budget=exact,calls=[A,B])
 empty=limits();empty['MaximumStringBytes']=0;add('zero-string-limit-empty',{A:template(encoded(b'',b''),encoded(b'',b''))},budget=empty)
 for field,value,calls in [('MaximumDepth',1,[A]),('MaximumTemplateBytes',len(inherited)-1,[A]),('MaximumSourceBytes',len(inherited)+len(base)-1,[A,B]),('MaximumStringBytes',4,[A,B])]:
  budget=dict(exact);budget[field]=value;add('one-less-'+field,{A:inherited,B:base},budget=budget,reject='Input',calls=calls)
 add('missing-root',{A:None},reject='Input');add('masked-root',{A:('Masked',b'')},reject='Input')
 add('missing-parent',{A:inherited,B:None},reject='Input',calls=[A,B]);add('masked-parent',{A:inherited,B:('Masked',b'')},reject='Input',calls=[A,B])
 add('self-cycle-unresolved',{A:template(b'\0',b'\0',A)},reject='Input')
 add('two-node-cycle',{A:template(b'\0',b'\0',B),B:template(b'\0',b'\0',A)},reject='Input',calls=[A,B])
 add('wrong-root-pin',{A:complete},pin='0'*64,reject='Input')
 for root in ('../a.iff','/object/a.iff','object/../a.iff','object/a\\b.iff','object/A.iff','object/a:bad.iff','object/a b.iff','object/.iff','object/a.iff/','object/'+('a'*1014)+'.iff'):
  add('unsafe-root-'+root,{root:complete},root=root,reject='Input',calls=[])
 for parent in ('../b.iff','/object/b.iff','object/../b.iff','object/b\\c.iff','object/B.iff','object/b:bad.iff'):
  add('unsafe-parent-'+parent,{A:template(encoded(),encoded(b'',b''),parent)},reject='Input')
 badparams=[b'',b'\0\0',b'\2',b'\1',b'\1\0',b'\1\2x\0\1y\0',b'\1\1x\0',b'\1\1x\0\0y\0',b'\1\1x\0\2y\0',b'\1\1x\0\1y',encoded()+b'\0']
 for i,raw in enumerate(badparams):add('bad-param-'+str(i),{A:template(raw,encoded(b'',b''))},reject='Input')
 add('shadowed-malformed-visited',{A:template(encoded(),b'\0',B),B:template(b'\2',encoded(b'desc',b'info'))},reject='Input',calls=[A,B])
 add('unsupported-version',{A:template(encoded(),encoded(),version=b'0006')},reject='Input')
 add('duplicate-field',{A:template(encoded(),encoded(),extra=[(b'objectName',encoded())])},reject='Input')
 add('count-mismatch',{A:template(encoded(),encoded(),count_delta=1)},reject='Input')
 for n in range(len(complete)):add('truncate-'+str(n),{A:complete[:n]},reject='Input')
 add('iff-trailing',{A:complete+b'\0'},reject='Input')
 for field,value in [('MaximumDepth',0),('MaximumDepth',17),('MaximumTemplateBytes',1048577),('MaximumSourceBytes',16777217),('MaximumStringBytes',65537),('MaximumStringBytes',-1)]:
  budget=limits();budget[field]=value;add('bad-option-'+field+str(value),{A:complete},budget=budget,reject='Options',calls=[])
 return result

def synthetic_profile(f):
 resources={r['Path']:r for r in f['Resources']};path=f['RootPath'];visits=[];sources={};nearest={};stop='EndOfChain'
 while path:
  raw=bytes.fromhex(resources[path]['Hex']);node,=nodes(raw);parent,layers=parse(node);version,fields=layers['SHOT'];kinds={}
  sources[path]=dict(Path=path,Priority=77,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper())
  for field in ('objectName','detailedDescription'):
   encoded_raw=fields.get(field);value=None if encoded_raw is None else pair(encoded_raw,f['Limits']['MaximumStringBytes']);kinds[field]='Absent' if encoded_raw is None else 'Inherited' if value is None else 'Explicit'
   if value is not None and field not in nearest:nearest[field]=dict(TableHex=value[0].hex().upper(),KeyHex=value[1].hex().upper(),IsEmptyPair=not value[0] and not value[1],Field=dict(SourcePath=path,Family='SHOT',Version=version))
  visits.append(dict(SourcePath=path,Version=version,ParentPath=parent,ObjectNameKind=kinds['objectName'],DetailedDescriptionKind=kinds['detailedDescription']))
  if len(nearest)==2:stop='Resolved';break
  if not f['FollowParents'] and parent is not None:stop='ParentTraversalDisabled';break
  path=parent
 return dict(RootPath=f['RootPath'],ObjectName=nearest.get('objectName'),DetailedDescription=nearest.get('detailedDescription'),Complete=len(nearest)==2,Visits=visits,Sources=sources,SourceBytes=sum(s['Bytes'] for s in sources.values()),Stop=stop)
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Template name fixtures:',len(fixtures()));sys.exit()
if sys.argv[1]=='verify':
 root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]}
 synthetic=r['template_name_synthetic'];assert len(synthetic['Cases'])==len(fixtures())
 for f,a in zip(fixtures(),synthetic['Cases']):
  assert a['Name']==f['Name'] and a['Reject']==f['Reject'] and a['Calls']==f['ExpectedCalls'],f['Name']
  if f['Reject']=='None':assert a['Profile']==synthetic_profile(f),f['Name']
 assert synthetic['ControlledChecks']==12 and synthetic['ConcurrentLoads']==32 and synthetic['Owned']
 evidence=original(root,config);actual=r['template_name_original'];assert len(actual['Profiles'])==2 and actual['ArchivesClosed']
 for p,oracle in zip(actual['Profiles'],evidence['Profiles']):
  assert p['Calls']==[oracle['RootPath']];expected=oracle['Nearest'];source=next(s for s in evidence['Sources'] if s['Path']==oracle['RootPath']);visits=[]
  fields={}
  for field in ('objectName','detailedDescription'):
   value=expected[field];fields[field]=dict(TableHex=value['TableHex'],KeyHex=value['KeyHex'],IsEmptyPair=not value['TableHex'] and not value['KeyHex'],Field={k:value[k] for k in ('SourcePath','Family','Version')})
  version=fields['objectName']['Field']['Version'];parent=oracle['Chain'][1]
  expectedProfile=dict(RootPath=oracle['RootPath'],ObjectName=fields['objectName'],DetailedDescription=fields['detailedDescription'],Complete=True,Visits=[dict(SourcePath=oracle['RootPath'],Version=version,ParentPath=parent,ObjectNameKind='Explicit',DetailedDescriptionKind='Explicit')],Sources={oracle['RootPath']:source},SourceBytes=source['Bytes'],Stop='Resolved')
  assert p['Profile']==expectedProfile
 assert actual['NameKeyExistence']==[dict(Path=p['StfPath'],MatchedId=p['MatchedId'],Found=True) for p in evidence['Profiles']]
 assert actual['StfInputs']==evidence['Tables']
 print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),rejected=sum(f['Reject']!='None' for f in fixtures()),controlled_checks=12,concurrent_loads=32,original_roots=2,minimal_profile_sources=2,full_chain_unique_sources=len(evidence['Sources']),full_chain_source_bytes=sum(s['Bytes'] for s in evidence['Sources']),chain_lengths=[len(p['Chain']) for p in evidence['Profiles']],stf_key_matches=2,original_getter_semantics='unverified'),indent=2))
