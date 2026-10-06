"""Independent full-frame bytes from sealed motion arithmetic and original-layer triangles."""
import copy,importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('transform64_grounded63',ROOT/'poc/round63-tests/Integration/terrain_playback_peer.py')
old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
def verify(d,terrain,text):
    pure=copy.deepcopy(d);pure.update(Version=3,Policy=old.current.PLAN)
    plan=old.nominal.plan(old.current.old.expanded(old.current.expanded(pure)))
    geometry=old.surface.fixture_geometry(terrain);start=old.records(text,'nominal_speed_ready=')[0]
    # Root ID comes from sealed mixed create wire, not from codec output.
    root=ROOT/'local-private/parallel-development/round16/integration-peer.json'
    create=bytes.fromhex(json.loads(root.read_text())['human_create']);object_id=struct.unpack_from('<Q',create,6)[0]
    rows=old.records(text,'terrain_full_transform=');assert len(rows)==240
    zbits=0;time=start['TimestampMicroseconds'];seconds=old.nominal.a.scalar(pure['SecondsBits']);checked=0
    for i,step in enumerate(plan):
        displacement=old.nominal.a.bits(old.nominal.a.precision(old.nominal.a.mul(old.nominal.a.scalar(step['StoredBits']),seconds),24))
        zbits=old.nominal.a.bits(old.nominal.a.precision(old.nominal.a.add(old.nominal.a.scalar(zbits),old.nominal.a.scalar(displacement)),24));time+=10000
        z=old.nominal.value(zbits);hit=old.surface.expected(geometry,-1.5,z);assert hit
        y=old.surface.f(old.nominal.word(hit['Height']))
        squared=old.nominal.a.bits(old.nominal.a.precision(old.nominal.a.mul(old.nominal.a.scalar(step['StoredBits']),old.nominal.a.scalar(step['StoredBits'])),24))
        speed=old.nominal.value(old.nominal.a.bits(old.nominal.circle.sqrt_round(old.nominal.a.scalar(squared),24)))
        for j,(direction,kind) in enumerate((('ClientToServer','Root'),('ClientToServer','WithParent'),('ServerToClient','Root'),('ServerToClient','WithParent'))):
            row=rows[4*i+j];parent=kind=='WithParent';outbound=direction=='ClientToServer'
            word=(0x7fc00000 if parent else time//1000) if outbound else 0
            assert row['GlobalStep']==i and row['Direction']==direction and row['Kind']==kind and row['OuterWord']==word and row['MovementCounter']==i+1
            q=row['QuaternionBits'];assert len(q)==4 and all(v&0x7fffffff==0 for v in q[:3]) and q[3]==0x3f800000
            # This supplied straight world-Z trace mathematically has identity orientation;
            # either sign of a zero quaternion component is admissible at this projection.
            header=struct.pack('<HIIIQII',5,0x80ce5e46,0x23 if outbound else 0x1b,0xf1 if parent else 0x71,object_id,word,i+1)
            frame=header+(struct.pack('<Q',0) if parent else b'')+struct.pack('<4I',*q)+struct.pack('<4f',-1.5,z,y,speed)
            assert bytes.fromhex(row['Hex'])==frame and len(frame)==(70 if parent else 62),(i,direction,kind,row)
            assert row['ActualDriverPosition'] and row['SyntheticParentEnvelope']==parent and not row['Sent'] and not row['WorldApplied'] and not row['OriginalSenderTimingParity']
            assert row['TimestampPolicy']==('SuppliedNaNPatternIntegerWord' if parent else 'SuppliedMicrosecondsDividedBy1000') if outbound else row['TimestampPolicy']=='ServerZeroScalar'
            checked+=1
    return dict(Passed=True,IndependentFrames=checked,RootFrames=120,ParentFrames=120,Directions=2,IntegerNaNPatternTimestamps=60,Sent=False,WorldApplied=False,OriginalSenderTimingParity=False)
if __name__=='__main__':
    fixture,terrain,log,out=sys.argv[1:5];result=verify(json.loads(Path(fixture).read_text()),json.loads(Path(terrain).read_text()),Path(log).read_text(encoding='utf-8-sig'))
    old.nominal.fresh(out).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
