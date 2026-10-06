using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldFullTransformApplicationScenarios
{
    public sealed record Case(string Name, string Hex);
    public sealed record Peer(Dictionary<string,string> Applications, string[] Initial, Case[] Cases);
    private static int checks;
    private static void Check(bool condition,string reason) { checks++; if(!condition)throw new Exception(reason); }
    private static void Reject<T>(Action action) where T:Exception
    { try { action(); } catch(T) { checks++; return; } throw new Exception("Expected "+typeof(T).Name); }
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;
        var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        ulong humanId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate")).ObjectId;
        ulong terminalId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("terminalCreate")).ObjectId;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
        int loads=0;
        SceneWorldCoordinator World()=>new(catalog,options,(q,t)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("Unexpected loader"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy);
        long Stage(SceneWorldCoordinator world){long g=world.BeginSelection(humanId).Generation;foreach(string key in peer.Initial)world.Accept(g,Bytes(key));world.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        byte[] Frame(uint counter,ulong? id=null, bool parent=false,float speed=2)
        { var b=Convert.FromHexString(peer.Cases[0].Hex);BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(26),counter);if(id.HasValue)BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(14),id.Value);BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(58),speed);
          if(parent){var result=new byte[70];b.AsSpan(0,30).CopyTo(result);BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(10),0xF1);b.AsSpan(30).CopyTo(result.AsSpan(38));return result;}return b; }
        var outputs=new List<object>();
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);
            using var router=new SceneWorldFullTransformApplicationRouter(world,store,SceneCreateZeroQuaternionPolicy.Identity);
            foreach(var item in peer.Cases)
            {
                byte[] frame=Convert.FromHexString(item.Hex);var original=Snapshot(world);var prior=store.Find(original);
                var result=router.Accept(g,frame,ObjectControllerTransformDirection.ServerToClient);
                Check(result.Applied&&ReferenceEquals(result.Motion,store.Find(Snapshot(world)))&&ReferenceEquals(result.PriorMotion,prior),"Actual issued result identity");
                Check(result.Motion!.Incarnation==original.Object.Incarnation&&result.Motion.Authoritative&&result.Motion.MovementCounter==result.Raw.MovementCounter,"Current incarnation/counter");
                Check(ReferenceEquals(original.Placement,Snapshot(world).Placement)&&world.IsCurrent(original.Replication!) && original.Replication!.Revision == Snapshot(world).Replication!.Revision,"Placement and replication untouched");
                var t=result.Motion.Transform;outputs.Add(new{item.Name,RawHex=Convert.ToHexString(ObjectControllerTransformCodec.Encode(result.Raw)),Rows=new[]{t.RowX.X,t.RowX.Y,t.RowX.Z,t.RowY.X,t.RowY.Y,t.RowY.Z,t.RowZ.X,t.RowZ.Y,t.RowZ.Z},PositionBits=new[]{t.Translation.X,t.Translation.Y,t.Translation.Z}.Select(BitConverter.SingleToUInt32Bits).ToArray(),SpeedBits=BitConverter.SingleToUInt32Bits(result.Motion.Speed),result.DerivedPlacement!.UsedZeroIdentity});
                Array.Clear(frame);Check(ObjectControllerTransformCodec.Encode(result.Raw).Length==62,"Owned result");
            }
            var snapshot=Snapshot(world);store.Clear();var serial=new List<object>();
            foreach(uint counter in new[]{uint.MaxValue,0U,0U,uint.MaxValue,0x80000000U,1U})
            { var prior=store.Find(snapshot);var result=router.Accept(g,Frame(counter),ObjectControllerTransformDirection.ServerToClient);Check(result.Applied||ReferenceEquals(prior,store.Find(snapshot)),"Serial rejection preserves prior");serial.Add(new{Counter=counter,Accepted=result.Applied,Revision=result.Motion!.Revision}); }
            Check(store.PublishLocal(snapshot,AppearanceTransform.Identity,1),"Local interleave");var local=store.Find(snapshot);
            Check(!router.Accept(g,Frame(1),ObjectControllerTransformDirection.ServerToClient).Applied&&ReferenceEquals(local,store.Find(snapshot)),"Private counter survives local");
            Check(router.Accept(g,Frame(2),ObjectControllerTransformDirection.ServerToClient).Applied,"Next authoritative");
            var held=store.Find(snapshot)!;
            void Decline(byte[] frame,SceneWorldFullTransformApplicationStatus expected,long? generation=null,ObjectControllerTransformDirection direction=ObjectControllerTransformDirection.ServerToClient)
            { var result=router.Accept(generation??g,frame,direction);Check(result.Status==expected&&ReferenceEquals(held,store.Find(Snapshot(world))),"Decline prior preservation "+expected); }
            Decline(Frame(3),SceneWorldFullTransformApplicationStatus.WrongDirection,direction:ObjectControllerTransformDirection.ClientToServer);
            Decline(Frame(3),SceneWorldFullTransformApplicationStatus.StaleGeneration,g-1);
            Decline(Frame(3,0),SceneWorldFullTransformApplicationStatus.MissingObject);
            Decline(Frame(3,parent:true),SceneWorldFullTransformApplicationStatus.UnsupportedParent);
            var nonzeroParent = Frame(3,parent:true); BinaryPrimitives.WriteUInt64LittleEndian(nonzeroParent.AsSpan(30),terminalId);
            Decline(nonzeroParent,SceneWorldFullTransformApplicationStatus.UnsupportedParent);
            Reject<InvalidDataException>(()=>router.Accept(g,Frame(3,speed:-1),ObjectControllerTransformDirection.ServerToClient));
            var valid=Frame(3);for(int n=0;n<valid.Length;n++){int size=n;Reject<InvalidDataException>(()=>router.Accept(g,valid.AsSpan(0,size),ObjectControllerTransformDirection.ServerToClient));}
            Reject<InvalidDataException>(()=>router.Accept(g,[..valid,0],ObjectControllerTransformDirection.ServerToClient));
            foreach(int offset in new[]{0,2,10}){var bad=valid.ToArray();bad[offset]^=2;Reject<InvalidDataException>(()=>router.Accept(g,bad,ObjectControllerTransformDirection.ServerToClient));}
            foreach(uint bits in new uint[]{0x7fc00001,0x7f800000,0xff800000})
            foreach(int offset in new[]{22,30,34,38,42,46,50,54,58})
            { var bad=valid.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset),bits);Reject<InvalidDataException>(()=>router.Accept(g,bad,ObjectControllerTransformDirection.ServerToClient));Check(ReferenceEquals(held,store.Find(snapshot)),"Nonfinite prior preservation"); }
            Reject<InvalidDataException>(()=>router.Accept(g,valid,(ObjectControllerTransformDirection)99));
            Reject<OperationCanceledException>(()=>router.Accept(g,valid,ObjectControllerTransformDirection.ServerToClient,new CancellationToken(true)));
            using(var rejectZero=new SceneWorldFullTransformApplicationRouter(world,store,SceneCreateZeroQuaternionPolicy.Reject))
            { var zero=valid.ToArray();Array.Clear(zero,30,16);Reject<InvalidDataException>(()=>rejectZero.Accept(g,zero,ObjectControllerTransformDirection.ServerToClient)); }
            Check(ReferenceEquals(held,store.Find(snapshot)),"All throwing inputs preserve prior");
            Exception? threadError=null;var thread=new Thread(()=>{try{router.Accept(g,valid,ObjectControllerTransformDirection.ServerToClient);}catch(Exception e){threadError=e;}});thread.Start();thread.Join();Check(threadError is InvalidOperationException&&ReferenceEquals(held,store.Find(snapshot)),"Owner thread atomic");
            threadError=null;var disposeThread=new Thread(()=>{try{router.Dispose();}catch(Exception e){threadError=e;}});disposeThread.Start();disposeThread.Join();Check(threadError is InvalidOperationException,"Foreign disposal rejects");
            threadError=null;var constructorThread=new Thread(()=>{try{new SceneWorldFullTransformApplicationRouter(world,store,SceneCreateZeroQuaternionPolicy.Identity);}catch(Exception e){threadError=e;}});constructorThread.Start();constructorThread.Join();Check(threadError is InvalidOperationException,"Foreign world-owner constructor rejects");
            world.Accept(g,Bytes("heightA"));var refreshed=Snapshot(world);Check(snapshot.Replication!.Revision != refreshed.Replication!.Revision&&ReferenceEquals(held,store.Find(refreshed)),"Typed state refresh preserves prior");
            Check(router.Accept(g,Frame(3),ObjectControllerTransformDirection.ServerToClient).Applied,"Fresh typed snapshot application");held=store.Find(refreshed)!;
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(humanId,terminalId,42)));Decline(Frame(4),SceneWorldFullTransformApplicationStatus.Contained);
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(humanId,0,0)));Check(router.Accept(g,Frame(4),ObjectControllerTransformDirection.ServerToClient).Applied,"Root restored");
            var old=store.Find(Snapshot(world))!;world.Accept(g,Bytes("humanDestroy"));world.Accept(g,Bytes("humanCreate"));
            Check(router.Accept(g,Frame(0),ObjectControllerTransformDirection.ServerToClient).Status==SceneWorldFullTransformApplicationStatus.Unfinalized,"Pending decline");world.Accept(g,Bytes("humanBaseline"));world.Accept(g,Bytes("humanEnd"));
            Check(router.Accept(g,Frame(0),ObjectControllerTransformDirection.ServerToClient).Applied&&!store.IsCurrent(old),"Reincarnation counter reset");
            var obsolete=store.Find(Snapshot(world))!;long next=Stage(world);Check(router.Accept(g,Frame(1),ObjectControllerTransformDirection.ServerToClient).Status==SceneWorldFullTransformApplicationStatus.StaleGeneration,"World reset old generation decline");
            Check(router.Accept(next,Frame(0),ObjectControllerTransformDirection.ServerToClient).Applied&&!store.IsCurrent(obsolete),"World reset fresh incarnation");
            router.Dispose();router.Dispose();Check(store.Find(Snapshot(world)) is not null&&world.State!=ZoneSceneTransitionState.Closed,"Borrowed disposal");Reject<ObjectDisposedException>(()=>router.Accept(next,Frame(1),ObjectControllerTransformDirection.ServerToClient));
            outputs.Add(new{Serial=serial});
        }
        using(var world=World())using(var foreign=World())
        {
            long g=Stage(world);Stage(foreign);using var store=new SceneWorldObjectMotionStore(world);using var other=new SceneWorldObjectMotionStore(foreign);
            Reject<ArgumentException>(()=>new SceneWorldFullTransformApplicationRouter(world,other,SceneCreateZeroQuaternionPolicy.Identity));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldFullTransformApplicationRouter(world,store,(SceneCreateZeroQuaternionPolicy)99));
            using var router=new SceneWorldFullTransformApplicationRouter(world,store,SceneCreateZeroQuaternionPolicy.Identity);store.Dispose();
            Check(router.Accept(g,Frame(0),ObjectControllerTransformDirection.ServerToClient).Status==SceneWorldFullTransformApplicationStatus.ClosedStore,"Closed store decline");world.Close();
            Check(router.Accept(world.Generation,Frame(0),ObjectControllerTransformDirection.ServerToClient).Status==SceneWorldFullTransformApplicationStatus.InactiveWorld,"Closed world decline");
        }
        using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))using(var router=new SceneWorldFullTransformApplicationRouter(world,store,SceneCreateZeroQuaternionPolicy.Identity))
        {Check(router.Accept(world.Generation,Frame(0),ObjectControllerTransformDirection.ServerToClient).Status==SceneWorldFullTransformApplicationStatus.InactiveWorld,"Idle world decline");}
        Check(loads==0,"No resource jobs or assets loaded");
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Cases=outputs,Checks=checks,ResourceLoads=loads}));Console.WriteLine("PASS full transform application checks="+checks);
    }
}
