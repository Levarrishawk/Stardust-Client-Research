"""Synthetic outer-field fixtures; deliberately no speculative inner customization decoder."""
import argparse,json,struct
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
H=0xF122334455667788

def produce():
    cases=[]
    for name,raw,extended in [('empty',b'',False),('packed-inspection',bytes.fromhex('010301FF0102FF0283FEFF02FF03'),False),
            ('embedded-bytes',bytes.fromhex('0080FF0400'),False),('all-octets',bytes(range(256)),False),
            ('extended-small',bytes.fromhex('0101017FFF03'),True),('extended-empty',b'',True)]:
        outer=(P('HI',65535,len(raw)) if extended else P('H',len(raw)))+raw
        # Independently handpacked complete 18-field baseline; no original player data.
        prefix=P('fHIHII',1,0,0,0,0,1)
        suffix=P('IIIIIIBBBQfIQII',0,7,0,0,0,100,1,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9)
        fields=prefix+outer+suffix
        def env(op,tag,count,body):return P('HIQIBIH',5,op,H,tag,3,len(body)+2,count)+body
        cases.append(dict(Name=name,Raw=raw.hex().upper(),Extended=extended,Baseline=env(0x68A75F0C,0x4352454F,18,fields).hex().upper(),
            Delta=env(0x12862153,0x4352454F,1,P('H',4)+outer).hex().upper(),Tano=env(0x68A75F0C,0x54414E4F,11,prefix+outer+suffix[:25]).hex().upper()))
    return cases

def verify(fixtures,export):
    assert len(export['Cases'])==len(fixtures)==6
    for f,r in zip(fixtures,export['Cases']):
        b=bytes.fromhex(f['Baseline']); pos=45
        prefix=struct.unpack_from('<H',b,pos)[0];pos+=2
        length=struct.unpack_from('<I',b,pos)[0] if prefix==65535 else prefix
        if prefix==65535:pos+=4
        raw=b[pos:pos+length]
        assert raw.hex().upper()==f['Raw']==r['Raw']
        assert r==dict(Name=f['Name'],Raw=f['Raw'],Extended=f['Extended'],Baseline=f['Baseline'],Delta=f['Delta'],Tano=f['Tano'])
    assert export['NegativeChecks']>500
    print(f"Independent outer-field oracle: 6 exact owned cases; {export['NegativeChecks']} malformed/budget/unsupported probes")

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--generate');p.add_argument('--peer');p.add_argument('--verify');a=p.parse_args()
    if a.generate:Path(a.generate).write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8')
    else:verify(json.loads(Path(a.peer).read_text()),json.loads(Path(a.verify).read_text()))
