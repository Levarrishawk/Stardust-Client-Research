using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

internal static class TerrainPatchGeometryScenarios
{
    private sealed record Fixture(string Name,int Pattern,uint ScaleBits,uint[][] Positions,uint[][] Normals,uint[] Colors,int[] Selectors);
    private static int checks, negatives;
    internal static void Run(string input,string output)
    {
        checks=negatives=0;var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(input))!;
        var rows=new List<object>();
        foreach(var f in fixtures)
        {
            var p=f.Positions.Select(V).ToArray();var n=f.Normals.Select(V).ToArray();var c=(uint[])f.Colors.Clone();var s=(int[])f.Selectors.Clone();
            var g=new TerrainPatchGeometry(p,n,c,BitConverter.UInt32BitsToSingle(f.ScaleBits),f.Pattern,s);
            object Project()=>new {f.Name,Pattern=g.PatternIndex,ScaleBits=BitConverter.SingleToUInt32Bits(g.UvScale),
                Positions=g.CopyPositions().Select(B).ToArray(),Normals=g.CopyNormals().Select(B).ToArray(),Colors=g.CopyArgbColors(),
                Rgba=g.CopyRgbaColors().Select(v=>new[]{F(v.X),F(v.Y),F(v.Z),F(v.W)}).ToArray(),
                BaseUvs=g.CopyBaseUvs().Select(v=>new[]{F(v.X),F(v.Y)}).ToArray(),
                Selectors=g.CopyAuxiliarySelectors(),Auxiliary=Enumerable.Range(0,g.AuxiliarySetCount).Select(i=>g.CopyAuxiliaryUvs(i).Select(v=>new[]{F(v.X),F(v.Y)}).ToArray()).ToArray(),
                Indices=g.CopyIndices(),Minimum=B(g.Minimum),Maximum=B(g.Maximum),g.TriangleCount};
            string before=JsonSerializer.Serialize(Project());
            p[0]=new(999,999,999);n[0]=Vector3.Zero;c[0]^=uint.MaxValue;if(s.Length>0)s[0]=(s[0]+1)%4;
            Check(before==JsonSerializer.Serialize(Project()),"Input ownership");
            var cp=g.CopyPositions();cp[0]=Vector3.Zero;var cn=g.CopyNormals();cn[0]=Vector3.Zero;var cc=g.CopyArgbColors();cc[0]=0;
            var cr=g.CopyRgbaColors();cr[0]=Vector4.Zero;var cu=g.CopyBaseUvs();cu[0]=Vector2.Zero;var ci=g.CopyIndices();ci[0]=ushort.MaxValue;
            var cs=g.CopyAuxiliarySelectors();if(cs.Length>0)cs[0]=99;for(int i=0;i<g.AuxiliarySetCount;i++){var a=g.CopyAuxiliaryUvs(i);a[0]=Vector2.Zero;}
            Check(before==JsonSerializer.Serialize(Project()),"Output ownership");
            Expect<ArgumentOutOfRangeException>(()=>g.CopyAuxiliaryUvs(-1));
            Expect<ArgumentOutOfRangeException>(()=>g.CopyAuxiliaryUvs(g.AuxiliarySetCount));
            rows.Add(Project());
        }
        var positions=fixtures[0].Positions.Select(V).ToArray();var normals=fixtures[0].Normals.Select(V).ToArray();var colors=fixtures[0].Colors;
        foreach(int length in new[]{0,8,10})
        {
            Expect<ArgumentException>(()=>new TerrainPatchGeometry(new Vector3[length],normals,colors,1,0));
            Expect<ArgumentException>(()=>new TerrainPatchGeometry(positions,new Vector3[length],colors,1,0));
            Expect<ArgumentException>(()=>new TerrainPatchGeometry(positions,normals,new uint[length],1,0));
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,0,float.MaxValue})
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,invalid,0));
        foreach(int invalid in new[]{-1,9,int.MaxValue,int.MinValue})
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,invalid));
        foreach(int selector in new[]{-1,4,int.MaxValue})
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,0,new[]{selector}));
        Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,0,new[]{0,1,2,3}));
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,float.MaxValue,1_000_001f})
        {
            var bad=(Vector3[])positions.Clone();bad[4]=new(invalid,0,0);
            Expect<ArgumentException>(()=>new TerrainPatchGeometry(bad,normals,colors,1,0));
        }
        foreach(var invalid in new[]{Vector3.Zero,new Vector3(1e-8f,0,0),new Vector3(float.NaN,0,0),new Vector3(4097,0,0),new Vector3(float.MaxValue,0,0)})
        {
            var bad=(Vector3[])normals.Clone();bad[4]=invalid;
            Expect<ArgumentException>(()=>new TerrainPatchGeometry(positions,bad,colors,1,0));
        }
        foreach(float invalid in new[]{0f,-1,float.NaN,float.PositiveInfinity,float.MaxValue})
        {
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,0,limits:new(invalid,4096,65536)));
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,0,limits:new(1_000_000,invalid,65536)));
            Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,1,0,limits:new(1_000_000,4096,invalid)));
        }
        Expect<ArgumentOutOfRangeException>(()=>new TerrainPatchGeometry(positions,normals,colors,2,0,limits:new(1_000_000,4096,1)));
        var exact=new TerrainPatchGeometry(positions,normals,colors,65536,0,limits:new(1_000_000,4096,65536));
        Check(exact.CopyBaseUvs().Max(v=>v.X)==65536,"Exact scale cap");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Negatives=negatives,Cases=fixtures.Length,SyntheticHeights=true,Results=rows},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R32_GEOMETRY_PASS checks={checks} negatives={negatives} cases={fixtures.Length}");
    }
    private static uint F(float value)=>BitConverter.SingleToUInt32Bits(value);
    private static Vector3 V(uint[] b)=>new(BitConverter.UInt32BitsToSingle(b[0]),BitConverter.UInt32BitsToSingle(b[1]),BitConverter.UInt32BitsToSingle(b[2]));
    private static uint[] B(Vector3 v)=>[F(v.X),F(v.Y),F(v.Z)];
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    private static void Expect<T>(Action action)where T:Exception{checks++;negatives++;try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
