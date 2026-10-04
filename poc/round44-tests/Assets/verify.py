"""Research-only runner; does not execute or load the original executable."""
import importlib.util
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location(
    "slope_initialization", root / "tools/python/verify_terrain_slope_initialization.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
if len(sys.argv) != 3:
    raise SystemExit("usage: verify.py executable packages")
print(json.dumps(module.verify(Path(sys.argv[1]), Path(sys.argv[2])), indent=2))
