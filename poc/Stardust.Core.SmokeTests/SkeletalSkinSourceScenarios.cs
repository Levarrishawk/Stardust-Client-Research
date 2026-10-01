using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class SkeletalSkinSourceScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var skin=SkeletalMeshSkinSourceDecoder.Decode(source);Array.Clear(source);
        if(skin.Positions.Count!=2||skin.Weights.Count!=3||skin.WeightRanges[1].Start!=1||skin.TransformNames[1]!="Head"||skin.UninterpretedSuffix.Count!=1||skin.Normals.Count!=2)throw new Exception("Owned skin source mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(valid.AsSpan(0,i)));
        Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(index:2)));Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(weight:float.NaN)));
        Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(weight:-1)));Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(weight:.4f)));
        Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(head:0)));Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(Fixture(head:3)));
        byte[] wrong=Fixture();wrong[23]=(byte)'3';Reject(()=>SkeletalMeshSkinSourceDecoder.Decode(wrong));
        Console.WriteLine("PASS: owned SKMG 0004 skin-source prefix, flat weight ranges/named arrays/retained suffix and malformed count/index/sum/float/version/truncation gates");
    }
    private static byte[] Fixture(uint index=1,float weight=.5f,int head=2)
    {
        byte[] Float(params float[] f)=>f.SelectMany(BitConverter.GetBytes).ToArray();
        var info=new[]{0,0,1,2,2,3,2,1,0}.SelectMany(BitConverter.GetBytes).Concat(new byte[8]).ToArray();
        return MeshScenarios.Form("SKMG",MeshScenarios.Form("0004",MeshScenarios.Chunk("INFO",info),
            MeshScenarios.Chunk("SKTM",Encoding.ASCII.GetBytes("appearance/skeleton/body.skt\0")),MeshScenarios.Chunk("XFNM",Encoding.ASCII.GetBytes("root\0Head\0")),
            MeshScenarios.Chunk("POSN",Float(0,0,0,0,1,0)),MeshScenarios.Chunk("TWHD",BitConverter.GetBytes(1).Concat(BitConverter.GetBytes(head)).ToArray()),
            MeshScenarios.Chunk("TWDT",BitConverter.GetBytes(0u).Concat(Float(1)).Concat(BitConverter.GetBytes(0u)).Concat(Float(.5f)).Concat(BitConverter.GetBytes(index)).Concat(Float(weight)).ToArray()),
            MeshScenarios.Chunk("NORM",Float(0,1,0,0,1,0)),MeshScenarios.Chunk("TEST",new byte[]{1})));
    }
    internal static string CanonicalHash(SkeletalMeshSkinSourceDescription s)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        foreach(int i in s.InfoWords)w.Write(i);foreach(short i in s.InfoTail)w.Write(i);
        foreach(string p in s.SkeletonPaths.Concat(s.TransformNames)){w.Write(Encoding.ASCII.GetBytes(p));w.Write((byte)0);}
        foreach(var p in s.Positions){w.Write(p.X);w.Write(p.Y);w.Write(p.Z);}
        foreach(var r in s.WeightRanges)w.Write(r.Count);
        foreach(var t in s.Weights){w.Write(t.TransformIndex);w.Write(t.Weight);}
        foreach(var p in s.Normals){w.Write(p.X);w.Write(p.Y);w.Write(p.Z);}
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
