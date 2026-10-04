"""Pinned FFRA/0005 bytes and independently supplied-sample Fraction consumer; no original sampler execution."""
import argparse,contextlib,hashlib,importlib.util,itertools,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
PIN='A601BE86EAB8D9405F8F567F2E27DF65AC2A208C6A556E564AA6B14E13C8C12D'
INPUT=dict(MaximumInputBytes=65536,MaximumNodes=64,MaximumDepth=8,MaximumStringBytes=4096,MaximumDecodedCopyBytes=65536)
QUERY=dict(MaximumSamples=65536,MaximumQueryWork=196608)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def setup():
 global old,fh,h,l,a,m,query_exact_height
 old=load('fractal_old_filtered','poc/round43-tests/Rendering/verify_filtered.py');fh=old.fh;h=old.h;l=h.l;a=h.a;m=fh.m
 source=(ROOT/'poc/round42-tests/Assets/verify_height_filter.py').read_text();body=source[source.index('def evaluate('):source.index('\ndef fixtures():')];ns=dict(fh.__dict__)
 exec(body.replace('def evaluate(','def query_exact_height(').replace('  projection=store(u)','  return u').replace("lo,hi,h=map(a.scalar,[f['Lower'],f['Upper'],f['Height']]);spanbits=None","lo,hi=map(a.scalar,[f['Lower'],f['Upper']]);h=f['ExactHeight'];spanbits=None"),ns);query_exact_height=ns['query_exact_height']
ANCHORS={
 0x00B77B53: ('push', '0x46465241'),
 0x00B77B5A: ('call', '0xb633e0'),
 0x00B77B61: ('mov', 'dword ptr [esi + 0x1c], eax'),
 0x00B77B64: ('mov', 'dword ptr [esi + 0x24], eax'),
 0x00B77B67: ('mov', 'dword ptr [esi + 0x2c], eax'),
 0x00B77B6A: ('mov', 'dword ptr [esi + 0x30], eax'),
 0x00B77B6D: ('mov', 'dword ptr [esi + 0x20], 0xffffffff'),
 0x00B77B74: ('mov', 'dword ptr [esi + 0x28], 0x3f800000'),
 0x00B77B7B: ('mov', 'dword ptr [esi], 0x15ed704'),
 0x00B77BE6: ('mov', 'eax, dword ptr [esi + 0x24]'),
 0x00B77BE9: ('cmp', 'dword ptr [esi + 0x20], eax'),
 0x00B77BEC: ('je', '0xb77c00'),
 0x00B77BEE: ('mov', 'dword ptr [esi + 0x20], eax'),
 0x00B77BF2: ('mov', 'eax, dword ptr [ebp + 0x18]'),
 0x00B77BF5: ('mov', 'ecx, dword ptr [eax + 0x58]'),
 0x00B77BF8: ('call', '0xb75b70'),
 0x00B77BFD: ('mov', 'dword ptr [esi + 0x1c], eax'),
 0x00B77C00: ('mov', 'ecx, dword ptr [ebp + 0x14]'),
 0x00B77C03: ('mov', 'edx, dword ptr [ebp + 0x10]'),
 0x00B77C06: ('mov', 'eax, dword ptr [ebp + 0xc]'),
 0x00B77C0A: ('mov', 'ecx, dword ptr [ebp + 8]'),
 0x00B77C10: ('mov', 'ecx, dword ptr [esi + 0x1c]'),
 0x00B77C13: ('call', '0x13e7c00'),
 0x00B77C18: ('fmul', 'dword ptr [esi + 0x28]'),
 0x00B77C1B: ('mov', 'ecx, dword ptr [esi + 0x2c]'),
 0x00B77C1E: ('mov', 'eax, dword ptr [esi + 0x30]'),
 0x00B77C21: ('mov', 'edx, dword ptr [esi + 0x18]'),
 0x00B77C24: ('fstp', 'dword ptr [ebp + 0x18]'),
 0x00B77C27: ('fld', 'dword ptr [ebp + 0x18]'),
 0x00B77C2A: ('mov', 'dword ptr [ebp + 0x14], ecx'),
 0x00B77C2D: ('fcomp', 'dword ptr [ebp + 0x14]'),
 0x00B77C30: ('mov', 'dword ptr [ebp + 0x10], eax'),
 0x00B77C33: ('mov', 'dword ptr [ebp + 0xc], edx'),
 0x00B77C39: ('test', 'ah, 0x41'),
 0x00B77C3C: ('jne', '0xb77c4b'),
 0x00B77C3E: ('fld', 'dword ptr [ebp + 0x18]'),
 0x00B77C41: ('fcomp', 'dword ptr [ebp + 0x10]'),
 0x00B77C46: ('test', 'ah, 5'),
 0x00B77C49: ('jnp', '0xb77c55'),
 0x00B77C4B: ('fld', 'dword ptr [0x15dc6a0]'),
 0x00B77C55: ('fld', 'dword ptr [ebp + 0x10]'),
 0x00B77C58: ('fsub', 'dword ptr [ebp + 0x14]'),
 0x00B77C5B: ('fmul', 'dword ptr [ebp + 0xc]'),
 0x00B77C5E: ('fmul', 'dword ptr [0x15dc5e4]'),
 0x00B77C64: ('fld', 'dword ptr [ebp + 0x14]'),
 0x00B77C67: ('fadd', 'st(1)'),
 0x00B77C69: ('fcomp', 'dword ptr [ebp + 0x18]'),
 0x00B77C6E: ('test', 'ah, 0x41'),
 0x00B77C71: ('jne', '0xb77c7f'),
 0x00B77C73: ('fld', 'dword ptr [ebp + 0x18]'),
 0x00B77C76: ('fsub', 'dword ptr [ebp + 0x14]'),
 0x00B77C79: ('fdivrp', 'st(1)'),
 0x00B77C7F: ('fld', 'dword ptr [ebp + 0x10]'),
 0x00B77C82: ('fsub', 'st(1)'),
 0x00B77C84: ('fcomp', 'dword ptr [ebp + 0x18]'),
 0x00B77C89: ('test', 'ah, 5'),
 0x00B77C8C: ('jp', '0xb77c9a'),
 0x00B77C8E: ('fld', 'dword ptr [ebp + 0x10]'),
 0x00B77C91: ('fsub', 'dword ptr [ebp + 0x18]'),
 0x00B77C94: ('fdivrp', 'st(1)'),
 0x00B77C9A: ('fstp', 'st(0)'),
 0x00B77C9C: ('fld', 'dword ptr [0x15dc6f8]'),
 0x00B787B6: ('push', '0x44415441'),
 0x00B787BD: ('call', '0xa94c30'),
 0x00B787C6: ('push', '0x5041524d'),
 0x00B787CD: ('call', '0xa94d00'),
 0x00B787DA: ('call', '0xa94f00'),
 0x00B787E5: ('call', '0xb789a0'),
 0x00B787F2: ('call', '0xa94f00'),
 0x00B787FD: ('call', '0xb65fd0'),
 0x00B7880A: ('call', '0xa94f00'),
 0x00B7881A: ('test', 'ah, 5'),
 0x00B7881F: ('mov', 'dword ptr [ebp + 8], 0'),
 0x00B78833: ('test', 'ah, 0x41'),
 0x00B78838: ('mov', 'dword ptr [ebp + 8], 0x3f800000'),
 0x00B7884F: ('call', 'dword ptr [eax + 0x10]'),
 0x00B7885A: ('call', '0xa94f00'),
 0x00B78865: ('call', '0xb789c0'),
 0x00B78872: ('call', '0xa94f00'),
 0x00B7887D: ('call', '0xb789d0'),
 0x00B7888A: ('call', '0xa94f00'),
 0x00B78895: ('call', '0xb789b0'),
 0x00B7891A: ('call', '0xa94870'),
 0x00B7892D: ('call', '0xa94870'),
 0x00B78940: ('call', '0xa94870'),
 0x00B78953: ('call', '0xa94870'),
 0x00B78966: ('call', '0xa94870'),
 0x00B78979: ('call', '0xa94870'),
 0x00B789C6: ('mov', 'dword ptr [ecx + 0x2c], eax'),
 0x00B789D6: ('mov', 'dword ptr [ecx + 0x30], eax'),
 0x00B7A02D: ('cmp', 'eax, 0x46465241'),
 0x00B7A05D: ('call', '0xb77b50'),
 0x00B7A07B: ('call', 'dword ptr [edx + 0x20]'),
}
def static(exe,packages):
 data=exe.read_bytes()
 if sha(data)!=EXE:raise ValueError('Executable identity mismatch; rejected before decoding')
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,want in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want,(i.mnemonic,i.op_str))
 assert struct.unpack_from('<9I',image,0x015ED704-base)==(0xb77b90,0xb62f90,0xb62fa0,0xb788d0,0xb65fe0,0xb77be0,0xb63420,0xb63430,0xb77cb0)
 assert struct.unpack_from('<I',image,0x015ED700-base)[0]==0x016263a4 and struct.unpack_from('<I',image,0x016263a4-base+12)[0]==0x018b8a94
 assert image[0x018b8a9c-base:0x018b8a9c-base+len(b'.?AVFilterFractal@@\0')]==b'.?AVFilterFractal@@\0'
 ranges=[(0xb77dd0,0xb77f20),(0xb77f20,0xb78140),(0xb78140,0xb78370),(0xb78370,0xb785f0),(0xb785f0,0xb78790),(0xb78790,0xb788d0)]
 reads=[]
 for start,end in ranges:
  ins=list(md.disasm(image[start-base:end-base],start));reads.append(sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins))
 assert reads==[4,13,14,15,5,6],reads
 return dict(ExecutableSha256=EXE,InstructionAnchors=len(ANCHORS),Rtti=1,VtableEntries=9,VersionScalarReadCounts=reads,Sampler='013E7C00',Query='00B77BE0',MandatoryScaleF32Store='00B77C24',OriginalExecuted=False,OriginalFloatingControls='Unknown')
def encoded(lo=0,hi=0x40000000,selector=0,amount=0x3f000000,scale=0x3f800000,family=27,enabled=1,version='0005',name=b'synthetic',data=None,header=None):
 raw=fh.raw;payload=raw.words([family,selector,amount,lo,hi,scale]) if data is None else data
 return raw.form(b'FFRA',raw.form(version.encode(),(raw.header(b'0001',name=name,enabled=enabled) if header is None else header)+raw.form(b'DATA',raw.chunk(b'PARM',payload))))
def snapshot(blob,typed=True,version='0005',lo=0,hi=0x40000000,selector=0,amount=0x3f000000,scale=0x3f800000,family=27,enabled=1,name=b'synthetic'):
 return dict(Version=version,ClientVersionRecognized=version in ('0000','0001','0002','0003','0004','0005'),HasDecodedFields=typed,DecodedCopyBytes=24+len(name) if typed else 0,Words=[family,selector,amount,lo,hi,scale] if typed else [],Lower=lo if typed else None,Upper=hi if typed else None,Family=family if typed else None,Scale=scale if typed else None,Selector=selector if typed else None,Amount=amount if typed else None,HeaderVersion='0001' if typed else None,Enabled=enabled if typed else None,NameSha=sha(name) if typed else None,LegacySha=sha(b'') if typed else None,EncodedBytes=len(blob),Sha256=sha(blob))
def evaluate(f,premature=False,omit_scale_store=False):
 p=f['Precision'];limit=f['Limits'];cp=f['Combine']
 if p not in (24,53,64) or cp not in (24,53,64) or not 0<=limit['MaximumSamples']<=1048576 or not 0<=limit['MaximumQueryWork']<=3145728:return dict(Reject='Options')
 if f['Version']!='0005':return dict(Reject='Input')
 active=f['Action']=='query' or bool(f['Enabled']);work=2 if f['Action']=='query' else 3 if active else 0
 if limit['MaximumSamples']<1 or limit['MaximumQueryWork']<work:return dict(Reject='Input')
 empty=dict(CarrierBits=None,LayerBits=None,CarrierSource=None,Skipped=True,Influence=None,Query=None,Branch=None,Scaled=None,Amount=None,Span=None,Work=0)
 if not active:return dict(Reject='None',**empty)
 if not all(a.finite(x) for x in [f['Lower'],f['Upper'],f['Amount'],f['Scale'],f['Sample']]):return dict(Reject='Input')
 scaled_number=a.precision(a.mul(a.scalar(f['Sample']),a.scalar(f['Scale'])),p);scaled=l.store(scaled_number) if a.finite(a.bits(scaled_number)) else None
 if scaled is None:return dict(Reject='Input')
 q=dict(Version='0002',Lower=f['Lower'],Upper=f['Upper'],Amount=f['Amount'],Height=scaled,Enabled=f['Enabled'],Precision=p,Limits=fh.QUERY,Action='query',Selector=f['Selector'])
 result=fh.evaluate(q,premature)
 if result['Reject']!='None':return dict(Reject=result['Reject'])
 u=old.query_number(q)
 if isinstance(u,dict):return dict(Reject='Input')
 if premature:u=a.scalar(l.store(u))
 carrier=l.store(l.shape(u,f['Selector'],cp))
 # Mandatory F32 sampler-product store witness uses a distinct model with retained scaled operand.
 if omit_scale_store:
  alt=query_exact_height(dict(q,ExactHeight=scaled_number))
  if not isinstance(alt,dict):carrier=l.store(l.shape(alt,f['Selector'],cp))
  else:return dict(Reject='Input')
 return dict(Reject='None',CarrierBits=carrier,LayerBits=carrier,CarrierSource=5,Skipped=False,Influence=result['Influence'],Query=result['Query'],Branch=result['Branch'],Scaled=scaled,Amount=result['Amount'],Span=result['Span'],Work=work)
def fixtures():
 raw=[];queries=[]
 def rc(blob,expected,limits=None):raw.append(dict(Name='raw-'+str(len(raw)),Hex=blob.hex(),Limits=limits or INPUT,Expected=expected))
 base=encoded();rc(base,dict(Reject='None',Snapshot=snapshot(base)))
 for v in ('0000','0001','0002','0003','0004','0099'):
  b=encoded(version=v);rc(b,dict(Reject='None',Snapshot=snapshot(b,False,version=v)))
 for i in range(len(base)):rc(base[:i],dict(Reject='Input'))
 for b in [base+b'\0',encoded(data=bytes(23)),encoded(data=bytes(25)),fh.raw.form(b'FHGT',base[12:]),fh.raw.form(b'FFRA',fh.raw.form(b'0005',fh.raw.chunk(b'DATA',bytes(24))))]:rc(b,dict(Reject='Input'))
 exact={**INPUT,'MaximumInputBytes':len(base),'MaximumNodes':7,'MaximumDepth':4,'MaximumStringBytes':9,'MaximumDecodedCopyBytes':33};rc(base,dict(Reject='None',Snapshot=snapshot(base)),exact)
 for k in exact:
  lim=exact.copy();lim[k]-=1;rc(base,dict(Reject='Input'),lim)
 for k,values in [('MaximumInputBytes',[23,4194305]),('MaximumNodes',[0,65537]),('MaximumDepth',[0,65]),('MaximumStringBytes',[-1,65537]),('MaximumDecodedCopyBytes',[-1,4194305])]:
  for v in values:lim=INPUT.copy();lim[k]=v;rc(base,dict(Reject='Options'),lim)
 for enabled,bits in itertools.product((0,1),(0,0x80000000,0x7fc12345,0x7f800000,0xff800000)):
  b=encoded(lo=bits,hi=bits,amount=bits,scale=bits,enabled=enabled);rc(b,dict(Reject='None',Snapshot=snapshot(b,lo=bits,hi=bits,amount=bits,scale=bits,enabled=enabled)))
 def qc(**kw):
  f=dict(Version='0005',Lower=0,Upper=0x40000000,Selector=0,Amount=0x3f000000,Scale=0x3f800000,Family=27,Enabled=1,Sample=0x3f800000,Precision=53,Combine=53,Action='evaluate',Limits=QUERY)
  f.update(kw);f.update(Name='query-'+str(len(queries)),Hex=encoded(lo=f['Lower'],hi=f['Upper'],selector=f['Selector'],amount=f['Amount'],scale=f['Scale'],family=f['Family'],enabled=f['Enabled'],version=f['Version']).hex());queries.append(f)
 fb=l.fb
 for p,cp,sel,sample,scale in itertools.product((24,53,64),(24,53,64),range(5),(0,0x80000000,1,fb(.2),fb(.9),fb(1),fb(2),fb(3)),(fb(-1),0x80000000,fb(.7),fb(1),fb(1.3))):qc(Precision=p,Combine=cp,Selector=sel,Sample=sample,Scale=scale,Amount=fb(.7))
 rng=random.Random(45003)
 for i in range(450):qc(Precision=rng.choice((24,53,64)),Combine=rng.choice((24,53,64)),Selector=rng.randrange(4),Sample=fb(rng.uniform(.01,1)),Scale=fb(rng.uniform(.2,2)),Amount=fb(rng.uniform(.3,1)),Upper=fb(rng.uniform(1,4)))
 for field in ('Lower','Upper','Amount','Scale','Sample'):
  for bits in (0x7fc12345,0x7f800000,0xff800000):qc(**{field:bits});qc(Enabled=0,**{field:bits})
 for lo,hi in ((fb(1),fb(1)),(fb(2),0),(0x80000000,0),(0xff7fffff,0x7f7fffff)):
  for p in (24,53,64):qc(Lower=lo,Upper=hi,Precision=p)
 for amt in (fb(-1),0x80000000,0,1,fb(2)):
  for p in (24,53,64):qc(Amount=amt,Precision=p,Sample=fb(.2))
 qc(Sample=0x7f7fffff,Scale=fb(2));qc(Action='query',Selector=3);qc(Version='0004');qc(Enabled=0,Sample=0x7fc12345,Limits={**QUERY,'MaximumQueryWork':0})
 for cap in (0,1,2,3):qc(Limits={**QUERY,'MaximumQueryWork':cap},Action='query');qc(Limits={**QUERY,'MaximumQueryWork':cap})
 for key,vals in [('MaximumSamples',[-1,0,1048577]),('MaximumQueryWork',[-1,3145729])]:
  for v in vals:qc(Limits={**QUERY,key:v})
 qc(Precision=23);qc(Combine=25)
 return dict(RawCases=raw,QueryCases=queries)
def original(root,cfg):
 if sha(cfg.read_bytes())!=CONFIG:raise ValueError('Config identity mismatch; rejected before decoding')
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and len(payload)==1188446 and sha(payload)==m.SOURCE
 v,=m.records(payload,12,len(payload));_,section,_=m.records(payload,v[2]+12,v[3]);data=payload[section[2]:section[3]];assert section[2]==210 and sha(data)==m.GENERATOR
 blob=data[14086:14204];assert len(blob)==118 and sha(blob)==PIN
 v,=m.records(blob,12,len(blob));hdr,d=list(m.records(blob,v[2]+12,v[3]));hv,=m.records(blob,hdr[2]+12,hdr[3]);hd,=m.records(blob,hv[2]+12,hv[3]);name=hd[1][4:-1];enabled=struct.unpack_from('<I',hd[1])[0];parm,=m.records(blob,d[2]+12,d[3]);family,selector,amount,lo,hi,scale=struct.unpack('<6I',parm[1]);assert (family,selector,amount,lo,hi,scale)==(27,3,1058776547,1057786692,1058642330,1065353216)
 return blob,snapshot(blob,lo=lo,hi=hi,selector=selector,amount=amount,scale=scale,family=family,enabled=enabled,name=name)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--asset-root',type=Path,required=True);p.add_argument('--config',type=Path,required=True);p.add_argument('--run-root',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
 if sha(args.exe.read_bytes())!=EXE:raise ValueError('Executable identity mismatch; rejected before decoding')
 if sha(args.config.read_bytes())!=CONFIG:raise ValueError('Config identity mismatch; rejected before decoding')
 assert 'local-private' in args.run_root.parts;st=static(args.exe,args.packages);setup();blob,original_expected=original(args.asset_root,args.config);fs=fixtures();fs['OriginalHex']=blob.hex()
 expected_raw=[dict(Name=f['Name'],**f['Expected']) for f in fs['RawCases']];expected_query=[dict(Name=f['Name'],**evaluate(f)) for f in fs['QueryCases']]
 witnesses=sum(evaluate(f,True)!=evaluate(f) for f in fs['QueryCases']);scale_witnesses=sum(evaluate(f,omit_scale_store=True).get('CarrierBits')!=evaluate(f).get('CarrierBits') for f in fs['QueryCases']);assert witnesses>0 and scale_witnesses>0
 carriers=l.fixtures();filtered=old.fixtures();result=dict(Static=st,RawCases=len(expected_raw),RawRejections=sum(f['Reject']!='None' for f in expected_raw),QueryCases=len(expected_query),QueryRejections=sum(f['Reject']!='None' for f in expected_query),PrematureProjectionWitnesses=witnesses,MandatoryScaleStoreWitnesses=scale_witnesses,CarrierRegression=len(carriers),FilteredRegression=len(filtered),OriginalOccurrences=1,OriginalSha256=PIN,SyntheticQuerySamples=True)
 args.run_root.mkdir(parents=True,exist_ok=True)
 if args.native:
  n=json.loads(args.native.read_text());assert n['Fresh']['RawCases']==expected_raw
  for f,want,got in zip(fs['QueryCases'],expected_query,n['Fresh']['QueryCases']):assert want==got,(f,want,got)
  assert len(n['Fresh']['QueryCases'])==len(expected_query) and n['Fresh']['OwnershipChecks']>=30 and n['Fresh']['ArgumentNegatives']==6;result['OwnershipChecks']=n['Fresh']['OwnershipChecks'];assert n['Fresh']['Original']==original_expected
  assert n['CarrierRegression']['Cases']==[l.evaluate(f) for f in carriers] and n['CarrierRegression']['OwnershipChecks']>=53
  assert n['FilteredRegression']['Cases']==[old.evaluate(f) for f in filtered] and n['FilteredRegression']['OwnershipChecks']>=50
 else:
  (args.run_root/'fixtures.json').write_text(json.dumps(fs,separators=(',',':')));(args.run_root/'carrier.json').write_text(json.dumps(carriers,separators=(',',':')));(args.run_root/'filtered.json').write_text(json.dumps(filtered,separators=(',',':')))
 (args.run_root/'receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
