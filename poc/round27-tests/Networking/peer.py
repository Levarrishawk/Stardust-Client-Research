"""Bounded synthetic CREO/TANO replay; independent byte packing and SOE oracle."""
import argparse
import hashlib
import importlib.util
import json
import struct
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('prior14', REPO/'poc/round14-tests/Networking/peer.py')
prior = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prior)
H, T = prior.H, prior.H+1
P = lambda f, *v: struct.pack('<'+f, *v)
HEX = lambda b: b.hex().upper()

def string(raw, extended=False):
    return (P('HI', 65535, len(raw)) if extended else P('H', len(raw)))+raw

def units(values):
    return P('I', len(values))+P('H'*len(values), *values)

def sid(file, key, spacer=0, fe=False, ke=False):
    return string(file, fe)+P('I', spacer)+string(key, ke)

def env(oid, tag, fields, baseline=False, count=65535):
    return P('HIQIBIH', 5, 0x68A75F0C if baseline else 0x12862153,
             oid, tag, 3, len(fields)+2, count)+fields

def component(counter, commands):
    return P('HII', 5, len(commands), counter)+b''.join(
        P('B', kind)+(P('i', key) if kind<2 else b'') for kind, key in commands)

def generate():
    catalog = (REPO/'poc/preview-profiles/scene-object-catalog.json').read_bytes()
    digest = hashlib.sha256(catalog).hexdigest().upper()
    assert digest == 'B945D2D4695FF37D54343F11A7DF3539A5AB36A2E5D13246969B04F92657492D'
    entries = json.loads(catalog)['entries']
    assert [e['templateCrc'] for e in entries] == [2937962913, 2907007746]
    assert all(prior.checksum(e['objectPath']) == e['templateCrc'] for e in entries)
    bits = [0x80000000,0x3E800000,0xBF000000,0x40000000,0x42F70000,0xC2350000,0x000116C2]
    human = prior.application('create', H, bits, entries[0]['templateCrc'], 255)
    static = prior.application('create', T, bits, entries[1]['templateCrc'], 255)
    file, key = b'synthetic_terminal', b'synthetic_initial'
    of, ok = b'synthetic_object', b'synthetic_shown'
    spacer = 0xA1B2C3D4
    common = P('I',0x80000000)+sid(file,key,spacer,True)+units([])+P('I',0xffffffff)+string(b'')+P('IIiii',3,7,9,9,-3)+P('IIIIB',1,2,3,100,1)
    baseline = env(T,0x54414E4F,common,True)
    # Complete 18-field CREO baseline. Long authored custom name forces SOE fragmentation.
    hc = P('I',0x80000000)+sid(b'synthetic_human',b'initial')+units(list(map(ord,'Synthetic human '*20)))+P('I',1)+string(b'')+P('IIi',1,1,7)+P('IIIIB',1,2,3,100,1)
    hb = env(H,0x4352454F,hc+P('BBQfIQII',1,2,0,1.0,0,0,0,0),True)
    custom = list(map(ord,'Static terminal'))
    updates = [
        ('hidden_custom',P('H',2)+units(custom)+P('H',4)+string(b'\x01\x02\x03',True)+P('HIHB',8,20,10,0)+component(10,[(0,-3),(1,11)])),
        ('shown_string_id',P('H',1)+sid(of,ok,0,False,True)+P('H',2)+units([])+P('H',4)+string(b'',True)+component(10,[(0,9)])+P('HBHIHI',10,255,0,0,7,9)),
        ('safe_custom',component(11,[(2,0)])+component(12,[(1,7)])+P('H',2)+units([65,0,0xd800,66])+P('H',4)+string(b'\xff\x00')+P('HB',10,1)),
        ('restored_bytes',P('H',1)+sid(file,key,spacer,True)+P('H',2)+units([])+P('H',4)+string(b'')+component(13,[(2,0)])+P('HIHB',8,3,10,1))]
    charge = 47+len(file)+len(key)+12
    def row(name,f,k,s,u,raw,comps,counter,visible,extra,fe,ke,re,complexity,dmg,use,additional):
        return dict(Name=name,NameFileHex=HEX(f),NameKeyHex=HEX(k),NameSpacer=s,NameCustomUnits=u,
                    NameFileExtended=fe,NameKeyExtended=ke,CustomizationHex=HEX(raw),CustomizationExtended=re,
                    Components=comps,ComponentCounter=counter,Visibility=visible,AdditionalNames=additional,
                    RetainedDecodedBytes=charge+16+4*len(comps)+extra,ComplexityBits=complexity,ConditionDamage=dmg,UseCount=use)
    expected = [
        row('baseline',file,key,spacer,[],b'',[-3,9],7,1,0,True,False,False,'80000000',3,2,0),
        row('hidden_custom',file,key,spacer,custom,b'\x01\x02\x03',[9,11],10,0,33,True,False,True,'80000000',20,2,30),
        row('shown_string_id',of,ok,0,[],b'',[9,11],10,255,len(of)+len(ok),False,True,True,'00000000',20,9,len(of)+len(ok)),
        row('safe_custom',of,ok,0,[65,0,0xd800,66],b'\xff\x00',[7],12,1,len(of)+len(ok)+10,False,True,False,'00000000',20,9,len(of)+len(ok)+8),
        row('restored_bytes',file,key,spacer,[],b'',[],13,1,len(file)+len(key),True,False,False,'00000000',3,9,len(file)+len(key))]
    stage = dict(Version=1,Synthetic=True,ObjectId=T,Create=HEX(static),Baseline=HEX(baseline),BaselineDecodedBytes=charge,
                 MaximumStoreBytes=512,Updates=[dict(Name=n,Hex=HEX(env(T,0x54414E4F,b,count=65535 if i%2==0 else 0))) for i,(n,b) in enumerate(updates)],Expected=expected,
                 LateUnsupported=HEX(env(T,0x54414E4F,P('H',4)+string(b'late')+P('HB',11,7),count=2)),
                 IntermediateOverBudget=HEX(env(T,0x54414E4F,P('H',4)+string(bytes(513))+P('H',4)+string(b''),count=2)))
    apps = []
    def add(name,kind,b,expected_row=None):
        a = dict(Name=name,Kind=kind,Hex=HEX(b))
        if expected_row is not None:a['Expected']=dict(expected_row,Name=name)
        apps.append(a)
    add('human_create','create',human);add('static_create','create',static)
    add('human_baseline','creo_baseline',hb);add('static_baseline','tano_baseline',baseline,expected[0])
    add('human_end','end',prior.application('end',H));add('static_end','end',prior.application('end',T))
    human_fields = [P('H',2)+units(list(map(ord,'Human renamed'))),P('H',1)+sid(b'synthetic_human',b'changed',123,True,True),P('HBHI',11,4,8,17),component(1,[])]
    for i,u in enumerate(stage['Updates']):
        add('human_named_'+str(i),'creo_delta',env(H,0x4352454F,human_fields[i],count=0 if i%2 else 65535))
        add(u['Name'],'tano_delta',bytes.fromhex(u['Hex']),expected[i+1])
    noop = dict(expected[4],Name='static_noop')
    add('static_noop','tano_delta',env(T,0x54414E4F,component(13,[]),count=0),noop)
    add('static_reseed','tano_baseline',baseline,dict(expected[0],Name='static_reseed'))
    add('static_reseed_end','end',prior.application('end',T))
    add('static_destroy','destroy',prior.application('destroy',T,255))
    add('static_recreate','create',static)
    add('static_recreate_baseline','tano_baseline',baseline,dict(expected[0],Name='static_recreate_baseline'))
    add('static_recreate_end','end',prior.application('end',T))
    final = row('static_recreated_show',file,key,spacer,list(map(ord,'Recreated static')),b'',[-3,9],7,255,32,True,False,False,'80000000',3,2,32)
    add('static_recreated_show','tano_delta',env(T,0x54414E4F,P('HBH',10,255,2)+units(final['NameCustomUnits']),count=65535),final)
    packets = []
    for app in apps:
        b = bytes.fromhex(app['Hex'])
        if len(b)<=489:packets.append(struct.pack('>HH',9,len(packets))+b)
        else:
            first = True
            while b:
                n = 485 if first else 489
                packets.append(struct.pack('>HH',13,len(packets))+(struct.pack('>I',len(bytes.fromhex(app['Hex'])) ) if first else b'')+b[:n])
                b=b[n:];first=False
    wires = [prior.frame(p) for p in packets]
    bad = bytearray(wires[0]);bad[-1]^=1
    order = [1,1,0,3,2]+list(range(4,len(wires)))+[0]
    peer = dict(Version=1,Synthetic=True,CatalogSha256=digest,CatalogHex=HEX(catalog),SceneGeneration=42,TangibleStage=stage,
                SessionResponseHex=HEX(struct.pack('>HII3BI',2,0x12345678,prior.SEED,2,1,4,496)),Applications=apps,
                PacketsHex=list(map(HEX,packets)),WiresHex=list(map(HEX,wires)),TranscriptHex=[HEX(bad)]+[HEX(wires[i]) for i in order])
    assert len(apps)<=32 and len(packets)<=64 and sum(p[:2]==b'\x00\x0d' for p in packets)<=8
    prior.delivery_oracle(peer)
    return peer

def verify(peer, output):
    assert peer == generate(), 'fixture identity differs from independent producer'
    assert output['Transport'] == prior.delivery_oracle(peer)
    for key in ('PacketsHex','WiresHex'):assert output[key] == peer[key]
    assert output['ApplicationsHex'] == [a['Hex'] for a in peer['Applications']]
    rows = [a['Expected'] for a in peer['Applications'] if 'Expected' in a]
    assert output['StaticRows'] == rows
    assert output['Revisions'] == list(range(1,10))
    assert output['IncarnationChecks'] and output['NegativeChecks']>=30
    print(f"PASS independent mixed replay: {len(peer['Applications'])} applications, {len(peer['PacketsHex'])} datagrams, {len(rows)} tangible states")

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--generate',type=Path);p.add_argument('--stage',type=Path)
    p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path)
    a=p.parse_args()
    if a.generate:
        for path in [a.generate,a.stage]:
            if path:assert path.resolve().is_relative_to((REPO/'local-private').resolve());path.parent.mkdir(parents=True,exist_ok=True)
        peer=generate();a.generate.write_bytes((json.dumps(peer,indent=2)+'\n').encode())
        if a.stage:a.stage.write_bytes((json.dumps(peer['TangibleStage'],indent=2)+'\n').encode())
        print('PASS bounded mixed fixture and standalone tangible stage exported')
    else:verify(json.loads(a.peer.read_text()),json.loads(a.verify.read_text()))
