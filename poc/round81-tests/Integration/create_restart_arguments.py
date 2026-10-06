"""Author explicit private per-run profiles and argument arrays for the interactive shell."""
import argparse, importlib.util, json, runpy
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]

def create(base, profile, runs, output):
    if type(runs) is not int or not 1 <= runs <= 32: raise ValueError('One to32 runs required')
    if type(base) is not list or not 2 <= len(base) <= 256 or any(type(x) is not str or len(x) > 4096 for x in base):
        raise ValueError('Bounded explicit argument array required')
    key = '--terrain-point-target-profile'
    if base.count(key) != 1 or base.index(key)+1 >= len(base): raise ValueError('One profile argument required')
    spec = importlib.util.spec_from_file_location('restart81_authoring', ROOT / 'poc/round81-tests/Networking/transport_fixtures.py')
    net = importlib.util.module_from_spec(spec); spec.loader.exec_module(net)
    net.admit(profile)
    authored = [net.reseed(profile, (profile['SoeConnectionId']+i) & 0xffffffff,
                           (profile['SoeSeed']+i) & 0xffffffff) for i in range(runs)]
    output = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](output, ROOT, directory=True)
    arguments = []
    for i, value in enumerate(authored, 1):
        path = output / f'run-{i}.json'; path.write_text(json.dumps(value, separators=(',', ':'))+'\n')
        row = list(base); row[row.index(key)+1] = str(path); arguments.append(row)
    raw = json.dumps(arguments, indent=2)
    if len(raw.encode()) > 262144: raise ValueError('Argument array exceeds launcher byte cap')
    (output/'arguments.json').write_text(raw+'\n')
    print('Created explicit offline run arguments:', output/'arguments.json')

if __name__ == '__main__':
    p=argparse.ArgumentParser();p.add_argument('--base-arguments',type=Path,required=True);p.add_argument('--profile',type=Path,required=True);p.add_argument('--runs',type=int,default=8);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    if a.base_arguments.stat().st_size>262144 or a.profile.stat().st_size>16384:raise ValueError('Bounded inputs required')
    create(json.loads(a.base_arguments.read_text(encoding='utf-8-sig')),json.loads(a.profile.read_text(encoding='utf-8-sig')),a.runs,a.output)
