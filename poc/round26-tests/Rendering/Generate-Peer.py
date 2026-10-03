"""Opaque terminal-world fixture plus explicit supplied CREO-model synthetic names."""
import json,struct,runpy,sys
from pathlib import Path
output=Path(sys.argv[1]);repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round25-tests/Rendering/Generate-Peer.py'),str(output)]
runpy.run_path(sys.argv[0],run_name='__main__');peer=json.loads(output.read_text())
start=bytearray.fromhex(peer['start']);struct.pack_into('<Q',start,7,0xE122334455667799);peer['static_start']=start.hex().upper()
output.write_text(json.dumps(peer,indent=2)+'\n');print('PASS: opaque TANO world and explicit supplied projection names; no typed wire claim')
