from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget\doc-backup')
backup.mkdir(exist_ok=False)
def save(p,s):
    shutil.copy2(p,backup/p.name)
    t=p.with_name(p.name+'.g7tmp')
    t.write_text(s,encoding='utf-8')
    t.replace(p)
receipt='''

### October20 live hit dispatch and local destruction recovery

Candidate773DD0E0's normal station repair changes chassis0/800 to800/800 in the live state receipt. Manual20% throttle leaves station geometry and full-stop works; a new map autopilot approach then reaches the NPC area near(1413,-4197,-5571). Live ShipController receipts decode shield(type0) and armor(type1) hits on nearby NPCs, with decreasing condition fractions and plausible240/360 chassis maxima. All64 recorded hits have decoded=1/local=0. This confirms live Pre-CU hit-envelope translation/dispatch for those occurrences, not player-attributed outgoing damage. Ambient NPC combat is present; observed target shield changes cannot be attributed to the test player.

The independent destruction receipt records an NPC event at t309274.456 and a local event at t309371.583, severity0.440 and chassis0/800. The next accepted space scene arrives at t309384.062, position(-2499.260,905.490,-6460.670), near Naboo Station with a fresh greeting. The interval is12.479s. Client remains running and accepts normal /quit. This is Confirmed for local DestroyShipMessage receipt, zero authoritative chassis state and accepted station recovery in this bounded encounter. The complete explosion/camera sequence was obscured by Options, so visual parity is not accepted. No player deathblow or cloning is performed. Equal-fraction hit-effect parity and component-destruction coverage remain open.

The routine state cap expired before combat, and ambient hits consumed the shared64-event cap. BFD996B2EF22230EE7466458F1374B7A29942DDF41C6327C35FE094619D1090A / matching PDB063C9F38C5C5EE987041493380688D1F44B3ABE734C1446DD2CD48B97B9D4A32 separates the opt-in hit budget into64 local and32 remote events. Only diagnostic accounting changes; original hit handling is unchanged. Build32 warnings/0 errors17.41s, matching pair deployed/reverified Experimental-only after normal exit. The27,708-check native-suite receipt belongs to773DD0E0; no new protocol behavior or fixture change is claimed for this diagnostic refinement. Prior EXE/PDB, source and all ship logs are preserved privately.

Automatic pilot mouse mode is restored through Options and saved. Virtual Joystick visibly steers the ship in the comparison; precise aim/physical parity is not accepted. The extra full-stop binding remains temporary test setup. Fresh BFD login/roster selection returns to Moenia(4731,4,-4677); ship inspection at the terminal shows chassis0%, front armor97%, rear armor100%, all other installed components100%. An external input/movement interruption places Kova at(4774,4,-4685); that final setup is not counted as autonomous navigation. Terminal double-click at1m opens ship selection and normal relaunch starts. Command trace off; bounded ship trace temporarily on. Player-local hit delivery, player-attributed outgoing damage, space mission/XP and broader G7/G8 remain open.
'''
p=root/'docs/conversion/SPACE_COMPATIBILITY.md'
save(p,p.read_text(encoding='utf-8')+receipt)
p=root/'PROGRESS.md'
s=p.read_text(encoding='utf-8')
old='`773DD0E0F6151E8904204532390C8AC51DD2A4340C39D07370EEC33783B09858` (matching PDB24529F91; bounded hit/destruction diagnostics; Pre-CU ship-hit translation retained; ship tracing temporarily on, command tracing off)'
new='`BFD996B2EF22230EE7466458F1374B7A29942DDF41C6327C35FE094619D1090A` (matching PDB063C9F38; separate local/remote hit-probe budgets; Pre-CU ship-hit translation retained; ship tracing temporarily on, command tracing off)'
assert old in s
save(p,s.replace(old,new,1)+'\nOctober20: live NPC shield/armor hit dispatch now confirmed; local DestroyShipMessage with chassis0/800 is followed12.479s later by accepted station recovery, without client exit. Normal Quit/fresh login and retained damaged ship/relaunch also pass. Explosion visuals were obscured; outgoing damage/XP remain pending. BFD996B2 diagnostic refinement builds32 warnings/0 errors, preserving an independent local-hit allowance. Automatic pilot mouse mode restored. [Receipt](docs/conversion/SPACE_COMPATIBILITY.md#october20-live-hit-dispatch-and-local-destruction-recovery). G7/G8 remain open.\n')
p=root/'docs/conversion/G7_ACCEPTANCE_MATRIX.md'
save(p,p.read_text(encoding='utf-8')+'\nOctober20: NPC OnShipHit decoding/dispatch and local DestroyShipMessage→accepted station recovery now have bounded live evidence (12.479s, chassis0/800, no client crash). Explosion visuals, player-attributed damage and space mission/XP remain open. BFD996B2 separates local64/remote32 opt-in hit budgets. [Receipt](SPACE_COMPATIBILITY.md#october20-live-hit-dispatch-and-local-destruction-recovery).\n')
p=root/'HANDOFF.md'
s=p.read_text(encoding='utf-8');a=s.index('Current resume point:');b=s.index('\n\n',a)
resume='Current resume point: October20 G7 space. BFD996B2/PDB063C9F38 deployed;32 warnings/0 errors. Native suite27,708/11,864 plus5 belongs to preceding773DD0E0. Local destruction confirmed: t309371.583 chassis0/800→accepted station scene t309384.062 (12.479s). NPC shield/armor hit dispatch passes; player-local hits/outgoing attribution pending. Probe now reserves64 local/32 remote hits. Fresh login returns Moenia; retained damaged ship/terminal double-click/relaunch pass. Automatic pilot mouse mode restored. Extra Ctrl+Shift+Backspace full-stop binding still temporary; original End retained. Ship trace temporarily on, command trace off. Preserve receipts then restore ship trace false. Latest flight is relaunch for local-hit evidence. No subagents/server/original-client changes/publication/player cloning. G7/G8 open.'
save(p,s[:a]+resume+s[b:])
print('Recorded local destruction recovery and updated deployed candidate in four documents.')
