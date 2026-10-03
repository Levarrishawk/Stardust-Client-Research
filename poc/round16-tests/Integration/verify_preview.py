"""Check observable bounded continuous original scene receipts; no pixel or heap claim."""
import json
import sys
from pathlib import Path
text=Path(sys.argv[1]).read_text(encoding="utf-8-sig")
mode=sys.argv[2] if len(sys.argv)>2 else "normal"
frames=[json.loads(line.split("pump_frame=",1)[1]) for line in text.splitlines() if "pump_frame=" in line]
assert len(frames)>=4
assert [r["frames"] for r in frames]==list(range(1,len(frames)+1))
for row in frames:
    step=row["receipt"]
    assert 0<=step["AcceptedMessages"]+step["DroppedMessages"]<=2
    assert 0<=step["PublishedResources"]<=1
    assert 0<=step["PendingMessages"]<=16 and 0<=step["PendingPublications"]<=2
    assert 0<=row["actors"]<=2
if mode=="cancel":
    assert "PASS: pumped canceled archives=2 publications=0 drained=true" in text
    assert all(r["actors"]==0 for r in frames)
else:
    states=[json.loads(line.split("pump_state=",1)[1]) for line in text.splitlines() if "pump_state=" in line]
    assert [r["stage"] for r in states]==["initial","recreated"]
    assert [(r["calls"],r["closed"],r["actors"]) for r in states]==[(2,2,2),(3,3,2)]
    assert states[1]["frames"]>states[0]["frames"]
    assert sum(r["receipt"]["PublishedResources"] for r in frames)==3
    assert "PASS: pumped world continuous=true budgets=true original_recipes=2 static_only_reload=true drained=true" in text
assert "Pumped world failed" not in text and "SCRIPT ERROR" not in text
print("PASS: continuous frame budgets, original lifecycle receipts and drain",mode)
