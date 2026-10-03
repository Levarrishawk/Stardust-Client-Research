"""Check joined supplied appearance revision receipts independently of the host assertions."""
import argparse, json
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('log',type=Path)
args=parser.parse_args()
lines=args.log.read_text(encoding='utf-8').splitlines()
rows=[json.loads(line.split('customization_state=',1)[1]) for line in lines if line.startswith('customization_state=')]
expected=[('zero',1),('latest_varied',4),('failed_retained',4),('restored_zero',6),('missing_retained',6)]
assert [(r['Name'],r['Revision']) for r in rows]==expected, rows
assert all(r['calls']==3 and r['closed']==3 and r['Height']==1.4 for r in rows),rows
assert len({r['Frame'] for r in rows})==1,rows
assert all(len(r['Fingerprint'])==64 for r in rows), rows
assert all(len(r['FaceSha256'])==64 for r in rows), rows
assert rows[0]['FaceSha256']==rows[3]['FaceSha256']==rows[4]['FaceSha256'],rows
assert rows[1]['FaceSha256']==rows[2]['FaceSha256'] and rows[1]['FaceSha256']!=rows[0]['FaceSha256'],rows
assert any('PASS: pumped customization revisions=true latest_only=true failure_retained=true pose_clock_retained=true no_asset_io=true drained=true' in line for line in lines)
# Exact pre-frame diagnostics also occur in the accepted Round19 host; retain their counts.
first_frame=next(i for i,line in enumerate(lines) if line.startswith('pump_frame='))
allowed={
 'ERROR: Failed to read the root certificate store.':'at: get_system_ca_certificates (platform/windows/os_windows.cpp:2582)',
 'ERROR: Condition "err != OK" is true.':'at: initialize (drivers/gles3/shader_gles3.cpp:802)',
}
diagnostics={}
for i,line in enumerate(lines):
 if 'ERROR:' not in line: continue
 assert i<first_frame and line in allowed and i+1<len(lines) and lines[i+1].strip()==allowed[line],(i,line)
 diagnostics[line]=diagnostics.get(line,0)+1
expected_hashes={'zero':'A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808','varied':'60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5'}
assert rows[0]['FaceSha256']==expected_hashes['zero'] and rows[1]['FaceSha256']==expected_hashes['varied'],rows
print('Observed baseline startup diagnostics: '+json.dumps(diagnostics,sort_keys=True))
drains=[json.loads(line.split('customization_drain=',1)[1]) for line in lines if line.startswith('customization_drain=')]
if drains:
 assert len(drains)==1 and drains[0]==dict(customizationEntered=1,customizationClosed=1,Applied=6,Complete=True),drains
 print('PASS: real owned pending customization job canceled, drained and not published')
print('PASS: five joined revision receipts, latest selection, failed/missing retention, fixed clock, closed archive count and host drain')
