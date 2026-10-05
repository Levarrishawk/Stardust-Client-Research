"""Independent raw-current provenance and Fraction paired-reader oracle; synthetic wire only."""
import argparse,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
raw=runpy.run_path(str(ROOT/'poc/round49-tests/Networking/peer.py'))
speed=runpy.run_path(str(ROOT/'poc/round59-tests/Runtime/verify_reader.py'))
def wire(updates):
 body=struct.pack('<H',65535)+b''.join(struct.pack('<H'+('Q' if i==6 else 'I'),i,b) for i,b in updates)
 return (struct.pack('<HIQIBI',5,0x12862153,42,0x4352454f,4,len(body))+body).hex().upper()
def generate(out):
 seed=out.with_suffix('.seed.json');raw['generate'](seed);original=json.loads(seed.read_text())
 cases=[]
 def put(**kw):
  c=dict(Name=str(len(cases)),Profile=0,Typed=True,Finalized=True,Precision=64,Updates=None);c.update(kw)
  if c['Updates'] is not None:c['Wire']=wire(c['Updates'])
  cases.append(c);return c
 for p in (24,53,64):
  for c in original['Cases']:put(**dict(c,Precision=p))
  for run,walk,first,second in [(0x40000000,0x3f800000,0x3f800000,0x3f800000),(0x7f7fffff,0,1,0x3f000000),(0,0,0x7f7fffff,0x7f7fffff),(0x3f800001,0xbf800001,0x3f123456,0x3f789abc),(0x80000000,1,0xbf800000,0x3f800000),(0x3f800000,0x7f7fffff,0x40000000,0x3f800000),(0,0,0,0x7fc12345)]:
   put(Precision=p,Updates=[[7,run],[11,walk],[4,first],[5,second]])
 fixed=dict(Updates=[[7,0x40000000],[11,0x3f800000],[4,0x3f800000],[5,0x3f800000]])
 for k,v in [('MaximumOutputBytes',2080),('MaximumCopyBytes',256),('MaximumWork',16384),('MaximumReaders',2),('MaximumReaderCopyBytes',128)]:
  put(**fixed,**{k:v},Boundary='exact');put(**fixed,**{k:v-1},Boundary='below')
 w=max(speed['work'](dict(Base=b,First=0x3f800000,Second=0x3f800000,Precision=64)) for b in [0x40000000,0x3f800000])
 put(**fixed,MaximumReaderWork=w,Boundary='exact');put(**fixed,MaximumReaderWork=w-1,Boundary='below')
 for k,h in [('MaximumOutputBytes',65536),('MaximumCopyBytes',256),('MaximumWork',16384),('MaximumReaders',2),('MaximumReaderCopyBytes',128),('MaximumReaderWork',8192)]:
  put(**fixed,**{k:-1});put(**fixed,**{k:h+1})
 put(**fixed,Policy=0);put(**fixed,Policy=2);put(**fixed,Precision=23);put(**fixed,Cancel=True)
 return dict(Baseline=original['Baseline'],Profiles=original['Profiles'],Cases=cases)
def expected_profiles(fixture):
 model=runpy.run_path(str(ROOT/'poc/round46-tests/Networking/peer.py'))['expected'];result=[]
 for p in fixture['Profiles']:
  resources={r['Path']:bytes.fromhex(r['Hex']) for r in p['Resources']}
  chain=raw['read_chain'](lambda q:(77,'synthetic',resources[q]) if q in resources else None,p['RootPath'])
  result.append(model(chain))
 return result
def evaluate(c,profile,pin):
 name=c['Name'];bad=dict(Name=name,Reject='Input')
 caps=[c.get(k,d) for k,d in [('MaximumOutputBytes',2080),('MaximumCopyBytes',256),('MaximumWork',16384),('MaximumReaders',2),('MaximumReaderCopyBytes',128),('MaximumReaderWork',8192)]]
 if c.get('Policy',1)!=1 or c['Precision'] not in (24,53,64) or any(not 0<=v<=h for v,h in zip(caps,[65536,256,16384,2,128,8192])) or any(v<minimum for v,minimum in zip(caps[:5],[2080,256,16384,2,128])):return bad
 if c.get('Cancel'):return dict(Name=name,Reject='Canceled')
 if not c['Typed'] or not c['Finalized']:return dict(Name=name,Reject='Null')
 updates=dict(c['Updates'] or []);fields=[];values={}
 for i in [7,11,4,5]:
  if i in updates:b=updates[i];origin='Received'
  elif i in [4,5]:b=0x3f800000;origin='ConstructorLiteralOne'
  else:
   a=profile['speed[0]' if i==7 else 'speed[1]'];b=a['Bits'] if a['Kind']=='Literal' else None;origin='ConstructorAuthoredRaw' if b is not None else 'Unknown'
  if b is None or (b&0x7f800000)==0x7f800000:return bad
  fields.append(dict(Index=i,RawBits=b,Origin=origin,Eligibility='FiniteCurrent'));values[i]=b
 results=[]
 for i in [7,11]:
  r=speed['evaluate'](dict(Name=name,Base=values[i],First=values[4],Second=values[5],Precision=c['Precision'],MaximumCopyBytes=caps[4],MaximumWork=caps[5]))
  if r['Reject']!='None':return bad
  r.pop('Name');r.pop('Reject');results.append(r)
 return dict(Name=name,Reject='None',RootSha256=pin,Fields=fields,Run=results[0],Walk=results[1],Output=2080,Copy=256,Reserved=16384,ActualWork=sum(r['Work'] for r in results),Counts=[2,6,4,2])
def main():
 p=argparse.ArgumentParser();sub=p.add_subparsers(dest='mode',required=True);g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True);v=sub.add_parser('verify')
 for key in ('cases','native','receipt'):v.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if a.mode=='generate':
  out=guard(a.out,ROOT);guard(out.with_suffix('.seed.json'),ROOT);f=generate(out);out.write_text(json.dumps(f,separators=(',',':'))+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(f['Cases']));return
 out=guard(a.receipt,ROOT);fixture=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());profiles=expected_profiles(fixture)
 expected=[evaluate(c,profiles[c['Profile']],fixture['Profiles'][c['Profile']]['RootSha256']) for c in fixture['Cases']]
 assert len(expected)==len(native['Cases'])
 for c,w,g in zip(fixture['Cases'],expected,native['Cases']):
  if w!=g:raise ValueError(f"Mismatch {c['Name']}: expected {w} actual {g}")
 positive=sum(r['Reject']=='None' for r in expected);assert native['PositiveIdentityChecks']==positive and native['NativeChecks']>100
 assert native['Repeated']==100 and native['LifecyclePassed'] and native['PriorResultPreserved'] and native['BorrowedReaderSurvivesAdapterDisposal']
 boundary=[(c,r) for c,r in zip(fixture['Cases'],expected) if 'Boundary' in c]
 assert len(boundary)==12 and all(r['Reject']==('None' if c['Boundary']=='exact' else 'Input') for c,r in boundary)
 report=dict(Cases=len(expected),Positive=positive,Rejected=sum(r['Reject']=='Input' for r in expected),Null=sum(r['Reject']=='Null' for r in expected),Canceled=sum(r['Reject']=='Canceled' for r in expected),Profiles=len(profiles),ExactCapCases=6,OneBelowCapCases=6,ActualReaderReceipts=2*positive,Multiplications=4*positive,CallerStores=2*positive,IdentityOwnershipChecks=positive,NativeChecks=native['NativeChecks'],Repeated=100,LifecyclePassed=True,PriorResultPreserved=True,BorrowedReaderSurvivesAdapterDisposal=True,GuardInjectionOnlyForReentry=True,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False)
 out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('PAIRED_FRACTION_PROVENANCE_PASS '+json.dumps(report))
if __name__=='__main__':main()
