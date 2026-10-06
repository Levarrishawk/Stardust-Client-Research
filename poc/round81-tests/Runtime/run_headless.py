"""Run an explicitly prepared private Godot project, headless and bounded."""
import argparse,json,runpy,subprocess
from pathlib import Path
def main():
    p=argparse.ArgumentParser();p.add_argument('--godot',type=Path,required=True);p.add_argument('--args',type=Path,required=True);p.add_argument('--console-out',type=Path,required=True);a=p.parse_args()
    repo=Path(__file__).resolve().parents[3];private=(repo/'local-private').resolve();args=json.loads(a.args.read_text())
    assert isinstance(args,list) and all(type(v) is str for v in args) and '--headless' in args
    assert private in Path(args[args.index('--path')+1]).resolve().parents
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    guard(Path(args[args.index('--log-file')+1]),repo);out=guard(a.console_out,repo)
    assert a.godot.is_file() and a.godot.name.startswith('Godot_') and a.godot.name.endswith('_console.exe')
    with out.open('w',encoding='utf-8') as stream:
        result=subprocess.run([str(a.godot),*args],stdout=stream,stderr=subprocess.STDOUT,timeout=45)
    print(json.dumps(dict(ReturnCode=result.returncode,Console=str(out),Headless=True)))
    raise SystemExit(result.returncode)
if __name__=='__main__':main()
