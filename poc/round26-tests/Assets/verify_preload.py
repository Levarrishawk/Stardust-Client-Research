"""Independent preload vectors, byte-level table oracle and two pinned original inputs."""
import contextlib,hashlib,json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
PINS={'string/en/obj_n.stf':(2527,'3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054'),'string/en/terminal_name.stf':(4610,'CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603')}
A='string/en/a.stf';B='string/en/b.stf';M='string/en/missing.stf'
def encode(rows):
 raw=struct.pack('<HHBII',0xABCD,0x1234,1,0xFFEEDDCC,len(rows))
 for ident,crc,key,u in rows:raw+=struct.pack('<III',ident,crc,len(u))+struct.pack('<'+'H'*len(u),*u)
 for ident,crc,key,u in reversed(rows):raw+=struct.pack('<II',ident,len(key))+key
 return raw
RA=encode([(0,0xFFFFFFFF,b'A\x80\0',[65,0xD800,0]),(0xFFFFFFFF,0,b'',[])])
RB=encode([(7,9,b'b\xff',[0xD83D,0xDE00,92,10])])
def decode(raw):
 magic,reserved,variant,next_id,count=struct.unpack_from('<HHBII',raw);assert magic==0xABCD and variant==1;cursor=13;values=[];keys={};order=[]
 for _ in range(count):
  ident,crc,n=struct.unpack_from('<III',raw,cursor);cursor+=12;u=list(struct.unpack_from('<'+'H'*n,raw,cursor));cursor+=n*2;values.append((ident,crc,u))
 for _ in range(count):
  ident,n=struct.unpack_from('<II',raw,cursor);cursor+=8;keys[ident]=raw[cursor:cursor+n];cursor+=n;order.append(ident)
 assert cursor==len(raw)
 return dict(Header=dict(Magic=magic,ReservedRaw=reserved,Variant=variant,MaxNextIdentityRaw=next_id,RecordCount=count),KeyPassIds=order,TotalValueUnits=sum(len(u) for i,c,u in values),TotalKeyBytes=sum(map(len,keys.values())),Records=[dict(Id=i,ValueCrcRaw=c,KeyHex=keys[i].hex().upper(),Units=u) for i,c,u in values])
def limits():return dict(MaximumPaths=3,MaximumSourceBytes=len(RA)+len(RB),MaximumRecords=3,MaximumValueUnits=7,MaximumKeyBytes=5,TableLimits=dict(MaximumBytes=max(len(RA),len(RB)),MaximumRecords=2,MaximumValueUnits=4,MaximumKeyBytes=3,MaximumTotalValueUnits=4,MaximumTotalKeyBytes=3))
def fixtures():
 result=[]
 def add(name,paths=None,resources=None,budget=None,reject='None',calls=None):
  paths=[A,B,M] if paths is None else paths;resources={A:RA,B:RB} if resources is None else resources
  result.append(dict(Name=name,Paths=paths,Resources=[dict(Path=p,Hex=None if raw is None else raw.hex().upper()) for p,raw in resources.items()],Limits=limits() if budget is None else budget,Reject=reject,ExpectedCalls=paths if calls is None else calls))
 add('exact-boundaries');add('reverse-order',paths=[M,B,A]);add('case-distinct',paths=[A,'string/en/A.stf'],resources={A:RA,'string/en/A.stf':RB})
 l=limits();l.update(MaximumPaths=0,MaximumSourceBytes=0,MaximumRecords=0,MaximumValueUnits=0,MaximumKeyBytes=0);add('empty-zero-budgets',paths=[],budget=l)
 l=limits();l.update(MaximumPaths=1,MaximumSourceBytes=0,MaximumRecords=0,MaximumValueUnits=0,MaximumKeyBytes=0);add('absence-zero-budgets',paths=[M],budget=l)
 add('present-empty-table',paths=[A],resources={A:encode([])})
 longpath='string/'+'a'*249+'/'+('B'*128)+'.stf';add('path-boundaries',paths=[longpath],resources={longpath:RA})
 add('duplicate',paths=[A,A],reject='Input',calls=[]);add('duplicate-late',paths=[A,B,A],reject='Input',calls=[])
 for path in [None,'','/string/en/a.stf','String/en/a.stf','string/a.stf','string//a.stf','string/en/../a.stf','string/en/a.STF','string/en/.stf','string/en/a.b.stf','string/en/a\\b.stf','string/en/a:bad.stf','string/en/a\0.stf','string/en/é.stf','string/en/a.stf/','string/en/'+('a'*129)+'.stf','string/'+('a'*250)+'/a.stf']:
  add('unsafe-'+str(path),paths=[A,path],reject='Input',calls=[])
 for field,n,attempts in [('MaximumPaths',2,[]),('MaximumSourceBytes',len(RA)+len(RB)-1,[A,B]),('MaximumRecords',2,[A,B]),('MaximumValueUnits',6,[A,B]),('MaximumKeyBytes',4,[A,B])]:
  l=limits();l[field]=n;add('aggregate-one-less-'+field,budget=l,reject='Input',calls=attempts)
 for field,n,attempts in [('MaximumBytes',len(RA)-1,[A]),('MaximumRecords',1,[A]),('MaximumValueUnits',3,[A,B]),('MaximumKeyBytes',2,[A]),('MaximumTotalValueUnits',3,[A,B]),('MaximumTotalKeyBytes',2,[A])]:
  l=limits();l['TableLimits'][field]=n;add('individual-one-less-'+field,budget=l,reject='Input',calls=attempts)
 for field,value in [('MaximumPaths',-1),('MaximumPaths',1025),('MaximumSourceBytes',268435457),('MaximumRecords',1000001),('MaximumValueUnits',33554433),('MaximumKeyBytes',-1)]:
  l=limits();l[field]=value;add('bad-option-'+field+str(value),budget=l,reject='Options',calls=[])
 l=limits();l['TableLimits']['MaximumBytes']=12;add('bad-parser-even-empty',paths=[],budget=l,reject='Options',calls=[])
 for n in range(len(RA)):add('truncate-'+str(n),paths=[A,B],resources={A:RA[:n],B:RB},reject='Input',calls=[A])
 for name,raw in [('variant0',RB[:4]+b'\0'+RB[5:]),('trailing',RB+b'\0'),('magic',b'\0\0'+RB[2:]),('orphan',RB[:-len(b'b\xff')-8]+struct.pack('<II',9,2)+b'b\xff')]:
  add('bad-later-'+name,paths=[A,B],resources={A:RA,B:raw},reject='Input',calls=[A,B])
 return result
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Preload fixtures:',len(fixtures()));sys.exit()
root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]}
synthetic=r['set_synthetic'];assert len(synthetic['Cases'])==len(fixtures())
for f,a in zip(fixtures(),synthetic['Cases']):
 assert a['Name']==f['Name'] and a['Reject']==f['Reject'] and a['Calls']==f['ExpectedCalls'],f['Name']
 if f['Reject']=='None':
  resources={e['Path']:None if e['Hex'] is None else bytes.fromhex(e['Hex']) for e in f['Resources']};tables={p:decode(resources[p]) for p in f['Paths'] if resources.get(p) is not None};missing=[p for p in f['Paths'] if resources.get(p) is None]
  expected=dict(RequestedPaths=f['Paths'],MissingPaths=missing,SourceBytes=sum(len(resources[p]) for p in tables),TotalRecords=sum(t['Header']['RecordCount'] for t in tables.values()),TotalValueUnits=sum(t['TotalValueUnits'] for t in tables.values()),TotalKeyBytes=sum(t['TotalKeyBytes'] for t in tables.values()),Tables=tables)
  assert a['Set']==expected,f['Name']
assert synthetic['ControlledChecks']==21 and synthetic['ConcurrentBuilds']==32 and synthetic['Owned']
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
 indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
 original=r['set_original'];assert original['ArchivesClosed'] and original['Calls']==list(PINS);expectedTables={};source=0
 for info in original['Inputs']:
  path=info['Path'];assert path in PINS
  for priority,index in indices:
   raw=index.lookup(path)
   if raw is not None:break
  else:raise AssertionError(path)
  size,pin=PINS[path];assert (priority,len(raw),hashlib.sha256(raw).hexdigest().upper())==(53,size,pin)
  assert info==dict(Path=path,Priority=priority,Bytes=size,Sha256=pin);expectedTables[path]=decode(raw);source+=size
 expected=dict(RequestedPaths=list(PINS),MissingPaths=[],SourceBytes=source,TotalRecords=97,TotalValueUnits=1724,TotalKeyBytes=1723,Tables=expectedTables)
 assert original['Set']==expected
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),rejected=sum(f['Reject']!='None' for f in fixtures()),controlled_checks=21,concurrent_builds=32,original_tables=2,original_records=97,source_bytes=source,value_units=1724,key_bytes=1723,variant0_supported=False,locale_cache=False),indent=2))
