"""Independent literal-bit/presence/provenance and world-lifecycle receipt oracle."""
import json,struct,sys
from pathlib import Path
FIELDS=[0,1,4,5,7,8,9,10,11,12]
BITS=[0,0x80000000,0x7f800000,0xff800000,0x7fc01234,0x7f800001,0xffc0abcd,0x3f800000,0xbf800000,1,0x80000001,0x7f7fffff]
def delta(declared,updates):
 body=b''.join(struct.pack('<H'+('Q' if f==6 else 'I'),f,b) for f,b in updates)
 return dict(Kind='Delta',Wire=(struct.pack('<HIQIBIH',5,0x12862153,42,0x4352454f,4,len(body)+2,declared)+body).hex().upper())
def generate():
 cases=[]
 for c in range(128):
  initial=[[6,18446744073709551615]] if c==0 else [[f,0] for f in FIELDS] if c==1 else [[FIELDS[c%10],BITS[c%12]],[FIELDS[c%10],BITS[(c+1)%12]],[FIELDS[(c+3)%10],BITS[(c+2)%12]]]
  actions=[delta(65535,initial),delta(0,[[11,BITS[(c+4)%12]],[6,c]]),dict(Kind='End'),dict(Kind='Contain'),delta(1,[[f,BITS[(c+i)%12]] for i,f in enumerate(FIELDS)]),dict(Kind='Destroy'),dict(Kind='Create'),delta(0,[[0,0],[7,0x80000000],[11,1]]),dict(Kind='Reset'),delta(65535,[[6,18446744073709551615]]),dict(Kind='End')]
  cases.append(dict(Actions=actions))
 return cases

def parse(wire):
 raw=bytes.fromhex(wire);count,op,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',raw)
 assert (count,op,oid,tag,view,size)==(5,0x12862153,42,0x4352454f,4,len(raw)-23)
 pos=25;updates=[]
 while pos<len(raw):
  f=struct.unpack_from('<H',raw,pos)[0];pos+=2;assert f in FIELDS+[6]
  n=8 if f==6 else 4;b=struct.unpack_from('<Q' if f==6 else '<I',raw,pos)[0];pos+=n;updates.append((f,b))
 assert pos==len(raw) and updates
 return declared,updates

def model(cases):
 out=[]
 for case in cases:
  generation=1;registry_revision=1;creation=1;handle=1;phase=0;known={};pub=0;state=0;previous=None;rows=[]
  for action in case['Actions']:
   kind=action['Kind'];source_current=previous is not None and kind in ('End','Contain');old_current=False
   if kind=='Delta':
    declared,updates=parse(action['Wire']);pub+=1;state+=1
    for ordinal,(f,b) in enumerate(updates):known[f]=(b,dict(Revision=state,DeclaredCount=declared,Ordinal=ordinal,Origin=1))
   elif kind=='End':phase=1;registry_revision+=1;handle=registry_revision
   elif kind=='Contain':registry_revision+=1;handle=registry_revision
   elif kind=='Destroy':known={};state=0
   elif kind=='Create':registry_revision+=1;creation=registry_revision;handle=creation;phase=0
   elif kind=='Reset':generation+=1;registry_revision+=1;creation=registry_revision;handle=creation;phase=0;known={};state=0
   else:raise AssertionError(kind)
   current=None
   if state:
    fields=[]
    for f in FIELDS:
     bits,prov=known.get(f,(None,None));cls=0 if bits is None else 2 if bits&0x7f800000==0x7f800000 else 1
     eligibility=0 if cls==0 else 1 if cls==2 else 2 if phase==0 else 3
     fields.append(dict(Index=f,RawBits=bits,Provenance=prov,Kind=cls,Eligibility=eligibility))
    current=dict(Generation=generation,SceneGeneration=generation,CreationRevision=creation,HandleRevision=handle,PublicationRevision=pub,StateRevision=state,Phase=phase,IsOperationalPhase=phase==1,DecodedBytes=544,SourceKnownMask=sum(1<<f for f in known),Fields=fields)
   rows.append(dict(Kind=kind,Current=current,OldCurrent=old_current,SourceCurrent=source_current,FinalizedRead=current is not None and phase==1));previous=current
  out.append(rows)
 return out
if __name__=='__main__':
 command,path,*rest=sys.argv[1:];cases=generate()
 if command=='generate':Path(path).write_text(json.dumps(cases,separators=(',',':'))+'\n',encoding='utf-8')
 else:
  assert json.loads(Path(path).read_text())==cases,'fixture regeneration'
  got=json.loads(Path(rest[0]).read_text());assert got['Cases']==model(cases),'independent raw/presence/provenance/lifecycle receipt model'
  print('PASS received movement cases',len(cases),'actions',sum(len(c['Actions']) for c in cases),'slot-observations',sum(10 for c in model(cases) for a in c if a['Current']),'checks',got['Checks'])
