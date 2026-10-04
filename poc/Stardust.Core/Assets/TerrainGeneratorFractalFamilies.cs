using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record TerrainGeneratorFractalFamilyLimits(TerrainFractalFamilySetLimits? FamilyLimits=null,
    int MaximumSectionBytes=1048576,long MaximumEncodedCopyBytes=83886080,
    long MaximumSnapshotBytes=33554432,long MaximumWork=117440512);

/// <summary>One-shot owned family publication with exact generator and group provenance; no runtime map.</summary>
public sealed class TerrainGeneratorFractalFamilies
{
    // Frozen TerrainGeneratorInput exposes only CopyGeneratorForm, not its source length/hash.
    public const int GeneratorCopyReservationBytes=67108864;
    private TerrainGeneratorFractalFamilies(TerrainFractalFamilySet families,string generatorHash,int generatorBytes,
        int offset,int groupBytes,long copies,long reservedCopies,long work,long reservedWork)
    { Families=families;GeneratorSha256=generatorHash;GeneratorBytes=generatorBytes;GroupOffset=offset;GroupBytes=groupBytes;
        EncodedCopyBytes=copies;ReservedEncodedCopyBytes=reservedCopies;Work=work;ReservedWork=reservedWork; }
    public TerrainFractalFamilySet Families { get; }
    public string GeneratorSha256 { get; }
    public int GeneratorBytes { get; }
    public string GroupSha256=>Families.SourceSha256;
    public int GroupOffset { get; }
    public int GroupBytes { get; }
    public string GroupVersion=>"0000";
    public long EncodedCopyBytes { get; }
    public long ReservedEncodedCopyBytes { get; }
    public long SnapshotBytes=>Families.SnapshotBytes;
    public long Work { get; }
    public long ReservedWork { get; }

    public static TerrainGeneratorFractalFamilies Bind(TerrainGeneratorInput generator,
        TerrainGeneratorFractalFamilyLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(generator);limits??=new();var f=limits.FamilyLimits??new();Policy(limits,f);
        token.ThrowIfCancellationRequested();TerrainGeneratorGroup? selected=null;
        foreach(var group in generator.Groups)
        {
            if(group.Role!=TerrainGeneratorGroupRole.FractalMap)continue;
            Need(selected is null,"Multiple FractalMap groups rejected.");selected=group;
        }
        Need(selected is not null,"Absent FractalMap rejected; no fallback.");var chosen=selected!;var section=chosen.Section;
        Need(chosen.ClientVersionRecognized&&section.Tag=="FORM"&&section.FormType=="MGRP"&&section.Version=="0000","Recognized exact MGRP/0000 required.");
        long names=section.EncodedBytes-24L-108L*chosen.RecordCount;
        long snapshot=names+136L*chosen.RecordCount;
        long familyWork=section.EncodedBytes+84L*chosen.RecordCount;
        long reservedCopies=GeneratorCopyReservationBytes+(long)section.EncodedBytes;
        long reservedWork=GeneratorCopyReservationBytes+section.EncodedBytes+familyWork;
        // Exact-grammar necessary aggregate charges and conservative root-copy reservation before any copies.
        Need(section.EncodedBytes>=24&&section.EncodedBytes<=limits.MaximumSectionBytes&&section.EncodedBytes<=f.MaximumInputBytes
            &&chosen.RecordCount<=f.MaximumEntries&&names>=chosen.RecordCount&&names<=f.MaximumAggregateNameBytes
            &&84L*chosen.RecordCount<=f.MaximumEmbeddedBytes&&snapshot<=f.MaximumSnapshotBytes
            &&snapshot<=limits.MaximumSnapshotBytes&&familyWork<=f.MaximumWork
            &&reservedCopies<=limits.MaximumEncodedCopyBytes&&reservedWork<=limits.MaximumWork,"Bridge preflight cap/extent.");
        token.ThrowIfCancellationRequested();byte[] generatorCopy=generator.CopyGeneratorForm();
        Need(generatorCopy.Length<=GeneratorCopyReservationBytes&&section.Offset>=0
            &&(long)section.Offset+section.EncodedBytes<=generatorCopy.Length,"Frozen generator extent invariant.");
        string generatorHash=Convert.ToHexString(SHA256.HashData(generatorCopy));int generatorBytes=generatorCopy.Length;
        token.ThrowIfCancellationRequested();byte[] encoded=section.CopyEncodedBytes();
        Need(encoded.Length==section.EncodedBytes&&generatorCopy.AsSpan(section.Offset,section.EncodedBytes).SequenceEqual(encoded),"Generator/group identity mismatch.");
        var families=TerrainFractalFamilySet.Decode(encoded,f,token);
        Need(families.Entries.Count==chosen.RecordCount&&families.SnapshotBytes==snapshot&&families.Work==familyWork,"Recognized family accounting mismatch.");
        var result=new TerrainGeneratorFractalFamilies(families,generatorHash,generatorBytes,section.Offset,section.EncodedBytes,
            generatorBytes+(long)encoded.Length,reservedCopies,generatorBytes+encoded.Length+families.Work,reservedWork);
        token.ThrowIfCancellationRequested();return result;
    }
    private static void Policy(TerrainGeneratorFractalFamilyLimits p,TerrainFractalFamilySetLimits f)
    {
        if(p.MaximumSectionBytes is <0 or >16777216||p.MaximumEncodedCopyBytes is <0 or >83886080
            ||p.MaximumSnapshotBytes is <0 or >33554432||p.MaximumWork is <0 or >117440512
            ||f.MaximumInputBytes is <0 or >16777216||f.MaximumEntries is <0 or >65536
            ||f.MaximumNameBytes is <1 or >65536||f.MaximumAggregateNameBytes is <0 or >16777216
            ||f.MaximumEmbeddedBytes is <0 or >5505024||f.MaximumSnapshotBytes is <0 or >33554432
            ||f.MaximumWork is <0 or >33554432)throw new ArgumentOutOfRangeException(nameof(p));
    }
    private static void Need(bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
