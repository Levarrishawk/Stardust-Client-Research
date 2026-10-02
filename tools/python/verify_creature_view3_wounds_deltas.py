"""Hand-packed synthetic CREO:3 wounds delta oracle and modern interval-policy reference."""
import argparse
import json
import struct
from pathlib import Path
from verify_creature_view3_baselines import fixture as baseline_fixture

OBJECT = 0xF122334455667788
P = lambda fmt, *values: struct.pack('<' + fmt, *values)
OPS = [(0,1,[]),(1,1,[-2147483648]),(2,0,[17]),(3,0,[-1,0,42]),(4,0,[]),(1,0,[-8]),(1,1,[2147483647])]


def field(counter, ops):
    body = P('HII',17,len(ops),counter)
    for kind,index,values in ops:
        body += P('B',kind)
        if kind != 4:
            body += P('H',len(values) if kind == 3 else index)
        body += P('i'*len(values),*values)
    return body


def envelope(fields, declared=0):
    return P('HIQIBIH',5,0x12862153,OBJECT,0x4352454F,3,len(fields)+2,declared) + fields


def apply(counter, values, updates, cap=8):
    values=list(values)
    if len(values)>cap:
        raise ValueError('initial capacity')
    for incoming,ops in updates:
        start=(incoming-len(ops)) & 0xFFFFFFFF
        skip=(counter-start) & 0xFFFFFFFF
        if skip>len(ops):
            raise ValueError('counter interval')
        for kind,index,entries in ops[skip:]:
            if kind==0:
                if index>=len(values): raise ValueError('remove index')
                del values[index]
            elif kind==1:
                if index>len(values) or len(values)>=cap: raise ValueError('insert capacity/index')
                values.insert(index,entries[0])
            elif kind==2:
                if index>=len(values): raise ValueError('replace index')
                values[index]=entries[0]
            elif kind==3:
                if len(entries)>cap: raise ValueError('assign capacity')
                values=list(entries)
            elif kind==4:
                values=[]
            else:
                raise ValueError('command')
        counter=incoming
    return dict(Counter=counter,Values=values)


def cases():
    seed=[-2,2147483647,-2]
    return [
        ('full',0xFFFFFFFD,seed,[(4,OPS)]),
        ('overlap',0,[17,-2147483648,-2],[(4,OPS)]),
        ('prefix5',2,[],[(4,OPS)]),
        ('prefix6',3,[-8],[(4,OPS)]),
        ('duplicate',4,[-8,2147483647],[(4,OPS)]),
        ('gap',0xFFFFFFFC,seed,[(4,OPS)]),
        ('ahead',5,seed,[(4,OPS)]),
        ('twofields',0xFFFFFFFD,seed,[(4,OPS),(6,[(2,0,[-2147483648]),(0,1,[])])]),
        ('atomicfail',0xFFFFFFFD,seed,[(4,OPS),(5,[(0,7,[])])]),
        ('skippedbadindex',1,[9],[(1,[(0,65535,[])])]),
        ('assignempty',7,[1],[(8,[(3,0,[])])]),
        ('zerocommands',9,[],[(9,[])]),
        ('zerocountergap',8,[],[(9,[])]),
        ('replacegrowth',0,[1],[(1,[(2,1,[2])])]),
        ('insertgap',0,[1],[(1,[(1,2,[2])])])]


def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--generate',type=Path); parser.add_argument('--verify',type=Path); args=parser.parse_args()
    if args.generate:
        args.generate.mkdir(parents=True,exist_ok=True)
        (args.generate/'input.bin').write_bytes(envelope(field(4,OPS)))
        baseline=bytearray(baseline_fixture()); baseline[-16:-12]=P('I',0xFFFFFFFD)
        (args.generate/'baseline.bin').write_bytes(baseline)
        specs=[]
        for name,counter,values,updates in cases():
            filename=name+'.bin'; (args.generate/filename).write_bytes(envelope(b''.join(field(c,ops) for c,ops in updates)))
            specs.append(dict(Name=name,File=filename,Counter=counter,Values=values))
        (args.generate/'cases.json').write_text(json.dumps(specs),encoding='utf-8')
    if args.verify:
        for count,name in [(0,'encoded.bin'),(65535,'encoded-count65535.bin')]:
            assert (args.verify/name).read_bytes()==envelope(field(4,OPS),count),name
        plan=json.loads((args.verify/'plan.json').read_text())
        assert plan==dict(ObjectId=OBJECT,DeclaredCount=0,Updates=[dict(Counter=4,CommandCount=7,Commands=[dict(Kind=k,Index=i,Values=v) for k,i,v in OPS])])
        results=json.loads((args.verify/'results.json').read_text()); expected=[]
        for name,counter,values,updates in cases():
            try:
                final=apply(counter,values,updates); expected.append(dict(Name=name,Accepted=True,**final))
            except ValueError:
                expected.append(dict(Name=name,Accepted=False,Counter=counter,Values=values))
        assert results==expected,(results,expected)
        assert json.loads((args.verify/'baseline-result.json').read_text())==dict(ObjectId=OBJECT,Counter=4,Values=[-8,2147483647])
        print('Independent hand-packed wire, all commands, baseline seed and 15 application cases verified.')


if __name__=='__main__':
    main()
