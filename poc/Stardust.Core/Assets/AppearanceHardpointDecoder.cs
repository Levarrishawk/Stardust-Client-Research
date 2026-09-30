using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record AppearanceHardpoint(string Name,uint NameChecksum,AppearanceTransform LocalTransform,int AuthoredIndex);
public sealed class AppearanceHardpointTable
{
    internal AppearanceHardpointTable(IReadOnlyList<AppearanceHardpoint> authored)
    {
        Authored=authored;
        Sorted=Array.AsReadOnly(authored.OrderBy(value=>value.NameChecksum).ThenBy(value=>value.Name,StringComparer.Ordinal).ThenBy(value=>value.AuthoredIndex).ToArray());
        DuplicateNames=Array.AsReadOnly(authored.GroupBy(value=>value.Name,StringComparer.Ordinal).Where(group=>group.Count()>1).Select(group=>group.Key).OrderBy(value=>value,StringComparer.Ordinal).ToArray());
    }
    public IReadOnlyList<AppearanceHardpoint> Authored{get;}
    public IReadOnlyList<AppearanceHardpoint> Sorted{get;}
    public IReadOnlyList<string> DuplicateNames{get;}
    public IReadOnlyList<AppearanceHardpoint> FindAll(string name)
    {
        byte[] bytes=AppearanceHardpointDecoder.NameBytes(name);uint checksum=LegacyAssetPath.ChecksumBytes(bytes);
        int low=0,high=Sorted.Count;
        while(low<high)
        {
            int mid=low+(high-low)/2;AppearanceHardpoint value=Sorted[mid];
            int comparison=value.NameChecksum.CompareTo(checksum);
            if(comparison==0)comparison=StringComparer.Ordinal.Compare(value.Name,name);
            if(comparison<0)low=mid+1;else high=mid;
        }
        List<AppearanceHardpoint> matches=new();
        for(int index=low;index<Sorted.Count && Sorted[index].NameChecksum==checksum && Sorted[index].Name==name;index++)matches.Add(Sorted[index]);
        return matches.AsReadOnly();
    }
    /// <summary>Ambiguous duplicate names remain explicit; this API never chooses an unverified original tie.</summary>
    public bool TryFindUnique(string name,out AppearanceHardpoint? hardpoint)
    {IReadOnlyList<AppearanceHardpoint> matches=FindAll(name);hardpoint=matches.Count==1?matches[0]:null;return hardpoint is not null;}
}

public static class AppearanceHardpointDecoder
{
    public static AppearanceHardpointTable DecodeBase(IffNode appearance)
    {
        Require(appearance.IsForm("APPR") && appearance.Children.Count==1 && appearance.Children[0].IsForm("0003"),"Unsupported hardpoint base version.");
        IffNode[] forms=appearance.Children[0].Children.Where(node=>node.IsForm("HPTS")).ToArray();
        Require(forms.Length<=1,"Duplicate HPTS form.");List<AppearanceHardpoint> result=new();
        if(forms.Length==0)return new(result.AsReadOnly());
        Require(forms[0].Children.Count<=4096,"Hardpoint record budget exceeded.");
        foreach(IffNode record in forms[0].Children)
        {
            ReadOnlySpan<byte> bytes=record.Payload.Span;
            Require(record.Tag=="HPNT" && bytes.Length is >=49 and <=304,"Invalid HPNT extent.");
            ReadOnlySpan<byte> name=bytes[48..];Require(name[^1]==0 && name[..^1].IndexOf((byte)0)<0,"Invalid hardpoint terminator.");
            foreach(byte value in name[..^1])Require(value is >=32 and <=126,"Hardpoint name outside printable ASCII slice.");
            float[] transform=new float[12];
            for(int field=0;field<12;field++)
            {transform[field]=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[(field*4)..]));Require(float.IsFinite(transform[field]),"Nonfinite hardpoint transform.");}
            AppearanceTransform local=new(new(transform[0],transform[1],transform[2]),new(transform[4],transform[5],transform[6]),
                new(transform[8],transform[9],transform[10]),new(transform[3],transform[7],transform[11]));
            result.Add(new(Encoding.ASCII.GetString(name[..^1]),LegacyAssetPath.ChecksumBytes(name[..^1]),local,result.Count));
        }
        return new(result.AsReadOnly());
    }
    internal static byte[] NameBytes(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if(name.Length>255 || name.Any(value=>value is <' ' or >'~'))throw new ArgumentException("Hardpoint query outside printable ASCII slice.",nameof(name));
        return Encoding.ASCII.GetBytes(name);
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
