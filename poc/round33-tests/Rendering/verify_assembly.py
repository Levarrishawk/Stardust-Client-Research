"""SHA-gated sample-table/branch oracle. Synthetic grids; no original client execution."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
OFFSETS=[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
RINGS=[[1,2,3,4,5,6,7,8,1],[1,2,4,5,6,7,8,1],[2,4,5,6,7,8,2],[2,3,4,5,6,7,8,2],[2,3,4,5,6,8,2],[1,2,3,4,5,6,8,1],[1,2,3,4,6,8,1],[1,2,3,4,6,7,8,1],[1,2,4,6,7,8,1]]
BASE=[(.5,.5),(0,.5),(0,0),(.5,0),(1,0),(1,.5),(1,1),(.5,1),(0,1)]
AUX=[BASE,[(1-v,u)for u,v in BASE],[(1-u,1-v)for u,v in BASE],[(v,1-u)for u,v in BASE]]
def bits(f):return struct.unpack('<I',struct.pack('<f',f))[0]
def vb(v):return [bits(f)for f in v]
p=argparse.ArgumentParser()
for key in ('exe','packages','fixtures','receipt'):p.add_argument('--'+key,type=Path,required=True)
p.add_argument('--native',type=Path);a=p.parse_args()
sha=hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()
if sha!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Executable identity mismatch before PE/VA')
sys.path.insert(0,str(a.packages))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
pe=pefile.PE(str(a.exe),fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;data=pe.get_memory_mapped_image();md=Cs(CS_ARCH_X86,CS_MODE_32)
table=data[0x15e71c8-base:0x15e7258-base]
assert [struct.unpack_from('<2i',table,i*16)for i in range(9)]==OFFSETS
assert [struct.unpack_from('<2f',table,i*16+8)for i in range(9)]==BASE
branch={i.address:i for i in md.disasm(data[0x86603d-base:0x8660e1-base],0x86603d)}
def direct_pattern(n,column,row,mask):
    # Bounded interpretation of the decoded equality/test branch only, over synthetic integers.
    registers={'eax':column,'ecx':row,'edx':mask,'esi':n-1}
    memory={'dword ptr [edi + 8]':row,'dword ptr [edi + 4]':column}
    def value(op):
        if op in registers:return registers[op]
        if op=='dl':return registers['edx']&255
        if op in memory:return memory[op]
        return int(op,0)
    ip=0x86603d;zero=False
    for _ in range(128):
        if ip==0x8660e1:return registers['eax']
        instruction=branch[ip];m=instruction.mnemonic;ops=instruction.op_str.split(', ')
        following=ip+instruction.size
        if m=='mov':registers[ops[0]]=value(ops[1])
        elif m=='test':zero=(value(ops[0])&value(ops[1]))==0
        elif m=='cmp':zero=value(ops[0])==value(ops[1])
        elif m=='xor':registers[ops[0]]=value(ops[0])^value(ops[1]);zero=registers[ops[0]]==0
        elif m=='jmp':following=value(ops[0])
        elif m=='je':
            if zero:following=value(ops[0])
        elif m=='jne':
            if not zero:following=value(ops[0])
        else:raise AssertionError((hex(ip),m,ops))
        ip=following
    raise AssertionError('branch interpretation work bound')
def literal_pattern(n,c,r,m):
    candidates=[(r==0 and m&4 and c==0 and m&2,4),(r==0 and m&4 and c==n-1 and m&8,6),(r==0 and m&4,5),
        (c==0 and m&2 and r==n-1 and m&1,2),(c==0 and m&2,3),(r==n-1 and m&1 and c==n-1 and m&8,8),
        (r==n-1 and m&1,1),(c==n-1 and m&8,7),(True,0)]
    return next(pattern for condition,pattern in candidates if condition)
anchors=[
(0x861c10,0x861c2a,[(0x861c18,'mov','eax, dword ptr [edi + 0x68]'),(0x861c1e,'lea','edx, [eax + esi*2]'),(0x861c24,'lea','edx, [eax + ecx*2]')]),
(0x861d6e,0x861e25,[(0x861d6e,'mov','eax, 0x15e71c8'),(0x861d81,'mov','edi, dword ptr [eax]'),(0x861d8b,'mov','ecx, dword ptr [eax + 4]'),(0x861d94,'imul','eax, ecx'),(0x861d97,'add','eax, edi'),(0x861dce,'imul','eax, ecx'),(0x861df6,'imul','eax, ecx'),(0x861e0f,'call','0xab87b0'),(0x861e18,'call','0xaba1c0')]),
(0x861ef5,0x861fc0,[(0x861efe,'add','eax, 0x10'),(0x861f07,'cmp','eax, 0x15e7258'),(0x861fa8,'push','edx'),(0x861fa9,'push','esi'),(0x861faa,'call','0x865da0'),(0x861fbb,'call','0x865ff0')]),
(0x865da0,0x865e60,[(0x865def,'mov','dword ptr [ebx + 4], ecx'),(0x865df2,'mov','dword ptr [ebx + 8], edx'),(0x865dfd,'mov','ecx, 0x1b'),(0x865e1a,'mov','ecx, 9')]),
(0x865ff0,0x8660e1,[(0x866023,'lea','esi, [eax - 1]'),(0x866063,'test','dl, 4'),(0x86606c,'test','dl, 2'),(0x86607c,'test','dl, 8'),(0x86609c,'test','dl, 1')])]
ranges=[]
for lo,hi,sites in anchors:
    actual={i.address:(i.mnemonic,i.op_str)for i in md.disasm(data[lo-base:hi-base],lo)}
    for site,mnemonic,operand in sites:assert actual.get(site)==(mnemonic,operand),(hex(site),actual.get(site))
    ranges.append(dict(start=f'{lo:08X}',end=f'{hi:08X}',sites=len(sites),sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest()))
fixtures=[];expected=[];selections=[]
for n in (1,2,3,4):
 for row in range(n):
  for column in range(n):
   for mask in range(16):
    pattern=direct_pattern(n,column,row,mask);assert pattern==literal_pattern(n,column,row,mask)
    border=(column+row+mask)%3;width=border+2*n+1+(row%2);height=border+2*n+1+(column%2)
    indices=[(border+2*row+dy)*width+border+2*column+dx for dx,dy in OFFSETS]
    scale=2.5 if mask%2 else .5;selectors=[mask%4]if mask%3==0 else []
    f=dict(CellsPerSide=n,Column=column,Row=row,SampleBorder=border,SeamMask=mask,Width=width,Height=height,ScaleBits=bits(scale),Selectors=selectors)
    fixtures.append(f)
    coords=[(i%width,i//width)for i in indices]
    positions=[(c*.25,r*.0625+c*.125,r*.25)for c,r in coords]
    colors=[(0x80000000|c<<16|r<<8|i&255)for i,(c,r)in zip(indices,coords)]
    expected.append(dict(**f,Pattern=pattern,SampleIndices=indices,Positions=list(map(vb,positions)),Normals=[vb((0,2,0))]*9,Colors=colors,
        BaseUvs=[vb((u*scale,v*scale))for u,v in BASE],Auxiliary=[[vb(v)for v in AUX[s]]for s in selectors],
        Indices=[i for left,right in zip(RINGS[pattern],RINGS[pattern][1:])for i in (0,left,right)]))
for n in (7,2147483647):
 for c in (0,1,n-1):
  for r in (0,1,n-1):
   for mask in range(16):
    pattern=direct_pattern(n,c,r,mask);assert pattern==literal_pattern(n,c,r,mask)
    selections.append(dict(CellsPerSide=n,Column=c,Row=r,SeamMask=mask,Pattern=pattern))
private=Path(__file__).resolve().parents[3]/'local-private'
for path in (a.fixtures,a.receipt):assert path.resolve().is_relative_to(private.resolve())
a.fixtures.write_text(json.dumps(dict(Cases=fixtures,Selections=selections),indent=2)+'\n')
native=None
if a.native:
 output=json.loads(a.native.read_text());assert output['Results']==expected and output['Selections']==selections
 native={k:output[k]for k in ('Checks','Negatives','Cases','SelectorCases','SyntheticGrids')}
a.receipt.write_text(json.dumps(dict(executable_sha256=sha,sample_offsets=OFFSETS,sample_table_sha256=hashlib.sha256(table).hexdigest(),ranges=ranges,
 instruction_sites=sum(len(x[2])for x in anchors),assembly_cases=len(fixtures),selector_cases=len(selections),
 branch_sha256=hashlib.sha256(data[0x86603d-base:0x8660e1-base]).hexdigest(),native=native,original_execution=False),indent=2)+'\n')
print('PASS: 9 sample offsets; 480 assembly cases; 288 large-count selections; 27 instruction sites; bounded original-branch interpretation'+('; native exact output matches'if native else ''))
