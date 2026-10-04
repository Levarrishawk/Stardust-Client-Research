"""Independent current-height FHGT -> padded geometry/query join, synthetic only."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
old=load('accepted42_join','poc/round42-tests/Integration/sequence_peer.py')
f=load('independent43_filter','poc/round43-tests/Rendering/verify_filtered.py')
p=old.p;a=p.a;t=p.t
def combine(case,xy,height,second=False):
    cp=case['CombinePrecision'];acc=a.scalar(0);bv=fv=0
    for value,selector in [(p.circle_u(case,xy),3),(p.rectangle_u(case,xy),2)]:
        shaped=p.shape(value,selector,cp);candidate=a.bits(shaped);bv+=1
        if acc[0]<shaped[0]:acc=a.scalar(candidate)
        if acc[0]==1:break
    prefix='Second' if second else ''
    if case[prefix+'BoundaryInverted']:acc=a.scalar(a.bits(p.sub(p.ONE,acc,cp)))
    current=None
    if acc[0]>0:
        inp=case[prefix+'Filter'];current=height;fv=1
        fixture=dict(inp,Height=height,Precision=64 if second else 53,Limits=f.fh.QUERY,Action='query')
        value=f.query_number(fixture);assert not isinstance(value,dict)
        shaped=p.shape(value,inp['Selector'],cp)
        if shaped[0]<acc[0]:acc=a.scalar(a.bits(shaped))
        if case[prefix+'FilterInverted']:acc=a.scalar(a.bits(p.sub(p.ONE,acc,cp)))
    return a.bits(acc),a.bits(p.mul(acc,a.scalar(case[prefix+'IncomingBits']),case['ProductPrecision'])),bv,fv,current
def generate():
    data=old.generate();stale=0
    rules=[[(1,0,p.word(4)),(1,3,p.word(1.5)),(1,2,p.word(.25))],[(1,0,p.word(-2)),(1,1,p.word(.5)),(1,3,p.word(2))]]
    for case in data['Cases']:
        case['Filter']=f.filter(p.word(-8),p.word(8),1,p.word(.5))
        case['SecondFilter']=f.filter(0,p.word(4),3,p.word(1))
        case['FirstFilterHex']=case['Filter']['Hex'];case['SecondFilterHex']=case['SecondFilter']['Hex']
        for chunk,expected in zip(case['Chunks'],case['Expected']):
            arrays={k:[] for k in ['LocalBits','ProductBits','Visits','HeightBits','HeightVisits','FirstHeightBits','FirstHeightVisits','SecondLocalBits','SecondProductBits','SecondVisits','SecondHeightVisits','FirstQueryHeight','SecondQueryHeight']}
            req=chunk['Request'];xs=[p.word(req['First']*8-6+2*i) for i in range(10)];zs=[p.word(req['Second']*8-6+2*i) for i in range(10)]
            for z in zs:
                for x in xs:
                    base=chunk['InitialHeightBits'][len(arrays['HeightBits'])];height=base
                    for stage in range(2):
                        local,product,bv,fv,qh=combine(case,[x,z],height,bool(stage));visits=3 if a.scalar(local)[0]>0 else 0
                        if stage==1:stale+=combine(case,[x,z],base,True)[:2]!=(local,product)
                        if visits:
                            for enabled,op,constant in rules[stage]:
                                reject,height,_=a.evaluate(enabled,op,constant,height,product,case['CombinePrecision']);assert reject=='None'
                        keys=['LocalBits','ProductBits','Visits','FirstHeightBits','FirstHeightVisits','FirstQueryHeight'] if stage==0 else ['SecondLocalBits','SecondProductBits','SecondVisits','HeightBits','SecondHeightVisits','SecondQueryHeight']
                        for key,value in zip(keys,[local,product,[bv,fv],height,visits,qh]):arrays[key].append(value)
                    arrays['HeightVisits'].append(arrays['FirstHeightVisits'][-1])
            expected.update(arrays)
            for patch in expected['Patches']:
                selected=[(3+2*patch['Row']+dz)*10+3+2*patch['Column']+dx for dx,dz in t.OFFSETS]
                for position,index in zip(patch['Positions'],selected):position[1]=arrays['HeightBits'][index]
                reference=dict(Positions=[[p.f32(v) for v in point] for point in patch['Positions']],Normals=[[0,1,0]]*9,Pattern=patch['Pattern'])
                patch['Hits']=[t.reference(reference,q) for q in patch['Locations']]
    assert stale>0;data['StaleInitialInfluenceWitnesses']=stale;return data
def verify(fixture,native):
    p.generate=generate;result=p.verify(fixture,native)
    for case,got in zip(fixture['Cases'],native['Cases']):
        for expected,actual in zip(case['Expected'],got['Chunks']):
            for key in ['FirstHeightBits','FirstHeightVisits','SecondLocalBits','SecondProductBits','SecondVisits','SecondHeightVisits','FirstQueryHeight','SecondQueryHeight']:assert expected[key]==actual[key],(case['Name'],key)
    return result|dict(QueryTimeFilters=True,StaleInitialInfluenceWitnesses=fixture['StaleInitialInfluenceWitnesses'])
if __name__=='__main__':
    mode,path=sys.argv[1:3];path=Path(path)
    if mode=='generate':path.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated current-height join')
    else:print(json.dumps(verify(json.loads(path.read_text()),json.loads(Path(sys.argv[3]).read_text()))))
