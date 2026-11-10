from pathlib import Path
import shutil

root = Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup = Path(r'C:\Stardust-Experimental\diagnostics\g7-damage-773DD0E0\doc-backup')
backup.mkdir(exist_ok=False)

def save(path, content):
    shutil.copy2(path, backup / path.name)
    temporary = path.with_name(path.name + '.g7tmp')
    temporary.write_text(content, encoding='utf-8')
    temporary.replace(path)

receipt = '''

### October20 bounded ship-damage probe candidate

773DD0E0F6151E8904204532390C8AC51DD2A4340C39D07370EEC33783B09858 / matching PDB24529F91BA4C6EBEA1669A4C22BFDC89466E913BB07AC2BD2F26632B1E3DC2C5 adds opt-in, capped hit/destruction receipts independently of the periodic state-sample cap. ShipController records at most64 decoded-hit events (local flag/type/fractions/chassis); GroundScene records at most16 DestroyShipMessage events (local flag/severity/chassis). Existing one-second ship-state samples add chassis and front/back shield values. No object IDs, account data or raw payloads are logged. Original hit effects, destruction handling and simulation remain unchanged. These diagnostics establish observations, not gameplay acceptance by themselves.

Release/Win32 build passes34 warnings/0 errors in20.65s. The rebuilt six-input production-linked suite passes27,708 checks/11,864 malformed-input rejections plus5 isolated CRC/ACST cases, exit0. Matched EXE/PDB deployed Experimental-only with prior pair, source and receipts preserved privately. No DLL/TRE/server changes. Command tracing is off; tracePrecuShip temporarily on for this bounded probe.

Fresh process returns Kova to Moenia. Autonomous hallway navigation reaches the terminal at(4775,4,-4680), with no speed/privilege change or operator reposition. Ship details show chassis0%, front armor87%, rear armor100%, other installed components100%. Launch reaches Naboo space(-2662,952,-6693). Pointer mode stops Virtual Joystick yaw/pitch input; stationary state has zero yaw/pitch/roll. At292m station distance the normal full-repair conversation quotes100 credits and reports Repairs complete. The live state changes from chassis0/800 to800/800, front/back shields200/200. This accepts repair-state replication in this candidate; a new credit comparison was not performed. The next encounter remains pending. Temporary Virtual Joystick selection and extra full-stop binding still need restoration after the comparison. G7/G8 remain open.
'''

p = root / 'docs/conversion/SPACE_COMPATIBILITY.md'
save(p, p.read_text(encoding='utf-8') + receipt)
p = root / 'PROGRESS.md'
s = p.read_text(encoding='utf-8')
old = '| Latest deployed candidate executable SHA-256 | `F18855246EF43FB85B96741C0061839C8155D0E049FB69D2F35E7599B2C522CF` (matching PDBCBCDA5F7; Pre-CU ship-hit subtype/payload translation; failure-only scene receipts; bounded live probe tracing temporarily on; local joystick dead zone calibrated) |'
new = '| Latest deployed candidate executable SHA-256 | `773DD0E0F6151E8904204532390C8AC51DD2A4340C39D07370EEC33783B09858` (matching PDB24529F91; bounded hit/destruction diagnostics; Pre-CU ship-hit translation retained; ship tracing temporarily on, command tracing off) |'
assert old in s
s = s.replace(old, new, 1)
save(p, s + '\nOctober20 probe candidate773DD0E0 builds with zero errors and passes the27,708-check suite. Fresh-process autonomous terminal approach, launch and station repair restore chassis0/800 to800/800. Independent capped hit/destruction receipts now distinguish the next encounter; damage/destruction acceptance remains pending. [Probe detail](docs/conversion/SPACE_COMPATIBILITY.md#october20-bounded-ship-damage-probe-candidate).\n')
p = root / 'docs/conversion/G7_ACCEPTANCE_MATRIX.md'
save(p, p.read_text(encoding='utf-8') + '\nOctober20 candidate773DD0E0 adds opt-in capped hit/destruction receipts, passes build/native suite and confirms fresh-process launch plus live chassis repair0/800 to800/800. This is diagnostic readiness and repair replication, not combat/destruction closure. [Receipt](SPACE_COMPATIBILITY.md#october20-bounded-ship-damage-probe-candidate).\n')
p = root / 'HANDOFF.md'
s = p.read_text(encoding='utf-8')
start = s.index('Current resume point:')
end = s.index('\n\n', start)
s = s[:start] + 'Current resume point: October20 G7 space. Candidate773DD0E0/PDB24529F91 deployed;34 warnings/0 errors;27,708 checks/11,864 rejections plus5 pass. Fresh process autonomously reaches Moenia terminal and launches; station repair confirmed chassis0/800 to800/800. Current flight leaving Naboo Station manually at20% throttle, Virtual Joystick pointer mode. tracePrecuShip temporarily on; independent64-hit/16-destruction event caps remain available even after periodic state cap. Command trace off. Restore Automatic pilot mouse mode and remove extra Ctrl+Shift+Backspace full-stop binding after comparison; original End retained. Preserve probe receipts then restore ship tracing false. No subagents/server/original-client changes/publication/player cloning. G7/G8 open.' + s[end:]
save(p, s)
print('Updated four conversion documents; prior versions backed up.')
