using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record SkeletalSkeletonReference(string Path,string AttachmentTransform);
public sealed record SkeletalAnimationReference(string SkeletonPath,string AnimationTablePath);
public sealed record SkeletalDistanceInterval(float Lower,float Upper);
public sealed record SkeletalAppearanceDescription(bool InfoFlag,IReadOnlyList<string> MeshGenerators,
    IReadOnlyList<SkeletalSkeletonReference> Skeletons,IReadOnlyList<SkeletalAnimationReference> AnimationTables,
    IReadOnlyList<SkeletalDistanceInterval> DistanceIntervals,bool? Sfsk,bool? Apag);
public sealed record LodMeshGeneratorDescription(IReadOnlyList<string> SerializedPaths)
{
    public IReadOnlyList<string> ExposedPaths(bool skipL0=false)
        =>Array.AsReadOnly(SerializedPaths.Skip(skipL0&&SerializedPaths.Count>1?1:0).Take(4).ToArray());
}

/// <summary>Deployed SMAT 0003 and MLOD 0000 descriptors. Flags retain raw names; no character instance is created.</summary>
public static class SkeletalAppearanceDecoder
{
    public static SkeletalAppearanceDescription Decode(ReadOnlySpan<byte> bytes)
    {
        var parts=Version(bytes,"SMAT","0003").Children;
        Require(parts.Count>=3&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==9&&parts[1].Tag=="MSGN"&&parts[2].Tag=="SKTI","Invalid skeletal appearance prefix.");
        var info=parts[0].Payload.Span;
        int meshes=Count(U32(info),64),skeletons=Count(U32(info[4..]),64);
        Require(info[8]<=1,"Invalid skeletal INFO flag.");
        var names=Strings(parts[1].Payload.Span,meshes);var meshPaths=names.Select(Path).ToArray();
        var pairs=Strings(parts[2].Payload.Span,checked(skeletons*2));
        var sk=new SkeletalSkeletonReference[skeletons];
        for(int i=0;i<skeletons;i++)sk[i]=new(Path(pairs[i*2]),pairs[i*2+1]);
        List<SkeletalAnimationReference> lat=new();List<SkeletalDistanceInterval> bands=new();bool? sfsk=null,apag=null;
        int cursor=3;
        if(cursor<parts.Count&&parts[cursor].Tag=="LATX")
        {
            var payload=parts[cursor++].Payload.Span;Require(payload.Length>=2,"Truncated LATX count.");
            int count=Count(U16(payload),64);var values=Strings(payload[2..],count*2);
            for(int i=0;i<count;i++)lat.Add(new(Path(values[i*2]),Path(values[i*2+1])));
        }
        if(cursor<parts.Count&&parts[cursor].IsForm("LDTB"))
        {
            var form=parts[cursor++];Require(form.Children.Count==1&&form.Children[0].IsForm("0000"),"Unsupported skeletal distance version.");
            var v=form.Children[0];Require(v.Children.Count==1&&v.Children[0].Tag=="INFO","Invalid skeletal distance envelope.");
            var payload=v.Children[0].Payload.Span;Require(payload.Length>=2,"Truncated distance count.");int count=Count(U16(payload),64);
            Require(payload.Length==2+count*8,"Invalid distance table width.");
            for(int i=0;i<count;i++)
            {
                float lower=F32(payload[(2+i*8)..]),upper=F32(payload[(6+i*8)..]);
                Require(float.IsFinite(lower)&&float.IsFinite(upper),"Nonfinite skeletal distance.");bands.Add(new(lower,upper));
            }
        }
        if(cursor<parts.Count&&parts[cursor].Tag=="SFSK")sfsk=Flag(parts[cursor++]);
        if(cursor<parts.Count&&parts[cursor].Tag=="APAG")apag=Flag(parts[cursor++]);
        Require(cursor==parts.Count,"Unknown, duplicate or unordered skeletal appearance suffix.");
        return new(info[8]!=0,Array.AsReadOnly(meshPaths),Array.AsReadOnly(sk),lat.AsReadOnly(),bands.AsReadOnly(),sfsk,apag);
    }

    public static LodMeshGeneratorDescription DecodeLodMesh(ReadOnlySpan<byte> bytes)
    {
        var parts=Version(bytes,"MLOD","0000").Children;
        Require(parts.Count>=1&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==2,"Invalid MLOD count.");
        int serialized=BinaryPrimitives.ReadInt16LittleEndian(parts[0].Payload.Span);
        int count=Math.Max(0,serialized);Require(count<=64&&parts.Count==count+1,"Invalid MLOD name count/budget.");
        var paths=new string[count];
        for(int i=0;i<count;i++){Require(parts[i+1].Tag=="NAME","Invalid MLOD name record.");paths[i]=Path(Strings(parts[i+1].Payload.Span,1)[0]);}
        return new(Array.AsReadOnly(paths));
    }

    internal static IffNode Version(ReadOnlySpan<byte> bytes,string rootTag,string version)
    {
        var root=IffDocument.Parse(bytes,new(MaximumBytes:1048576,MaximumNodes:8192,MaximumDepth:8)).Root;
        Require(root.IsForm(rootTag)&&root.Children.Count==1&&root.Children[0].IsForm(version),"Unsupported "+rootTag+" envelope/version.");return root.Children[0];
    }
    internal static string[] Strings(ReadOnlySpan<byte> payload,int count)
    {
        var result=new string[count];int cursor=0;
        for(int i=0;i<count;i++)
        {
            int length=payload[cursor..].IndexOf((byte)0);Require(length>=0&&length<=1024,"Invalid skeletal string extent.");
            var text=payload.Slice(cursor,length);foreach(byte b in text)Require(b>=32&&b<=126,"Invalid skeletal string encoding.");
            result[i]=Encoding.ASCII.GetString(text);cursor+=length+1;
        }
        Require(cursor==payload.Length,"Trailing skeletal string bytes.");return result;
    }
    private static string Path(string value){Require(value.Length>0,"Empty skeletal resource path.");return LegacyAssetPath.Normalize(value);}
    private static bool Flag(IffNode n){Require(n.Payload.Length==1&&n.Payload.Span[0]<=1,"Invalid skeletal Boolean.");return n.Payload.Span[0]!=0;}
    private static int Count(uint value,int maximum){Require(value<=maximum,"Skeletal count budget.");return (int)value;}
    private static ushort U16(ReadOnlySpan<byte> v)=>BinaryPrimitives.ReadUInt16LittleEndian(v);
    private static uint U32(ReadOnlySpan<byte> v)=>BinaryPrimitives.ReadUInt32LittleEndian(v);
    private static float F32(ReadOnlySpan<byte> v)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(v));
    internal static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
