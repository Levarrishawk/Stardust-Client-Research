using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record MosTaikePlacement(WorldSnapshotNode Node,string ObjectPath,string ObjectSha256,
    SharedObjectAppearanceProfile Profile,TexturedAppearanceGraph Appearance,AppearanceTransform Transform);
public sealed record MosTaikeOmission(int Id,string ObjectPath,string Reason);

/// <summary>Original outdoor snapshot roots only; interiors and server-spawned objects are not invented.</summary>
public sealed record MosTaikeSceneResources(WorldSnapshotInput Snapshot,IReadOnlyList<MosTaikePlacement> Placements,IReadOnlyList<MosTaikeOmission> Omissions)
{
    public static MosTaikeSceneResources Load(Func<string,TreLookupResult> resolve,CancellationToken token=default)
    {
        byte[] Read(string path){token.ThrowIfCancellationRequested();var selected=resolve(path);if(selected.Kind!=TreLookupKind.Present||selected.Payload is null)throw new InvalidDataException("Missing original scene resource: "+path);return selected.Payload;}
        var snapshot=WorldSnapshotInput.Decode(Read("snapshot/tatooine.ws"));
        if(snapshot.Sha256!="87E1810A7C87F1D6CDF3FB47BBE6F84D0922471802A74C69E4CAAA5157FC7477")throw new InvalidDataException("Unverified Mos Taike snapshot.");
        var placements=new List<MosTaikePlacement>();var omitted=new List<MosTaikeOmission>();
        var profiles=new Dictionary<string,(SharedObjectAppearanceProfile Profile,string Sha,TexturedAppearanceGraph Appearance)>();
        foreach(var node in snapshot.Nodes.Where(n=>n.ParentId is null&&n.ContainerId==0&&n.CellIndex==0&&n.Position.X is >=3720 and <=3880&&n.Position.Z is >=2280 and <=2440))
        {
            token.ThrowIfCancellationRequested();string path=snapshot.Templates[node.TemplateIndex];
            try
            {
                float length=node.Rotation.LengthSquared();if(length<.99f||length>1.01f)throw new InvalidDataException("Nonunit selected snapshot quaternion.");
                if(!profiles.TryGetValue(path,out var prepared))
                {
                    var bytes=Read(path);string sha=Convert.ToHexString(SHA256.HashData(bytes));
                    SharedObjectAppearanceProfile profile;
                    try{profile=SharedObjectAppearanceProfileReader.Load(resolve,path,sha);}
                    catch(InvalidDataException e)when(path.StartsWith("object/building/")&&e.Message=="No nonempty deterministic nearest appearance.")
                    {profile=SharedObjectAppearanceProfileReader.Load(resolve,path,sha,fieldName:"portalLayoutFilename");}
                    byte[] appearance=Read(profile.AppearancePath);
                    string appearanceSha=Convert.ToHexString(SHA256.HashData(appearance));
                    var material=profile.AppearancePath.EndsWith(".pob")
                        ?TexturedPortalSceneReader.Load(resolve,profile.AppearancePath,appearanceSha,new(MaximumResources:128,MaximumSourceBytes:64*1024*1024),maximumDecodedBytes:64*1024*1024,selectedAppearanceCells:new[]{0}).Materials
                        :TexturedAppearanceGraphReader.Load(resolve,profile.AppearancePath,appearanceSha,new(MaximumResources:128,MaximumSourceBytes:64*1024*1024),maximumDecodedBytes:64*1024*1024);
                    bool Complete(AppearanceGeometryTemplate g)=>g.Diagnostics.Count==0&&g.Children.All(c=>Complete(c.Template));
                    if(!Complete(material.Geometry.Root)||material.Diagnostics.Count>0||material.Meshes.Count==0)throw new InvalidDataException("Incomplete original geometry/material closure: "+string.Join(";",material.Diagnostics));
                    prepared=(profile,sha,material);profiles.Add(path,prepared);
                }
                var q=node.Rotation;
                // System.Numerics Vector3.Transform provides a column map; retain the authored raw quaternion.
                Vector3 a=Vector3.Transform(Vector3.UnitX,q),b=Vector3.Transform(Vector3.UnitY,q),c=Vector3.Transform(Vector3.UnitZ,q);
                var transform=new AppearanceTransform(new(a.X,b.X,c.X),new(a.Y,b.Y,c.Y),new(a.Z,b.Z,c.Z),node.Position);
                placements.Add(new(node,path,prepared.Sha,prepared.Profile,prepared.Appearance,transform));
            }
            catch(InvalidDataException e)
            {
                if(path.StartsWith("object/building/"))throw new InvalidDataException("Required original Mos Taike building unavailable: "+path+" "+e.Message,e);
                omitted.Add(new(node.Id,path,e.Message));
            }
        }
        if(!placements.Any(p=>p.ObjectPath.Contains("parking_garage"))||placements.Count(p=>p.ObjectPath.StartsWith("object/building/"))<4)
            throw new InvalidDataException("Required original Mos Taike garage/building district unavailable.");
        return new(snapshot,placements.AsReadOnly(),omitted.AsReadOnly());
    }
}
