"""Independent raw selected-rule oracle; original bytes and computed values remain private."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
t=load('selected_tgen33','poc/round33-tests/Assets/verify_tgen_input.py')
f=load('selected_family50','poc/round50-tests/Assets/peer.py')
ah=load('selected_ahfr45','tools/python/verify_terrain_height_fractal.py')
r=load('selected_route51','poc/round51-tests/Rendering/verify_route.py')
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def finite(v):return v&0x7f800000!=0x7f800000
def operational(s,w):
 return s['Operation']<=3 and finite(s['Scale']) and w[12]<=5 and w[1]==w[3]==0 and 1<=w[5]<=16 and w[0]!=0x7fffffff and all(finite(x) for x in w[6:12])
def generator(rule,words=None,id=-7,extra=b''):
 words=words or [47,0,0,0,0,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
 groups=[t.form(tag,t.form(v)) for tag,v in zip(t.TAGS[:4],t.VERS[:4])]+[bytes.fromhex(r.group(id,words).hex())]
 return t.encode(groups=groups,last=t.form(b'LYRS',t.layer(children=rule+extra)))
def original(root,cfg):
 prior=ah.original(root,cfg)
 with contextlib.ExitStack() as stack:
  for priority,path in t.archives(root,cfg):
   payload=t.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and sha(payload)==t.SOURCE
 version,=t.records(payload,12,len(payload));_,section,_=t.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]]
 assert len(g)==139608 and sha(g)==t.GENERATOR
 meta=t.metadata(g);first=next((i,q) for i,q in enumerate(meta['Rules']) if q['Section']['FormType']=='AHFR')
 i,q=first;s=q['Section'];blob=g[s['Offset']:s['Offset']+s['EncodedBytes']]
 assert i==0 and s['Offset']==12346 and len(blob)==114 and sha(blob)=='51735AECA9C172F0BD1E8194ACBCECD8CEF03041075CF307822E275D5E936B1E'
 assert prior['First']['Snapshot']==ah.snapshot(blob)
 return g,i

def expected(c):
 name=c['Name'];reject=dict(Name=name,Reject='Input')
 try:
  g=bytes.fromhex(c['GeneratorHex']);meta=t.metadata(g);index=c.get('Index',0);policy=c.get('Policy',1)
  if c.get('Cancel') or index<0 or index>=len(meta['Rules']) or policy not in (0,1):return reject
  if 'BindingHex' in c and bytes.fromhex(c['BindingHex'])!=g:return reject
  q=meta['Rules'][index];s=q['Section'];blob=g[s['Offset']:s['Offset']+s['EncodedBytes']]
  if s['Tag']!='FORM' or s['FormType']!='AHFR' or s['Version']!='0003':return reject
  snap=ah.snapshot(blob)
  if not snap['HasDecodedFields']:return reject
  group=next(x for x in meta['Groups'] if x['Role']=='FractalMap')['Section'];gb=g[group['Offset']:group['Offset']+group['EncodedBytes']]
  entries=f.inspect(gb,f.LIMITS)['Entries'];key=struct.unpack('<i',struct.pack('<I',snap['Family']))[0]
  family=next((e for e in entries if e['Id']==key),None)
  if family is None:return reject
  used=operational(snap,family['Words']);enabled=snap['Enabled']!=0
  if policy==1 and not(enabled and used):return reject
  maximum=c.get('MaximumRuleBytes',65536);decoded=c.get('MaximumDecodedCopyBytes',65536);names=c.get('MaximumNameBytes',4096);cap=c.get('MaximumSnapshotBytes',1048576);work=c.get('MaximumWork',1048576)
  if not(24<=maximum<=4194304 and 0<=decoded<=4194304 and 0<=names<=65536 and 0<=cap<=16777216 and 0<=work<=16777216):return reject
  reserved=3*len(blob)+min(len(blob),decoded);actual=3*len(blob)+snap['DecodedCopyBytes']
  namebytes=snap['DecodedCopyBytes']-12-(3 if snap['HeaderVersion']=='0000' else 0)
  if len(blob)>maximum or reserved>cap or reserved>work or snap['DecodedCopyBytes']>decoded or namebytes>names:return reject
  result=dict(GeneratorSha256=sha(g),GeneratorBytes=len(g),GroupSha256=sha(gb),GroupOffset=group['Offset'],GroupBytes=len(gb),RuleIndex=index,LayerIndex=q['LayerIndex'],ChildOrdinal=q['ChildOrdinal'],RuleSha256=sha(blob),RuleOffset=s['Offset'],RuleBytes=len(blob),FamilyId=key,FamilyGeneratorOffset=group['Offset']+family['Offset'],FamilyBytes=family['EncodedBytes'],RawSha256=sha(bytes.fromhex(family['RawHex'])),IsEnabled=enabled,HasOperationalInputs=used,Policy=policy,SnapshotBytes=actual,ReservedSnapshotBytes=reserved,Work=actual,ReservedWork=reserved,DecodedCopyBytes=snap['DecodedCopyBytes'],Version=snap['Version'],HeaderVersion=snap['HeaderVersion'],EnabledBits=snap['Enabled'],NameSha256=snap['NameSha'],LegacySha256=snap['LegacySha'],FamilyKeyBits=snap['Family'],OperationBits=snap['Operation'],ScaleBits=snap['Scale'],EncodedSha256=sha(blob))
  return dict(Name=name,Reject='None',Snapshot=result)
 except (ValueError,AssertionError,IndexError,StopIteration,struct.error):return reject

def generate(exe,packages,assetroot,config,out):
 ah.identity(exe.read_bytes());anchors=ah.static(exe,str(packages));g,index=original(assetroot,config)
 cases=[]
 def add(name,encoded=None,**kw):cases.append(dict(Name=name,GeneratorHex=(encoded or base).hex(),**kw))
 base=generator(bytes.fromhex(r.rule(-7)));add('synthetic-signed-family')
 add('original-first-enabled-AHFR',g,Index=index,Original=True)
 for id in (-1,-2147483648,2147483647):add('signed-'+str(id),generator(bytes.fromhex(r.rule(id)),id=id))
 add('explicit-second-rule',generator(bytes.fromhex(r.rule(-7)),extra=bytes.fromhex(r.rule(-7,op=2))),Index=1)
 for idx in (-1,1,2147483647):add('bad-index-'+str(idx),Index=idx)
 add('bad-policy',Policy=2);add('cancel-before-copy',Cancel=True)
 add('wrong-binding',BindingHex=generator(bytes.fromhex(r.rule(-7,op=2))).hex())
 add('wrong-type',generator(t.ahcn()))
 add('wrong-version',generator(ah.encoded(family=0xfffffff9,v='0002')))
 add('unknown-header',generator(ah.encoded(family=0xfffffff9,h=t.header(b'0099'))))
 add('malformed-parameter',generator(ah.encoded(family=0xfffffff9,data=bytes(11))))
 add('missing-family',generator(bytes.fromhex(r.rule(-1))))
 for label,blob in [('disabled',bytes.fromhex(r.rule(-7,enabled=0))),('operation',bytes.fromhex(r.rule(-7,op=4))),('scale',bytes.fromhex(r.rule(-7,scale=0x7fc12345)))]:
  add(label+'-operational',generator(blob));add(label+'-evidence',generator(blob),Policy=0)
 sw=[47,0,0,0,0,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
 for at,value in [(0,0x7fffffff),(1,1),(3,1),(5,0),(5,17),(12,6),(6,0x7f800000),(7,0x7fc12345),(8,0xff800000),(9,0x7f800000),(10,0x7fc12345),(11,0x7f800000)]:
  changed=list(sw);changed[at]=value;data=generator(bytes.fromhex(r.rule(-7)),changed)
  add('family-reject-'+str(at)+'-'+str(value),data);add('family-evidence-'+str(at)+'-'+str(value),data,Policy=0)
 blob=bytes.fromhex(r.rule(-7));decoded=ah.snapshot(blob)['DecodedCopyBytes'];reserve=3*len(blob)+decoded
 add('precopy-large-budget',generator(ah.encoded(family=0xfffffff9,h=t.header(name=b's'*60000,enabled=1))),MaximumNameBytes=60000,MaximumSnapshotBytes=0)
 add('exact-budgets',MaximumRuleBytes=len(blob),MaximumDecodedCopyBytes=decoded,MaximumNameBytes=decoded-12,MaximumSnapshotBytes=reserve,MaximumWork=reserve)
 for key,val in [('MaximumRuleBytes',len(blob)-1),('MaximumDecodedCopyBytes',decoded-1),('MaximumNameBytes',decoded-13),('MaximumSnapshotBytes',4*len(blob)-1),('MaximumWork',4*len(blob)-1),('MaximumRuleBytes',4194305),('MaximumDecodedCopyBytes',-1),('MaximumNameBytes',65537),('MaximumSnapshotBytes',16777217),('MaximumWork',-1)]:add('cap-'+key+'-'+str(val),**{key:val})
 exp=[expected(c) for c in cases];assert exp[0]['Reject']==exp[1]['Reject']=='None'
 originalcase=cases[1];meta=t.metadata(g);group=next(x for x in meta['Groups'] if x['Role']=='FractalMap')['Section'];gb=g[group['Offset']:group['Offset']+group['EncodedBytes']];selected=meta['Rules'][index]['Section'];rb=g[selected['Offset']:selected['Offset']+selected['EncodedBytes']];snap=ah.snapshot(rb);fid=exp[1]['Snapshot']['FamilyId'];family=next(e for e in f.inspect(gb,f.LIMITS)['Entries'] if e['Id']==fid)
 r.n.setup();r.lead.n.setup();route=r.expected(dict(Name='actual-selected-rule',GroupHex=gb.hex(),Words=family['Words'],FamilyId=fid,RuleHex=rb.hex(),Operation=snap['Operation'],Scale=snap['Scale'],Table=53,Construction=53,Query=53,Consumer=53,X=0x3e000000,Z=0x3ec00000,Height=0xbe800000,Weight=0x3f400000))
 assert route['Reject']=='None';route.pop('Name');route.pop('Reject')
 out.joinpath('fixtures.json').write_text(json.dumps(cases),encoding='utf-8');out.joinpath('expected.json').write_text(json.dumps(dict(Cases=exp,Route=route)),encoding='utf-8')
 summary=dict(Cases=len(cases),Rejected=sum(c['Reject']!='None' for c in exp),OriginalCandidatesInspected=1,OriginalCandidatesExcluded=0,OriginalSelected=1,OriginalSyntheticControlSamples=1,StaticAnchors=anchors)
 out.joinpath('producer-receipt.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary))
def verify(out):
 expecteddata=json.loads(out.joinpath('expected.json').read_text());native=json.loads(out.joinpath('native.json').read_text());assert native['Cases']==expecteddata['Cases'],'Selected rule metadata/fields differ';assert native['Route']==expecteddata['Route'],'Original selected rule bounded sample differs';assert native['OwnershipChecks']==9
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=9);out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Unknown operation')
