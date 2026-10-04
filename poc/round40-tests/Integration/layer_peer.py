"""Independent supplied-query layer -> AHCN -> padded patch/query join; synthetic only."""
import importlib.util,itertools,json,math,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
c=module('joined_circle','poc/round36-tests/Rendering/verify_circle.py');a=c.a;m=a.m
r=module('joined_rectangle','poc/round39-tests/Assets/verify_rectangle.py')
t=module('joined_topology','poc/round37-tests/Runtime/peer.py')
word=c.fbits
f32=lambda bits:struct.unpack('<f',struct.pack('<I',bits))[0]
def add(x,y,p):return a.precision(a.add(x,y),p)
def sub(x,y,p):return add(x,a.neg(y),p)
def mul(x,y,p):return a.precision(a.mul(x,y),p)
ONE=a.scalar(0x3f800000)
def circle_u(case,xy):
    q=c.evaluate(dict(Precision=64,CachePrecision=53,Selector=0,Enabled=1,Geometry=case['CircleGeometry'],Coordinates=xy,Amount=case['CircleAmount'],Version='0002',Action='query'))
    assert q['Reject']=='None'
    if q['Branch']==0:return a.scalar(0)
    if q['Branch']==1:return ONE
    inner=mul(sub(ONE,a.scalar(q['Amount']),64),a.scalar(case['CircleGeometry'][2]),64)
    square=mul(inner,inner,64);distance=a.scalar(q['DistanceSquared']);radius=a.scalar(q['RadiusSquared'])
    return sub(ONE,a.precision((sub(distance,square,64)[0]/sub(radius,square,64)[0],False),64),64)
def rectangle_u(case,xy):
    q=r.evaluate(dict(Precision=64,CachePrecision=53,Selector=0,Enabled=1,Geometry=case['RectangleGeometry'],Coordinates=xy,Amount=case['RectangleAmount'],Version='0002',Action='query'))
    assert q['Reject']=='None'
    if q['Branch']==0:return a.scalar(0)
    if q['Branch'] in (1,2):return ONE
    denominator=mul(mul(a.scalar(q['Amount']),a.scalar(q['QueryExtents'][2]),64),a.scalar(0x3f000000),64)
    edge=denominator
    for bits in q['QueryEdges']:
        if a.scalar(bits)[0]<edge[0]:edge=a.scalar(bits)
    return a.precision((edge[0]/denominator[0],edge[1] if not edge[0] else False),64)
def shape(value,selector,p):
    if selector==0:return value
    if selector==1:return mul(value,value,p)
    if selector==2:return c.sqrt_round(value,p)
    if selector==3:return mul(mul(sub(a.scalar(0x40400000),add(value,value,p),p),value,p),value,p)
    return a.scalar(0)
def combine(case,xy):
    p=case['CombinePrecision'];acc=a.scalar(0);bv=fv=0
    for value,selector in [(circle_u(case,xy),3),(rectangle_u(case,xy),2)]:
        shaped=shape(value,selector,p);candidate=a.bits(shaped);bv+=1
        if acc[0]<shaped[0]:acc=a.scalar(candidate)
        if acc[0]==1:break
    if case['BoundaryInverted']:acc=a.scalar(a.bits(sub(ONE,acc,p)))
    if acc[0]>0:
        shaped=shape(a.scalar(case['FilterBits']),1,p);fv+=1
        if shaped[0]<acc[0]:acc=a.scalar(a.bits(shaped))
        if case['FilterInverted']:acc=a.scalar(a.bits(sub(ONE,acc,p)))
    local=a.bits(acc);product=a.bits(mul(acc,a.scalar(case['IncomingBits']),case['ProductPrecision']))
    return local,product,bv,fv
def generate():
    cases=[]
    for p,pp in itertools.product((24,53,64),repeat=2):
        case=dict(Name=f'{p}_{pp}',CombinePrecision=p,ProductPrecision=pp,BoundaryInverted=p==53,FilterInverted=pp==64,
            CircleGeometry=[word(-2),word(1),word(6)],CircleAmount=word(.5),RectangleGeometry=[word(-6),word(-5),word(5),word(6)],
            RectangleAmount=word(.75),FilterBits=word(.6),IncomingBits=word(.75))
        case['CircleHex']=c.encode(dict(Enabled=1,Version='0002',Geometry=case['CircleGeometry'],EncodedSelector=3,Amount=case['CircleAmount']))
        case['RectangleHex']=r.encode(dict(Enabled=1,Version='0002',Geometry=case['RectangleGeometry'],EncodedSelector=2,Amount=case['RectangleAmount']))
        rule=a.rule(1,0,word(4));groups=b''.join(m.form(tag,m.form(version)) for tag,version in zip(m.TAGS,m.VERS))
        case['GeneratorHex']=m.form(b'TGEN',m.form(b'0000',groups+m.form(b'LYRS',m.layer(b'0001',children=rule)))).hex()
        chunks=[];expected=[]
        for chunk,(first,second) in enumerate([(-1,-1),(0,-1),(-1,0),(0,0)]):
            request=dict(ChunkWidthBits=word(8),Tiles=2,Margin=3,First=first,Second=second,Scale=1)
            xs=[word(first*8-6+2*i) for i in range(10)];zs=[word(second*8-6+2*i) for i in range(10)]
            initial=[word(f32(z)/8) for z in zs for _ in xs];heights=[];locals=[];products=[];visits=[]
            for z in zs:
                for x in xs:
                    local,product,bv,fv=combine(case,[x,z]);locals.append(local);products.append(product);visits.append([bv,fv])
                    reject,height,_=a.evaluate(1,0,word(4),initial[len(heights)],product,p);assert reject=='None';heights.append(height)
            mask=chunk*5;patches=[]
            for row in range(2):
                for column in range(2):
                    selected=[(3+2*row+dz)*10+3+2*column+dx for dx,dz in t.OFFSETS]
                    positions=[[xs[k%10],heights[k],zs[k//10]] for k in selected]
                    pattern=t.select(2,column,row,mask);indices=t.PATTERNS[pattern]
                    real=[[f32(v) for v in position] for position in positions];cx,_,cz=real[0]
                    locations=[[cx,cz],[cx+.5,cz+.25],[cx-2,cz],[cx+3,cz+3]]
                    reference=dict(Positions=real,Normals=[[0,1,0]]*9,Pattern=pattern)
                    patches.append(dict(Column=column,Row=row,Pattern=pattern,Positions=positions,Indices=indices,
                        Locations=locations,Hits=[t.reference(reference,q) for q in locations]))
            chunks.append(dict(Request=request,InitialHeightBits=initial,SeamMask=mask,Locations=[p['Locations'] for p in patches]))
            expected.append(dict(LocalBits=locals,ProductBits=products,Visits=visits,HeightBits=heights,Patches=patches))
        case['Chunks']=chunks;case['Expected']=expected;cases.append(case)
    return dict(Synthetic=True,OriginalTerrainParity=False,Cases=cases)
def verify(fixture,native):
    assert fixture==generate(),'Fixture differs from independent regeneration'
    assert len(native['Cases'])==len(fixture['Cases'])==9
    patches=queries=samples=0
    for case,got in zip(fixture['Cases'],native['Cases']):
        assert got['Name']==case['Name'] and len(got['Chunks'])==4
        for expected,actual in zip(case['Expected'],got['Chunks']):
            for key in ['LocalBits','ProductBits','Visits','HeightBits']:assert expected[key]==actual[key],(case['Name'],key)
            samples+=len(expected['HeightBits']);assert len(actual['Patches'])==4
            for ep,ap in zip(expected['Patches'],actual['Patches']):
                patches+=1
                for key in ['Column','Row','Pattern','Positions','Indices']:assert ep[key]==ap[key],(case['Name'],key)
                assert len(ep['Hits'])==len(ap['Hits'])==4
                for eh,ah in zip(ep['Hits'],ap['Hits']):
                    queries+=1
                    if eh is None:assert ah is None;continue
                    assert ah is not None and ah['TriangleIndex']==eh['TriangleIndex']
                    assert [ah[k] for k in ['VertexA','VertexB','VertexC']]==eh['Vertices']
                    for key,value in zip(['WeightA','WeightB','WeightC'],eh['Weights']):assert math.isclose(ah[key],value,rel_tol=2e-13,abs_tol=2e-13)
                    assert math.isclose(ah['Height'],eh['Height'],rel_tol=2e-13,abs_tol=2e-13)
                    assert ah['Normal']==dict(X=0,Y=1,Z=0)
    return dict(Passed=True,Profiles=9,Samples=samples,Patches=patches,Queries=queries,Synthetic=True,OriginalTerrainParity=False)
if __name__=='__main__':
    mode,path=sys.argv[1:3];path=Path(path)
    if mode=='generate':path.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated independent nine-profile supplied-layer join')
    elif mode=='verify':print(json.dumps(verify(json.loads(path.read_text()),json.loads(Path(sys.argv[3]).read_text()))))
    else:raise SystemExit('generate fixture | verify fixture native')
