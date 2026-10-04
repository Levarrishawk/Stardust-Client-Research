"""Independent three-affector supplied height -> padded geometry/query join."""
import importlib.util,json,sys
from pathlib import Path

root=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('accepted_layer_peer',root/'poc/round40-tests/Integration/layer_peer.py')
prior=importlib.util.module_from_spec(spec);spec.loader.exec_module(prior)
original_generate=prior.generate

def generate():
    data=original_generate();a=prior.a;m=prior.m;t=prior.t
    rules=[(1,0,prior.word(4)),(1,3,prior.word(1.5)),(1,2,prior.word(.25))]
    children=b''.join(a.rule(enabled,operation,bits) for enabled,operation,bits in rules)
    groups=b''.join(m.form(tag,m.form(version)) for tag,version in zip(m.TAGS,m.VERS))
    generator=m.form(b'TGEN',m.form(b'0000',groups+m.form(b'LYRS',m.layer(b'0001',children=children)))).hex()
    for case in data['Cases']:
        case['GeneratorHex']=generator
        for chunk,expected in zip(case['Chunks'],case['Expected']):
            heights=[];visits=[]
            for base,local,product in zip(chunk['InitialHeightBits'],expected['LocalBits'],expected['ProductBits']):
                height=base;active=a.scalar(local)[0]>0
                if active:
                    for enabled,operation,constant in rules:
                        reject,height,_=a.evaluate(enabled,operation,constant,height,product,case['CombinePrecision'])
                        assert reject=='None'
                heights.append(height);visits.append(3 if active else 0)
            expected['HeightBits']=heights;expected['HeightVisits']=visits
            for patch in expected['Patches']:
                selected=[(3+2*patch['Row']+dz)*10+3+2*patch['Column']+dx for dx,dz in t.OFFSETS]
                for position,index in zip(patch['Positions'],selected):position[1]=heights[index]
                reference=dict(Positions=[[prior.f32(v) for v in point] for point in patch['Positions']],Normals=[[0,1,0]]*9,Pattern=patch['Pattern'])
                patch['Hits']=[t.reference(reference,q) for q in patch['Locations']]
    return data

def verify(fixture,native):
    prior.generate=generate
    result=prior.verify(fixture,native)
    operations=0
    for case,actual in zip(fixture['Cases'],native['Cases']):
        for expected,got in zip(case['Expected'],actual['Chunks']):
            assert got['HeightVisits']==expected['HeightVisits'];operations+=sum(got['HeightVisits'])
    return result|dict(OrderedRules=3,HeightOperationVisits=operations,UsesSuppliedExecutor=True)

if __name__=='__main__':
    mode,path=sys.argv[1:3];path=Path(path)
    if mode=='generate':path.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated independent ordered three-affector join')
    elif mode=='verify':print(json.dumps(verify(json.loads(path.read_text()),json.loads(Path(sys.argv[3]).read_text()))))
    else:raise SystemExit('generate fixture | verify fixture native')
