using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
internal static class FloorQueryScenarios
{
    internal static void Run()
    {
        FloorMeshDescription floor=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());FloorSurfaceQuery query=new(floor);
        if(query.IntersectSegment(new(1,5,1),new(1,-1,1)) is not null)throw new Exception("Non-solid query policy mismatch.");
        FloorSegmentHit? hit=query.IntersectSegment(new(1,5,1),new(1,-1,1),true);
        if(hit is null || hit.Parameter!=0.5 || hit.Position!=new Vector3(1,2,1) || hit.TriangleId!=17 || hit.GeometricNormal!=new Vector3(0,-1,0))throw new Exception("Floor segment/normal mismatch.");
        if(query.IntersectSegment(new(9,5,9),new(9,-1,9),true) is not null || query.IntersectSegment(new(1,2,1),new(1,2,1),true) is not null)throw new Exception("Floor miss/zero segment mismatch.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>query.IntersectSegment(new(float.NaN,0,0),Vector3.Zero));
        List<Vector3> vertices=new();List<FloorTriangle> triangles=new();
        for(int z=0;z<16;z++)for(int x=0;x<16;x++)
        {
            int index=vertices.Count;vertices.AddRange(new[]{new Vector3(x,3,z),new Vector3(x+1,3,z),new Vector3(x,3,z+1),new Vector3(x+1,3,z+1)});
            FloorTriangle t=floor.Triangles[0] with{NonSolidByte=0,Id=(uint)triangles.Count,VertexIndices=new[]{index,index+1,index+2}};triangles.Add(t);triangles.Add(t with{Id=(uint)triangles.Count,VertexIndices=new[]{index+1,index+3,index+2}});
        }
        FloorSurfaceQuery grid=new(floor with{Vertices=vertices,Triangles=triangles});if(grid.NodeCount<=1)throw new Exception("Floor acceleration tree was not split.");
        Random random=new(20260930);
        for(int i=0;i<1000;i++)
        {
            float x=(float)(random.NextDouble()*16),z=(float)(random.NextDouble()*16);int ordinal=((int)z*16+(int)x)*2+(x-(int)x+z-(int)z>1?1:0);
            hit=grid.IntersectSegment(new(x,10,z),new(x,-10,z));
            if(hit is null || hit.TriangleIndex!=ordinal || Math.Abs(hit.Parameter-0.35)>1e-12 || hit.Position!=new Vector3(x,3,z))throw new Exception("Accelerated grid/analytic query disagreement.");
        }
        vertices.Clear();triangles.Clear();if(grid.IntersectSegment(new(0.2f,10,0.3f),new(0.2f,-10,0.3f)) is null)throw new Exception("Floor query borrowed caller geometry.");
        FloorTriangle solid=floor.Triangles[0] with{NonSolidByte=0};query=new(floor with{Triangles=new[]{solid,solid}});
        if(query.IntersectSegment(new(1,5,1),new(1,-1,1))?.TriangleIndex!=0)throw new Exception("Coincident triangle tie was nondeterministic.");
        query=new(floor with{Triangles=Enumerable.Repeat(solid,20).ToArray()});
        if(query.IntersectSegment(new(1,5,1),new(1,-1,1))?.TriangleIndex!=0)throw new Exception("Cross-leaf coincident tie mismatch.");
        Vector3[] layered=floor.Vertices.Concat(floor.Vertices.Select(p=>p+new Vector3(0,2,0))).ToArray();
        query=new(floor with{Vertices=layered,Triangles=new[]{solid,solid with{Id=18,VertexIndices=new[]{3,4,5}}}});
        if(query.IntersectSegment(new(1,5,1),new(1,-1,1))?.TriangleId!=18 || query.IntersectSegment(new(1,-1,1),new(1,5,1))?.TriangleId!=17 ||
            query.IntersectSegment(new(1,2,1),new(1,1,1))?.Parameter!=0 || query.IntersectSegment(new(1,1,1),new(1,2,1))?.Parameter!=1 ||
            query.IntersectSegment(new(1,2,1),new(2,2,1)) is not null)throw new Exception("Closest reverse/endpoint/coplanar segment policy mismatch.");
        query=new(floor with{Triangles=Array.Empty<FloorTriangle>()});if(query.IntersectSegment(Vector3.Zero,Vector3.One) is not null)throw new Exception("Empty floor query mismatch.");
        query=new(floor with{Triangles=new[]{solid with{VertexIndices=new[]{0,0,0}}}});if(query.IntersectSegment(new(0,5,0),new(0,-1,0)) is not null)throw new Exception("Degenerate triangle query mismatch.");
        Console.WriteLine("PASS: owned deterministic floor BVH, analytic seeded queries, closest two-sided segments, non-solid policy, stable ties and finite guards");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--fixtures" || args[6]!="--expected-sha256")throw new ArgumentException("Floor queries need pinned fixture/configuration.");
        if(new FileInfo(args[5]).Length>8_388_608)throw new InvalidDataException("Floor query fixture budget.");byte[] input=File.ReadAllBytes(args[5]);
        if(!Convert.ToHexString(SHA256.HashData(input)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Floor query fixture identity mismatch.");
        using JsonDocument document=JsonDocument.Parse(input);JsonElement fixtures=document.RootElement;if(fixtures.ValueKind!=JsonValueKind.Array || fixtures.GetArrayLength() is 0 or >10000)throw new InvalidDataException("Floor query fixture count.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);int count=0;
        Vector3 Vector(JsonElement value){if(value.GetArrayLength()!=3)throw new InvalidDataException("Query coordinate width.");return new(value[0].GetSingle(),value[1].GetSingle(),value[2].GetSingle());}
        foreach(JsonElement fixture in fixtures.EnumerateArray())
        {
            string member=fixture.GetProperty("member").GetString()!;TreLookupResult source=overlay.Lookup(member);
            if(source.Kind!=TreLookupKind.Present || source.Payload is null || Convert.ToHexString(SHA256.HashData(source.Payload))!=fixture.GetProperty("sha256").GetString())throw new InvalidDataException("Floor query source identity mismatch.");
            FloorSurfaceQuery query=new(FloorMeshDecoder.Decode(source.Payload));int index=0;
            foreach(JsonElement probe in fixture.GetProperty("queries").EnumerateArray())
            {
                if(++count>50000)throw new InvalidDataException("Floor query aggregate budget.");
                FloorSegmentHit? h=query.IntersectSegment(Vector(probe.GetProperty("start")),Vector(probe.GetProperty("end")),probe.GetProperty("include_non_solid").GetBoolean());
                Console.WriteLine("query="+JsonSerializer.Serialize(new{member,index=index++,hit=h is null?null:new{triangle=h.TriangleIndex,id=h.TriangleId,parameter=h.Parameter,position=new[]{h.Position.X,h.Position.Y,h.Position.Z}}}));
            }
        }
    }
}
