"""CREO6 inherited defender identity and exact-specialization audit; static only."""
import argparse,contextlib,hashlib,io,json,runpy,struct,sys
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def audit(exe,packages):
 raw=exe.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
 if digest!=EXPECTED:raise ValueError('Wrong executable identity; rejected before PE imports/VAs')
 repo=Path(__file__).resolve().parents[3]
 identity=runpy.run_path(str(repo/'poc/round31-tests/Networking/static_audit.py'))['audit'](exe,packages)
 state=runpy.run_path(str(repo/'poc/round29-tests/Networking/static_audit.py'))['audit'](exe,packages)
 previous=sys.argv;sys.argv=['static_audit.py',str(exe),'--packages',str(packages)]
 try:
  out=io.StringIO()
  with contextlib.redirect_stdout(out):runpy.run_path(str(repo/'poc/round28-tests/Networking/static_audit.py'),run_name='__main__')
  codec=json.loads(out.getvalue())
 finally:sys.argv=previous
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);read=lambda va,n:pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
 # Direct creature-to-parent constructor path and nullable owner hook, independent of server analogy.
 ctor=list(md.disasm(read(0x42ecf0,0x100),0x42ecf0));parent=[i for i in ctor if i.mnemonic=='call' and i.op_str=='0x63ee10']
 assert len(parent)==1 and parent[0].address==0x42ed2a
 owner=[(0x43e862,'mov','ecx, dword ptr [eax + 8]'),(0x43e865,'test','ecx, ecx'),(0x43e867,'je','0x43e86f'),(0x43e86a,'call','0x118a7c0')]
 for va,mnemonic,operand in owner:
  i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(mnemonic,operand)
 assert identity['Fields'][1]['Vtable']=='015E0614' and identity['Fields'][1]['Delta']=='006423E0'
 return dict(ExeSha256=digest,IdentityAnchors=identity['InstructionAnchors'],KernelAnchors=state['InstructionAnchors'],CodecAnchors=codec['InstructionAnchors'],DirectAnchors=5,CreatureParentCall=f'{parent[0].address:08X}',Field=1,Member='2F0',Vtable='015E0614',Snapshot='006421A0',Delta='006423E0',DefaultIdBits=state['DefaultIdBits'],Equality='both U32 ID words, ignoring cached watcher',OriginalExecuted=False)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args();repo=Path(__file__).resolve().parents[3];assert a.receipt.resolve().is_relative_to((repo/'local-private').resolve())
 try:r=audit(a.exe,a.packages)
 except ValueError as e:p.exit(1,str(e)+'\n')
 a.receipt.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8');print(f"PASS {r['IdentityAnchors']} identity + {r['KernelAnchors']} kernel + {r['CodecAnchors']} codec anchors, creature parent/owner path; static only")
