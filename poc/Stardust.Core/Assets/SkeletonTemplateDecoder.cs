using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct ScalarFirstQuaternion(float W,float X,float Y,float Z);
public sealed record SkeletonJoint(string Name,int Parent,ScalarFirstQuaternion Rpre,ScalarFirstQuaternion Rpst,
    Vector3 Bptr,ScalarFirstQuaternion Bpro,uint Jror);
public sealed record BasicSkeletonDescription(IReadOnlyList<SkeletonJoint> Joints)
{
    /// <summary>Explicit ASCII-insensitive adapter lookup. Authored names remain unchanged; ambiguous matches reject.</summary>
    public int FindTransform(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length>1024||name.Any(c=>c<32||c>126))throw new ArgumentException("Bounded ASCII transform name required.");
        int found=-1;
        for(int i=0;i<Joints.Count;i++)if(string.Equals(Joints[i].Name,name,StringComparison.OrdinalIgnoreCase))
        {
            if(found>=0)throw new InvalidDataException("Ambiguous transform-name match.");found=i;
        }
        return found;
    }
}
public sealed record SkeletonTemplateDescription(IReadOnlyList<BasicSkeletonDescription> SerializedLevels)
{
    public IReadOnlyList<BasicSkeletonDescription> ExposedLevels(bool skipL0=false)
        =>Array.AsReadOnly(SerializedLevels.Skip(skipL0&&SerializedLevels.Count>1?1:0).Take(4).ToArray());
}

/// <summary>Owned deployed SKTM 0002/SLOD 0000; named arrays and scalar-first components remain in legacy coordinates.</summary>
public static class SkeletonTemplateDecoder
{
    public static SkeletonTemplateDescription Decode(ReadOnlySpan<byte> bytes)
    {
        var root=IffDocument.Parse(bytes,new(MaximumBytes:1048576,MaximumNodes:8192,MaximumDepth:8)).Root;
        if(root.IsForm("SKTM"))return new(Array.AsReadOnly(new[]{Basic(root)}));
        Require(root.IsForm("SLOD")&&root.Children.Count==1&&root.Children[0].IsForm("0000"),"Unsupported skeleton envelope/version.");
        var parts=root.Children[0].Children;
        Require(parts.Count>=1&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==2,"Invalid skeleton LOD count.");
        int count=Math.Max(0,(int)BinaryPrimitives.ReadInt16LittleEndian(parts[0].Payload.Span));
        Require(count<=64&&parts.Count==count+1,"Invalid skeleton LOD count/budget.");
        var levels=new BasicSkeletonDescription[count];int total=0;
        for(int i=0;i<count;i++){levels[i]=Basic(parts[i+1]);total=checked(total+levels[i].Joints.Count);Require(total<=8192,"Skeleton joint aggregate budget.");}
        return new(Array.AsReadOnly(levels));
    }
    private static BasicSkeletonDescription Basic(IffNode root)
    {
        Require(root.IsForm("SKTM")&&root.Children.Count==1&&root.Children[0].IsForm("0002"),"Unsupported inline skeleton version.");
        var parts=root.Children[0].Children;string[] tags={"INFO","NAME","PRNT","RPRE","RPST","BPTR","BPRO","JROR"};
        Require(parts.Count==tags.Length&&parts.Select(p=>p.Tag).SequenceEqual(tags),"Invalid skeleton field order.");
        Require(parts[0].Payload.Length==4,"Invalid skeleton transform count width.");uint value=BinaryPrimitives.ReadUInt32LittleEndian(parts[0].Payload.Span);
        Require(value<=2048,"Skeleton transform budget.");int count=(int)value;
        var names=SkeletalAppearanceDecoder.Strings(parts[1].Payload.Span,count);
        Require(names.All(n=>n.Length>0)&&names.Distinct(StringComparer.Ordinal).Count()==count,"Invalid/duplicate skeleton transform names.");
        int[] strides={4,16,16,12,16,4};for(int i=2;i<8;i++)Require(parts[i].Payload.Length==count*strides[i-2],"Invalid skeleton array width.");
        var joints=new SkeletonJoint[count];int roots=0;
        for(int i=0;i<count;i++)
        {
            int parent=BinaryPrimitives.ReadInt32LittleEndian(parts[2].Payload.Span[(i*4)..]);
            Require(parent>=-1&&parent<i,"Skeleton parent must precede child.");if(parent==-1)roots++;
            var translation=parts[5].Payload.Span[(i*12)..];Vector3 bptr=new(F32(translation),F32(translation[4..]),F32(translation[8..]));
            Require(float.IsFinite(bptr.X)&&float.IsFinite(bptr.Y)&&float.IsFinite(bptr.Z)&&Math.Abs(bptr.X)<=1e6&&Math.Abs(bptr.Y)<=1e6&&Math.Abs(bptr.Z)<=1e6,"Invalid skeleton translation.");
            joints[i]=new(names[i],parent,Q(parts[3],i),Q(parts[4],i),bptr,Q(parts[6],i),BinaryPrimitives.ReadUInt32LittleEndian(parts[7].Payload.Span[(i*4)..]));
        }
        Require(count==0||roots==1,"Nonempty skeleton requires one root.");return new(Array.AsReadOnly(joints));
    }
    private static ScalarFirstQuaternion Q(IffNode node,int i)
    {
        var raw=node.Payload.Span[(i*16)..];var q=new ScalarFirstQuaternion(F32(raw),F32(raw[4..]),F32(raw[8..]),F32(raw[12..]));
        double norm=(double)q.W*q.W+(double)q.X*q.X+(double)q.Y*q.Y+(double)q.Z*q.Z;
        Require(double.IsFinite(norm)&&Math.Abs(norm-1)<=.0001,"Skeleton quaternion must be finite and unit length.");return q;
    }
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw));
    private static void Require(bool value,string text)=>SkeletalAppearanceDecoder.Require(value,text);
}
