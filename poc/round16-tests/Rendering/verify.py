"""Independent standalone host receipt oracle; no concrete Round16 pump required."""
import json, sys
from pathlib import Path
text=Path(sys.argv[1]).read_text(encoding='utf-8-sig',errors='replace')
rows=[json.loads(line[len('PASS: host proof '):]) for line in text.splitlines() if line.startswith('PASS: host proof ')]
assert len(rows)==1
r=rows[0]
assert r['checks']==len(r['observations'])==102
assert r['pumps']==r['closes']==r['disposals']==18
assert r['steps']==7 and r['concretePump'] is False and r['originalReads']==0
required={'three real process frames close once','rejected pump remains caller owned',
          'second candidate untouched','worker configure rejects','worker close rejects',
          'zero delta accepted','invalid delta closes before step','ingress exception closes and disposes',
          'step fault emits no receipt','observer fault suppresses later callbacks',
          'frame reentrancy closes','callback mutation rejected',
          'close then callback fault still fails completion','dispose waits for drain',
          'delayed drain completes before dispose','exit synchronously closes and hands off drain',
          'detached host awaits drain on owner thread','close drain disposal faults retained',
          'all pump owners released once'}
required|={'null pump rejected','missing drain task rejects configuration',
           'invalid configuration retains ownership','configure callback reentrancy rejected',
           'ingress close suppresses later callbacks and step','observer close suppresses later callbacks'}
assert required<=set(r['observations']),required-set(r['observations'])
assert 'Host proof failed:' not in text and 'ERROR: Condition' not in text
print(json.dumps({'pass':True,'checks':r['checks'],'pumps':r['pumps'],'steps':r['steps'],
                  'close_dispose_once':True,'callbacks_close_drain':True,'headless_process':True}))
