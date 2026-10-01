using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct SkeletalTransformWeight(uint TransformIndex,float Weight);
public readonly record struct SkeletalWeightRange(int Start,int Count);
public sealed record SkeletalMeshSkinSourceDescription(IReadOnlyList<int> InfoWords,IReadOnlyList<short> InfoTail,
    IReadOnlyList<string> SkeletonPaths,IReadOnlyList<string> TransformNames,IReadOnlyList<Vector3> Positions,
    IReadOnlyList<SkeletalWeightRange> WeightRanges,IReadOnlyList<SkeletalTransformWeight> Weights,
    IReadOnlyList<Vector3> Normals,IReadOnlyList<IffNode> UninterpretedSuffix);

/// <summary>Owned SKMG 0004 skin-source prefix. Suffix records remain owned metadata, not validated generated geometry.</summary>
public static class SkeletalMeshSkinSourceDecoder
{
    public static SkeletalMeshSkinSourceDescription Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source).Root;
        Require(root.IsForm("SKMG")&&root.Children.Count==1&&root.Children[0].IsForm("0004"),"Unsupported skin-source envelope/version.");
        var parts=root.Children[0].Children;string[] prefix={"INFO","SKTM","XFNM","POSN","TWHD","TWDT"};
        Require(parts.Count>=prefix.Length&&parts.Take(prefix.Length).Select(p=>p.Tag).SequenceEqual(prefix),"Invalid skin-source prefix order.");
        var info=parts[0].Payload.Span;Require(info.Length==44,"Invalid skin-source INFO width.");
        int[] words=new int[9];short[] tail=new short[4];
        for(int i=0;i<9;i++)words[i]=I32(info[(i*4)..]);for(int i=0;i<4;i++)tail[i]=BinaryPrimitives.ReadInt16LittleEndian(info[(36+i*2)..]);
        int skeletonCount=Bound(words[2],64),transformCount=Bound(words[3],2048),positions=Bound(words[4],262144),weightCount=Bound(words[5],1048576),normalCount=Bound(words[6],262144);
        Bound(words[7],4096);Bound(words[8],4096);
        var skeletons=SkeletalAppearanceDecoder.Strings(parts[1].Payload.Span,skeletonCount).Select(p=>{Require(p.Length>0,"Empty mesh skeleton path.");return LegacyAssetPath.Normalize(p);}).ToArray();
        var names=SkeletalAppearanceDecoder.Strings(parts[2].Payload.Span,transformCount);
        Require(names.All(n=>n.Length>0)&&names.Distinct(StringComparer.OrdinalIgnoreCase).Count()==names.Length,"Invalid/ambiguous skin transform names.");
        var points=Vectors(parts[3].Payload.Span,positions);
        var heads=parts[4].Payload.Span;var rawWeights=parts[5].Payload.Span;
        Require(heads.Length==positions*4&&rawWeights.Length==weightCount*8,"Invalid skin weight array widths.");
        var weights=new SkeletalTransformWeight[weightCount];
        for(int i=0;i<weightCount;i++)
        {
            var raw=rawWeights[(i*8)..];uint index=BinaryPrimitives.ReadUInt32LittleEndian(raw);float value=F32(raw[4..]);
            Require(index<transformCount&&float.IsFinite(value)&&value>=0&&value<=1,"Invalid skin transform weight.");weights[i]=new(index,value);
        }
        var ranges=new SkeletalWeightRange[positions];int cursor=0;
        for(int i=0;i<positions;i++)
        {
            int count=Bound(I32(heads[(i*4)..]),64);Require(count>0&&count<=weightCount-cursor,"Invalid per-position skin weight range.");
            double sum=0;for(int j=0;j<count;j++)sum+=weights[cursor+j].Weight;
            Require(Math.Abs(sum-1)<=.0001,"Skin weight sum is outside the declared unit tolerance.");ranges[i]=new(cursor,count);cursor+=count;
        }
        Require(cursor==weightCount,"Unused skin weight data.");
        int next=6;Vector3[] normals=Array.Empty<Vector3>();
        if(next<parts.Count&&parts[next].Tag=="NORM")normals=Vectors(parts[next++].Payload.Span,normalCount);
        return new(Array.AsReadOnly(words),Array.AsReadOnly(tail),Array.AsReadOnly(skeletons),Array.AsReadOnly(names),Array.AsReadOnly(points),Array.AsReadOnly(ranges),Array.AsReadOnly(weights),Array.AsReadOnly(normals),Array.AsReadOnly(parts.Skip(next).ToArray()));
    }
    private static Vector3[] Vectors(ReadOnlySpan<byte> raw,int count)
    {
        Require(raw.Length==count*12,"Invalid skin vector width.");var values=new Vector3[count];
        for(int i=0;i<count;i++)
        {
            var p=raw[(i*12)..];values[i]=new(F32(p),F32(p[4..]),F32(p[8..]));
            Require(float.IsFinite(values[i].X)&&float.IsFinite(values[i].Y)&&float.IsFinite(values[i].Z)&&Math.Abs(values[i].X)<=1e6&&Math.Abs(values[i].Y)<=1e6&&Math.Abs(values[i].Z)<=1e6,"Invalid skin vector.");
        }
        return values;
    }
    private static int Bound(int value,int maximum){Require(value>=0&&value<=maximum,"Skin-source count budget.");return value;}
    private static int I32(ReadOnlySpan<byte> raw)=>BinaryPrimitives.ReadInt32LittleEndian(raw);
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(I32(raw));
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
