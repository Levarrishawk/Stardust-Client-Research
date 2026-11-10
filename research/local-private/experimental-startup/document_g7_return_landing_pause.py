from pathlib import Path
import shutil

root = Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup = Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget\return-landing-doc-backup')
backup.mkdir(exist_ok=False)

def save(relative, content):
    path = root / relative
    shutil.copy2(path, backup / path.name)
    temporary = path.with_name(path.name + '.g7tmp')
    temporary.write_text(content, encoding='utf-8')
    temporary.replace(path)

receipt = '''

### October20 return flight, landing and operator control pause

On unchanged BFD996B2, the normal map autopilot returns from the system-edge warning to Naboo Station, disengaging at zero speed near(-2490,904,-6460). A fresh station greeting appears. Two separately observed Stop Conversing clicks close the visible panel while capacitor remains2000 in the inspected frames. Request Permission to Land, Moenia, and Land also retain2000. These bounded negative controls do not reproduce the earlier suspected firing; no input-handling patch is made, and intermittent click-through remains Unknown. Static popup/button inspection alone does not establish a runtime leak.

Normal landing accepts scene2 at t312664.593, terrain/naboo.trn, position(4730.000,4.200,-4678.000), and the ground HUD renders without a new fatal. The complete BFD ship-log set, including the separate player-local shield-hit receipt, is preserved privately before another process can overwrite it. This adds a return-flight/landing receipt; outgoing player-attributed damage and space mission/XP are still unaccepted.

Normal /target V3-FX selects the trainer at122m with visible target range. /follow reaches the hangar wall at(4740,4,-4694),103m remaining; /stopFollow visibly cancels. Ordinary routing then begins. Computer use reports fresh external input, followed by a changed view/position near(4741,4,-4684). The operator explicitly requests that the agent wait while they handle Kova. All further computer input stops. That external movement is not counted as autonomous navigation or a new client acceptance result.

The client remains running under operator control. Automatic pilot mouse mode and saved0.205 joystick dead zone are retained. Temporary extra Ctrl+Shift+Backspace full-stop binding remains alongside End; ground Options exposes the ground keymap, so no unrelated binding is cleared. Ship tracing remains temporarily enabled in the running process, command tracing off. After controls are released, preserve any new receipts, finish the probe cleanup through the space keymap and a normal process exit, restore tracePrecuShip=false, then continue the trainer mission and outgoing-damage/XP checks. G7/G8 remain open. No new build or native-suite execution, server/original-client operation, player cloning, staging or publication occurs in this follow-up.
'''
name = 'docs/conversion/SPACE_COMPATIBILITY.md'
save(name, (root/name).read_text(encoding='utf-8') + receipt)
for name, link in (
    ('PROGRESS.md', 'docs/conversion/SPACE_COMPATIBILITY.md'),
    ('docs/conversion/G7_ACCEPTANCE_MATRIX.md', 'SPACE_COMPATIBILITY.md'),
):
    save(name, (root/name).read_text(encoding='utf-8') + '\nOctober20 BFD996B2 return flight and normal Moenia landing pass; two isolated Stop Conversing controls do not reproduce suspected firing. Operator now handles Kova; computer input is stopped. Temporary space full-stop binding/ship-trace cleanup remains pending. Outgoing damage/space mission/XP and G7/G8 stay open. [Receipt](' + link + '#october20-return-flight-landing-and-operator-control-pause).\n')
name = 'HANDOFF.md'
content = (root/name).read_text(encoding='utf-8')
start = content.index('Current resume point:')
end = content.index('\n\n', start)
resume = 'Current resume point: October20 G7 space, operator control pause. Unchanged BFD996B2/PDB063C9F38 running. Player-local shield hit and prior773 local destruction/station recovery are recorded separately; outgoing attributed damage/space mission/XP remain open. Return autopilot to Naboo Station and normal Moenia landing pass (scene2 t312664.593,4730,4.2,-4678). Two isolated Stop Conversing controls retain capacitor2000; earlier suspected firing remains unconfirmed, no patch. Trainer selected122m; blocked Follow explicitly stopped103m away. Fresh external input changes view/position near4741,-4684; operator explicitly says wait while they handle Kova. STOP computer input until controls are released. BFD ship logs preserved privately. Automatic pilot mode restored/deadzone0.205 retained. Extra Ctrl+Shift+Backspace full-stop binding and temporary ship trace still require cleanup; command trace off. Then trainer mission/outgoing damage/XP. No subagents/server/original-client changes/publication/player cloning. G7/G8 open.'
save(name, content[:start] + resume + content[end:])
print('Recorded return landing and explicit operator-control pause in four source documents.')
