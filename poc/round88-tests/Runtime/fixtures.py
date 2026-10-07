"""Accepted87 independent typed login/movement/disconnect peer; no native oracle values."""
import argparse,importlib.util,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(number,name):
    spec=importlib.util.spec_from_file_location(f'run88_{number}_{name}',ROOT/f'poc/round{number}-tests/Networking/{name}.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    peer=out.with_name(out.stem+'-accepted87-peer')
    path='object/creature/player/shared_authored.iff';crc=load(14,'peer').checksum(path)
    t=load(87,'produce').generate(peer,crc,0x1122334455667788,path,'terrain/authored.trn')
    soe=load(14,'peer');soe.SEED=t['ZoneSeed'];P=struct.pack
    good=bytes.fromhex(t['ExpectedUnhandledMovement'][0]);bad=bytes.fromhex(t['CorrectionApplications'][0])[:-1]
    multi=b'\0\x19'+bytes([len(good)])+good+bytes([len(bad)])+bad
    seq=len(t['WorldFrames'])+2
    t.update(UnknownThenMalformed=soe.frame(P('>HH',9,seq)+multi).hex().upper(),MalformedCorrection=soe.frame(P('>HH',9,seq)+bad).hex().upper(),Synthetic=True,ExpectedUnhandledCount=len(t['ExpectedDeferred'])+3)
    out.write_text(json.dumps(t,indent=2)+'\n');print(json.dumps(dict(Generated=True,WorldFrames=len(t['WorldFrames']),ExpectedUnhandled=t['ExpectedUnhandledCount'],Path=str(out))))
if __name__=='__main__':main()
