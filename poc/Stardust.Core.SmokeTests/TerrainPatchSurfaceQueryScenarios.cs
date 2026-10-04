using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;
internal static class TerrainPatchSurfaceQueryScenarios
{
    static int checks,negative;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;negative++;return;}throw new Exception("Expected "+typeof(T).Name);}
    static Vector3[] Vectors(JsonElement value)=>value.EnumerateArray().Select(v=>new Vector3(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle())).ToArray();
    static TerrainPatchSurfaceLocation[] Locations(JsonElement value)=>value.EnumerateArray().Select(v=>new TerrainPatchSurfaceLocation(v[0].GetDouble(),v[1].GetDouble())).ToArray();
    static TerrainPatchGeometry Geometry(Vector3[] p,Vector3[] n,int pattern=0)=>new(p,n,Enumerable.Repeat(0xFFFFFFFFU,9).ToArray(),1,pattern);
    static Vector3[] Plane()=>new Vector3[]{new(.5f,3,.5f),new(0,3,.5f),new(0,3,1),new(.5f,3,1),new(1,3,1),new(1,3,.5f),new(1,3,0),new(.5f,3,0),new(0,3,0)};
    static Vector3[] Normals()=>Enumerable.Repeat(new Vector3(0,2,0),9).ToArray();
    internal static void Run(string fixture,string output)
    {
        checks=negative=0;using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();
        foreach(string group in new[]{"Cases","Seams"})foreach(var c in doc.RootElement.GetProperty(group).EnumerateArray())
        {
            var positions=Vectors(c.GetProperty("Positions"));var normals=Vectors(c.GetProperty("Normals"));int pattern=c.GetProperty("Pattern").GetInt32();TerrainPatchGeometry geometry;
            if(group=="Seams")
            {
                int width=c.GetProperty("Width").GetInt32(),height=c.GetProperty("Height").GetInt32();var p=new Vector3[width*height];var n=new Vector3[p.Length];var color=new uint[p.Length];
                for(int row=0;row<height;row++)for(int col=0;col<width;col++){int i=row*width+col;p[i]=new(col/2f,(col-2*row)/16f,row/2f);n[i]=new(-.125f,1,.25f);color[i]=0xFFFFFFFF;}
                var cell=new TerrainPatchCell(c.GetProperty("Count").GetInt32(),c.GetProperty("Column").GetInt32(),c.GetProperty("Row").GetInt32(),c.GetProperty("Border").GetInt32(),c.GetProperty("Mask").GetInt32());var assembly=TerrainPatchAssembly.Assemble(width,height,p,n,color,cell,1);geometry=assembly.Geometry;
                Check(geometry.PatternIndex==pattern&&geometry.CopyPositions().SequenceEqual(positions)&&geometry.CopyNormals().SequenceEqual(normals),"Accepted seam/owned geometry input");Array.Clear(p);Array.Clear(n);Array.Clear(color);
            }
            else geometry=Geometry(positions,normals,pattern);
            var query=new TerrainPatchSurfaceQuery(geometry);var points=Locations(c.GetProperty("Locations"));var hits=query.QueryBatch(points);Check(hits.Count==points.Length&&query.PatternIndex==pattern,"Capped batch cardinality/pattern");
            for(int i=0;i<points.Length;i++)Check(hits[i]==query.Query(points[i].X,points[i].Z),"Batch/scalar deterministic identity");
            var old=hits.ToArray();Array.Clear(positions);Array.Clear(normals);Array.Clear(query.CopyPositions());Array.Clear(query.CopyNormals());Array.Clear(query.CopyIndices());Array.Clear(geometry.CopyPositions());Array.Clear(geometry.CopyNormals());Array.Clear(geometry.CopyIndices());
            Check(query.QueryBatch(points).SequenceEqual(old),"Previous query survives caller/source/inspection alias modification");Array.Clear(points);Check(hits.SequenceEqual(old),"Published hit values independent of caller location mutation");
            records.Add(new{Name=c.GetProperty("Name").GetString(),Pattern=query.PatternIndex,query.TriangleCount,query.LogicalOwnedBytes,Hits=hits});
        }
        var plane=Plane();var normal=Normals();var geo=Geometry(plane,normal);var stable=new TerrainPatchSurfaceQuery(geo);var seed=stable.Query(.5,.5)!.Value;
        Check(seed.TriangleIndex==0&&seed.Height==3&&seed.Normal==new TerrainPatchSurfaceVector(0,1,0),"Exact center tie, flat height and normalized raw normal");Check(stable.Query(-.125,.5) is null&&stable.Query(1.125,.5) is null&&stable.Query(.5,-.125) is null,"Outside returns null");
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,1_000_001d,-1_000_001d}){Reject<ArgumentOutOfRangeException>(()=>stable.Query(bad,0));Reject<ArgumentOutOfRangeException>(()=>stable.Query(0,bad));}
        foreach(var bad in new[]{new TerrainPatchSurfaceQueryLimits(MaximumCoordinateComponent:0),new(MaximumCoordinateComponent:-1),new(MaximumCoordinateComponent:double.NaN),new(MaximumCoordinateComponent:double.PositiveInfinity),new(MaximumCoordinateComponent:1e9+1),new(MinimumProjectedDoubleArea:0),new(MinimumProjectedDoubleArea:-1),new(MinimumProjectedDoubleArea:double.NaN),new(MinimumProjectedDoubleArea:double.PositiveInfinity),new(MinimumProjectedDoubleArea:1e18+1e6),new(MinimumInterpolatedNormalLength:0),new(MinimumInterpolatedNormalLength:-1),new(MinimumInterpolatedNormalLength:double.NaN),new(MinimumInterpolatedNormalLength:double.PositiveInfinity),new(MinimumInterpolatedNormalLength:1e9+1),new(MaximumQueriesPerBatch:0),new(MaximumQueriesPerBatch:65537)})Reject<ArgumentOutOfRangeException>(()=>new TerrainPatchSurfaceQuery(geo,bad));
        Reject<ArgumentNullException>(()=>new TerrainPatchSurfaceQuery(null!));Reject<ArgumentException>(()=>new TerrainPatchSurfaceQuery(geo,new(MaximumCoordinateComponent:2.999)));Check(new TerrainPatchSurfaceQuery(geo,new(MaximumCoordinateComponent:3)).Query(.5,.5)!.Value.Height==3,"Exact source/result coordinate cap");
        Reject<ArgumentException>(()=>new TerrainPatchSurfaceQuery(geo,new(MinimumProjectedDoubleArea:.25)));Check(new TerrainPatchSurfaceQuery(geo,new(MinimumProjectedDoubleArea:Math.BitDecrement(.25))).TriangleCount==8,"Strict projected area threshold");
        var batchQuery=new TerrainPatchSurfaceQuery(geo,new(MaximumQueriesPerBatch:2));Check(batchQuery.QueryBatch(Array.Empty<TerrainPatchSurfaceLocation>()).Count==0&&batchQuery.QueryBatch(new[]{new TerrainPatchSurfaceLocation(0,0),new(1,1)}).Count==2,"Empty/exact capped batch");Reject<ArgumentOutOfRangeException>(()=>batchQuery.QueryBatch(new TerrainPatchSurfaceLocation[3]));Reject<ArgumentOutOfRangeException>(()=>batchQuery.QueryBatch(new[]{new TerrainPatchSurfaceLocation(.5,.5),new(double.NaN,0)}));Check(stable.Query(.5,.5)==seed,"Invalid later location publishes nothing and retains old query");
        var zeroArea=Plane();for(int i=0;i<zeroArea.Length;i++)zeroArea[i].Z=0;Reject<ArgumentException>(()=>new TerrainPatchSurfaceQuery(Geometry(zeroArea,normal)));var vertical=Plane();vertical[2]=new(vertical[1].X,4,vertical[1].Z);Reject<ArgumentException>(()=>new TerrainPatchSurfaceQuery(Geometry(vertical,normal)));var down=Enumerable.Repeat(-Vector3.UnitY,9).ToArray();Reject<ArgumentException>(()=>new TerrainPatchSurfaceQuery(Geometry(Plane(),down)));Check(new TerrainPatchSurfaceQuery(Geometry(Plane(),down),new(RequireCoherentWinding:false)).Query(.5,.5)!.Value.Normal.Y==-1,"Explicit winding bypass does not force upward normals");
        var cancellation=Enumerable.Repeat(Vector3.UnitY,9).ToArray();cancellation[1]=-Vector3.UnitY;var cancel=new TerrainPatchSurfaceQuery(Geometry(Plane(),cancellation));Reject<InvalidDataException>(()=>cancel.Query(.25,.5));Reject<InvalidDataException>(()=>cancel.QueryBatch(new[]{new TerrainPatchSurfaceLocation(.5,.5),new(.25,.5)}));Check(cancel.Query(.5,.5)!.Value.Normal.Y==1&&stable.Query(.5,.5)==seed,"Late normal failure retains all immutable queries");
        var minLength=new TerrainPatchSurfaceQuery(geo,new(MinimumInterpolatedNormalLength:2));Reject<InvalidDataException>(()=>minLength.Query(.5,.5));Check(new TerrainPatchSurfaceQuery(geo,new(MinimumInterpolatedNormalLength:Math.BitDecrement(2d))).Query(.5,.5)!.Value.Normal.Y==1,"Strict normal eligibility threshold");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){var p=Plane();p[0].X=bad;Reject<ArgumentException>(()=>Geometry(p,normal));var n=Normals();n[0].Y=bad;Reject<ArgumentException>(()=>Geometry(plane,n));}
        Exception? threadError=null;TerrainPatchSurfaceHit? threaded=null;Task.Run(()=>{try{threaded=stable.Query(.5,.5);}catch(Exception e){threadError=e;}}).GetAwaiter().GetResult();Check(threadError is null&&threaded==seed,"Immutable query is cross-thread readable without owner resources");
        Check(stable.LogicalOwnedBytes==328&&stable.TriangleCount==8,"Fixed owned array logical budget");File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,NegativeChecks=negative,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R37_TERRAIN_SURFACE_PASS checks={checks} negatives={negative} records={records.Count}");
    }
}
