"""Independent full model/events plus literal current-incarnation/global-store receipts."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]

def verify(fixture,receipt):
    source=ROOT/'poc/round42-tests/Runtime/peer.py'
    spec=importlib.util.spec_from_file_location('accepted42_full_peer',source);p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
    data=json.loads(Path(fixture).read_text());assert data==p.generate()
    actual=json.loads(Path(receipt).read_text());assert actual['Synthetic'] and not actual['WorldRouting']
    assert len(actual['Records'])==len(data['Cases']);events=0
    for case,record in zip(data['Cases'],actual['Records']):
        raw,_=p.BASE['parse'](bytes.fromhex(case['Baseline']),'Baseline');updates,codec=p.parse(case['Hex'])
        initial,current,expected,_,_=p.oracle(raw,updates,codec)
        assert record['Name']==case['Name'] and record['Initial']==initial and record['Current']==current
        assert record['Events']==expected
        assert record['StoreRevision']==2 and record['StateBytes']==current['DecodedBytes']
        assert record['EventBytes']==sum(e['LogicalBytes'] for e in expected) and record['PreparedAbortRetained']
        events+=len(expected)
    assert actual['Lifecycle']==dict(HistoricalCurrentAfterFinalize=True,StoreRevision=4,RecreatedStateRevision=1,ClosedStateBytes=0,ClosedEventBytes=0,BorrowedRegistryCurrent=True)
    assert actual['Budgets']==[
        dict(Mission=False,Cap=274,Accepted=True,StateBytes=161,EventBytes=113,StoreRevision=4),
        dict(Mission=False,Cap=273,Accepted=False,StateBytes=144,EventBytes=48,StoreRevision=3),
        dict(Mission=True,Cap=272,Accepted=True,StateBytes=160,EventBytes=112,StoreRevision=4),
        dict(Mission=True,Cap=271,Accepted=False,StateBytes=144,EventBytes=48,StoreRevision=3)]
    return dict(Passed=True,Sequences=len(data['Cases']),Events=events,PreparedAbort=True,Incarnation=True,VariableJournalGlobalCaps=4,WorldRouting=False)

if __name__=='__main__':print(json.dumps(verify(*sys.argv[1:3])))
