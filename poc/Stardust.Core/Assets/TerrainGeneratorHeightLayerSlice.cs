using System.Security.Cryptography;
namespace Stardust.Core.Assets;
public enum TerrainGeneratorHeightLayerSlicePolicy { TypedEvidence, EnabledSupportedSlice }
public sealed record TerrainGeneratorHeightLayerSliceLimits(int MaximumLayerBytes=1048576,int MaximumChildBytes=65536,
    int MaximumNameBytes=65536,long MaximumDecodedCopyBytes=1048576,long MaximumSnapshotBytes=8388608,long MaximumWork=8388608);
public sealed record TerrainGeneratorHeightLayerChild(int ChildOrdinal,int GlobalRuleIndex,string FormType,string Version,
    int Offset,int EncodedBytes,string Sha256);
/// <summary>One owned immediate BCIR/FFRA/AHFR layer. No traversal, preparation, sampler or evaluation.</summary>
public sealed class TerrainGeneratorHeightLayerSlice
{
    private readonly byte[] encoded,description;
    private readonly uint[] adta;
    private TerrainGeneratorHeightLayerSlice(TerrainGeneratorInput g,TerrainGeneratorFractalFamilies binding,TerrainGeneratorLayer layer,
        byte[] encoded,TerrainRuleHeader header,uint[] adta,byte[] description,TerrainBoundaryCircleInput circle,
        TerrainGeneratorFractalFilterRule filter,TerrainGeneratorHeightFractalRule height,TerrainGeneratorHeightLayerChild[] children,
        TerrainGeneratorHeightLayerSlicePolicy policy,bool supported,long decoded,long reservedDecoded,long snapshot,long reserved,long work,long reservedWork)
    {
        this.encoded=encoded;this.adta=adta;this.description=description;Header=header;Circle=circle;Filter=filter;Height=height;
        GeneratorSha256=g.SourceSha256;GeneratorBytes=g.EncodedBytes;GroupSha256=binding.GroupSha256;
        GroupOffset=binding.GroupOffset;GroupBytes=binding.GroupBytes;LayerIndex=layer.Index;ParentIndex=layer.ParentIndex;Depth=layer.Depth;
        LayerOffset=layer.Section.Offset;LayerBytes=encoded.Length;LayerSha256=Hash(encoded);Children=Array.AsReadOnly(children);
        Policy=policy;HasSupportedInputs=supported;DecodedCopyBytes=decoded;ReservedDecodedCopyBytes=reservedDecoded;
        SnapshotBytes=snapshot;ReservedSnapshotBytes=reserved;Work=work;ReservedWork=reservedWork;
    }
    public string GeneratorSha256 { get; }
    public int GeneratorBytes { get; }
    public string GroupSha256 { get; }
    public int GroupOffset { get; }
    public int GroupBytes { get; }
    public int LayerIndex { get; }
    public int? ParentIndex { get; }
    public int Depth { get; }
    public int LayerOffset { get; }
    public int LayerBytes { get; }
    public string LayerSha256 { get; }
    public TerrainRuleHeader Header { get; }
    public bool IsLocallyEnabled=>Header.IsEnabled;
    public bool BoundaryInverted=>adta[0]!=0;
    public bool FilterInverted=>adta[1]!=0;
    public uint AdtaWord2Bits=>adta[2];
    public TerrainBoundaryCircleInput Circle { get; }
    public TerrainGeneratorFractalFilterRule Filter { get; }
    public TerrainGeneratorHeightFractalRule Height { get; }
    public IReadOnlyList<TerrainGeneratorHeightLayerChild> Children { get; }
    public TerrainGeneratorHeightLayerSlicePolicy Policy { get; }
    public bool HasSupportedInputs { get; }
    public long DecodedCopyBytes { get; }
    public long ReservedDecodedCopyBytes { get; }
    public long SnapshotBytes { get; }
    public long ReservedSnapshotBytes { get; }
    public long Work { get; }
    public long ReservedWork { get; }
    public byte[] CopyEncodedBytes()=>(byte[])encoded.Clone();
    public uint[] CopyAdtaWordBits()=>(uint[])adta.Clone();
    public byte[] CopyDescriptionBytes()=>(byte[])description.Clone();
    public static TerrainGeneratorHeightLayerSlice Select(TerrainGeneratorInput g,TerrainGeneratorFractalFamilies binding,int layerIndex,
        TerrainGeneratorHeightLayerSlicePolicy policy,TerrainGeneratorHeightLayerSliceLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(g);ArgumentNullException.ThrowIfNull(binding);limits??=new();
        if(policy is not (TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence or TerrainGeneratorHeightLayerSlicePolicy.EnabledSupportedSlice)
            ||limits.MaximumLayerBytes is <24 or >16777216||limits.MaximumChildBytes is <24 or >4194304
            ||limits.MaximumNameBytes is <0 or >65536||limits.MaximumDecodedCopyBytes is <0 or >16777216
            ||limits.MaximumSnapshotBytes is <0 or >134217728||limits.MaximumWork is <0 or >134217728)throw new ArgumentOutOfRangeException(nameof(limits));
        token.ThrowIfCancellationRequested();Need(g.SourceSha256==binding.GeneratorSha256&&g.EncodedBytes==binding.GeneratorBytes,"Exact generator binding.");
        var groups=g.Groups.Where(x=>x.Role==TerrainGeneratorGroupRole.FractalMap).ToArray();
        Need(groups.Length==1,"Exact FractalMap role.");var group=groups[0];var gs=group.Section;
        Need(group.ClientVersionRecognized&&gs.Tag=="FORM"&&gs.FormType=="MGRP"&&gs.Version=="0000"
            &&gs.Offset==binding.GroupOffset&&gs.EncodedBytes==binding.GroupBytes&&group.RecordCount==binding.Families.Entries.Count
            &&gs.EncodedBytes==binding.Families.InputBytes&&gs.Offset>=0&&(long)gs.Offset+gs.EncodedBytes<=g.EncodedBytes,"Exact group extent.");
        if(layerIndex<0||layerIndex>=g.Layers.Count)throw new ArgumentOutOfRangeException(nameof(layerIndex));
        var layer=g.Layers[layerIndex];var ls=layer.Section;
        Need(layer.Index==layerIndex&&ls.Tag=="FORM"&&ls.FormType=="LAYR"&&ls.Version=="0003"&&layer.HasDecodedStructure
            &&layer.Header is not null&&layer.Children.Count==3&&ls.Offset>=0&&(long)ls.Offset+ls.EncodedBytes<=g.EncodedBytes,"Exact typed three-child LAYR0003.");
        string[] tags=["BCIR","FFRA","AHFR"],versions=["0002","0005","0003"];long childBytes=0;
        for(int i=0;i<3;i++)
        {
            var s=layer.Children[i];Need(s.Tag=="FORM"&&s.FormType==tags[i]&&s.Version==versions[i]
                &&s.EncodedBytes>=24&&s.EncodedBytes<=limits.MaximumChildBytes&&s.Offset>=ls.Offset
                &&(long)s.Offset+s.EncodedBytes<=(long)ls.Offset+ls.EncodedBytes,"Immediate child order/version/extent.");
            if(i>0)Need(layer.Children[i-1].Offset+layer.Children[i-1].EncodedBytes==s.Offset,"Contiguous child identity.");childBytes+=s.EncodedBytes;
        }
        Need((long)layer.Children[2].Offset+layer.Children[2].EncodedBytes==(long)ls.Offset+ls.EncodedBytes,"Exact layer tail.");
        long reservedDecoded=ls.EncodedBytes+12L+childBytes;
        long reserved=2L*ls.EncodedBytes+12+4L*childBytes;
        long metadataWork=3L*g.Rules.Count+g.Groups.Count;long reservedWork=reserved+metadataWork;
        // Whole-layer length conservatively bounds each header/description name BEFORE any export.
        Need(ls.EncodedBytes<=limits.MaximumLayerBytes&&ls.EncodedBytes<=limits.MaximumNameBytes
            &&reservedDecoded<=limits.MaximumDecodedCopyBytes&&reserved<=limits.MaximumSnapshotBytes
            &&reservedWork<=limits.MaximumWork,"Whole-slice pre-copy reservation.");
        int[] indices=new int[3];
        for(int i=0;i<3;i++)
        {
            int match=-1;var s=layer.Children[i];
            for(int j=0;j<g.Rules.Count;j++)
            {
                var r=g.Rules[j];if(r.LayerIndex!=layerIndex||r.ChildOrdinal!=i+2)continue;
                Need(match<0&&r.Section.Offset==s.Offset&&r.Section.EncodedBytes==s.EncodedBytes
                    &&r.Section.Tag==s.Tag&&r.Section.FormType==s.FormType&&r.Section.Version==s.Version,"Exact global rule identity.");match=j;
            }
            Need(match>=0,"Missing immediate global rule.");indices[i]=match;
        }
        token.ThrowIfCancellationRequested();byte[] owned=ls.CopyEncodedBytes();uint[] adta=layer.CopyAdtaWordBits();
        Need(adta.Length==3,"Exact ADTA words.");byte[] name=layer.Header!.CopyNameBytes(),legacy=layer.Header.CopyLegacyBytes(),description=layer.CopyDescriptionBytes();
        var header=new TerrainRuleHeader(layer.Header.Version,layer.Header.EnabledBits,name,legacy);
        byte[] circleBytes=layer.Children[0].CopyEncodedBytes();
        var circle=TerrainBoundaryCircleInput.Decode(circleBytes,new(limits.MaximumChildBytes,64,8,limits.MaximumNameBytes));
        Need(circle.Version=="0002"&&circle.Header is not null&&circle.Fields is not null,"Typed BCIR header.");
        bool operational=policy==TerrainGeneratorHeightLayerSlicePolicy.EnabledSupportedSlice;
        var filter=TerrainGeneratorFractalFilterRule.Select(g,binding,indices[1],operational?TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset:TerrainGeneratorFractalFilterRulePolicy.TypedEvidence,
            new(limits.MaximumChildBytes,limits.MaximumChildBytes,limits.MaximumNameBytes,Math.Min(16777216L,limits.MaximumSnapshotBytes),Math.Min(16777216L,limits.MaximumWork)),token);
        var height=TerrainGeneratorHeightFractalRule.Select(g,binding,indices[2],operational?TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset:TerrainGeneratorHeightFractalRulePolicy.TypedEvidence,
            new(limits.MaximumChildBytes,limits.MaximumChildBytes,limits.MaximumNameBytes,Math.Min(16777216L,limits.MaximumSnapshotBytes),Math.Min(16777216L,limits.MaximumWork)),token);
        bool supported=circle.Header!.IsEnabled&&circle.Fields!.HasFiniteGeometry&&circle.Fields.HasFiniteSerializedFeatheringAmount==true
            &&filter.IsEnabled&&filter.HasOperationalInputs&&height.IsEnabled&&height.HasOperationalInputs;
        Need(!operational||(header.IsEnabled&&supported),"Explicit locally enabled supported-slice policy.");
        long circleDecoded=20L+circle.Header.CopyNameBytes().Length+circle.Header.CopyLegacyBytes().Length;
        long layerDecoded=name.Length+legacy.Length+description.Length+12L;
        long decoded=layerDecoded+circleDecoded+filter.Input.DecodedCopyBytes+height.Input.DecodedCopyBytes;
        // Charge the two temporary circle name/legacy exports used for exact accounting as well.
        long snapshot=owned.Length+layerDecoded+3L*circleBytes.Length+circleDecoded*2-20+filter.SnapshotBytes+height.SnapshotBytes;
        Need(decoded<=reservedDecoded&&snapshot<=reserved,"Whole-slice accounting invariant.");
        var children=new TerrainGeneratorHeightLayerChild[3];
        children[0]=new(2,indices[0],tags[0],versions[0],layer.Children[0].Offset,circleBytes.Length,Hash(circleBytes));
        children[1]=new(3,indices[1],tags[1],versions[1],filter.RuleOffset,filter.RuleBytes,filter.RuleSha256);
        children[2]=new(4,indices[2],tags[2],versions[2],height.RuleOffset,height.RuleBytes,height.RuleSha256);
        token.ThrowIfCancellationRequested();return new(g,binding,layer,owned,header,adta,description,circle,filter,height,children,
            policy,supported,decoded,reservedDecoded,snapshot,reserved,snapshot+metadataWork,reservedWork);
    }
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
}
