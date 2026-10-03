"""Independent Python integer/bit scheduler reference; modern arithmetic only."""
import json,math,struct,sys
from pathlib import Path
F=lambda x:struct.unpack('<f',struct.pack('<I',x))[0]
B=lambda x:struct.unpack('<I',struct.pack('<f',x))[0]
U=(1<<64)-1;inverse=F(0x3a83126f)
fixtures=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());native=json.loads(Path(sys.argv[1]).read_text())
assert native['Checks']==390 and native['Cases']==len(fixtures)==36 and native['OriginalPC24Parity'] is False
out={r['Name']:r for r in native['Results']};assert len(out)==len(fixtures)
def global_receipt(c,tick,rev,entries):return dict(Tick=tick,Revision=rev,RegisteredCount=len(entries),Capacity=c['Capacity'],MaximumTicksPerAdvance=c['MaximumTicks'],NumericPolicy='binary64-multiply-truncate-min1',InputUnits='seconds',OriginalPC24Parity=False)
def object_receipt(v,tick,closed):return v|dict(Current=not closed and v['ThroughTick']==tick)
def obj(id,token,start,end,elapsed,rev):return dict(ObjectId=id,RegistrationToken=token,FromTick=start,ThroughTick=end,LastServicedTick=end,ElapsedBits=B(elapsed),Revision=rev,InverseScaleBits=0x3a83126f,NumericPolicy='binary64-difference-times-float32-inverse-float32-storage',OriginalPC24Parity=False)
operations=0;failures={};same_tick=0;conversion_edges=[]
for c in fixtures:
 result=out[c['Name']];invalid=not 1<=c['Capacity']<=4096 or c['MaximumTicks']==0
 assert result['Constructor']==('argument' if invalid else 'ok')
 if invalid:assert not result['Rows'];continue
 tick=c['TickSeed'];rev=c['RevisionSeed'];token=c['TokenSeed'];entries={};closed=False
 assert result['Initial']==global_receipt(c,tick,rev,entries) and len(result['Rows'])==len(c['Operations'])
 for op,row in zip(c['Operations'],result['Rows']):
  kind=op['Kind'];id=op.get('Id',0);outcome='ok';changed=False;delivery=False;removed=None;receipt=None
  if closed and kind!='close':outcome='closed'
  elif kind=='advance':
   seconds=F(op.get('SecondsBits',0));product=seconds*1000.0
   if not math.isfinite(seconds) or seconds<0:outcome='argument'
   elif not math.isfinite(product) or product>=float(1<<64):outcome='invalid'
   else:
    # Python integer truncation has no fixed-width cast; independently range-check before modeling checked sum.
    ticks=max(1,math.trunc(product))
    if ticks>c['MaximumTicks']:outcome='invalid'
    elif tick>U-ticks or rev==U:outcome='overflow'
    else:tick+=ticks;rev+=1;changed=True
  elif kind=='register':
   if id in entries or len(entries)==c['Capacity']:outcome='invalid'
   elif token==U or rev==U:outcome='overflow'
   else:token+=1;rev+=1;receipt=obj(id,token,tick,tick,0.0,rev);entries[id]=receipt;changed=True;delivery=True
  elif kind=='service':
   old=entries.get(id)
   if old is not None:
    if old['ThroughTick']==tick:receipt=old;same_tick+=1
    elif rev==U:outcome='overflow'
    else:
     rev+=1;elapsed=float(tick-old['ThroughTick'])*inverse;receipt=obj(id,old['RegistrationToken'],old['ThroughTick'],tick,elapsed,rev);entries[id]=receipt;changed=True;delivery=True
  elif kind=='remove':
   if id not in entries:removed=False
   elif rev==U:outcome='overflow'
   else:del entries[id];rev+=1;removed=True;changed=True
  elif kind=='reset':
   if rev==U:outcome='overflow'
   else:entries={};rev+=1;changed=True
  elif kind=='close':
   if not closed:closed=True;entries={};changed=True
  else:raise AssertionError(kind)
  expected=dict(Kind=kind,Id=id,SecondsBits=op.get('SecondsBits',0),Outcome=outcome,Global=global_receipt(c,tick,rev,entries),GlobalChanged=changed,DeliveryNew=delivery,Removed=removed,Receipt=None if receipt is None else object_receipt(receipt,tick,closed),Objects=[object_receipt(entries[id],tick,closed) for id in sorted(entries)],Closed=closed)
  assert row==expected,(c['Name'],op,row,expected)
  operations+=1
  if outcome!='ok':failures[outcome]=failures.get(outcome,0)+1
 assert result['Final']==global_receipt(c,tick,rev,entries)
 if c['Name'].startswith('u64-cast'):conversion_edges.append(dict(name=c['Name'],tick=tick,outcomes=[r['Outcome'] for r in result['Rows']]))
two=out['partition-two']['Final']['Tick'];single=out['partition-single']['Final']['Tick'];assert (two,single)==(2,3)
divergent=out['divergent-objects']['Rows'];assert divergent[6]['Objects'][1]['ThroughTick']==0 and divergent[6]['Objects'][1]['Current'] is False
assert divergent[7]['Receipt']['ThroughTick']==6 and divergent[8]['DeliveryNew'] is False and divergent[8]['Receipt']==divergent[7]['Receipt']
assert out['registration-at-now']['Rows'][1]['Receipt']['ThroughTick']==100 and out['registration-at-now']['Rows'][2]['DeliveryNew'] is False
print(json.dumps(dict(passed=True,native_checks=native['Checks'],cases=len(fixtures),independent_operations=operations,failures=failures,same_tick_identity_cases=same_tick,partition_ticks=[two,single],u64_conversion_edges=conversion_edges,inverse_scale_bits='3A83126F',original_pc24_parity=False),indent=2))
