using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private JsonDocument? creature4Fixture;
    private SceneWorldCreatureView4FullApplicationRouter? creature4FullRouter;
    private int creature4Index;
    private void LoadCreature4FullPeer(string path)
    {
        Check(new FileInfo(path).Length is >0 and <=32768,"Bounded private-view fixture");
        creature4Fixture=JsonDocument.Parse(File.ReadAllBytes(path),new JsonDocumentOptions{MaxDepth=16});
        var root=creature4Fixture.RootElement;
        Check(root.GetProperty("Version").GetInt32()==1&&root.GetProperty("Synthetic").GetBoolean()
            &&root.GetProperty("ObjectId").GetUInt64()==humanId&&root.GetProperty("Steps").GetArrayLength()==5,"Explicit synthetic complete CREO4 peer");
        Check(root.GetProperty("Baseline").GetString()!.Length<=8192&&root.GetProperty("Steps").EnumerateArray().All(v=>v.GetProperty("Hex").GetString()!.Length<=8192&&v.GetProperty("ModelHex").GetString()!.Length<=8192),"Private-view wire bounds");
        peer["human_view4_baseline"]=root.GetProperty("Baseline").GetString()!;
    }
    private void QueueCreature4Full(int index)
    {
        byte[] bytes=Convert.FromHexString(creature4Fixture!.RootElement.GetProperty("Steps")[index].GetProperty("Hex").GetString()!);
        try{pump!.QueueApplication(generation,bytes);}finally{Array.Clear(bytes);}
    }
    private void Creature4FullState(int index)
    {
        var snapshot=world!.FindCreatureView4Full(generation,humanId)!;
        Check(snapshot is not null&&world.IsCurrent(snapshot),"Current complete private-view publication");
        var state=snapshot!.State;var expected=index==0?creature4Fixture!.RootElement.GetProperty("Seed"):creature4Fixture!.RootElement.GetProperty("Steps")[index-1];
        string modelHex=Convert.ToHexString(CreatureView4Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:4096)));
        Check(modelHex==expected.GetProperty("ModelHex").GetString(),"Exact canonical full CREO4 model");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman)&&visuals[humanId].CreatureVisual!.Root.Transform.IsEqualApprox(retainedCreature6Transform)&&calls==3&&closed==3,"Private state retains original pose/publication without archive I/O");
        var batch=world.DrainCreatureView4FullEvents(generation)!;
        object Text(ReplicationByteString value)=>new{Hex=Convert.ToHexString(value.CopyBytes()),Extended=value.UsesExtendedLength};
        var events=batch.Events.Select(record=>{var e=record.Event;return new{e.Kind,e.Field,e.UpdateOrdinal,e.Opcode,e.Index,e.OldBits,e.NewBits,e.OldValue,e.NewValue,Name=e.Name is null?null:Text(e.Name),e.OldBase,e.OldBonus,e.NewBase,e.NewBonus,e.Pair,e.LogicalBytes};}).ToArray();
        Check(Enumerable.Range(0,events.Length).All(i=>world.IsCurrentCreatureView4FullEvent(batch,i)),"Current finalized private event recipients");
        Check(index==0?events.Length==0:JsonSerializer.Serialize(events)==JsonSerializer.Serialize(expected.GetProperty("Events")),"Independent ordered private event metadata");
        long pending=world.RetainedCreatureView4FullBytes;
        Check(pending==state.DecodedBytes+batch.Events.Sum(e=>e.Event.LogicalBytes),"Exact variable private journal charge");
        Check(world.AcknowledgeCreatureView4FullEvents(batch)&&!world.AcknowledgeCreatureView4FullEvents(batch),"Private journal acknowledged exactly once");
        Check(world.RetainedCreatureView4FullBytes==state.DecodedBytes&&state.DecodedBytes==expected.GetProperty("StateBytes").GetInt64(),"Private current-state accounting");
        GD.Print("creature4_full_state="+JsonSerializer.Serialize(new{Index=index,Name=index==0?"seed":expected.GetProperty("Name").GetString(),state.ObjectId,ModelHex=modelHex,StateRevision=state.Revision,StoreRevision=snapshot.Revision,RetainedBytes=world.RetainedCreatureView4FullBytes,Events=events,PendingBytesBeforeAck=pending,Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void CloseCreature4Full()
    {
        creature4FullRouter?.Dispose();creature4FullRouter=null;
        creature4Fixture?.Dispose();creature4Fixture=null;
    }
}
