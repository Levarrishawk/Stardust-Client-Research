"""Hash-gated static anchors and independent finite-domain terrain color oracle."""
import argparse, hashlib, json, struct, sys
from fractions import Fraction
from pathlib import Path
BASELINE='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
a=argparse.ArgumentParser();a.add_argument('--exe',required=True);a.add_argument('--packages',required=True);a.add_argument('--receipt',required=True);a.add_argument('--native');args=a.parse_args()
raw=Path(args.exe).read_bytes()
if hashlib.sha256(raw).hexdigest()!=BASELINE:raise SystemExit('Executable identity mismatch; no addresses consumed.')
sys.path.insert(0,args.packages)
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
p=pefile.PE(data=raw,fast_load=True);base=p.OPTIONAL_HEADER.ImageBase;data=p.get_memory_mapped_image();md=Cs(CS_ARCH_X86,CS_MODE_32)
anchors={
0xab87b4:('movzx','eax, byte ptr [ecx + 2]'),0xab87bf:('movzx','eax, byte ptr [ecx]'),
0xab87c8:('fmul','dword ptr [0x15eab04]'),0xab87e9:('fstp','dword ptr [eax]'),
0xab87eb:('fstp','dword ptr [eax + 4]'),0xab87ee:('fstp','dword ptr [eax + 8]'),0xab87f1:('fstp','dword ptr [eax + 0xc]'),
0xab88d0:('sub','edx, ecx'),0xab88de:('fmul','dword ptr [ebp + 0x14]'),0xab88e1:('call','0x131e27c'),0xab88eb:('add','bl, byte ptr [ebp - 8]'),
0xab88fd:('call','0x131e27c'),0xab891b:('call','0x131e27c'),0xab892c:('mov','byte ptr [eax + 2], bl'),0xab892f:('mov','byte ptr [eax], dl'),
0xab8612:('mov','byte ptr [0x1945aa4], al'),0xab8760:('mov','al, 0xff'),0xab8762:('mov','byte ptr [0x1945a88], al'),
0x84a4d0:('mov','al, byte ptr [0x1924b80]'),0x861e61:('cmp','byte ptr [eax + ecx], 0'),0x861e65:('push','0x3f19999a'),
0x861e6c:('push','0x1945aa4'),0x861e90:('push','0x1945a88'),0x861ec1:('call','0xab87b0'),0x861eca:('call','0xaba1c0'),
0xaba1ce:('fmul','dword ptr [0x15dd8cc]'),0xaba1d4:('call','0x131e27c'),0x131e28b:('or','ah, 0xc'),0x131e295:('fistp','qword ptr [ebp - 0xc]')}
for va,expected in anchors.items():
 i=next(md.disasm(data[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==expected,(hex(va),i.mnemonic,i.op_str)
assert data[0x15eab04-base:0x15eab08-base]==bytes.fromhex('8180803b')
assert data[0x15dd8cc-base:0x15dd8d0-base]==bytes.fromhex('00007f43')
f32=lambda v:struct.unpack('<f',struct.pack('<f',v))[0]
t=Fraction(f32(.6)); inv=Fraction(f32(1/255))
# Compare products under binary32 and exact wider arithmetic before truncation.
for d in range(-255,256):assert int(d*t)==int(f32(float(d*t)))
normalized=[f32(float(b*inv)) for b in range(256)]
for b,n in enumerate(normalized):assert int(Fraction(n)*255)==int(f32(n*255))==b
h=hashlib.sha256();size=0
normbits=[struct.unpack('<I',struct.pack('<f',n))[0] for n in normalized]
# All ordered pairs occur independently in each of three channels.
for s in range(256):
 for e in range(256):
  src=(s,255-s,s^85);end=(e,e^170,255-e)
  rgb=tuple(v+int((w-v)*t) for v,w in zip(src,end))
  word=0xff000000|rgb[0]<<16|rgb[1]<<8|rgb[2]
  block=struct.pack('<IIII',word,*(normbits[v] for v in rgb))
  h.update(block*4);size+=len(block)*4
  h.update(struct.pack('<I',0xff000000|src[0]<<16|src[1]<<8|src[2]));size+=4
for s in range(256):
 for mask in range(256):
  v=s+int(((255 if mask else 0)-s)*t)
  h.update(struct.pack('<I',0xff000000|v<<16|v<<8|v));size+=4
for corner in range(8):
 for mask in (0,1,42,255):
  for enabled in (False,True):
   rgb=tuple(255 if corner&bit else 0 for bit in (1,2,4))
   if enabled:rgb=tuple(v+int(((255 if mask else 0)-v)*t) for v in rgb)
   h.update(struct.pack('<I',0xff000000|rgb[0]<<16|rgb[1]<<8|rgb[2]));size+=4
for s in range(256):
 for mask in range(256):h.update(struct.pack('<I',0xff000000|s<<16|s<<8|s));size+=4
receipt={'sha256':BASELINE,'anchors':len(anchors),'constants':2,'orderedPairs':65536,'maskByteCases':65536,'signedDeltaPrecisionCases':511,'packPrecisionCases':256,'expectedDigest':h.hexdigest(),'expectedBytes':size}
if args.native:
 native=json.loads(Path(args.native).read_text());assert native['digest']==h.hexdigest(),native;assert native['bytes']==size
 receipt['native']=native
Path(args.receipt).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
