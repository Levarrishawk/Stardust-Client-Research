import json,math,struct,sys
from pathlib import Path
F=lambda b:struct.unpack('<f',struct.pack('<I',b))[0]
B=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
U=(1<<64)-1;inv=F(0x3a83126f)
fs=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());native=json.loads(Path(sys.argv[1]).read_text());assert native['Checks']==147 and native['Cases']==14 and native['OriginalParity'] is False
out={r['Name']:r for r in native['Results']};n=0;faults=0
for f in fs:
 tick=f['Tick'];cr=f['ClockRevision'];token=f['Token'];rev=f['Revision'];entries={};closed=False;faulted=False
 for op,row in zip(f['Operations'],out[f['Name']]['Rows']):
  kind=op['Kind'];id=op.get('Id',1);outcome='ok';delivery=False
  if closed and kind!='close':outcome='closed'
  elif kind=='close':closed=True;entries={}
  elif kind=='service' and (id not in entries or entries[id]['state']['Through']==tick):pass
  elif kind=='remove' and id not in entries:pass
  elif rev==U:outcome='overflow'
  elif kind=='register':
   interval=F(op['IntervalBits']);work=op['Work']
   if op['Count']<1 or not math.isfinite(interval) or interval<=0 or not 1<=work<=1048576:outcome='argument'
   elif id in entries or len(entries)>=f['Capacity']:outcome='invalid'
   elif token==U or cr==U:outcome='overflow'
   else:
    token+=1;cr+=1;rev+=1;delivery=True
    state=dict(ObjectId=id,RegistrationToken=token,Revision=rev,Through=tick,From=tick,ElapsedBits=0,ClockRevision=cr,Index=0,ResidualBits=0,CursorRevision=op.get('CursorSeed',0),CumulativeAdvances=0,LastAdvanceSteps=0)
    entries[id]=dict(state=state,count=op['Count'],interval=interval,work=work)
  elif kind=='advance':
   seconds=F(op.get('SecondsBits',0));product=seconds*1000
   if not math.isfinite(seconds) or seconds<0:outcome='argument'
   elif product>=float(1<<64):outcome='invalid'
   else:
    ticks=max(1,math.trunc(product))
    if ticks>f['MaxTicks']:outcome='invalid'
    elif tick>U-ticks or cr==U:outcome='overflow'
    else:tick+=ticks;cr+=1;rev+=1
  elif kind=='service':
   e=entries[id];s=e['state']
   if cr==U:outcome='overflow'
   else:
    cr+=1;elapsed=F(B(float(tick-s['Through'])*inv));remaining=F(s['ResidualBits'])+elapsed;steps=0
    if s['CursorRevision']==U:outcome='overflow'
    else:
     while remaining>=e['interval']:
      if steps==e['work']:outcome='invalid';break
      remaining-=e['interval'];steps+=1
    if outcome!='ok':faulted=True;closed=True;entries={};faults+=1
    else:
     rev+=1;delivery=True;e['state']=dict(s,Revision=rev,Through=tick,From=s['Through'],ElapsedBits=B(elapsed),ClockRevision=cr,Index=(s['Index']+steps)%e['count'],ResidualBits=B(remaining),CursorRevision=s['CursorRevision']+1,CumulativeAdvances=s['CumulativeAdvances']+steps,LastAdvanceSteps=steps)
  elif kind in ('remove','reset'):
   if cr==U:outcome='overflow'
   else:
    cr+=1;rev+=1
    if kind=='reset':entries={}
    else:del entries[id]
  else:raise AssertionError(kind)
  states=[dict(entries[id]['state'],Current=not closed and entries[id]['state']['Through']==tick) for id in sorted(entries)]
  expected=dict(Outcome=outcome,Tick=tick,ClockRevision=cr,Revision=rev,Closed=closed,Faulted=faulted,Delivery=delivery,States=states)
  assert row==expected,(f['Name'],op,row,expected);n+=1
assert faults==2
same=[row for result in native['Results'] for row in result['Rows'] if row['Outcome']=='ok' and not row['Delivery']]
print(json.dumps(dict(passed=True,native_checks=147,cases=14,independent_operations=n,terminal_fault_cases=faults,modern_numeric_policy=True,original_parity=False),indent=2))
