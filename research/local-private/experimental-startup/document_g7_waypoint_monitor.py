from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget\waypoint-monitor-doc-backup')
backup.mkdir(exist_ok=False)
def save(name,s):
    p=root/name;shutil.copy2(p,backup/p.name)
    t=p.with_name(p.name+'.g7tmp');t.write_text(s,encoding='utf-8');t.replace(p)
receipt='''

### October20 pilot journal and restored waypoint monitor

The operator positions Kova at V3-FX, Naboo(4764,6,-4795), and releases controls. Ordinary world double-click produces the trainer's localized first-assignment-active response. Ctrl-J shows Rebel Alliance → Naboo system: Patrol Nabooan frontier, including localized task instructions and return-to-V3-FX guidance. This accepts populated pilot-quest projection and this NPC double-click occurrence; mission acceptance itself was not performed in this slice.

The operator subsequently launches, starts flight toward the first patrol waypoint and explicitly releases controls again. In space the onscreen waypoint monitor is absent, while the forward-projected waypoint distance is already visible. Options → Interface shows Onscreen Waypoint Monitor unchecked. Clicking that existing checkbox immediately constructs the original panel with Patrol the Nabooan Frontier, distance4357m and green direction arrow. After OK it remains visible and updates to3495m as the ship approaches. Show Arrows to Waypoints Onscreen was already enabled. This is a Confirmed saved-display-setting cause and live restoration, not missing server mission data or a new source fix. Full-process preference persistence and waypoint replacement/arrival remain pending.

The existing research [waypoint-monitor contract](../../../Stardust-Client/research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_WAYPOINT_MONITOR_UI_COMMAND.md) distinguishes the named monitor child from projected waypoint indicators. Current SwgCuiHudWindowManager creates WS_WaypointMonitorBox only when CuiPreferences::getUseWaypointMonitor() is true; SwgCuiOptUi binds the checkbox to that preference and its callback. The current source initializes the preference false, then registers the saved LocalMachineOptionManager value. No default or saved preference is overridden in source by this restoration.

BFD996B2 remains unchanged. No new build/native suite, server/original-client action or publication. Kova continues the operator-started ordinary patrol approach; G7/G8 remain open. The user selected Virtual Joystick before releasing controls; retain that choice rather than silently restoring an earlier agent comparison. Temporary extra full-stop binding and capped ship tracing still await probe cleanup.
'''
p='docs/conversion/SPACE_COMPATIBILITY.md';save(p,(root/p).read_text(encoding='utf-8')+receipt)
for p,link in [('PROGRESS.md','docs/conversion/SPACE_COMPATIBILITY.md'),('docs/conversion/G7_ACCEPTANCE_MATRIX.md','SPACE_COMPATIBILITY.md')]:
    save(p,(root/p).read_text(encoding='utf-8')+'\nOctober20 resumed at operator-positioned V3-FX: NPC double-click and populated pilot journal pass. Operator launches toward first patrol waypoint and releases controls. Missing onscreen monitor is traced to its unchecked Interface option; enabling it restores localized name, live distance and direction arrow without a rebuild. Arrival/mission combat/reward and restart persistence remain open. [Receipt]('+link+'#october20-pilot-journal-and-restored-waypoint-monitor).\n')
p='HANDOFF.md';s=(root/p).read_text(encoding='utf-8');a=s.index('Current resume point:');b=s.index('\n\n',a)
resume='Current resume point: October20 G7 pilot patrol live, controls released. BFD996B2/PDB063C9F38 unchanged/running. Operator positions Kova at V3-FX; normal double-click and localized active patrol journal pass. Operator launches and starts autopilot toward first waypoint, then releases controls. Missing onscreen waypoint monitor is its unchecked Options Interface preference; enabling restores panel/name/distance/arrow, saved with OK. No source fix. Ship continues first patrol approach (last3495m,390HUD speed). Retain user-selected Virtual Joystick. Temporary Ctrl+Shift+Backspace full-stop binding/ship tracing cleanup pending. BFD local hit and prior773 destruction receipts separately preserved; outgoing attributed damage/space mission completion/XP remain open. No subagents/server/original-client changes/publication/player cloning; G7/G8 open.'
save(p,s[:a]+resume+s[b:]);print('Recorded pilot journal and waypoint-monitor restoration in four source documents.')
