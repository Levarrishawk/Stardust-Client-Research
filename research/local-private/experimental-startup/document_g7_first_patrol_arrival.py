from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget\first-patrol-doc-backup')
backup.mkdir(exist_ok=False)
def save(name,s):
    p=root/name;shutil.copy2(p,backup/p.name)
    t=p.with_name(p.name+'.g7tmp');t.write_text(s,encoding='utf-8');t.replace(p)
receipt='''

### October20 first patrol arrival and operator flight continuation

The next live observation on unchanged BFD996B2 shows Mission Update: You have arrived at a patrol nav point; Mission in Progress: Naboo system: Patrol Nabooan frontier; the sector-completion warning; and Mission Update: Patrol waypoint located. The restored monitor remains populated, displaying455m while the projected marker reads453m near ship position(6318,-4873,-2056). These observations confirm one natural patrol arrival/progress notification and visible replacement waypoint guidance. The small distance difference is consistent with separate presentation sampling while moving; exact timing parity is not established. Local VortexSquadronScreenplay defines the next point at(6031,-4540,-1962), consistent with this observed distance, but deployed source identity is not asserted.

The operator returns to the keys and explicitly takes over to finish the patrol and land. Computer input stops immediately. Remaining patrol/ambush outcome, outgoing player damage, pilot XP, mission completion/reward and final landing are not yet accepted from this observation. Await the operator's result; retain any subsequent trace separately. No rebuild, source behavior change, native-suite rerun, server edit, player cloning or publication. G7/G8 remain open.
'''
p='docs/conversion/SPACE_COMPATIBILITY.md';save(p,(root/p).read_text(encoding='utf-8')+receipt)
for p,link in [('PROGRESS.md','docs/conversion/SPACE_COMPATIBILITY.md'),('docs/conversion/G7_ACCEPTANCE_MATRIX.md','SPACE_COMPATIBILITY.md')]:
    save(p,(root/p).read_text(encoding='utf-8')+'\nOctober20 first natural pilot-patrol arrival/progress and replacement waypoint guidance pass on unchanged BFD996B2. Operator takes controls to complete patrol/landing; agent input stopped. Completion/reward/XP and outgoing damage remain pending. [Receipt]('+link+'#october20-first-patrol-arrival-and-operator-flight-continuation).\n')
p='HANDOFF.md';s=(root/p).read_text(encoding='utf-8');a=s.index('Current resume point:');b=s.index('\n\n',a)
resume='Current resume point: October20 G7 pilot patrol, OPERATOR CONTROLS. BFD996B2/PDB063C9F38 running unchanged. First natural patrol arrival/progress notification and replacement waypoint guidance observed near6318,-4873,-2056 (monitor455m). Missing monitor was unchecked Interface preference; enabled through UI, now works. Operator explicitly takes over to finish patrol and land; STOP computer input until released. Await outcome before accepting completion/reward/XP/outgoing attributed damage. Prior local shield hit and773 destruction/station recovery receipts preserved separately. Retain user-selected Virtual Joystick. Temporary extra Ctrl+Shift+Backspace full-stop binding/ship-trace cleanup still pending. No subagents/server/original-client changes/publication/player cloning. G7/G8 open.'
save(p,s[:a]+resume+s[b:]);print('Recorded first patrol arrival and operator takeover in four documents.')
