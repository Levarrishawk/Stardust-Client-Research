"""Compare independent synthetic motion bytes to continuous native/engine/body observations."""
import json,math,struct,sys
from pathlib import Path
text=Path(sys.argv[1]).read_text(encoding="utf-8-sig")
peer=json.loads(Path(sys.argv[2]).read_text(encoding="utf-8-sig"))
states=[json.loads(line.split("motion_state=",1)[1]) for line in text.splitlines() if "motion_state=" in line]
bodies=[json.loads(line.split("motion_body=",1)[1]) for line in text.splitlines() if "motion_body=" in line]
assert [r["Name"] for r in states]==["moved","wrapped","local"]
def native(key):
    raw=bytes.fromhex(peer[key])
    count,opcode,oid,x,z,y,counter,speed,direction=struct.unpack("<HIQhhhIbb",raw)
    assert count==8 and opcode==0x1B24F808 and direction==0
    return [x*.25,y*.25,z*.25],counter
for state,key in zip(states[:2],["move_a","move_b"]):
    position,counter=native(key)
    assert state["Native"]==position and state["MovementCounter"]==counter
    assert state["Engine"]==[position[0],position[1],-position[2]]
assert states[2]["Native"]==[-.75,0,.5] and states[2]["Engine"]==[-.75,0,-.5]
assert states[2]["MovementCounter"] is None
assert [r["Revision"] for r in states]==sorted({r["Revision"] for r in states})
assert len({r["CreateHex"] for r in states})==1 and all(r["calls"]==3 or r["Name"]=="moved" and r["calls"]==2 for r in states)
assert len(bodies)==3
for body in bodies:
    assert abs(body["Radius"]-.7)<1e-6 and abs(body["LocalCenter"]["Y"]-1.4)<1e-6
    assert body["LocalCenter"]["X"]==body["LocalCenter"]["Z"]==0
    assert len(body["ProfileSources"])==5 and len(body["Fields"])==8
    assert abs(body["CollisionHeight"]-2.52)<1e-5 and abs(body["StepHeight"]-.7)<1e-6
assert "PASS: pumped motion native_pose=true height_once=true resource_retained=true stale_rejected=true body_bridge=true" in text
assert "Pumped world failed" not in text
print("PASS: independent wire/native/Godot/body observations, wrap/stale, clear/local resume and retained raw create")
