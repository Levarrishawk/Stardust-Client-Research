using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class DoorCapabilityScenarios
{
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length is not (6 or 7) || args[2]!="--configuration" || args[4]!="--table-sha256" || args.Length==7 && args[6]!="--direct-only")throw new ArgumentException("Door capability probe requires pinned table.");
        using StreamReader cfg=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],cfg);
        byte[] raw=overlay.Lookup(SingleLeafDoorAssetReader.TablePath).Payload??throw new InvalidDataException("Missing door table.");
        if(Convert.ToHexString(SHA256.HashData(raw))!=args[5].ToUpperInvariant())throw new InvalidDataException("Door table identity mismatch.");
        foreach(var style in DoorStyleTableDecoder.Decode(raw).Rows)
        {
            string category=style.IsForceField?"force_field":style.SecondaryAppearance.Length!=0?"secondary_leaf":style.FlipSecondaryRaw!=0?"flip_without_secondary":style.PrimaryAppearance.Length==0?"missing_primary":"single_leaf_candidate";
            string? error=null;object? bounds=null;string? primarySha=null;
            if(category=="single_leaf_candidate")
            {
                var source=overlay.Lookup(style.PrimaryAppearance);
                if(source.Kind!=TreLookupKind.Present || source.Payload is null)category="missing_primary_asset";
                else
                {
                    primarySha=Convert.ToHexString(SHA256.HashData(source.Payload));
                    try
                    {
                        var asset=SingleLeafDoorAssetReader.Load(overlay.Lookup,style.Name,args[5],primarySha);
                        if(args.Length==7 && asset.Geometry.Root.Mesh is null)throw new InvalidDataException("Door leaf asset slice requires a direct static mesh.");
                        category="supported_single_leaf";bounds=new{min=InteriorDoorScenarios.V(asset.Extent.Minimum!.Value),max=InteriorDoorScenarios.V(asset.Extent.Maximum!.Value),sources=asset.Sources.Values};
                    }
                    catch(InvalidDataException failure){category="unsupported_primary_asset";error=failure.Message;}
                }
            }
            Console.WriteLine("door_capability="+JsonSerializer.Serialize(new{name=style.Name,category,primary=style.PrimaryAppearance,primary_sha256=primarySha,frame=style.FrameAppearance,secondary=style.SecondaryAppearance,flip=style.FlipSecondaryRaw,force=style.ForceFieldRaw,bounds,error}));
        }
    }
}
