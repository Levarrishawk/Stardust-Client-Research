"""Synthetic original-preparation oracle and independent protected application frames."""
import argparse,importlib.util,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools'))
def load(path,name):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    assets=load('poc/round88-tests/Assets/peer.py','assets88');directory=out.with_name(out.stem+'-installation');assets.generate(directory)
    f=json.loads((directory/'fixtures.json').read_text());soe=load('poc/round14-tests/Networking/peer.py','soe14')
    ptat=runpy.run_path(str(ROOT/'poc/round32-tests/Assets/verify_ptat_input.py'),run_name='fixture_import')
    terrain=ptat['encode'](b'0014')
    assets.oracle.preflight.archive(directory/'good/terrain.tre',[('terrain/authored.trn',terrain,0)])
    with (directory/'good/mounts.cfg').open('ab') as stream:stream.write(b'searchTree_00_6=terrain.tre\n')
    networking=load('poc/round87-tests/Networking/produce.py','networking87');peer=out.with_name(out.stem+'-peer');t=networking.generate(peer,soe.checksum('object/root.iff'),0x1122334455667788,'object/root.iff','terrain/authored.trn')
    f.update(Peer=t,ExpectedTerrainBytes=len(terrain),Synthetic=True);out.write_text(json.dumps(f,indent=2)+'\n');print(json.dumps(dict(Generated=True,Path=str(out))))
if __name__=='__main__':main()
