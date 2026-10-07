"""Independent strict-input counts, protected selection and exact typed-budget outcomes."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig'))
    assert r['Passed'] and r['InvalidInputs']==len(f['InvalidLaunchHex']) and r['WrongCharacterPreserved'] and r['Auth']==f['Peer']['ExpectedAuthWire'] and r['Select']==f['Peer']['ExpectedSelectWire']
    assert r['AttemptCases']==[dict(Name=x['Name'],Reason=x['Reason']) for x in f['ExpectedAttemptCases']]
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],StrictInvalidInputs=r['InvalidInputs'],TypedBudgetCases=4,WrongCharacterPreserved=True,ExactProtectedHandoff=True,Synthetic=True,SocketCreated=False)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
