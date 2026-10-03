"""Independent full creature packet additions and exact ignored/private original ACST preload."""
import argparse,contextlib,hashlib,json,struct,sys
from pathlib import Path
repo=Path(__file__).resolve().parents[3];sys.path.insert(0,str(repo/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('config',type=Path);p.add_argument('mixed',type=Path);p.add_argument('baseline',type=Path);p.add_argument('output',type=Path);p.add_argument('directory',type=Path);a=p.parse_args()
for target in (a.output,a.directory):assert target.resolve().is_relative_to((repo/'local-private').resolve()) and target.parent.is_dir()
assert hashlib.sha256(a.config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
 for priority,path in archives(a.root,a.config):
  data=Index(stack.enter_context(path.open('rb'))).lookup('customization/asset_customization_manager.iff')
  if data is not None:break
 else:raise AssertionError('Missing original ACST')
pin=hashlib.sha256(data).hexdigest().upper();assert (priority,len(data),pin)==(53,458340,'5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20')
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
def components(counter,commands):
 return P('HII',5,len(commands),counter)+b''.join(P('B',kind)+(P('i',key) if kind!=2 else b'') for kind,key in commands)
def wounds(counter,kind,values=(),index=0):
 step=1+len(values) if kind==3 else 1
 body=P('B',kind)+(P('H',len(values)) if kind==3 else P('H',index) if kind!=4 else b'')+b''.join(P('i',v) for v in values)
 return P('HII',17,step,counter)+body
v=json.loads(a.mixed.read_text());assert v['MixedScalars'] and len(v['Messages'])==10
baseline=bytes.fromhex(json.loads(a.baseline.read_text())['human_baseline'])
# The independently authored Round16 baseline has empty name/customization fields
# and no component keys. Read its exact count/counter at the documented boundary.
assert baseline[25:47]==P('fHIHIIH',1,0,0,0,0,1,0)
assert struct.unpack_from('<II',baseline,47)==(0,7)
extras={
 'wire_zero':[(components(10,[(1,11),(1,-3),(1,11)]),1),(wounds(12,3,[10,20]),1)],
 'wire_varied':[(components(11,[(2,0)]),1),(wounds(13,2,[30],1),1)],
 'wire_duplicate':[(wounds(14,4),1),(components(12,[(1,7)]),1)],
 'wire_empty':[(components(13,[(0,7)]),1),(wounds(16,3,[40]),1),(components(14,[(1,9)]),1)],
 'wire_count_zero':[(wounds(16,3,[40]),1),(components(14,[(1,9)]),1)],
 'wire_extended_zero':[(wounds(17,4),1),(components(15,[(2,0)]),1)]}
for key,extra in extras.items():
 wire=bytes.fromhex(v['Messages'][key]);count=struct.unpack_from('<H',wire,23)[0]
 fields=b''.join(part for part,n in extra[:1])+wire[25:]+b''.join(part for part,n in extra[1:])
 payload=P('H',count+sum(n for _,n in extra))+fields
 v['Messages'][key]=(wire[:19]+P('I',len(payload))+payload).hex().upper()
v['FullFields']=True;v['WoundsPolicy']='LegacyBounded';v['DirectorySha256']=pin;v['DirectoryBytes']=len(data)
v['BaselineComponentCounter']=7
v['ExpectedContainers']=[dict(Name=name,Components=keys,ComponentCounter=cc,Wounds=values,WoundsCounter=wc) for name,keys,cc,values,wc in [('zero',[-3,11],10,[10,20],12),('ordered_valid_then_invalid',[],11,[10,30],13),('duplicate_retains_prior_valid',[7],12,[],14),('empty_forms_retain',[9],14,[40],16),('extended_zero_restore',[],15,[],17)]]
a.directory.write_bytes(data);a.output.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
print('PASS: independent full union, legacy assignment/replay and original private ACST preload')
