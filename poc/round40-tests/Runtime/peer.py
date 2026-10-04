"""Independent wire/full-model/counter/event/peak/work oracle for the supported CREO4 union."""
import copy,json,random,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
BASE=runpy.run_path(str(ROOT/'poc/round38-tests/Networking/peer.py'))
ENC=runpy.run_path(str(ROOT/'poc/round39-tests/Networking/peer.py'))
FLOATS=BASE['FLOATS'];OID=BASE['OID'];P=BASE['P'];MASK=0xffffffff
def encode(updates,count=65535):
    body=b''
    for u in updates:
        if u['Index']!=2:body+=P('HQ' if u['Index']==6 else 'HI',u['Index'],u['Bits']);continue
        body+=P('HII',2,u['Extent'],u['Counter'])
        for c in u['Commands']:
            body+=P('B',c['Opcode'])
            if c['Opcode']!=4:body+=P('H',len(c['Values']) if c['Opcode']==3 else c['Index'])
            body+=P('i'*len(c['Values']),*c['Values'])
    return BASE['envelope'](body,'Delta',count)
def parse(wire):
    raw=bytes.fromhex(wire) if isinstance(wire,str) else wire
    if len(raw)<25:raise ValueError('header')
    n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
    if (n,op,oid,tag,view,size)!=(5,0x12862153,OID,0x4352454f,4,len(raw)-23):raise ValueError('envelope')
    pos=25;updates=[];commands=values=0
    def get(fmt):
        nonlocal pos
        width=struct.calcsize('<'+fmt)
        if pos+width>len(raw):raise ValueError('truncated')
        result=struct.unpack_from('<'+fmt,raw,pos)[0];pos+=width;return result
    while pos<len(raw):
        index=get('H')
        if index==2:
            extent=get('I');counter=get('I');cursor=0;cs=[]
            while cursor<extent:
                op=get('B');cursor+=1;commands+=1;i=0;vs=[]
                if op<=2:i=get('H')
                if op in (1,2):vs=[get('i')]
                elif op==3:
                    n=get('H');cursor+=n;vs=[get('i') for _ in range(n)]
                elif op not in (0,4):raise ValueError('opcode')
                values+=len(vs);cs.append(dict(Opcode=op,Index=i,Values=vs))
            updates.append(dict(Index=2,Extent=extent,Counter=counter,Commands=cs))
        elif index==6 or index in FLOATS:updates.append(dict(Index=index,Bits=get('Q' if index==6 else 'I')))
        else:raise ValueError('unsupported')
    if not updates:raise ValueError('empty')
    return dict(ObjectId=oid,DeclaredCount=count,Updates=updates),dict(Payload=len(raw)-23,Fields=len(updates),Commands=commands,Values=values,Decoded=len(raw)-25+10,Work=len(updates)+commands+values)
def fullcost(m):return BASE['modelcost'](m)
def apply(model,updates,message_work):
    m=copy.deepcopy(model);events=[];cost=fullcost(m);work=cost['SeedWork']+message_work
    peak_bytes=cost['StateBytes'];peak_elements=cost['Elements'];peak_total=cost['TotalElements'];peak_output=cost['StateBytes']
    def gate(construction_count=None,added=0):
        nonlocal peak_bytes,peak_elements,peak_total,peak_output
        c=fullcost(m);peak_bytes=max(peak_bytes,c['StateBytes']);peak_elements=max(peak_elements,c['Elements']);peak_total=max(peak_total,c['TotalElements']);peak_output=max(peak_output,c['StateBytes']+48*len(events))
        if construction_count is not None:
            child=c['StateBytes']+4*(construction_count-len(m['Encumbrances']['Values']))+48*(len(events)-added)+24*added
            peak_output=max(peak_output,child)
    for ordinal,u in enumerate(updates):
        f=u['Index']
        if f!=2:
            old=m['ListenTo'] if f==6 else m['FloatBits'][FLOATS.index(f)]
            work+=3*fullcost(m)['SeedWork']+1
            if f==6:m['ListenTo']=u['Bits']
            else:m['FloatBits'][FLOATS.index(f)]=u['Bits']
            events.append(dict(Kind=0,Field=f,UpdateOrdinal=ordinal,Opcode=None,Index=None,OldBits=old,NewBits=u['Bits'],OldValue=None,NewValue=None));gate();continue
        a=m['Encumbrances']['Values'];counter=m['Encumbrances']['Counter'];work+=len(a)
        skip=min((u['Extent']-u['Counter']+counter)&MASK,u['Extent']);cursor=0
        for command in u['Commands']:
            ignored=cursor<skip;op=command['Opcode'];i=command['Index'];vs=command['Values'];cursor+=1+(len(vs) if op==3 else 0)
            if ignored:continue
            n=len(a);count=n;added=0;mutation=0;old=new=None
            if op==0 and i<n:count=n-1;added=1;mutation=n-i
            elif op==1:count=max(n,i)+1;added=1;mutation=count-n+max(0,n-i)
            elif op==2 and (i>=n or a[i]!=vs[0]):count=max(n,i+1);added=1;mutation=count-n+1
            elif op==3:count=len(vs);added=1;mutation=n+count
            elif op==4 and n:count=0;added=1;mutation=n
            if count>65536:raise ValueError('hard growth')
            work+=n+2+len(vs)+mutation+count+added
            if added:
                if op==0:old=a.pop(i);counter=(counter+1)&MASK
                elif op==1:a.extend([0]*max(0,i-n));a.insert(i,vs[0]);new=vs[0];counter=(counter+1)&MASK
                elif op==2:a.extend([0]*max(0,i+1-n));old=a[i];a[i]=vs[0];new=vs[0];counter=(counter+1)&MASK
                elif op==3:a=list(vs);counter=(counter+1+len(vs))&MASK
                else:a=[];counter=(counter+1)&MASK
                events.append(dict(Kind=1,Field=2,UpdateOrdinal=ordinal,Opcode=op,Index=i if op<=2 else None,OldBits=None,NewBits=None,OldValue=old,NewValue=new))
            m['Encumbrances']=dict(Counter=counter,Values=a);gate(n if op in (0,4) else count,added)
    work+=fullcost(m)['SeedWork']+len(events)
    finalcost=fullcost(m)
    statecost=dict(Updates=len(updates),Elements=peak_elements,TotalElements=peak_total,MaxString=cost['MaxString'],Strings=cost['Strings'],StateBytes=peak_bytes,Events=len(events),OutputBytes=peak_output,Work=work,SeedWork=cost['SeedWork'],SeedBytes=cost['StateBytes'])
    return dict(Model=m,Revision=2,DecodedBytes=finalcost['StateBytes']),events,statecost
def generate():
    cases=[]
    def add(name,m,updates):
        bw=BASE['envelope'](BASE['baseline_fields'](m),'Baseline',m['DeclaredCount']);wire=encode(updates)
        decoded,bc=BASE['parse'](bw,'Baseline');assert decoded==m
        plan,mc=parse(wire);_,_,sc=apply(m,plan['Updates'],mc['Work'])
        prefixes=[]
        if len(wire)<2048:
            for offset in range(25,len(wire)):
                partial=bytearray(wire[:offset]);struct.pack_into('<I',partial,19,offset-23)
                try:parse(partial)
                except ValueError:pass
                else:prefixes.append(offset)
        cases.append(dict(Name=name,Baseline=bw.hex().upper(),Hex=wire.hex().upper(),CodecCost=mc,StateCost=sc,ValidPrefixes=prefixes))
    for number,c in enumerate(ENC['generate']()):
        m=BASE['seed']();m['DeclaredCount']=[0,14,65535][number%3];m['Encumbrances']=dict(Counter=c['SeedCounter'],Values=c['Seed'])
        # Parse the independent narrow producer with a corrected fullwidth synthetic recipient.
        raw=bytearray.fromhex(c['Wire']);struct.pack_into('<Q',raw,6,OID);narrow,_=parse(raw)
        us=[dict(Index=0,Bits=0x7fa00001),dict(Index=6,Bits=0xfedcba9876543210)]
        for ix,u in enumerate(narrow['Updates']):us += [u,dict(Index=4,Bits=(0x80000000+ix)&MASK)]
        us += [dict(Index=0,Bits=0x80000000)]
        add('mixed-'+c['Name'],m,us)
    def vector(cs,counter=0,extent=None):
        extent=sum(1+(len(c['Values']) if c['Opcode']==3 else 0) for c in cs) if extent is None else extent
        return dict(Index=2,Extent=extent,Counter=counter,Commands=cs)
    def cmd(op,index=0,values=[]):return dict(Opcode=op,Index=index,Values=values)
    m=BASE['seed']();m['Encumbrances']=dict(Counter=0,Values=[])
    add('all-eleven-scalars',m,[dict(Index=f,Bits=(1<<64)-1 if f==6 else 0x7fc01234) for f in (*FLOATS,6)])
    add('grow-clear-interleaved',m,[dict(Index=1,Bits=0xffffffff),vector([cmd(1,30,[7]),cmd(4)],2),dict(Index=1,Bits=0)])
    m=BASE['seed']();m['Encumbrances']=dict(Counter=0,Values=[i for i in range(100)])
    add('large-clear-construction-peak',m,[vector([cmd(4)],1),dict(Index=6,Bits=0)])
    m=BASE['seed']();m['SkillMods']['Values']=[dict(RawCommand=255,Name=BASE['text'](b'\x00\xff'*32768,True),BaseValue=-1,Bonus=1)]
    add('untouched-extended-key',m,[dict(Index=7,Bits=0x7f800000),vector([],0,0),dict(Index=6,Bits=(1<<64)-1)])
    rng=random.Random(40)
    for ix in range(30):
        m=BASE['seed']();m['Encumbrances']=dict(Counter=0,Values=[]);us=[];counter=0
        for j in range(12):
            if j%2:
                f=rng.choice((*FLOATS,6));us.append(dict(Index=f,Bits=rng.getrandbits(64 if f==6 else 32)))
            else:
                op=rng.choice([1,2,3,4]);vs=[rng.randrange(-100,100)] if op in [1,2] else [rng.randrange(-100,100) for _ in range(rng.randrange(5))] if op==3 else []
                extent=1+(len(vs) if op==3 else 0);counter=(counter+extent)&MASK;us.append(vector([cmd(op,rng.randrange(12) if op<=2 else 0,vs)],counter))
        add('ordered-random-'+str(ix),m,us)
    malformed=[]
    for field in [3,13,14,65535]:malformed.append(BASE['envelope'](P('HI',0,7)+P('H',field)+b'\x00'*20,'Delta').hex().upper())
    for op in range(5,256):malformed.append(BASE['envelope'](P('HII',2,1,1)+P('B',op),'Delta').hex().upper())
    malformed.append(BASE['envelope'](b'','Delta').hex().upper())
    return dict(Synthetic=True,Cases=cases,Malformed=malformed)
def verify(fixture,receipt):
    data=json.loads(Path(fixture).read_text());assert data==generate();out=json.loads(Path(receipt).read_text());assert len(out['Records'])==len(data['Cases']);event_count=0
    for c,r in zip(data['Cases'],out['Records']):
        model,_=BASE['parse'](bytes.fromhex(c['Baseline']),'Baseline');plan,mc=parse(c['Hex']);expected,events,sc=apply(model,plan['Updates'],mc['Work'])
        assert r['Name']==c['Name'] and r['Hex']==c['Hex'] and r['Plan']==plan
        assert r['Initial']==dict(Model=model,Revision=1,DecodedBytes=fullcost(model)['StateBytes'])
        assert r['Current']==expected and r['Events']==events and r['WorkUsed']==sc['Work'] and r['DeclaredCount']==65535
        assert r['DecodedOutputBytes']==expected['DecodedBytes']+48*len(events)
        assert r['Reseed']==dict(Model=model,Revision=3,DecodedBytes=fullcost(model)['StateBytes']);event_count+=len(events)
    assert out['Negatives']>1000 and out['Budgets']>1000 and out['Ownership']>500
    print(json.dumps(dict(passed=True,sequences=len(data['Cases']),ordered_events=event_count,negatives=out['Negatives'],budgets=out['Budgets'],ownership=out['Ownership']),indent=2))
if __name__=='__main__':
    if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n');print('Generated independent composed fixtures')
    elif sys.argv[1]=='verify':verify(sys.argv[2],sys.argv[3])
    else:raise SystemExit('generate fixture | verify fixture receipt')
