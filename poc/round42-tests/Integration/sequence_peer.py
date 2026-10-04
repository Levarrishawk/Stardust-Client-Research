"""Independent two supplied layers -> padded geometry/query, nine phase profiles."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('accepted41_height_peer',ROOT/'poc/round41-tests/Integration/height_peer.py')
h=importlib.util.module_from_spec(spec);spec.loader.exec_module(h)
p=h.prior;a=p.a;m=p.m;t=p.t

def generate():
    data=h.generate()
    for case in data['Cases']:
        second=dict(case);second['BoundaryInverted']=not case['BoundaryInverted']
        second['FilterInverted']=not case['FilterInverted'];second['FilterBits']=p.word(.8);second['IncomingBits']=p.word(.5)
        case['SecondBoundaryInverted']=second['BoundaryInverted'];case['SecondFilterInverted']=second['FilterInverted']
        case['SecondFilterBits']=second['FilterBits'];case['SecondIncomingBits']=second['IncomingBits']
        rules=[(1,0,p.word(-2)),(1,1,p.word(.5)),(1,3,p.word(2))]
        groups=b''.join(m.form(tag,m.form(version)) for tag,version in zip(m.TAGS,m.VERS))
        case['SecondGeneratorHex']=m.form(b'TGEN',m.form(b'0000',groups+m.form(b'LYRS',m.layer(b'0001',children=b''.join(a.rule(*r) for r in rules))))).hex()
        for chunk,expected in zip(case['Chunks'],case['Expected']):
            expected['FirstHeightBits']=expected['HeightBits'];expected['FirstHeightVisits']=expected['HeightVisits']
            req=chunk['Request'];xs=[p.word(req['First']*8-6+2*i) for i in range(10)];zs=[p.word(req['Second']*8-6+2*i) for i in range(10)]
            heights=[];locals=[];products=[];visits=[];counts=[]
            for z in zs:
                for x in xs:
                    local,product,bv,fv=p.combine(second,[x,z]);height=expected['FirstHeightBits'][len(heights)]
                    active=a.scalar(local)[0]>0
                    if active:
                        for enabled,operation,constant in rules:
                            reject,height,_=a.evaluate(enabled,operation,constant,height,product,case['CombinePrecision']);assert reject=='None'
                    heights.append(height);locals.append(local);products.append(product);visits.append([bv,fv]);counts.append(3 if active else 0)
            expected.update(HeightBits=heights,SecondLocalBits=locals,SecondProductBits=products,SecondVisits=visits,SecondHeightVisits=counts)
            for patch in expected['Patches']:
                selected=[(3+2*patch['Row']+dz)*10+3+2*patch['Column']+dx for dx,dz in t.OFFSETS]
                for position,index in zip(patch['Positions'],selected):position[1]=heights[index]
                reference=dict(Positions=[[p.f32(v) for v in point] for point in patch['Positions']],Normals=[[0,1,0]]*9,Pattern=patch['Pattern'])
                patch['Hits']=[t.reference(reference,q) for q in patch['Locations']]
    return data

def verify(fixture,native):
    p.generate=generate
    result=p.verify(fixture,native);operation_visits=0;changed=0
    for case,actual in zip(fixture['Cases'],native['Cases']):
        for expected,got in zip(case['Expected'],actual['Chunks']):
            for key in ['FirstHeightBits','FirstHeightVisits','SecondLocalBits','SecondProductBits','SecondVisits','SecondHeightVisits']:
                assert got[key]==expected[key],(case['Name'],key)
            operation_visits+=sum(expected['FirstHeightVisits'])+sum(expected['SecondHeightVisits'])
            changed+=sum(a!=b for a,b in zip(expected['FirstHeightBits'],expected['HeightBits']))
    assert changed>0
    return result|dict(SuppliedLayers=2,OrderedRulesPerLayer=3,HeightOperationVisits=operation_visits,ChangedBySecondLayer=changed)

if __name__=='__main__':
    mode,path=sys.argv[1:3];path=Path(path)
    if mode=='generate':path.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated independent two-layer join')
    elif mode=='verify':print(json.dumps(verify(json.loads(path.read_text()),json.loads(Path(sys.argv[3]).read_text()))))
    else:raise SystemExit('generate fixture | verify fixture native')
