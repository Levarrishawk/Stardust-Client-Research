"""Independent complete TANO3 wire/state fixtures for the bounded stage join."""
import argparse,json,struct
from pathlib import Path
p=argparse.ArgumentParser()
for name in ('runtime_peer','named_peer','output'):p.add_argument(name,type=Path)
a=p.parse_args();repo=Path(__file__).resolve().parents[3]
assert a.output.resolve().is_relative_to((repo/'local-private').resolve())
peer=json.loads(a.runtime_peer.read_text());names=json.loads(a.named_peer.read_text())['ExpectedNames']
terminal=names[3];obj=names[0];oid=struct.unpack_from('<Q',bytes.fromhex(peer['staticCreate']),6)[0]
P=lambda f,*v:struct.pack('<'+f,*v)
def string(raw,extended=False):return (P('HI',65535,len(raw)) if extended else P('H',len(raw)))+raw
def units(values):return P('I',len(values))+P('H'*len(values),*values)
def sid(file,key,spacer=0,fe=False,ke=False):return string(file,fe)+P('I',spacer)+string(key,ke)
def env(op,fields,count):return P('HIQIBIH',5,op,oid,0x54414e4f,3,len(fields)+2,count)+fields
def component(counter,commands):
 return P('HII',5,len(commands),counter)+b''.join(P('B',kind)+(P('i',key) if kind<2 else b'') for kind,key in commands)
file=bytes.fromhex(terminal['NameFileHex']);key=bytes.fromhex(terminal['NameKeyHex'])
objfile=bytes.fromhex(obj['NameFileHex']);objkey=bytes.fromhex(obj['NameKeyHex'])
spacer=0xa1b2c3d4
fields=P('I',0x80000000)+sid(file,key,spacer,True)+units([])+P('I',0xffffffff)+string(b'')+P('IIiii',3,7,9,9,-3)+P('IIIIB',1,2,3,100,1)
baseline=env(0x68a75f0c,fields,65535);basecharge=47+len(file)+len(key)+12
custom=list(map(ord,'Static terminal'))
updates=[
 ('hidden_custom',P('H',2)+units(custom)+P('H',4)+string(b'\x01\x02\x03',True)+P('HIHB',8,20,10,0)+component(10,[(0,-3),(1,11)])),
 ('shown_string_id',P('H',1)+sid(objfile,objkey,0,False,True)+P('H',2)+units([])+P('H',4)+string(b'',True)+component(10,[(0,9)])+P('HBHIHI',10,255,0,0,7,9)),
 ('safe_custom',component(11,[(2,0)])+component(12,[(1,7)])+P('H',2)+units([65,0,0xd800,66])+P('H',4)+string(b'\xff\x00')+P('HB',10,1)),
 ('restored_bytes',P('H',1)+sid(file,key,spacer,True)+P('H',2)+units([])+P('H',4)+string(b'')+component(13,[(2,0)])+P('HIHB',8,3,10,1))]
def row(name,f,k,s,u,raw,comps,counter,visible,display,origin,extra,fe,ke,rawextended,complexity,dmg,use):
 return dict(Name=name,NameFileHex=f.hex().upper(),NameKeyHex=k.hex().upper(),NameSpacer=s,NameCustomUnits=u,
  NameFileExtended=fe,NameKeyExtended=ke,CustomizationHex=raw.hex().upper(),CustomizationExtended=rawextended,
  Components=comps,ComponentCounter=counter,Visibility=visible,DisplayText=display,Origin=origin,
  AdditionalNames=len(f)+len(k)+2*len(u) if name!='baseline' else 0,RetainedDecodedBytes=basecharge+16+4*len(comps)+extra,
  ComplexityBits=complexity,ConditionDamage=dmg,UseCount=use)
expected=[
 row('baseline',file,key,spacer,[],b'',[-3,9],7,1,terminal['NameDisplay'],'PrimaryTable',0,True,False,False,'80000000',3,2),
 row('hidden_custom',file,key,spacer,custom,b'\x01\x02\x03',[9,11],10,0,'Static terminal','Custom',2*len(custom)+3,True,False,True,'80000000',20,2),
 row('shown_string_id',objfile,objkey,0,[],b'',[9,11],10,255,obj['NameDisplay'],'PrimaryTable',len(objfile)+len(objkey),False,True,True,'00000000',20,9),
 row('safe_custom',objfile,objkey,0,[65,0,0xd800,66],b'\xff\x00',[7],12,1,'A\\u0000\ufffdB','Custom',len(objfile)+len(objkey)+8+2,False,True,False,'00000000',20,9),
 row('restored_bytes',file,key,spacer,[],b'',[],13,1,terminal['NameDisplay'],'PrimaryTable',len(file)+len(key),True,False,False,'00000000',3,9)]
# Current baseline name references survive a custom-only update; only custom storage adds charge.
expected[1]['AdditionalNames']=2*len(custom)
fixture=dict(Version=1,Synthetic=True,ObjectId=oid,Create=peer['staticCreate'],Baseline=baseline.hex().upper(),BaselineDecodedBytes=basecharge,
 MaximumStoreBytes=512,Updates=[dict(Name=name,Hex=env(0x12862153,field,0 if n%2 else 65535).hex().upper()) for n,(name,field) in enumerate(updates)],Expected=expected,
 LateUnsupported=env(0x12862153,P('H',4)+string(b'late')+P('HB',11,7),2).hex().upper(),
 IntermediateOverBudget=env(0x12862153,P('H',4)+string(bytes(513))+P('H',4)+string(b''),2).hex().upper())
a.output.write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
print('PASS: independent complete TANO3 baseline and ordered common/name/raw/components stage fixture')
