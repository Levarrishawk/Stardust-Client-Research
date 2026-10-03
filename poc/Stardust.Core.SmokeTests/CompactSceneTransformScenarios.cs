using System.Text.Json;
using Stardust.Core.Protocol;

namespace Stardust.Core.SmokeTests;

public static class CompactSceneTransformScenarios
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Malformed transform accepted");}
    public sealed record Case(string Name,string Hex);
    public sealed record Peer(int Version,string Label,Case[] Cases);
    public static void Run(string peerPath,string exportPath)
    {
        byte[] input=File.ReadAllBytes(peerPath);Check(input.Length<=1048576,"Fixture byte cap");
        var peer=JsonSerializer.Deserialize<Peer>(input)??throw new InvalidDataException("Peer required");Check(peer.Version==1&&peer.Cases.Length==522,"Bounded fixture shape");
        var output=new List<object>();int malformed=0;
        foreach(var item in peer.Cases)
        {
            byte[] bytes=Convert.FromHexString(item.Hex);var value=CompactSceneTransformCodec.Decode(bytes);
            Check(Convert.ToHexString(CompactSceneTransformCodec.Encode(value))==item.Hex,"Exact whole APPLICATION roundtrip");
            for(int n=0;n<bytes.Length;n++){int length=n;Reject(()=>CompactSceneTransformCodec.Decode(bytes.AsSpan(0,length)));malformed++;}
            Reject(()=>CompactSceneTransformCodec.Decode([..bytes,0]));malformed++;
            for(int i=0;i<6;i++){byte[] bad=bytes.ToArray();bad[i]^=1;Reject(()=>CompactSceneTransformCodec.Decode(bad));malformed++;}
            Array.Clear(bytes);Check(Convert.ToHexString(CompactSceneTransformCodec.Encode(value))==item.Hex,"Owned scalar DTO");
            byte[] encoded=CompactSceneTransformCodec.Encode(value);Array.Clear(encoded);Check(Convert.ToHexString(CompactSceneTransformCodec.Encode(value))==item.Hex,"Independent output array");
            var position=value.NativePosition;
            output.Add(new {item.Name,item.Hex,Value=new {value.ObjectId,value.ParentId,value.RawX,value.RawZ,value.RawY,value.MovementCounter,value.SpeedRaw,value.DirectionRaw,
                value.CoordinateScale,NativePosition=new[]{position.X,position.Y,position.Z},NativePositionBits=new[]{BitConverter.SingleToUInt32Bits(position.X),BitConverter.SingleToUInt32Bits(position.Y),BitConverter.SingleToUInt32Bits(position.Z)}}});
        }
        byte[] shortBranch=Convert.FromHexString(peer.Cases[0].Hex),parentBranch=Convert.FromHexString(peer.Cases[3].Hex);
        shortBranch[2]=0x5A;shortBranch[3]=0xAB;shortBranch[4]=0x67;shortBranch[5]=0xC8;Reject(()=>CompactSceneTransformCodec.Decode(shortBranch));malformed++;
        parentBranch[2]=0x08;parentBranch[3]=0xF8;parentBranch[4]=0x24;parentBranch[5]=0x1B;Reject(()=>CompactSceneTransformCodec.Decode(parentBranch));malformed++;
        bool nullRejected=false;try{CompactSceneTransformCodec.Encode(null!);}catch(ArgumentNullException){nullRejected=true;}Check(nullRejected,"Null encode rejection");
        var root=CompactSceneTransformCodec.Decode(Convert.FromHexString(peer.Cases[0].Hex));var parentZero=CompactSceneTransformCodec.Decode(Convert.FromHexString(peer.Cases[3].Hex));
        Check(root.ParentId is null&&parentZero.ParentId==0&&parentZero.CoordinateScale==0.125f,"Parent zero retains branch distinction");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
        File.WriteAllBytes(exportPath,JsonSerializer.SerializeToUtf8Bytes(new {Cases=output,MalformedProbes=malformed,OwnershipAndNullChecks=true},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS compact transforms: {output.Count} exact fixtures, {malformed} malformed probes, raw ownership and parent-zero distinction");
    }
}
