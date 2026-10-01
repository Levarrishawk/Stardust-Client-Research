using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class OriginalHallCollisionScenarios
{
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256")throw new ArgumentException("Pinned original hall scene required.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        var scene=TexturedPortalSceneReader.Load(overlay.Lookup,args[5],args[7],loadFloors:true,
            customizationMode:CustomizableShaderInspectionMode.AuthoredTextureDefaults,selectedAppearanceCells:new[]{6});
        const string meshPath="appearance/mesh/thm_must_mining_outpost_r6_connecting_hall_mesh_r6.msh";
        if(scene.Materials.Meshes.Count!=1||!scene.Materials.Meshes.TryGetValue(meshPath,out var material))throw new InvalidDataException("Expected representative hall mesh.");
        if(scene.Sources[meshPath].Sha256!="8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B")throw new InvalidDataException("Hall mesh pin mismatch.");
        var flags=material.Materials.ToDictionary(pair=>pair.Key,pair=>MeshPrimitiveCollisionPolicy.FromInvariantCandidates(material.Effects[pair.Value.References.EffectPath]));
        var geometry=StaticMeshCollisionGeometryReader.Create(material.Mesh,flags,1);
        var floor=scene.Scene.CellFloors[6];var walker=new SingleCellFloorTriangleBoxWalker(floor,geometry.Triangles);var floorWalker=new SingleCellFloorWalker(floor);
        Console.WriteLine("hall_geometry="+JsonSerializer.Serialize(new{mesh=meshPath,floor=scene.Scene.Layout.Cells[6].FloorPath,source=geometry.SourceTriangleCount,eligible=geometry.EligibleTriangleCount,degenerate=geometry.DegenerateTriangleCount,triangles=geometry.Triangles.Count,sha256=StaticMeshCollisionVerification.ComputeSha256(geometry)}));
        foreach(var source in scene.Sources.Values)Console.WriteLine("hall_source="+JsonSerializer.Serialize(source));
        const float feetClearance=.0001f;int index=0;
        var directions=new Vector3[]{new(6,0,0),new(-6,0,0),new(0,0,15),new(0,0,-15)};
        for(int ti=0;ti<floor.Triangles.Count;ti++)
        {
            var t=floor.Triangles[ti];if(t.NonSolidByte!=0)continue;
            var a=floor.Vertices[t.VertexIndices[0]];var b=floor.Vertices[t.VertexIndices[1]];var c=floor.Vertices[t.VertexIndices[2]];
            if(Vector3.Cross(b-a,c-a).Y==0)continue;
            Vector3 start=new((float)(((double)a.X+b.X+c.X)/3),(float)(((double)a.Y+b.Y+c.Y)/3),(float)(((double)a.Z+b.Z+c.Z)/3));
            var noOp=floorWalker.Walk(ti,start,start);if(noOp.Status!=FloorWalkStatus.Accepted)throw new InvalidDataException("Original hall centroid floor start.");start=noOp.Position;
            foreach(var direction in directions)
            foreach(Vector3 half in new[]{new Vector3(.25f,.9f,.25f),new Vector3(.65f,.9f,.25f)})
            {
                Vector3 desired=start+direction;var path=floorWalker.Trace(ti,start,desired);var result=walker.Walk(ti,start,desired,half,true,feetClearance:feetClearance);
                var reusable=floorWalker.Walk(result.Floor.TriangleIndex,result.Floor.Position,result.Floor.Position);
                if(reusable.Status!=FloorWalkStatus.Accepted)throw new InvalidDataException("Unusable original triangle-contact floor locator.");
                var repeat=walker.Walk(result.Floor.TriangleIndex,result.Floor.Position,desired,half,true,feetClearance:feetClearance);
                bool stable=repeat.Floor.Position==result.Floor.Position;
                for(int i=1;i<100;i++){repeat=walker.Walk(repeat.Floor.TriangleIndex,repeat.Floor.Position,desired,half,true,feetClearance:feetClearance);stable&=repeat.Floor.Position==result.Floor.Position;}
                var retreat=walker.Walk(result.Floor.TriangleIndex,result.Floor.Position,start,half,true,feetClearance:feetClearance);
                if(!stable||retreat.Status!=FloorBoxWalkStatus.Accepted||Vector3.Distance(retreat.Floor.Position,start)>1e-4)throw new InvalidDataException("Original hall hold/retreat failed.");
                Console.WriteLine("hall_query="+JsonSerializer.Serialize(new{index=index++,triangle=ti,start=InteriorDoorScenarios.V(start),desired=InteriorDoorScenarios.V(desired),half=InteriorDoorScenarios.V(half),feet_clearance=feetClearance,status=result.Status.ToString(),floor_status=result.Floor.Status.ToString(),parameter=result.Floor.Parameter,position=InteriorDoorScenarios.V(result.Floor.Position),landing_triangle=result.Floor.TriangleIndex,diagnostic=result.Diagnostic,reusable=true,repeat_stable=stable,repeat_count=100,retreat_accepted=true,
                    path_parameter=path.Result.Parameter,segments=path.Segments.Select(s=>new{triangle=s.TriangleIndex,first=s.StartParameter,last=s.EndParameter,start=new[]{s.Start.X,s.Start.Y,s.Start.Z},end=new[]{s.End.X,s.End.Y,s.End.Z}}),
                    contact=result.Contact is null?null:new{status=result.Contact.Status.ToString(),parameter=result.Contact.Parameter,segment=result.Contact.SegmentIndex,id=result.Contact.TriangleId,center=new[]{result.Contact.Center.X,result.Contact.Center.Y,result.Contact.Center.Z},normal=InteriorDoorScenarios.V(result.Contact.Normal)}}));
            }
        }
        Console.WriteLine($"PASS original hall supported collision probes={index} triangles={geometry.Triangles.Count}");
    }
}
