"""Independent strict schema/float32/budget fixtures and a three-tick actual-driver reference."""
import argparse,hashlib,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
DEFAULT=dict(Version=1,MaximumUpdates=6000,MaximumClickAttempts=128,StepMicroseconds=10000,WalkSpeed=3,RunSpeed=4,ArrivalRadius=.001,HoldMissingDestination=True)
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def f(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def encode(c):return json.dumps(c,separators=(',',':'),ensure_ascii=False).encode('utf-8')
def pairs(entries):
    result={}
    for k,v in entries:
        if k in result:raise ValueError('duplicate')
        result[k]=v
    return result
def invalid_constant(value):raise ValueError('NonJSON constant')
def describe(c):
    return dict(Version=c['Version'],MaximumUpdates=c['MaximumUpdates'],MaximumClickAttempts=c['MaximumClickAttempts'],StepMicroseconds=c['StepMicroseconds'],Walk=bits(c['WalkSpeed']),Run=bits(c['RunSpeed']),Radius=bits(c['ArrivalRadius']),HoldMissingDestination=c['HoldMissingDestination'],BudgetUpdates=c['MaximumUpdates'],BudgetTargets=c['MaximumClickAttempts'])
def expected(raw):
    try:
        if len(raw)>1024:raise ValueError('size')
        c=json.loads(raw.decode('utf-8',errors='strict'),object_pairs_hook=pairs,parse_constant=invalid_constant)
        if not isinstance(c,dict) or set(c)!=set(DEFAULT):raise ValueError('schema')
        for key in ['Version','MaximumUpdates','MaximumClickAttempts','StepMicroseconds']:
            if type(c[key]) is not int:raise ValueError('integer token')
        if c['Version']!=1 or not 1<=c['MaximumUpdates']<=6000 or not 1<=c['MaximumClickAttempts']<=128 or c['StepMicroseconds']!=10000:raise ValueError('count/range')
        for key in ['WalkSpeed','RunSpeed','ArrivalRadius']:
            if type(c[key]) not in (int,float):raise ValueError('number token')
            c[key]=f(c[key])
            if not math.isfinite(c[key]):raise ValueError('finite')
        if not 0<c['WalkSpeed']<=10 or not c['WalkSpeed']<=c['RunSpeed']<=10 or not f(.0001)<=c['ArrivalRadius']<=1:raise ValueError('float range')
        if type(c['HoldMissingDestination']) is not bool:raise ValueError('bool')
        return 'None',describe(c)
    except (ValueError,UnicodeError,OverflowError,TypeError):return 'Input',None
def fixture(runtime,terrain):
    cases=[]
    def put(name,raw):cases.append(dict(Name=name,Hex=raw.hex()))
    put('defaults',encode(DEFAULT))
    put('minimums-subnormal-walk',encode(dict(DEFAULT,MaximumUpdates=1,MaximumClickAttempts=1,WalkSpeed=1e-45,RunSpeed=1e-45,ArrivalRadius=.0001,HoldMissingDestination=False)))
    put('maximums-equal-speeds',encode(dict(DEFAULT,WalkSpeed=10,RunSpeed=10,ArrivalRadius=1)))
    put('custom-float32',encode(dict(DEFAULT,MaximumUpdates=3,MaximumClickAttempts=7,WalkSpeed=3.141592653589793,RunSpeed=6.125,ArrivalRadius=.123456789)))
    put('order-and-escaped-name',encode(dict(reversed(list(DEFAULT.items())))).replace(b'"Version"',b'"\\u0056ersion"'))
    put('json-whitespace',b' \r\n\t'+encode(DEFAULT)+b' \r\n\t')
    put('exact-1024-byte-document',encode(DEFAULT)+b' '*(1024-len(encode(DEFAULT))))
    for key in DEFAULT:
        c=dict(DEFAULT);del c[key];put('missing-'+key,encode(c))
        raw=encode(DEFAULT);entry=encode({key:DEFAULT[key]})[1:-1];put('duplicate-'+key,raw[:-1]+b','+entry+b'}')
        for value in [None,[],{},'number',True if key!='HoldMissingDestination' else 1]:
            if key=='HoldMissingDestination' and value is True:continue
            put('wrong-'+key+'-'+type(value).__name__,encode(dict(DEFAULT,**{key:value})))
    for key,values in dict(Version=[0,2,-1,1.0],MaximumUpdates=[0,6001,-1,3.0,2**63],MaximumClickAttempts=[0,129,-1,1.0],StepMicroseconds=[9999,10001,10000.0,2**63],WalkSpeed=[0,-1,10.00001,1e100,1e-999],RunSpeed=[2,-1,10.00001,1e100],ArrivalRadius=[0,.00009,1.001,1e100]).items():
        for i,value in enumerate(values):put('range-'+key+'-'+str(i),encode(dict(DEFAULT,**{key:value})))
    raw=encode(DEFAULT)
    for key,lexeme in [('Version',b'1e0'),('MaximumUpdates',b'3e0'),('MaximumClickAttempts',b'1e0'),('StepMicroseconds',b'1e4')]:
        put('integer-exponent-'+key,raw.replace(encode({key:DEFAULT[key]})[1:-1],('"'+key+'":').encode()+lexeme))
    for name,bad in [('unknown',encode(dict(DEFAULT,Extra=1))),('wrong-case',raw.replace(b'"Version"',b'"version"')),('empty-object',b'{}'),('root-array',b'[]'),('root-null',b'null'),('root-string',b'"x"'),('root-number',b'1'),('two-documents',raw+raw),('trailing-junk',raw+b'x'),('trailing-zero-byte',raw+b'\0'),('trailing-comma',raw[:-1]+b',}'),('comment',b'/*a*/'+raw),('bom',b'\xef\xbb\xbf'+raw),('oversize',raw+b' '*(1025-len(raw))),('nested-depth',raw.replace(b'"WalkSpeed":3',b'"WalkSpeed":{"a":{"b":1}}')),('NaN',raw.replace(b'"WalkSpeed":3',b'"WalkSpeed":NaN')),('numeric-string',raw.replace(b'"WalkSpeed":3',b'"WalkSpeed":"3"')),('invalid-utf8-key',raw.replace(b'Version',b'Ver\xffsion')),('invalid-utf8-string',raw.replace(b'"WalkSpeed":3',b'"WalkSpeed":"\xc0\xaf"')),('utf8-surrogate',raw.replace(b'Version',b'Ver\xed\xa0\x80sion')),('incomplete-utf8',raw+b'\xc2'),('escaped-duplicate',raw[:-1]+b',"\\u0056ersion":1}')]:put(name,bad)
    for end in [0,1,4,len(raw)//2,len(raw)-1]:put('truncated-'+str(end),raw[:end])
    join=encode(dict(DEFAULT,MaximumUpdates=3,MaximumClickAttempts=1))
    return dict(Cases=cases,Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],JoinSettingsHex=join.hex())
def compute(source):
    cases=[]
    for c in source['Cases']:
        rejection,settings=expected(bytes.fromhex(c['Hex']));cases.append(dict(Name=c['Name'],Reject=rejection,Settings=settings))
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'))
    model=dict(Name='settings-max3',Start=[7.8,7.8],Targets=[],Manual=[dict(At=0,X=1,Z=0)],Pauses=[],Clears=[2],Speeds=[],Updates=3,Speed=3,Radius=.001,Step=10000,Sparse=False)
    wanted,_=peer['compute'](dict(Terrain=source['Terrain'],Cases=[model]))
    join=[dict(Index=row['Index'],Mode=row['Mode'],Completed=row['CompletedUpdates'],Time=row['Time'],Position=row['Position'],Speed=row['Speed'],Stopped=row['Index']==2,Held=False) for row in wanted[0]['Rows']]
    return dict(Cases=cases,Defaults=describe(DEFAULT),Join=join)
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for key in ['runtime','terrain','out']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':
        guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(wanted,separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('SETTINGS_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text())
    for key in wanted:assert actual[key]==wanted[key],(key,actual[key],wanted[key])
    positives=sum(c['Reject']=='None' for c in wanted['Cases']);negatives=len(wanted['Cases'])-positives
    assert positives==7 and wanted['Join'][-1]['Stopped'] and wanted['Join'][-1]['Speed']==0
    report=dict(Passed=True,PositiveSettings=positives,RejectedSettings=negatives,ActualUpdates=3,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentStrictUtf8SchemaF32BudgetAndGroundedTick=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('PLAYGROUND_SETTINGS_PASS',json.dumps(report))
if __name__=='__main__':main()
