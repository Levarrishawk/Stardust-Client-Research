"""Complete synthetic name baseline reseeds; diagnostic names, not named deltas."""
import json,struct,runpy,sys
from pathlib import Path
output=Path(sys.argv[1]);repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round24-tests/Rendering/Generate-Peer.py'),str(output)]
runpy.run_path(sys.argv[0],run_name='__main__');peer=json.loads(output.read_text())
original=bytes.fromhex(peer['human_baseline']);P=lambda f,*v:struct.pack('<'+f,*v)
assert original[29:41]==bytes(12)
def baseline(file,key,spacer,units,visibility=1):
 suffix=bytearray(original[41:]);suffix[30]=visibility
 body=original[25:29]+P('H',len(file))+file+P('I',spacer)+P('H',len(key))+key+P('I',len(units))+P('H'*len(units),*units)+suffix
 return P('HIQIBIH',5,0x68A75F0C,0xF122334455667788,0x4352454F,3,len(body)+2,18)+body
first=baseline(b'diagnostic',b'alpha',0xF1234567,[])
latest=baseline(b'diagnostic',b'beta',0xF1234568,list(map(ord,'LATEST CAPTION')))
peer.update(name_first_baseline=first.hex().upper(),name_latest_baseline=latest.hex().upper(),
 name_hidden_baseline=baseline(b'diagnostic',b'beta',0xF1234568,list(map(ord,'LATEST CAPTION')),0).hex().upper())
output.write_text(json.dumps(peer,indent=2)+'\n');print('PASS: independent complete name baseline reseeds; explicit diagnostic mappings')
