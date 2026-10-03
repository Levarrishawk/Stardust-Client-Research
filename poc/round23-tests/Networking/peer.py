import argparse,json,struct,itertools,math
P=lambda f,*v:struct.pack('<'+f,*v)
def envelope(fields,count=65535):return P('HIQIBIH',5,0x12862153,0xfedcba9876543210,0x4352454f,3,len(fields)+2,count)+fields
def raw(b,extended=False):return P('H',4)+(P('HI',65535,len(b)) if extended else P('H',len(b)))+b
def component(counter=0xffffffff):return P('HII',5,3,counter)+P('BiBiB',1,-2147483648,0,2147483647,2)
def wounds(legacy=False):return P('HII',17,4 if legacy else 2,0)+P('BHiiB',3,2,-2147483648,2147483647,4)
def parse(hexwire,legacy):
 b=bytes.fromhex(hexwire);assert len(b)>=25
 a,op,obj,tag,view,size,count=struct.unpack_from('<HIQIBIH',b);assert (a,op,tag,view,size)==(5,0x12862153,0x4352454f,3,len(b)-23)
 p=25; rows=[]
 def read(fmt):
  nonlocal p
  n=struct.calcsize('<'+fmt);assert p+n<=len(b);v=struct.unpack_from('<'+fmt,b,p);p+=n;return v
 while p<len(b):
  start=p;index,=read('H');values=[];counter=None;bound=None
  if index==4:
   n,=read('H')
   if n==65535:n,=read('I')
   assert p+n<=len(b);p+=n
  elif 11<=index<=16:
   fmt={11:'B',12:'B',13:'Q',14:'f',15:'I',16:'Q'}[index];value=read(fmt)[0]
   if index==14:assert math.isfinite(value)
  elif index in (5,17):
   bound,counter=read('II');cursor=0
   while cursor<bound:
    kind,=read('B');cursor+=1
    if index==5:
     assert kind<=2
     if kind<2:read('i')
    else:
     assert kind<=4
     if kind<3:
      read('H')
      if kind:read('i')
     elif kind==3:
      n,=read('H');read('i'*n)
      if legacy:cursor+=n
  else:raise AssertionError('index')
  rows.append({'Index':index,'Hex':b[start:p].hex().upper(),'Counter':counter,'Bound':bound})
 return rows

def generate(path):
 cases=[]
 atoms=[raw(b''),raw(b'\x00\xff\x04',True),component(),P('HB',11,255),P('HB',12,128),P('HQ',13,2**64-1),P('HI',14,0x80000000),P('HI',15,2**32-1),P('HQ',16,0)]
 for legacy in (False,True):
  pool=atoms+[wounds(legacy),P('HII',5,0,0),P('HII',17,0,2**32-1)]
  sequences=[pool,list(reversed(pool)),pool*2]
  sequences += [[raw(bytes(range(256))),P('HI',14,bits),component(counter),wounds(legacy)] for bits,counter in [(1,0),(0xff7fffff,1),(0x7f7fffff,0xffffffff)]]
  sequences += [[P('HII',17,1,0)+P('BH',3,0),raw(b'',True)]]
  sequences += [[a] for a in pool]
  sequences += [[atoms[1],a,wounds(legacy),atoms[0],a] for a in pool]
  for i,seq in enumerate(sequences):
   wire=envelope(b''.join(seq),[0,1,65535][i%3]).hex().upper();cases.append({'Hex':wire,'Legacy':legacy,'Rows':parse(wire,legacy)})
 # A legacy assignment may consume more loop units than the raw bound.
 wire=envelope(P('HII',17,1,7)+P('BHii',3,2,3,4)+raw(b'x')).hex().upper();cases.append({'Hex':wire,'Legacy':True,'Rows':parse(wire,True)})
 for counter in (5,9):
  wire=envelope(raw(b'prior')+P('HII',17,4,counter)+P('BHiii',3,3,7,8,9)+raw(b'after')).hex().upper();cases.append({'Hex':wire,'Legacy':True,'Rows':parse(wire,True),'MaximumElements':1})
 json.dump(cases,open(path,'w'),indent=2);print('Generated',len(cases),'independent full wire cases')
def verify(peer,output):
 cases=json.load(open(peer));out=json.load(open(output));assert len(cases)==len(out)
 for c,o in zip(cases,out):
  assert c['Hex']==o['Hex'];assert c['Rows']==parse(o['Hex'],c['Legacy']);assert [r['Index'] for r in c['Rows']]==o['Indices']
 print('Verified',len(out),'complete ordered wires independently')
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--generate');a.add_argument('--peer');a.add_argument('--verify');x=a.parse_args()
 if x.generate:generate(x.generate)
 else:verify(x.peer,x.verify)
