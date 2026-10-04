using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainGeneratorFractalFamilyScenarios
{
    internal static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));var results=new List<object>();int ownership=0,atomic=0;
        TerrainGeneratorFractalFamilies? prior=null;string? priorJson=null;
        object Export(TerrainGeneratorFractalFamilies b)=>new {b.GeneratorSha256,b.GeneratorBytes,b.GroupSha256,b.GroupOffset,b.GroupBytes,b.GroupVersion,
            b.EncodedCopyBytes,b.ReservedEncodedCopyBytes,b.SnapshotBytes,b.Work,b.ReservedWork,
            Entries=b.Families.Entries.Select(e=>new {e.Id,e.Offset,e.EncodedBytes,e.NameBytes,NameHex=Convert.ToHexString(e.CopyNameBytes()),
                RawHex=Convert.ToHexString(e.Raw.CopyEncodedBytes()),Words=e.Raw.CopyDataWordBits()}).ToArray()};
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            byte[] input=Convert.FromHexString(c.GetProperty("Hex").GetString()!);string stage="Generator",reject="None";object? snapshot=null;
            try
            {
                var generator=TerrainGeneratorInput.Decode(input);stage="Bridge";
                var p=JsonSerializer.Deserialize<TerrainGeneratorFractalFamilyLimits>(c.GetProperty("Limits").GetRawText())!;
                using var cancel=new CancellationTokenSource();if(c.GetProperty("Canceled").GetBoolean())cancel.Cancel();
                var result=TerrainGeneratorFractalFamilies.Bind(generator,p,cancel.Token);snapshot=Export(result);string before=JsonSerializer.Serialize(snapshot);
                Array.Clear(input);Array.Clear(generator.CopyGeneratorForm());
                foreach(var group in generator.Groups)Array.Clear(group.Section.CopyEncodedBytes());
                foreach(var e in result.Families.Entries)
                {Array.Clear(e.CopyNameBytes());Array.Clear(e.Raw.CopyEncodedBytes());Array.Clear(e.Raw.CopyDataWordBits());ownership+=3;
                    if(!ReferenceEquals(result.Families.Find(e.Id),e))throw new Exception("Stable family lookup.");}
                if(JsonSerializer.Serialize(Export(result))!=before)throw new Exception("Mutable exported provenance/data.");ownership++;
                prior=result;priorJson=before;
            }
            catch(ArgumentOutOfRangeException){reject="Options";}
            catch(InvalidDataException){reject="Input";}
            catch(OperationCanceledException){reject="Canceled";}
            if(reject!="None"&&prior is not null){if(JsonSerializer.Serialize(Export(prior))!=priorJson)throw new Exception("Failure changed prior result.");atomic++;}
            results.Add(new {Name=c.GetProperty("Name").GetString(),Stage=stage,Reject=reject,Snapshot=snapshot});
        }
        return new {Cases=results,OwnershipChecks=ownership,AtomicFailureChecks=atomic};
    }
}
