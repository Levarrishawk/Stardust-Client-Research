"""Static-only Round46 research runner."""
import importlib.util,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('family_lookup',root/'tools/python/verify_terrain_fractal_family_lookup.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
if len(sys.argv)!=5:raise SystemExit('usage: verify.py executable packages asset-root config')
print(json.dumps(module.verify(*map(Path,sys.argv[1:])),indent=2))
