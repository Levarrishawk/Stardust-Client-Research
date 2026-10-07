"""Independent advertised endpoint lists and accepted synthetic preparation."""
import argparse,importlib.util,json,runpy,struct,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools'))
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    prepared=out.with_name(out.stem+'-prepared.json')
    subprocess.run([sys.executable,str(ROOT/'poc/round89-tests/Runtime/fixtures.py'),'--out',str(prepared)],check=True,capture_output=True)
    f=json.loads(prepared.read_text());peer=f['Peer']
    spec=importlib.util.spec_from_file_location('endpoint91_soe',ROOT/'poc/round14-tests/Networking/peer.py');soe=importlib.util.module_from_spec(spec);spec.loader.exec_module(soe);soe.SEED=peer['LoginSeed']
    P=struct.pack;header=lambda n,h:P('<HI',n,h)
    def text(s):b=s.encode('ascii');return P('<H',len(b))+b
    def lists(host='zone.invalid',port=44453,galaxy=7,characters=1,clusters=1,statuses=1,ping=44454):
        cluster=P('<I',galaxy)+text('Authored')+P('<I',0xffff8f80)
        status=P('<I',galaxy)+text(host)+P('<HHIIIII',port,ping,100,3250,8,0xffff8f80,2)+b'\0'
        char=P('<I',1)+b'A\0'+P('<IQII',peer['TemplateCrc'],peer['ObjectId'],galaxy,1)
        raw=[header(2,0xC11C63B9)+P('<I',clusters)+cluster*clusters+P('<I',8),header(3,0x3436AEB6)+P('<I',statuses)+status*statuses,header(2,0x65EA4574)+P('<I',characters)+char*characters]
        return [peer['LoginReplies'][0]]+[soe.frame(P('>HH',9,i+1)+app).hex().upper() for i,app in enumerate(raw)]
    definitions=[('matching',{},True,False),('casefold',dict(host='ZONE.INVALID'),True,False),('literal',dict(host='127.0.0.2'),True,False),('ping-ignored',dict(ping=1),True,False),('wrong-host',dict(host='elsewhere.invalid'),False,False),('wrong-literal',dict(host='127.0.0.3'),False,False),('wrong-zone-port',dict(port=44454),False,False),('wrong-galaxy',dict(galaxy=8),False,False),('missing-character',dict(characters=0),False,False),('duplicate-character',dict(characters=2),False,False),('missing-cluster',dict(clusters=0),False,False),('missing-status',dict(statuses=0),False,False),('duplicate-cluster',dict(clusters=2),False,True),('duplicate-status',dict(statuses=2),False,True),('uri',dict(host='https://zone.invalid'),False,False),('ipv6',dict(host='::1'),False,False),('shorthand',dict(host='127.1'),False,False),('wrong-fixed-ip',{},False,False),('ping-peer',{},False,False)]
    f['Cases']=[dict(Name=name,LoginReplies=lists(**kw),Accepted=good,ReceiveFault=fault,ApprovedHost='127.0.0.2' if name=='literal' else 'zone.invalid',FixedAddress=0x7f000003 if name=='wrong-fixed-ip' else 0x7f000002,FixedPort=44454 if name=='ping-peer' else 44453) for name,kw,good,fault in definitions]
    out.write_text(json.dumps(f,indent=2)+'\n');print(json.dumps(dict(Generated=True,Cases=len(definitions),Path=str(out))))
if __name__=='__main__':main()
