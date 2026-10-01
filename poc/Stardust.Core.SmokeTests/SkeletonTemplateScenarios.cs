using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class SkeletonTemplateScenarios
{
    internal static void Run()
    {
        byte[] bytes=Basic();var skeleton=SkeletonTemplateDecoder.Decode(bytes);Array.Clear(bytes);
        if(skeleton.SerializedLevels.Count!=1||skeleton.SerializedLevels[0].Joints[1].Name!="head"||skeleton.SerializedLevels[0].Joints[1].Parent!=0||skeleton.SerializedLevels[0].Joints[1].Bptr!=new Vector3(0,1,0)||skeleton.SerializedLevels[0].Joints[0].Rpre.W!=1||skeleton.SerializedLevels[0].Joints[1].Jror!=7)throw new Exception("Owned skeleton fields mismatch.");
        if(SkeletonTemplateDecoder.Decode(Basic(0)).SerializedLevels[0].Joints.Count!=0)throw new Exception("Valid zero-joint skeleton rejected.");
        if(skeleton.SerializedLevels[0].FindTransform("HEAD")!=1||skeleton.SerializedLevels[0].FindTransform("absent")!=-1)throw new Exception("Explicit adapter transform lookup mismatch.");
        var ambiguous=new BasicSkeletonDescription(Array.AsReadOnly(new[]{skeleton.SerializedLevels[0].Joints[1],skeleton.SerializedLevels[0].Joints[1] with{Name="HEAD"}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>ambiguous.FindTransform("head"));
        var lod=SkeletonTemplateDecoder.Decode(Lod(7));
        if(lod.SerializedLevels.Count!=7||lod.ExposedLevels().Count!=4||ReferenceEquals(lod.ExposedLevels(true)[0],lod.SerializedLevels[0])||!ReferenceEquals(lod.ExposedLevels(true)[0],lod.SerializedLevels[1]))throw new Exception("Skeleton cap/skip policy mismatch.");
        if(SkeletonTemplateDecoder.Decode(Lod(-1)).SerializedLevels.Count!=0)throw new Exception("Negative SLOD policy mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] basic=Basic();for(int i=0;i<basic.Length;i++)Reject(()=>SkeletonTemplateDecoder.Decode(basic.AsSpan(0,i)));
        byte[] slod=Lod(2);for(int i=0;i<slod.Length;i++)Reject(()=>SkeletonTemplateDecoder.Decode(slod.AsSpan(0,i)));
        Reject(()=>SkeletonTemplateDecoder.Decode(Basic(parent:1)));Reject(()=>SkeletonTemplateDecoder.Decode(Basic(parent:-2)));
        Reject(()=>SkeletonTemplateDecoder.Decode(Basic(parent:-1)));Reject(()=>SkeletonTemplateDecoder.Decode(Basic(count:2049)));
        Reject(()=>SkeletonTemplateDecoder.Decode(Basic(rotation:2)));Reject(()=>SkeletonTemplateDecoder.Decode(Basic(rotation:float.NaN)));
        Reject(()=>SkeletonTemplateDecoder.Decode(Basic(translation:float.PositiveInfinity)));Reject(()=>SkeletonTemplateDecoder.Decode(Basic(duplicate:true)));
        Reject(()=>SkeletonTemplateDecoder.Decode(Lod(65)));
        byte[] wrong=Basic();wrong[23]=(byte)'1';Reject(()=>SkeletonTemplateDecoder.Decode(wrong));
        Console.WriteLine("PASS: owned SKTM 0002/SLOD 0000 fields, zero joints/raw JROR, cap/skip, parent topology/unit quaternion and malformed array/count/version/truncation gates");
    }
    internal static byte[] Basic(int count=2,int parent=0,float rotation=1,float translation=1,bool duplicate=false)
    {
        byte[] Floats(params float[] values)=>values.SelectMany(BitConverter.GetBytes).ToArray();
        string[] names=Enumerable.Range(0,count).Select(i=>i==0||duplicate?"root":i==1?"head":"j"+i).ToArray();
        byte[] q=Enumerable.Range(0,count).SelectMany(i=>Floats(rotation,0,0,0)).ToArray();
        return MeshScenarios.Form("SKTM",MeshScenarios.Form("0002",
            MeshScenarios.Chunk("INFO",BitConverter.GetBytes((uint)count)),MeshScenarios.Chunk("NAME",Encoding.ASCII.GetBytes(string.Join('\0',names)+(count==0?"":"\0"))),
            MeshScenarios.Chunk("PRNT",Enumerable.Range(0,count).SelectMany(i=>BitConverter.GetBytes(i==0?-1:parent)).ToArray()),
            MeshScenarios.Chunk("RPRE",q),MeshScenarios.Chunk("RPST",q),MeshScenarios.Chunk("BPTR",Enumerable.Range(0,count).SelectMany(i=>Floats(0,i==0?0:translation,0)).ToArray()),
            MeshScenarios.Chunk("BPRO",q),MeshScenarios.Chunk("JROR",Enumerable.Range(0,count).SelectMany(i=>BitConverter.GetBytes(i==0?0u:7u)).ToArray())));
    }
    private static byte[] Lod(short count)=>MeshScenarios.Form("SLOD",MeshScenarios.Form("0000",new[]{MeshScenarios.Chunk("INFO",BitConverter.GetBytes(count))}.Concat(Enumerable.Range(0,Math.Max(0,(int)count)).Select(i=>Basic())).ToArray()));
    internal static string CanonicalHash(BasicSkeletonDescription skeleton)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);w.Write(skeleton.Joints.Count);
        void Q(ScalarFirstQuaternion q){w.Write(q.W);w.Write(q.X);w.Write(q.Y);w.Write(q.Z);}
        foreach(var j in skeleton.Joints){w.Write(Encoding.ASCII.GetBytes(j.Name));w.Write((byte)0);w.Write(j.Parent);Q(j.Rpre);Q(j.Rpst);w.Write(j.Bptr.X);w.Write(j.Bptr.Y);w.Write(j.Bptr.Z);Q(j.Bpro);w.Write(j.Jror);}
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
