using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
internal static class RetainedSetMotionChecks
{
    internal static object Run(TerrainModernPatchSetSurfaceQuery surface)
    {
        int checks=0,loads=0;
        void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
        using var fixture=JsonDocument.Parse(File.ReadAllBytes("poc/round39-tests/Runtime/ordered-fixtures.json"));
        var peer=fixture.RootElement.GetProperty("Peer").Deserialize<Dictionary<string,string>>()!;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{
            [SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(idle,bank,true),
            [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)};
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,
            new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
        using var world=new SceneWorldCoordinator(catalog,options,(_,_)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("Unexpected load"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy);
        byte[] Bytes(string name)=>Convert.FromHexString(peer[name]);ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId;
        long generation=world.BeginSelection(id).Generation;
        foreach(var name in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})Check(world.Accept(generation,Bytes(name)),"staged synthetic lifecycle");
        Check(world.CompleteMinimumSceneBarrier(generation,true) is not null,"scene barrier");
        var snapshot=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);using var motions=new SceneWorldObjectMotionStore(world);
        foreach(var limits in new[]{new SceneWorldTerrainMotionLimits(MaximumPatches:15),new(MaximumTriangleTestsPerCandidate:127)}){
            bool rejected=false;try{using var invalid=new SceneWorldTerrainMotionDriver(world,motions,id,surface,limits:limits);}catch(ArgumentOutOfRangeException){rejected=true;}
            Check(rejected&&motions.Find(snapshot) is null,"aggregate admission before local publication");
        }
        using var driver=new SceneWorldTerrainMotionDriver(world,motions,id,surface,0,0,10000,1,new(MaximumPatches:16,MaximumTriangleTestsPerCandidate:128));
        Check(driver.State.Transform.Translation.Y==(float)surface.Query(0,0)!.Hit.Height,"actual aggregate initial grounding");
        driver.Submit(0,new(new Vector3(0,0,1),null));var state=driver.Advance(10000);
        Check(state.FaultType is null&&state.StepsThisUpdate==1&&state.Transform.Translation.Y==(float)surface.Query(state.Transform.Translation.X,state.Transform.Translation.Z)!.Hit.Height,"actual aggregate tick grounding");
        var saved=motions.Find(snapshot)!;driver.Dispose();Check(motions.IsCurrent(saved)&&surface.Query(0,0) is not null&&loads==0,"borrowed source and final motion preserved without I/O");
        return new{Passed=true,Checks=checks,ResourceLoads=loads,ActualAggregateGrounding=true,AdmissionRejectedBeforePublication=2,OriginalCollisionParity=false};
    }
}
