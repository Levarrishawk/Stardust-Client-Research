using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class InteriorDoorSceneScenarios
{
    internal static (PortalLayoutDescription Layout, Dictionary<int, FloorMeshDescription> Floors, InteriorDoorBinding[] Bindings) Fixture()
    {
        var basis = InteriorDoorScenarios.Fixture(); var pose = basis.DoorToCell;
        var secondPose = pose with { Translation = new(4,0,0) };
        var r = basis.Layout.Cells[0].Portals[0];
        var layout = basis.Layout with
        {
            Geometry = new[] { basis.Layout.Geometry[0], basis.Layout.Geometry[0] with { Vertices = basis.Layout.Geometry[0].Vertices.Select(v => v + new Vector3(4,0,0)).ToArray() } },
            Cells = new[]
            {
                basis.Layout.Cells[0] with { Portals = new[] { r with { TargetCell=1 } } },
                basis.Layout.Cells[1] with { Portals = new[] { r with { TargetCell=0 }, r with { GeometryIndex=1, TargetCell=2, DoorTransform=secondPose } } },
                basis.Layout.Cells[1] with { Portals = new[] { r with { GeometryIndex=1, TargetCell=1, DoorTransform=secondPose } } }
            }
        };
        FloorMeshDescription Cell(float left,float right,int leftPortal,int rightPortal)
        {
            FloorTriangle T(int[] v,int[] n,int[] p) => new(v,0,n,Vector3.UnitY,n.Select(i=>(byte)(i>=0?1:0)).ToArray(),0,0,p);
            return basis.Floors[0] with
            {
                Vertices=new Vector3[]{new(left,0,-2),new(right,0,-2),new(right,0,2),new(left,0,2)},
                Triangles=new[]{T(new[]{0,1,2},new[]{-1,-1,1},new[]{-1,rightPortal,-1}),T(new[]{0,2,3},new[]{0,-1,-1},new[]{-1,-1,leftPortal})}
            };
        }
        return (layout,new(){[0]=Cell(-2,0,-1,0),[1]=Cell(0,4,0,1),[2]=Cell(4,6,0,-1)},
            new[]{new InteriorDoorBinding(0,0,basis.Leaf),new InteriorDoorBinding(1,1,basis.Leaf)});
    }
    internal static void Run()
    {
        var f=Fixture(); var initial=new CellFloorLocator(0,0,new(-.8f,0,0)); int transfers=0;
        foreach(var owner in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9))})
        {
            using var scene=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,owner);
            var before=scene.State; int samples=0;
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>scene.Tick(.05f,Vector3.Zero,l=>{if(++samples==2)throw new InvalidOperationException("Injected permission failure");return new(true,true);}));
            Require(ReferenceEquals(scene.State,before)&&scene.State.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Permission failure precedes every mutation");
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>scene.Tick(.05f,Vector3.Zero,l=>{scene.Reset();return new(true,true);}));
            Require(ReferenceEquals(scene.State,before),"Reentrant reset rejected atomically");
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>scene.Tick(float.NaN,Vector3.Zero,_=>new(true,true)));
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>scene.Tick(.05f,new(101,0,0),_=>new(true,true)));
            void Go(float x,int cell)
            {
                for(int i=0;i<160&&Math.Abs(scene.State.Locator.LocalPosition.X-x)>1e-5;i++)
                {
                    float delta=Math.Clamp(x-scene.State.Locator.LocalPosition.X,-.08f,.08f);
                    var state=scene.Tick(.05f,owner.TransformDirection(new(delta,0,0)),_=>new(true,true));
                    Require(state.Movement!.Movement.Status!=CellFloorWalkStatus.InvalidStart&&state.Movement.Movement.Transfers<=1,"Reusable one-transfer tick");
                }
                Require(scene.State.Locator.CellIndex==cell&&Math.Abs(scene.State.Locator.LocalPosition.X-x)<1e-5,$"Three-cell route reaches connected owner: desired {x}/{cell}; actual {scene.State.Locator}; movement {scene.State.Movement}; doors {string.Join(',',scene.State.Doors.Select(p=>$"{p.Key}:{p.Value.Controller.Fraction}"))}");
            }
            for(int trip=0;trip<10;trip++){Go(4.8f,2);Go(-.8f,0);}
            Require(before.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Earlier scene snapshot remains immutable");
            Require(scene.State.TotalTransfers==40,"Two leaves and reverse incidences share exact ownership");transfers+=(int)scene.State.TotalTransfers;
            Go(4.8f,2);
            scene.Tick(.05f,Vector3.Zero,_=>new(true,true));
            Require(scene.State.Doors[0].RetainedActors==0,"Leaving both incident cells removes stale retention");
            scene.Reset();Require(scene.State.Frame==0&&scene.State.TotalTransfers==0&&scene.State.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Reset clears every owned controller");
            for(int i=0;i<100;i++)scene.Tick(.05f,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(scene.State.Locator.CellIndex==1&&scene.State.TotalTransfers==1&&scene.State.Movement!.ContactKind==PortalActorContactKind.DoorLeaf&&scene.State.Doors[1].Controller.Fraction==0,"Denied closed second leaf retains owner without opening");
            var held=scene.State.WorldPosition;scene.Tick(.05f,owner.TransformDirection(new(-.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(Vector3.Distance(held,scene.State.WorldPosition)>.07,"Retreat from denied second doorway");
            scene.Reset();Go(4.8f,2);Go(3.2f,1);
            long priorTransfers=scene.State.TotalTransfers;
            for(int i=0;i<20;i++)scene.Tick(0,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(scene.State.Locator.CellIndex==1&&scene.State.TotalTransfers==priorTransfers&&scene.State.Movement!.Movement.Status==CellFloorWalkStatus.EntryDenied,"Denied open second doorway stops at directional barrier");
            scene.Tick(0,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex==1?null:true,true));
            Require(scene.State.Movement!.Movement.Status==CellFloorWalkStatus.PermissionUnknown&&scene.State.TotalTransfers==priorTransfers,"Unknown second doorway permission remains distinct");
            scene.Dispose();Require(scene.State.Doors.Values.All(d=>d.RetainedActors==0&&d.Controller.Fraction==0),"Dispose clears all shared leaves");
            HostAccountingScenarios.Reject<ObjectDisposedException>(()=>scene.Tick(.05f,Vector3.Zero,_=>new(true,true)));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings.Take(1).ToArray(),initial));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,new[]{f.Bindings[0],f.Bindings[0]},initial));
        Require(transfers==80,"Synthetic multi-door transfer accounting");
        int reads=0;TreLookupResult Never(string path){reads++;throw new Exception("Malformed specification must reject before asset lookup.");}
        void RejectSpec(byte[] bytes,string? pin=null)
        {
            string sha=pin??Convert.ToHexString(SHA256.HashData(bytes));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorDoorSceneBundleReader.Load(Never,bytes,sha,new string('0',64)));
        }
        RejectSpec(Array.Empty<byte>());RejectSpec(new byte[16385]);RejectSpec(System.Text.Encoding.UTF8.GetBytes("{}"),new string('0',64));
        foreach(string malformed in new[]{"{","{}","null","[]","{\"member\":\"x\",\"layout_sha256\":\"x\",\"doors\":[]}","{\"member\":\"x\",\"layout_sha256\":\"x\",\"doors\":true}"})RejectSpec(System.Text.Encoding.UTF8.GetBytes(malformed));
        Require(reads==0,"Specification identity/shape/resource rejection precedes every lookup");
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),loadMaterials:true));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),characterAlbedoFactors:true));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),characterFaceValues:new Dictionary<BlueprintCustomizationKey,int>()));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),characterCustomizationValues:new Dictionary<ShaderCustomizationKey,int>()));
        HostAccountingScenarios.Reject<OperationCanceledException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),cancellation:new CancellationToken(true)));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),characterShapeValues:new Dictionary<string,int>()));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64)));
        Require(reads==0,"Content option/specification rejection precedes lookup");
        Console.WriteLine("PASS: bounded three-cell/two-leaf scene, 80 repeated transfers, directional denial/retreat, permission failure/reentrancy, stale retention and reset/dispose");
    }
    private static void Require(bool value,string text){if(!value)throw new Exception("Interior scene: "+text);}
    internal static void Probe()
    {
        var f=Fixture();
        foreach(bool transformed in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            using var scene=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,new(0,0,new(-.8f,0,0)),owner);
            int sequence=0;
            void Phase(string phase,int ticks,float target,bool? secondPermission,bool reset=false)
            {
                if(reset)scene.Reset();
                for(int i=0;i<ticks;i++)
                {
                    float dx=Math.Clamp(target-scene.State.Locator.LocalPosition.X,-.08f,.08f);
                    var delta=owner.TransformDirection(new(dx,0,0));
                    var state=scene.Tick(.05f,delta,l=>new(l.GeometryIndex==1?secondPermission:true,true));
                    Console.WriteLine("interior_scene="+JsonSerializer.Serialize(new{sequence=sequence++,phase,transformed,reset=reset&&i==0,target,second_permission=secondPermission,delta=InteriorDoorScenarios.V(delta),
                        frame=state.Frame,total=state.TotalTransfers,cell=state.Locator.CellIndex,local=InteriorDoorScenarios.V(state.Locator.LocalPosition),world=InteriorDoorScenarios.V(state.WorldPosition),
                        status=state.Movement!.Movement.Status.ToString(),kind=state.Movement.ContactKind?.ToString(),transfers=state.Movement.Movement.Transfers,parameter=state.Movement.Movement.Parameter,
                        doors=state.Doors.OrderBy(p=>p.Key).Select(p=>new{geometry=p.Key,fraction=p.Value.Controller.Fraction,curve=p.Value.DisplacementFraction,retained=p.Value.RetainedActors,transition=(int)p.Value.Controller.Transition}).ToArray()}));
                }
            }
            for(int i=0;i<2;i++){Phase("forward",100,4.8f,true);Phase("reverse",100,-.8f,true);}
            Phase("deny",100,4.8f,false,true);Phase("retreat",10,3.0f,false);Phase("unknown",10,4.8f,null);Phase("recover",100,4.8f,true);Phase("return",100,-.8f,true);
        }
    }
    internal static void ProbeOptional(string[] args,bool closingOnly=false,bool contentBundle=false)
    {
        if(args.Length!=8||args[2]!="--configuration"||args[4]!="--scene-spec"||args[6]!="--expected-sha256")throw new ArgumentException("Original scene requires a pinned bounded specification.");
        if(new FileInfo(args[5]).Length>16384)throw new InvalidDataException("Scene specification byte budget.");
        var bytes=File.ReadAllBytes(args[5]);if(Convert.ToHexString(SHA256.HashData(bytes))!=args[7].ToUpperInvariant())throw new InvalidDataException("Scene specification identity mismatch.");
        using StreamReader cfg=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],cfg);
        var counts=new Dictionary<string,int>();
        TreLookupResult Counted(string path){counts[path]=counts.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
        InteriorSceneContentBundle? content=contentBundle?InteriorSceneContentBundleReader.Load(Counted,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7","appearance/thm_must_mining_outpost.pob","79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818",true,true):null;
        var bundle=content?.Scene??InteriorDoorSceneBundleReader.Load(Counted,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7");
        Require(counts.Values.All(c=>c==1),"Scene bundle resolves each normalized asset once across physical leaves");
        if(content is not null)
        {
            Require(content.Rooms!.CellAppearances.Count==3&&content.Materials!.Meshes.Count==5,"Combined selected room/leaf material count");
            var textureReferences=new Dictionary<string,DdsImage>(StringComparer.Ordinal);
            foreach(var mesh in content.Materials!.Meshes.Values)foreach(var texture in mesh.Textures)
            {
                if(textureReferences.TryGetValue(texture.Key,out var prior))Require(ReferenceEquals(prior,texture.Value),"Shared decoded scene texture identity");else textureReferences.Add(texture.Key,texture.Value);
            }
            Console.WriteLine($"PASS shared_material_templates={content.Materials!.Meshes.Count} unique_decoded_textures={textureReferences.Count}");
            foreach(var source in content.Sources.Values)Console.WriteLine("content_source="+JsonSerializer.Serialize(source));
            Console.WriteLine($"PASS content_sources={content.Sources.Count} external_lookups={counts.Count} all_once=true");
            var substitute=bundle.Sources.Values.First(source=>source.Path!=bundle.LayoutPath);
            HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7",substitute.Path,substitute.Sha256));
            HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7","appearance/thm_must_mining_outpost.pob","79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818",true,true,new(MaximumResources:2)));
            HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7","appearance/thm_must_mining_outpost.pob","79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818",true,true,new(MaximumSourceBytes:1)));
            HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7","appearance/thm_must_mining_outpost.pob","79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818",true,true,maximumDecodedBytes:4));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorDoorSceneBundleReader.Load(overlay.Lookup,bytes,new string('0',64),"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7"));
        void RejectVariant(Action<JsonObject> mutate)
        {
            var variant=JsonNode.Parse(bytes)!.AsObject();mutate(variant);var changed=System.Text.Encoding.UTF8.GetBytes(variant.ToJsonString());
            HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorDoorSceneBundleReader.Load(overlay.Lookup,changed,Convert.ToHexString(SHA256.HashData(changed)),"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7"));
        }
        RejectVariant(v=>v["doors"]![1]=v["doors"]![0]!.DeepClone());
        RejectVariant(v=>v["doors"]![1]!["leaf_sha256"]=new string('0',64));
        var bundles=bundle.Doorways;if(bundles.Count!=2)throw new InvalidDataException("Original route probe requires exactly two explicitly selected leaves.");
        if(bundles[0].TargetCell!=bundles[1].Cell||bundles[0].Cell==bundles[1].TargetCell)throw new InvalidDataException("Route requires three chained cells.");
        var floors=bundle.Floors;var bindings=bundle.Bindings;
        var first=bundles[0].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));var last=bundles[1].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        foreach(var source in bundle.Sources.Values)Console.WriteLine("scene_source="+JsonSerializer.Serialize(source));
        if(closingOnly){ProbeOriginalClosing(bundle);return;}
        foreach(bool transformed in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            using var scene=new InteriorDoorActorScene(bundles[0].Layout,floors,bindings,first.Start,owner,stepPolicy:new(UseLocalRayCoverage:true));int sequence=0;
            int secondGeometry=scene.Links.First(l=>l.SourceCell==bundles[1].Cell&&l.SourcePortal==bundles[1].Portal).GeometryIndex;
            void Phase(string phase,int ticks,Vector3 target,bool? allowed,bool reset=false)
            {
                if(reset)scene.Reset();
                for(int i=0;i<ticks;i++)
                {
                    var local=target-scene.State.Locator.LocalPosition;local.Y=0;float length=local.Length();if(length>.08f)local*=.08f/length;
                    var delta=owner.TransformDirection(local);var state=scene.Tick(.05f,delta,l=>new(l.GeometryIndex==secondGeometry?allowed:true,true));
                    Console.WriteLine("interior_scene="+JsonSerializer.Serialize(new{sequence=sequence++,phase,transformed,reset=reset&&i==0,target=InteriorDoorScenarios.V(target),second_permission=allowed,delta=InteriorDoorScenarios.V(delta),
                        frame=state.Frame,total=state.TotalTransfers,cell=state.Locator.CellIndex,local=InteriorDoorScenarios.V(state.Locator.LocalPosition),world=InteriorDoorScenarios.V(state.WorldPosition),
                        status=state.Movement!.Movement.Status.ToString(),kind=state.Movement.ContactKind?.ToString(),transfers=state.Movement.Movement.Transfers,parameter=state.Movement.Movement.Parameter,steps=state.Movement.Steps,
                        doors=state.Doors.OrderBy(p=>p.Key).Select(p=>new{geometry=p.Key,fraction=p.Value.Controller.Fraction,curve=p.Value.DisplacementFraction,retained=p.Value.RetainedActors,transition=(int)p.Value.Controller.Transition}).ToArray()}));
                }
            }
            for(int i=0;i<2;i++){Phase("forward",200,last.End,true);Phase("reverse",200,first.Start.LocalPosition,true);}
            Phase("deny",200,last.End,false,true);Phase("retreat",20,last.Start.LocalPosition,false);Phase("unknown",20,last.End,null);Phase("recover",200,last.End,true);Phase("return",200,first.Start.LocalPosition,true);
        }
    }
    private static void ProbeOriginalClosing(InteriorDoorSceneBundle bundle)
    {
        var door=bundle.Doorways[1];var approach=door.ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        var direction=approach.End-approach.Start.LocalPosition;direction.Y=0;direction=Vector3.Normalize(direction);
        int geometry=bundle.Layout.Cells[door.Cell].Portals[door.Portal].GeometryIndex;
        int warmTicks=(int)Math.Ceiling(door.Leaf.Selection.Style.OpenSeconds/.05)+2;
        if(warmTicks is <2 or >200)throw new InvalidDataException("Original closing probe warm-up budget.");
        foreach(bool transformed in new[]{false,true})foreach(bool guarded in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            using var scene=bundle.CreateActor(approach.Start,owner,new(UseLocalRayCoverage:true),guarded);int sequence=0;
            void Phase(string phase,int count,float meters,bool allowed)
            {
                for(int i=0;i<count;i++)
                {
                    var delta=owner.TransformDirection(direction*meters);var state=scene.Tick(.05f,delta,l=>new(l.GeometryIndex==geometry?allowed:true,true));var leaf=state.Doors[geometry];
                    Console.WriteLine("scene_closing="+JsonSerializer.Serialize(new{sequence=sequence++,phase,transformed,guarded,allowed,delta=InteriorDoorScenarios.V(delta),frame=state.Frame,cell=state.Locator.CellIndex,total=state.TotalTransfers,
                        local=InteriorDoorScenarios.V(state.Locator.LocalPosition),world=InteriorDoorScenarios.V(state.WorldPosition),status=state.Movement!.Movement.Status.ToString(),kind=state.Movement.ContactKind?.ToString(),parameter=state.Movement.Movement.Parameter,
                        fraction=leaf.Controller.Fraction,curve=leaf.DisplacementFraction,held=leaf.ClosingHeld,retained=leaf.RetainedActors,transition=(int)leaf.Controller.Transition,leaf_position=InteriorDoorScenarios.V(leaf.LeafPose.Translation)}));
                }
            }
            Phase("warm",warmTicks,0,true);Phase("approach",6,.08f,true);Phase("deny-close",80,0,false);Phase("retreat",20,-.08f,false);Phase("settle",60,0,false);
        }
    }
}
