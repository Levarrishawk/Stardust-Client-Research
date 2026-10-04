import argparse,importlib.util,json,struct,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
lead=load('family_route_lead50','poc/round50-tests/Integration/family_sampler_join_peer.py');mode=load('family_route_mode50','poc/round50-tests/Rendering/verify_modes.py');n=mode.n;b=mode.b
sha=lambda v:hashlib.sha256(v).hexdigest().upper()
def group(id,w):return lead.form(b'MGRP',lead.form(b'0000',lead.form(b'MFAM',b'DATA'+struct.pack('>I',5)+struct.pack('<i',id)+b'\0'+b.frame(w))))
def rule(id,op=1,scale=0x41700000,enabled=1):
 raw=bytearray(lead.j.ah.encoded(family=id&0xffffffff,op=op,scale=scale,enabled=enabled));offset=raw.index(b'PARM')+8;struct.pack_into('<I',raw,offset,id&0xffffffff);return raw.hex()
def generate(a):
 n.gate(a.exe.read_bytes(),n.EXE);n.setup();lead.n.setup();raw,entries=lead.original_group(a.asset_root,a.config);w=entries[0]['Words'];cases=[]
 for tp in (24,53,64):
  for cp in (24,53,64):
   for qp in (24,53,64):
    for hp in (24,53,64):
     cases.append(dict(GroupHex=raw.hex(),Words=w,FamilyId=1,RuleHex=rule(1),Operation=1,Scale=0x41700000,Table=tp,Construction=cp,Query=qp,Consumer=hp,X=0x3e000000,Z=0x3ec00000,Height=0xbe800000,Weight=0x3f400000))
 synthetic=list(w);synthetic[0]=47;synthetic[5]=3;synthetic[7]=0x3f000000;synthetic[1]=synthetic[3]=0;synthetic[2]=0x7fc12345;synthetic[4]=0x7f800000
 for id in (-1,-7,1,-2147483648,2147483647):
  for modeId in range(6):
   sw=list(synthetic);sw[12]=modeId
   for p in (24,53,64):cases.append(dict(GroupHex=group(id,sw).hex(),Words=sw,FamilyId=id,RuleHex=rule(id,modeId%4),Operation=modeId%4,Scale=0x41700000,Table=p,Construction=53,Query=p,Consumer=p,X=0xbf000001,Z=0x3e800001,Height=0xbe800000,Weight=0x3f400000))
 template=dict(cases[0]);neg=[]
 for changes in (dict(RuleHex=rule(-1),Missing=True),dict(RuleHex=rule(-7),Missing=True),dict(RuleHex=rule(987654),Missing=True),dict(RuleHex=rule(1,enabled=0),Disabled=True),dict(RuleHex=rule(1,op=4),Operation=4),dict(RuleHex=rule(1,op=99),Operation=99),dict(RuleHex=rule(1,scale=0x7fc12345),Scale=0x7fc12345),dict(Weight=0),dict(Weight=0xbf800000),dict(Weight=0x7fc12345),dict(Height=0x7f800000),dict(X=0x7fc12345),dict(Column=2),dict(Width=0),dict(Table=23),dict(Construction=65),dict(Query=0),dict(Consumer=23),dict(MaximumRuleBytes=0),dict(MaximumOwnedBytes=0),dict(MaximumWork=0),dict(MaximumWork=4194305),dict(Cancel=True),dict(Height=0x7f7fffff,Weight=0x7f7fffff,RuleHex=rule(1,scale=0x7f7fffff),Scale=0x7f7fffff)):
  c=dict(template);c.update(changes);neg.append(c)
 malformed=bytearray.fromhex(template['RuleHex']);malformed[0]^=1;neg.append(dict(template,RuleHex=malformed.hex(),Malformed=True))
 opaque=bytearray.fromhex(template['RuleHex']);opaque[20:24]=b'9999';neg.append(dict(template,RuleHex=opaque.hex(),Opaque=True))
 for index,value in ((12,6),(1,1),(3,1),(6,0x7f800000),(5,0),(0,0x7fffffff)):
  sw=list(synthetic);sw[index]=value;neg.append(dict(template,GroupHex=group(1,sw).hex(),Words=sw))
 cases+=neg
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases

def expected(c):
 try:
  if c.get('Missing') or c.get('Disabled') or c.get('Malformed') or c.get('Opaque') or c.get('Cancel'):raise ValueError()
  tp,cp,qp,hp=(c[k] for k in ('Table','Construction','Query','Consumer'))
  for p in (tp,cp,qp,hp):n.arithmetic(p)
  width,rows,col,row=(c.get(k,v) for k,v in (('Width',2),('Rows',2),('Column',0),('Row',0)))
  if not(1<=width<=128 and 1<=rows<=128 and width*rows<=1024 and 0<=col<width and 0<=row<rows):raise ValueError()
  r=bytes.fromhex(c['RuleHex']);length=len(r);cells=width*rows;account=8*length+65536+9512+136+32*cells+160+1280+256;work=length+20000+256+cells+8192+64+32
  if not 0<=c.get('MaximumRuleBytes',65536)<=65536 or length>c.get('MaximumRuleBytes',65536) or not account<=c.get('MaximumOwnedBytes',1048576)<=4194304 or not work<=c.get('MaximumWork',1048576)<=4194304:raise ValueError()
  height=n.finite_scalar(c['Height']);weight=n.finite_scalar(c['Weight']);n.finite_scalar(c['X']);n.finite_scalar(c['Z']);scale=n.finite_scalar(c['Scale']);op=c['Operation']
  if weight[0]<=0 or op>3:raise ValueError()
  m=mode.Model(dict(Hex=b.frame(c['Words']).hex(),Width=width,Rows=rows,TablePrecision=tp,ConstructionPrecision=cp))
  v=m.miss(dict(XBits=c['X'],ZBits=c['Z']),qp);add,mul,_=n.arithmetic(hp);target=mul(v,scale)
  if op==1:result=add(height,mul(target,weight))
  elif op==2:result=add(height,n.negate(mul(target,weight)))
  elif op==3:result=add(height,mul(add(mul(target,height),n.negate(height)),weight))
  else:result=add(height,mul(add(target,n.negate(height)),weight))
  return dict(Name=c['Name'],Reject='None',FamilyId=c['FamilyId'],SourceSha256=sha(bytes.fromhex(c['GroupHex'])),Origin='ConditionalComputedMiss',Retained=n.fraction_text(v),Projection=b.stored(v),Result=b.stored(result),AccountedBytes=account,AccountedWork=work)
 except (ValueError,ZeroDivisionError,OverflowError,IndexError):return dict(Name=c['Name'],Reject='Input')
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for k in ('exe','asset-root','config','out'):p.add_argument('--'+k,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args()
 if not a.out.resolve().is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 c=generate(a);e=[expected(x) for x in c];a.out.mkdir(parents=True,exist_ok=True)
 summary=dict(Cases=len(c),Rejected=sum(x['Reject']!='None' for x in e),OriginalNoiseSamples=0)
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Cases']==e,'Native routed affector differs';summary.update(Passed=True,OwnershipChecks=native['OwnershipChecks']);(a.out/'route-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:(a.out/'route-fixtures.json').write_text(json.dumps(c),encoding='utf-8');(a.out/'route-expected.json').write_text(json.dumps(e),encoding='utf-8')
 print(json.dumps(summary))
