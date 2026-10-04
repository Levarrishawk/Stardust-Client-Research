using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainFractalFamilySetScenarios
{
    internal static object Run(string fixtures)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(fixtures));var rows=new List<object>();int ownership=0,atomic=0;
        TerrainFractalFamilySet? previous=null;string? previousEncoding=null;
        object Export(TerrainFractalFamilySet s)=>new {s.SourceSha256,s.InputBytes,s.AggregateNameBytes,s.EmbeddedBytes,s.SnapshotBytes,s.Work,
            Entries=s.Entries.Select(e=>new {e.Id,e.Offset,e.EncodedBytes,e.NameBytes,NameHex=Convert.ToHexString(e.CopyNameBytes()),
                Words=e.Raw.CopyDataWordBits(),RawHex=Convert.ToHexString(e.Raw.CopyEncodedBytes())}).ToArray()};
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            byte[] bytes=Convert.FromHexString(c.GetProperty("Hex").GetString()!);
            var limits=JsonSerializer.Deserialize<TerrainFractalFamilySetLimits>(c.GetProperty("Limits").GetRawText())!;
            using var cancel=new CancellationTokenSource();if(c.GetProperty("Canceled").GetBoolean())cancel.Cancel();
            string reject="None";object? snapshot=null;
            try
            {
                var set=TerrainFractalFamilySet.Decode(bytes,limits,cancel.Token);snapshot=Export(set);string before=JsonSerializer.Serialize(snapshot);
                foreach(var entry in set.Entries)
                {
                    if(!ReferenceEquals(set.Find(entry.Id),entry))throw new Exception("Owned lookup identity.");
                    Array.Clear(entry.CopyNameBytes());Array.Clear(entry.Raw.CopyEncodedBytes());Array.Clear(entry.Raw.CopyDataWordBits());
                    ownership+=3;
                }
                if(set.Find(123456789) is not null)throw new Exception("Missing lookup must be explicit.");
                Array.Clear(bytes);
                if(JsonSerializer.Serialize(Export(set))!=before)throw new Exception("Borrowed/export mutation changed snapshot.");
                ownership++;
                previous=set;previousEncoding=before;
            }
            catch(ArgumentOutOfRangeException){reject="Options";}
            catch(InvalidDataException){reject="Input";}
            catch(OperationCanceledException){reject="Canceled";}
            if(reject!="None"&&previous is not null)
            {if(JsonSerializer.Serialize(Export(previous))!=previousEncoding)throw new Exception("Failure altered prior owned result.");atomic++;}
            rows.Add(new {Name=c.GetProperty("Name").GetString(),Reject=reject,Snapshot=snapshot});
        }
        return new {Cases=rows,OwnershipChecks=ownership,AtomicFailureChecks=atomic};
    }
}
