using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
namespace Stardust.Core.SmokeTests;
public static class TerrainSuppliedStaticMaterialScenarios
{
    private static void Need(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static float F(JsonElement b)=>BitConverter.UInt32BitsToSingle(b.GetUInt32());
    private static Vector4 Color(JsonElement c)=>new(F(c[0]),F(c[1]),F(c[2]),F(c[3]));
    private static uint[] Bits(Vector4 v)=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z),BitConverter.SingleToUInt32Bits(v.W)};
    private static object Snapshot(TerrainSuppliedStaticMaterial m)=>new{m.ShaderSource,m.TextureSource,m.SourceBytes,m.DecodedBytes,m.ExternalReads,m.Width,m.Height,m.MipCount,m.IsCompleteMipChain,m.IsSingleMipOrCompleteChain,Format=m.Format.ToString(),ColorSpace=m.ColorSpace.ToString(),AlphaMode=m.AlphaMode.ToString(),m.DxgiFormat,Version=m.Shader.Version,m.Shader.EffectPath,Binding=new{m.Binding.Tag,m.Binding.SourceFlag,m.Binding.Version,m.Binding.AssetPath},Diffuse=Bits(m.DiffuseMultiplier),Authored=m.AuthoredMainDiffuse is { } d?Bits(d):null,Materials=m.Parameters.Materials.ToDictionary(p=>p.Key.ToString(),p=>p.Value.Select(BitConverter.SingleToUInt32Bits).ToArray()),Coordinates=m.Parameters.CoordinateSets.ToDictionary(p=>p.Key.ToString(),p=>p.Value),Factors=m.Factors.ToDictionary(p=>p.Key.ToString(),p=>p.Value),Mips=m.CopyDecodedTexture().Mips.Select((p,i)=>new{p.Width,p.Height,Bytes=p.Rgba.Length,Sha256=Hash(m.CopyRgba(i))}).ToArray(),m.OriginalTerrainAssociation,m.OriginalShaderExecutionParity};
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int checks=0;TerrainSuppliedStaticMaterial? prior=null;string? priorHash=null;
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            var resources=c.GetProperty("Resources").EnumerateArray().ToDictionary(p=>p.GetProperty("Path").GetString()!,p=>new TreLookupResult(Enum.Parse<TreLookupKind>(p.GetProperty("Kind").GetString()!),p.TryGetProperty("Hex",out var h)&&h.ValueKind!=JsonValueKind.Null?Convert.FromHexString(h.GetString()!):null,p.GetProperty("Priority").GetInt32()));
            var calls=new List<string>();using var cancellation=new CancellationTokenSource();if(c.TryGetProperty("PreCancel",out var pc)&&pc.GetBoolean())cancellation.Cancel();
            TerrainSuppliedStaticMaterial Build()=>TerrainSuppliedStaticMaterial.Load(p=>{calls.Add(p);if(c.TryGetProperty("CancelRead",out var cr)&&cr.GetInt32()==calls.Count)cancellation.Cancel();return resources.TryGetValue(p,out var v)?v:new(TreLookupKind.Missing);},c.GetProperty("RootPath").GetString()!,c.GetProperty("Pin").GetString()!,(TerrainSuppliedStaticMaterialPolicy)c.GetProperty("Policy").GetInt32(),c.GetProperty("Limits").Deserialize<TerrainSuppliedStaticMaterialLimits>(),cancellation.Token);
            try
            {
                var material=Build();Need(ReferenceEquals(material.Binding,material.Shader.Textures.Single(b=>b.Tag==TerrainSuppliedStaticMaterial.MainTag)),"Actual binding in actual owned shader description.");checks++;
                var snapshot=Snapshot(material);var before=Hash(material.CopyRgba(0));var copied=material.CopyRgba(0);Array.Clear(copied);
                var image=material.CopyDecodedTexture();foreach(var mip in image.Mips)if(MemoryMarshal.TryGetArray(mip.Rgba,out var segment))Array.Clear(segment.Array!,segment.Offset,segment.Count);
                foreach(var source in resources.Values)if(source.Payload is not null)Array.Clear(source.Payload);
                Need(Hash(material.CopyRgba(0))==before,"Detached pixels/image and borrowed bytes.");checks++;
                var shades=c.GetProperty("Colors").EnumerateArray().Select(v=>Bits(material.Modulate(Color(v.GetProperty("Texel")),Color(v.GetProperty("Vertex"))))).ToArray();
                bool failed=false;try{_=material.Modulate(new(float.NaN,0,0,1),Vector4.One);}catch(ArgumentException){failed=true;}Need(failed&&Hash(material.CopyRgba(0))==before,"Invalid modulation preserves result.");checks++;
                Parallel.For(0,4,i=>Need(Hash(material.CopyRgba(0))==before,"Concurrent detached export."));checks+=4;
                prior=material;priorHash=before;output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Calls=calls,Material=snapshot,Colors=shades});
            }
            catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException or OverflowException){output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input",Calls=calls});checks++;}
            if(prior is not null){Need(Hash(prior.CopyRgba(0))==priorHash,"Prior success preserved after later rejection.");checks++;}
        }
        return new{Cases=output,OwnershipChecks=checks};
    }
}
