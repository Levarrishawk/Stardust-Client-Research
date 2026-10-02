using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TangentLightingScenarios
{
    internal static void Run()
    {
        var v=TangentLightingVertexEvaluator.Evaluate(Vector3.Zero,Vector3.UnitZ,new(1,0,0,1),Vector3.UnitY,Vector3.UnitZ);
        if(v.Light!=Vector3.UnitY||Vector3.Distance(v.HalfAngle,new(0,MathF.Sqrt(.5f),MathF.Sqrt(.5f)))>1e-6)throw new Exception("Hand-computed tangent L/H projection failed.");
        var flipped=TangentLightingVertexEvaluator.Evaluate(Vector3.Zero,Vector3.UnitZ,new(1,0,0,-1),Vector3.UnitY,Vector3.UnitZ);if(flipped.Light!=-Vector3.UnitY||Math.Abs(flipped.HalfAngle.Y+v.HalfAngle.Y)>1e-6)throw new Exception("Source tangent handedness projection failed.");
        var raw=TangentLightingVertexEvaluator.Evaluate(Vector3.Zero,Vector3.UnitZ,new(2,0,0,1),Vector3.UnitX,Vector3.UnitZ);if(raw.Light.X!=2)throw new Exception("Source tangent was silently normalized.");
        var zero=TangentLightingVertexEvaluator.Evaluate(Vector3.Zero,Vector3.UnitZ,new(0,0,0,1),Vector3.UnitY,Vector3.UnitZ);if(zero.Light!=Vector3.Zero||zero.HalfAngle.X!=0||zero.HalfAngle.Y!=0)throw new Exception("Original zero tangent was silently repaired.");
        var vertices=new[]{new TangentLightingVaryings(Vector3.UnitX,Vector3.UnitY),new TangentLightingVaryings(Vector3.UnitY,Vector3.UnitZ),new TangentLightingVaryings(Vector3.UnitZ,Vector3.UnitX)};
        var interpolated=TangentLightingVertexEvaluator.Interpolate(vertices,new(.2f,.3f,.5f),new(1,2,4));var expected=new Vector3(.2f,.15f,.125f)/.475f;if(Vector3.Distance(interpolated.Light,expected)>1e-6)throw new Exception("Hand perspective-correct unnormalized varying interpolation failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>TangentLightingVertexEvaluator.Interpolate(vertices,new(.2f,.3f,.5f),new(1,0,4)));HostAccountingScenarios.Reject<InvalidDataException>(()=>TangentLightingVertexEvaluator.Interpolate(vertices,new(-.2f,.7f,.5f),Vector3.One));HostAccountingScenarios.Reject<InvalidDataException>(()=>TangentLightingVertexEvaluator.Evaluate(Vector3.Zero,Vector3.UnitZ,new(float.NaN,0,0,1),Vector3.UnitY,Vector3.UnitZ));
        Console.WriteLine("PASS: source tangent light/half-angle handedness/raw scale/zero preservation and perspective-weight/finite negative gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};int cases=0;var bundle=content.StaticNormals!;
        Console.WriteLine("original_tangent_vertex_functions="+JsonSerializer.Serialize(new{path="vertex_program/include/functions.inc",sha256=bundle.VertexFunctionsSha256,vertexSources=bundle.VertexSources!.Sha256}));
        foreach(var pair in content.Materials!.Meshes.OrderBy(p=>p.Key,StringComparer.Ordinal))foreach(var primitive in pair.Value.Mesh.Primitives)
        {
            if(!bundle.Materials.TryGetValue(primitive.ShaderPath,out var description))continue;
            for(int variant=0;variant<6;variant++)for(int index=0;index<primitive.Vertices.Count;index++)
            {
                var light=variant%3==0?Vector3.UnitZ:variant%3==1?Vector3.UnitY:Vector3.Normalize(new Vector3(1,2,3));var camera=variant<3?new Vector3(0,1.7f,-3):new Vector3(2,3,5);var vertex=primitive.Vertices[index];var tangent=StaticNormalMaterialReader.ReadTangent(vertex,description.TangentCoordinateSet);var value=TangentLightingVertexEvaluator.Evaluate(vertex.Position,vertex.Normal,tangent,light,camera);
                Console.WriteLine("original_tangent_vertex_case="+JsonSerializer.Serialize(new{path=pair.Key,variant,index,light=V(value.Light),half=V(value.HalfAngle)}));cases++;
            }
        }
        if(cases!=660)throw new Exception("Original tangent-varying denominator changed.");Console.WriteLine($"PASS: original static door tangent varying vertex cases={cases} raw_zero_tangents_preserved=true");
    }
}
