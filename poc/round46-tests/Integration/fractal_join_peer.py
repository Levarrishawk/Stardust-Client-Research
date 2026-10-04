"""Independent supplied AHFR -> retained FFRA/FHGT -> AHFR cell join, synthetic only."""
import importlib.util,itertools,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
ah=load('join46_ahfr','tools/python/verify_terrain_height_fractal.py')
ff=load('join46_ffra','tools/python/verify_terrain_filter_fractal.py');ff.setup()
a=ff.a;l=ff.l;fh=ff.fh
def word(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def height_rule(c,initial,sample,operation,scale,enabled=1):
    result=ah.evaluate(dict(Precision=c['Precision'],Height=initial,Influence=c.get('Weight',word(1)),Fractal=sample,Scale=scale,Operation=operation,Enabled=enabled))
    assert result['Reject']=='None';return result
def query(c,height,fractal=False):
    if fractal:
        scaled=l.store(a.precision(a.mul(a.scalar(c['FilterSample']),a.scalar(c['FilterScale'])),c['Query']))
        fixture=dict(Version='0002',Lower=0,Upper=word(2),Amount=word(.75),Height=scaled,Precision=c['Query'],Limits=fh.QUERY,Action='query',Selector=c['FilterSelector'],Enabled=1)
    else:
        scaled=None;fixture=dict(Version='0002',Lower=word(-2),Upper=word(2),Amount=word(.5),Height=height,Precision=c['HeightQuery'],Limits=fh.QUERY,Action='query',Selector=c['HeightSelector'],Enabled=1)
    u=ff.old.query_number(fixture);assert not isinstance(u,dict)
    return u,scaled
def oracle(c,stale=False,premature=False):
    first=height_rule(c,c['Initial'],c['RootSample'],c['RootOperation'],c['RootScale'],c['RootEnabled'])
    result=dict(Name=c['Name'],First=first['Output'],FirstOutcome=first['Outcome'],FractalQuery=None,Scaled=None,HeightQuery=None,QueryHeight=None,Local=c['Boundary'],Product=None,FilterVisits=0,Final=first['Output'],ChildOutcome=None)
    acc=a.scalar(c['Boundary'])
    if acc[0]>0:
        u,scaled=query(c,first['Output'],True);result.update(FractalQuery=l.store(u),Scaled=scaled,FilterVisits=1)
        if premature:u=a.scalar(l.store(u))
        shaped=l.shape(u,c['FilterSelector'],c['Combine'])
        if shaped[0]<acc[0]:acc=a.scalar(l.store(shaped))
        if acc[0]>0:
            height=c['Initial'] if stale else first['Output'];u,_=query(c,height);result.update(HeightQuery=l.store(u),QueryHeight=height,FilterVisits=2)
            if premature:u=a.scalar(l.store(u))
            shaped=l.shape(u,c['HeightSelector'],c['Combine'])
            if shaped[0]<acc[0]:acc=a.scalar(l.store(shaped))
    result['Local']=l.store(acc);result['Product']=l.store(a.precision(a.mul(acc,a.scalar(c['Incoming'])),c['Product']))
    if acc[0]>0:
        final=height_rule(dict(c,Weight=result['Product']),first['Output'],c['ChildSample'],c['ChildOperation'],c['ChildScale'],c['ChildEnabled'])
        result.update(Final=final['Output'],ChildOutcome=final['Outcome'])
    return result
def generate():
    rng=random.Random(4603);cases=[]
    profiles=list(itertools.product([24,53,64],repeat=2))
    for i in range(100):
        for pi,(p,cp) in enumerate(profiles):
            c=dict(Name=f'cell-{i}-{pi}',Precision=p,Combine=cp,Product=[24,53,64][pi%3],Query=[24,53,64][(pi+1)%3],HeightQuery=[24,53,64][(pi+2)%3],
                   Initial=word(rng.uniform(-2,2)),Incoming=word([0,.25,1][i%3]),Boundary=word([0,.4,1][(i//3)%3]),
                   RootSample=word(rng.uniform(-2,2)),FilterSample=word(rng.uniform(-.2,2.2)),ChildSample=word(rng.uniform(-1,1)),
                   RootOperation=i%6,ChildOperation=(i//6)%6,RootScale=word(.75),ChildScale=word(1.25),FilterScale=word(.9),
                   FilterSelector=i%4,HeightSelector=(i//4)%4,RootEnabled=int(i%11!=0),ChildEnabled=int(i%13!=0))
            c.update(RootHex=ah.encoded(1,c['RootOperation'],c['RootScale'],c['RootEnabled']).hex(),ChildHex=ah.encoded(27,c['ChildOperation'],c['ChildScale'],c['ChildEnabled']).hex(),
                     FractalFilterHex=ff.encoded(lo=0,hi=word(2),selector=c['FilterSelector'],amount=word(.75),scale=c['FilterScale']).hex(),
                     HeightFilterHex=fh.encoded('0002',word(-2),word(2),c['HeightSelector'],word(.5)).hex())
            c['Expected']=oracle(c);cases.append(c)
    # Unused invalid sample words must stay behind boundary, product and enabled gates.
    for label,key in [('boundary-unused','FilterSample'),('product-unused','ChildSample'),('disabled-unused','RootSample')]:
        c=dict(cases[1]);c.update(Name=label,RootEnabled=1,ChildEnabled=1)
        if label=='boundary-unused':c['Boundary']=0
        elif label=='product-unused':c['Incoming']=0;c['Boundary']=word(1);c['FilterSample']=word(1);c['RootSample']=0;c['Initial']=0;c['RootOperation']=0
        else:c['RootEnabled']=0
        c[key]=0x7fc00000;c['RootHex']=ah.encoded(1,c['RootOperation'],c['RootScale'],c['RootEnabled']).hex();c['ChildHex']=ah.encoded(27,c['ChildOperation'],c['ChildScale'],c['ChildEnabled']).hex();c['Expected']=oracle(c);cases.append(c)
    assert cases[-2]['Expected']['ChildOutcome']==2 and cases[-1]['Expected']['FirstOutcome']==1
    stale=sum(c['Expected']!=oracle(c,stale=True) for c in cases)
    retained=sum(c['Expected']!=oracle(c,premature=True) for c in cases)
    assert stale>0
    return dict(Version=1,Synthetic=True,OriginalTerrainParity=False,Cases=cases,StaleHeightWitnesses=stale,PrematureProjectionWitnesses=retained)
def verify(fixture,native):
    data=json.loads(Path(fixture).read_text());assert data==generate();rows=json.loads(Path(native).read_text());assert len(rows)==len(data['Cases'])
    for c,row in zip(data['Cases'],rows):assert c['Expected']==row,(c['Name'],c['Expected'],row)
    print(json.dumps(dict(Passed=True,Cases=len(rows),StaleHeightWitnesses=data['StaleHeightWitnesses'],PrematureProjectionWitnesses=data['PrematureProjectionWitnesses'],OriginalTerrainParity=False)))
if __name__=='__main__':
    if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n');print('Generated supplied fractal composition')
    else:verify(*sys.argv[2:4])
