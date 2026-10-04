using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private JsonDocument? creature4PartialFixture;
    private SceneWorldCreatureView4PartialApplicationRouter? creature4PartialRouter;
    private int creature4PartialIndex;
    private SceneWorldReceivedCreatureMovementInputs? receivedMovementInputs;
    private void LoadCreature4PartialPeer(string path)
    {
        Check(new FileInfo(path).Length is >0 and <=32768,"Bounded received-only peer");
        creature4PartialFixture=JsonDocument.Parse(File.ReadAllBytes(path),new JsonDocumentOptions{MaxDepth=16});
        var root=creature4PartialFixture.RootElement;
        Check(root.GetProperty("Version").GetInt32()==1&&root.GetProperty("Synthetic").GetBoolean()
            &&root.GetProperty("ObjectId").GetUInt64()==humanId&&root.GetProperty("NoDefaultSeeds").GetBoolean()
            &&!root.GetProperty("FullBaselineSupplied").GetBoolean()&&root.GetProperty("Steps").GetArrayLength()==5,"Explicit partial observer peer");
        Check(root.GetProperty("Seed").GetProperty("Hex").GetString()!.Length<=8192
            &&root.GetProperty("Steps").EnumerateArray().All(s=>s.GetProperty("Hex").GetString()!.Length<=8192),"Partial wire bounds");
        peer["human_view4_partial"]=root.GetProperty("Seed").GetProperty("Hex").GetString()!;
    }
    private void QueueCreature4Partial(int index)
    {
        byte[] bytes=Convert.FromHexString(creature4PartialFixture!.RootElement.GetProperty("Steps")[index].GetProperty("Hex").GetString()!);
        try{pump!.QueueApplication(generation,bytes);}finally{Array.Clear(bytes);}
    }
    private void Creature4PartialState(int index)
    {
        var snapshot=world!.FindCreatureView4Partial(generation,humanId);
        Check(snapshot is not null&&world.IsCurrent(snapshot),"Current received-only publication");
        var state=snapshot!.State;
        var expected=index==0?creature4PartialFixture!.RootElement.GetProperty("Seed"):creature4PartialFixture!.RootElement.GetProperty("Steps")[index-1];
        var fields=state.KnownFields.Select(f=>new{f.Index,f.Bits,f.Provenance}).ToArray();
        Check(JsonSerializer.Serialize(fields)==JsonSerializer.Serialize(expected.GetProperty("Fields"))
            &&state.KnownMask==expected.GetProperty("KnownMask").GetUInt16()&&state.KnownCount==expected.GetProperty("KnownCount").GetInt32(),"Exact partial presence/raw words/provenance");
        if(index==0)Check(state.KnownMask==1058&&state.KnownCount==3&&!state.IsKnown(0)&&!state.IsKnown(7)&&!state.IsKnown(11),"Observer has three received fields, no default seed");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman)&&visuals[humanId].CreatureVisual!.Root.Transform.IsEqualApprox(retainedCreature6Transform)&&calls==3&&closed==3,"Partial state retains original pose/publication without archive I/O");
        var batch=world.DrainCreatureView4PartialEvents(generation)!;
        var events=batch.Events.Select(e=>e.Event).ToArray();
        Check(Enumerable.Range(0,events.Length).All(i=>world.IsCurrentCreatureView4PartialEvent(batch,i)),"Current partial journal recipients");
        Check(JsonSerializer.Serialize(events)==JsonSerializer.Serialize(expected.GetProperty("Events")),"Independent ordered received events");
        long pending=world.RetainedCreatureView4PartialBytes;
        Check(pending==state.DecodedBytes+64L*events.Length,"Exact fixed partial journal accounting");
        Check(world.AcknowledgeCreatureView4PartialEvents(batch)&&!world.AcknowledgeCreatureView4PartialEvents(batch),"Partial journal acknowledged exactly once");
        Check(world.RetainedCreatureView4PartialBytes==state.DecodedBytes&&state.DecodedBytes==expected.GetProperty("StateBytes").GetInt64(),"Partial state accounting after ack");
        receivedMovementInputs??=new(world);
        var movement=receivedMovementInputs.Read(generation,humanId)??throw new InvalidDataException("Received movement publication missing");
        Check(receivedMovementInputs.IsCurrent(movement)&&movement.IsOperationalPhase,"Exact received movement context");
        GD.Print("received_movement_state="+JsonSerializer.Serialize(new{Index=index,Phase=movement.Phase.ToString(),movement.Generation,movement.PublicationRevision,movement.StateRevision,movement.DecodedBytes,Fields=movement.Fields,Current=true,NoDefaultSeeds=true,MotionApplied=false,Archives=closed}));
        GD.Print("creature4_partial_state="+JsonSerializer.Serialize(new{Index=index,Name=expected.GetProperty("Name").GetString(),state.ObjectId,StateRevision=state.Revision,StoreRevision=snapshot.Revision,state.KnownMask,state.KnownCount,StateBytes=state.DecodedBytes,Fields=fields,Events=events,PendingBytesBeforeAck=pending,RetainedBytes=world.RetainedCreatureView4PartialBytes,Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void CloseCreature4Partial()
    {
        receivedMovementInputs?.Dispose();receivedMovementInputs=null;
        creature4PartialRouter?.Dispose();creature4PartialRouter=null;
        creature4PartialFixture?.Dispose();creature4PartialFixture=null;
    }
}
