"""Check32 issued rays/exact exhaustion, then reuse the independently established401-row oracle."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('configured77_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
def verify(p,t,fixture,text):
    assert p['Version']==13 and p['PlaygroundSettings']['MaximumClickAttempts']==32 and 'SoeRequestHex' not in p and 'SoeResponseHex' not in p
    inputs=old.records(text,'point_input_target=');picks=old.records(text,'point_camera_pick=');assert len(inputs)==len(picks)==32
    assert [r['Attempt'] for r in inputs]==[r['TargetRevision'] for r in inputs]==list(range(1,33)) and all(r['AtStep']==300 and r['ActualPickTarget'] for r in inputs)
    g=old.phase.prior.ray.surface.fixture_geometry(t);old.phase.prior.ray.compare(g,[r['Native'] for r in picks])
    for r in inputs:old.near(r['Target'],[2,2],2e-5,'independent repeated target')
    declined,=old.records(text,'offline_click_budget_declined=');assert declined==dict(AtStep=300,Attempts=32,Remaining=0,IsExhausted=True,ClockCallbackSkipped=True,PickerUnchanged=True,HostFaulted=False,DriverIntentPreserved=True,LastResultPreserved=True,ActualViewportEvent=True)
    transport,=old.records(text,'soe_root_transport_ready=');assert transport['RequestHex']==('000100000002'+p['SoeConnectionId'].to_bytes(4,'big').hex()+ '000001f0').upper()
    assert transport['ResponseHex']==('0002'+p['SoeConnectionId'].to_bytes(4,'big').hex()+p['SoeSeed'].to_bytes(4,'big').hex()+'020104000001f0').upper() and transport['Established'] and transport['Synthetic'] and not transport['SocketCreated']
    ready,=old.records(text,'offline_interactive_ready=');assert ready['MaximumClickAttempts']==32 and ready['MaximumUpdates']==6000
    destination=old.records(text,'offline_interactive_destination=');assert all(row['ClickAttempts']==(0 if row['Index']<300 else 32) for row in destination)
    limits=old.records(text,'offline_configured_limits=');assert len(limits)==401
    assert all('Terrain clicks remaining: '+str(32 if row['Index']<300 else 0)+' |' in row['Caption'] for row in limits)
    # The32 rays share one exact camera/time/target and precede motion300. Check them all above,
    # then project only their equivalent first pair for the prior single-event oracle.
    lines=[];counts={}
    for line in text.splitlines():
        key=line.split('=',1)[0]
        if key in ('point_camera_pick','point_input_target'):
            counts[key]=counts.get(key,0)+1
            if counts[key]>1:continue
        lines.append(line)
    result=old.verify(dict(p,Version=12),t,fixture,'\n'.join(lines));result.update(ConfiguredSettings=True,IssuedPointTargets=32,ExhaustedClickDeclined=True,NoClockCallbackAfterClickCap=True,OriginalSenderParity=False)
    return result
if __name__=='__main__':
    p,t,fixture,log,out=sys.argv[1:];p,t,fixture=[json.loads(Path(x).read_text()) for x in (p,t,fixture)];value=verify(p,t,fixture,Path(log).read_text(encoding='utf-8-sig'));out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
