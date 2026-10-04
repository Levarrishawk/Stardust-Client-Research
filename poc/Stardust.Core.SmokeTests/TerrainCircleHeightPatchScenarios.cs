using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainCircleHeightPatchScenarios
{
    public static object Run(string fixtures)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();
        TerrainBoundaryCircleInput? retainedCircle=null;TerrainHeightConstantInput? retainedHeight=null;
        foreach(var f in document.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            var circle=TerrainBoundaryCircleInput.Decode(Convert.FromHexString(f.GetProperty("CircleHex").GetString()!));
            var height=TerrainGeneratorInput.Decode(Convert.FromHexString(f.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!;
            uint[] Words(string key)=>f.GetProperty(key).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
            try
            {
                var value=TerrainCircleHeightPatch.Build(f.GetProperty("Cells").GetInt32(),Words("First"),Words("Second"),Words("Initial"),circle,height,
                    (TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32(),(TerrainHeightArithmeticPrecision)f.GetProperty("QueryPrecision").GetInt32(),
                    (TerrainHeightArithmeticPrecision)f.GetProperty("HeightPrecision").GetInt32(),f.GetProperty("Mask").GetInt32());
                cases.Add(new {Name=name,Reject="None",Heights=value.CopyHeightBits(),Influences=value.Samples.Select(s=>s.Circle.InfluenceBits).ToArray(),
                    Skipped=value.Samples.Select(s=>s.Circle.SkippedDisabled).ToArray(),Outcomes=value.Samples.Select(s=>(int?)s.Height?.Outcome).ToArray(),
                    Patches=value.Patches.Select(p=>new {Pattern=p.Geometry.PatternIndex,Source=p.CopySourceSampleIndices(),Indices=p.Geometry.CopyIndices(),
                        Positions=p.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray(),
                        Normals=p.Geometry.CopyNormals().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray(),Colors=p.Geometry.CopyArgbColors()}).ToArray()});
                retainedCircle??=circle;retainedHeight??=height;
            }
            catch(ArgumentOutOfRangeException){cases.Add(new{Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new{Name=name,Reject="Input"});}
            catch(ArgumentException){cases.Add(new{Name=name,Reject="Input"});}
        }
        var mode=TerrainHeightArithmeticPrecision.Binary53;var c=retainedCircle!;var h=retainedHeight!;int checks=0;
        void Check(bool value){checks++;if(!value)throw new InvalidOperationException("Patch ownership check.");}
        void Bad<T>(Action action) where T:Exception {try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected patch rejection.");}
        uint[] axis={0xbf800000,0,0x3f800000},initial=new uint[9];
        TerrainCircleHeightPatch Make(uint[] x,uint[] z,uint[] b,TerrainCircleHeightPatchLimits? cap=null)=>TerrainCircleHeightPatch.Build(1,x,z,b,c,h,mode,mode,mode,limits:cap);
        var retained=Make(axis,axis,initial,new(1,9));var old=retained.CopyHeightBits();
        Check(axis.SequenceEqual(new uint[]{0xbf800000,0,0x3f800000})&&initial.All(b=>b==0));
        var copy=retained.CopyHeightBits();copy[0]=0xdeadbeef;Check(retained.CopyHeightBits().SequenceEqual(old));
        initial[8]=0x7fc12345;Bad<InvalidDataException>(()=>Make(axis,axis,initial));Check(retained.CopyHeightBits().SequenceEqual(old));
        axis[0]=0xff800000;Bad<InvalidDataException>(()=>Make(axis,axis,new uint[9]));Check(retained.Samples[0].FirstCoordinateBits==0xbf800000);axis[0]=0xbf800000;
        Bad<InvalidDataException>(()=>Make(axis,axis,new uint[8]));Bad<InvalidDataException>(()=>Make(axis,new uint[]{0,0,0x3f800000},new uint[9]));
        Bad<InvalidDataException>(()=>Make(axis,new uint[]{0x3f800000,0,0xbf800000},new uint[9]));
        Bad<ArgumentOutOfRangeException>(()=>Make(axis,axis,new uint[9],new(0,9)));
        Bad<ArgumentOutOfRangeException>(()=>Make(axis,axis,new uint[9],new(1025,9)));
        Bad<ArgumentOutOfRangeException>(()=>Make(axis,axis,new uint[9],new(1,8)));
        Bad<ArgumentOutOfRangeException>(()=>Make(axis,axis,new uint[9],new(1,4226)));
        uint[] five=Enumerable.Range(-2,5).Select(i=>BitConverter.SingleToUInt32Bits(i)).ToArray();
        Check(TerrainCircleHeightPatch.Build(2,five,five,new uint[25],c,h,mode,mode,mode,limits:new(4,25)).Patches.Count==4);
        Bad<InvalidDataException>(()=>TerrainCircleHeightPatch.Build(2,five,five,new uint[25],c,h,mode,mode,mode,limits:new(3,25)));
        Bad<InvalidDataException>(()=>TerrainCircleHeightPatch.Build(2,five,five,new uint[25],c,h,mode,mode,mode,limits:new(4,24)));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(33,[],[],[],c,h,mode,mode,mode));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,h,0,mode,mode));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,h,mode,0,mode));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,h,mode,mode,0));
        Bad<ArgumentNullException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],null!,h,mode,mode,mode));
        Bad<ArgumentNullException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,null!,mode,mode,mode));
        Bad<NotSupportedException>(()=>((IList<TerrainCircleHeightSample>)retained.Samples)[0]=default);
        Bad<NotSupportedException>(()=>((IList<TerrainPatchAssembly>)retained.Patches)[0]=retained.Patches[0]);
        var positions=retained.Patches[0].Geometry.CopyPositions();var before=positions[0];positions[0]=new(99,99,99);Check(retained.Patches[0].Geometry.CopyPositions()[0]==before);
        var indices=retained.Patches[0].CopySourceSampleIndices();indices[0]=99;Check(retained.Patches[0].CopySourceSampleIndices()[0]==4);
        uint[] seventeen=Enumerable.Range(-8,17).Select(i=>BitConverter.SingleToUInt32Bits(i)).ToArray();
        Check(TerrainCircleHeightPatch.Build(8,seventeen,seventeen,new uint[289],c,h,mode,mode,mode).Patches.Count==64);
        uint[] sixtyFive=Enumerable.Range(-32,65).Select(i=>BitConverter.SingleToUInt32Bits(i)).ToArray();
        Check(TerrainCircleHeightPatch.Build(32,sixtyFive,sixtyFive,new uint[4225],c,h,mode,mode,mode,limits:new(1024,4225)).Patches.Count==1024);
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(0,[],[],[],c,h,mode,mode,mode));
        Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,h,mode,mode,mode,seamMask:16));
        foreach(float scale in new[]{0f,17f,float.NaN})Bad<ArgumentOutOfRangeException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],c,h,mode,mode,mode,uvScale:scale));
        uint[] shared={0xbf800000,0,0x3f800000,0,0,0,0,0,0};var sharedBefore=(uint[])shared.Clone();
        Check(TerrainCircleHeightPatch.Build(1,shared.AsSpan(0,3),shared.AsSpan(0,3),shared,c,h,mode,mode,mode).Samples.Count==9&&shared.SequenceEqual(sharedBefore));
        var opaqueBytes=c.CopyEncodedBytes();opaqueBytes[23]=(byte)'9';var opaque=TerrainBoundaryCircleInput.Decode(opaqueBytes);
        Bad<InvalidDataException>(()=>TerrainCircleHeightPatch.Build(1,axis,axis,new uint[9],opaque,h,mode,mode,mode));
        return new {Cases=cases,OwnershipChecks=checks};
    }
}
