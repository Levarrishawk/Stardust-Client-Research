using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldClientRootMotionBuilderScenarios
{
    public sealed record Case(string Name,uint[] RowBits,uint[] PositionBits,uint SpeedBits,uint Flags,uint Counter,uint Timestamp);
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Case[] Cases);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception
    {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static float F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate")).ObjectId,otherId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("terminalCreate")).ObjectId;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        int loads=0;
        SceneWorldCoordinator World()=>new(catalog,options,(q,t)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("No resource jobs"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,
            new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)));
        long Stage(SceneWorldCoordinator world){long g=world.BeginSelection(id).Generation;foreach(var key in peer.Initial)world.Accept(g,Bytes(key));world.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        var output=new List<object>();
        object Export(string name,SceneWorldClientRootMotionPacket packet)=>new{Name=name,Hex=Convert.ToHexString(packet.Application),QuaternionBits=new[]{packet.Raw.QuaternionX,packet.Raw.QuaternionY,packet.Raw.QuaternionZ,packet.Raw.QuaternionW}.Select(BitConverter.SingleToUInt32Bits).ToArray()};
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var builder=new SceneWorldClientRootMotionBuilder(world,store,new(float.MaxValue,float.MaxValue));
            foreach(var item in peer.Cases)
            {
                float[] a=item.RowBits.Select(F).ToArray();var pose=new AppearanceTransform(new(a[0],a[1],a[2]),new(a[3],a[4],a[5]),new(a[6],a[7],a[8]),new(F(item.PositionBits[0]),F(item.PositionBits[1]),F(item.PositionBits[2])));
                var snapshot=Snapshot(world);Check(store.PublishLocal(snapshot,pose,F(item.SpeedBits)),"Actual local row");var motion=store.Find(snapshot)!;
                var batch=world.DrainChanges(g)!;int entries=batch.Entries.Count;
                var packet=builder.Build(snapshot,motion,item.Flags,item.Counter,item.Timestamp);
                Check(world.IsCurrent(batch)&&world.DrainChanges(g)!.Entries.Count==entries,"Build preserves inspected batch identity/entries without acknowledgement");
                Check(ReferenceEquals(packet.SourceMotion,motion)&&ReferenceEquals(packet.SourceSnapshot,snapshot)&&ReferenceEquals(store.Find(snapshot),motion),"No row mutation/exact issued source");
                Check(packet.Raw.Timestamp==item.Timestamp&&packet.Raw.MovementCounter==item.Counter&&packet.Raw.Flags==item.Flags&&packet.Raw.Direction==ObjectControllerTransformDirection.ClientToServer&&packet.Application.Length==62,"Explicit metadata");
                var decoded=ObjectControllerTransformCodec.Decode(packet.Application,ObjectControllerTransformDirection.ClientToServer);Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(decoded))==Convert.ToHexString(packet.Application),"Exact outbound codec roundtrip");
                var corroborated=SceneCreatePlacementBuilder.Create(snapshot.Object.Create with{QuaternionX=decoded.QuaternionX,QuaternionY=decoded.QuaternionY,QuaternionZ=decoded.QuaternionZ,QuaternionW=decoded.QuaternionW,PositionX=decoded.PositionX,PositionY=decoded.PositionY,PositionZ=decoded.PositionZ},SceneCreateZeroQuaternionPolicy.Reject);
                Check(Vector3.Distance(corroborated.Transform.RowX,pose.RowX)<2e-6&&Vector3.Distance(corroborated.Transform.RowY,pose.RowY)<2e-6&&Vector3.Distance(corroborated.Transform.RowZ,pose.RowZ)<2e-6,"Pose roundtrip corroboration");
                output.Add(Export(item.Name,packet));var another=builder.Build(snapshot,motion,item.Flags,item.Counter,item.Timestamp);Array.Clear(packet.Application);Check(another.Application.Any(b=>b!=0)&&packet.Raw.Timestamp==item.Timestamp&&store.IsCurrent(motion),"Caller owns isolated encoded bytes");
            }
            var current=Snapshot(world);store.PublishLocal(current,AppearanceTransform.Identity,0);var held=store.Find(current)!;
            Reject<InvalidDataException>(()=>builder.Build(current,held with{},0,0,0));
            Reject<InvalidDataException>(()=>builder.Build(current with{Placement=current.Placement with{}},held,0,0,0));
            Reject<InvalidDataException>(()=>builder.Build(current with{Replication=null},held,0,0,0));
            Reject<InvalidDataException>(()=>builder.Build(current with{Envelopes=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==otherId).Envelopes},held,0,0,0));
            using(var second=new SceneWorldObjectMotionStore(world)){second.PublishLocal(current,AppearanceTransform.Identity,0);Reject<InvalidDataException>(()=>builder.Build(current,second.Find(current)!,0,0,0));}
            var untyped=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==otherId);store.PublishLocal(untyped,AppearanceTransform.Identity,0);Reject<InvalidDataException>(()=>builder.Build(untyped,store.Find(untyped)!,0,0,0));
            Reject<InvalidDataException>(()=>builder.Build(current,held with{Transform=AppearanceTransform.Identity with{RowX=Vector3.Zero}},0,0,0));
            Reject<OperationCanceledException>(()=>builder.Build(current,held,0,0,0,new CancellationToken(true)));
            Exception? error=null;var thread=new Thread(()=>{try{builder.Build(current,held,0,0,0);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&ReferenceEquals(held,store.Find(current)),"Owner thread atomic");
            error=null;var disposal=new Thread(()=>{try{builder.Dispose();}catch(Exception e){error=e;}});disposal.Start();disposal.Join();Check(error is InvalidOperationException,"Foreign disposal rejects without clearing borrowed rows");
            using(var bounded=new SceneWorldClientRootMotionBuilder(world,store,new(1,1)))
            {store.PublishLocal(current,AppearanceTransform.Identity with{Translation=new(2,0,0)},0);var row=store.Find(current)!;Reject<InvalidDataException>(()=>bounded.Build(current,row,0,0,0));store.PublishLocal(current,AppearanceTransform.Identity,2);row=store.Find(current)!;Reject<InvalidDataException>(()=>bounded.Build(current,row,0,0,0));}
            foreach(var invalid in new[]{AppearanceTransform.Identity with{RowX=Vector3.Zero},AppearanceTransform.Identity with{RowX=-Vector3.UnitX},AppearanceTransform.Identity with{RowX=new(2,0,0)},AppearanceTransform.Identity with{RowX=new(float.NaN,0,0)},AppearanceTransform.Identity with{Translation=new(float.PositiveInfinity,0,0)}})
            {var prior=store.Find(current);Reject<InvalidDataException>(()=>store.PublishLocal(current,invalid,0));Check(ReferenceEquals(prior,store.Find(current)),"Inherited malformed rigid store boundary");}
            foreach(float speed in new[]{-1f,float.NaN,float.PositiveInfinity})Reject<ArgumentOutOfRangeException>(()=>store.PublishLocal(current,AppearanceTransform.Identity,speed));
            store.PublishLocal(current,AppearanceTransform.Identity,0);held=store.Find(current)!;world.Accept(g,Bytes("heightA"));Reject<InvalidDataException>(()=>builder.Build(current,held,0,0,0));current=Snapshot(world);Check(ReferenceEquals(held,store.Find(current)),"Typed refresh preserves row but requires fresh snapshot");
            store.PublishAuthoritative(current,AppearanceTransform.Identity,0,0);var authoritative=store.Find(current)!;Reject<InvalidDataException>(()=>builder.Build(current,authoritative,0,0,0));store.PublishLocal(current,AppearanceTransform.Identity,0);held=store.Find(current)!;
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(id,otherId,0)));var contained=Snapshot(world);Reject<InvalidDataException>(()=>builder.Build(contained,held,0,0,0));world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(id,0,0)));current=Snapshot(world);
            store.PublishLocal(current,AppearanceTransform.Identity,0);held=store.Find(current)!;store.PublishLocal(current,AppearanceTransform.Identity,1);Reject<InvalidDataException>(()=>builder.Build(current,held,0,0,0));
            var oldSnapshot=current;var oldMotion=store.Find(current)!;world.Accept(g,Bytes("humanDestroy"));world.Accept(g,Bytes("humanCreate"));var pending=Snapshot(world);Reject<InvalidDataException>(()=>builder.Build(pending,oldMotion,0,0,0));world.Accept(g,Bytes("humanBaseline"));world.Accept(g,Bytes("humanEnd"));current=Snapshot(world);store.PublishLocal(current,AppearanceTransform.Identity,0);Reject<InvalidDataException>(()=>builder.Build(oldSnapshot,oldMotion,0,0,0));
            long next=Stage(world);current=Snapshot(world);store.PublishLocal(current,AppearanceTransform.Identity,0);held=store.Find(current)!;Reject<InvalidDataException>(()=>builder.Build(oldSnapshot,oldMotion,0,0,0));
            builder.Dispose();Check(store.IsCurrent(held)&&world.State!=ZoneSceneTransitionState.Closed,"Borrowed disposal survival");Reject<ObjectDisposedException>(()=>builder.Build(current,held,0,0,0));
        }
        using(var world=World())
        {
            Stage(world);using var store=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world);store.PublishLocal(snapshot,AppearanceTransform.Identity,0);
            using var builder=new SceneWorldClientRootMotionBuilder(world,store,new(100,100));
            using(var local=new SceneWorldLocalMotionDriver(world,store,id,stepMicroseconds:10000))
            {local.Submit(0,new(new(2,0,0),null));Check(local.Advance(10000).FaultType is null,"Actual local driver");output.Add(Export("local-driver",builder.Build(Snapshot(world),store.Find(Snapshot(world))!,0x89abcdef,0xffffffff,0x7fc00001)));}
            store.PublishLocal(Snapshot(world),AppearanceTransform.Identity,0);
            Vector2[] layout=[new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)];
            var points=layout.Select(p=>new Vector3(-4+p.X*8,2,4-p.Y*8)).ToArray();var surface=new TerrainPatchSurfaceQuery(new TerrainPatchGeometry(points,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0));
            using(var terrain=new SceneWorldTerrainMotionDriver(world,store,id,new[]{surface},stepMicroseconds:10000))
            {terrain.Submit(0,new(new(0,0,2),null));Check(terrain.Advance(10000).FaultType is null,"Actual grounded terrain driver");output.Add(Export("terrain-driver",builder.Build(Snapshot(world),store.Find(Snapshot(world))!,0xfedcba98,0,0xff800000)));}
            var held=store.Find(Snapshot(world))!;store.Dispose();Reject<InvalidDataException>(()=>builder.Build(Snapshot(world),held,0,0,0));
        }
        using(var world=World())using(var foreign=World())
        {
            Stage(world);Stage(foreign);using var store=new SceneWorldObjectMotionStore(world);using var other=new SceneWorldObjectMotionStore(foreign);var own=Snapshot(world);var outside=Snapshot(foreign);store.PublishLocal(own,AppearanceTransform.Identity,0);other.PublishLocal(outside,AppearanceTransform.Identity,0);
            Reject<ArgumentException>(()=>new SceneWorldClientRootMotionBuilder(world,other,new(100,100)));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldClientRootMotionBuilder(world,store,new(float.NaN,100)));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldClientRootMotionBuilder(world,store,new(100,-1)));
            using var builder=new SceneWorldClientRootMotionBuilder(world,store,new(100,100));Reject<InvalidDataException>(()=>builder.Build(outside,other.Find(outside)!,0,0,0));var held=store.Find(own)!;world.Close();Reject<InvalidDataException>(()=>builder.Build(own,held,0,0,0));
        }
        Check(loads==0,"No resource loads or sends");File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Cases=output,Checks=checks,ResourceLoads=loads}));Console.WriteLine("PASS outbound local root checks="+checks);
    }
}
