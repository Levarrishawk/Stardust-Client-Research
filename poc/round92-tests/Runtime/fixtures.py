"""Strict controlled inputs and deterministic caller-clock budget scenarios."""
import argparse,copy,json,runpy,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    prepared=out.with_name(out.stem+'-prepared.json')
    subprocess.run([sys.executable,str(ROOT/'poc/round91-tests/Runtime/fixtures.py'),'--out',str(prepared)],check=True,capture_output=True)
    f=json.loads(prepared.read_text())
    launch=dict(Version=1,Profile=f['Profile'],ClientVersion='20260817-SD:01',Login=dict(Hostname='login.invalid',Ipv4Address=0x7f000001,Port=44452),Zone=dict(Hostname='zone.invalid',Ipv4Address=0x7f000002,Port=44453),GalaxyId=7,ApprovedCharacterId=0x1122334455667788,LoginConnectionId=0x12345678,ZoneConnectionId=0x87654321,PhaseBudgetMilliseconds=30000,OverallBudgetMilliseconds=120000,DisconnectTimeoutMilliseconds=3000,MinimumReadinessApproved=True)
    bad=[]
    def changed(path,value):
        item=copy.deepcopy(launch);parent=item
        for key in path[:-1]:parent=parent[key]
        parent[path[-1]]=value;bad.append(json.dumps(item).encode().hex())
    for key,value in [('Version',2),('Version',1.5),('ClientVersion',''),('ClientVersion','x'*257),('ClientVersion','bad\nversion'),('ApprovedCharacterId',0),('ApprovedCharacterId',-1),('ApprovedCharacterId',str(launch['ApprovedCharacterId'])),('ApprovedCharacterId',2**64),('LoginConnectionId',-1),('LoginConnectionId',2**32),('ZoneConnectionId',launch['LoginConnectionId']),('PhaseBudgetMilliseconds',0),('PhaseBudgetMilliseconds',30001),('OverallBudgetMilliseconds',120001),('OverallBudgetMilliseconds',29999),('DisconnectTimeoutMilliseconds',0),('DisconnectTimeoutMilliseconds',30001),('MinimumReadinessApproved',False),('MinimumReadinessApproved',None),('MovementPrerequisites',None),('Credentials','forbidden')]:changed([key],value)
    for path,value in [(['Login','Password'],'forbidden'),(['Zone','Hostname'],'https://zone.invalid'),(['Login','Ipv4Address'],0),(['Login','Port'],65536),(['Login','Port'],'44452'),(['Profile','Version'],2),(['Profile','Extra'],1),(['Profile','Idle','Extra'],1),(['Profile','AssetRoot'],'relative')]:changed(path,value)
    raw=json.dumps(launch)
    bad.extend(x.hex() for x in [b'',b'\xff',raw[:-1].encode(),('/*comment*/'+raw).encode(),(raw[:-1]+',}').encode(),raw.replace('"Version": 1','"Version": 1, "Version": 1',1).encode(),raw.replace('"Hostname": "login.invalid"','"Hostname": "login.invalid", "Hostname": "login.invalid"').encode(),raw.replace('"AssetRoot":','"Version": 1, "AssetRoot":').encode(),b' '*131073])
    movement=copy.deepcopy(launch);movement['MovementPrerequisites']=dict(FreshStampZero=True,MatchingValidatedPosition=True,MovementPrerequisitesValidated=True)
    disabled=copy.deepcopy(movement);disabled['MovementPrerequisites']['MatchingValidatedPosition']=False
    malformed=copy.deepcopy(movement);malformed['MovementPrerequisites']['FreshStampZero']=1;bad.append(json.dumps(malformed).encode().hex())
    f.update(Launch=launch,MovementLaunch=movement,DisabledMovementLaunch=disabled,InvalidLaunchHex=bad,ExpectedAttemptCases=[dict(Name='phase',Times=[100,30099,30100],Reason='PhaseTimeout'),dict(Name='overall',Times=[100,30099,60098,90097,119999,120100],Reason='OverallTimeout'),dict(Name='ready-stall',Times=[0,1,30001],Reason='PhaseTimeout'),dict(Name='visible-stall',Times=[0,1,30001],Reason='PhaseTimeout')])
    out.write_text(json.dumps(f,indent=2)+'\n');print(json.dumps(dict(Generated=True,InvalidInputs=len(bad),Path=str(out))))
if __name__=='__main__':main()
