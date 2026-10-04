using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainCoordinateHeightPatchScenarios
{
    public static object Run(string fixtures)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();
        foreach(var f in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            {
                var r=f.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>();var p=f.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
                var fixtureCoordinates=TerrainSampleCoordinateGrid.Build(r,p[0],p[1],p[2],p[3]);
                uint[] Words(string key)=>f.GetProperty(key).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
                var fixtureNormals=f.GetProperty("Normals").EnumerateArray().Select(v=>new Vector3(BitConverter.UInt32BitsToSingle(v[0].GetUInt32()),BitConverter.UInt32BitsToSingle(v[1].GetUInt32()),BitConverter.UInt32BitsToSingle(v[2].GetUInt32()))).ToArray();
                var patch=TerrainCoordinateHeightPatch.Build(fixtureCoordinates,Words("Heights"),fixtureNormals,Words("Colors"),f.GetProperty("Mask").GetInt32(),
                    BitConverter.UInt32BitsToSingle(f.GetProperty("ScaleBits").GetUInt32()),f.GetProperty("Limits").Deserialize<TerrainCoordinateHeightPatchLimits>());
                cases.Add(new{Name=name,Reject="None",patch.GridWidth,patch.CellsPerSide,patch.SampleBorder,patch.LeadingPadding,patch.TrailingPadding,
                    InputHeights=patch.CopyInputHeightBits(),PositionHeights=patch.CopyPositionHeightBits(),Colors=patch.CopyArgbColors(),
                    Patches=patch.Patches.Select(a=>new{Source=a.CopySourceSampleIndices(),Pattern=a.Geometry.PatternIndex,Indices=a.Geometry.CopyIndices(),
                        Positions=a.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray(),
                        Normals=a.Geometry.CopyNormals().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray(),
                        Colors=a.Geometry.CopyArgbColors(),LayerPairs=a.CopySourceSampleIndices().Select(i=>fixtureCoordinates.Layer.GetPair(i%patch.GridWidth,i/patch.GridWidth)).ToArray()}).ToArray()});
            }
            catch(ArgumentOutOfRangeException){cases.Add(new{Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new{Name=name,Reject="Input"});}
            catch(ArgumentException){cases.Add(new{Name=name,Reject="Input"});}
        }
        int checks=0;void Check(bool condition){checks++;if(!condition)throw new InvalidOperationException("Padded patch ownership check.");}
        void Bad<T>(Action action) where T:Exception{try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected patch rejection.");}
        var mode=TerrainHeightArithmeticPrecision.Binary24;var request=new TerrainSampleCoordinateRequest(0x41000000,2,3,-1,0,1);
        TerrainSampleCoordinateGrid Grid(TerrainSampleCoordinateRequest r)=>TerrainSampleCoordinateGrid.Build(r,mode,mode,mode,mode);
        var coordinates=Grid(request);uint[] heights=new uint[100],colors=Enumerable.Repeat(0xFF4080C0u,100).ToArray();var normals=Enumerable.Repeat(Vector3.UnitY,100).ToArray();
        TerrainCoordinateHeightPatch Make(uint[] h,Vector3[] n,uint[] c,TerrainCoordinateHeightPatchLimits? cap=null)=>TerrainCoordinateHeightPatch.Build(coordinates,h,n,c,limits:cap);
        var old=Make(heights,normals,colors,new(100,4,36));
        Check(ReferenceEquals(old.Coordinates,coordinates)&&old.GridWidth==10&&old.LeadingPadding==3&&old.TrailingPadding==2&&old.Patches.Count==4);
        Check(old.Patches[0].CopySourceSampleIndices().SequenceEqual(new[]{44,43,53,54,55,45,35,34,33}));
        var expected=old.Patches[0].Geometry.CopyPositions();var copied=old.CopyInputHeightBits();copied[0]=99;Check(old.CopyInputHeightBits()[0]==0);
        copied=old.CopyPositionHeightBits();copied[0]=99;Check(old.CopyPositionHeightBits()[0]==0);
        copied=old.CopyArgbColors();copied[0]=0;Check(old.CopyArgbColors()[0]==0xFF4080C0);
        var normalCopy=old.CopyNormals();normalCopy[0]=default;Check(old.CopyNormals()[0]==Vector3.UnitY);
        heights[99]=0x7fc12345;Bad<InvalidDataException>(()=>Make(heights,normals,colors));Check(old.Patches[0].Geometry.CopyPositions().SequenceEqual(expected));heights[99]=0;
        normals[99]=default;Bad<InvalidDataException>(()=>Make(heights,normals,colors));normals[99]=Vector3.UnitY;
        colors[0]=0;Check(old.CopyArgbColors()[0]==0xFF4080C0);colors[0]=0xFF4080C0;
        Bad<NotSupportedException>(()=>((IList<TerrainPatchAssembly>)old.Patches)[0]=old.Patches[1]);
        Bad<InvalidDataException>(()=>Make(new uint[99],normals,colors));Bad<InvalidDataException>(()=>Make(heights,new Vector3[99],colors));Bad<InvalidDataException>(()=>Make(heights,normals,new uint[99]));
        Bad<InvalidDataException>(()=>Make(heights,normals,colors,new(99,4,36)));Bad<InvalidDataException>(()=>Make(heights,normals,colors,new(100,3,36)));Bad<InvalidDataException>(()=>Make(heights,normals,colors,new(100,4,35)));
        foreach(var cap in new[]{new TerrainCoordinateHeightPatchLimits(0,4,36),new(1048577,4,36),new(100,0,36),new(100,1025,36),new(100,4,0),new(100,4,9217)})Bad<ArgumentOutOfRangeException>(()=>Make(heights,normals,colors,cap));
        Bad<InvalidDataException>(()=>TerrainCoordinateHeightPatch.Build(Grid(request with{Margin=0}),new uint[16],Enumerable.Repeat(Vector3.UnitY,16).ToArray(),new uint[16]));
        Bad<InvalidDataException>(()=>TerrainCoordinateHeightPatch.Build(Grid(request with{Margin=-1}),new uint[4],Enumerable.Repeat(Vector3.UnitY,4).ToArray(),new uint[4]));
        var zeros=Grid(request with{ChunkWidthBits=1});int count=zeros.Parameters.Count;
        Bad<InvalidDataException>(()=>TerrainCoordinateHeightPatch.Build(zeros,new uint[count*count],Enumerable.Repeat(Vector3.UnitY,count*count).ToArray(),new uint[count*count]));
        foreach(int i in new[]{55,56,57,65,66,67,75,76,77})normals[i]=-Vector3.UnitY;Bad<ArgumentException>(()=>Make(heights,normals,colors));foreach(int i in new[]{55,56,57,65,66,67,75,76,77})normals[i]=Vector3.UnitY;Check(old.Patches[0].Geometry.CopyPositions().SequenceEqual(expected));
        uint[] alias=new uint[100];var shared=Make(alias,normals,alias);Check(alias.All(v=>v==0)&&shared.CopyArgbColors().All(v=>v==0));alias[0]=1;Check(shared.CopyInputHeightBits()[0]==0&&shared.CopyArgbColors()[0]==0);
        heights[44]=0x80000000;var signed=Make(heights,normals,colors);Check(signed.CopyInputHeightBits()[44]==0x80000000&&signed.Patches[0].Geometry.CopyPositions()[0].Y==0&&BitConverter.SingleToUInt32Bits(signed.Patches[0].Geometry.CopyPositions()[0].Y)==0);heights[44]=0;
        Bad<ArgumentNullException>(()=>TerrainCoordinateHeightPatch.Build(null!,heights,normals,colors));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCoordinateHeightPatch.Build(coordinates,heights,normals,colors,seamMask:16));
        foreach(float uv in new[]{0f,17f,float.NaN})Bad<ArgumentOutOfRangeException>(()=>TerrainCoordinateHeightPatch.Build(coordinates,heights,normals,colors,uvScale:uv));
        var right=Grid(request with{First=0});int last=request.Margin+2*request.Tiles;
        Check(coordinates.Position.GetPair(last,3).FirstBits==right.Position.GetPair(request.Margin,3).FirstBits);
        var nextRow=Grid(request with{Second=1});Check(coordinates.Position.GetPair(3,last).SecondBits==nextRow.Position.GetPair(3,3).SecondBits);
        var exact=Grid(request with{Margin=1});Check(TerrainCoordinateHeightPatch.Build(exact,new uint[36],Enumerable.Repeat(Vector3.UnitY,36).ToArray(),new uint[36],limits:new(36,4,36)).TrailingPadding==0);
        var defaultGrid=Grid(request with{Tiles=8});int samples=defaultGrid.Parameters.Count*defaultGrid.Parameters.Count;
        Check(TerrainCoordinateHeightPatch.Build(defaultGrid,new uint[samples],Enumerable.Repeat(Vector3.UnitY,samples).ToArray(),new uint[samples]).Patches.Count==64);
        var query=new TerrainPatchSurfaceQuery(old.Patches[0].Geometry);var center=expected[0];var hit=query.Query(center.X,center.Z);
        Check(hit is not null&&hit.Value.Height==0&&hit.Value.Normal.Y==1);
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
