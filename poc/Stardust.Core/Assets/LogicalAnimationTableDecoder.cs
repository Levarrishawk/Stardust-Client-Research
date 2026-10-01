using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record LogicalAnimationEntry(string SerializedName,string Name,IffNode? Template,bool Synthetic=false);
public sealed record LogicalAnimationTableDescription(string AssociatedHierarchy,IReadOnlyList<LogicalAnimationEntry> SerializedEntries,
    IReadOnlyList<LogicalAnimationEntry> LookupEntries,LogicalAnimationEntry DefaultEntry)
{
    /// <summary>Explicit modern ASCII-insensitive lookup; original CRC sorting is not reproduced here.</summary>
    public LogicalAnimationEntry Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);name=LogicalAnimationTableDecoder.ValidateName(name).TrimEnd(' ');
        var found=LookupEntries.Where(e=>e.Name.Equals(name,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        if(found.Length>1)throw new InvalidDataException("Ambiguous logical animation lookup.");return found.Length==0?DefaultEntry:found[0];
    }
}

/// <summary>Owned LATT 0000 outer grammar. Typed child templates are retained, not executed/semantically validated.</summary>
public static class LogicalAnimationTableDecoder
{
    public static LogicalAnimationTableDescription Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:2*1024*1024,MaximumNodes:100000,MaximumDepth:32)).Root;
        Require(root.IsForm("LATT")&&root.Children.Count==1&&root.Children[0].IsForm("0000"),"Unsupported logical table envelope/version.");
        var parts=root.Children[0].Children;Require(parts.Count>0&&parts[0].Tag=="INFO","Missing logical table INFO.");
        var info=parts[0].Payload.Span;int end=info.IndexOf((byte)0);Require(end>=0&&info.Length==end+3,"Invalid logical table INFO width.");
        string hierarchy=Text(info[..(end+1)]);if(hierarchy.Length>0)hierarchy=LegacyAssetPath.Normalize(hierarchy);
        int count=BinaryPrimitives.ReadInt16LittleEndian(info[(end+1)..]);Require(count>=0&&count<=4096&&parts.Count==count+1,"Invalid logical animation count.");
        var entries=new LogicalAnimationEntry[count];
        LogicalAnimationEntry? defaultEntry=null;
        for(int i=0;i<count;i++)
        {
            var node=parts[i+1];Require(node.IsForm("ANIM")&&node.Children.Count==2&&node.Children[0].Tag=="INFO"&&node.Children[1].Tag=="FORM","Invalid logical animation entry.");
            string raw=Text(node.Children[0].Payload.Span);string name=raw.TrimEnd(' ');Require(name.Length>0,"Empty logical animation name.");
            entries[i]=new(raw,name,node.Children[1]);if(name=="default")defaultEntry=entries[i];
        }
        if(defaultEntry is null)defaultEntry=new("default","default",null,true);
        var lookup=(defaultEntry.Synthetic?entries.Append(defaultEntry):entries).OrderBy(e=>e.Name,StringComparer.OrdinalIgnoreCase).ToArray();
        return new(hierarchy,Array.AsReadOnly(entries),Array.AsReadOnly(lookup),defaultEntry);
    }
    internal static string ValidateName(string name)
    {
        if(name.Length>2047||name.Any(c=>c<32||c>126))throw new InvalidDataException("Bounded ASCII logical name required.");return name;
    }
    private static string Text(ReadOnlySpan<byte> raw)
    {
        Require(raw.Length>0&&raw.Length<=2048&&raw[^1]==0&&raw[..^1].IndexOf((byte)0)<0,"Invalid logical table string framing.");
        foreach(byte value in raw[..^1])Require(value>=32&&value<=126,"Invalid logical table string encoding.");return ValidateName(Encoding.ASCII.GetString(raw[..^1]));
    }
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
