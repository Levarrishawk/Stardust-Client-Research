import argparse,json,struct,itertools,math
P=lambda f,*v:struct.pack('<'+f,*v)
def envelope(fields,count=65535):return P('HIQIBIH',5,0x12862153,0xfedcba9876543210,0x4352454f,3,len(fields)+2,count)+fields
def raw(b,extended=False):return P('H',4)+(P('HI',65535,len(b)) if extended else P('H',len(b)))+b
def name_string(b,extended=False):return (P('HI',65535,len(b)) if extended or len(b)>=65535 else P('H',len(b)))+b
def string_id(file,key,spacer=0,file_extended=False,key_extended=False):return P('H',1)+name_string(file,file_extended)+P('I',spacer)+name_string(key,key_extended)
def custom(units):return P('HI',2,len(units))+P('H'*len(units),*units)
def component(counter=0xffffffff):return P('HII',5,3,counter)+P('BiBiB',1,-2147483648,0,2147483647,2)
def wounds(legacy=False):return P('HII',17,4 if legacy else 2,0)+P('BHiiB',3,2,-2147483648,2147483647,4)
def parse(hexwire,legacy,max_fields=4096,max_names=4096,max_string=65536,max_units=65536,max_decoded=8388608):
 b=bytes.fromhex(hexwire);assert len(b)>=25
 a,op,obj,tag,view,size,count=struct.unpack_from('<HIQIBIH',b);assert (a,op,tag,view,size)==(5,0x12862153,0x4352454f,3,len(b)-23)
 p=25; rows=[]
 def read(fmt):
  nonlocal p
  n=struct.calcsize('<'+fmt);assert p+n<=len(b);v=struct.unpack_from('<'+fmt,b,p);p+=n;return v
 while p<len(b):
  start=p;index,=read('H');values=[];counter=None;bound=None;name_bytes=0;decoded=0
  if index==1:
   for part in range(2):
    n,=read('H')
    if n==65535:n,=read('I')
    assert n<=max_string and p+n<=len(b);p+=n;name_bytes+=n
    if part==0:read('I');name_bytes+=4
  elif index==2:
   n,=read('I');assert n<=max_units and n<=(len(b)-p)//2;read('H'*n);name_bytes=2*n
  elif index in (0,3,6,7,8,9,10):
   value=read('B' if index==10 else 'f' if index==0 else 'I')[0];decoded=1 if index==10 else 4
   if index==0:assert math.isfinite(value)
  elif index==4:
   n,=read('H')
   if n==65535:n,=read('I')
   assert p+n<=len(b);p+=n;decoded=n
  elif 11<=index<=16:
   fmt={11:'B',12:'B',13:'Q',14:'f',15:'I',16:'Q'}[index];value=read(fmt)[0];decoded=struct.calcsize('<'+fmt)
   if index==14:assert math.isfinite(value)
  elif index in (5,17):
   bound,counter=read('II');cursor=0
   while cursor<bound:
    kind,=read('B');cursor+=1
    if index==5:
     assert kind<=2
     if kind<2:read('i');decoded+=4
    else:
     assert kind<=4
     if kind<3:
      read('H')
      if kind:read('i');decoded+=4
     elif kind==3:
      n,=read('H');read('i'*n);decoded+=4*n
      if legacy:cursor+=n
  else:raise AssertionError('index')
  rows.append({'Index':index,'Hex':b[start:p].hex().upper(),'Counter':counter,'Bound':bound,'NameBytes':name_bytes,'DecodedBytes':name_bytes if index in (1,2) else decoded})
 assert len(rows)<=max_fields and sum(r['Index'] in (1,2) for r in rows)<=max_names
 assert sum(r['NameBytes'] for r in rows)<=1048576 and sum(r['DecodedBytes'] for r in rows)<=max_decoded
 return rows

def malformed_checks():
 bad=[P('H',1)+P('HI',65535,0xffffffff),P('H',1)+P('H',0)+P('I',0)+P('HI',65535,0xffffffff),P('HI',2,0xffffffff),P('HIH',2,2,0xffff),P('HI',2,0)+b'\xff',P('H',18)]
 bad += [P('HI',index,bits) for index in (0,14) for bits in (0x7f800000,0xff800000,0x7fc00000,0x7f800001)]
 bad += [custom([0xffff]*65536)*9]
 for field in bad:
  try:parse(envelope(field).hex(),False)
  except AssertionError:pass
  else:raise AssertionError('Independent malformed case accepted')
 print('Independent malformed/aggregate checks',len(bad))
def generate(path):
 cases=[]
 atoms=[raw(b''),raw(b'\x00\xff\x04',True),component(),P('HB',11,255),P('HB',12,128),P('HQ',13,2**64-1),P('HI',14,0x80000000),P('HI',15,2**32-1),P('HQ',16,0)]
 for legacy in (False,True):
  common=[P('HI',0,0x80000000),P('HI',3,0xffffffff),P('HI',6,0),P('HI',7,0x80000000),P('HI',8,17),P('HI',9,0xffffffff),P('HB',10,255)]
  names=[string_id(b'\x00\xfftable',b'key\x00\x80',0xffffffff,True,False),custom([0,0xffff,0xd800,0xdc00,0xdfff,0xd800,0xd800])]
  pool=list(itertools.chain.from_iterable(zip(common,atoms[:7])))+names+atoms[7:]+[wounds(legacy),P('HII',5,0,0),P('HII',17,0,2**32-1)]
  sequences=[pool,list(reversed(pool)),pool*2]
  sequences += [[raw(bytes(range(256))),P('HI',14,bits),component(counter),wounds(legacy)] for bits,counter in [(1,0),(0xff7fffff,1),(0x7f7fffff,0xffffffff)]]
  sequences += [[P('HII',17,1,0)+P('BH',3,0),raw(b'',True)]]
  sequences += [[a] for a in pool]
  sequences += [common,list(reversed(common)),common*2]
  sequences += [names,list(reversed(names)),names*2,[string_id(b'',b'',0,True,True),custom([])]]
  sequences += [[string_id(b'file',b'key',spacer,f,k),custom([0xd800,0,0xffff]),P('HB',10,255)] for spacer,f,k in [(0,False,False),(1,False,True),(0xffffffff,True,False),(0x80000000,True,True)]]
  sequences += [[P('HI',0,bits),raw(b'float'),P('HB',10,visible)] for bits,visible in [(0,0),(1,1),(0xff7fffff,128),(0x7f7fffff,255)]]
  sequences += [[atoms[1],a,wounds(legacy),atoms[0],a] for a in pool]
  for i,seq in enumerate(sequences):
   wire=envelope(b''.join(seq),[0,1,65535][i%3]).hex().upper();cases.append({'Hex':wire,'Legacy':legacy,'Rows':parse(wire,legacy)})
 # A legacy assignment may consume more loop units than the raw bound.
 wire=envelope(P('HII',17,1,7)+P('BHii',3,2,3,4)+raw(b'x')).hex().upper();cases.append({'Hex':wire,'Legacy':True,'Rows':parse(wire,True)})
 for counter in (5,9):
  wire=envelope(P('HI',0,1)+string_id(b'table',b'key',7)+raw(b'prior')+P('HII',17,4,counter)+P('BHiii',3,3,7,8,9)+custom([0xdfff])+raw(b'after')+P('HB',10,0)).hex().upper();cases.append({'Hex':wire,'Legacy':True,'Rows':parse(wire,True),'MaximumElements':1})
 for n in (65534,65535,65536):
  wire=envelope(string_id(bytes([0xff])*n,bytes([0])*n,0x80000000)+custom([0xd800]*n)+P('HB',10,255)).hex().upper();cases.append({'Hex':wire,'Legacy':False,'Rows':parse(wire,False)})
 for fields in (custom([])*4096,custom([0xffff]*65536)*8):
  wire=envelope(fields,0).hex().upper();cases.append({'Hex':wire,'Legacy':False,'Rows':parse(wire,False)})
 json.dump(cases,open(path,'w'),indent=2);print('Generated',len(cases),'independent named wire cases')
def verify(peer,output):
 cases=json.load(open(peer));out=json.load(open(output));assert len(cases)==len(out);budgets=0
 for c,o in zip(cases,out):
  assert c['Hex']==o['Hex'];assert c['Rows']==parse(o['Hex'],c['Legacy']);assert [r['Index'] for r in c['Rows']]==o['Indices']
  charge=sum(r['DecodedBytes'] for r in c['Rows'])
  if charge:
   try:parse(o['Hex'],c['Legacy'],max_decoded=charge-1)
   except AssertionError:budgets+=1
   else:raise AssertionError('Independent decoded cap accepted')
 print('Independent one-less decoded budgets',budgets)
 print('Verified',len(out),'complete named ordered wires independently')
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--generate');a.add_argument('--peer');a.add_argument('--verify');x=a.parse_args()
 malformed_checks()
 if x.generate:generate(x.generate)
 else:verify(x.peer,x.verify)
