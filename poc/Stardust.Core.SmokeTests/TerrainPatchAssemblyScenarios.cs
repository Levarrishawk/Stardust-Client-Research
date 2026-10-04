using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

internal static class TerrainPatchAssemblyScenarios
{
    private sealed record Fixture(int CellsPerSide,int Column,int Row,int SampleBorder,int SeamMask,int Width,int Height,uint ScaleBits,int[] Selectors);
    private sealed record Selection(int CellsPerSide,int Column,int Row,int SeamMask,int Pattern);
    private sealed record Inputs(Fixture[] Cases,Selection[] Selections);
    private static int checks,negatives;
    private static uint[] Bits(Vector3 v)=>[BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)];
    private static uint[] Bits(Vector2 v)=>[BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y)];
    internal static void Run(string input,string output)
    {
        checks=negatives=0;var fixtures=JsonSerializer.Deserialize<Inputs>(File.ReadAllText(input))!;var rows=new List<object>();var selections=new List<object>();
        foreach(var f in fixtures.Cases)
        {
            var (p,n,c)=Grid(f.Width,f.Height);var selector=(int[])f.Selectors.Clone();
            var result=TerrainPatchAssembly.Assemble(f.Width,f.Height,p,n,c,new(f.CellsPerSide,f.Column,f.Row,f.SampleBorder,f.SeamMask),
                BitConverter.UInt32BitsToSingle(f.ScaleBits),selector);
            var g=result.Geometry;
            object Project()=>new {f.CellsPerSide,f.Column,f.Row,f.SampleBorder,f.SeamMask,f.Width,f.Height,f.ScaleBits,Selectors=g.CopyAuxiliarySelectors(),
                Pattern=g.PatternIndex,SampleIndices=result.CopySourceSampleIndices(),Positions=g.CopyPositions().Select(Bits).ToArray(),
                Normals=g.CopyNormals().Select(Bits).ToArray(),Colors=g.CopyArgbColors(),BaseUvs=g.CopyBaseUvs().Select(Bits).ToArray(),
                Auxiliary=Enumerable.Range(0,g.AuxiliarySetCount).Select(i=>g.CopyAuxiliaryUvs(i).Select(Bits).ToArray()).ToArray(),Indices=g.CopyIndices()};
            string before=JsonSerializer.Serialize(Project());
            Check(result.Cell==new TerrainPatchCell(f.CellsPerSide,f.Column,f.Row,f.SampleBorder,f.SeamMask)&&result.GridWidth==f.Width&&result.GridHeight==f.Height,"Immutable receipt metadata");
            int index=result.CopySourceSampleIndices()[0];p[index]=new(float.NaN,0,0);n[index]=Vector3.Zero;c[index]^=uint.MaxValue;
            if(selector.Length>0)selector[0]=99;
            Check(before==JsonSerializer.Serialize(Project()),"Selected input ownership");
            var ids=result.CopySourceSampleIndices();ids[0]=int.MaxValue;var positions=g.CopyPositions();positions[0]=Vector3.Zero;
            Check(before==JsonSerializer.Serialize(Project()),"Output ownership");
            var actual=g.CopyPositions();var normals=g.CopyNormals();var ix=g.CopyIndices();
            for(int i=0;i<ix.Length;i+=3)Check(Vector3.Dot(Vector3.Cross(actual[ix[i+1]]-actual[ix[i]],actual[ix[i+2]]-actual[ix[i]]),normals[ix[i]])>0,"Declared synthetic winding");
            rows.Add(Project());
        }
        foreach(var s in fixtures.Selections)
        {
            int pattern=TerrainPatchAssembly.SelectPattern(s.CellsPerSide,s.Column,s.Row,s.SeamMask);
            Check(pattern==s.Pattern,"Large-count selector");
            selections.Add(new{s.CellsPerSide,s.Column,s.Row,s.SeamMask,Pattern=pattern});
        }
        foreach(int count in new[]{0,-1,int.MinValue})Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.SelectPattern(count,0,0,0));
        foreach(int mask in new[]{-1,16,255,int.MaxValue})Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.SelectPattern(2,0,0,mask));
        foreach(int coordinate in new[]{-1,2,int.MaxValue,int.MinValue})
        {Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.SelectPattern(2,coordinate,0,0));Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.SelectPattern(2,0,coordinate,0));}
        var (gp,gn,gc)=Grid(5,5);TerrainPatchCell cell=new(2,0,0,0,0);
        var retained=TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1);var retainedIds=retained.CopySourceSampleIndices();var retainedPositions=retained.Geometry.CopyPositions();
        foreach(int size in new[]{0,24,26})
        {Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,new Vector3[size],gn,gc,cell,1));Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,new Vector3[size],gc,cell,1));Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,new uint[size],cell,1));}
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(0,5,gp,gn,gc,cell,1));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,-1,gp,gn,gc,cell,1));
        Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(4,5,gp,gn,gc,cell,1));
        Expect<OverflowException>(()=>TerrainPatchAssembly.Assemble(int.MaxValue,2,gp,gn,gc,cell,1));
        Expect<OverflowException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,new(int.MaxValue,0,0,0,0),1));
        Expect<OverflowException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,new(2,0,0,int.MaxValue,0),1));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,new(2,0,0,-1,0),1));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1,limits:new(24)));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1,limits:new(0)));
        Check(TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1,limits:new(25)).Geometry.TriangleCount==8,"Exact sample cap");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,float.MaxValue,1_000_001f})
        {var bad=(Vector3[])gp.Clone();bad[retainedIds[0]]=new(invalid,0,0);Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,bad,gn,gc,cell,1));}
        var zeroNormals=new Vector3[25];Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,zeroNormals,gc,cell,1));
        Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,new Vector3[25],gn,gc,cell,1));
        Expect<ArgumentException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn.Select(v=>-v).ToArray(),gc,cell,1));
        Check(TerrainPatchAssembly.Assemble(5,5,new Vector3[25],gn,gc,cell,1,limits:new(25,false)).Geometry.CopyPositions().All(v=>v==Vector3.Zero),"Explicit modern winding bypass");
        foreach(float scale in new[]{0f,float.NaN,float.MaxValue})Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,scale));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1,new[]{4}));
        Expect<ArgumentOutOfRangeException>(()=>TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1,new[]{0,1,2,3}));
        gp[24]=new(float.NaN,0,0);gn[24]=Vector3.Zero;
        Check(TerrainPatchAssembly.Assemble(5,5,gp,gn,gc,cell,1).CopySourceSampleIndices().SequenceEqual(retainedIds),"Unselected data not scanned");
        Check(retained.CopySourceSampleIndices().SequenceEqual(retainedIds)&&retained.Geometry.CopyPositions().SequenceEqual(retainedPositions),"Earlier result survives later rejection");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Negatives=negatives,Cases=fixtures.Cases.Length,SelectorCases=fixtures.Selections.Length,SyntheticGrids=true,Results=rows,Selections=selections},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R33_ASSEMBLY_PASS checks={checks} negatives={negatives} cases={fixtures.Cases.Length} selectors={fixtures.Selections.Length}");
    }
    private static (Vector3[],Vector3[],uint[]) Grid(int width,int height)
    {
        var p=new Vector3[width*height];var n=new Vector3[p.Length];var c=new uint[p.Length];
        for(int i=0;i<p.Length;i++){int column=i%width,row=i/width;p[i]=new(column*.25f,row*.0625f+column*.125f,row*.25f);n[i]=new(0,2,0);c[i]=0x80000000u|((uint)column<<16)|((uint)row<<8)|((uint)i&255);}
        return(p,n,c);
    }
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Expect<T>(Action action)where T:Exception{checks++;negatives++;try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
}
