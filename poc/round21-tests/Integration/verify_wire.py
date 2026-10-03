"""Independent joined field4-to-appearance receipts; packet production imports no native codec."""
import json,sys
from pathlib import Path
log=Path(sys.argv[1]);lines=log.read_text(encoding='utf-8-sig').splitlines()
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('wire_customization_state=')]
expected=['zero','ordered_valid_then_invalid','duplicate_retains_prior_valid','empty_forms_retain','extended_zero_restore']
assert [r['Name'] for r in rows]==expected,rows
zero='A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808';varied='60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5'
assert [r['FaceSha256'] for r in rows]==[zero,varied,varied,varied,zero],rows
assert all(r['Pending']==0 and r['calls']==r['closed']==3 and r['Height']==1.4 for r in rows),rows
assert len({r['Frame'] for r in rows})==1,rows
assert all(r['Applied']==r['Raw'] for i,r in enumerate(rows) if i!=3),rows
assert rows[3]['Applied']==rows[2]['Applied'] and rows[3]['Raw']>rows[3]['Applied'],rows
assert rows[-1]['Extended'] and all(not r['Extended'] for r in rows[:-1]),rows
assert any('PASS: pumped wire_customization=true outer_owned=true ordered=true range_rejection=true empty_retention=true extended_preserved=true pose_clock_retained=true no_asset_reload=true drained=true' in line for line in lines)
first=next(i for i,line in enumerate(lines) if line.startswith('pump_frame='))
known={'ERROR: Failed to read the root certificate store.':'at: get_system_ca_certificates (platform/windows/os_windows.cpp:2582)','ERROR: Condition "err != OK" is true.':'at: initialize (drivers/gles3/shader_gles3.cpp:802)'}
diagnostics={}
for i,line in enumerate(lines):
 if 'ERROR:' not in line:continue
 assert i<first and line in known and i+1<len(lines) and lines[i+1].strip()==known[line],(i,line)
 diagnostics[line]=diagnostics.get(line,0)+1
print(json.dumps(dict(passed=True,stages=expected,prior_valid_survives_invalid=True,empty_bypasses=True,extended_preserved=True,retained_clock=True,baseline_startup_diagnostics=diagnostics)))
