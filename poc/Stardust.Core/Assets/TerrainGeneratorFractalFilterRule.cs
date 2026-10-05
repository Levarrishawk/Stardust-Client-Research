using System.Security.Cryptography;
namespace Stardust.Core.Assets;

public enum TerrainGeneratorFractalFilterRulePolicy { TypedEvidence, EnabledOperationalSubset }
public sealed record TerrainGeneratorFractalFilterRuleLimits(int MaximumRuleBytes=65536,
    int MaximumDecodedCopyBytes=65536,int MaximumNameBytes=4096,
    long MaximumSnapshotBytes=1048576,long MaximumWork=1048576);

/// <summary>One explicit rule selection; no ancestor activation, sampler, traversal or fallback.</summary>
public sealed class TerrainGeneratorFractalFilterRule
{
    private TerrainGeneratorFractalFilterRule(TerrainGeneratorInput generator,TerrainGeneratorFractalFamilies binding,
        TerrainGeneratorRule selected,int index,TerrainFilterFractalInput input,TerrainFractalFamilyEntry family,
        string hash,TerrainGeneratorFractalFilterRulePolicy policy,bool operational,long reserved)
    {
        GeneratorSha256=generator.SourceSha256;GeneratorBytes=generator.EncodedBytes;
        GroupSha256=binding.GroupSha256;GroupOffset=binding.GroupOffset;GroupBytes=binding.GroupBytes;
        RuleIndex=index;LayerIndex=selected.LayerIndex;ChildOrdinal=selected.ChildOrdinal;
        RuleOffset=selected.Section.Offset;RuleBytes=selected.Section.EncodedBytes;RuleSha256=hash;
        Input=input;Family=family;Policy=policy;HasOperationalInputs=operational;
        ReservedSnapshotBytes=reserved;SnapshotBytes=3L*RuleBytes+input.DecodedCopyBytes;
    }
    public string GeneratorSha256 { get; }
    public int GeneratorBytes { get; }
    public string GroupSha256 { get; }
    public int GroupOffset { get; }
    public int GroupBytes { get; }
    public string RuleSha256 { get; }
    public int RuleOffset { get; }
    public int RuleBytes { get; }
    public int RuleIndex { get; }
    public int LayerIndex { get; }
    public int ChildOrdinal { get; }
    public TerrainFilterFractalInput Input { get; }
    public TerrainRuleHeader Header=>Input.Header!;
    public TerrainFilterFractalFields Fields=>Input.Fields!;
    public TerrainFractalFamilyEntry Family { get; }
    public int FamilyId=>Family.Id;
    public int FamilyGeneratorOffset=>GroupOffset+Family.Offset;
    public TerrainGeneratorFractalFilterRulePolicy Policy { get; }
    public bool IsEnabled=>Header.IsEnabled;
    // Necessary input subset only: no promise that normalization or every query succeeds.
    public bool HasOperationalInputs { get; }
    public long SnapshotBytes { get; }
    public long ReservedSnapshotBytes { get; }
    public long Work=>SnapshotBytes;
    public long ReservedWork=>ReservedSnapshotBytes;
    public byte[] CopyEncodedBytes()=>Input.CopyEncodedBytes();

    public static TerrainGeneratorFractalFilterRule Select(TerrainGeneratorInput generator,
        TerrainGeneratorFractalFamilies binding,int ruleIndex,TerrainGeneratorFractalFilterRulePolicy policy,
        TerrainGeneratorFractalFilterRuleLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(generator);ArgumentNullException.ThrowIfNull(binding);limits??=new();
        if(policy is not (TerrainGeneratorFractalFilterRulePolicy.TypedEvidence or TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset)
            ||limits.MaximumRuleBytes is <24 or >4194304||limits.MaximumDecodedCopyBytes is <0 or >4194304
            ||limits.MaximumNameBytes is <0 or >65536||limits.MaximumSnapshotBytes is <0 or >16777216
            ||limits.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        token.ThrowIfCancellationRequested();
        Need(generator.SourceSha256==binding.GeneratorSha256&&generator.EncodedBytes==binding.GeneratorBytes,"Exact generator binding required.");
        TerrainGeneratorGroup? group=null;
        foreach(var candidate in generator.Groups)
            if(candidate.Role==TerrainGeneratorGroupRole.FractalMap){Need(group is null,"Ambiguous FractalMap.");group=candidate;}
        Need(group is not null,"Missing FractalMap.");var gs=group!.Section;
        Need(group.ClientVersionRecognized&&gs.Tag=="FORM"&&gs.FormType=="MGRP"&&gs.Version=="0000"
            &&gs.Offset==binding.GroupOffset&&gs.EncodedBytes==binding.GroupBytes
            &&binding.Families.InputBytes==gs.EncodedBytes&&binding.Families.Entries.Count==group.RecordCount
            &&gs.Offset>=0&&(long)gs.Offset+gs.EncodedBytes<=generator.EncodedBytes,"Exact group role/version/extent required.");
        if(ruleIndex<0||ruleIndex>=generator.Rules.Count)throw new ArgumentOutOfRangeException(nameof(ruleIndex));
        var selected=generator.Rules[ruleIndex];var s=selected.Section;
        Need(s.Tag=="FORM"&&s.FormType=="FFRA"&&s.Version=="0005"&&s.Offset>=0
            &&s.EncodedBytes>=24&&(long)s.Offset+s.EncodedBytes<=generator.EncodedBytes,"Exact FFRA/0005 extent required.");
        Need(selected.LayerIndex>=0&&selected.LayerIndex<generator.Layers.Count,"Rule layer extent.");
        var layer=generator.Layers[selected.LayerIndex];
        Need(layer.HasDecodedStructure&&selected.ChildOrdinal>=2&&selected.ChildOrdinal-2<layer.Children.Count,"Rule child ordinal.");
        var child=layer.Children[selected.ChildOrdinal-2];
        Need(child.Offset==s.Offset&&child.EncodedBytes==s.EncodedBytes&&child.Tag==s.Tag&&child.FormType==s.FormType
            &&child.Version==s.Version&&s.Offset>=layer.Section.Offset
            &&(long)s.Offset+s.EncodedBytes<=(long)layer.Section.Offset+layer.Section.EncodedBytes,"Rule/layer identity.");
        // Selected buffer + decoder-owned buffer + temporary IFF buffer; decoded bytes cannot exceed encoded length.
        long reserved=3L*s.EncodedBytes+Math.Min(s.EncodedBytes,limits.MaximumDecodedCopyBytes);
        Need(s.EncodedBytes<=limits.MaximumRuleBytes&&reserved<=limits.MaximumSnapshotBytes
            &&reserved<=limits.MaximumWork,"Selected copy/decode preflight cap.");
        token.ThrowIfCancellationRequested();var encoded=s.CopyEncodedBytes();
        var input=TerrainFilterFractalInput.Decode(encoded,new(limits.MaximumRuleBytes,64,8,limits.MaximumNameBytes,limits.MaximumDecodedCopyBytes));
        Need(input.Version=="0005"&&input.HasDecodedFields&&input.Header is not null&&input.Fields is not null,"Typed inherited header required.");
        token.ThrowIfCancellationRequested();var fields=input.Fields!;int id=unchecked((int)fields.FamilyIdBits);
        var family=binding.Families.Find(id);Need(family is not null,"Exact signed family missing; no fallback.");
        Need(family!.Offset>=24&&family.EncodedBytes>=109&&(long)family.Offset+family.EncodedBytes<=binding.GroupBytes,"Family/group extent invariant.");
        var f=family.Raw.Fields;
        bool operational=fields.HasFiniteRange&&fields.HasFiniteSerializedFeatheringAmount&&Finite(fields.SampleScaleBits)&&f.ModeBits<=5
            &&!f.FirstCurveEnabled&&!f.SecondCurveEnabled&&f.SignedOctaveCount is >=1 and <=16&&f.SignedSeed!=int.MaxValue
            &&Finite(f.FrequencyMultiplierBits)&&Finite(f.AmplitudeMultiplierBits)&&Finite(f.FirstCoordinateScaleBits)
            &&Finite(f.SecondCoordinateScaleBits)&&Finite(f.FirstOffsetBits)&&Finite(f.SecondOffsetBits);
        Need(policy!=TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset||(input.Header!.IsEnabled&&operational),"Caller enabled operational policy rejected raw state.");
        var result=new TerrainGeneratorFractalFilterRule(generator,binding,selected,ruleIndex,input,family,
            Convert.ToHexString(SHA256.HashData(encoded)),policy,operational,reserved);
        token.ThrowIfCancellationRequested();return result;
    }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
}
