"""Bounded terrain startup/configuration static verifier; never runs original code."""
import argparse, hashlib, json, struct, sys
from pathlib import Path
EXPECTED='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
ap=argparse.ArgumentParser();ap.add_argument('--exe',required=True);ap.add_argument('--packages',required=True);ap.add_argument('--receipt',required=True);args=ap.parse_args()
raw=Path(args.exe).read_bytes()
if hashlib.sha256(raw).hexdigest()!=EXPECTED:raise SystemExit('Executable identity mismatch; decoding not started.')
sys.path.insert(0,args.packages)
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
p=pefile.PE(data=raw,fast_load=True);base=p.OPTIONAL_HEADER.ImageBase;data=p.get_memory_mapped_image();md=Cs(CS_ARCH_X86,CS_MODE_32)
def decode(va):return next(md.disasm(data[va-base:va-base+15],va,count=1))
anchors={
0x849de5:('call','0xb8d450'),0x849dea:('mov','dword ptr [ebx + 0x14], esi'),0x849ded:('mov','dword ptr [ebx + 0x18], 0x85ee60'),0x849df4:('mov','dword ptr [ebx], 0x15e66a8'),0x849e41:('call','0xb8d550'),
0xb8d559:('push','esi'),0xb8d55a:('push','0xb8d600'),0xb8d563:('call','0x132104a'),
0x1321073:('mov','dword ptr [esi + 0x48], eax'),0x132107c:('mov','dword ptr [esi + 0x4c], eax'),0x1321080:('push','0x13210b5'),0x132108b:('call','dword ptr [0x15dc180]'),
0x13210ff:('mov','eax, dword ptr [0x18ee4ac]'),0x1321108:('call','eax'),0x1321111:('call','dword ptr [esi + 0x48]'),0x131db40:('ret',''),
0xb8d625:('call','0xaa44e0'),0xb8d62a:('mov','edx, dword ptr [esi]'),0xb8d631:('call','dword ptr [edx + 4]'),0x84bff2:('mov','ecx, dword ptr [eax + 0x14]'),0x84bff5:('jmp','dword ptr [eax + 0x18]'),
0x85ef69:('call','0x84a530'),0x84a79e:('call','0xb65400'),0xb6571a:('call','0xb65280'),0xb65366:('call','0xb63d20'),0xb6418c:('call','dword ptr [edx + 0x10]'),0xb76618:('fstp','dword ptr [edx + ecx*4]'),0x84a7d0:('call','0x8618d0'),
0x131dd55:('call','0x401810'),0xaa7c6f:('call','0xaa7c20'),0x131dd1f:('call','0x1324772'),0x132478c:('push','0x1866b90'),0x1324791:('push','0x184d000'),0x1324796:('call','0x1324896'),0x132489b:('cmp','esi, dword ptr [esp + 0xc]'),0x13248a7:('call','eax'),0x13248a9:('add','esi, 4'),
0xab8600:('jmp','0xab8610'),0xab8610:('xor','al, al'),0xab8612:('mov','byte ptr [0x1945aa4], al'),0xab8617:('mov','byte ptr [0x1945aa5], al'),0xab861c:('mov','byte ptr [0x1945aa6], al'),
0xab8750:('jmp','0xab8760'),0xab8760:('mov','al, 0xff'),0xab8762:('mov','byte ptr [0x1945a88], al'),0xab8767:('mov','byte ptr [0x1945a89], al'),0xab876c:('mov','byte ptr [0x1945a8a], al'),
0x84a4c3:('mov','al, byte ptr [ebp + 8]'),0x84a4c6:('mov','byte ptr [0x1924b80], al'),0x84a4d0:('mov','al, byte ptr [0x1924b80]'),
0xa9feab:('call','0xaa7b00'),0xaa0729:('call','0xaa7bd0'),0xaa7b1d:('and','eax, 0xf0ff'),0xaa7b22:('or','eax, 0x3f'),0xaa7b36:('and','byte ptr [0x19447b8], 0xdf'),0xaa7b4c:('and','byte ptr [0x19447b8], 0xef'),0xaa7b62:('and','byte ptr [0x19447b8], 0xf7'),0xaa7b78:('and','byte ptr [0x19447b8], 0xfb'),0xaa7b8e:('and','byte ptr [0x19447b8], 0xfd'),0xaa7ba4:('and','byte ptr [0x19447b8], 0xfe'),0xaa7b25:('mov','word ptr [0x19447b8], ax'),0xaa7bbb:('call','0xaa7c20'),0xaa7be4:('call','0xaa7c20'),
0xaa7c23:('fldcw','word ptr [ebp + 8]'),0xaa7c33:('mov','eax, dword ptr [0x19447cc]'),0xaa7c57:('mov','dword ptr [0x19447cc], eax'),0xaa7c61:('and','eax, 0xfcff'),0xaa7c69:('mov','word ptr [0x19447b8], ax'),0x1264b99:('call','0xaa7c30'),0x1264ca9:('call','0xaa7c30')}
for va,expected in anchors.items():
 i=decode(va);assert (i.mnemonic,i.op_str)==expected,(hex(va),i.mnemonic,i.op_str)
def u32(va):return struct.unpack_from('<I',data,va-base)[0]
assert u32(0x15e66ac)==0x84bff0 and u32(0x15ed688)==0xb76540
assert u32(0x18ee4ac)==0x131db40
assert u32(0x1855ed0)==0xab8600 and u32(0x1855eec)==0xab8750
assert 0x184d000<=0x1855ed0<0x1855eec<0x1866b90
p.parse_data_directories(directories=[0,1]);imports={i.address:i.name.decode() if i.name else None for d in p.DIRECTORY_ENTRY_IMPORT for i in d.imports}
assert imports[0x15dc180]=='CreateThread'
assert imports[0x15dc1cc]=='TlsSetValue'
assert all(base+s.address!=0x84a4c0 for s in p.DIRECTORY_ENTRY_EXPORT.symbols)
# Exact literal-reference occurrence inventories. These are not general alias proofs.
expected_refs={
0x1945aa4:[0x51bac2,0x573e5d,0x574492,0x586e5d,0x848a28,0x84b794,0x84b7d4,0x85143c,0x85145b,0x861e6d,0xab8613,0xb6f4c9,0xb6f4e9,0xb8aca6],
0x1945aa5:[0xab8618],
0x1945aa6:[0x51bac9,0x573e6f,0x5744a4,0x586e6f,0x84b79f,0x84b7df,0x85144b,0x85146a,0xab861d,0xb6f4d4,0xb6f4f4,0xb8acac],
0x1945a88:[0x5b68e3,0x8490d3,0x851406,0x85141d,0x861e91,0xab8763,0xb65470,0xf03e70,0xf3cd01,0x107981a,0x13a27c2],
0x1945a89:[0xab8768],0x1945a8a:[0x8490de,0x851411,0x85142c,0xab876d,0xb6547c],
0x1924b80:[0x84a4c7,0x84a4d1],0x18ee4ac:[0x1321100,0x1406abb],0xab8600:[0x1855ed0],0xab8750:[0x1855eec],0x84a4c0:[]}
endpoint_sites={
0x1945aa4:[0x51bac1,0x573e5b,0x574490,0x586e5b,0x848a27,0x84b791,0x84b7d1,0x851439,0x851458,0x861e6c,0xab8612,0xb6f4c6,0xb6f4e6,0xb8aca4],
0x1945aa5:[0xab8617],
0x1945aa6:[0x51bac6,0x573e6e,0x5744a3,0x586e6e,0x84b79d,0x84b7dd,0x851449,0x851468,0xab861c,0xb6f4d2,0xb6f4f2,0xb8acaa],
0x1945a88:[0x5b68e2,0x8490d0,0x851403,0x85141a,0x861e90,0xab8762,0xb6546d,0xf03e6f,0xf3cd00,0x1079819,0x13a27c1],
0x1945a89:[0xab8767],0x1945a8a:[0x8490dc,0x85140f,0x85142a,0xab876c,0xb6547a]}
writes=[]
for va,sites in endpoint_sites.items():
 assert len(sites)==len(expected_refs[va])
 for site in sites:
  i=decode(site);assert i.mnemonic in ('mov','movzx','push') and hex(va) in i.op_str
  operand=i.op_str.split(', ')[0]
  if operand.startswith(('byte ptr [','word ptr [','dword ptr [')):writes.append(site)
assert sorted(writes)==[0xab8612,0xab8617,0xab861c,0xab8762,0xab8767,0xab876c]
for va,expected in {0x848a31:('call','0xab88b0'),0x5b68e7:('call','0x5b5690'),0xf03e75:('call','0x5b7210'),0xf3cd08:('call','0x5b7210'),0x1079821:('call','0x5b7210'),0x13a27c7:('call','0x5b7210'),0x5b722e:('call','0x5b5690'),0x5b5696:('movzx','ecx, byte ptr [eax + 1]'),0x5b569c:('mov','dh, byte ptr [eax]'),0x5b569e:('movzx','eax, byte ptr [eax + 2]')}.items():
 i=decode(va);assert (i.mnemonic,i.op_str)==expected

def refs(va):
 out=[];pos=0;needle=struct.pack('<I',va)
 while (pos:=data.find(needle,pos))!=-1:out.append(base+pos);pos+=1
 return out
for va,sites in expected_refs.items():assert refs(va)==sites,(hex(va),refs(va))
# Scan executable byte encodings for direct calls, then verify every retained candidate instruction.
def calls(target):
 out=[]
 for sec in p.sections:
  if sec.Characteristics&0x20000000:
   block=sec.get_data();off=0
   while (off:=block.find(b'\xe8',off))!=-1:
    if off+5<=len(block):
     site=base+sec.VirtualAddress+off
     if site+5+struct.unpack_from('<i',block,off+1)[0]==target:out.append(site)
    off+=1
 return out
expected_calls={0x84a4c0:[],0xaa7b00:[0xa9feab],0xaa7bd0:[0xaa0729],0xaa7c30:[0x1264b99,0x1264ca9],0xaa7c80:[]}
for target,sites in expected_calls.items():assert calls(target)==sites,(hex(target),calls(target))
# No local x87 control installation in these exact aligned bodies. Callees/OS remain separate.
ranges=[(0xb8d550,0xb8d570),(0x132104a,0x1321132),(0xb8d600,0xb8d647),(0xaa44e0,0xaa4646),(0x84bff0,0x84bff8),(0x85ee60,0x85efc9),(0x84a530,0x84a840),(0xb65400,0xb65728),(0xb65280,0xb653b3),(0xb76540,0xb76622)]
for lo,hi in ranges:
 ins=list(md.disasm(data[lo-base:hi-base],lo));assert ins and ins[-1].address+ins[-1].size==hi,(hex(lo),hex(hi))
 assert not any(i.mnemonic in ('fldcw','fninit','finit','ldmxcsr') for i in ins)
 assert not any(i.mnemonic=='call' and i.op_str in ('0xaa7b00','0xaa7bd0','0xaa7c20','0xaa7c30','0xaa7c80') for i in ins)
# PE zero-filled enable byte: beyond raw .data, within its virtual extent.
section=next(s for s in p.sections if base+s.VirtualAddress<=0x1924b80<base+s.VirtualAddress+s.Misc_VirtualSize)
assert 0x1924b80-(base+section.VirtualAddress)>=section.SizeOfRawData and data[0x1924b80-base]==0
# Independent bit oracle for installer prefix and all six config-unmask combinations.
and_mask=int(decode(0xaa7b1d).op_str.split(', ')[1],16);or_mask=int(decode(0xaa7b22).op_str.split(', ')[1],16)
for old in range(65536):
 for unmask in range(64):
  observed=((old&and_mask)|or_mask)&~unmask
  reference=(old//4096)*4096+(old&192)+(63^unmask)
  assert observed==reference
# Small decoded integer interpreter for the precision setter, synthetic state only.
setter={i.address:i for i in md.disasm(data[0xaa7c30-base:0xaa7c79-base],0xaa7c30)}
def interpreted(old,prior,requested):
 regs={'eax':0,'ecx':0};mem={0x19447cc:prior,0x19447b8:old};pc=0xaa7c33;zero=False;steps=0
 while pc!=0xaa7c6f:
  steps+=1;assert steps<40
  i=setter[pc];pc+=i.size;ops=i.op_str.split(', ');a=ops[0]
  def value(op):
   if op in regs:return regs[op]
   if op=='ax':return regs['eax']&65535
   if op=='dword ptr [ebp + 8]':return requested
   if op.startswith('dword ptr [0x'):return mem[int(op[11:-1],16)]
   return int(op,0)
  if i.mnemonic=='mov':
   v=value(ops[1])
   if a in regs:regs[a]=v
   else:mem[int(a[a.index('0x'):-1],16)]=v&65535 if a.startswith('word ') else v
  elif i.mnemonic in ('sub','xor','and','or'):
   x,y=regs[a],value(ops[1]);regs[a]={'sub':lambda:x-y,'xor':lambda:x^y,'and':lambda:x&y,'or':lambda:x|y}[i.mnemonic]()&0xffffffff;zero=regs[a]==0
  elif i.mnemonic=='dec':regs[a]=(regs[a]-1)&0xffffffff;zero=regs[a]==0
  elif i.mnemonic=='jmp' or (i.mnemonic=='je' and zero) or (i.mnemonic=='jne' and not zero):pc=int(a,16)
  elif i.mnemonic in ('je','jne','push'):pass
  else:raise AssertionError((i.mnemonic,i.op_str))
 return mem[0x19447b8],mem[0x19447cc]
precision_cases=0
for old in (0,0x3f,0x27f,0x37f,0xffff):
 for prior in (0,1,2,3,0xffffffff):
  for requested in (0,1,2,3,0xffffffff):
   result=interpreted(old,prior,requested)
   assert result==((old&0xfcff)|{0:0,1:0x200,2:0x300}.get(prior,0),requested)
   precision_cases+=1
receipt={'sha256':EXPECTED,'anchors':len(anchors),'dataPointers':5,'literalReferenceOccurrences':sum(map(len,expected_refs.values())),'literalReferenceKeys':len(expected_refs),'endpointInstructionSites':sum(map(len,endpoint_sites.values())),'endpointLiteralWriteSites':len(writes),'endpointPassedPointerAnchors':10,'directCallTargets':len(expected_calls),'localBodyNoInstallRanges':len(ranges),'installerSyntheticCases':65536*64,'precisionSetterSyntheticCases':precision_cases,'profileApiAdded':False,'limits':['OS-initial worker CW and transitive/virtual callback mutation unproven.','Literal/direct-reference inventories exclude computed aliases and external mutation.'],'refs':{f'{v:08X}':[f'{s:08X}' for s in ss] for v,ss in expected_refs.items()}}
Path(args.receipt).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:v for k,v in receipt.items() if k!='refs'}))

