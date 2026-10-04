using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record TerrainGeneratorInputLimits(int MaximumInputBytes=8388608,
    int MaximumNodes=50000,int MaximumDepth=64,int MaximumLayers=4096,
    int MaximumLayerDepth=32,int MaximumRules=32768,int MaximumGroupRecords=16384,
    int MaximumStringBytes=4096,int MaximumTotalStringBytes=1048576,int MaximumSectionBytes=4194304);

/// <summary>Immutable section over an inaccessible owned generator snapshot.</summary>
public sealed class TerrainGeneratorSection
{
    private readonly byte[] source;
    internal TerrainGeneratorSection(byte[] source,IffNode node)
    {
        this.source=source;Tag=node.Tag;FormType=node.FormType;Offset=node.Offset;
        EncodedBytes=node.Payload.Length+(node.FormType is null?8:12);
        Version=node.Children.Count==1&&node.Children[0].FormType is not null?node.Children[0].FormType:null;
    }
    public string Tag { get; }
    public string? FormType { get; }
    public string? Version { get; }
    public int Offset { get; }
    public int EncodedBytes { get; }
    public byte[] CopyEncodedBytes()=>source.AsSpan(Offset,EncodedBytes).ToArray();
}

public enum TerrainGeneratorGroupRole { Shader,Flora,Radial,Environment,FractalMap,Bitmap }
public sealed class TerrainGeneratorGroup
{
    internal TerrainGeneratorGroup(TerrainGeneratorGroupRole role,TerrainGeneratorSection section,bool recognized,int records)
    { Role=role;Section=section;ClientVersionRecognized=recognized;RecordCount=records; }
    public TerrainGeneratorGroupRole Role { get; }
    public TerrainGeneratorSection Section { get; }
    public bool ClientVersionRecognized { get; }
    public int RecordCount { get; }
}

public sealed class TerrainRuleHeader
{
    private readonly byte[] name,legacy;
    internal TerrainRuleHeader(string version,uint enabled,byte[] name,byte[] legacy)
    { Version=version;EnabledBits=enabled;this.name=name;this.legacy=legacy; }
    public string Version { get; }
    public uint EnabledBits { get; }
    public bool IsEnabled=>EnabledBits!=0;
    public byte[] CopyNameBytes()=>(byte[])name.Clone();
    /// <summary>Three discarded bytes in IHDR/0000; empty for0001.</summary>
    public byte[] CopyLegacyBytes()=>(byte[])legacy.Clone();
}

public sealed class TerrainHeightConstantInput
{
    internal TerrainHeightConstantInput(TerrainRuleHeader header,uint operation,uint height)
    { Header=header;OperationBits=operation;HeightBits=height; }
    public TerrainRuleHeader Header { get; }
    public uint OperationBits { get; }
    public int Operation=>unchecked((int)OperationBits);
    public uint HeightBits { get; }
    public float Height=>BitConverter.UInt32BitsToSingle(HeightBits);
    public bool HasFiniteHeight=>float.IsFinite(Height);
}

public sealed class TerrainGeneratorRule
{
    internal TerrainGeneratorRule(int layer,int ordinal,TerrainGeneratorSection section,TerrainHeightConstantInput? height)
    { LayerIndex=layer;ChildOrdinal=ordinal;Section=section;HeightConstant=height; }
    public int LayerIndex { get; }
    public int ChildOrdinal { get; }
    public TerrainGeneratorSection Section { get; }
    public TerrainHeightConstantInput? HeightConstant { get; }
}

public sealed class TerrainGeneratorLayer
{
    private readonly uint[] flags;
    private readonly byte[] description;
    internal TerrainGeneratorLayer(int index,int? parent,int depth,TerrainGeneratorSection section,
        TerrainRuleHeader? header,uint[] flags,byte[] description,TerrainGeneratorSection[] children)
    { Index=index;ParentIndex=parent;Depth=depth;Section=section;Header=header;this.flags=flags;this.description=description;Children=Array.AsReadOnly(children); }
    public int Index { get; }
    /// <summary>Encoded containment parent only; no inherited activation/evaluation policy.</summary>
    public int? ParentIndex { get; }
    public int Depth { get; }
    public TerrainGeneratorSection Section { get; }
    public TerrainRuleHeader? Header { get; }
    public bool HasDecodedStructure=>Header is not null;
    public IReadOnlyList<TerrainGeneratorSection> Children { get; }
    public uint[] CopyAdtaWordBits()=>(uint[])flags.Clone();
    public byte[] CopyDescriptionBytes()=>(byte[])description.Clone();
}

/// <summary>Owned TGEN framing and one typed rule family; all generator evaluation remains unsupported.</summary>
public sealed class TerrainGeneratorInput
{
    private readonly byte[] source;
    private TerrainGeneratorInput(byte[] source,TerrainGeneratorGroup[] groups,TerrainGeneratorLayer[] layers,TerrainGeneratorRule[] rules,int records,int strings)
    { this.source=source;Groups=Array.AsReadOnly(groups);Layers=Array.AsReadOnly(layers);Rules=Array.AsReadOnly(rules);GroupRecordCount=records;DecodedStringBytes=strings; }
    public IReadOnlyList<TerrainGeneratorGroup> Groups { get; }
    public IReadOnlyList<TerrainGeneratorLayer> Layers { get; }
    public IReadOnlyList<TerrainGeneratorRule> Rules { get; }
    public int GroupRecordCount { get; }
    public int DecodedStringBytes { get; }
    public int HeightConstantCount=>Rules.Count(r=>r.HeightConstant is not null);
    public byte[] CopyGeneratorForm()=>(byte[])source.Clone();
    public static TerrainGeneratorInput Decode(ReadOnlySpan<byte> generatorForm,TerrainGeneratorInputLimits? limits=null)
    {
        limits??=new();
        if(limits.MaximumInputBytes is <24 or >67108864 || limits.MaximumNodes is <1 or >1000000 ||
            limits.MaximumDepth is <1 or >128 || limits.MaximumLayers is <0 or >65536 ||
            limits.MaximumLayerDepth is <0 or >64 || limits.MaximumRules is <0 or >262144 ||
            limits.MaximumGroupRecords is <0 or >262144 || limits.MaximumStringBytes is <0 or >65536 ||
            limits.MaximumTotalStringBytes is <0 or >16777216 || limits.MaximumSectionBytes is <12 or >33554432)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Need(generatorForm.Length<=limits.MaximumInputBytes,"Generator input budget.");byte[] owned=generatorForm.ToArray();
        var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("TGEN")&&root.Children.Count==1&&root.Children[0].IsForm("0000"),"Exact TGEN/0000 required.");
        var children=root.Children[0].Children;Need(children.Count>=5,"Bounded profile requires five ordered groups.");
        var groups=new List<TerrainGeneratorGroup>();var layers=new List<TerrainGeneratorLayer>();var rules=new List<TerrainGeneratorRule>();
        int groupRecords=0,stringBytes=0;
        TerrainGeneratorSection Section(IffNode node)
        { var s=new TerrainGeneratorSection(owned,node);Need(s.EncodedBytes<=limits.MaximumSectionBytes,"Individual section budget.");return s; }
        byte[] String(byte[] raw,ref int cursor)
        {
            int end=raw.AsSpan(cursor).IndexOf((byte)0);Need(end>=0&&end<=limits.MaximumStringBytes,"Bounded string terminator.");
            Need((long)stringBytes+end+1<=limits.MaximumTotalStringBytes,"Combined decoded string budget.");
            byte[] result=raw.AsSpan(cursor,end).ToArray();cursor+=end+1;stringBytes+=end+1;return result;
        }
        TerrainRuleHeader? Header(IffNode node)
        {
            Need(node.IsForm("IHDR")&&node.Children.Count==1&&node.Children[0].FormType is not null,"Inherited IHDR version form.");
            var version=node.Children[0];if(!version.IsForm("0000")&&!version.IsForm("0001"))return null;
            Need(version.Children.Count==1&&Leaf(version.Children[0],"DATA"),"IHDR DATA record.");
            byte[] raw=version.Children[0].Payload.ToArray();int cursor=0;uint enabled=Word(raw,ref cursor);byte[] name=String(raw,ref cursor);
            int legacyLength=version.IsForm("0000")?3:0;Need(raw.Length-cursor==legacyLength,"Exact known IHDR DATA extent.");
            return new(version.FormType!,enabled,name,raw.AsSpan(cursor).ToArray());
        }
        TerrainHeightConstantInput? Height(IffNode node)
        {
            Need(node.Children.Count==1&&node.Children[0].FormType is not null,"AHCN version envelope.");
            var version=node.Children[0];if(!version.IsForm("0000"))return null;
            Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&Leaf(version.Children[1],"DATA")&&version.Children[1].Payload.Length==8,"Exact AHCN0000 inherited header/DATA8.");
            var header=Header(version.Children[0]);if(header is null)return null;
            var raw=version.Children[1].Payload.Span;
            return new(header,BinaryPrimitives.ReadUInt32LittleEndian(raw),BinaryPrimitives.ReadUInt32LittleEndian(raw[4..]));
        }
        void Layer(IffNode node,int? parent,int depth)
        {
            Need(depth<=limits.MaximumLayerDepth&&layers.Count<limits.MaximumLayers,"Layer count/containment depth budget.");
            Need(node.IsForm("LAYR")&&node.Children.Count==1&&node.Children[0].FormType is not null,"LAYR version envelope.");
            int index=layers.Count;var section=Section(node);var version=node.Children[0];var contents=version.Children;
            bool supported=version.FormType is "0001" or "0002" or "0003" or "0004";
            if(!supported) { layers.Add(new(index,parent,depth,section,null,Array.Empty<uint>(),Array.Empty<byte>(),Array.Empty<TerrainGeneratorSection>()));return; }
            Need(contents.Count>=2&&contents[0].IsForm("IHDR")&&Leaf(contents[1],"ADTA"),"Known layer IHDR/ADTA order.");
            var header=Header(contents[0]);
            if(header is null) { layers.Add(new(index,parent,depth,section,null,Array.Empty<uint>(),Array.Empty<byte>(),Array.Empty<TerrainGeneratorSection>()));return; }
            byte[] adta=contents[1].Payload.ToArray();int cursor=0;int count=version.IsForm("0001")?2:version.IsForm("0004")?4:3;
            uint[] flags=new uint[count];for(int i=0;i<count;i++)flags[i]=Word(adta,ref cursor);
            byte[] description=version.FormType is "0003" or "0004"?String(adta,ref cursor):Array.Empty<byte>();Need(cursor==adta.Length,"Exact known ADTA extent.");
            var records=contents.Skip(2).Select(Section).ToArray();layers.Add(new(index,parent,depth,section,header,flags,description,records));
            for(int i=2;i<contents.Count;i++)
            {
                var child=contents[i];if(child.IsForm("LAYR"))Layer(child,index,depth+1);
                else { Need(rules.Count<limits.MaximumRules,"Rule record budget.");rules.Add(new(index,i,Section(child),child.IsForm("AHCN")?Height(child):null)); }
            }
        }
        string[] tags={"SGRP","FGRP","RGRP","EGRP","MGRP"};
        for(int i=0;i<5;i++)Group(children[i],i);
        void Group(IffNode node,int role)
        {
            Need(node.IsForm(role==5?"MGRP":tags[role])&&node.Children.Count==1&&node.Children[0].FormType is not null,"Ordered group/version envelope.");
            var version=node.Children[0];groupRecords=checked(groupRecords+version.Children.Count);Need(groupRecords<=limits.MaximumGroupRecords,"Group/family record budget.");
            string v=version.FormType!;bool recognized=role switch {
                0=>v is "0000" or "0001" or "0002" or "0003" or "0004" or "0005" or "0006",
                1=>v is "0001" or "0002" or "0003" or "0004" or "0005" or "0006" or "0007" or "0008",
                2=>v is "0000" or "0001" or "0002" or "0003",3=>v is "0000" or "0001" or "0002",_=>v=="0000"};
            groups.Add(new((TerrainGeneratorGroupRole)role,Section(node),recognized,version.Children.Count));
        }
        int next=5;if(next<children.Count&&children[next].IsForm("MGRP"))Group(children[next++],5);
        if(next<children.Count)
        {
            var lyrs=children[next++];Need(lyrs.IsForm("LYRS"),"Optional LYRS container; direct LAYR not supported by this client path.");
            _=Section(lyrs);foreach(var node in lyrs.Children)Layer(node,null,0);
        }
        Need(next==children.Count,"No trailing generator top-level records.");
        return new(owned,groups.ToArray(),layers.ToArray(),rules.ToArray(),groupRecords,stringBytes);
    }
    private static bool Leaf(IffNode n,string tag)=>n.Tag==tag&&n.FormType is null;
    private static uint Word(byte[] raw,ref int cursor)
    { Need(raw.Length-cursor>=4,"Truncated metadata word.");uint value=BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(cursor));cursor+=4;return value; }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
