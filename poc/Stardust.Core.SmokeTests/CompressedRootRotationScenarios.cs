using System.Collections;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

namespace Stardust.Core.SmokeTests;

public static class CompressedRootRotationScenarios
{
    public static void Run(string fixtures,string output)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var root=document.RootElement;
        int checks=0,rejected=0;var tracks=new List<object>();
        void Check(bool value){checks++;if(!value)throw new InvalidOperationException("Root rotation fixture mismatch.");}
        void Reject(Action action){try{action();}catch(InvalidDataException){checks++;return;}throw new InvalidOperationException("Expected root rotation rejection.");}
        foreach(var item in root.GetProperty("Valid").EnumerateArray())
        {
            string name=item.GetProperty("Name").GetString()!;var bytes=Convert.FromHexString(item.GetProperty("Bytes").GetString()!);
            var curve=CompressedRootRotation.Create(bytes);string identity=Convert.ToHexString(SHA256.HashData(bytes));
            Check(curve.SourceSha256==identity);Check(curve.HasRotation==item.GetProperty("HasRotation").GetBoolean());
            Check(curve.FrameRate==30);Check(curve.EndFrame==item.GetProperty("End").GetInt32());
            var before=curve.Keys.ToArray();var samples=new List<object>();
            foreach(var sample in item.GetProperty("Samples").EnumerateArray())
            {
                double frame=sample.GetProperty("Frame").GetDouble();var value=curve.SampleRotation(frame);var q=value.Rotation;
                Check(float.IsFinite(q.X)&&float.IsFinite(q.Y)&&float.IsFinite(q.Z)&&float.IsFinite(q.W));Check(Math.Abs(q.Length()-1)<2e-6);
                samples.Add(new{Frame=frame,Quaternion=new[]{q.X,q.Y,q.Z,q.W},value.Repaired});
            }
            if(curve.HasRotation)
            {
                var saved=curve.SampleRotation(5);Array.Fill<byte>(bytes,0);Check(curve.SampleRotation(5)==saved&&curve.SourceSha256==identity&&curve.Keys.SequenceEqual(before));
                if(curve.Keys is IList<CompressedRotationKey> mutable)
                {try{mutable[0]=new(0,0);throw new InvalidOperationException("Mutable root keys.");}catch(NotSupportedException){checks++;}}
                var local=curve.SampleRotation(5).Rotation;local.X=999;Check(curve.SampleRotation(5)==saved);
                foreach(double frame in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,-1d,curve.EndFrame+.01})Reject(()=>curve.SampleRotation(frame));
                var original=Convert.FromHexString(item.GetProperty("Bytes").GetString()!);var clip=CompressedKeyframeDecoder.Decode(original);
                Reject(()=>CompressedLocomotion.Create(clip));
                if(name=="identity")Check(curve.SampleRotation(5).Rotation==Quaternion.Identity);
                if(name=="antipodal")Check(Math.Abs(curve.SampleRotation(5).Rotation.X)>.99);
                if(name=="repair")Check(curve.RepairedPackedValues==2&&curve.SampleRotation(5).Repaired);
                if(name=="turn")Check(curve.SampleRotation(2).Rotation==Quaternion.Identity&&Math.Abs(curve.SampleRotation(8).Rotation.Y)>.999);
            }
            else{Reject(()=>curve.SampleRotation(0));Check(curve.Keys.Count==0&&curve.RepairedPackedValues==0);}
            tracks.Add(new{Name=name,Sha256=curve.SourceSha256,End=(int)curve.EndFrame,curve.HasRotation,Selectors=curve.HasRotation?new int[]{curve.XSelector,curve.YSelector,curve.ZSelector}:Array.Empty<int>(),Keys=curve.Keys.Select(k=>new long[]{k.Frame,k.Value}).ToArray(),Samples=samples});
        }
        Reject(()=>CompressedRootRotation.Create(new byte[8*1024*1024+1]));
        foreach(var item in root.GetProperty("Invalid").EnumerateArray())
        {var bytes=Convert.FromHexString(item.GetProperty("Bytes").GetString()!);Reject(()=>CompressedRootRotation.Create(bytes));rejected++;}
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Tracks=tracks,RejectedMalformed=rejected,Checks=checks},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS root rotation checks={checks} malformed={rejected}");
    }
}
