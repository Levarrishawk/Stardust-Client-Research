"""Independent protected fresh-scene peer and binary32 controlled roots."""
import argparse,importlib.util,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools'))
def load(number,name):
    spec=importlib.util.spec_from_file_location(f'pacer90_{number}_{name}',ROOT/f'poc/round{number}-tests/Networking/{name}.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    soe=load(14,'peer');path='object/creature/player/shared_authored.iff'
    t=load(87,'produce').generate(out.with_name(out.stem+'-peer'),soe.checksum(path),0x1122334455667788,path,'terrain/authored.trn')
    soe.SEED=t['ZoneSeed'];f32=lambda x:struct.unpack('<f',struct.pack('<f',x))[0]
    z=f32(-3.75);apps=[];wires=[]
    for i in range(3):
        for step in range(4):z=f32(z+f32(f32(.5)*f32(.1)))
        app=load(86,'produce').transform(0x1122334455667788,0x23,(i+1)*400,i+1,[1.25,2.5,z],.5)
        apps.append(app.hex().upper());wires.append(soe.frame(struct.pack('>HH',9,i+3)+app).hex().upper())
    t.update(ExpectedControlledApplications=apps,ExpectedControlledWires=wires,Synthetic=True)
    out.write_text(json.dumps(t,indent=2)+'\n');print(json.dumps(dict(Generated=True,Path=str(out))))
if __name__=='__main__':main()
