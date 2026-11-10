from pathlib import Path
from datetime import datetime
import shutil

root = Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup = Path(r'C:\Stardust-Experimental\diagnostics\g7-ground-modal-terminal-pause')
backup.mkdir(exist_ok=False)
section = '''

### October20 ground preference verification and terminal control pause

On the unchanged deployed54CD3361/PDBFA2AE3F5 pair, Computer Use opens Options -> Keymap on ground and observes Enable modal chat unchecked and Chase Camera Mode unchecked. Neither preference is changed and Options closes normally. This confirms the saved ground-off state after this fresh-process login; fresh-profile defaults and the space saved choice remain separate checks. No whole-preset reset is performed.

Agent walking reaches the Moenia starport approach. Computer Use then detects operator input; the refreshed view places Kova beside the starship terminal at Naboo(4777,4,-4680). Treat that final positioning as operator setup, not autonomous doorway-navigation acceptance. Agent game input stops and a control-handoff question is pending. Ship repairs, the space modal-on check, temporary extra full-stop binding cleanup and reward restart persistence remain pending. No new build/tests, server/original-client operation, player cloning or publication. G7/G8 remain open.
'''
paths = ['PROGRESS.md', 'HANDOFF.md', 'docs/conversion/SPACE_COMPATIBILITY.md',
         'docs/conversion/G7_ACCEPTANCE_MATRIX.md', 'docs/conversion/CHAT_INPUT_COMPATIBILITY.md']
for rel in paths:
    path = root / rel
    shutil.copy2(path, backup / path.name)
    text = path.read_text(encoding='utf-8')
    if '### October20 ground preference verification and terminal control pause' in text:
        raise RuntimeError('Receipt already exists: ' + rel)
    if rel == 'HANDOFF.md':
        start = text.index('Current resume point:')
        end = text.index('\n\n', start)
        text = text[:start] + ('Current resume point: October20 G7 space.54CD3361/PDBFA2AE3F5 remains deployed/running. Patrol/combat and normal V3-FX turn-in pass; bank1146->1246/cash1100, pilot XP338 displayed. Ground Options->Keymap confirms modal chat OFF and Chase OFF. Computer Use detects operator input and refreshed view shows Kova at Moenia starship terminal(4777,4,-4680); agent game input is stopped pending control handoff. Final terminal positioning is operator setup. Next after controls return: inspect damaged ship, launch and repair through the normal station workflow; verify saved space modal ON independently, remove temporary extra full-stop binding without losing End, then reward/repair restart persistence. Ship/command traces off. Unsupported component-effect correction has build/native coverage but no matching live recurrence yet. G7/G8 open; no subagents/server/original-client edits/publication/player cloning.') + text[end:]
    elif rel.endswith('CHAT_INPUT_COMPATIBILITY.md'):
        text += '\n\nOctober20 live ground follow-up: unchanged54CD3361 Options -> Keymap confirms Enable modal chat unchecked after fresh-process login, alongside Chase Camera Mode unchecked. No preference or preset reset occurs. Saved space-on verification remains pending; a ground observation does not establish the space value.\n'
    elif rel.endswith('G7_ACCEPTANCE_MATRIX.md'):
        text += '\n\nOctober20 ground preference/control pause: unchanged54CD3361 confirms saved ground modal chat OFF and Chase OFF. Operator input repositions Kova beside the Moenia starship terminal(4777,4,-4680); agent input is stopped pending handoff. Repairs, space modal-on, temporary full-stop cleanup and persistence remain pending. [Receipt](SPACE_COMPATIBILITY.md#october20-ground-preference-verification-and-terminal-control-pause). G7/G8 remain open.\n'
    else:
        text += section
    temp = path.with_suffix(path.suffix + '.g7.tmp')
    temp.write_text(text, encoding='utf-8', newline='\n')
    temp.replace(path)
print('Updated five conversion documents; private backups:', backup)
