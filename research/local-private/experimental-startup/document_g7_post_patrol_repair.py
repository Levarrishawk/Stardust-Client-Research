from pathlib import Path
import shutil

root = Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup = Path(r'C:\Stardust-Experimental\diagnostics\g7-post-patrol-repair-docs')
backup.mkdir(exist_ok=False)
heading = '### October20 post-patrol repair, scene chat and persistence'
receipt = '''

### October20 post-patrol repair, scene chat and persistence

Operator returns controls after terminal positioning. On unchanged54CD3361/PDBFA2AE3F5, ordinary terminal double-click/Select shows chassis80%, Main Reactor0%, Front Armor93%, Rear Armor68%, and Engine/Shield/Capacitor/Weapon100%; Ordnance0% is the previously absent launcher. Launch succeeds. Space Options -> Keymap shows Enable modal chat selected, while Chase Camera is unselected. Enter produces the chat caret, /comm enters normally, submission clears the field and opens the selected Naboo Space Station conversation at678m. No scene preference or whole preset is reset.

Request Repairs -> All of it quotes100 credits. Before Yes inventory shows cash1100/bank1246; afterward Repairs complete appears and inventory independently shows cash1000/bank1246. Normal station conversation reopens and Request Permission to Land -> Moenia Starport -> Land returns Kova to Naboo(4730,4,-4678). This is ordinary landing, distinct from the operator's earlier teleport recovery.

Datapad -> Z-95 control device -> View Components opens the normal read-only management panel away from a terminal; its existing modification restriction remains visible. Chassis is800/800. Reactor armor55.5/55.5 and HP110/110; front armor109.8/109.8 and HP159.8/159.8; rear armor134.8/134.8 and HP159.8/159.8. These values accept the repaired postconditions, not restoration of pre-decay maxima. Ground Options -> Keymap still shows modal chat OFF and Chase OFF after returning from space ON, accepting scene independence for this transition.

Normal /quit ends the process. A fresh Computer Use launch starts with blank account input, then authorized login/Kova selection restores ground(4730,4,-4678). Inventory retains cash1000/bank1246; the mission reward bank increase and repair cash deduction therefore survive a full process restart. Fresh View Components independently retains chassis800/800 and reactor55.5/55.5 armor,110/110 HP; armor component restart persistence was not separately re-inspected. Kova is left stationary with inspection windows closed. No new fatal report appears; latest remains October20 03:06:57 local. Active executable hash remains54CD33613251A12C4C4E4C7E61B11D1E3C0D5CED6954B084271336D4B72C5DB3; ship/command tracing remains off.

One Request Repairs click made before enabling pointer mode produces Your weapon is disabled and does not select the response. After Alt enables pointer mode, the same response works. This bounds the observation to captured flight input versus pointer interaction; it does not establish click-through in pointer mode or original-client parity, and no speculative input patch is made. Temporary extra Ctrl+Shift+Backspace full-stop binding remains alongside End; Clear Binding removes all bindings for an action, so it is not used blindly. Preserve the operator-selected Virtual Joystick.

Confidence: Confirmed for observed UI, credit/state postconditions, normal landing, scene-specific saved values and bounded reconnect persistence. No build/native-suite rerun is needed for this runtime/documentation-only slice; prior27,948/11,864 plus5 suite remains the source candidate receipt. Matching unsupported destruction-effect recurrence, exact pilot XP award delta, broader space/ammunition/loadout/mission journeys and G7/G8 remain open. No server/original-client operation, player death/cloning, staging or publication.
'''
paths = ['PROGRESS.md', 'HANDOFF.md', 'docs/conversion/SPACE_COMPATIBILITY.md',
         'docs/conversion/G7_ACCEPTANCE_MATRIX.md', 'docs/conversion/CHAT_INPUT_COMPATIBILITY.md']
for rel in paths:
    path = root / rel
    shutil.copy2(path, backup / path.name)
    text = path.read_text(encoding='utf-8')
    if heading in text:
        raise RuntimeError('Receipt already exists: ' + rel)
    if rel == 'HANDOFF.md':
        start = text.index('Current resume point:')
        end = text.index('\n\n', start)
        text = text[:start] + ('Current resume point: October20 G7 space.54CD3361/PDBFA2AE3F5 remains deployed/running; fresh process window1183586. Kova stationary on Moenia ground(4730,4,-4678), inspection windows closed. Operator patrol/combat + ordinary V3-FX turn-in pass. Full100-credit station repair, normal Moenia landing, repaired chassis800/800/reactor110/110 HP and fresh-process persistence now pass; balances1000cash/1246bank retain reward and repair. Space modal ON/Enter submission and subsequent ground modal OFF/Chase OFF pass without preference changes. No new fatal; traces off. Next bounded space work: clean up only the temporary extra Ctrl+Shift+Backspace full-stop binding while retaining End, preserve Virtual Joystick; qualify component-effect correction on a matching natural recurrence and extend required ammunition/loadout/mission coverage. Original parity of captured-mode conversation interaction and exact pilot XP delta remain unaccepted. G7/G8 open; no subagents/server/original-client changes/publication/player cloning.') + text[end:]
    elif rel.endswith('CHAT_INPUT_COMPATIBILITY.md'):
        text += '\n\nOctober20 live scene-independence follow-up: unchanged54CD3361 space Keymap shows modal chat ON. Enter activates the chat caret; /comm types/submits normally, clears the field and opens station conversation. Subsequent normal Moenia landing shows ground modal chat OFF and Chase OFF. No checkbox/preset changes occur. These are saved scene-value/input observations; no fresh-profile or live preset-reset default claim follows. [Repair/transition receipt](SPACE_COMPATIBILITY.md#october20-post-patrol-repair-scene-chat-and-persistence).\n'
    elif rel.endswith('G7_ACCEPTANCE_MATRIX.md'):
        lines = text.splitlines()
        index = next(i for i, line in enumerate(lines) if line.startswith('| G7-X1 |'))
        lines[index] = '| G7-X1 | Space/custom deployment scope | Required ground and space scope. Pilot training, weapon/loadout display, launch/throttle/stop, hyperspace, station conversations and normal landing/reconnect; live incoming hit/local ship-destruction recovery; operator-confirmed patrol/combat, completed journal and ordinary turn-in; paid station repair/state and full-process credit/chassis/reactor persistence. [Space evidence](SPACE_COMPATIBILITY.md). | Broader required flight/input, ammunition/loadout and mission journeys; exact pilot XP award delta, effect visual parity and matching unsupported component-effect recurrence. Temporary extra full-stop cleanup remains. Wider custom-system inventory needs deployment correlation. | Active space slice; gate open |'
        text = '\n'.join(lines) + '\n'
        text += '\nOctober20 post-patrol repair: full100-credit station service deducts cash1100->1000 with bank1246 unchanged; normal Moenia landing, repaired chassis/reactor/armor and independent ground-OFF/space-ON modal chat pass. Full-process reconnect retains balances, ground position, chassis800/800 and reactor110/110 HP. [Receipt](SPACE_COMPATIBILITY.md#october20-post-patrol-repair-scene-chat-and-persistence). Matching effect recurrence and broader G7/G8 remain open.\n'
    elif rel == 'PROGRESS.md':
        text += '\n\n' + heading + '\n\nFull100-credit station repair, normal Moenia landing, repaired chassis/reactor/armor and independent ground-OFF/space-ON modal chat pass on unchanged54CD3361. Full-process reconnect retains cash1000/bank1246, ground position, chassis800/800 and reactor110/110 HP. Mission reward and repair balances persist. Kova remains stationary, windows closed and traces off. [Detailed outcomes and remaining limits](docs/conversion/SPACE_COMPATIBILITY.md#october20-post-patrol-repair-scene-chat-and-persistence). Temporary extra full-stop cleanup, matching unsupported-effect recurrence and broader G7/G8 remain open; no code/build/server/publication change.\n'
    else:
        text += receipt
    temp = path.with_suffix(path.suffix + '.g7.tmp')
    temp.write_text(text, encoding='utf-8', newline='\n')
    temp.replace(path)
print('Updated five conversion documents; private backups:', backup)
