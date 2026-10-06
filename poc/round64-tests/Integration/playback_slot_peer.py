"""Check raw replacement counters, then reuse the independently derived full-trace oracle."""
import copy,importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('slot64_grounded63',ROOT/'poc/round63-tests/Integration/terrain_playback_peer.py')
old=importlib.util.module_from_spec(s);s.loader.exec_module(old)

def verify(d,terrain,wire,text):
    assert d['Version']==5 and d['TerrainBindingPolicy']=='OwnedTerrainPlaybackSlot' and d['ReplacementAtStep']==30
    assert d['Policy']=='supplied_local_current_reader_and_replaced_nominal_plan_modern_terrain_trace'
    ready=old.records(text,'terrain_playback_slot_ready=')
    assert ready==[dict(Revision=1,ActualBinding=True,OwnedSlot=True,OriginalEffectiveMovementParity=False)]
    pure=copy.deepcopy(d);pure.update(Version=4,Policy='supplied_local_current_reader_and_nominal_plan_modern_terrain_trace',TerrainBindingPolicy='OwnedTerrainDriverAndCurrentPlanPlayback')
    planar=copy.deepcopy(pure);planar.update(Version=3,Policy=old.current.PLAN)
    expanded=old.current.expanded(planar);plan=old.nominal.plan(old.current.old.expanded(expanded))
    start=old.records(text,'nominal_speed_ready=')[0]['TimestampMicroseconds']
    replacement=old.records(text,'terrain_playback_slot_replaced=')
    assert replacement==[dict(GlobalStep=30,Revision=2,RemainingSteps=30,InitialCurrentBits=plan[29]['StoredBits'],InvalidPreserved=True,
        UnavailablePreserved=True,OldBindingDisposed=True,OldDriverInactive=True,OldPlaybackDisposed=True,PriorReceiptCurrent=False,
        ActualSource=True,ActualTerrain=True,PosePreserved=True,TimestampMicroseconds=start+300000,OriginalEffectiveMovementParity=False)]
    assert old.records(text,'terrain_playback_slot_cleanup=')==[dict(Disposed=True,CurrentCleared=True,Revision=2,BorrowedSourcePreserved=True,BorrowedTerrainPreserved=True)]
    rows=old.records(text,'current_plan_playback_step=');assert len(rows)==60
    material=bool(old.records(text,'terrain_supplied_material_cpu='))
    patches=old.records(text,'supplied_terrain_patch=');assert len(patches)==16
    assert all(p['RenderPolicy']==('ModernSuppliedMainTexture' if material else 'ModernLitVertexColor') for p in patches)
    for i,row in enumerate(rows):
        index=i if i<30 else i-30
        assert row['Index']==index
        for key in ('CompletedSteps','StateReads','SubmitAttempts','AdvanceAttempts'):assert row[key]==index+1,(i,key,row)
        assert row['State']==('Complete' if i==59 else 'Ready')
    # Raw local indices/counters have passed. Project them to global trace coordinates
    # solely for the unchanged full numerical, source, grounding and cleanup oracle.
    index=0;lines=[]
    for line in text.splitlines():
        if line.startswith('current_plan_playback_step='):
            row=json.loads(line.split('=',1)[1]);row['Index']=index
            for key in ('CompletedSteps','StateReads','SubmitAttempts','AdvanceAttempts'):row[key]=index+1
            line='current_plan_playback_step='+json.dumps(row);index+=1
        elif line.startswith('supplied_terrain_patch='):
            # Material binding has its separate original-byte/upload verifier.
            # Project only the already checked policy label for the old geometry oracle.
            row=json.loads(line.split('=',1)[1]);row['RenderPolicy']='ModernLitVertexColor'
            line='supplied_terrain_patch='+json.dumps(row)
        lines.append(line)
    result=old.verify(pure,terrain,wire,'\n'.join(lines))
    result.update(ActualReplacementSlot=True,SuccessfulRevisions=2,InvalidReplacementPreserved=True,UnavailableReplacementPreserved=True,
        OldOwnedChildrenRetired=True,RawSegmentCounterChecks=300,OriginalEffectiveMovementParity=False)
    return result

if __name__=='__main__':
    fixture,terrain,wire,log,out=sys.argv[1:6]
    result=verify(json.loads(Path(fixture).read_text()),json.loads(Path(terrain).read_text()),json.loads(Path(wire).read_text()),Path(log).read_text(encoding='utf-8-sig'))
    old.nominal.fresh(out).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
