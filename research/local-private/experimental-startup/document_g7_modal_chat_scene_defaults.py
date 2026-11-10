from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-modal-chat-scene-defaults\doc-backup');backup.mkdir(exist_ok=False)
def save(name,s):
    p=root/name;shutil.copy2(p,backup/p.name)
    t=p.with_name(p.name+'.g7tmp');t.write_text(s,encoding='utf-8');t.replace(p)
receipt='''

## October20 scene-specific modal-chat defaults

Operator requests modal chat off by default on ground and on by default in space. CuiPreferences now seeds false for ST_ground and true for ST_space under the existing usePrecuCamera profile before registering both independent saved user options. Saved choices still take precedence. InputScheme::resetFromType applies the same scene policy when a player explicitly resets/selects a standard preset; ordinary NGE flag behavior is unchanged. Merely switching scenes does not run a preference reset. Enter/NumpadEnter activation, text submission and focus logic are unchanged.

Release/Win32 build passes37 warnings/0 errors,21.75s. EXE2DCBF8F0B61C429D75B8351CE283D5EBB3C2415E0AED5C9A3A56E9C3B49A57A6 and matching PDB5DF527989368B80B25330290F8B8BC2E9B60EE14D63EF9177E3AB6CDBAFEF4DE are copied and hash-verified as a separate private Experimental candidate. BFD996B2 is still running under operator control during the patrol; the active executable/symbol pair is not replaced. Deploy the candidate after normal exit. This small defaults-only change uses successful compilation and focused source/diff inspection; no new native-suite or fresh-profile/live preset-reset acceptance is claimed. Do not reset the operator's complete keymap merely to verify this default.

Existing saved space-off preferences are intentionally retained; enabling modal chat through space Options sets only that scene's saved value. Ground and space remain independently configurable. No DLL/TRE/network/gameplay change, server operation, staging or publication. The user's space Virtual Joystick selection and current flight are preserved. G7/G8 remain open.
'''
p='docs/conversion/CHAT_INPUT_COMPATIBILITY.md';save(p,(root/p).read_text(encoding='utf-8')+receipt)
p='PROGRESS.md';save(p,(root/p).read_text(encoding='utf-8')+'\nOctober20 requested modal-chat defaults implemented: ground off/space on for new Pre-CU profiles and explicit preset resets, saved choices preserved. Candidate2DCBF8F0/PDB5DF52798 builds37 warnings/0 errors and is archived separately in Experimental; BFD996B2 remains active while the operator completes the patrol. Deployment after normal exit and live persistence verification remain pending. [Chat receipt](docs/conversion/CHAT_INPUT_COMPATIBILITY.md#october20-scene-specific-modal-chat-defaults).\n')
p='docs/conversion/SPACE_COMPATIBILITY.md';save(p,(root/p).read_text(encoding='utf-8')+'\nOctober20 modal-chat candidate2DCBF8F0/PDB5DF52798 builds successfully and is preserved separately in Experimental for the next normal process restart. It implements user-requested ground-off/space-on defaults while preserving saved choices. BFD996B2 continues the operator-controlled patrol; no flight result is inferred from the rebuild. [Chat receipt](CHAT_INPUT_COMPATIBILITY.md#october20-scene-specific-modal-chat-defaults).\n')
p='HANDOFF.md';s=(root/p).read_text(encoding='utf-8');a=s.index('Current resume point:');b=s.index('\n\n',a)
resume='Current resume point: October20 G7 pilot patrol, OPERATOR CONTROLS. BFD996B2/PDB063C9F38 still running; first natural patrol arrival/replacement waypoint guidance recorded. Monitor restored via Interface preference. Operator finishes patrol/landing; STOP computer input until released. New requested modal defaults (ground off/space on, saved choices preserved, explicit preset reset consistent) built as2DCBF8F0/PDB5DF52798,37 warnings/0 errors21.75s; candidate EXE/PDB separately in Experimental diagnostics/g7-modal-chat-scene-defaults. Active pair not replaced. Deploy after normal exit; verify saved scene independence without whole-keymap reset. Prior local hit/destruction receipts preserved; full mission/reward/XP/outgoing damage not yet accepted. Retain user Virtual Joystick. Extra full-stop binding and temporary ship-trace cleanup pending. No subagents/server/original-client changes/publication/player cloning; G7/G8 open.'
save(p,s[:a]+resume+s[b:]);print('Recorded built scene-default candidate and pending safe deployment.')
