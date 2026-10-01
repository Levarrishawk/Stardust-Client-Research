using Stardust.Core.Assets;

internal static class MeshCollisionPolicyScenarios
{
    internal static void Run()
    {
        var candidate = StaticEffectDecoder.Decode(MaterialScenarios.Effect()).Implementations[0];
        var first = candidate.Passes[0];
        StaticEffectImplementation Make(string version, uint blend, uint alpha, byte? flag, bool empty = false)
        {
            var fields = first.Fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            fields[0x20] = blend; fields[0x30] = alpha;
            return candidate with { Version = version, Flag6 = flag, Passes = empty ? Array.Empty<StaticEffectPass>() : new[] { first with { Fields = fields } } };
        }
        foreach (var test in new[]
        {
            (Make("0005",0,0,0),new MeshPrimitiveCollisionFlags(true,false)),
            (Make("0005",1,0,1),new MeshPrimitiveCollisionFlags(false,true)),
            (Make("0006",0,1,0),new MeshPrimitiveCollisionFlags(false,false)),
            (Make("0007",0,0,1),new MeshPrimitiveCollisionFlags(true,true)),
            (Make("0005",0,0,1,true),new MeshPrimitiveCollisionFlags(false,true)),
            (Make("0004",0,0,null),new MeshPrimitiveCollisionFlags(true,true)),
            (Make("0004",1,0,null),new MeshPrimitiveCollisionFlags(false,false)),
            (Make("0004",0,0,null,true),new MeshPrimitiveCollisionFlags(false,false))
        })
        {
            var flags=MeshPrimitiveCollisionPolicy.FromSelectedImplementation(test.Item1);
            if(flags!=test.Item2 || flags.AcceptsSelector(0)!=test.Item2.OpaqueFirstPass || flags.AcceptsSelector(1)!=test.Item2.SelectorOne || flags.AcceptsSelector(2) || flags.AcceptsSelector(256))
                throw new Exception("Recovered mesh collision selector truth table failed.");
        }
        if(MeshPrimitiveCollisionPolicy.FromSelectedImplementation(null).RequiresCollisionData)throw new Exception("Absent selected implementation accepted.");
        var agreed=StaticEffectDecoder.Decode(MaterialScenarios.Effect());
        if(!MeshPrimitiveCollisionPolicy.FromInvariantCandidates(agreed).SelectorOne)throw new Exception("Invariant effect candidate flags lost.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>MeshPrimitiveCollisionPolicy.FromInvariantCandidates(agreed with{Implementations=new[]{Make("0005",0,0,1),Make("0005",1,0,0)}}));
        var mesh=StaticMeshDecoder.Decode(MeshScenarios.Mesh("0000"));
        var shader=mesh.Primitives[0].ShaderPath;var geometry=StaticMeshCollisionGeometryReader.Create(mesh,new Dictionary<string,MeshPrimitiveCollisionFlags>{{shader,new(true,true)}},1);
        if(geometry.SourceTriangleCount!=1||geometry.EligibleTriangleCount!=1||geometry.Triangles.Count!=1||geometry.Triangles[0].Id!=0)throw new Exception("Indexed collision geometry lost source identity.");
        var degenerate=mesh with{Primitives=new[]{mesh.Primitives[0] with{Indices=new ushort[]{0,0,0}}}};
        if(StaticMeshCollisionGeometryReader.Create(degenerate,new Dictionary<string,MeshPrimitiveCollisionFlags>{{shader,new(true,true)}},1).DegenerateTriangleCount!=1)throw new Exception("Exact collision degeneracy accounting lost.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticMeshCollisionGeometryReader.Create(mesh,new Dictionary<string,MeshPrimitiveCollisionFlags>(),1));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticMeshCollisionGeometryReader.Create(mesh,new Dictionary<string,MeshPrimitiveCollisionFlags>{{shader,new(true,true)}},1,0));
        // Only the first pass contributes; a later opaque pass cannot change the result.
        var laterOpaque=Make("0007",1,0,0) with{Passes=new[]{Make("0007",1,0,0).Passes[0],Make("0007",0,0,0).Passes[0]}};
        if(MeshPrimitiveCollisionPolicy.FromSelectedImplementation(laterOpaque).RequiresCollisionData)throw new Exception("Later pass changed collision predicate.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>MeshPrimitiveCollisionPolicy.FromSelectedImplementation(Make("0008",0,0,1)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>MeshPrimitiveCollisionPolicy.FromSelectedImplementation(Make("0005",0,0,null)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>MeshPrimitiveCollisionPolicy.FromSelectedImplementation(Make("0005",2,0,1)));
        Console.WriteLine("PASS: recovered selected-implementation collision predicates, zero-pass helpers, older derivation and rejected selectors");
    }
}
