using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class OriginalPortalTriangleScenarios
{
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256")throw new ArgumentException("Pinned original portal collision scene required.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        var scene=TexturedPortalSceneReader.Load(overlay.Lookup,args[5],args[7],loadFloors:true,customizationMode:CustomizableShaderInspectionMode.AuthoredTextureDefaults,selectedAppearanceCells:new[]{5,6,7});
        var paths=new Dictionary<int,string>{{5,"appearance/mesh/thm_must_mining_outpost_r5_entrance_room_01_msh_r5.msh"},{6,"appearance/mesh/thm_must_mining_outpost_r6_connecting_hall_mesh_r6.msh"},{7,"appearance/mesh/thm_must_mining_outpost_r7_entrance_room_02_mesh_r7.msh"}};
        var pins=new Dictionary<int,string>{{5,"425D589385176D77E9F7D29508F4674D98636E99389B8E9CB2CCBA4E022F6EC2"},{6,"8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B"},{7,"2A386A135FAF9F749955F5E054C0A2A3F2FB83DB43EE4CDDBCF967BAA5A727E2"}};
        var triangles=new Dictionary<int,IReadOnlyList<CollisionTriangle>>();var floors=paths.Keys.ToDictionary(cell=>cell,cell=>scene.Scene.CellFloors[cell]);
        foreach(var pair in paths)
        {
            var material=scene.Materials.Meshes[pair.Value];if(scene.Sources[pair.Value].Sha256!=pins[pair.Key])throw new InvalidDataException("Original room collision mesh pin.");
            var flags=material.Materials.ToDictionary(m=>m.Key,m=>MeshPrimitiveCollisionPolicy.FromInvariantCandidates(material.Effects[m.Value.References.EffectPath]));
            var geometry=StaticMeshCollisionGeometryReader.Create(material.Mesh,flags,1,65536);triangles.Add(pair.Key,geometry.Triangles);
            Console.WriteLine("scene_collision_geometry="+JsonSerializer.Serialize(new{cell=pair.Key,mesh=pair.Value,floor=scene.Scene.Layout.Cells[pair.Key].FloorPath,source=geometry.SourceTriangleCount,eligible=geometry.EligibleTriangleCount,degenerate=geometry.DegenerateTriangleCount,triangles=geometry.Triangles.Count,sha256=StaticMeshCollisionVerification.ComputeSha256(geometry)}));
        }
        foreach(var source in scene.Sources.Values)Console.WriteLine("scene_collision_source="+JsonSerializer.Serialize(source));
        int index=0;Vector3 half=new(.25f,.9f,.25f);const float clearance=.0001f;
        foreach(var pose in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-30))})
        {
            var poses=paths.Keys.ToDictionary(cell=>cell,_=>pose);var walker=new PortalFloorTriangleBoxWalker(scene.Scene.Layout,floors,triangles,poses,65536);
            var floorTrace=new PortalFloorTransferQuery(scene.Scene.Layout,floors,poses);
            foreach(float destination in new[]{58.5f,58f,70.5f,71f})
            {
                var hit=new FloorSurfaceQuery(floors[6]).IntersectSegment(new(-28.349918f,20,64.5f),new(-28.349918f,16,64.5f))??throw new InvalidDataException("Original hall route start.");
                var initial=new CellFloorLocator(6,hit.TriangleIndex,hit.Position);var desired=pose.TransformPoint(new(-28.349918f,17.769766f,destination));
                var result=walker.Walk(initial,desired,half,_=>new(true,true),true,feetClearance:clearance);
                var path=floorTrace.Trace(initial,desired,_=>new(true,true));
                var returned=walker.Walk(result.Movement.Locator,result.Movement.WorldPosition,half,_=>new(true,true),true,feetClearance:clearance);
                if(returned.Movement.Locator.CellIndex!=result.Movement.Locator.CellIndex)throw new InvalidDataException("Original triangle portal locator reuse.");
                var reverse=walker.Walk(result.Movement.Locator,pose.TransformPoint(initial.LocalPosition),half,_=>new(true,true),true,feetClearance:clearance);
                var repeated=result;bool stable=true;
                if(result.Contact is not null)for(int i=0;i<100;i++){repeated=walker.Walk(repeated.Movement.Locator,desired,half,_=>new(true,true),true,feetClearance:clearance);stable&=repeated.Movement.Locator==result.Movement.Locator;}
                if(!stable||reverse.Status!=FloorBoxWalkStatus.Accepted||reverse.Movement.Locator.CellIndex!=6||reverse.Movement.Transfers!=1||Vector3.Distance(reverse.Movement.Locator.LocalPosition,initial.LocalPosition)>1e-4)throw new InvalidDataException("Original complete-scene repeat/retreat.");
                Console.WriteLine("scene_collision_query="+JsonSerializer.Serialize(new{index=index++,start_cell=6,start=InteriorDoorScenarios.V(initial.LocalPosition),desired=InteriorDoorScenarios.V(desired),half=InteriorDoorScenarios.V(half),feet_clearance=clearance,pose=new[]{InteriorDoorScenarios.V(pose.RowX),InteriorDoorScenarios.V(pose.RowY),InteriorDoorScenarios.V(pose.RowZ),InteriorDoorScenarios.V(pose.Translation)},status=result.Status.ToString(),movement=result.Movement.Status.ToString(),cell=result.Movement.Locator.CellIndex,triangle=result.Movement.Locator.TriangleIndex,position=InteriorDoorScenarios.V(result.Movement.Locator.LocalPosition),world=InteriorDoorScenarios.V(result.Movement.WorldPosition),parameter=result.Movement.Parameter,transfers=result.Movement.Transfers,diagnostic=result.Diagnostic,reverse_status=reverse.Status.ToString(),reverse_cell=reverse.Movement.Locator.CellIndex,reverse_transfers=reverse.Movement.Transfers,reverse_position=InteriorDoorScenarios.V(reverse.Movement.Locator.LocalPosition),repeat_stable=stable,repeat_count=result.Contact is null?0:100,
                    floors=path.Floors.Select(part=>new{cell=part.CellIndex,parameter=part.Path.Result.Parameter,segments=part.Path.Segments.Select(s=>new{triangle=s.TriangleIndex,first=s.StartParameter,last=s.EndParameter,start=new[]{s.Start.X,s.Start.Y,s.Start.Z},end=new[]{s.End.X,s.End.Y,s.End.Z}})}),
                    contact=result.Contact is null?null:new{cell=result.Contact.ColliderCell,path_cell=result.Contact.PathCell,id=result.Contact.World.TriangleId,parameter=result.Contact.World.Parameter,status=result.Contact.World.Status.ToString(),center=new[]{result.Contact.World.Center.X,result.Contact.World.Center.Y,result.Contact.World.Center.Z}}}));
            }
            var all=triangles.SelectMany(pair=>pair.Value.Select(t=>t with{Id=pair.Key*65536+t.Id,A=pose.TransformPoint(t.A),B=pose.TransformPoint(t.B),C=pose.TransformPoint(t.C)})).ToArray();
            var accelerated=new StaticTriangleBoxCollider(all,65536);var exhaustive=new StaticTriangleBoxCollider(all,65536,false);long candidates=0;int contacts=0;
            for(int q=0;q<32;q++)
            {
                Vector3 a=pose.TransformPoint(new(-28.35f+(q%4-1.5f)*.3f,18.67f,60+q/4));
                Vector3 b=pose.TransformPoint(new(-28.35f+(q%4-1.5f)*2,18.67f,60+q/4));
                var actual=accelerated.SweepWithStatistics(a,b,half,true);var expected=exhaustive.Sweep(a,b,half,true);
                if(actual.Contact!=expected)throw new InvalidDataException("Original complete-scene bounds/exhaustive disagreement.");
                candidates+=actual.Statistics.CandidateTriangles;if(actual.Contact.Status!=TriangleBoxContactStatus.Clear)contacts++;
            }
            Console.WriteLine($"scene_collision_bounds queries=32 source={all.Length} candidates={candidates} exhaustive={all.Length*32L} contacts={contacts}");
        }
        Console.WriteLine($"PASS original complete triangle scene queries={index} triangles={triangles.Values.Sum(v=>v.Count)}");
    }
}
