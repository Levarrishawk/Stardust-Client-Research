"""SHA-gated static semantics and independently authored synthetic geometry oracle."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
BASE=[(.5,.5),(0,.5),(0,0),(.5,0),(1,0),(1,.5),(1,1),(.5,1),(0,1)]
RINGS=[[1,2,3,4,5,6,7,8,1],[1,2,4,5,6,7,8,1],[2,4,5,6,7,8,2],[2,3,4,5,6,7,8,2],[2,3,4,5,6,8,2],[1,2,3,4,5,6,8,1],[1,2,3,4,6,8,1],[1,2,3,4,6,7,8,1],[1,2,4,6,7,8,1]]
AUX=[BASE,[(1-v,u)for u,v in BASE],[(1-u,1-v)for u,v in BASE],[(v,1-u)for u,v in BASE]]
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def single(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def vector_bits(v):return [bits(x)for x in v]
p=argparse.ArgumentParser()
for name in ('exe','packages','fixtures','receipt'):p.add_argument('--'+name,type=Path,required=True)
p.add_argument('--native',type=Path);a=p.parse_args()
sha=hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()
if sha!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Executable identity mismatch before PE/VA')
sys.path.insert(0,str(a.packages))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
pe=pefile.PE(str(a.exe),fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;data=pe.get_memory_mapped_image();md=Cs(CS_ARCH_X86,CS_MODE_32)
actual_base=[struct.unpack_from('<2f',data,0x15e69d0-base+i*16)for i in range(9)]
assert actual_base==BASE
aux_raw=data[0x15e7640-base:0x15e7760-base]
assert list(struct.unpack('<72f',aux_raw))==[c for uv in AUX for point in uv for c in point]
anchors=[
(0x84e270,0x84e485,[
(0x84e30d,'mov','edi, dword ptr [ecx + 0x10c]'),(0x84e313,'lea','eax, [ecx + 0x78]'),(0x84e316,'lea','edx, [ecx + 0xc]'),
(0x84e326,'mov','ebx, 0x15e69d0'),(0x84e376,'mov','edx, dword ptr [ecx + edi*4 + 0xe4]'),
(0x84e38a,'fmul','dword ptr [ebx + 4]'),(0x84e390,'fmul','dword ptr [ebx]'),
(0x84e3a9,'and','eax, 0xf'),(0x84e3b1,'add','ecx, 0x110'),(0x84e3cf,'call','0x866db0'),
(0x84e41c,'add','ebx, 0x10'),(0x84e423,'cmp','ebx, 0x15e6a60'),
(0x84e446,'mov','di, word ptr [eax]'),(0x84e449,'add','di, word ptr [ebp - 0x18]'),(0x84e465,'add','eax, 9')]),
(0x865da0,0x865e60,[(0x865dfd,'mov','ecx, 0x1b'),(0x865e07,'lea','edi, [ebx + 0x78]'),(0x865e14,'lea','edi, [ebx + 0xe4]'),(0x865e1a,'mov','ecx, 9'),(0x865e2d,'mov','dword ptr [ebx + 0x10c], ecx'),(0x865e35,'lea','edx, [ebx + 0x110]')]),
(0x866db0,0x866ddb,[(0x866db9,'lea','edx, [ecx + eax*8]'),(0x866dbc,'add','eax, edx'),(0x866dbe,'mov','ecx, dword ptr [eax*8 + 0x15e7640]'),(0x866dd1,'mov','eax, dword ptr [eax + 4]')]),
(0xaba1c0,0xaba227,[(0xaba1e4,'mov','bh, al'),(0xaba1f4,'mov','bl, al'),(0xaba1f6,'shl','ebx, 8'),(0xaba20c,'shl','ebx, 8'),(0xaba21e,'mov','dword ptr [eax], ebx')])]
ranges=[]
for lo,hi,sites in anchors:
 actual={i.address:(i.mnemonic,i.op_str)for i in md.disasm(data[lo-base:hi-base],lo)}
 for site,mnemonic,operand in sites:assert actual.get(site)==(mnemonic,operand),(hex(site),actual.get(site))
 ranges.append(dict(start=f'{lo:08X}',end=f'{hi:08X}',sites=len(sites),sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest()))
fixtures=[];expected=[]
colors=[0xffff0000,0x80402010,0,0xffffffff,0x01020304,0xff0000ff,0xff00ff00,0x12345678,0xffeeddcc]
for pattern in range(9):
 for mode,scale in enumerate((1.,2.5,65536.)):
  positions=[(2*u-1,single(i*.125+mode*.25),1-2*v)for i,(u,v)in enumerate(BASE)]
  normals=[(0,2 if mode==1 else 1,0)]*9
  selectors=[] if mode==0 else [pattern%4] if mode==1 else [(pattern+i)%4 for i in range(3)]
  name=f'synthetic-{pattern}-{mode}'
  fixtures.append(dict(Name=name,Pattern=pattern,ScaleBits=bits(scale),Positions=list(map(vector_bits,positions)),Normals=list(map(vector_bits,normals)),Colors=colors,Selectors=selectors))
  indices=[x for left,right in zip(RINGS[pattern],RINGS[pattern][1:])for x in (0,left,right)]
  expected.append(dict(Name=name,Pattern=pattern,ScaleBits=bits(scale),Positions=list(map(vector_bits,positions)),Normals=list(map(vector_bits,normals)),Colors=colors,
   Rgba=[[bits(single((word>>shift&255)/255))for shift in (16,8,0,24)]for word in colors],
   BaseUvs=[[bits(single(u*scale)),bits(single(v*scale))]for u,v in BASE],Selectors=selectors,
   Auxiliary=[[vector_bits(v)for v in AUX[s]]for s in selectors],Indices=indices,
   Minimum=vector_bits([min(v[i]for v in positions)for i in range(3)]),Maximum=vector_bits([max(v[i]for v in positions)for i in range(3)]),TriangleCount=len(indices)//3))
private=Path(__file__).resolve().parents[3]/'local-private'
for target in (a.fixtures,a.receipt):assert target.resolve().is_relative_to(private.resolve())
a.fixtures.write_text(json.dumps(fixtures,indent=2)+'\n')
native=None
if a.native:
 output=json.loads(a.native.read_text());assert output['SyntheticHeights']is True and output['Results']==expected
 native={key:output[key]for key in ('Checks','Negatives','Cases')}
a.receipt.write_text(json.dumps(dict(executable_sha256=sha,base_uv_points=9,auxiliary_selectors=4,ranges=ranges,instruction_sites=sum(len(x[2])for x in anchors),
 auxiliary_table_sha256=hashlib.sha256(aux_raw).hexdigest(),native=native,original_execution=False,synthetic_heights=True),indent=2)+'\n')
print('PASS: 9 base UVs; 4 auxiliary sets; 30 semantic instruction anchors; 27 synthetic geometry cases'+('; native exact bits/indices/colors/UV/ownership verified'if native else ''))
