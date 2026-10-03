"""Independent ring-edge oracle and SHA-gated original table audit; no original execution."""
import argparse, hashlib, json, struct, sys
from pathlib import Path

BASELINE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
COUNTS = [24,21,18,21,18,21,18,21,18]
# Independent ring-edge description: each directed edge forms a center triangle.
RINGS = [
 [1,2,3,4,5,6,7,8,1], [1,2,4,5,6,7,8,1], [2,4,5,6,7,8,2],
 [2,3,4,5,6,7,8,2], [2,3,4,5,6,8,2], [1,2,3,4,5,6,8,1],
 [1,2,3,4,6,8,1], [1,2,3,4,6,7,8,1], [1,2,4,6,7,8,1]]
PATTERNS = [[v for a,b in zip(ring, ring[1:]) for v in (0,a,b)] for ring in RINGS]

parser = argparse.ArgumentParser()
parser.add_argument('--exe', type=Path, required=True)
parser.add_argument('--packages', type=Path, required=True)
parser.add_argument('--fixtures', type=Path, required=True)
parser.add_argument('--receipt', type=Path, required=True)
parser.add_argument('--native', type=Path)
args = parser.parse_args()
identity = hashlib.sha256(args.exe.read_bytes()).hexdigest().upper()
if identity != BASELINE:
    raise ValueError('Executable identity mismatch before PE loading or VA use')
sys.path.insert(0, str(args.packages))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
pe = pefile.PE(str(args.exe), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = pe.get_memory_mapped_image()
md = Cs(CS_ARCH_X86, CS_MODE_32)
counts_bytes = data[0x15e7468-base:0x15e748c-base]
table_bytes = data[0x15e7490-base:0x15e7640-base]
counts = list(struct.unpack('<9I', counts_bytes))
assert counts == COUNTS
for index, pattern in enumerate(PATTERNS):
    slot = struct.unpack_from('<24H', table_bytes, index*48)
    assert list(slot[:counts[index]]) == pattern, index
    assert not any(slot[counts[index]:]), ('unused padding', index)
    assert len(pattern) == COUNTS[index] and all(0 <= i < 9 for i in pattern)
ANCHORS = [
 (0x865a80,0x865c2c,[
  (0x865ac7,'mov','ebx, 0x15e7468'),
  (0x865ad1,'mov','dword ptr [ebp - 0x14], 0x15e7490'),
  (0x865b23,'mov','di, word ptr [ecx]'),
  (0x865b26,'mov','word ptr [esi + eax*2], di'),
  (0x865c03,'add','ebx, 4'),
  (0x865c06,'add','ecx, 0x30'),
  (0x865c09,'cmp','ebx, 0x15e748c')]),
 (0x8668e0,0x866b13,[
  (0x866991,'mov','dword ptr [ebp - 8], 9'),
  (0x8669d9,'mov','ebx, dword ptr [ebx + 0x120]'),
  (0x866a04,'movzx','ecx, word ptr [ecx + eax*2]'),
  (0x866a0e,'add','ecx, edi'),
  (0x866a1e,'mov','dword ptr [edi], ecx')])]
ranges = []
for lo,hi,anchors in ANCHORS:
    actual = {i.address:(i.mnemonic,i.op_str) for i in md.disasm(data[lo-base:hi-base],lo)}
    for site,mnemonic,operand in anchors:
        assert actual.get(site) == (mnemonic,operand), (hex(site),actual.get(site))
    ranges.append(dict(start=f'{lo:08X}',end=f'{hi:08X}',sites=len(anchors),
        sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest().upper()))
fixtures = []
for index,pattern in enumerate(PATTERNS):
    rebases = [dict(VertexBase=v,DestinationVertexCount=v+9,Indices=[v+i for i in pattern])
        for v in (0,9,65536,23,2147483638)]
    fixtures.append(dict(Pattern=index,Indices=pattern,Rebases=rebases))
private = Path(__file__).resolve().parents[3]/'local-private'
for target in (args.fixtures,args.receipt):
    assert target.resolve().is_relative_to(private.resolve()), target
args.fixtures.write_text(json.dumps(fixtures,indent=2)+'\n')
native_summary = None
if args.native:
    output = json.loads(args.native.read_text())
    assert output['OriginalCoordinates'] is False
    assert output['TriangleChecks'] == 60 and output['NegativeChecks'] == 66 and output['RebaseCases'] == 45
    assert output['Results'] == [dict(Pattern=f['Pattern'],Indices=f['Indices'],Rebases=f['Rebases']) for f in fixtures]
    native_summary = {key:output[key] for key in ('Checks','TriangleChecks','NegativeChecks','RebaseCases')}
receipt = dict(executable_sha256=identity,patterns=9,active_indices=180,counts=counts,
    counts_sha256=hashlib.sha256(counts_bytes).hexdigest().upper(),
    padded_table_sha256=hashlib.sha256(table_bytes).hexdigest().upper(),
    pattern_sha256=[hashlib.sha256(struct.pack('<'+'H'*len(p),*p)).hexdigest().upper() for p in PATTERNS],
    ranges=ranges,instruction_sites=12,native=native_summary,
    original_execution=False,original_vertex_coordinate_order='Unknown',seam_mask_mapping='Deferred')
args.receipt.write_text(json.dumps(receipt,indent=2)+'\n')
print('PASS: 9 patterns; 180 active indices; 12 instruction sites; padding and 48-byte stride'+
    ('; native 45 rebases/66 negatives/60 synthetic triangles' if args.native else ''))
