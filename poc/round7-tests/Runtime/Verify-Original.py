"""Independent effective-source identity check for the initializer's static original reader job."""
import argparse
import contextlib
import hashlib
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index

p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('native_output',type=Path);args=p.parse_args()
assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
assert args.native_output.stat().st_size<1048576
rows={}
for line in args.native_output.read_text(encoding='utf-8-sig').splitlines():
 if line.startswith('source '):
  _,path,pin,size=line.split();assert path not in rows;rows[path]=(pin,int(size))
assert len(rows)==37 and sum(size for _,size in rows.values())==4683848
with contextlib.ExitStack() as stack:
 indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(args.root,args.configuration)]
 for path,(pin,size) in rows.items():
  for index in indices:
   data=index.lookup(path)
   if data is not None:break
  else:raise ValueError('Missing effective source '+path)
  assert len(data)==size and hashlib.sha256(data).hexdigest().upper()==pin,path
print('PASS: independent effective overlay identities;37 sources/4683848 bytes; asynchronous policy remains modern')
