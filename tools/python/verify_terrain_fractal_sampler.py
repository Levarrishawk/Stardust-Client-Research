"""Static SHA-gated sampler evidence and conditional finite research vectors; no production noise sampler."""
import argparse,hashlib,importlib.util,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
EPSILON=0x3727c5ac
ANCHORS={
 0x00B8B2F7: ('call', '0x13e7c00'),
 0x00B75AF3: ('mov', 'ebx, dword ptr [ebp + 0xc]'),
 0x00B75AF6: ('mov', 'ecx, dword ptr [ebp + 8]'),
 0x00B75AF9: ('push', 'ebx'),
 0x00B75AFA: ('push', 'ecx'),
 0x00B75AFB: ('mov', 'ecx, dword ptr [esi + 0x14]'),
 0x00B75AFE: ('call', '0xb757b0'),
 0x00B75B05: ('call', '0x13e6ad0'),
 0x00B768E2: ('push', 'edi'),
 0x00B768E3: ('push', 'ebx'),
 0x00B768E4: ('push', 'ecx'),
 0x00B768E8: ('push', 'edx'),
 0x00B768E9: ('call', '0x13e7c00'),
 0x00B768EE: ('fmul', 'dword ptr [esi + 0x20]'),
 0x00B7695F: ('fstp', 'dword ptr [edx + ecx*4]'),
 0x00B77C13: ('call', '0x13e7c00'),
 0x00B77C18: ('fmul', 'dword ptr [esi + 0x28]'),
 0x00B77C24: ('fstp', 'dword ptr [ebp + 0x18]'),
 0x013E650B: ('call', '0xb59ef0'),
 0x013E6523: ('mov', 'dword ptr [ebp - 8], eax'),
 0x013E6605: ('mov', 'ecx, 0x102'),
 0x013E663D: ('jne', '0x13e6610'),
 0x013E66F8: ('fadd', 'dword ptr [0x15e4f74]'),
 0x013E6704: ('call', '0x131e27c'),
 0x013E6732: ('sub', 'ecx, eax'),
 0x013E673C: ('and', 'eax, 0xff'),
 0x013E674E: ('fstp', 'dword ptr [ebp + 8]'),
 0x013E6767: ('call', '0x131e27c'),
 0x013E6795: ('mov', 'edx, dword ptr [esi + edi*4 + 0x510]'),
 0x013E67B8: ('mov', 'edi, dword ptr [esi + edi*4 + 0x510]'),
 0x013E67C2: ('lea', 'edi, [esi + edi*8 + 0xd18]'),
 0x013E67DC: ('fsubr', 'dword ptr [0x15dd850]'),
 0x013E67E2: ('fmul', 'dword ptr [ebp + 8]'),
 0x013E67E5: ('fmul', 'dword ptr [ebp + 8]'),
 0x013E6AD4: ('mov', 'ebx, dword ptr [ebp + 0xc]'),
 0x013E6ADA: ('mov', 'eax, dword ptr [esi + 0x2574]'),
 0x013E6AE1: ('mov', 'edi, dword ptr [ebp + 8]'),
 0x013E6AE4: ('cmp', 'edi, eax'),
 0x013E6AE6: ('jg', '0x13e6af0'),
 0x013E6AE8: ('cmp', 'ebx, dword ptr [esi + 0x2578]'),
 0x013E6AEE: ('jle', '0x13e6b3d'),
 0x013E6AFB: ('call', '0xac1660'),
 0x013E6B0F: ('je', '0x13e6b3d'),
 0x013E6B13: ('je', '0x13e6b3d'),
 0x013E6B15: ('mov', 'dword ptr [esi + 0x2574], edi'),
 0x013E6B1B: ('imul', 'edi, ebx'),
 0x013E6B1E: ('shl', 'edi, 4'),
 0x013E6B22: ('mov', 'dword ptr [esi + 0x2578], ebx'),
 0x013E6B28: ('call', '0xac15e0'),
 0x013E6B32: ('mov', 'dword ptr [esi + 0x257c], eax'),
 0x013E6B38: ('call', '0x13e6b50'),
 0x013E6B53: ('mov', 'edi, dword ptr [eax + 0x257c]'),
 0x013E6B59: ('test', 'edi, edi'),
 0x013E6B5B: ('je', '0x13e6b7d'),
 0x013E6B5D: ('mov', 'ecx, dword ptr [eax + 0x2578]'),
 0x013E6B63: ('imul', 'ecx, dword ptr [eax + 0x2574]'),
 0x013E6B6A: ('shl', 'ecx, 4'),
 0x013E6B74: ('rep stosd', 'dword ptr es:[edi], eax'),
 0x013E6B7B: ('rep stosb', 'byte ptr es:[edi], al'),
 0x013E6B89: ('cmp', 'dword ptr [esi], eax'),
 0x013E6B8B: ('je', '0x13e6b9f'),
 0x013E6B93: ('call', '0x13e64d0'),
 0x013E6B9A: ('call', '0x13e6b50'),
 0x013E6BB9: ('mov', 'dword ptr [ecx + 4], eax'),
 0x013E6BBC: ('mov', 'dword ptr [ecx + 8], edx'),
 0x013E6BBF: ('call', '0x13e6b50'),
 0x013E6BD9: ('mov', 'dword ptr [ecx + 0xc], eax'),
 0x013E6BDC: ('mov', 'dword ptr [ecx + 0x10], edx'),
 0x013E6BDF: ('call', '0x13e6b50'),
 0x013E6BF6: ('mov', 'dword ptr [ecx + 0x14], eax'),
 0x013E6BF9: ('call', '0x13e6b50'),
 0x013E6C16: ('mov', 'dword ptr [ecx + 0x18], eax'),
 0x013E6C19: ('call', '0x13e6b50'),
 0x013E6C39: ('mov', 'dword ptr [esi + 0x1c], eax'),
 0x013E6C3C: ('call', '0x13e6dd0'),
 0x013E6C43: ('call', '0x13e6b50'),
 0x013E6C59: ('mov', 'byte ptr [ecx + 0x24], al'),
 0x013E6C5C: ('mov', 'dword ptr [ecx + 0x28], edx'),
 0x013E6C5F: ('call', '0x13e6b50'),
 0x013E6C79: ('mov', 'byte ptr [ecx + 0x2c], al'),
 0x013E6C7C: ('mov', 'dword ptr [ecx + 0x30], edx'),
 0x013E6C7F: ('call', '0x13e6b50'),
 0x013E6C96: ('mov', 'byte ptr [ecx + 0x34], al'),
 0x013E6C99: ('call', '0x13e6b50'),
 0x013E6CB6: ('cmp', 'eax, 5'),
 0x013E6CB9: ('mov', 'dword ptr [ecx + 0x38], eax'),
 0x013E6CBC: ('ja', '0x13e6d2f'),
 0x013E6CBE: ('jmp', 'dword ptr [eax*4 + 0x13e6d38]'),
 0x013E6CD3: ('call', '0x13e6b50'),
 0x013E6CEA: ('call', '0x13e6b50'),
 0x013E6D01: ('call', '0x13e6b50'),
 0x013E6D18: ('call', '0x13e6b50'),
 0x013E6D2F: ('call', '0x13e6b50'),
 0x013E6DC1: ('jmp', '0x13e6b50'),
 0x013E6DD4: ('mov', 'eax, dword ptr [ecx + 0x14]'),
 0x013E6DD7: ('test', 'eax, eax'),
 0x013E6DD9: ('mov', 'dword ptr [ecx + 0x20], 0'),
 0x013E6DF8: ('fadd', 'st(2)'),
 0x013E6DFE: ('fmulp', 'st(1)'),
 0x013E6E06: ('fstp', 'dword ptr [ecx + 0x20]'),
 0x013E6E0C: ('fdivr', 'dword ptr [0x15dc6f8]'),
 0x013E6E12: ('fstp', 'dword ptr [ecx + 0x20]'),
 0x013E7C0A: ('mov', 'ebx, dword ptr [esi + 0x2574]'),
 0x013E7C10: ('imul', 'ebx, dword ptr [ebp + 0x14]'),
 0x013E7C14: ('mov', 'ecx, dword ptr [esi + 0x257c]'),
 0x013E7C1B: ('add', 'ebx, dword ptr [ebp + 0x10]'),
 0x013E7C1E: ('shl', 'ebx, 4'),
 0x013E7C21: ('mov', 'al, byte ptr [ebx + ecx]'),
 0x013E7C26: ('test', 'al, al'),
 0x013E7C2B: ('je', '0x13e7c91'),
 0x013E7C2D: ('fld', 'dword ptr [ebx + 4]'),
 0x013E7C30: ('fld', 'dword ptr [0x15e128c]'),
 0x013E7C36: ('fsubr', 'st(1)'),
 0x013E7C38: ('fcomp', 'dword ptr [ebp + 8]'),
 0x013E7C3D: ('test', 'ah, 0x41'),
 0x013E7C40: ('jp', '0x13e7c8f'),
 0x013E7C42: ('fld', 'dword ptr [0x15e128c]'),
 0x013E7C48: ('fadd', 'st(1)'),
 0x013E7C4A: ('fld', 'dword ptr [ebp + 8]'),
 0x013E7C4D: ('fcompp', ''),
 0x013E7C53: ('test', 'ah, 0x41'),
 0x013E7C56: ('jp', '0x13e7c91'),
 0x013E7C58: ('fld', 'dword ptr [ebx + 8]'),
 0x013E7C61: ('fsubr', 'st(1)'),
 0x013E7C63: ('fcomp', 'dword ptr [ebp + 0xc]'),
 0x013E7C68: ('test', 'ah, 0x41'),
 0x013E7C6B: ('jp', '0x13e7c8f'),
 0x013E7C73: ('fadd', 'st(1)'),
 0x013E7C78: ('fcompp', ''),
 0x013E7C7E: ('test', 'ah, 0x41'),
 0x013E7C81: ('jp', '0x13e7c91'),
 0x013E7C83: ('fld', 'dword ptr [ebx + 0xc]'),
 0x013E7C8C: ('ret', '0x10'),
 0x013E7CA3: ('mov', 'byte ptr [ebx], 1'),
 0x013E7CA6: ('mov', 'dword ptr [ebx + 4], eax'),
 0x013E7CA9: ('mov', 'dword ptr [ebx + 8], ecx'),
 0x013E7CAC: ('fmul', 'dword ptr [esi + 4]'),
 0x013E7CAF: ('mov', 'eax, dword ptr [esi + 0x38]'),
 0x013E7CB2: ('cmp', 'eax, 5'),
 0x013E7CB5: ('fstp', 'dword ptr [ebp + 8]'),
 0x013E7CC9: ('fmul', 'dword ptr [esi + 8]'),
 0x013E7CCC: ('fstp', 'dword ptr [ebp + 0xc]'),
 0x013E7CCF: ('ja', '0x13e7f94'),
 0x013E7CD5: ('jmp', 'dword ptr [eax*4 + 0x13e8080]'),
 0x013E7CDC: ('mov', 'edi, dword ptr [esi + 0x14]'),
 0x013E7CEA: ('jle', '0x13e7d3f'),
 0x013E7CF2: ('fadd', 'dword ptr [esi + 0x10]'),
 0x013E7CF5: ('fstp', 'dword ptr [ebp + 0xc]'),
 0x013E7CFB: ('fadd', 'dword ptr [esi + 0xc]'),
 0x013E7CFE: ('fstp', 'dword ptr [ebp - 4]'),
 0x013E7D07: ('fmul', 'dword ptr [ebp + 0x14]'),
 0x013E7D0C: ('fstp', 'dword ptr [esp + 4]'),
 0x013E7D13: ('fmul', 'dword ptr [ebp + 0x14]'),
 0x013E7D16: ('fstp', 'dword ptr [esp]'),
 0x013E7D19: ('call', '0x13e66f0'),
 0x013E7D1F: ('fmul', 'dword ptr [ebp + 0x10]'),
 0x013E7D22: ('fadd', 'dword ptr [ebp - 8]'),
 0x013E7D25: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7D2B: ('fmul', 'dword ptr [esi + 0x18]'),
 0x013E7D2E: ('fstp', 'dword ptr [ebp + 0x14]'),
 0x013E7D34: ('fmul', 'dword ptr [esi + 0x1c]'),
 0x013E7D37: ('fstp', 'dword ptr [ebp + 0x10]'),
 0x013E7D3A: ('jne', '0x13e7d01'),
 0x013E7D42: ('test', 'al, al'),
 0x013E7D49: ('fadd', 'dword ptr [ebp + 8]'),
 0x013E7D4C: ('fsin', ''),
 0x013E7D4E: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7D54: ('fmul', 'dword ptr [esi + 0x20]'),
 0x013E7D57: ('fadd', 'dword ptr [0x15dc6f8]'),
 0x013E7D5D: ('fmul', 'dword ptr [0x15dc5e4]'),
 0x013E7DA9: ('call', '0x13e66f0'),
 0x013E7DAF: ('fabs', ''),
 0x013E7DB1: ('fsubr', 'dword ptr [0x15dc6f8]'),
 0x013E7DB7: ('fmul', 'dword ptr [ebp + 0x10]'),
 0x013E7DBA: ('fadd', 'dword ptr [ebp - 8]'),
 0x013E7DBD: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7E1D: ('call', '0x13e66f0'),
 0x013E7E23: ('fabs', ''),
 0x013E7E25: ('fmul', 'dword ptr [ebp + 0x10]'),
 0x013E7E28: ('fadd', 'dword ptr [ebp - 8]'),
 0x013E7E2B: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7E8B: ('call', '0x13e66f0'),
 0x013E7E90: ('fcom', 'dword ptr [0x15dc6a0]'),
 0x013E7E9B: ('jp', '0x13e7ea7'),
 0x013E7EA7: ('fcom', 'dword ptr [0x15dc6f8]'),
 0x013E7EB2: ('jne', '0x13e7ebc'),
 0x013E7EC3: ('fsub', 'st(1)'),
 0x013E7EC5: ('fmul', 'dword ptr [ebp + 0x10]'),
 0x013E7EC8: ('fadd', 'dword ptr [ebp - 8]'),
 0x013E7ECB: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7F2A: ('call', '0x13e66f0'),
 0x013E7F2F: ('fcom', 'dword ptr [0x15dc6a0]'),
 0x013E7F3A: ('jp', '0x13e7f46'),
 0x013E7F46: ('fcom', 'dword ptr [0x15dc6f8]'),
 0x013E7F51: ('jne', '0x13e7f5b'),
 0x013E7F5C: ('fmul', 'dword ptr [ebp + 0x10]'),
 0x013E7F5F: ('fadd', 'dword ptr [ebp - 8]'),
 0x013E7F62: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7F7C: ('test', 'al, al'),
 0x013E7F83: ('fadd', 'dword ptr [ebp + 8]'),
 0x013E7F86: ('fsin', ''),
 0x013E7F88: ('fstp', 'dword ptr [ebp - 8]'),
 0x013E7F8E: ('fmul', 'dword ptr [esi + 0x20]'),
 0x013E7F91: ('fst', 'dword ptr [ebp - 4]'),
 0x013E7F99: ('je', '0x13e7fc3'),
 0x013E7FA0: ('fld', 'dword ptr [esi + 0x28]'),
 0x013E7FA7: ('fyl2x', ''),
 0x013E7FA9: ('fst', 'dword ptr [ebp + 8]'),
 0x013E7FB4: ('fyl2x', ''),
 0x013E7FB6: ('fst', 'dword ptr [ebp + 8]'),
 0x013E7FB9: ('fdivp', 'st(1)'),
 0x013E7FBB: ('call', '0x131e2b0'),
 0x013E7FC0: ('fst', 'dword ptr [ebp - 4]'),
 0x013E7FC8: ('je', '0x13e8073'),
 0x013E7FCE: ('fcom', 'qword ptr [0x15de0d8]'),
 0x013E7FD9: ('jp', '0x13e7fef'),
 0x013E7FE5: ('fst', 'dword ptr [ebx + 0xc]'),
 0x013E7FEC: ('ret', '0x10'),
 0x013E7FEF: ('fcom', 'qword ptr [0x1609620]'),
 0x013E7FFA: ('jne', '0x13e8010'),
 0x013E8006: ('fst', 'dword ptr [ebx + 0xc]'),
 0x013E801D: ('fyl2x', ''),
 0x013E801F: ('fdiv', 'dword ptr [0x19bb4c4]'),
 0x013E8025: ('fstp', 'dword ptr [ebp + 8]'),
 0x013E803D: ('call', '0x131e2b0'),
 0x013E804A: ('fst', 'dword ptr [ebx + 0xc]'),
 0x013E8062: ('call', '0x131e2b0'),
 0x013E806D: ('fsubr', 'dword ptr [0x15dc6f8]'),
 0x013E8074: ('fst', 'dword ptr [ebx + 0xc]'),
 0x013E807C: ('ret', '0x10'),
}
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def identity(data):
 if hashlib.sha256(data).hexdigest().upper()!=EXE:raise ValueError('Executable identity mismatch; rejected before decoding')

def static(data,packages):
 identity(data);sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);im=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,want in ANCHORS.items():
  i=next(md.disasm(im[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want,(i.mnemonic,i.op_str))
 assert struct.unpack_from('<I',im,0x15e128c-base)[0]==EPSILON
 assert struct.unpack_from('<Q',im,0x15de0d8-base)[0]==0x3f50624dd2f1a9fc
 assert struct.unpack_from('<Q',im,0x1609620-base)[0]==0x3feff7ced916872b
 assert struct.unpack_from('<Q',im,0x15e6480-base)[0]==0x3fe0000000000000
 assert struct.unpack_from('<6I',im,0x13e8080-base)==(0x13e7cdc,0x13e7cdc,0x13e7d68,0x13e7ddc,0x13e7e4a,0x13e7ee9)
 ins=list(md.disasm(im[0x13e7c00-base:0x13e807f-base],0x13e7c00))
 assert [i.address for i in ins if i.mnemonic=='call' and i.op_str=='0x13e66f0']==[0x13e7d19,0x13e7da9,0x13e7e1d,0x13e7e8b,0x13e7f2a]
 assert [i.address for i in ins if i.mnemonic=='fst' and i.op_str=='dword ptr [ebx + 0xc]']==[0x13e7fe5,0x13e8006,0x13e804a,0x13e8074]
 assert not any(i.mnemonic in ('fldcw','fnstcw','fstcw') for i in ins)
 # TEST AH,41 / JP: finite LT and EQ proceed; GT and unordered miss.
 status={'LT':0x01,'EQ':0x40,'GT':0x00,'Unordered':0x45}
 assert {k:((v&0x41).bit_count()%2==0) for k,v in status.items()}=={'LT':False,'EQ':False,'GT':True,'Unordered':True}
 negatives=0
 for off in (0,1,len(data)//2,len(data)-1):
  changed=bytearray(data);changed[off]^=1
  try:identity(changed)
  except ValueError:negatives+=1
  else:raise AssertionError('Identity accepted')
 for changed in (b'',data[:-1],data+b'\0',bytes(64)):
  try:identity(changed)
  except ValueError:negatives+=1
  else:raise AssertionError('Identity accepted')
 return dict(ExecutableSha256=EXE,InstructionAnchors=len(ANCHORS),WrongIdentityNegatives=negatives,ModeTargets=['013E7CDC','013E7CDC','013E7D68','013E7DDC','013E7E4A','013E7EE9'],CacheValueStores=4,OriginalExecuted=False,OriginalControls='Unknown',NoiseReproduced=False)

def setup():
 global old,a,store
 old=load('sampler_frozen_ffra','tools/python/verify_terrain_filter_fractal.py');old.setup();a=old.a;store=old.l.store

def finite(bits):return bits&0x7f800000!=0x7f800000

def evaluate(f):
 p=f['Precision']
 if p not in (24,53,64):return dict(Name=f['Name'],Reject='Options')
 def need(v):
  if not v:raise ValueError('Modern finite eligibility')
 def get(k):
  bits=f[k];need(finite(bits));return a.scalar(bits)
 def add(x,y):return a.precision(a.add(x,y),p)
 def mul(x,y):return a.precision(a.mul(x,y),p)
 def sub(x,y):return add(x,(-y[0],not y[1] if y[0]==0 else False))
 try:
  result=dict(Name=f['Name'],Reject='None')
  if f['Kind']=='Probe':
   stride,rows,col,row=f['Stride'],f['Rows'],f['Column'],f['Row'];need(1<=stride<=128 and 1<=rows<=128 and stride*rows<=1024 and 0<=col<stride and 0<=row<rows and 0<=f['Valid']<=255)
   x,z=get('X'),get('Z');hit=False
   if f['Valid']:
    cx,cz=get('CachedX'),get('CachedZ');e=a.scalar(EPSILON)
    hit=sub(cx,e)[0]<=x[0]<=add(cx,e)[0] and sub(cz,e)[0]<=z[0]<=add(cz,e)[0]
   result.update(Hit=hit,CellOffset=16*(row*stride+col),Value=store(get('CachedValue')) if hit else None)
  elif f['Kind']=='Grid':
   ow,oh,w,h=f['OldStride'],f['OldRows'],f['RequestedStride'],f['RequestedRows'];col,row=f['Column'],f['Row'];need(0<=ow<=128 and 0<=oh<=128 and (ow==0)==(oh==0) and 1<=w<=128 and 1<=h<=128 and ow*oh<=1024 and w*h<=1024 and 0<=col<w and 0<=row<h)
   growth=w>ow or h>oh;sw,sh=(w,h) if growth else (ow,oh);result.update(Growth=growth,Stride=sw,Rows=sh,ClearBytes=16*sw*sh if growth else 0,CellOffset=16*(row*sw+col))
  elif f['Kind']=='Coordinates':
   x=store(mul(get('X'),get('ScaleX')));z=store(mul(get('Z'),get('ScaleZ')))
   shiftedx=store(add(a.scalar(x),get('OffsetX')));shiftedz=store(add(a.scalar(z),get('OffsetZ')))
   result.update(ScaledX=x,ScaledZ=z,ShiftedX=shiftedx,ShiftedZ=shiftedz,NoiseX=store(mul(a.scalar(shiftedx),get('Frequency'))),NoiseZ=store(mul(a.scalar(shiftedz),get('Frequency'))))
  else:
   cp=f['Combine'];need(cp in (24,53,64));u=mul(get('A'),get('B'));cached=store(u);scale=get('Scale');h=get('Height');w=get('Weight')
   def cmul(x,y):return a.precision(a.mul(x,y),cp)
   def cadd(x,y):return a.precision(a.add(x,y),cp)
   miss=cmul(u,scale);hit=cmul(a.scalar(cached),scale)
   result.update(Retained=str(u[0].numerator)+'/'+str(u[0].denominator),NegativeZero=u[1],Cached=cached,AhfrMiss=store(cadd(h,cmul(miss,w))),AhfrHit=store(cadd(h,cmul(hit,w))),FfraMiss=store(miss),FfraHit=store(hit))
  return result
 except (ValueError,OverflowError):return dict(Name=f['Name'],Reject='Input')

def fixtures():
 fs=[]
 def add(kind,p,**kw):fs.append(dict(Name=str(len(fs)),Kind=kind,Precision=p,**kw))
 for p in (24,53,64):
  for center in (0,0x80000000,0x3f800000,0xbf800000,0x47800000,0x7f000000):
   c=a.scalar(center)
   for delta in (0,EPSILON,EPSILON|0x80000000,EPSILON-1,EPSILON+1,(EPSILON-1)|0x80000000,(EPSILON+1)|0x80000000):
    coord=store(a.add(c,a.scalar(delta)))
    for valid in (0,1,255):
     for axis in (0,1):add('Probe',p,Stride=5,Rows=4,Column=3,Row=2,Valid=valid,X=coord if axis==0 else center,Z=center if axis==0 else coord,CachedX=center,CachedZ=center,CachedValue=0x3f123456)
  for overrides in ({'Stride':0},{'Column':5},{'Row':4},{'Column':-1},{'Rows':129},{'Stride':33,'Rows':33},{'Valid':256},{'X':0x7fc12345},{'CachedValue':0x7f800000},{'CachedX':0x7f800000}):
   base=dict(Stride=5,Rows=4,Column=3,Row=2,Valid=1,X=0,Z=0,CachedX=0,CachedZ=0,CachedValue=0);base.update(overrides);add('Probe',p,**base)
  add('Probe',p,Stride=1,Rows=1,Column=0,Row=0,Valid=0,X=0,Z=0,CachedX=0x7fc12345,CachedZ=0x7f800000,CachedValue=0x7f800000)
  for x in (0,0x80000000,1,0x80000001,0x3f800001,0xbf800001,0x7f7fffff):
   for scale in (0,0x3f800000,0x3eaaaaab,0xbf800000):
    for offset in (0,0x3f800000,0xbf800000):add('Coordinates',p,X=x,Z=x^0x80000000,ScaleX=scale,ScaleZ=scale,OffsetX=offset,OffsetZ=offset,Frequency=0x3f800001)
  for bad in (0x7fc12345,0x7f800000):add('Coordinates',p,X=bad,Z=0,ScaleX=0x3f800000,ScaleZ=0x3f800000,OffsetX=0,OffsetZ=0,Frequency=0x3f800000)
 rng=random.Random(46)
 pairs=[(0x3f800001,0x3f800001),(0x3eaaaaab,0x3faaaaab),(0x80000000,0x3f800000),(1,0x3f000000)]+[(rng.randrange(0x3e800000,0x40000000),rng.randrange(0x3e800000,0x40000000)) for _ in range(150)]
 for p in (24,53,64):
  for cp in (24,53,64):
   for x,y in pairs:
    u=a.precision(a.mul(a.scalar(x),a.scalar(y)),p);cached=store(u);add('Post',p,Combine=cp,A=x,B=y,Scale=0x3f800001,Height=cached^0x80000000,Weight=0x3f800000)
 for p in (24,53,64):
  for ow,oh,w,h in ((0,0,4,5),(4,5,4,5),(4,5,3,4),(4,5,3,6),(4,5,5,3),(4,5,5,6),(32,32,1,1),(0,0,0,1),(4,5,129,1),(4,5,33,33)):
   add('Grid',p,OldStride=ow,OldRows=oh,RequestedStride=w,RequestedRows=h,Column=max(0,w-1),Row=max(0,h-1))
 for p in (23,0,65):add('Probe',p,Stride=1,Rows=1,Column=0,Row=0,Valid=0,X=0,Z=0,CachedX=0,CachedZ=0,CachedValue=0)
 return fs

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--exe',type=Path,required=True);parser.add_argument('--packages',type=Path,required=True);parser.add_argument('--run-root',type=Path,required=True);parser.add_argument('--native',type=Path);args=parser.parse_args()
 data=args.exe.read_bytes();identity(data)
 if not args.run_root.resolve().is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Private output root required')
 st=static(data,args.packages);setup();fs=fixtures();expected=[evaluate(f) for f in fs]
 witnesses=sum(e.get('AhfrMiss')!=e.get('AhfrHit') for e in expected);ffra_witnesses=sum(e.get('FfraMiss')!=e.get('FfraHit') for e in expected);assert witnesses>0 and ffra_witnesses>0
 result=dict(Static=st,SyntheticCases=len(fs),Rejected=sum(e['Reject']!='None' for e in expected),CacheHits=sum(e.get('Hit',False) for e in expected),RetainedReturnWitnesses=witnesses,FfraRetainedReturnWitnesses=ffra_witnesses,CasesByKind={kind:sum(f['Kind']==kind for f in fs) for kind in ('Probe','Grid','Coordinates','Post')},NoiseSamples='None; synthetic post-result products only')
 args.run_root.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native)==len(expected)
  for f,want,got in zip(fs,expected,native):assert want==got,(f,want,got)
  result['NativeCompared']=True
 else:(args.run_root/'sampler-fixtures.json').write_text(json.dumps(fs,separators=(',',':')))
 (args.run_root/'sampler-receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
