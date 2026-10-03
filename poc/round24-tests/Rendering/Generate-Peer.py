"""Synthetic complete baseline visibility reseeds; NOT a common-field delta fixture."""
import json,runpy,struct,sys
from pathlib import Path
output=Path(sys.argv[1]);repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round23-tests/Rendering/Generate-Peer.py'),str(output)]
runpy.run_path(sys.argv[0],run_name='__main__')
peer=json.loads(output.read_text());original=bytes.fromhex(peer['human_baseline'])
# Empty StringId/name/customization/components seed; independently specified common prefix.
offset=25+struct.calcsize('<fHIHIIHIIIIII');assert offset==71 and original[offset]==1
for name,value in [('visibility_hidden_baseline',0),('visibility_shown_baseline',255)]:
 packet=bytearray(original);packet[offset]=value;peer[name]=packet.hex().upper()
output.write_text(json.dumps(peer,indent=2)+'\n')
print('PASS: two independent complete baseline visibility reseeds; 0/255 raw bytes')
