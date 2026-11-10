from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget\local-hit-doc-backup')
backup.mkdir(exist_ok=False)
def save(p,s):
    shutil.copy2(p,backup/p.name)
    t=p.with_name(p.name+'.g7tmp');t.write_text(s,encoding='utf-8');t.replace(p)
receipt='''

### October20 player-local shield-hit acceptance

On BFD996B2, station Follow closes914m to658m and the ordinary100-credit full-repair conversation reports completion. The periodic state cap had expired before this repair, so its postcondition is not re-claimed from that stale state log. Map autopilot reaches the NPC area and full-stop cancels normally. Keymap and the fresh key receipt agree Tab=CMD_spaceTargetClosestEnemy, press1056; native GameControllerMessage.def has CM_spaceTargetClosestEnemy at1056. Its distant capital-ship selection is not evidence of a numeric-input translation mismatch. The closest-friend key selects a nearby Trade Federation Guardian(Z-95,Tier2); current ClientShipTargeting defines friend as not isEnemy(). Moving-NPC Follow engages and tracks at180-370m. These are current-source/live observations, not original executable parity proof.

Normal /ship weaponGroup fireProjectile visibly consumes capacitor while following. The selected Guardian remains100% in the inspected frames; no player-attributed outgoing damage or XP is accepted. The refined independent budget then records event32 at t311720.860, decoded=1/local=1/type0, old1.000000→new0.765920, chassis800/800. This confirms player-local Pre-CU shield-hit decoding/dispatch while preserving chassis state; attacker identity and complete effect/audio parity are not established. Remote events0-31 and two NPC destruction events are distinct, current-process receipts. The old local-destruction receipt is preserved separately and is not misattributed to this process.

Follow reaches the ordinary effective-sublight-limit warning near(468,-5772,-8160); full-stop cancels and settles at0. A normal map route back to Naboo Station is engaged afterward. No fresh fatal report is produced (latest remains20261020070657). Station Stop Conversing clicks were followed by capacitor use in several observations; possible UI click-through/firing needs a bounded input-ownership check before any source fix. Automatic pilot mode remains restored; extra full-stop binding and temporary ship trace still require cleanup. G7/G8 remain open.
'''
p=root/'docs/conversion/SPACE_COMPATIBILITY.md';save(p,p.read_text(encoding='utf-8')+receipt)
p=root/'PROGRESS.md';save(p,p.read_text(encoding='utf-8')+'\nOctober20 BFD996B2 now confirms a player-local shield hit (decoded, type0,1.0→0.765920; chassis800/800), plus moving-NPC Follow and normal full-stop at the system-edge warning. Outgoing damage/XP remains unaccepted. Possible station-conversation click-through is a new bounded UI question. [Receipt](docs/conversion/SPACE_COMPATIBILITY.md#october20-player-local-shield-hit-acceptance).\n')
p=root/'docs/conversion/G7_ACCEPTANCE_MATRIX.md';save(p,p.read_text(encoding='utf-8')+'\nOctober20 BFD996B2: player-local Pre-CU shield-hit dispatch now confirmed; moving non-enemy NPC Follow/full-stop pass. Outgoing damage attribution/XP and effect parity remain open; station-conversation click-through needs investigation. [Receipt](SPACE_COMPATIBILITY.md#october20-player-local-shield-hit-acceptance).\n')
p=root/'HANDOFF.md';s=p.read_text(encoding='utf-8');a=s.index('Current resume point:');b=s.index('\n\n',a)
resume='Current resume point: October20 G7 space. BFD996B2/PDB063C9F38 deployed;32 warnings/0 errors. Player-local hit confirmed t311720.860: type0 shield1→0.765920, chassis800/800. Prior773 local destruction→station recovery12.479s preserved separately. Moving Guardian Follow works; Tab1056 matches native closestEnemy. Outgoing damage/XP not accepted. Current normal autopilot returns to Naboo Station from system edge(468,-5772,-8160), after full-stop0. Automatic pilot mode restored. Extra Ctrl+Shift+Backspace full-stop binding still temporary, original End retained. Ship trace temporarily on, command trace off. Preserve current receipts and restore tracing false after probe; investigate observed Stop Conversing capacitor use as possible UI click-through. No subagents/server/original-client changes/publication/player cloning. G7/G8 open.'
save(p,s[:a]+resume+s[b:])
print('Recorded player-local shield-hit acceptance and precise remaining limits.')
