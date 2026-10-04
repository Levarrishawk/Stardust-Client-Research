using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldTerrainMotionDriverScenarios
{
    private static int checks;
    private static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
    private static float F(JsonElement p, string key) => p.GetProperty(key).GetSingle();
    internal static void Run(JsonElement fixture, string exportPath)
    {
        checks = 0;
        var peer = fixture.GetProperty("Peer").Deserialize<Dictionary<string, string>>()!;
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile = File.OpenRead("poc/preview-profiles/human-locomotion.json");
        var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options = new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> {
            { SceneObjectResourceRecipe.CreatureDetail0, new SceneCreatureResourceOptions(idle, bank, true) },
            { SceneObjectResourceRecipe.StaticAppearance, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, null) } };
        var policy = new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,
            new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,
            new(1024,32,64,64,16),new(16,16));
        int loads = 0;
        SceneWorldCoordinator World() => new(catalog, options, (q,t) => { loads++; return Task.FromException<SceneObjectResources>(new Exception("Unexpected load")); },
            SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy);
        byte[] Bytes(string name) => Convert.FromHexString(peer[name]);
        ulong id = SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId;
        ulong staticId = SceneObjectLifecycleCodec.DecodeCreate(Bytes("staticCreate")).ObjectId;
        long Stage(SceneWorldCoordinator world) {
            long g = world.BeginSelection(id).Generation;
            foreach (var name in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"}) world.Accept(g, Bytes(name));
            world.CompleteMinimumSceneBarrier(g,true); return g;
        }
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world, ulong objectId) => world.Snapshots().Single(s=>s.Object.Handle.ObjectId==objectId);
        AppearanceTransform Pose(float x=0,float y=0,float z=0) => AppearanceTransform.Identity with { Translation=new(x,y,z) };
        PortalActorInput Input(float x,float z) => new(new(x,0,z),null);
        TerrainPatchSurfaceQuery Surface(float lo,float hi,float a=0,float b=0,float c=0,bool badNormal=false) {
            Vector2[] layout=[new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)];
            var points=layout.Select(p=>{float x=lo+p.X*(hi-lo),z=4-p.Y*8;return new Vector3(x,a*x+b*z+c,z);}).ToArray();
            var geometry=new TerrainPatchGeometry(points,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0);
            return new(geometry,new(MinimumInterpolatedNormalLength:badNormal?2:1e-6));
        }
        object Export(SceneWorldLocalMotionDriverState s, SceneWorldObjectMotionSnapshot? motion) => new {
            X=s.Transform.Translation.X,Y=s.Transform.Translation.Y,Z=s.Transform.Translation.Z,
            RowX=new[]{s.Transform.RowX.X,s.Transform.RowX.Y,s.Transform.RowX.Z},RowY=new[]{s.Transform.RowY.X,s.Transform.RowY.Y,s.Transform.RowY.Z},RowZ=new[]{s.Transform.RowZ.X,s.Transform.RowZ.Y,s.Transform.RowZ.Z},
            s.Speed,s.TimestampMicroseconds,s.StepsThisUpdate,s.DroppedMicroseconds,s.SuspendedMicroseconds,s.Active,s.FaultType,
            MotionRevision=motion?.Revision,Authoritative=motion?.Authoritative };
        var exports=new List<object>();
        foreach(var item in fixture.GetProperty("Cases").EnumerateArray())
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);
            var initial=item.GetProperty("Initial");motions.PublishAuthoritative(Snapshot(world,id),Pose(initial[0].GetSingle(),initial[1].GetSingle(),initial[2].GetSingle()),0,1);
            var surfaces=item.GetProperty("Surfaces").EnumerateArray().Select(p=>Surface(F(p,"Lo"),F(p,"Hi"),F(p,"A"),F(p,"B"),F(p,"C"),p.GetProperty("BadNormal").GetBoolean())).ToArray();
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,F(item,"Offset"),0,10000,item.GetProperty("MaximumSteps").GetInt32(),new(MaximumTriangleTestsPerCandidate:item.GetProperty("Budget").GetInt32()));
            var records=new List<object>{Export(driver.State,motions.Find(Snapshot(world,id)))};
            foreach(var t in item.GetProperty("Inputs").EnumerateArray()) driver.Submit(t[0].GetInt64(),Input(t[1].GetSingle(),t[2].GetSingle()));
            foreach(var update in item.GetProperty("Updates").EnumerateArray()) {
                long time=update.GetProperty("Time").GetInt64();
                if(update.TryGetProperty("Correction",out var correction)) Check(motions.PublishAuthoritative(Snapshot(world,id),Pose(correction[0].GetSingle(),correction[1].GetSingle(),correction[2].GetSingle()),correction[3].GetSingle(),(uint)(time+2)),"Correction accepted");
                var state=driver.Advance(time,update.GetProperty("Active").GetBoolean());
                var motion=motions.Find(Snapshot(world,id)); records.Add(Export(state,motion));
                Check(motion is not null && motions.IsCurrent(motion),"Exact current local/authoritative record retained");
                Check(state.FaultType is null || !state.Active,"Fault is inactive");
            }
            exports.Add(new{Name=item.GetProperty("Name").GetString(),Records=records});
        }
        // Admission and owned collection isolation, without internal production access.
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            motions.PublishAuthoritative(snapshot,Pose(),0,1);var first=Surface(-4,4,0,0,3);var second=Surface(-4,4,0,0,99);
            var mutable=new[]{first,second};using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,mutable,.5f,0,10000);
            mutable[0]=second;var copied=first.CopyPositions();copied[0]=new(999,999,999);
            Check(driver.State.Transform.Translation.Y==3.5f,"First supplied overlap and offset at constructor");
            driver.Submit(0,Input(1,0));Check(driver.Advance(10000).Transform.Translation.Y==3.5f,"Collection and query inspection copies cannot mutate driver");
            var before=driver.State;var issued=motions.Find(snapshot);
            foreach(var input in new[]{new PortalActorInput(new(0,1,0),null),Input(float.NaN,0),Input(11,0),Input(0,float.PositiveInfinity)}) Reject<ArgumentOutOfRangeException>(()=>driver.Submit(10000,input));
            Reject<ArgumentOutOfRangeException>(()=>driver.Submit(9999,Input(0,0)));
            Reject<ArgumentOutOfRangeException>(()=>driver.Submit(60010001,Input(0,0)));
            Reject<ArgumentOutOfRangeException>(()=>driver.Advance(9999));Reject<ArgumentOutOfRangeException>(()=>driver.Reset(-1));
            Check(driver.State==before&&ReferenceEquals(issued,motions.Find(snapshot)),"Bad admission preserves state/publication, no sticky fault");
            driver.Submit(20000,Input(0,1));Reject<ArgumentOutOfRangeException>(()=>driver.Submit(19000,Input(1,0)));
            driver.Reset(0);for(int i=0;i<1024;i++)driver.Submit(0,Input(1,0));Reject<InvalidOperationException>(()=>driver.Submit(0,Input(0,1)));
            Check(driver.Advance(10000).Speed==1,"Capacity rejects only new input");
            Exception? foreign=null;var thread=new Thread(()=>{try{driver.Advance(20000);}catch(Exception e){foreign=e;}});thread.Start();thread.Join();
            Check(foreign is InvalidOperationException&&driver.State.TimestampMicroseconds==10000,"Owner thread rejection");
            var current=motions.Find(snapshot)!;driver.Dispose();driver.Dispose();Check(motions.IsCurrent(current)&&!driver.State.Active&&world.State!=ZoneSceneTransitionState.Closed,"Borrowed lifetime preserved");
            Reject<ObjectDisposedException>(()=>driver.Advance(20000));Reject<ObjectDisposedException>(()=>driver.Reset(0));Reject<ObjectDisposedException>(()=>driver.Submit(0,Input(0,0)));
        }
        // A late bad sample/budget/outside failure retains all prior successful publications.
        foreach(string failure in new[]{"outside","budget","normal","position"})
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            motions.PublishAuthoritative(snapshot,Pose(),0,1);
            var surfaces=failure=="outside"?new[]{Surface(-1,.025f)}:new[]{Surface(-1,.025f),Surface(.025f,1,badNormal:failure=="normal")};
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,0,0,10000,8,
                new(MaximumTriangleTestsPerCandidate:failure=="budget"?8:16,MaximumPositionComponent:failure=="position"?.025f:1000));
            driver.Submit(0,Input(1,0));var failed=driver.Advance(80000);var publication=motions.Find(snapshot)!;
            Check(failed.StepsThisUpdate==2&&failed.FaultType==nameof(InvalidDataException)&&!failed.Active&&publication.Revision==4,"Two prior ticks survive late "+failure);
            Check(Math.Abs(failed.Transform.Translation.X-.02f)<1e-7&&publication.Transform==failed.Transform,"Last valid publication retained "+failure);
            Reject<InvalidOperationException>(()=>driver.Submit(80000,Input(0,0)));
            var stopped=driver.Advance(90000);Check(stopped.StepsThisUpdate==0&&stopped.Transform==failed.Transform&&ReferenceEquals(publication,motions.Find(snapshot)),"No implicit retry "+failure);
            driver.Reset(100000);Check(driver.State.FaultType is null&&driver.State.Speed==0&&driver.State.Transform.Translation==Vector3.Zero,"Explicit reset grounds immutable create placement "+failure);
            driver.Submit(100000,Input(1,0));Check(driver.Advance(110000).Active,"Reset resumes "+failure);
        }
        foreach(var change in new[]{"recreate","generation","failure","close"})
        using(var world=World()) {
            long g=Stage(world);using var motions=new SceneWorldObjectMotionStore(world);motions.PublishAuthoritative(Snapshot(world,id),Pose(),0,1);
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-1000,1000)},0,0,10000);
            var initial=driver.State;driver.Submit(0,Input(1,0));
            if(change=="recreate") {world.Accept(g,Bytes("destroy"));world.Accept(g,Bytes("create"));world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("end"));}
            else if(change=="generation")Stage(world);else if(change=="failure")Reject<InvalidDataException>(()=>world.Accept(g,new byte[]{1}));else world.Close();
            Check(!driver.Advance(10000).Active&&driver.State.Transform==initial.Transform&&driver.State.TimestampMicroseconds==0,"Old incarnation stops before clock admission "+change);
            Reject<InvalidOperationException>(()=>driver.Reset(10000));
            if(change is "recreate" or "generation") {using var replacement=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-1000,1000)},0,10000,10000);var latest=motions.Find(Snapshot(world,id))!;driver.Dispose();Check(motions.IsCurrent(latest)&&replacement.State.Active,"Old disposal cannot erase replacement");}
        }
        using(var world=World()) using(var otherWorld=World()) {
            Stage(world);Stage(otherWorld);using var motions=new SceneWorldObjectMotionStore(world);using var other=new SceneWorldObjectMotionStore(otherWorld);
            var surfaces=new[]{Surface(-1000,1000)};
            Reject<ArgumentException>(()=>new SceneWorldTerrainMotionDriver(world,other,id,surfaces));
            Reject<InvalidDataException>(()=>new SceneWorldTerrainMotionDriver(world,motions,staticId,surfaces));
            Reject<InvalidDataException>(()=>new SceneWorldTerrainMotionDriver(world,motions,123,surfaces));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,Array.Empty<TerrainPatchSurfaceQuery>()));
            Reject<ArgumentException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,new TerrainPatchSurfaceQuery[]{null!}));
            foreach(float offset in new[]{float.NaN,float.PositiveInfinity,1001f})Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,offset));
            foreach(var limits in new[]{new SceneWorldTerrainMotionLimits(MaximumPatches:0),new(MaximumPatches:257),new(MaximumTriangleTestsPerCandidate:0),new(MaximumTriangleTestsPerCandidate:2049),new(MaximumPositionComponent:float.NaN),new(MaximumFootOffset:-1)})Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,limits:limits));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,stepMicroseconds:999));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,initialTimestampMicroseconds:-1));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,surfaces,maximumStepsPerUpdate:33));
            Check(motions.Find(Snapshot(world,id)) is null,"Rejected construction has no local publication");
            Exception? error=null;var thread=new Thread(()=>{try{using var d=new SceneWorldTerrainMotionDriver(world,motions,id,surfaces);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Constructor owner affinity");
        }
        // Exact/one-less configuration reservations and finite grounded-height rejection.
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            var surface=Surface(-1,1,c:1);var pair=new[]{surface,surface};
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,pair,limits:new(MaximumPatches:1)));
            Reject<InvalidDataException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,new[]{surface},limits:new(MaximumTriangleTestsPerCandidate:7)));
            Reject<InvalidDataException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,new[]{surface},.5f,limits:new(MaximumPositionComponent:1.499f)));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldTerrainMotionDriver(world,motions,id,new[]{surface},.5f,limits:new(MaximumFootOffset:.499f)));
            Check(motions.Find(snapshot) is null,"One-less constructor limits reject before publication");
            using var exact=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{surface},.5f,0,10000,1,new(1,8,1.5f,.5f));
            Check(exact.State.Transform.Translation.Y==1.5f,"Exact patch/triangle/position/offset reservations");
            Check(exact.Advance(10000).Active&&exact.State.Transform.Translation.Y==1.5f,"Exact bounds allow zero-velocity grounded tick");
        }
        // Grounding replaces altitude alone; it does not align orientation to a slope normal.
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            var tilted=Pose() with {RowY=Vector3.UnitZ,RowZ=-Vector3.UnitY};motions.PublishAuthoritative(snapshot,tilted,3,1);
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-4,4,a:.5f,b:.25f)},0,0,10000);
            Check(driver.State.Transform.RowY==Vector3.UnitZ&&driver.State.Speed==3,"Constructor holds proper supplied rotation and speed");
            var zero=driver.Advance(10000);Check(zero.Transform.RowY==Vector3.UnitZ&&zero.Speed==0,"Zero velocity grounds without normal alignment");
            driver.Submit(10000,new(new(10,0,10),false));var diagonal=driver.Advance(20000);
            Check(diagonal.Transform.RowY==Vector3.UnitY&&diagonal.Speed==MathF.Sqrt(200)&&diagonal.Active,"Planar maximum diagonal yaw/speed ignores entry flag");
            Check(Math.Abs(diagonal.Transform.Translation.Y-.075f)<1e-6,"Slope altitude from candidate X/Z only");
        }
        // Correction eligibility is checked before owned adoption; a no-tick correction is not overwritten.
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-1,1)},0,0,10000,8,new(MaximumPositionComponent:2));
            driver.Submit(0,Input(1,0));var valid=driver.Advance(10000);
            motions.PublishAuthoritative(snapshot,Pose(3,0,0),4,1);var external=motions.Find(snapshot)!;
            var rejected=driver.Advance(10000);
            Check(rejected.Transform==valid.Transform&&!rejected.Active&&rejected.FaultType==nameof(InvalidDataException),"Over-cap correction rejected before owned pose mutation");
            Check(ReferenceEquals(external,motions.Find(snapshot)),"Driver never rolls back an externally published authoritative record");
            driver.Reset(20000);Check(driver.State.FaultType is null&&driver.State.Transform.Translation==Vector3.Zero,"Explicit current-incarnation reset recovers correction fault");
        }
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,id);
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-1,1)},0,0,10000);
            driver.Advance(10000,false);motions.PublishAuthoritative(snapshot,Pose(2,77,0),5,1);
            var correction=motions.Find(snapshot)!;var paused=driver.Advance(20000,false);
            Check(paused.Transform.Translation==new Vector3(2,77,0)&&paused.Speed==5&&ReferenceEquals(correction,motions.Find(snapshot)),"Paused correction exposed without terrain pseudo-publication");
            Check(driver.Advance(30000).FaultType is null,"Resume drops suspended movement without querying");
            var failed=driver.Advance(40000);Check(!failed.Active&&failed.FaultType==nameof(InvalidDataException)&&ReferenceEquals(correction,motions.Find(snapshot)),"Next local candidate outside coverage faults without overwrite");
        }
        // Borrowed motion-store closure and capacity loss require explicit recovery.
        using(var world=World()) {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world,1);var snapshot=Snapshot(world,id);
            using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,new[]{Surface(-1,1)},0,0,10000);
            driver.Submit(0,Input(1,0));motions.Clear();motions.PublishLocal(Snapshot(world,staticId),Pose(),0);
            var other=motions.Find(Snapshot(world,staticId))!;var failed=driver.Advance(10000);
            Check(!failed.Active&&failed.FaultType==nameof(InvalidDataException)&&motions.IsCurrent(other),"Capacity fault preserves unrelated borrowed publication");
            Reject<InvalidDataException>(()=>driver.Reset(10000));motions.Clear();driver.Reset(20000);
            Check(driver.State.Active&&driver.State.FaultType is null,"Explicit reset after capacity restored");
            motions.Dispose();Check(!driver.Advance(30000).Active,"Closed borrowed store latches local failure");
            Reject<ObjectDisposedException>(()=>driver.Reset(30000));
        }
        Check(loads==0,"No resource loads");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=exports,ResourceLoads=loads,AssetReads=0,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("R39_TERRAIN_MOTION_PASS checks="+checks+" sequences="+exports.Count);
    }
}
