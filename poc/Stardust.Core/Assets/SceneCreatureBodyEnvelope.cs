using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Detached native local geometry/provenance. No world orientation or active participation is implied.</summary>
public sealed class SceneCreatureBodyEnvelopeReceipt
{
    internal SceneCreatureBodyEnvelopeReceipt(float height,bool special,CreatureFallbackSphere sphere,float step,float collisionHeight,float length,float warp,
        SharedCreatureTemplateProfile profile,AssetSourceIdentity root,AssetSourceIdentity appearance)
    {
        Height=height; SpecialDimensions=special; LocalCenter=sphere.LocalCenter; Radius=sphere.Radius;
        StepHeight=step; CollisionHeight=collisionHeight; Length=length; WarpTolerance=warp;
        RootPath=profile.RootPath; AppearancePath=profile.AppearancePath; RootSource=root with {}; AppearanceSource=appearance with {};
        Fields=new ReadOnlyDictionary<string,ResolvedTemplateField>(profile.Fields.ToDictionary(p=>p.Key,p=>p.Value with {},StringComparer.Ordinal));
        ProfileSources=new ReadOnlyDictionary<string,AssetSourceIdentity>(profile.Sources.ToDictionary(p=>p.Key,p=>p.Value with {},StringComparer.Ordinal));
    }
    public float Height {get;}
    public bool SpecialDimensions {get;}
    public Vector3 LocalCenter {get;}
    public float Radius {get;}
    public float StepHeight {get;}
    public float CollisionHeight {get;}
    public float Length {get;}
    public float WarpTolerance {get;}
    public string RootPath {get;}
    public string AppearancePath {get;}
    public AssetSourceIdentity RootSource {get;}
    public AssetSourceIdentity AppearanceSource {get;}
    public IReadOnlyDictionary<string,ResolvedTemplateField> Fields {get;}
    public IReadOnlyDictionary<string,AssetSourceIdentity> ProfileSources {get;}
}

public static class SceneCreatureBodyEnvelope
{
    public const float MinimumHeight=.25f, MaximumHeight=2f;
    /// <summary>Pure bridge over the already loaded creature profile; modern positive-volume policy.</summary>
    public static SceneCreatureBodyEnvelopeReceipt Create(SceneObjectResources resources,float height,bool specialDimensions=false)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if(!float.IsFinite(height)||height<MinimumHeight||height>MaximumHeight)throw new ArgumentOutOfRangeException(nameof(height),"Modern Height must be in [0.25, 2].");
        Need(resources.Creature is {} && resources.Static is null && resources.Recipe is {} && resources.Recipe.Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature resources required.");
        var creature=resources.Creature; var profile=creature.Creature?.Profile;
        Need(profile is {} && profile.Collision is {} && profile.Fields is {} && profile.Sources is {Count:>0 and <=16} && resources.Sources is {},"Incomplete creature profile.");
        Need(resources.Create is {} && resources.Create.TemplateCrc==resources.Recipe.TemplateCrc && profile.RootPath==resources.Recipe.ObjectPath,"Profile/create recipe identity mismatch.");
        Need(profile.RootPath is {} && profile.AppearancePath is {},"Profile paths absent.");
        Need(resources.Sources.TryGetValue(profile.RootPath,out var root) && root is {} && root.Sha256==resources.Recipe.ObjectSha256,"Profile root pin mismatch.");
        Need(resources.Sources.TryGetValue(profile.AppearancePath,out var appearance) && appearance is {} && appearance.Sha256==resources.Recipe.AppearanceSha256,"Profile appearance pin mismatch.");
        foreach(var pair in profile.Sources)
            Need(pair.Value is {} && pair.Key==pair.Value.Path && pair.Value.Bytes>0 && resources.Sources.TryGetValue(pair.Key,out var common) && common==pair.Value,"Profile source identity mismatch.");
        Need(profile.Sources.TryGetValue(profile.RootPath,out var profileRoot) && profileRoot==root,"Profile root source absent.");
        Need(root.Path==profile.RootPath && appearance.Path==profile.AppearancePath && root.Bytes>0 && appearance.Bytes>0,"Root/appearance metadata mismatch.");
        string[] required={"stepHeight","collisionHeight","collisionRadius","collisionOffsetX","collisionOffsetZ","collisionLength","warpTolerance","appearanceFilename"};
        Need(profile.Fields.Count==required.Length,"Incomplete resolved field provenance.");
        foreach(string name in required)
            Need(profile.Fields.TryGetValue(name,out var field) && field is {} && field.SourcePath is {} && profile.Sources.ContainsKey(field.SourcePath) && field.Family==(name=="appearanceFilename"?"SHOT":"SCOT") && field.Version is {Length:4},"Resolved field provenance mismatch.");
        var c=profile.Collision;
        Need(float.IsFinite(c.StepHeight)&&c.StepHeight>=0 && float.IsFinite(c.Radius)&&c.Radius>=0 && float.IsFinite(c.CollisionHeight)&&c.CollisionHeight>0 &&
            float.IsFinite(c.Length)&&c.Length>0 && float.IsFinite(c.WarpTolerance)&&c.WarpTolerance>=0 && float.IsFinite(c.OffsetX)&&float.IsFinite(c.OffsetZ),"Invalid body dimensions.");
        var sphere=c.CreateFallbackSphere(height,specialDimensions);
        float step=(specialDimensions?1:c.StepHeight)*height,collisionHeight=c.CollisionHeight*height,length=c.Length*height,warp=c.WarpTolerance*height;
        Need(float.IsFinite(sphere.Radius)&&sphere.Radius>0 && float.IsFinite(step)&&step>=0 && float.IsFinite(collisionHeight)&&collisionHeight>0 &&
            float.IsFinite(length)&&length>0 && float.IsFinite(warp)&&warp>=0,"Unrepresentable scaled body dimensions.");
        return new(height,specialDimensions,sphere,step,collisionHeight,length,warp,profile,root,appearance);
    }
    private static void Need([DoesNotReturnIf(false)] bool condition,string reason) {if(!condition)throw new InvalidDataException(reason);}
}
