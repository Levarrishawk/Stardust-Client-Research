import argparse,json,struct,math,itertools
P=lambda f,*v:struct.pack('<'+f,*v)
TAG=0x54414e4f
BASE=0x68a75f0c
DELTA=0x12862153
def envelope(fields,kind,count):return P('HIQIBIH',5,BASE if kind=='Baseline' else DELTA,0xfedcba9876543210,TAG,3,len(fields)+2,count)+fields
def string(b,extended=False):return (P('HI',65535,len(b)) if extended or len(b)>=65535 else P('H',len(b)))+b
def name(file,key,spacer=0,f=False,k=False):return string(file,f)+P('I',spacer)+string(key,k)
def unicode(units):return P('I',len(units))+P('H'*len(units),*units)
class Reader:
 def __init__(self,b):self.b=b;self.p=0
 def read(self,f):
  n=struct.calcsize('<'+f);assert n<=len(self.b)-self.p;v=struct.unpack_from('<'+f,self.b,self.p);self.p+=n;return v
 def u(self,f):return self.read(f)[0]
 def string(self):
  n=self.u('H');extended=n==65535
  if extended:n=self.u('I')
  assert n<=len(self.b)-self.p;b=self.b[self.p:self.p+n];self.p+=n;return {'Hex':b.hex().upper(),'Extended':extended}
 def units(self):
  n=self.u('I');assert n<=(len(self.b)-self.p)//2;return list(self.read('H'*n))
 def finite(self):
  bits=self.u('I');assert math.isfinite(struct.unpack('<f',P('I',bits))[0]);return bits
 def end(self):assert self.p==len(self.b)
def outer(hexwire,kind):
 b=bytes.fromhex(hexwire);assert len(b)>=25;r=Reader(b);a,opcode,obj,tag,view,size,count=r.read('HIQIBIH');assert (a,opcode,tag,view,size)==(5,BASE if kind=='Baseline' else DELTA,TAG,3,len(b)-23);return r,obj,count
def parse_base(hexwire,max_decoded=8388608):
 r,obj,count=outer(hexwire,'Baseline');v={'ObjectId':obj,'DeclaredCount':count,'ComplexityBits':r.finite(),'File':r.string(),'Spacer':r.u('I'),'Key':r.string(),'Units':r.units(),'Volume':r.u('I'),'Raw':r.string()}
 n,counter=r.read('II');assert n<=(len(r.b)-r.p)//4;v.update(Counter=counter,Components=list(r.read('i'*n)))
 v.update(Options=r.u('I'),Use=r.u('I'),Condition=r.u('I'),Maximum=r.u('I'),Visibility=r.u('B'));r.end()
 v['Decoded']=47+sum(len(v[k]['Hex'])//2 for k in ('File','Key','Raw'))+2*len(v['Units'])+4*n;assert v['Decoded']<=max_decoded;return v
def parse_delta(hexwire,max_decoded=8388608):
 r,obj,count=outer(hexwire,'Delta');rows=[];names=0
 while r.p<len(r.b):
  start=r.p;index=r.u('H');cost=0;name_cost=0
  if index==0:r.finite();cost=4
  elif index==1:
   f=r.string();r.u('I');k=r.string();assert len(f['Hex'])//2<=65536 and len(k['Hex'])//2<=65536;cost=len(f['Hex'])//2+4+len(k['Hex'])//2;name_cost=cost
  elif index==2:
   units=r.units();assert len(units)<=65536;cost=2*len(units);name_cost=cost
  elif index in (3,6,7,8,9):r.u('I');cost=4
  elif index==4:cost=len(r.string()['Hex'])//2
  elif index==5:
   n,counter=r.read('II');assert n<=4096
   for _ in range(n):
    op=r.u('B');assert op<=2
    if op<2:r.u('i');cost+=4
  elif index==10:r.u('B');cost=1
  else:raise AssertionError('TANO common indices only')
  names+=name_cost;rows.append({'Index':index,'Hex':r.b[start:r.p].hex().upper(),'Decoded':cost})
 assert 1<=len(rows)<=4096 and names<=1048576 and sum(row['Decoded'] for row in rows)<=max_decoded;return rows
def malformed_checks():
 empty=P('I',0)+name(b'',b'')+unicode([])+P('I',0)+string(b'')+P('II',0,0)+P('IIIIB',0,0,0,0,0)
 bad=[]
 for offset in (12,22):
  b=bytearray(empty);struct.pack_into('<I',b,offset,0xffffffff);bad.append(('Baseline',bytes(b)))
 for offset in (4,10,20):bad.append(('Baseline',empty[:offset]+P('HI',65535,0xffffffff)+empty[offset+2:]))
 bad += [('Baseline',empty+b'\x00'),('Baseline',P('I',0x7f800000)+empty[4:])]
 bad += [('Delta',P('H',index)) for index in range(11,19)]
 bad += [('Delta',P('H',1)+P('HI',65535,0xffffffff)),('Delta',P('HI',2,0xffffffff)),('Delta',P('HI',0,0x7fc00000)),('Delta',P('HII',5,4097,0)+bytes([2])*4097)]
 bad += [('Delta',P('H',1)+name(bytes([255])*65537,b'')),('Delta',(P('H',2)+unicode([0xffff]*65536))*9)]
 for kind,fields in bad:
  try:(parse_base if kind=='Baseline' else parse_delta)(envelope(fields,kind,0).hex())
  except AssertionError:pass
  else:raise AssertionError('Independent malformed TANO accepted')
 print('Independent malformed TANO cases',len(bad))
def generate(path):
 cases=[]
 def add(kind,fields,count):
  wire=envelope(fields,kind,count).hex().upper();cases.append({'Kind':kind,'Hex':wire,'Expected':parse_base(wire) if kind=='Baseline' else parse_delta(wire)})
 for i,(bits,flags) in enumerate(itertools.product((0,0x80000000,1,0xff7fffff,0x7f7fffff),itertools.product((False,True),repeat=3))):
  f,k,a=flags;units=[0,0xffff,0xd800,0xdc00,0xdfff];components=[-2147483648,7,7,2147483647,-3]
  fields=P('I',bits)+name(b'\x00\xfftable',b'key\x80\x00',0x80000000,f,k)+unicode(units)+P('I',0xffffffff)+string(b'\xff\x04\x00',a)+P('II',len(components),0xffffffff)+P('i'*len(components),*components)+P('IIIIB',0x80000000,0xffffffff,0xffffffff,0,255)
  add('Baseline',fields,(0,11,65535)[i%3])
 empty=P('I',0)+name(b'',b'')+unicode([])+P('I',0)+string(b'')+P('II',0,0)+P('IIIIB',0,0,0,0,0);add('Baseline',empty,1)
 for n in (65534,65535,65536,65537):
  fields=P('I',0x80000000)+name(b'\xff'*n,b'\x00'*n,0xffffffff)+unicode([0xd800]*n)+P('I',0)+string(b'\x80'*n)+P('IIi',1,0,-2147483648)+P('IIIIB',0,1,2,3,128);add('Baseline',fields,0)
 atoms=[P('HI',0,0x80000000),P('H',1)+name(b'\x00\xff',b'\x80key',0xffffffff,True,False),P('H',2)+unicode([0,0xffff,0xd800,0xdfff]),P('HI',3,0xffffffff),P('H',4)+string(b'\xff\x04\x00',True),P('HII',5,3,0xffffffff)+P('BiBiB',1,-2147483648,0,2147483647,2),P('HI',6,0x80000000),P('HI',7,0xffffffff),P('HI',8,0xffffffff),P('HI',9,0),P('HB',10,255)]
 seqs=[atoms,list(reversed(atoms)),atoms*2]+[[a] for a in atoms]
 seqs += [[P('H',1)+name(b'',b'',0,True,True),P('H',2)+unicode([]),P('HII',5,0,0)], [P('HB',10,0),P('HI',0,1),P('H',4)+string(b'',True)]]
 for f,k in itertools.product((False,True),repeat=2):seqs.append([P('H',1)+name(b'file',b'key',0x80000000,f,k),atoms[5],atoms[2]])
 seqs += [[P('HI',0,bits),P('HB',10,visible),atoms[1]] for bits,visible in [(0,0),(1,1),(0xff7fffff,128),(0x7f7fffff,255)]]
 for i,seq in enumerate(seqs):add('Delta',b''.join(seq),(0,1,11,65535)[i%4])
 for n in (65534,65535,65536):add('Delta',P('H',1)+name(b'\xff'*n,b'\x00'*n,0xffffffff)+P('H',2)+unicode([0xdfff]*n)+P('H',4)+string(b'\x80'*n),0)
 add('Delta',P('H',4)+string(b'\xff'*65537),65535)
 add('Delta',(P('H',2)+unicode([]))*4096,0)
 add('Delta',(P('H',2)+unicode([0xffff]*65536))*8,0)
 json.dump(cases,open(path,'w'),indent=2);print('Generated',sum(c['Kind']=='Baseline' for c in cases),'baselines',sum(c['Kind']=='Delta' for c in cases),'deltas')
def verify(peer,out):
 cases=json.load(open(peer));exports=json.load(open(out));assert len(cases)==len(exports);budgets=0
 for c,o in zip(cases,exports):
  assert o['Hex']==c['Hex'];parser=parse_base if c['Kind']=='Baseline' else parse_delta;v=parser(o['Hex']);assert v==c['Expected'];charge=v['Decoded'] if c['Kind']=='Baseline' else sum(row['Decoded'] for row in v);assert charge==o['Decoded']
  if charge:
   try:parser(o['Hex'],max_decoded=charge-1)
   except AssertionError:budgets+=1
   else:raise AssertionError('one-less budget accepted')
 print('Verified',len(cases),'complete TANO wires and',budgets,'independent one-less charges')
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--generate');a.add_argument('--peer');a.add_argument('--verify');x=a.parse_args()
 malformed_checks()
 if x.generate:generate(x.generate)
 else:verify(x.peer,x.verify)
