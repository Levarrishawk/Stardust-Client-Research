using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class AuthoredCollisionExtentScenarios
{
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=10 || args[2]!="--configuration" || args[4]!="--sources" || args[6]!="--expected-sha256" || args[8]!="--configuration-sha256")
            throw new ArgumentException("Authored extent probe requires pinned configuration and source manifest.");
        byte[] configBytes=File.ReadAllBytes(args[3]);
        if(Convert.ToHexString(SHA256.HashData(configBytes))!=args[9].ToUpperInvariant())throw new InvalidDataException("Extent configuration pin mismatch.");
        if(new FileInfo(args[5]).Length>262144)throw new InvalidDataException("Extent manifest budget.");
        byte[] manifest=File.ReadAllBytes(args[5]);
        if(Convert.ToHexString(SHA256.HashData(manifest))!=args[7].ToUpperInvariant())throw new InvalidDataException("Extent manifest pin mismatch.");
        var sources=JsonSerializer.Deserialize<AssetSourceIdentity[]>(manifest)??throw new InvalidDataException("Missing extent sources.");
        if(sources.Length is <1 or >16 || sources.Select(s=>s.Path).Distinct(StringComparer.Ordinal).Count()!=sources.Length)
            throw new InvalidDataException("Extent source count/uniqueness budget.");
        using var config=new StreamReader(new MemoryStream(configBytes));using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(var source in sources)
        {
            var selected=overlay.Lookup(source.Path);
            if(selected.Kind!=TreLookupKind.Present || selected.Payload is null || selected.Priority!=source.Priority || selected.Payload.Length!=source.Bytes || Convert.ToHexString(SHA256.HashData(selected.Payload))!=source.Sha256)
                throw new InvalidDataException("Authored extent source identity mismatch.");
            IffNode root=IffDocument.Parse(selected.Payload).Root;
            if(root.FormType is not ("MESH" or "DTLA") || root.Children.Count!=1 || root.Children[0].Children.Count==0 || !root.Children[0].Children[0].IsForm("APPR"))
                throw new InvalidDataException("Authored extent probe requires a mesh or detail appearance.");
            IffNode appr=root.Children[0].Children[0];
            var primary=AppearanceExtentDecoder.DecodeBase(appr);var collision=AppearanceExtentDecoder.DecodeCollisionBase(appr);
            object? Describe(AppearanceExtent? extent)=>extent is null?null:new{extent.Kind,extent.Version,center=InteriorDoorScenarios.V(extent.Center),extent.Radius,
                minimum=extent.Minimum is {} minimum?InteriorDoorScenarios.V(minimum):null,maximum=extent.Maximum is {} maximum?InteriorDoorScenarios.V(maximum):null};
            Console.WriteLine("authored_extent="+JsonSerializer.Serialize(new{source,family=root.FormType,primary=Describe(primary),collision=Describe(collision)}));
        }
        Console.WriteLine($"PASS authored extent sources={sources.Length}");
    }
}
