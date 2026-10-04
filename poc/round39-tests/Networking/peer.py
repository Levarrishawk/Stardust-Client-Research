"""Synthetic little-endian fixtures and independent field2 list/counter/accounting oracle."""
import argparse,json,random,struct
from pathlib import Path
U=lambda x:x&0xffffffff

def command(op,index=0,values=()):return dict(op=op,index=index,values=list(values))
def update(counter,commands,extent=None):return dict(counter=counter,commands=commands,extent=sum(1+(len(c['values']) if c['op']==3 else 0) for c in commands) if extent is None else extent)
def wire(updates,declared=65535):
 b=bytearray()
 for u in updates:
  b+=struct.pack('<HII',2,u['extent'],u['counter'])
  for c in u['commands']:
   b+=bytes([c['op']])
   if c['op']!=4:b+=struct.pack('<H',len(c['values']) if c['op']==3 else c['index'])
   for v in c['values']:b+=struct.pack('<i',v)
 payload=struct.pack('<H',declared)+b
 return (struct.pack('<HIQIBI',5,0x12862153,0x1122334455667788,0x4352454f,4,len(payload))+payload).hex()
def oracle(seed,counter,updates):
 a=list(seed);events=[];work=len(a)+len(updates)+sum(len(u['commands'])+sum(len(c['values']) for c in u['commands']) for u in updates)
 peak=len(a);peak_output=8+4*peak
 for u in updates:
  skip=min(U(u['extent']-u['counter']+counter),u['extent']);position=0
  for c in u['commands']:
   ignored=position<skip;op=c['op'];i=c['index'];v=c['values'];position+=1+(len(v) if op==3 else 0)
   if ignored:continue
   old=new=None;cap=len(a)
   if op==0:
    if i>=len(a):continue
    work+=len(a)-i;old=a.pop(i);counter=U(counter+1)
   elif op==1:
    cap=max(len(a),i)+1;work+=cap-len(a)+max(0,len(a)-i)
    a.extend([0]*max(0,i-len(a)));a[i:i]=v;new=v[0];counter=U(counter+1)
   elif op==2:
    if i<len(a) and a[i]==v[0]:continue
    cap=max(len(a),i+1);work+=cap-len(a)+1;a.extend([0]*max(0,i+1-len(a)));old=a[i];a[i]=v[0];new=v[0];counter=U(counter+1)
   elif op==3:
    cap=len(v);work+=len(a)+len(v);a=list(v);counter=U(counter+1+len(v))
   else:
    if not a:continue
    work+=len(a);a=[];counter=U(counter+1)
   events.append(dict(Opcode=op,Index=i if op<=2 else None,OldValue=old,NewValue=new))
   peak=max(peak,cap);peak_output=max(peak_output,8+4*cap+24*len(events))
 work+=len(a)+len(events)
 return dict(Values=a,Counter=counter,Events=events,WorkUsed=work,DecodedOutputBytes=8+4*len(a)+24*len(events),Peak=peak,PeakOutput=peak_output)
def generate():
 cases=[]
 def add(name,a,c,us):cases.append(dict(Name=name,Seed=a,SeedCounter=c,Wire=wire(us),Expected=oracle(a,c,us)))
 for op in range(5):
  cs=[command(op,1,[7] if op in (1,2) else [5,-6,2147483647] if op==3 else [])]
  add('opcode'+str(op),[1,2,3],10,[update(10+sum(1+(len(x['values']) if x['op']==3 else 0) for x in cs),cs)])
 add('equal-set',[9],0,[update(1,[command(2,0,[9])])]);add('invalid-remove',[9],0,[update(1,[command(0,65535)])]);add('empty-clear',[],0,[update(1,[command(4)])])
 add('sparse-insert',[4],0,[update(1,[command(1,8,[-2147483648])])]);add('sparse-set-zero',[4],0,[update(1,[command(2,8,[0])])]);add('index65535',[],0,[update(1,[command(2,65535,[-1])])])
 add('wrap',[4],0xffffffff,[update(0,[command(2,0,[5])])]);add('stale-all',[4],100,[update(2,[command(1,0,[5]),command(4)])]);add('duplicate-half',[4],1,[update(2,[command(2,0,[9]),command(1,1,[8])])])
 add('replace-overshoot',[4],0,[update(1,[command(3,0,[8,9])],1)]);add('skip-replace-overshoot',[4],1,[update(2,[command(3,0,[8,9])],2)])
 add('replace65535',[],0,[update(65536,[command(3,0,[i-32768 for i in range(65535)])])])
 add('empty-replace',[],0,[update(1,[command(3)])]);add('zero-extent',[1],4,[update(99,[],0)])
 add('ordered',[1,2],0,[update(1,[command(1,0,[3])]),update(2,[command(4)]),update(3,[command(1,2,[-9])])])
 add('intermediate-grow-clear',[],0,[update(2,[command(1,30,[8]),command(4)])])
 r=random.Random(39)
 for k in range(100):
  a=[r.randrange(-100,101) for _ in range(r.randrange(10))];counter=r.randrange(100);cs=[]
  for j in range(r.randrange(1,12)):
   op=r.randrange(5);cs.append(command(op,r.randrange(16) if op<3 else 0,[r.randrange(-100,101)] if op in (1,2) else [r.randrange(-100,101) for _ in range(r.randrange(8))] if op==3 else []))
  extent=sum(1+(len(c['values']) if c['op']==3 else 0) for c in cs)
  add('random'+str(k),a,counter,[update(U(counter+extent-r.randrange(extent+1)),cs)])
 return cases

def main():
 p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('fixture');p.add_argument('export',nargs='?');a=p.parse_args()
 if a.mode=='generate':Path(a.fixture).write_text(json.dumps(generate())+'\n',encoding='utf-8');print('generated',len(generate()),'synthetic independent cases');return
 cases=json.loads(Path(a.fixture).read_text());out=json.loads(Path(a.export).read_text());assert len(out['Records'])==len(cases)
 for c,x in zip(cases,out['Records']):
  assert x['Name']==c['Name'] and x['Wire']==c['Wire'],c['Name']
  for key in ['Values','Counter','Events','WorkUsed','DecodedOutputBytes']:assert x[key]==c['Expected'][key],(c['Name'],key,x[key],c['Expected'][key])
 print('PASS independent wire/model/counter/event/work oracle',len(cases),'cases', {k:v for k,v in out.items() if k!='Records'})
if __name__=='__main__':main()
