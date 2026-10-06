"""Verify all128 actual rays before projecting equivalent repetitions to the prior32-click oracle."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('maxclicks79_prior77',ROOT/'poc/round77-tests/Integration/configured_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
p,t,fixture,log,out=sys.argv[1:];p,t,fixture=[json.loads(Path(x).read_text()) for x in (p,t,fixture)];text=Path(log).read_text(encoding='utf-8-sig')
assert p['PlaygroundSettings']['MaximumClickAttempts']==128 and p['InteractiveReplay'] and p['SyntheticFrameClock']
records=m.old.records;inputs=records(text,'point_input_target=');picks=records(text,'point_camera_pick=');assert len(inputs)==len(picks)==128
assert [r['Attempt'] for r in inputs]==[r['TargetRevision'] for r in inputs]==list(range(1,129)) and all(r['AtStep']==300 and r['ActualPickTarget'] for r in inputs)
g=m.old.phase.prior.ray.surface.fixture_geometry(t);m.old.phase.prior.ray.compare(g,[r['Native'] for r in picks])
for r in inputs:m.old.near(r['Target'],[2,2],2e-5,'independent128 repeated target')
declined,=records(text,'offline_click_budget_declined=');assert declined==dict(AtStep=300,Attempts=128,Remaining=0,IsExhausted=True,ClockCallbackSkipped=True,PickerUnchanged=True,HostFaulted=False,DriverIntentPreserved=True,LastResultPreserved=True,ActualViewportEvent=True)
ready,=records(text,'offline_interactive_ready=');assert ready['MaximumClickAttempts']==128
for row in records(text,'offline_interactive_destination='):assert row['ClickAttempts']==(0 if row['Index']<300 else 128)
limits=records(text,'offline_configured_limits=');assert len(limits)==401 and all('Terrain clicks remaining: '+str(128 if r['Index']<300 else 0)+' |' in r['Caption'] for r in limits)
lines=[];counts={}
for line in text.splitlines():
 key,sep,raw=line.partition('=')
 if key in ('point_camera_pick','point_input_target'):
  counts[key]=counts.get(key,0)+1
  if counts[key]>32:continue
 if key in ('offline_click_budget_declined','offline_interactive_ready','offline_interactive_destination','offline_configured_limits'):
  row=json.loads(raw)
  if key=='offline_click_budget_declined':row['Attempts']=32
  elif key=='offline_interactive_ready':row['MaximumClickAttempts']=32
  elif key=='offline_interactive_destination' and row['ClickAttempts']==128:row['ClickAttempts']=32
  elif key=='offline_configured_limits':row['Caption']=row['Caption'].replace('Terrain clicks remaining: 128 |','Terrain clicks remaining: 32 |')
  line=key+'='+json.dumps(row,separators=(',',':'))
 lines.append(line)
projected=dict(p,PlaygroundSettings=dict(p['PlaygroundSettings'],MaximumClickAttempts=32));value=m.verify(projected,t,fixture,'\n'.join(lines))
completion,=records(text,'offline_completion_summary=');assert completion==dict(Reason='FinishRequested',MaximumUpdates=6000,CompletedUpdates=401,AttemptedUpdates=401,Stopped=True,Faulted=False,Mode='Idle',Speed=0,TargetActive=False)
roots,=records(text,'offline_root_completion_summary=');assert roots['Samples']==401 and roots['Drained']==39 and roots['CleanDrain'] and roots['QueueCount']==roots['PendingMessages']==roots['RetainedPackets']==0
value.update(IssuedPointTargets=128,DeclinedViewportClick=129,All128RaysIndependentlyVerified=True,ProjectionEquivalentRepeatedTargetsOnly=True,PrimitiveCompletionReason='FinishRequested',PrimitiveRootCleanDrain=True)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
