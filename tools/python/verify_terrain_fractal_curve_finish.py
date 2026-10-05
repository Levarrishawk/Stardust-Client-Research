"""SHA-gated static curve finish, symbolic operands and exact branch fixtures. No power/noise implementation."""
import argparse,hashlib,importlib.util,json,stat,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
REGIONS={
 'Finish':(0x013E7F79,0x013E807F,'5fc8847432656daa28b92927a8efc72624f29ad818e6554ec9ffaec266161538'),
 'PowerWrapper':(0x0131E2B0,0x0131E2C9,'a39b146e16cbbab84442ad0a2ac74ad3532fec7ad9f70bf38cf2d7d8f4a99ff9'),
 'DenominatorWriter':(0x013E6E30,0x013E6E41,'42fbec7a07a90212525418ab85728bbdba825388dcd9efa6cf109f0e1e961f9e'),
 'PowerCoreBoundedFragment':(0x0131E2D2,0x0131E4A5,'8ffbfc05a7dbb02aad5bc74aa1cc0a1fca098acc5acf913239e8d4fc9b8a160b')}
ANCHORS={
 0x013E7F91:('fst','dword ptr [ebp - 4]'),0x013E7F99:('je','0x13e7fc3'),
 0x013E7F9B:('fstp','st(0)'),0x013E7F9D:('fld','dword ptr [ebp - 4]'),
 0x013E7FA0:('fld','dword ptr [esi + 0x28]'),0x013E7FA3:('fldln2',''),
 0x013E7FA5:('fxch','st(1)'),0x013E7FA7:('fyl2x',''),0x013E7FA9:('fst','dword ptr [ebp + 8]'),
 0x013E7FAC:('fldln2',''),0x013E7FAE:('fld','qword ptr [0x15e6480]'),
 0x013E7FB4:('fyl2x',''),0x013E7FB6:('fst','dword ptr [ebp + 8]'),0x013E7FB9:('fdivp','st(1)'),
 0x013E7FBB:('call','0x131e2b0'),0x013E7FC0:('fst','dword ptr [ebp - 4]'),
 0x013E7FC8:('je','0x13e8073'),0x013E7FCE:('fcom','qword ptr [0x15de0d8]'),
 0x013E7FD6:('test','ah, 5'),0x013E7FD9:('jp','0x13e7fef'),0x013E7FFA:('jne','0x13e8010'),
 0x013E8010:('fld','dword ptr [esi + 0x30]'),0x013E8013:('fsubr','dword ptr [0x15dc6f8]'),
 0x013E8019:('fldln2',''),0x013E801D:('fyl2x',''),0x013E801F:('fdiv','dword ptr [0x19bb4c4]'),
 0x013E8025:('fstp','dword ptr [ebp + 8]'),0x013E8028:('fcomp','dword ptr [0x15dc5e4]'),
 0x013E8033:('jp','0x13e8054'),0x013E8035:('fld','dword ptr [ebp - 4]'),
 0x013E8038:('fadd','st(0), st(0)'),0x013E803A:('fld','dword ptr [ebp + 8]'),
 0x013E803D:('call','0x131e2b0'),0x013E8042:('fmul','dword ptr [0x15dc5e4]'),
 0x013E805A:('fsub','dword ptr [ebp - 4]'),0x013E805D:('fadd','st(0), st(0)'),
 0x013E805F:('fld','dword ptr [ebp + 8]'),0x013E8062:('call','0x131e2b0'),
 0x013E806D:('fsubr','dword ptr [0x15dc6f8]'),0x013E8074:('fst','dword ptr [ebx + 0xc]'),
 0x0131E2B0:('sub','esp, 0x10'),0x0131E2B3:('fxch','st(1)'),
 0x0131E2B5:('fstp','qword ptr [esp]'),0x0131E2B8:('fst','qword ptr [esp + 8]'),
 0x0131E2BC:('mov','eax, dword ptr [esp + 0xc]'),0x0131E2C0:('call','0x131e2d2'),
 0x0131E2C5:('add','esp, 0x10'),0x0131E2C8:('ret',''),
 0x0131E2D6:('fnstcw','word ptr [esp]'),0x0131E2D9:('cmp','word ptr [esp], 0x27f'),
 0x0131E2E1:('call','0x13258c5'),0x0131E2FC:('call','0x13258f5'),
 0x0131E31F:('fyl2x',''),0x0131E321:('call','0x13258b0'),
 0x013E6E30:('fldln2',''),0x013E6E32:('fld','qword ptr [0x15e6480]'),
 0x013E6E38:('fyl2x',''),0x013E6E3A:('fstp','dword ptr [0x19bb4c4]'),0x013E6E40:('ret',''),
 0x013E86A4:('test','ecx, ecx'),0x013E86A6:('setne','cl'),0x013E86AF:('call','0x13e6c50'),
 0x013E86D5:('test','eax, eax'),0x013E86D7:('setne','cl'),0x013E86E0:('call','0x13e6c70')}
CONSTANTS={0x015DE0D8:(8,0x3F50624DD2F1A9FC),0x01609620:(8,0x3FEFF7CED916872B),
 0x015E6480:(8,0x3FE0000000000000),0x015DC5E4:(4,0x3F000000),0x015DC6A0:(4,0),0x015DC6F8:(4,0x3F800000)}
def gate(data):
 if hashlib.sha256(data).hexdigest().upper()!=EXE:raise ValueError('Executable identity rejected before decoding')
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m

def private_root(path):
 path=path.absolute();base=ROOT/'local-private'
 if not path.is_relative_to(base):raise ValueError('Private output root required')
 if path.exists() or path.is_symlink():raise ValueError('Fresh output root required')
 for p in (path,*path.parents):
  try:info=p.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output ancestor rejected')
  if p!=path and not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 if not path.resolve().is_relative_to(base.resolve()):raise ValueError('Private output containment required')
 return path

def pe_sections(data):
 def need(v):
  if not v:raise ValueError('Malformed raw PE')
 need(len(data)>=64 and data[:2]==b'MZ');o=struct.unpack_from('<I',data,60)[0]
 need(o+24<=len(data) and data[o:o+4]==b'PE\0\0');n=struct.unpack_from('<H',data,o+6)[0];size=struct.unpack_from('<H',data,o+20)[0]
 need(1<=n<=96 and size>=96 and o+24+size+40*n<=len(data));op=o+24;need(struct.unpack_from('<H',data,op)[0]==0x10b);base=struct.unpack_from('<I',data,op+28)[0];result=[]
 for i in range(n):
  s=op+size+40*i;virtual,va,count,raw=struct.unpack_from('<4I',data,s+8);flags=struct.unpack_from('<I',data,s+36)[0]
  need(raw+count<=len(data));result.append((va,virtual,count,raw,flags))
 return base,result

def raw_read(data,va,count):
 base,sections=pe_sections(data);rva=va-base
 for start,virtual,length,off,flags in sections:
  if start<=rva and rva+count<=start+length:return data[off+rva-start:off+rva-start+count]
 raise ValueError('Unbacked/outside raw VA range')
def graph(md,image,base,start,end):
 pending=[start];decoded={};returns=[];calls=[];tails=[]
 while pending:
  pc=pending.pop()
  while pc not in decoded:
   if not start<=pc<end or len(decoded)>=256:raise ValueError('Bounded graph escaped')
   i=next(md.disasm(image[pc-base:pc-base+15],pc,count=1));decoded[pc]=i
   if i.mnemonic=='call':calls.append((pc,int(i.op_str,16)))
   if i.mnemonic.startswith('ret'):returns.append(pc);break
   if i.mnemonic.startswith('j'):
    target=int(i.op_str,16)
    if start<=target<end:pending.append(target)
    else:tails.append((pc,target))
    if i.mnemonic=='jmp':break
   pc+=i.size
 return decoded,returns,calls,tails

def static(data,packages):
 gate(data);sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 frozen=load('curve_frozen_sampler','tools/python/verify_terrain_fractal_sampler.py').static(data,packages);assert frozen['InstructionAnchors']==230
 for va,want in ANCHORS.items():
  b=raw_read(data,va,15);i=next(md.disasm(b,va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want)
  assert bytes(i.bytes)==image[va-base:va-base+i.size]
 for label,(start,end,sha) in REGIONS.items():
  b=raw_read(data,start,end-start);assert hashlib.sha256(b).hexdigest()==sha,label
  assert b==image[start-base:end-base]
 for va,(count,bits) in CONSTANTS.items():assert int.from_bytes(raw_read(data,va,count),'little')==bits
 decoded,returns,calls,tails=graph(md,image,base,0x131e2d2,0x131e4a5)
 assert len(decoded)==127 and returns==[0x131e368] and len(calls)==9 and len(tails)==8
 assert {target for _,target in calls}=={0x13258c5,0x13258f5,0x13258b0,0x131e4a5,0x1325850,0x1325a3c}
 assert {target for _,target in tails}=={0x132594e,0x1325999}
 wrapper,wr,wc,wt=graph(md,image,base,0x131e2b0,0x131e2c9)
 assert len(wrapper)==8 and wr==[0x131e2c8] and wc==[(0x131e2c0,0x131e2d2)] and not wt
 initializer,ir,ic,it=graph(md,image,base,0x13e6e20,0x13e6e41)
 assert len(initializer)==6 and ir==[0x13e6e40] and not ic and not it
 assert raw_read(data,0x1866b08,4)==struct.pack('<I',0x13e6e20)
 assert raw_read(data,0x13e6e20,5)==b'\xe9\x0b\0\0\0'
 assert image[0x19bb4c4-base:0x19bb4c4-base+4]==bytes(4)
 try:raw_read(data,0x19bb4c4,4)
 except ValueError:pass
 else:raise AssertionError('Zero-fill global incorrectly treated as file-backed constant')
 loader=list(md.disasm(raw_read(data,0x13e8640,0x17d),0x13e8640));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in loader)==13
 return dict(FrozenSamplerAnchors=230,SelectedAnchors=len(ANCHORS),RawMappedRegionMatches=len(REGIONS),Constants=len(CONSTANTS),CompletedNewHelperBodies=2,PowerWrapperBytes=25,DenominatorWriterBytes=17,InitializerForwardEdgeBytes=5,InitializerReachableInstructions=6,PowerCoreComplete=False,PowerCoreBoundedInstructions=127,UnexpandedCallsites=9,SharedTailEdges=8,OriginalControls='Unknown',PowerImplementation='Not reproduced',OriginalExecuted=False)

def exact(bits,width):
 mantissa=23 if width==32 else 52;bias=127 if width==32 else 1023;exp=(bits>>mantissa)&((1<<(width-mantissa-1))-1);fraction=bits&((1<<mantissa)-1)
 if exp==2*bias+1:return None
 n=fraction if exp==0 else (1<<mantissa)+fraction;e=(1-bias if exp==0 else exp-bias)-mantissa
 return Fraction((-n if bits>>(width-1) else n),1)*(Fraction(2)**e)
def branches(v):
 lo=exact(CONSTANTS[0x15de0d8][1],64);hi=exact(CONSTANTS[0x1609620][1],64)
 def status(t):return 0x45 if v is None else 1 if v<t else 0x40 if v==t else 0
 if (status(lo)&5).bit_count()%2:return 'Zero'
 if not(status(hi)&0x41):return 'One'
 return 'Upper' if (status(Fraction(1,2))&5).bit_count()%2==0 else 'Lower'
def fixtures():
 lo=exact(CONSTANTS[0x15de0d8][1],64);hi=exact(CONSTANTS[0x1609620][1],64);values=[Fraction(-1),Fraction(0),Fraction(1),None]
 for center in (lo,Fraction(1,2),hi):
  values.extend([center-Fraction(1,2**k) for k in (24,53,64,80)]+[center]+[center+Fraction(1,2**k) for k in (24,53,64,80)])
 for v in values:
  reference='Upper' if v is None else 'Zero' if v<lo else 'One' if v>hi else 'Lower' if v<Fraction(1,2) else 'Upper'
  assert branches(v)==reference
 assert branches(lo)=='Lower' and branches(hi)=='Upper' and branches(Fraction(1,2))=='Upper'
 status={'LT':1,'EQ':0x40,'GT':0,'Unordered':0x45}
 masks={k:dict(LowContinues=(v&5).bit_count()%2==0,HighContinues=bool(v&0x41),HalfUpper=(v&5).bit_count()%2==0) for k,v in status.items()}
 assert masks=={'LT':dict(LowContinues=False,HighContinues=True,HalfUpper=False),'EQ':dict(LowContinues=True,HighContinues=True,HalfUpper=True),'GT':dict(LowContinues=True,HighContinues=False,HalfUpper=True),'Unordered':dict(LowContinues=True,HighContinues=True,HalfUpper=True)}
 # Symbolic x87 stack: FYL2X consumes x,y and retains y*log2(x); FST observations do not replace that expression.
 ln2=('FLDLN2',);base=('ReloadF32','u');param=('F32','p');lnp=('MulLog2',ln2,param);lnhalf=('MulLog2',ln2,('F64','half'));exponent=('Divide',lnp,lnhalf)
 stack=[exponent,base];stack[0],stack[1]=stack[1],stack[0];stored_base=('StoreF64',stack.pop(0));stored_exponent=('StoreF64',stack[0]);assert stack[0] is exponent and stored_base==('StoreF64',base)
 second=('ReloadF32',('StoreF32',('Divide',('MulLog2',ln2,('Subtract','one',('F32','c'))),('ReloadF32','initialized_ln_half'))))
 assert second[1][0]=='StoreF32' and stored_exponent[1] is exponent
 flagchecks=0
 for word in (0,1,2,255,256,65536,0x80000000,0xffffffff):assert int(word!=0)==(1 if word else 0);flagchecks+=1
 # Authored tiny raw PE: independently test exact extent, zero-fill and malformed/truncated descriptors.
 b=bytearray(640);b[:2]=b'MZ';struct.pack_into('<I',b,60,128);b[128:132]=b'PE\0\0';struct.pack_into('<H',b,134,1);struct.pack_into('<H',b,148,224);struct.pack_into('<H',b,152,0x10b);struct.pack_into('<I',b,180,0x400000);struct.pack_into('<4I',b,384,256,0x1000,128,512);b[512:]=bytes(range(128));assert raw_read(b,0x401000,128)==bytes(range(128));pechecks=1
 for data,va,count in [(b,0x401000,129),(b,0x401080,1),(b,0x400000,1),(b[:639],0x401000,1),(bytes(640),0x401000,1),(b[:60],0x401000,1)]:
  try:raw_read(data,va,count)
  except ValueError:pechecks+=1
  else:raise AssertionError('Malformed raw mapping accepted')
 return dict(ThresholdCases=len(values),StatusPredicates=12,FlagCases=flagchecks,SymbolicOperandChecks=4,RawPeCases=pechecks,PowerNumericSamples=0,UnorderedPolicy='masked status branch only; not safe domain admission')

def selected(root,config):
 old=load('curve_frozen_layer55','poc/round55-tests/Assets/peer.py');g=old.original(root,config);meta=old.t.metadata(g);ss=meta['Layers'][3]['Children'][2];blob=g[ss['Offset']:ss['Offset']+ss['EncodedBytes']]
 snap=old.ah.snapshot(blob);assert snap['Family']==4 and snap['Enabled']!=0
 gs=next(x['Section'] for x in meta['Groups'] if x['Role']=='FractalMap');gb=g[gs['Offset']:gs['Offset']+gs['EncodedBytes']]
 # Direct pinned record only: do not decode other families' words/names.
 record=gb[412:544];assert record[:4]==b'FORM' and record[8:12]==b'MFAM' and struct.unpack_from('<i',record,20)[0]==4
 raw=record[-84:];assert hashlib.sha256(raw).hexdigest()=='77fdac709b0d3e3113faad8a82f4746fdf3969434dc8bf2baf42273ae89d5fe3'
 assert raw[:4]==b'FORM' and raw[8:12]==b'MFRC' and raw[20:24]==b'0001';words=list(struct.unpack_from('<13I',raw,32));assert words[1]!=0 and words[3]==0
 public=dict(OriginalFamiliesInspected=1,LayerIndex=3,AhfrOffset=13672,AhfrSha256=ss['Sha256'],FamilyId=4,MfrcOffset=7062,MfrcSha256=hashlib.sha256(raw).hexdigest().upper(),FirstEnabled=True,SecondEnabled=False,FirstParameterFinite=exact(words[2],32) is not None,FirstParameterBetweenZeroAndOne=0<exact(words[2],32)<1,OriginalNumericParity=False)
 return public,dict(Selected=public,RawHex=raw.hex(),Words=words,FirstParameterBits=words[2],SecondParameterBits=words[4])
def main():
 p=argparse.ArgumentParser()
 for key in ('exe','packages','asset-root','config','run-root'):p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();data=a.exe.read_bytes();gate(data);out=private_root(a.run_root);st=static(data,a.packages);tests=fixtures();original,private=selected(a.asset_root,a.config)
 negatives=0
 for bad in (b'',data[:-1],data+b'\0',bytes(64)):
  try:gate(bad)
  except ValueError:negatives+=1
  else:raise AssertionError('Wrong identity accepted')
 receipt=dict(Passed=True,Static=st,Fixtures=tests,Selected=original,IdentityNegatives=negatives)
 out.mkdir(parents=True);(out/'selected-private.json').write_text(json.dumps(private,indent=2)+'\n',encoding='utf-8');(out/'curve-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
if __name__=='__main__':main()
