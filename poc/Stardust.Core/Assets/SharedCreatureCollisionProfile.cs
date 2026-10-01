using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record ResolvedTemplateField(string SourcePath,string Family,string Version);
public sealed record CreatureFallbackSphere(Vector3 LocalCenter,float Radius);
public sealed record SharedCreatureCollisionProfile(float StepHeight,float CollisionHeight,float Radius,float OffsetX,float OffsetZ,float Length,float WarpTolerance)
{
    /// <summary>Recovered local fallback construction; does not select runtime participation or transform policy.</summary>
    public CreatureFallbackSphere CreateFallbackSphere(float scale=1,bool specialDimensions=false)
    {
        if(!float.IsFinite(scale)||scale<=0||scale>1000)throw new ArgumentOutOfRangeException(nameof(scale));
        if(!float.IsFinite(StepHeight)||StepHeight<0||!float.IsFinite(Radius)||Radius<0)throw new InvalidDataException("Invalid creature fallback dimensions.");
        float radius=specialDimensions ? .5f : Radius;float step=specialDimensions ? 1 : StepHeight;
        Vector3 center=new(OffsetX*scale,(step+radius)*scale,OffsetZ*scale);float scaledRadius=radius*scale;
        if(!float.IsFinite(center.X)||!float.IsFinite(center.Y)||!float.IsFinite(center.Z)||!float.IsFinite(scaledRadius)||scaledRadius<0)
            throw new InvalidDataException("Unrepresentable creature fallback sphere.");
        return new(center,scaledRadius);
    }
}
public sealed record SharedCreatureTemplateProfile(string RootPath,string AppearancePath,SharedCreatureCollisionProfile Collision,
    IReadOnlyDictionary<string,ResolvedTemplateField> Fields,IReadOnlyDictionary<string,AssetSourceIdentity> Sources);

public static class SharedCreatureTemplateProfileReader
{
    public static SharedCreatureTemplateProfile Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,int maximumDepth=16)
    {
        if(maximumDepth is <1 or >16)throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        path=LegacyAssetPath.Normalize(path);OwnedAssetSourceCache cache=new(resolve,new(MaximumResources:16,MaximumSourceBytes:1048576));
        List<(string Path,SharedObjectTemplateDescription Description)> chain=new();HashSet<string> seen=new(StringComparer.Ordinal);
        string? current=path;
        while(current is not null)
        {
            if(chain.Count>=maximumDepth)throw new AppearanceGraphBudgetException("Object template inheritance depth budget.");
            if(!seen.Add(current))throw new InvalidDataException("Object template inheritance cycle.");
            byte[] source;
            if(chain.Count==0)source=cache.ReadPinned(current,expectedSha256);
            else
            {
                TreLookupResult result=cache.Read(current);if(result.Kind!=TreLookupKind.Present||result.Payload is null)throw new InvalidDataException("Inherited object template missing/masked.");
                source=result.Payload;
            }
            var description=SharedObjectTemplateDecoder.Decode(source);chain.Add((current,description));current=description.ParentPath;
        }
        if(chain[0].Description.Layers[0].Family!="SCOT")throw new InvalidDataException("Creature profile requires SCOT root.");
        Dictionary<string,ResolvedTemplateField> fields=new(StringComparer.Ordinal);
        float Float(string name)
        {
            foreach(var input in chain)
            {
                var layer=input.Description.Layers.FirstOrDefault(l=>l.Family=="SCOT");
                if(layer is null||!layer.Parameters.TryGetValue(name,out var encoded))continue;
                ReadOnlySpan<byte> bytes=encoded.Span;SharedObjectTemplateDecoder.Require(bytes.Length>=2 && bytes[1]==32,"Unsupported float parameter modifier.");
                if(bytes[0]==0){SharedObjectTemplateDecoder.Require(bytes.Length==2,"Invalid inherited float parameter.");continue;}
                SharedObjectTemplateDecoder.Require(bytes[0]==1 && bytes.Length==6,"Unsupported float parameter mode/extent.");
                float value=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes[2..]));
                SharedObjectTemplateDecoder.Require(float.IsFinite(value),"Nonfinite template float.");fields.Add(name,new(input.Path,layer.Family,layer.Version));return value;
            }
            throw new InvalidDataException("Unresolved creature collision field: "+name);
        }
        string Appearance()
        {
            foreach(var input in chain)
            {
                var layer=input.Description.Layers.First(l=>l.Family=="SHOT");
                if(!layer.Parameters.TryGetValue("appearanceFilename",out var encoded))continue;
                ReadOnlySpan<byte> bytes=encoded.Span;SharedObjectTemplateDecoder.Require(bytes.Length>=1,"Missing string parameter mode.");
                if(bytes[0]==0){SharedObjectTemplateDecoder.Require(bytes.Length==1,"Invalid inherited string parameter.");continue;}
                SharedObjectTemplateDecoder.Require(bytes[0]==1,"Unsupported appearance string parameter mode.");
                string value=SharedObjectTemplateDecoder.Text(bytes[1..],1024);SharedObjectTemplateDecoder.Require(value.Length>0,"Empty resolved creature appearance.");
                fields.Add("appearanceFilename",new(input.Path,layer.Family,layer.Version));return LegacyAssetPath.Normalize(value);
            }
            throw new InvalidDataException("Unresolved creature appearance.");
        }
        var profile=new SharedCreatureCollisionProfile(Float("stepHeight"),Float("collisionHeight"),Float("collisionRadius"),Float("collisionOffsetX"),Float("collisionOffsetZ"),Float("collisionLength"),Float("warpTolerance"));
        if(profile.StepHeight<0||profile.CollisionHeight<0||profile.Radius<0||profile.Length<0||profile.WarpTolerance<0)throw new InvalidDataException("Negative collision dimensions are outside the creature profile slice.");
        return new(path,Appearance(),profile,new ReadOnlyDictionary<string,ResolvedTemplateField>(fields),cache.Sources);
    }
}
