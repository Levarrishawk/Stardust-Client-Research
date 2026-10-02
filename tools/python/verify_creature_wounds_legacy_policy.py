"""Instruction-anchored unsigned-register and vector-state oracle; synthetic fixtures only."""
import argparse,hashlib,json,struct
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
MASK=0xFFFFFFFF
ID=0xF122334455667788
SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS={'0043B1E8':'2BD9','0043B1EA':'03DA','0043B1EC':'3BD8','0043B1F1':'7605','0043B233':'FF45EC','0043B252':'40','0043B260':'0F8353010000','0043B2B8':'0F83E9000000','0043B2D8':'40','0043B35E':'7313','0043B367':'C745E400000000','0043B36E':'E8ADD20000','0043B3FD':'7307','0043B404':'749E','0043B41D':'FF4618','0043B430':'8D4301','0043B436':'C745E400000000','0043B43D':'E8DED10000','0043B528':'FF45EC','0043B579':'E8D2F7FFFF','0043B3C9':'C20400','0043ADA9':'FF4618','0043AEEE':'FF4018','0043A7FF':'0F84C2000000','0043A83D':'FF4618','0044863C':'7324','00448686':'8B12','0118AA4E':'FF5218','0118B065':'772B'}


def anchors(exe):
    import pefile
    assert hashlib.sha256(exe.read_bytes()).hexdigest().upper()==SHA
    pe=pefile.PE(str(exe),fast_load=True)
    for va,raw in ANCHORS.items():
        data=bytes.fromhex(raw); assert pe.get_data(int(va,16)-pe.OPTIONAL_HEADER.ImageBase,len(data))==data,va
    assert pe.get_data(0x43B5A0-pe.OPTIONAL_HEADER.ImageBase,20)==P('IIIII',0x43B29E,0x43B312,0x43B3CC,0x43B474,0x43B592)
    print('Pinned executable: 29 instruction anchors and five-case jump table verified.')


def packet(n,c,ops):
    b=P('HII',17,n,c)
    for k,i,v in ops:
        b+=P('B',k)
        if k!=4:b+=P('H',len(v) if k==3 else i)
        b+=P('i'*len(v),*v)
    return P('HIQIBIH',5,0x12862153,ID,0x4352454F,3,len(b)+2,65535)+b


def decode(wire):
    pos=25; updates=[]
    def read(fmt):
        nonlocal pos
        size=struct.calcsize('<'+fmt)
        if pos+size>len(wire): raise ValueError('truncation')
        result=struct.unpack_from('<'+fmt,wire,pos);pos+=size;return result
    while pos<len(wire):
        index,n,c=read('HII')
        if index!=17 or n>100000:raise ValueError('field/budget')
        cursor=0; ops=[]
        while cursor<n:
            k=read('B')[0]
            if k not in range(5):raise ValueError('command')
            i=read('H')[0] if k!=4 else 0
            count=i if k==3 else 1 if k in (1,2) else 0
            v=list(read('i'*count));ops.append((k,0 if k==3 else i,v));cursor+=1+(count if k==3 else 0)
        updates.append((n,c,ops))
    return updates


def emulate(local,values,updates,cap):
    values=list(values)
    for eax,ecx,ops in updates:
        # 0043B1DD..0043B1F6: EAX=N, ECX=C, EDX=local, EBX=N-C+local, unsigned JBE clamp.
        ebx=((eax-ecx)&MASK);ebx=(ebx+local)&MASK
        if ebx>eax:ebx=eax
        cursor=0
        for kind,index,entries in ops:
            execute=cursor>=ebx;cursor+=1+(len(entries) if kind==3 else 0)
            if not execute:continue
            if kind==0:
                if index>=len(values):continue
                del values[index];local=(local+1)&MASK
            elif kind==1:
                target=max(index,len(values))+1
                if target>cap:raise ValueError('bounded growth')
                values.extend([0]*max(0,index-len(values)));values.insert(index,entries[0]);local=(local+1)&MASK
            elif kind==2:
                if index<len(values) and values[index]==entries[0]:continue
                if index+1>cap:raise ValueError('bounded growth')
                values.extend([0]*max(0,index+1-len(values)));values[index]=entries[0];local=(local+1)&MASK
            elif kind==3:
                if len(entries)>cap:raise ValueError('bounded assign')
                values=list(entries);local=(local+len(entries)+1)&MASK
            elif kind==4:
                if not values:continue
                values=[];local=(local+1)&MASK
    return dict(Counter=local,Values=values)


def cases():
    replace=[(2,0,[7])]
    out=[('fresh',1,1,0,[9],replace,8),('equal',1,1,0,[9],[(2,0,[9])],8),('invalidremove',1,1,0,[9],[(0,65535,[])],8),
      ('futuregap',1,7,0,[9],replace,8),('whollyold',1,1,9,[9],replace,8),('wrapfresh',1,0,MASK,[9],replace,8),
      ('wrapoverlap',2,0,MASK,[9],[(2,0,[2]),(2,0,[7])],8),('underflowclamp',1,2,0,[9],replace,8),
      ('oldwrap',2,1,MASK,[9],[(2,0,[2]),(2,0,[7])],8),('zero',0,77,0,[9],[],8),
      ('insertgap',1,1,0,[9],[(1,3,[-8])],8),('replacegap',1,1,0,[9],[(2,3,[-8])],8),
      ('clearEmpty',1,1,0,[],[(4,0,[])],8),('clearNonempty',1,1,0,[9],[(4,0,[])],8),
      ('assignempty',1,1,0,[],[(3,0,[])],8),('assignsame',2,2,0,[9],[(3,0,[9])],8),
      ('assignfollow',5,5,0,[9],[(3,0,[1,2,3]),(2,1,[7])],8),('assignskipcross',5,5,2,[9],[(3,0,[1,2,3]),(2,0,[7])],8),
      ('assignend',4,4,0,[9],[(3,0,[1,2,3])],8),('assignovershoot',1,1,0,[9],[(3,0,[1,2,3])],8),
      ('badstrictarity',2,2,0,[9],[(3,0,[1,2,3]),(4,0,[])],8),
      ('lateGrowth',2,2,0,[9],[(2,0,[7]),(1,65535,[1])],8),('growMax',1,1,0,[9],[(2,65535,[-1])],65536),
      ('sameThenChange',2,2,0,[9],[(2,0,[9]),(2,0,[7])],8)]
    # Deterministic adversarial U32 counter relations, including high-bit/signed-compare discriminators.
    for local in [0,1,2,0x7FFFFFFF,0x80000000,0xFFFFFFFE,MASK]:
        for incoming in [0,1,2,0x7FFFFFFF,0x80000000,0xFFFFFFFE,MASK]:
            out.append((f'counter-{local:08X}-{incoming:08X}',2,incoming,local,[9],[(2,0,[2]),(2,0,[7])],8))
    return out


def main():
    p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--generate',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args();anchors(a.exe)
    if a.generate:
        a.generate.mkdir(parents=True,exist_ok=True);specs=[]
        for name,n,c,local,values,ops,cap in cases():
            filename=name+'.bin';(a.generate/filename).write_bytes(packet(n,c,ops))
            specs.append(dict(Name=name,File=filename,Counter=local,Values=values,Elements=cap))
        (a.generate/'cases.json').write_text(json.dumps(specs),encoding='utf-8')
    if a.verify:
        expected=[]
        for name,n,c,local,values,ops,cap in cases():
            wire=packet(n,c,ops);decoded=False
            try:
                updates=decode(wire);decoded=True
                assert (a.verify/(name+'.bin')).read_bytes()==wire,name
                final=emulate(local,values,updates,cap);expected.append(dict(Name=name,Decoded=True,Accepted=True,**final))
            except ValueError:expected.append(dict(Name=name,Decoded=decoded,Accepted=False,Counter=local,Values=values))
        actual=json.loads((a.verify/'results.json').read_text());assert actual==expected,'Independent legacy state/counter comparison failed'
        print(f'Independent instruction-derived oracle: {len(expected)} native cases/encoded messages agree.')


if __name__=='__main__':main()
