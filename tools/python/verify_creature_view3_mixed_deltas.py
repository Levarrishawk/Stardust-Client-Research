"""Independent hand-packed mixed CREO:3 wire/state fixtures using accepted policy oracles."""
import argparse,json,math,struct
from pathlib import Path
from verify_creature_view3_baselines import fixture as baseline
from verify_creature_view3_wounds_deltas import apply as strict_apply
from verify_creature_wounds_legacy_policy import emulate as legacy_apply
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
ID=0xF122334455667788
SCALARS=dict(Posture=255,Rank=254,LinkId=0xFE02030405060708,HeightBits=0xBFE00000,Shock=0xF1223344,States=0xF0E1D2C3B4A59687)
WIDTH={11:('B','Posture'),12:('B','Rank'),13:('Q','LinkId'),14:('I','HeightBits'),15:('I','Shock'),16:('Q','States')}


def wound(n,c,ops):
    b=P('HII',17,n,c)
    for k,i,v in ops:
        b+=P('B',k)
        if k!=4:b+=P('H',len(v) if k==3 else i)
        b+=P('i'*len(v),*v)
    return b


def scalar(index,value):return P('H',index)+P(WIDTH[index][0],value)


def packet(fields,count=0):return P('HIQIBIH',5,0x12862153,ID,0x4352454F,3,len(fields)+2,count)+fields


def golden(legacy):
    return scalar(11,255)+wound(1,1,[(2,0,[-2147483648])])+scalar(14,0x80000000)+wound(4 if legacy else 1,5 if legacy else 2,[(3,0,[-1,0,7])])+scalar(16,0xF0E1D2C3B4A59687)+scalar(11,7)+wound(1,6 if legacy else 3,[(0,1,[])])+scalar(12,254)+scalar(13,0xFE02030405060708)+scalar(15,0xF1223344)


def parse(wire,legacy):
    if len(wire)<25:raise ValueError('outer truncation')
    h=struct.unpack_from('<HIQIBIH',wire)
    if h[:5]!=(5,0x12862153,ID,0x4352454F,3) or h[5]!=len(wire)-23:raise ValueError('outer')
    pos=25;out=[];commands=values=scalars=0
    def read(fmt):
        nonlocal pos
        size=struct.calcsize('<'+fmt)
        if pos+size>len(wire):raise ValueError('field truncation')
        v=struct.unpack_from('<'+fmt,wire,pos);pos+=size;return v
    while pos<len(wire):
        if len(out)>=32:raise ValueError('field cap')
        index=read('H')[0]
        if index in WIDTH:
            scalars+=1
            if scalars>24:raise ValueError('scalar cap')
            v=read(WIDTH[index][0])[0]
            if index==14 and not math.isfinite(struct.unpack('<f',P('I',v))[0]):raise ValueError('height')
            out.append((index,v));continue
        if index!=17:raise ValueError('unsupported field')
        n,c=read('II');commands+=n
        if commands>64:raise ValueError('command cap')
        cursor=0;ops=[]
        while cursor<n:
            kind=read('B')[0]
            if kind not in range(5):raise ValueError('command')
            index=read('H')[0] if kind!=4 else 0
            k=index if kind==3 else 1 if kind in (1,2) else 0
            values+=k
            if values>64:raise ValueError('values cap')
            entries=list(read('i'*k));ops.append((kind,0 if kind==3 else index,entries));cursor+=1+(k if legacy and kind==3 else 0)
        out.append((17,(n,c,ops)))
    return out


def apply(updates,legacy):
    scalars=dict(SCALARS);counter=0;values=[-2,2147483647,-2]
    for index,value in updates:
        if index!=17:scalars[WIDTH[index][1]]=value;continue
        n,c,ops=value
        result=legacy_apply(counter,values,[(n,c,ops)],16) if legacy else strict_apply(counter,values,[(c,ops)],16)
        counter,values=result['Counter'],result['Values']
    return dict(ObjectId=ID,**scalars,WoundsCounter=counter,Wounds=values)


def initial():return dict(ObjectId=ID,**SCALARS,WoundsCounter=0,Wounds=[-2,2147483647,-2])


def cases():
    return [('strict',False,packet(golden(False))),('legacy',True,packet(golden(True))),('legacy-count65535',True,packet(golden(True),65535)),
        ('wrong-strict',False,packet(golden(True))),('wrong-legacy',True,packet(golden(False))),
        ('latecounter',False,packet(scalar(11,8)+wound(1,8,[(2,0,[7])])+scalar(12,7))),
        ('lategrowth',True,packet(scalar(11,8)+wound(1,1,[(1,65535,[7])])+scalar(12,7))),
        ('lateunknown',False,packet(scalar(11,8)+P('H',10))),('latefloat',True,packet(scalar(11,8)+scalar(14,0x7FC00000))),
        ('latetruncated',False,packet(scalar(11,8)+P('H',13)+b'\x01')),
        ('repeatedheight',False,packet(scalar(14,0x80000000)+scalar(14,0x3FC00000)+scalar(14,0x80000000))),
        ('empty',True,packet(b'',65535)),('equalstrict',False,packet(scalar(11,8)+wound(1,1,[(2,0,[-2])])+scalar(11,9))),
        ('equallegacy',True,packet(scalar(11,8)+wound(1,1,[(2,0,[-2])])+scalar(11,9))),
        ('repeatedlegacy',True,packet(wound(1,1,[(2,0,[-2])])+scalar(11,8)+wound(1,2,[(2,0,[7])]))),
        ('rawcountoverflow',False,packet(scalar(11,8)+wound(0xFFFFFFFF,1,[]))),
        ('latevaluescap',True,packet(scalar(11,8)+wound(66,66,[(3,0,list(range(65)))])))]


def main():
    p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
    if a.generate:
        a.generate.mkdir(parents=True,exist_ok=True);specs=[]
        b=bytearray(baseline());b[-16:-12]=P('I',0);(a.generate/'baseline.bin').write_bytes(b)
        for name,legacy,wire in cases():
            (a.generate/(name+'.bin')).write_bytes(wire);specs.append(dict(Name=name,File=name+'.bin',Policy=1 if legacy else 0))
        (a.generate/'cases.json').write_text(json.dumps(specs),encoding='utf-8')
    if a.verify:
        expected=[]
        for name,legacy,wire in cases():
            decoded=False
            try:
                updates=parse(wire,legacy);decoded=True
                assert (a.verify/(name+'.bin')).read_bytes()==wire,name
                result=apply(updates,legacy);expected.append(dict(Name=name,Decoded=True,Accepted=True,**result))
            except ValueError:expected.append(dict(Name=name,Decoded=decoded,Accepted=False,**initial()))
        actual=json.loads((a.verify/'results.json').read_text());assert actual==expected,(actual,expected)
        assert json.loads((a.verify/'seed.json').read_text())==initial()
        print(f'Independent mixed baseline/ordered-state/wire verification passed for {len(expected)} cases.')


if __name__=='__main__':main()
