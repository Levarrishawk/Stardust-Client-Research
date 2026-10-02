"""Independent synthetic composite peer and ordered state oracle; no replacement-code reads."""
import argparse,json,struct,math
from pathlib import Path
from verify_creature_view3_baselines import fixture
from verify_creature_view3_components_deltas import apply as components_apply,field as components_field
from verify_creature_view3_wounds_deltas import apply as strict_apply
from verify_creature_wounds_legacy_policy import emulate as legacy_apply
from verify_creature_view3_mixed_deltas import scalar,wound,SCALARS,WIDTH
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
ID=0xF122334455667788;C=0xF1234567


def message(fields,count=0):return P('HIQIBIH',5,0x12862153,ID,0x4352454F,3,len(fields)+2,count)+fields


def golden(legacy):
    return scalar(11,255)+components_field(C+2,[(1,17),(0,-1)])+wound(1,1,[(2,0,[-2147483648])])+scalar(14,0x80000000)+components_field(C+9,[(1,2147483647)])+wound(4 if legacy else 1,5 if legacy else 2,[(3,0,[-1,0,7])])+scalar(16,0xF0E1D2C3B4A59687)+scalar(11,9)+components_field(C+10,[(2,0)])+wound(1,6 if legacy else 3,[(0,1,[])])+components_field(C+12,[(1,-2147483648),(1,2147483647)])+scalar(12,254)+scalar(13,0xFE02030405060708)+scalar(15,0xF1223344)


def cases():
    prefix=scalar(11,8)+components_field(C+1,[(1,17)])+wound(1,1,[(2,0,[7])])
    out=[dict(Name='strict',Policy=0,Fields=golden(False)),dict(Name='legacy',Policy=1,Fields=golden(True)),dict(Name='rawcount65535',Policy=1,Fields=golden(True),Declared=65535),
      dict(Name='wrong-strict',Policy=0,Fields=golden(True)),dict(Name='wrong-legacy',Policy=1,Fields=golden(False)),
      dict(Name='latecomponentgrowth',Policy=0,Fields=prefix+components_field(C+2,[(1,23)]),ComponentsElements=3),
      dict(Name='latewoundgrowth',Policy=1,Fields=prefix+wound(1,2,[(1,65535,[7])])),
      dict(Name='latecounter',Policy=0,Fields=prefix+wound(1,9,[(2,0,[7])])),
      dict(Name='lateunsupported',Policy=0,Fields=prefix+P('H',6)),dict(Name='latetruncated',Policy=0,Fields=prefix+P('H',13)+b'\x01'),
      dict(Name='latefloat',Policy=1,Fields=prefix+scalar(14,0x7FC00000)),
      dict(Name='component-key-budget',Policy=0,Fields=golden(False),ComponentsKeys=4),dict(Name='wound-value-budget',Policy=1,Fields=golden(True),WoundsValues=3),
      dict(Name='wrapboth',Policy=0,Fields=scalar(11,8)+components_field(0,[(1,17)])+wound(1,0,[(2,0,[7])])+scalar(12,7),ComponentsCounter=0xFFFFFFFF,WoundsCounter=0xFFFFFFFF),
      dict(Name='noop-wrap',Policy=1,Fields=components_field(1,[(1,-1)])+scalar(11,8)+wound(1,0,[(2,0,[-2])]),ComponentsCounter=0xFFFFFFFF,WoundsCounter=0xFFFFFFFF),
      dict(Name='componentfuture',Policy=0,Fields=components_field(C+99,[(1,17)])+scalar(11,8)),
      dict(Name='componentstale',Policy=0,Fields=components_field(1,[(1,17)])+scalar(11,8)),
      dict(Name='empty',Policy=1,Fields=b'',Declared=65535)]
    for s in out:
        fields=s.pop('Fields');s['WireHex']=message(fields,s.pop('Declared',0)).hex()
    return out


def parse(wire,spec):
    h=struct.unpack_from('<HIQIBIH',wire)
    if h[:5]!=(5,0x12862153,ID,0x4352454F,3) or h[5]!=len(wire)-23:raise ValueError('header')
    pos=25;ops=[];scalars=cc=ck=wc=wv=0
    def read(fmt):
        nonlocal pos
        size=struct.calcsize('<'+fmt)
        if pos+size>len(wire):raise ValueError('truncation')
        r=struct.unpack_from('<'+fmt,wire,pos);pos+=size;return r
    while pos<len(wire):
        if len(ops)>=32:raise ValueError('fields')
        index=read('H')[0]
        if index in WIDTH:
            scalars+=1
            if scalars>24:raise ValueError('scalars')
            v=read(WIDTH[index][0])[0]
            if index==14 and not math.isfinite(struct.unpack('<f',P('I',v))[0]):raise ValueError('float')
            ops.append((index,v));continue
        if index not in (5,17):raise ValueError('index')
        n,c=read('II');commands=[]
        if index==5:
            cc+=n
            if cc>64:raise ValueError('component commands')
            for _ in range(n):
                k=read('B')[0]
                if k not in (0,1,2):raise ValueError('component command')
                key=0
                if k!=2:
                    ck+=1
                    if ck>spec.get('ComponentsKeys',64):raise ValueError('component keys')
                    key=read('i')[0]
                commands.append((k,key))
        else:
            wc+=n
            if wc>64:raise ValueError('wound commands')
            cursor=0
            while cursor<n:
                k=read('B')[0]
                if k not in range(5):raise ValueError('wound command')
                i=read('H')[0] if k!=4 else 0;count=i if k==3 else 1 if k in (1,2) else 0;wv+=count
                if wv>spec.get('WoundsValues',64):raise ValueError('wound values')
                vals=list(read('i'*count));commands.append((k,0 if k==3 else i,vals));cursor+=1+(count if spec['Policy']==1 and k==3 else 0)
        ops.append((index,(n,c,commands)))
    return ops


def seed(spec):return dict(ObjectId=ID,**SCALARS,WoundsCounter=spec.get('WoundsCounter',0),Wounds=[-2,2147483647,-2],ComponentsCounter=spec.get('ComponentsCounter',C),Components=[-2147483648,-1],RawCounter=C,RawComponents=[-1,-2147483648,-1])


def apply(spec,updates):
    out=seed(spec)
    for index,v in updates:
        if index in WIDTH:out[WIDTH[index][1]]=v
        elif index==5:
            r=components_apply(out['ComponentsCounter'],out['Components'],[v],spec.get('ComponentsElements',16));out['ComponentsCounter']=r['Counter'];out['Components']=r['Keys']
        else:
            n,c,commands=v
            r=legacy_apply(out['WoundsCounter'],out['Wounds'],[v],16) if spec['Policy']==1 else strict_apply(out['WoundsCounter'],out['Wounds'],[(c,commands)],16)
            out['WoundsCounter']=r['Counter'];out['Wounds']=r['Values']
    return out


def main():
    p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
    specs=cases();b=bytearray(fixture());b[85:89]=P('i',-1);b[-16:-12]=P('I',0)
    if a.generate:
        a.generate.parent.mkdir(parents=True,exist_ok=True);a.generate.write_text(json.dumps(dict(BaselineHex=b.hex(),Cases=specs)),encoding='utf-8')
    if a.verify:
        expected=[]
        for s in specs:
            decoded=False;wire=bytes.fromhex(s['WireHex'])
            try:
                updates=parse(wire,s);decoded=True;assert (a.verify/(s['Name']+'.bin')).read_bytes()==wire,s['Name']
                final=apply(s,updates);expected.append(dict(Name=s['Name'],Decoded=True,Accepted=True,State=final))
            except ValueError:expected.append(dict(Name=s['Name'],Decoded=decoded,Accepted=False,State=seed(s)))
        actual=json.loads((a.verify/'results.json').read_text());assert actual==expected,'Composite independent result mismatch'
        assert json.loads((a.verify/'seed.json').read_text())==seed({})
        print(f'Independent composite baseline, exact wire and ordered state verified for {len(expected)} cases.')


if __name__=='__main__':main()
