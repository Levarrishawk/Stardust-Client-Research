"""Independent synthetic producer arithmetic and literal CREO4 observer-delta oracle."""
import json,struct,sys
from pathlib import Path
BITS=[0,0x80000000,0x3f800000,0xbf800000,0x3dcccccd,0x7f800000,0xff800000,0x7fa00001,0x7fc12345,0xffffffff]
OID=0xf122334455667788
INITIAL=[0x7fa00101+i for i in range(10)]
def generate():
 rows=[]
 for a in BITS:
  for m in BITS:
   for own in [False,True]:
    # Zero test uses raw sign-insensitive zero, without unpack/repack canonicalizing NaN payloads.
    az=a&0x7fffffff==0;mz=m&0x7fffffff==0
    aa=0x3dcccccd if az and mz else a;mm=0x3dcccccd if mz and not own else m;t=0x80000000
    us=[dict(Index=1,Bits=aa),dict(Index=5,Bits=mm),dict(Index=10,Bits=t)]
    body=b''.join(struct.pack('<HI',u['Index'],u['Bits']) for u in us)
    wire=struct.pack('<HIQIBIH',5,0x12862153,OID,0x4352454f,4,len(body)+2,3)+body
    result=INITIAL.copy();result[1]=aa;result[3]=mm;result[7]=t
    rows.append(dict(Name=f'{a:08X}-{m:08X}-{int(own)}',Acceleration=a,Speed=m,SendSelf=own,Wire=wire.hex().upper(),Updates=us,Initial=INITIAL,Result=result))
 return dict(Synthetic=True,Cases=rows)
def verify(fixture,receipt):
 data=json.loads(Path(fixture).read_text());assert data==generate();out=json.loads(Path(receipt).read_text());assert len(out['Records'])==200
 for c,r in zip(data['Cases'],out['Records']):
  assert r==dict(Name=c['Name'],ObjectId=OID,DeclaredCount=3,Updates=c['Updates'],FloatBits=c['Result'],ListenTo=(1<<64)-1,StateRevision=2,BaselineDeclaredCount=65535),(c['Name'],'independent raw bits/width/ordering/model')
 assert out['Negatives']==10200 and out['Budgets']==1800 and out['Ownership']==400
 print(json.dumps(dict(passed=True,cases=200,negatives=out['Negatives'],budgets=out['Budgets'],ownership=out['Ownership']),indent=2))
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n')
 else:verify(sys.argv[2],sys.argv[3])
