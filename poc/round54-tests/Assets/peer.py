"""Independent raw selected-rule oracle; original bytes and computed values remain private."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
t=load('filter_tgen33','poc/round33-tests/Assets/verify_tgen_input.py')
f=load('filter_family50','poc/round50-tests/Assets/peer.py')
ff=load('selected_ffra45','tools/python/verify_terrain_filter_fractal.py');ff.setup()
r=load('selected_route51','poc/round51-tests/Rendering/verify_route.py')
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def finite(v):return v&0x7f800000!=0x7f800000
def operational(s,w):
 return all(finite(s[k]) for k in ('Lower','Upper','Amount','Scale')) and w[12]<=5 and w[1]==w[3]==0 and 1<=w[5]<=16 and w[0]!=0x7fffffff and all(finite(x) for x in w[6:12])
def generator(rule,words=None,id=-7,extra=b''):
 words=words or [47,0,0,0,0,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
 groups=[t.form(tag,t.form(v)) for tag,v in zip(t.TAGS[:4],t.VERS[:4])]+[bytes.fromhex(r.group(id,words).hex())]
 return t.encode(groups=groups,last=t.form(b'LYRS',t.layer(children=rule+extra)))
def snapshot(blob):
 top,=t.records(blob,0,len(blob));assert top[0]==b'FORM' and top[1][:4]==b'FFRA'
 version,=t.records(blob,12,len(blob));tag=version[1][:4].decode()
 base=dict(Version=tag,HasDecodedFields=False)
 if tag!='0005':return base
 hdr,data=list(t.records(blob,version[2]+12,version[3]));assert hdr[0]==data[0]==b'FORM' and hdr[1][:4]==b'IHDR' and data[1][:4]==b'DATA'
 hv,=t.records(blob,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
 if ht not in ('0000','0001'):return base
 hd,=t.records(blob,hv[2]+12,hv[3]);assert hd[0]==b'DATA';raw=hd[1];end=raw.index(0,4);name=raw[4:end];legacy=raw[end+1:];assert len(legacy)==(3 if ht=='0000' else 0)
 parm,=t.records(blob,data[2]+12,data[3]);assert parm[0]==b'PARM';words=struct.unpack('<6I',parm[1]);family,selector,amount,lower,upper,scale=words
 base.update(HasDecodedFields=True,HeaderVersion=ht,Enabled=struct.unpack_from('<I',raw)[0],DecodedCopyBytes=24+len(name)+len(legacy),NameSha=sha(name),LegacySha=sha(legacy),Words=list(words),Family=family,Selector=selector,Amount=amount,Lower=lower,Upper=upper,Scale=scale)
 return base

def original(root,cfg):
 pinned,pinned_snapshot=ff.original(root,cfg)
 assert snapshot(pinned)['Words']==pinned_snapshot['Words'] and sha(pinned)==ff.PIN
 with contextlib.ExitStack() as stack:
  for priority,path in t.archives(root,cfg):
   payload=t.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and sha(payload)==t.SOURCE
 version,=t.records(payload,12,len(payload));_,section,_=t.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]]
 assert len(g)==139608 and sha(g)==t.GENERATOR
 meta=t.metadata(g);group=next(x for x in meta['Groups'] if x['Role']=='FractalMap')['Section'];gb=g[group['Offset']:group['Offset']+group['EncodedBytes']];families=f.inspect(gb,f.LIMITS)['Entries'];visited=[]
 for i,q in enumerate(meta['Rules']):
  if q['Section']['FormType']!='FFRA':continue
  assert len(visited)<16,'Bounded candidate limit exhausted'
  section=q['Section'];blob=g[section['Offset']:section['Offset']+section['EncodedBytes']];reason=None
  try:
   snap=snapshot(blob);key=struct.unpack('<i',struct.pack('<I',snap['Family']))[0];family=next((e for e in families if e['Id']==key),None)
   if not snap['Enabled']:reason='disabled'
   elif family is None:reason='missing-family'
   elif not operational(snap,family['Words']):reason='ineligible-inputs'
  except (KeyError,ValueError,AssertionError,struct.error):reason='untyped-or-malformed'
  visited.append(dict(RuleIndex=i,LayerIndex=q['LayerIndex'],ChildOrdinal=q['ChildOrdinal'],Offset=section['Offset'],Bytes=section['EncodedBytes'],Sha=sha(blob),Exclusion=reason))
  if reason is None:
   assert snap['Version']=='0005'
   assert (i,q['LayerIndex'],q['ChildOrdinal'],section['Offset'],len(blob),sha(blob))==(8,3,3,13554,118,'038FA063AD5C2B8A10562A0040748B9C0CB247645418F38B5ACAE682409A1136')
   return g,i,visited
 raise ValueError('No bounded eligible original FFRA')

def expected(c):
 name=c['Name'];reject=dict(Name=name,Reject='Input')
 try:
  g=bytes.fromhex(c['GeneratorHex']);meta=t.metadata(g);index=c.get('Index',0);policy=c.get('Policy',1)
  if c.get('Cancel') or index<0 or index>=len(meta['Rules']) or policy not in (0,1):return reject
  if 'BindingHex' in c and bytes.fromhex(c['BindingHex'])!=g:return reject
  q=meta['Rules'][index];s=q['Section'];blob=g[s['Offset']:s['Offset']+s['EncodedBytes']]
  if s['Tag']!='FORM' or s['FormType']!='FFRA' or s['Version']!='0005':return reject
  snap=snapshot(blob)
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
  namebytes=snap['DecodedCopyBytes']-24-(3 if snap['HeaderVersion']=='0000' else 0)
  if len(blob)>maximum or reserved>cap or reserved>work or snap['DecodedCopyBytes']>decoded or namebytes>names:return reject
  result=dict(GeneratorSha256=sha(g),GeneratorBytes=len(g),GroupSha256=sha(gb),GroupOffset=group['Offset'],GroupBytes=len(gb),RuleIndex=index,LayerIndex=q['LayerIndex'],ChildOrdinal=q['ChildOrdinal'],RuleSha256=sha(blob),RuleOffset=s['Offset'],RuleBytes=len(blob),FamilyId=key,FamilyGeneratorOffset=group['Offset']+family['Offset'],FamilyBytes=family['EncodedBytes'],RawSha256=sha(bytes.fromhex(family['RawHex'])),IsEnabled=enabled,HasOperationalInputs=used,Policy=policy,SnapshotBytes=actual,ReservedSnapshotBytes=reserved,Work=actual,ReservedWork=reserved,DecodedCopyBytes=snap['DecodedCopyBytes'],Version=snap['Version'],HeaderVersion=snap['HeaderVersion'],EnabledBits=snap['Enabled'],NameSha256=snap['NameSha'],LegacySha256=snap['LegacySha'],FamilyIdBits=snap['Family'],FeatheringTypeBits=snap['Selector'],FeatheringAmountBits=snap['Amount'],LowerSampleBits=snap['Lower'],UpperSampleBits=snap['Upper'],SampleScaleBits=snap['Scale'],EncodedSha256=sha(blob))
  return dict(Name=name,Reject='None',Snapshot=result)
 except (ValueError,AssertionError,IndexError,StopIteration,struct.error):return reject

def generate(exe,packages,assetroot,config,out):
 if sha(exe.read_bytes())!=ff.EXE:raise ValueError('EXE identity rejected before VA interpretation')
 anchors=ff.static(exe,packages);g,index,visited=original(assetroot,config);cases=[]
 def rule(id=-7,**kw):return ff.encoded(family=id&0xffffffff,**kw)
 def add(name,encoded=None,**kw):cases.append(dict(Name=name,GeneratorHex=(encoded or base).hex(),**kw))
 base=generator(rule());add('synthetic-signed-family')
 add('original-enabled-FFRA',g,Index=index,Original=True)
 for id in (-1,-2147483648,2147483647):add('signed-'+str(id),generator(rule(id),id=id))
 add('explicit-second-rule',generator(rule(),extra=rule(selector=2)),Index=1)
 for idx in (-1,1,2147483647):add('bad-index-'+str(idx),Index=idx)
 add('bad-policy',Policy=2);add('cancel-before-copy',Cancel=True)
 add('wrong-binding',BindingHex=generator(rule(selector=2)).hex())
 add('wrong-type',generator(t.ahcn()))
 for version in ('0000','0001','0002','0003','0004','0099'):add('opaque-version-'+version,generator(rule(version=version)))
 add('unknown-header',generator(rule(header=t.header(b'0099'))))
 add('malformed-parameter',generator(rule(data=bytes(23))))
 add('missing-family',generator(rule(-1)))
 for label,blob in [('disabled',rule(enabled=0)),('lower',rule(lo=0x7fc12345)),('upper',rule(hi=0x7f800000)),('amount',rule(amount=0xff800000)),('scale',rule(scale=0x7fc12345))]:
  add(label+'-operational',generator(blob));add(label+'-evidence',generator(blob),Policy=0)
 # These are valid raw finite inputs, including consumer-handled unusual thresholds/selectors/amounts.
 for label,kw in [('reversed',dict(lo=0x40000000,hi=0)),('equal',dict(lo=0x3f800000,hi=0x3f800000)),('unknown-selector',dict(selector=0xffffffff)),('negative-amount',dict(amount=0xbf800000)),('large-amount',dict(amount=0x40000000)),('zero-scale',dict(scale=0)),('legacy-header',dict(header=t.header(b'0000',enabled=1)))]:add(label,generator(rule(**kw)))
 sw=[47,0,0,0,0,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
 for at,value in [(0,0x7fffffff),(1,1),(3,1),(5,0),(5,17),(12,6),(6,0x7f800000),(7,0x7fc12345),(8,0xff800000),(9,0x7f800000),(10,0x7fc12345),(11,0x7f800000)]:
  changed=list(sw);changed[at]=value;data=generator(rule(),changed)
  add('family-reject-'+str(at)+'-'+str(value),data);add('family-evidence-'+str(at)+'-'+str(value),data,Policy=0)
 unused=list(sw);unused[2]=0x7fc12345;unused[4]=0x7f800000;add('disabled-curve-unused-nonfinite',generator(rule(),unused))
 blob=rule();decoded=snapshot(blob)['DecodedCopyBytes'];reserve=3*len(blob)+decoded
 add('precopy-large-budget',generator(rule(name=b's'*60000)),MaximumNameBytes=60000,MaximumSnapshotBytes=0)
 add('exact-budgets',MaximumRuleBytes=len(blob),MaximumDecodedCopyBytes=decoded,MaximumNameBytes=decoded-24,MaximumSnapshotBytes=reserve,MaximumWork=reserve)
 for key,val in [('MaximumRuleBytes',len(blob)-1),('MaximumDecodedCopyBytes',decoded-1),('MaximumNameBytes',decoded-25),('MaximumSnapshotBytes',4*len(blob)-1),('MaximumWork',4*len(blob)-1),('MaximumRuleBytes',4194305),('MaximumDecodedCopyBytes',-1),('MaximumNameBytes',65537),('MaximumSnapshotBytes',16777217),('MaximumWork',-1)]:add('cap-'+key+'-'+str(val),**{key:val})
 exp=[expected(c) for c in cases];assert exp[0]['Reject']==exp[1]['Reject']=='None'
 meta=t.metadata(g);group=next(x for x in meta['Groups'] if x['Role']=='FractalMap')['Section'];gb=g[group['Offset']:group['Offset']+group['EncodedBytes']];section=meta['Rules'][index]['Section'];blob=g[section['Offset']:section['Offset']+section['EncodedBytes']];snap=snapshot(blob);fid=exp[1]['Snapshot']['FamilyId'];family=next(e for e in f.inspect(gb,f.LIMITS)['Entries'] if e['Id']==fid)
 r.n.setup();r.lead.n.setup();model=r.mode.Model(dict(Hex=r.b.frame(family['Words']).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53));value=model.miss(dict(XBits=0x3e000000,ZBits=0x3ec00000),53)
 _,mul,_=r.n.arithmetic(53);scaled=r.b.stored(mul(value,r.n.finite_scalar(snap['Scale'])))
 fixture=dict(Version='0002',Lower=snap['Lower'],Upper=snap['Upper'],Amount=snap['Amount'],Height=scaled,Precision=53,Limits=ff.fh.QUERY,Action='query',Selector=snap['Selector'],Enabled=1)
 result=ff.fh.evaluate(fixture);assert result['Reject']=='None';query=ff.old.query_number(fixture);assert not isinstance(query,dict)
 route=dict(FamilyId=fid,SourceSha256=sha(gb),Origin='ConditionalComputedMiss',Retained=r.n.fraction_text(value),Projection=r.b.stored(value),Scaled=scaled,Amount=result['Amount'],Branch=result['Branch'],Query=result['Query'],Span=result['Span'],Influence=ff.l.store(ff.l.shape(query,snap['Selector'],53)),Work=3)
 out.joinpath('fixtures.json').write_text(json.dumps(cases),encoding='utf-8');out.joinpath('expected.json').write_text(json.dumps(dict(Cases=exp,Route=route)),encoding='utf-8');out.joinpath('candidate-receipt.json').write_text(json.dumps(visited,indent=2),encoding='utf-8')
 summary=dict(Cases=len(cases),Rejected=sum(c['Reject']!='None' for c in exp),OriginalCandidatesInspected=len(visited),OriginalCandidatesExcluded=len(visited)-1,OriginalSelected=1,OriginalSyntheticControlSamples=1,StaticAnchors=anchors)
 out.joinpath('producer-receipt.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary))

def verify(out):
 expecteddata=json.loads(out.joinpath('expected.json').read_text());native=json.loads(out.joinpath('native.json').read_text());assert native['Cases']==expecteddata['Cases'],'Selected rule metadata/fields differ';assert native['Route']==expecteddata['Route'],'Original selected rule bounded sample differs';assert native['OwnershipChecks']==9;assert native['CleanupChecks']==2
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=9,CleanupChecks=2);out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Unknown operation')
