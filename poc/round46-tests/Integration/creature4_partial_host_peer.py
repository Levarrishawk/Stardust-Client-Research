"""Literal observer CREO4 scalar wires and independent received-only host oracle."""
import json,struct,sys
from pathlib import Path
OID=0xDABC001122334455
FIELDS=[0,1,4,5,6,7,8,9,10,11,12]
def wire(updates,declared):
    body=b''.join(struct.pack('<H'+('Q' if f==6 else 'I'),f,b) for f,b in updates)
    return (struct.pack('<HIQIBIH',5,0x12862153,OID,0x4352454f,4,len(body)+2,declared)+body).hex().upper()
def generate():
    plans=[('observer_before_end',3,[(1,0x3f800000),(5,0x3f000000),(10,0x3f400000)]),
           ('equal_and_new',65535,[(5,0x3f000000),(6,(1<<64)-1),(0,0x7fa00001)]),
           ('ordered_repeated',0,[(1,0x80000000),(1,0),(1,0)]),
           ('complete_received_scalars',1,[(f,(1<<63)+7 if f==6 else 0x3f800000+f) for f in FIELDS]),
           ('equal_source_revision',11,[(f,(1<<63)+7 if f==6 else 0x3f800000+f) for f in FIELDS]),
           ('raw_final',2,[(10,0x80000000),(7,0xffc00001)])]
    known={};rows=[]
    for revision,(name,declared,updates) in enumerate(plans,1):
        events=[]
        for ordinal,(f,bits) in enumerate(updates):
            prior=known.get(f);source=dict(Revision=revision,DeclaredCount=declared,Ordinal=ordinal,Origin=1)
            events.append(dict(Index=f,Ordinal=ordinal,OldWasKnown=prior is not None,OldBits=prior[0] if prior else None,NewBits=bits,OldProvenance=prior[1] if prior else None,Provenance=source));known[f]=(bits,source)
        fields=[dict(Index=f,Bits=b,Provenance=p) for f,(b,p) in sorted(known.items())]
        rows.append(dict(Name=name,Hex=wire(updates,declared),StateRevision=revision,StoreRevision=revision,
                         KnownMask=sum(1<<f for f in known),KnownCount=len(known),StateBytes=16+32*len(known),Fields=fields,Events=events))
    return dict(Version=1,Synthetic=True,ObjectId=OID,NoDefaultSeeds=True,FullBaselineSupplied=False,Seed=rows[0],Steps=rows[1:])
def verify(fixture,log):
    data=json.loads(Path(fixture).read_text());assert data==generate();text=Path(log).read_text(encoding='utf-8-sig')
    rows=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('creature4_partial_state=')]
    expected=[data['Seed']]+data['Steps'];assert len(rows)==len(expected)==6
    for index,(got,want) in enumerate(zip(rows,expected)):
        for key in ['Name','StateRevision','StoreRevision','KnownMask','KnownCount','StateBytes','Fields','Events']:assert got[key]==want[key],(index,key)
        assert got['Index']==index and got['ObjectId']==OID and got['Current'] and got['AcknowledgedOnce'] and got['VisualRetained'] and got['Archives']==3
        assert got['PendingBytesBeforeAck']==want['StateBytes']+64*len(want['Events']) and got['RetainedBytes']==want['StateBytes']
    assert rows[0]['KnownMask']==1058 and rows[0]['KnownCount']==3
    assert 'PASS: pumped creature_view4_partial=true' in text
    assert 'creature4_partial_cleanup={"StateAndJournalBytes":0,"Drained":true}' in text
    print(json.dumps(dict(Passed=True,PartialModels=6,ReceivedScalarEvents=sum(len(e['Events']) for e in expected),NoDefaultSeeds=True,OriginalResources=True,WorldIngress=True,Live=False)))
if __name__=='__main__':
    if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n');print('Generated received-only observer host fixture')
    else:verify(*sys.argv[2:4])
