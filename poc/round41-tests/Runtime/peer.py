"""Independent supported union using frozen Python wire/list/map oracles; all data synthetic."""
import ast,copy,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OLD=runpy.run_path(str(ROOT/'poc/round40-tests/Runtime/peer.py'));BASE=OLD['BASE'];OID=OLD['OID'];P=OLD['P']
MAP=runpy.run_path(str(ROOT/'poc/round40-tests/Networking/peer.py'))
source=(ROOT/'poc/round40-tests/Networking/peer.py').read_text();tree=ast.parse(source)
node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='oracle')
mapped_namespace=dict(MAP);exec(ast.get_source_segment(source,node).replace('32*len(events)','64*len(events)'),mapped_namespace)
MAPPED=mapped_namespace['oracle']
def rawentries(m):return [dict(Key=e['Name']['Hex'].lower(),Extended=e['Name']['Extended'],BaseValue=e['BaseValue'],Bonus=e['Bonus'],RawCommand=e['RawCommand']) for e in m['SkillMods']['Values']]
def project(m,entries,counter):
    m=copy.deepcopy(m);m['SkillMods']=dict(Counter=counter,Values=[dict(RawCommand=0,Name=dict(Hex=e['Key'].upper(),Extended=e['Extended']),BaseValue=e['BaseValue'],Bonus=e['Bonus']) for e in entries]);return m
def semantic(m):return 72+4*len(m['Encumbrances']['Values'])+16*len(m['SpaceMissionObjects']['Values'])+16*len(m['SkillMods']['Values'])+sum(len(bytes.fromhex(e['Name']['Hex'])) for e in m['SkillMods']['Values'])
def encode(us):
    body=b''
    for u in us:
        if u['Index']!=3:body+=OLD['encode']([u])[25:];continue
        v=u['Skill'];body+=P('HII',3,v['Extent'],v['Counter'])
        for c in v['Commands']:body+=P('B',c['Opcode'])+BASE['string'](dict(Hex=c['Key'],Extended=c['Extended']))+P('ii',c['BaseValue'],c['Bonus'])
    return BASE['envelope'](body,'Delta')
def parse(raw):
    if isinstance(raw,str):raw=bytes.fromhex(raw)
    if len(raw)<25:raise ValueError('header')
    header=struct.unpack_from('<HIQIBIH',raw)
    if header[:6]!=(5,0x12862153,OID,0x4352454f,4,len(raw)-23):raise ValueError('envelope')
    pos=25;us=[];commands=values=keys=mapcommands=maxkey=0
    def get(fmt):
        nonlocal pos
        n=struct.calcsize('<'+fmt)
        if pos+n>len(raw):raise ValueError('cut')
        v=struct.unpack_from('<'+fmt,raw,pos)[0];pos+=n;return v
    while pos<len(raw):
        start=pos;field=get('H')
        if field==3:
            extent=get('I');counter=get('I');cs=[]
            for _ in range(extent):
                op=get('B')
                if op>2:raise ValueError('map opcode')
                size=get('H');extended=size==65535
                if extended:size=get('I')
                if size>1048576 or pos+size>len(raw):raise ValueError('key')
                key=raw[pos:pos+size];pos+=size;commands+=1;mapcommands+=1;keys+=size;maxkey=max(maxkey,size)
                cs.append(dict(Opcode=op,Key=key.hex(),Extended=extended,BaseValue=get('i'),Bonus=get('i')))
            us.append(dict(Index=3,Skill=dict(Extent=extent,Counter=counter,Commands=cs)))
        elif field==2:
            extent=get('I');counter=get('I');cursor=0;cs=[]
            while cursor<extent:
                op=get('B');cursor+=1;i=0;vs=[]
                if op<=2:i=get('H')
                if op in (1,2):vs=[get('i')]
                elif op==3:n=get('H');cursor+=n;vs=[get('i') for _ in range(n)]
                elif op not in (0,4):raise ValueError('vector opcode')
                commands+=1;values+=len(vs);cs.append(dict(Opcode=op,Index=i,Values=vs))
            us.append(dict(Index=2,Extent=extent,Counter=counter,Commands=cs))
        elif field==6 or field in BASE['FLOATS']:us.append(dict(Index=field,Bits=get('Q' if field==6 else 'I')))
        else:raise ValueError('unsupported')
    if not us:raise ValueError('empty')
    return us,dict(Payload=len(raw)-23,Fields=len(us),Commands=commands,Values=values,MaxKey=maxkey,Keys=keys,Decoded=len(raw)-25+10+24*mapcommands,Work=len(us)+commands+values+keys)
def oracle(raw,us,mc):
    seed=MAP['oracle'](rawentries(raw),raw['SkillMods']['Counter'],[]);m=project(raw,seed['Initial'],raw['SkillMods']['Counter'])
    initial=copy.deepcopy(m);seedwork=BASE['modelcost'](raw)['SeedWork']+seed['SeedWork']+BASE['modelcost'](m)['SeedWork']
    seedcost=BASE['modelcost'](raw);seedcost.update(StateBytes=semantic(raw),OutputBytes=semantic(raw),Work=seedwork,Events=0,Updates=4096)
    cost=BASE['modelcost'](m);peak=semantic(m);outpeak=peak;elements=cost['Elements'];total=cost['TotalElements'];strings=cost['Strings'];maxstr=cost['MaxString'];work=cost['SeedWork']+mc['Work'];events=[];eventbytes=0
    def blank():return dict(Kind=None,Field=None,UpdateOrdinal=None,Opcode=None,Index=None,OldBits=None,NewBits=None,OldValue=None,NewValue=None,Name=None,OldBase=None,OldBonus=None,NewBase=None,NewBonus=None,LogicalBytes=None)
    for ordinal,u in enumerate(us):
        if u['Index']!=3:
            _,child_mc=OLD['parse'](OLD['encode']([u]));expected,es,sc=OLD['apply'](m,[u],child_mc['Work']);extra=semantic(m)-BASE['modelcost'](m)['StateBytes'];work+=BASE['modelcost'](m)['SeedWork']+sc['Work'];peak=max(peak,sc['StateBytes']+extra);outpeak=max(outpeak,sc['OutputBytes']+extra+eventbytes);elements=max(elements,sc['Elements']);total=max(total,sc['TotalElements']);m=expected['Model']
            for e in es:
                v=blank();v.update(e);v['UpdateOrdinal']=ordinal;v['LogicalBytes']=48;events.append(v);eventbytes+=48
        else:
            result=MAP['oracle'](rawentries(m),m['SkillMods']['Counter'],[u['Skill']]);mapped=MAPPED(rawentries(m),m['SkillMods']['Counter'],[u['Skill']]);other=semantic(m)-(8+16*len(m['SkillMods']['Values'])+BASE['modelcost'](m)['Strings']);work+=result['WorkUsed'];peak=max(peak,other+result['PeakBytes']);outpeak=max(outpeak,other+mapped['PeakOutput']+eventbytes)
            elements=max(elements,result['PeakCount']);total=max(total,result['PeakCount']+len(m['Encumbrances']['Values'])+len(m['SpaceMissionObjects']['Values']));strings=max(strings,result['PeakKeys']);maxstr=max(maxstr,result['PeakKey']);m=project(m,result['Entries'],result['Counter'])
            for e in result['Events']:
                v=blank();v.update(Kind=2,Field=3,UpdateOrdinal=ordinal,Opcode=e['Opcode'],Name=dict(Hex=e['Key'].upper(),Extended=e['Extended']),OldBase=e['OldBase'],OldBonus=e['OldBonus'],NewBase=e['NewBase'],NewBonus=e['NewBonus'],LogicalBytes=64+len(bytes.fromhex(e['Key'])));events.append(v);eventbytes+=v['LogicalBytes']
    work+=BASE['modelcost'](m)['SeedWork']+len(events)
    statecost=dict(Updates=len(us),Elements=elements,TotalElements=total,MaxString=maxstr,Strings=strings,StateBytes=peak,Events=len(events),OutputBytes=outpeak,Work=work)
    return dict(Model=initial,Revision=1,DecodedBytes=semantic(initial)),dict(Model=m,Revision=2,DecodedBytes=semantic(m)),events,statecost,seedcost
def generate():
    cases=[]
    for i,c in enumerate(MAP['generate']()):
        m=BASE['seed']();m['DeclaredCount']=[0,14,65535][i%3];m['SkillMods']=dict(Counter=c['SeedCounter'],Values=[dict(RawCommand=e['RawCommand'],Name=dict(Hex=e['Key'].upper(),Extended=e['Extended']),BaseValue=e['BaseValue'],Bonus=e['Bonus']) for e in c['Seed']])
        raw=bytearray.fromhex(c['Wire']);struct.pack_into('<Q',raw,6,OID);skill,_=parse(raw)
        us=[dict(Index=0,Bits=0x7fa00001),dict(Index=2,Extent=1,Counter=0,Commands=[dict(Opcode=2,Index=4,Values=[-7])])]
        for u in skill:us += [u,dict(Index=6,Bits=(1<<64)-1)]
        us += [dict(Index=0,Bits=0x80000000)]
        wire=encode(us);parsed,mc=parse(wire);_,_,_,sc,seedcost=oracle(m,parsed,mc)
        prefixes=[]
        if len(wire)<2048:
            for at in range(25,len(wire)):
                partial=bytearray(wire[:at]);struct.pack_into('<I',partial,19,at-23)
                try:parse(partial)
                except ValueError:pass
                else:prefixes.append(at)
        cases.append(dict(ValidPrefixes=prefixes,Name=c['Name'],Baseline=BASE['envelope'](BASE['baseline_fields'](m),'Baseline',m['DeclaredCount']).hex().upper(),Hex=wire.hex().upper(),CodecCost=mc,StateCost=sc,SeedCost=seedcost))
    return dict(Synthetic=True,Cases=cases)
def verify(fixture,receipt):
    data=json.loads(Path(fixture).read_text());assert data==generate();actual=json.loads(Path(receipt).read_text());assert len(actual['Records'])==len(data['Cases']);n=0
    for c,r in zip(data['Cases'],actual['Records']):
        raw,_=BASE['parse'](bytes.fromhex(c['Baseline']),'Baseline');us,mc=parse(c['Hex']);initial,current,events,sc,_=oracle(raw,us,mc)
        assert r['Name']==c['Name'] and r['Hex']==c['Hex'] and r['Initial']==initial and r['Current']==current,(c['Name'],'models')
        assert r['Events']==events and r['WorkUsed']==sc['Work'] and r['DecodedOutputBytes']==current['DecodedBytes']+sum(e['LogicalBytes'] for e in events),(c['Name'],'events/work')
        reseed=copy.deepcopy(initial);reseed['Revision']=3
        assert r['Reseed']==reseed and r['DeclaredCount']==65535,(c['Name'],'reseed/metadata')
        n+=len(events)
    print(json.dumps(dict(passed=True,sequences=len(data['Cases']),events=n,negatives=actual['Negatives'],budgets=actual['Budgets'],ownership=actual['Ownership']),indent=2))
if __name__=='__main__':
    if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n');print('Generated122 independent supported cases')
    else:verify(sys.argv[2],sys.argv[3])
