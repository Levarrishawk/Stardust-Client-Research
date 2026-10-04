using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainVertexColorScenarios
{
    public static void Run(string output)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        long checks = 0;
        void Check(bool value) { checks++; if (!value) throw new InvalidOperationException("Terrain color scenario failed."); }
        TerrainRgb black = new(0,0,0), white = new(255,255,255);
        for (int s=0;s<256;s++) for (int t=0;t<256;t++)
        {
            TerrainRgb source = new((byte)s,(byte)(255-s),(byte)(s^85));
            TerrainRgb endpoint = new((byte)t,(byte)(t^170),(byte)(255-t));
            foreach (byte mask in new byte[]{0,1,2,255})
            {
                var result=TerrainVertexColor.Convert(source,mask,true,endpoint,endpoint);
                Check(result.Rgba.W==1f && float.IsFinite(result.Rgba.X) && float.IsFinite(result.Rgba.Y) && float.IsFinite(result.Rgba.Z));
                Check((result.Argb & 0xFFFFFFu)==((uint)result.ModifiedRgb.R<<16 | (uint)result.ModifiedRgb.G<<8 | result.ModifiedRgb.B));
                writer.Write(result.Argb);
                writer.Write(BitConverter.SingleToInt32Bits(result.Rgba.X));
                writer.Write(BitConverter.SingleToInt32Bits(result.Rgba.Y));
                writer.Write(BitConverter.SingleToInt32Bits(result.Rgba.Z));
            }
            var disabled=TerrainVertexColor.Convert(source,255,false,black,white);
            Check(disabled.ModifiedRgb==source);
            writer.Write(disabled.Argb);
        }
        for(int s=0;s<256;s++) for(int mask=0;mask<256;mask++)
        {
            var result=TerrainVertexColor.Convert(new((byte)s,(byte)s,(byte)s),(byte)mask,true,black,white);
            Check(result.ModifiedRgb.R==result.ModifiedRgb.G && result.ModifiedRgb.G==result.ModifiedRgb.B);
            writer.Write(result.Argb);
        }
        for(int corner=0;corner<8;corner++) foreach(byte mask in new byte[]{0,1,42,255}) foreach(bool enabled in new[]{false,true})
        {
            TerrainRgb src=new((byte)((corner&1)==0?0:255),(byte)((corner&2)==0?0:255),(byte)((corner&4)==0?0:255));
            var result=TerrainVertexColor.Convert(src,mask,enabled,black,white);
            Check(result.Rgba.X>=0 && result.Rgba.X<=1 && result.Rgba.Y>=0 && result.Rgba.Y<=1 && result.Rgba.Z>=0 && result.Rgba.Z<=1);
            writer.Write(result.Argb);
        }
        for(int s=0;s<256;s++) for(int mask=0;mask<256;mask++)
        {
            var result=TerrainVertexColor.Convert(new((byte)s,(byte)s,(byte)s),(byte)mask,false,black,white);
            Check(result.ModifiedRgb==new TerrainRgb((byte)s,(byte)s,(byte)s));
            writer.Write(result.Argb);
        }
        Check(TerrainVertexColor.Convert(new(5,5,5),0,true,black,white).ModifiedRgb==new TerrainRgb(2,2,2));
        Check(TerrainVertexColor.Convert(new(0,0,0),1,true,black,white).Argb==0xFF999999u);
        Check(TerrainVertexColor.Convert(new(255,255,255),0,true,black,white).Argb==0xFF666666u);
        var original=new TerrainRgb(10,20,30);
        var owned=TerrainVertexColor.Convert(original,0,false,black,white);
        original=new(90,90,90);
        var copiedVector=owned.Rgba; copiedVector.X=0;
        Check(owned.Rgba.X>0);
        Check(owned.ModifiedRgb==new TerrainRgb(10,20,30));
        File.WriteAllText(output,JsonSerializer.Serialize(new { checks, negativeCases=0, digest=Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant(), bytes=stream.Length }));
        Console.WriteLine($"Terrain color: {checks} checks; byte-only inputs exclude invalid finite values.");
    }
}
