using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct SkeletalBlendDelta(uint Index,Vector3 Delta);
public sealed record SkeletalBlendTarget(string VariableName,IReadOnlyList<SkeletalBlendDelta> Positions,
    IReadOnlyList<SkeletalBlendDelta> Normals,IReadOnlyList<SkeletalBlendDelta> Tangents,IffNode? HardpointMetadata);
public sealed record SkeletalBlendShapeDescription(IReadOnlyList<SkeletalBlendTarget> Targets);

/// <summary>Owned deployed SKMG0004 BLTS/BLT sparse arrays; optional hardpoint payload stays explicit metadata.</summary>
public static class SkeletalBlendShapeDecoder
{
    public static SkeletalBlendShapeDescription Create(SkeletalMeshGeometryDescription geometry,int maximumDeltas=1_048_576)
    {
        ArgumentNullException.ThrowIfNull(geometry);if(maximumDeltas is <0 or >4_194_304)throw new ArgumentOutOfRangeException(nameof(maximumDeltas));
        var forms=geometry.Skin.UninterpretedSuffix.Where(n=>n.IsForm("BLTS")).ToArray();
        int count=geometry.Skin.InfoWords[8];Need(count is >=0 and <=4096&&forms.Length<=1,"Invalid blend form/count.");
        if(forms.Length==0){Need(count==0,"Missing declared blend form.");return new(Array.Empty<SkeletalBlendTarget>());}
        Need(forms[0].Children.Count==count,"Blend target count differs.");var targets=new List<SkeletalBlendTarget>();int used=0;
        foreach(var target in forms[0].Children)
        {
            Need(target.IsForm("BLT ")&&target.Children.Count>0&&target.Children[0].Tag=="INFO","Invalid blend target prefix.");
            var parts=target.Children;var info=parts[0].Payload.Span;Need(info.Length is >=9 and <=73,"Invalid blend INFO extent.");
            int positionCount=Count(I32(info),maximumDeltas-used),normalCount=Count(I32(info[4..]),maximumDeltas-used-positionCount);
            string name=SkeletalAppearanceDecoder.Strings(info[8..],1)[0];Need(name.Length is >0 and <=64,"Invalid blend variable name.");
            int cursor=1;IReadOnlyList<SkeletalBlendDelta> Read(string tag,int declared,int bound)
            {
                if(cursor>=parts.Count||parts[cursor].Tag!=tag){Need(declared==0,"Missing declared blend array.");return Array.Empty<SkeletalBlendDelta>();}
                var raw=parts[cursor++].Payload.Span;used+=declared;Need(used<=maximumDeltas,"Blend delta budget.");return Deltas(raw,declared,bound);
            }
            var positions=Read("POSN",positionCount,geometry.Skin.Positions.Count);var normals=Read("NORM",normalCount,geometry.Skin.Normals.Count);
            IReadOnlyList<SkeletalBlendDelta> tangents=Array.Empty<SkeletalBlendDelta>();
            if(cursor<parts.Count&&parts[cursor].Tag=="DOT3")
            {
                var raw=parts[cursor++].Payload.Span;Need(raw.Length>=4,"Truncated blend tangent count.");int tangentCount=Count(I32(raw),maximumDeltas-used);used+=tangentCount;
                tangents=Deltas(raw[4..],tangentCount,geometry.Tangents.Count);
            }
            IffNode? hardpoints=null;if(cursor<parts.Count&&parts[cursor].Tag=="HPTS")hardpoints=parts[cursor++];
            Need(cursor==parts.Count,"Unknown/unordered blend suffix.");targets.Add(new(name,positions,normals,tangents,hardpoints));
        }
        return new(targets.AsReadOnly());
    }
    private static IReadOnlyList<SkeletalBlendDelta> Deltas(ReadOnlySpan<byte> raw,int count,int bound)
    {
        Need(raw.Length==count*16,"Invalid blend sparse-vector width.");var result=new SkeletalBlendDelta[count];
        for(int i=0;i<count;i++)
        {
            var record=raw[(i*16)..];uint index=BinaryPrimitives.ReadUInt32LittleEndian(record);var delta=new Vector3(F32(record[4..]),F32(record[8..]),F32(record[12..]));
            Need(index<bound&&float.IsFinite(delta.X)&&float.IsFinite(delta.Y)&&float.IsFinite(delta.Z)&&Math.Abs(delta.X)<=1e6&&Math.Abs(delta.Y)<=1e6&&Math.Abs(delta.Z)<=1e6,"Invalid blend sparse index/vector.");
            result[i]=new(index,delta);
        }
        return Array.AsReadOnly(result);
    }
    private static int Count(int count,int maximum){Need(count>=0&&count<=maximum,"Blend delta count budget.");return count;}
    private static int I32(ReadOnlySpan<byte> raw)=>BinaryPrimitives.ReadInt32LittleEndian(raw);
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(I32(raw));
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
}
