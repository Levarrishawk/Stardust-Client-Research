"""Independent synthetic scalar/HAM host peer; no native implementation imports."""
import json, runpy, struct, sys
from pathlib import Path
root=Path(__file__).resolve().parents[3]
base=runpy.run_path(str(root/'poc/round34-tests/Integration/generate_peer.py'))
oracle=runpy.run_path(str(root/'poc/round34-tests/Networking/peer.py'))
p,oid=base['p'],base['oid']
model=p.parse(bytes.fromhex(base['baseline']),'Baseline')[0]
def envelope(fields,opcode,count):
 return struct.pack('<HIQIBIH',5,opcode,oid,0x4352454f,6,len(fields)+2,count)+fields
def snapshot(m):
 return dict(ModelHex=envelope(p.baseline_fields(m),0x68a75f0c,m['DeclaredCount']).hex().upper(),StateBytes=oracle['measure'](m)['StateBytes'])
assert snapshot(model)['ModelHex']==base['baseline']
H,S=oracle['H'],oracle['S']
plans=[
 ('wrapped-ham-and-scalar',[H(13,1,0,[(1,1,[-1])]),S(9,0xffffffffffffffff)]),
 ('maximum-ham-and-strings',[H(14,1,0x80000001,[(2,1,[-2147483648])]),S(3,t=p.text(b'\x00\xff',True))]),
 ('interleaved-assign',[S(7,0,second=0x8000000000000000),H(13,3,3,[(3,0,[-1,2147483647])]),S(12,0xffffffff)]),
 ('clear-and-reinsert',[H(14,1,0x80000002,[(4,0,[])]),H(14,1,0x80000003,[(1,2,[9])]),S(17,0)]),
 ('equal-and-skipped',[H(13,1,4,[(2,0,[-1])]),S(9,0xffffffffffffffff),H(14,1,0x80000003,[(0,0,[])])]),
]
seed=snapshot(model);steps=[]
for name,updates in plans:
 result,events,peak=oracle['apply'](model,updates);model=result['Model']
 fields=b''.join(oracle['field'](u) for u in updates)
 steps.append(dict(Name=name,Hex=envelope(fields,0x12862153,65535).hex().upper(),Events=events,Peak=peak,**snapshot(model)))
fixture=dict(Version=1,Synthetic=True,ObjectId=oid,Baseline=base['baseline'],Seed=seed,Steps=steps)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
 print('Prepared five ordered scalar/HAM plans and full-model/event expectations')
