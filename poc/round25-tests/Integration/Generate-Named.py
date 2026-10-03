"""Independent name-field wires and bounded original STF context for the main join."""
import argparse,contextlib,hashlib,json,struct,sys
from pathlib import Path
repo=Path(__file__).resolve().parents[3];sys.path.insert(0,str(repo/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
p=argparse.ArgumentParser(description=__doc__)
for name in ('root','config','extended','output','context'):p.add_argument(name,type=Path)
a=p.parse_args();assert hashlib.sha256(a.config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
for target in (a.output,a.context):assert target.resolve().is_relative_to((repo/'local-private').resolve()) and target.parent.is_dir()
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
pins={'string/en/obj_n.stf':(2527,'3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054'),
 'string/en/terminal_name.stf':(4610,'CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603')}
tables=[];chosen={}
with contextlib.ExitStack() as stack:
 indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.config)]
 for path,(size,pin) in pins.items():
  for priority,index in indices:
   raw=index.lookup(path)
   if raw is not None:break
  else:raise AssertionError(path)
  assert (priority,len(raw),hashlib.sha256(raw).hexdigest().upper())==(53,size,pin)
  magic,reserved,variant,nxt,count=struct.unpack_from('<HHBII',raw);assert magic==0xabcd and reserved==0 and variant==1
  offset=13;values={}
  for _ in range(count):
   identity,crc,length=struct.unpack_from('<III',raw,offset);offset+=12
   units=struct.unpack_from('<'+'H'*length,raw,offset);offset+=length*2;assert identity not in values;values[identity]=units
  records=[]
  for _ in range(count):
   identity,length=struct.unpack_from('<II',raw,offset);offset+=8;key=raw[offset:offset+length];offset+=length
   records.append((identity,key,values[identity]))
  assert offset==len(raw)
  candidates=[record for record in records if record[1] and all(32<=c<=126 and c!=92 for c in record[2]) and len(record[2])<=128]
  identity,key,units=min(candidates,key=lambda record:record[0]);text=''.join(map(chr,units))
  chosen[path]=(identity,key,text)
  tables.append(dict(Path=path,Priority=priority,Bytes=size,Sha256=pin,Records=count,Hex=raw.hex().upper()))
context=dict(Version=1,PrimaryDirectory='string/native_missing',FallbackDirectory='string/en',LiveLocaleConfiguration=False,Tables=tables)
def string(value,extended=False):return (P('HI',65535,len(value)) if extended else P('H',len(value)))+value
def string_id(file,key,spacer=0,file_extended=False,key_extended=False):
 return P('H',1)+string(file,file_extended)+P('I',spacer)+string(key,key_extended)
def custom(text):
 raw=text.encode('utf-16-le');return P('HI',2,len(raw)//2)+raw
obj_id,obj_key,obj_text=chosen['string/en/obj_n.stf'];terminal_id,terminal_key,terminal_text=chosen['string/en/terminal_name.stf']
updates={
 'wire_zero':[string_id(b'obj_n',obj_key,0xa5a5a5a5,key_extended=True),custom('')],
 'wire_varied':[custom('Native actor')],
 'wire_duplicate':[string_id(b'obj_n',b'__missing_native_key__',file_extended=True),custom('')],
 'wire_empty':[string_id(b'terminal_name',terminal_key,0x12345678)],
 'wire_count_zero':[custom('')],
 'wire_extended_zero':[custom(''),string_id(b'obj_n',obj_key,file_extended=True,key_extended=True)]}
fixture=json.loads(a.extended.read_text());assert fixture['ExtendedFields']
for key,fields in updates.items():
 wire=bytes.fromhex(fixture['Messages'][key]);declared=struct.unpack_from('<H',wire,23)[0]
 payload=P('H',(declared+len(fields))&65535)+b''.join(fields[:1])+wire[25:]+b''.join(fields[1:])
 fixture['Messages'][key]=(wire[:19]+P('I',len(payload))+payload).hex().upper()
fixture['NamedFields']=True
def expected(name,file,key,spacer,custom_text,file_extended,key_extended,text,origin,matched):
 return dict(Name=name,NameFileHex=file.hex().upper(),NameKeyHex=key.hex().upper(),NameSpacer=spacer,
  NameCustomUnits=list(struct.unpack('<'+'H'*(len(custom_text.encode('utf-16-le'))//2),custom_text.encode('utf-16-le'))),
  NameFileExtended=file_extended,NameKeyExtended=key_extended,NameDisplay=text,NameOrigin=origin,NameMatchedId=matched,
  NameAdditionalBytes=len(file)+len(key)+len(custom_text.encode('utf-16-le')))
fixture['ExpectedNames']=[
 expected('zero',b'obj_n',obj_key,0xa5a5a5a5,'',False,True,obj_text,'FallbackTable',obj_id),
 expected('ordered_valid_then_invalid',b'obj_n',obj_key,0xa5a5a5a5,'Native actor',False,True,'Native actor','Custom',None),
 expected('duplicate_retains_prior_valid',b'obj_n',b'__missing_native_key__',0,'',True,False,'(missing key)','MissingKey',None),
 expected('empty_forms_retain',b'terminal_name',terminal_key,0x12345678,'',False,False,terminal_text,'FallbackTable',terminal_id),
 expected('extended_zero_restore',b'obj_n',obj_key,0,'',True,True,obj_text,'FallbackTable',obj_id)]
a.context.write_text(json.dumps(context,indent=2)+'\n',encoding='utf-8');a.output.write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
print('PASS: independent raw names, custom precedence, missing key, original fallback and private two-table context')
