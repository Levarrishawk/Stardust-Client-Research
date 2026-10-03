"""Independent synthetic name-policy vectors and two targeted STF byte/display oracles."""
import contextlib,hashlib,json,struct,sys,unicodedata
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
PINS={'string/en/obj_n.stf':(2527,'3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054'),'string/en/terminal_name.stf':(4610,'CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603')}
ROWS=[(1,b'key',[65,0,92,0xD800,0xDC00,0xDC00,10,0xFFFF]),(2,b'Key',[66]),(3,b'key\0\x80\xff',[67]),(4,b'',[68]),(5,b'empty',[])]
def stf():
 r=struct.pack('<HHBII',0xABCD,0,1,6,len(ROWS))
 for ident,key,u in ROWS:r+=struct.pack('<III',ident,0,len(u))+struct.pack('<'+'H'*len(u),*u)
 for ident,key,u in ROWS:r+=struct.pack('<II',ident,len(key))+key
 return r.hex().upper()
def display(units,maximum):
 out='';used=0;length=0
 while used<len(units) and length<maximum:
  u=units[used];consume=1
  if 0xD800<=u<=0xDBFF and used+1<len(units) and 0xDC00<=units[used+1]<=0xDFFF:
   token=chr(0x10000+((u-0xD800)<<10)+(units[used+1]-0xDC00));consume=2;size=2
  else:
   token='\uFFFD' if 0xD800<=u<=0xDFFF else '\\\\' if u==92 else '\\u%04X'%u if unicodedata.category(chr(u))=='Cc' else chr(u);size=len(token)
  if size>maximum-length:break
  out+=token;length+=size;used+=consume
 return out,used<len(units)
def valid_component(raw):return len(raw)>0 and all(b in b'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-' for b in raw)
def directory(d):return isinstance(d,str) and 1<=len(d)<=256 and all(valid_component(p.encode('utf-8')) for p in d.split('/'))
def expected(f):
 file=bytes.fromhex(f['FileHex']);key=bytes.fromhex(f['KeyHex']);u=f['Units'];maximum=f['Maximum'];calls=[];path=None;ident=None
 if not 0<=maximum<=4096 or not directory(f['Primary']) or f['Fallback'] is not None and not directory(f['Fallback']):return dict(Rejected=True,Calls=[])
 if u:origin='Custom';text,truncated=display(u,maximum)
 else:
  if not file and not key:origin='Empty';text=''
  elif not 1<=len(file)<=128 or len(key)>65536 or not valid_component(file):origin='InvalidStringId';text='(invalid name)'
  else:
   path=f['Primary']+'/'+file.decode('ascii')+'.stf';calls.append(path);present=f['Mode']=='primary';origin='PrimaryTable'
   if not present and f['Fallback'] is not None and f['Fallback']!=f['Primary']:
    path=f['Fallback']+'/'+file.decode('ascii')+'.stf';calls.append(path);present=f['Mode']=='fallback';origin='FallbackTable'
   if not present:origin='MissingTable';text='(missing table)'
   else:
    row=next((r for r in ROWS if r[1]==key),None)
    if row is None:origin='MissingKey';text='(missing key)'
    else:ident=row[0];text,truncated=display(row[2],maximum)
  if ident is None:truncated=len(text)>maximum;text=text[:maximum]
 return dict(Rejected=False,Calls=calls,Result=dict(DisplayText=text,Origin=origin,Truncated=truncated,TablePath=path,MatchedId=ident))
def fixtures():
 result=[]
 def add(name,file=b'obj_n',key=b'key',units=[],maximum=256,primary='string/en',fallback=None,mode='primary'):
  result.append(dict(Name=name,FileHex=file.hex().upper(),KeyHex=key.hex().upper(),Units=units,Maximum=maximum,Primary=primary,Fallback=fallback,Mode=mode))
 add('custom-precedence',file=b'../bad',units=[65,0,92,0xD800,66,0xDC00,0xD83D,0xDE00,10]);add('custom-nul-nonempty',units=[0]);add('empty',file=b'',key=b'');add('partial-empty',file=b'',key=b'key');add('empty-key',key=b'');add('empty-localized',key=b'empty')
 for key in [b'Key',b'key\0\x80\xff',b'key\x80',b'KEY']:add('key-'+key.hex(),key=key)
 add('fallback',fallback='string/fr',mode='fallback');add('no-fallback',mode='none');add('both-missing',fallback='string/fr',mode='none');add('same-directory-once',fallback='string/en',mode='none');add('key-missing-no-fallback',key=b'missing',fallback='string/fr');add('primary-wins',fallback='string/fr')
 for file in [b'..',b'../obj',b'/obj',b'a.b',b'C:obj',b'a\\b',b'a\0b',b'a\x80',b' obj',b'a/b',b'a'*129]:add('unsafe-'+file.hex(),file=file)
 add('identifier-limit',file=b'A'*128);add('key-limit',key=b'K'*65536);add('key-over-limit',key=b'K'*65537)
 for maximum in range(0,22):add('custom-bound-'+str(maximum),units=[65,0,92,0xD800,0xDC00,0xDC00,10],maximum=maximum)
 for maximum in range(0,22):add('localized-bound-'+str(maximum),maximum=maximum)
 add('maximum-display',units=[65]*4097,maximum=4096);add('status-bound',key=b'missing',maximum=4)
 for primary in ['', '/string/en','string//en','string/en/','string/../en','C:/string','string\\en','string/é','a'*257]:add('bad-directory-'+primary,primary=primary)
 add('bad-fallback',fallback='../en');add('negative-budget',maximum=-1);add('over-budget',maximum=4097);add('directory-limit',primary='A'*256)
 return result
if sys.argv[1]=='generate':
 Path(sys.argv[2]).write_text(json.dumps(dict(TableHex=stf(),Cases=fixtures()),indent=2),encoding='utf-8');print('Name fixtures:',len(fixtures()));sys.exit()
root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');receipts={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]}
syn=receipts['name_synthetic'];assert len(syn['Cases'])==len(fixtures())
for f,a in zip(fixtures(),syn['Cases']):assert a==dict(Name=f['Name'],**expected(f)),f['Name']
assert syn['ControlledChecks']==14 and syn['ConcurrentCalls']==32 and syn['RawPreserved']
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
 indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
 original=receipts['name_original'];assert len(original['Lookups'])==2 and original['ArchivesClosed']
 for actual in original['Lookups']:
  path=actual['Path'];assert path in PINS
  for priority,index in indices:
   raw=index.lookup(path)
   if raw is not None:break
  else:raise AssertionError(path)
  size,pin=PINS[path];assert (priority,len(raw),hashlib.sha256(raw).hexdigest().upper())==(53,size,pin)
  magic,reserved,variant,next_id,count=struct.unpack_from('<HHBII',raw);assert magic==0xABCD and variant==1;cursor=13;values=[];keys={}
  for _ in range(count):
   ident,crc,n=struct.unpack_from('<III',raw,cursor);cursor+=12;units=list(struct.unpack_from('<'+'H'*n,raw,cursor));cursor+=n*2;values.append((ident,units))
  for _ in range(count):
   ident,n=struct.unpack_from('<II',raw,cursor);cursor+=8;keys[ident]=raw[cursor:cursor+n];cursor+=n
  assert cursor==len(raw);ident,units=values[0];text,trunc=display(units,256)
  assert actual==dict(Path=path,Priority=priority,Bytes=size,Sha256=pin,KeyHex=keys[ident].hex().upper(),Units=units,Calls=[path],Result=dict(DisplayText=text,Origin='PrimaryTable',Truncated=trunc,TablePath=path,MatchedId=ident))
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),controlled_checks=14,concurrent_calls=32,original_lookups=2,variant0_supported=False,cache=False),indent=2))
