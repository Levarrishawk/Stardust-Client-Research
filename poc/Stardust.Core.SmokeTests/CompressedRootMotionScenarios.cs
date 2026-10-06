using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

namespace Stardust.Core.SmokeTests;

public static class CompressedRootMotionScenarios
{
    public static void Run(string fixtures,string output)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var root=document.RootElement;int checks=0,rejected=0;var tracks=new List<object>();
        void Check(bool value){checks++;if(!value)throw new InvalidOperationException("Root motion fixture mismatch.");}
        void Reject(Action action){try{action();}catch(InvalidDataException){checks++;return;}throw new InvalidOperationException("Expected root motion rejection.");}
        void Cancel(Action action){try{action();}catch(OperationCanceledException){checks++;return;}throw new InvalidOperationException("Expected root motion cancellation.");}
        using var canceled=new CancellationTokenSource();canceled.Cancel();
        foreach(var item in root.GetProperty("Valid").EnumerateArray())
        {
            string name=item.GetProperty("Name").GetString()!;var bytes=Convert.FromHexString(item.GetProperty("Bytes").GetString()!);var curve=CompressedRootMotion.Create(bytes);
            string identity=Convert.ToHexString(SHA256.HashData(bytes));Check(curve.SourceSha256==identity);Check(curve.FrameRate==30);Check(curve.EndFrame==item.GetProperty("End").GetInt32());
            Check(curve.Mode.ToString()==item.GetProperty("Mode").GetString());var owned=curve.Positions.ToArray();var rotationKeys=curve.RotationTrack.Keys.ToArray();var samples=new List<object>();
            foreach(var sample in item.GetProperty("Samples").EnumerateArray())
            {
                double a=sample.GetProperty("From").GetDouble(),b=sample.GetProperty("To").GetDouble();var value=curve.SampleDelta(a,b);var q=value.Rotation;var d=value.Translation;
                Check(float.IsFinite(q.X)&&float.IsFinite(q.Y)&&float.IsFinite(q.Z)&&float.IsFinite(q.W)&&Math.Abs(q.Length()-1)<3e-6);
                Check(float.IsFinite(d.X)&&float.IsFinite(d.Y)&&float.IsFinite(d.Z));
                if(a==b)Check(d==Vector3.Zero&&Math.Abs(q.W-1)<3e-6);
                samples.Add(new{From=a,To=b,Rotation=new[]{q.X,q.Y,q.Z,q.W},Translation=new[]{d.X,d.Y,d.Z},Repaired=value.RotationRepaired});
            }
            var before=curve.SampleDelta(2,8);Array.Fill<byte>(bytes,0);Check(curve.SampleDelta(2,8)==before&&curve.SourceSha256==identity&&curve.Positions.SequenceEqual(owned)&&curve.RotationTrack.Keys.SequenceEqual(rotationKeys));
            if(curve.Positions.Count>0&&curve.Positions is IList<LocomotionPositionKey> mutable)
            {try{mutable[0]=new(0,Vector3.One);throw new InvalidOperationException("Mutable position keys.");}catch(NotSupportedException){checks++;}}
            var copy=before.Translation;copy.X=999;Check(curve.SampleDelta(2,8)==before);
            foreach(double frame in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,-1d,curve.EndFrame+.01})
            {Reject(()=>curve.SampleDelta(frame,0));Reject(()=>curve.SampleDelta(0,frame));}
            Cancel(()=>curve.SampleDelta(0,1,canceled.Token));Check(curve.SampleDelta(2,8)==before);Cancel(()=>CompressedRootMotion.Create(Convert.FromHexString(item.GetProperty("Bytes").GetString()!),canceled.Token));
            if(curve.Mode==CompressedRootMotionMode.None)Check(before==new RootMotionDelta(Quaternion.Identity,Vector3.Zero,false));
            if(curve.Mode==CompressedRootMotionMode.Rotation)Check(before.Translation==Vector3.Zero);
            if(curve.Mode==CompressedRootMotionMode.Translation)Check(before.Rotation==Quaternion.Identity&&before.Translation==new Vector3(4,7,11));
            tracks.Add(new{Name=name,Sha256=curve.SourceSha256,Mode=curve.Mode.ToString(),End=(int)curve.EndFrame,Speed=curve.AuthoredSpeed,Positions=curve.Positions.Select(k=>new object[]{k.Frame,new[]{k.Position.X,k.Position.Y,k.Position.Z}}).ToArray(),Samples=samples});
        }
        Reject(()=>CompressedRootMotion.Create(new byte[8*1024*1024+1]));Cancel(()=>CompressedRootMotion.Create(ReadOnlySpan<byte>.Empty,canceled.Token));
        foreach(var item in root.GetProperty("Invalid").EnumerateArray()){var b=Convert.FromHexString(item.GetProperty("Bytes").GetString()!);Reject(()=>CompressedRootMotion.Create(b));rejected++;}
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Tracks=tracks,RejectedMalformed=rejected,Checks=checks},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS root motion checks={checks} malformed={rejected}");
    }
}
