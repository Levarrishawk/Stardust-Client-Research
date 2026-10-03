"""Compact audit of controlled native lifecycle receipts; this does not independently execute C# scheduling."""
import json,sys
from pathlib import Path
folder=Path(sys.argv[1])
expected_cases={'retire','dispose','tick-false','tick-throw','apply-throw','destroy-batch','reset','close-boundary','rollback-false','rollback-wrong','rollback-throw','rollback-stale','rollback-generation','rollback-dispose-throw','rollback-getter-throw','rollback-getter-null','rollback-getter-reentry','rollback-dispose-and-getter-throw','replacement-close','capacity-reservation','bounded-churn128','bounded-long-worker-churn128','shared-task','fault-failclosed','cancel-failclosed','partial-batch','thread-reentry','pump-normal','pump-dispose-throw','pump-getter-throw','pump-worker-fault','pump-factory-reentry','pump-tick-throw','pump-tick-false','pump-getter-reentry','pump-capacity-failclosed','pump-combined-error-wait'}
r=json.loads((folder/'drain-receipt.json').read_text())
assert r['Result']=='PASS' and r['Checks']==1165 and len(r['Cases'])==len(expected_cases)==37 and set(r['Cases'])==expected_cases
assert r['OriginalSources']==66 and r['ArchiveScopes']==0 and r['NoSleeps']
regressions={}
for name,count,marker in [('consumer',160,'R15_PRESENTATION_PASS'),('pump',171,'R16_PUMP_PASS')]:
 row=json.loads((folder/f'{name}-receipt.json').read_text());assert row['Result']=='PASS' and row['Checks']==count and row['OriginalSources']==66 and row['ArchiveScopes']==0
 assert marker in (folder/f'{name}-native.txt').read_text(encoding='utf-8-sig');regressions[name]=count
assert 'R20_DRAIN_PASS checks=1165 cases=37' in (folder/'drain-native.txt').read_text(encoding='utf-8-sig')
print(json.dumps(dict(passed=True,controlled_native_checks=1165,lifecycle_cases=37,prior_native_checks=regressions,no_sleeps=True,independent_scheduler_execution=False),indent=2))
