"""Independent original-scene compact motion input; synthetic values, no server contact."""
import json,struct,sys
from pathlib import Path
base=json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
human=struct.unpack_from("<Q",bytes.fromhex(base["human_create"]),6)[0]
def packet(x,z,counter):
    return struct.pack("<HIQhhhIbb",8,0x1B24F808,human,x,z,0,counter,1,0).hex().upper()
result={"move_a":packet(-2,4,0xFFFFFFFE),"move_b":packet(-6,-4,0),"move_stale":packet(8,8,0xFFFFFFFF)}
Path(sys.argv[2]).write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
print("PASS: independently packed quarter-unit motion, wrap and stale counter fixture")
