using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainGeneratorIdentityScenarios
{
    internal static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));var rows=new List<object>();int ownership=0,atomic=0,guards=0,getters=0;
        TerrainGeneratorInput? prior=null;string? priorHash=null;int priorLength=0;
        string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            byte[] source=Convert.FromHexString(c.GetProperty("Hex").GetString()!);string stage="Generator",reject="None";
            object? identity=null,bridge=null;bool guard=c.GetProperty("AllocationGuard").GetBoolean();long delta=0;
            try
            {
                var gp=JsonSerializer.Deserialize<TerrainGeneratorInputLimits>(c.GetProperty("GeneratorLimits").GetRawText())!;
                var g=TerrainGeneratorInput.Decode(source,gp);stage="Bridge";string hash=g.SourceSha256;
                if(g.EncodedBytes!=source.Length||hash!=Hash(source))throw new Exception("Owned generator identity mismatch.");
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<1000;i++)if(!ReferenceEquals(hash,g.SourceSha256)||g.EncodedBytes!=source.Length)throw new Exception("Identity getter changed.");
                if(GC.GetAllocatedBytesForCurrentThread()!=before)throw new Exception("Cached identity getter allocated.");getters++;
                identity=new {g.EncodedBytes,g.SourceSha256};
                Array.Clear(source);Array.Clear(g.CopyGeneratorForm());
                foreach(var group in g.Groups)Array.Clear(group.Section.CopyEncodedBytes());
                if(g.SourceSha256!=hash||Hash(g.CopyGeneratorForm())!=hash)throw new Exception("Export/source mutation altered owned identity.");ownership++;
                prior=g;priorHash=hash;priorLength=g.EncodedBytes;
                var p=JsonSerializer.Deserialize<TerrainGeneratorFractalFamilyLimits>(c.GetProperty("Limits").GetRawText())!;
                using var canceled=new CancellationTokenSource();if(c.GetProperty("Canceled").GetBoolean())canceled.Cancel();
                long allocationStart=GC.GetAllocatedBytesForCurrentThread();
                TerrainGeneratorFractalFamilies b;
                try {b=TerrainGeneratorFractalFamilies.Bind(g,p,canceled.Token);}
                finally {delta=GC.GetAllocatedBytesForCurrentThread()-allocationStart;}
                if(b.ReservedEncodedCopyBytes!=b.EncodedCopyBytes||b.ReservedWork!=b.Work||b.GeneratorSha256!=hash||b.GeneratorBytes!=g.EncodedBytes)throw new Exception("Actual reservation/provenance mismatch.");
                bridge=new {b.GeneratorSha256,b.GeneratorBytes,b.GroupSha256,b.GroupOffset,b.GroupBytes,b.GroupVersion,
                    b.EncodedCopyBytes,b.ReservedEncodedCopyBytes,b.SnapshotBytes,b.Work,b.ReservedWork};
            }
            catch(ArgumentOutOfRangeException){reject="Options";}
            catch(InvalidDataException){reject="Input";}
            catch(OperationCanceledException){reject="Canceled";}
            if(guard){if(stage!="Bridge"||reject!="Input"||delta>=8192)throw new Exception("Pre-copy rejection allocated a root snapshot.");guards++;}
            if(reject!="None"&&prior is not null)
            {if(prior.SourceSha256!=priorHash||prior.EncodedBytes!=priorLength||Hash(prior.CopyGeneratorForm())!=priorHash)throw new Exception("Failed request changed prior identity.");atomic++;}
            rows.Add(new {Name=c.GetProperty("Name").GetString(),Stage=stage,Reject=reject,Identity=identity,Bridge=bridge});
        }
        return new {Cases=rows,OwnershipChecks=ownership,AtomicFailureChecks=atomic,PreCopyAllocationGuards=guards,CachedGetterChecks=getters};
    }
}
