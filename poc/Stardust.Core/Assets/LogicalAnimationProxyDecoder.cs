namespace Stardust.Core.Assets;

public sealed record LogicalAnimationProxyDescription(string ClipPath,IReadOnlyList<float> TimeScales,IffNode? PriorityMetadata)
{public double EffectiveTimeScale=>TimeScales.Aggregate(1.0,(product,value)=>product*value);}
public sealed record LogicalAnimationClipSelection(string TablePath,string LogicalName,string ClipSha256,IReadOnlyList<int>? ExplicitChildRoute=null);
public sealed record SelectedLogicalAnimationClip(LogicalAnimationClipSelection Selection,LogicalAnimationProxyDescription Proxy,IffNode Clip);

/// <summary>Bounded proxy/time-scale selection, without priority execution or animation evaluation.</summary>
public static class LogicalAnimationProxyDecoder
{
    public static LogicalAnimationProxyDescription Decode(IffNode template,IReadOnlyList<int>? explicitChildRoute=null)
    {
        ArgumentNullException.ThrowIfNull(template);
        if(explicitChildRoute is not null)
        {
            Require(explicitChildRoute.Count<=8,"Explicit animation route depth budget.");
            foreach(int child in explicitChildRoute)
            {
                var parts=Version(template);IReadOnlyList<IffNode> candidates;
                if(template.IsForm("SSAT"))
                {
                    Require(parts.Count>=2&&parts[0].Tag=="INFO"&&parts[1].IsForm("ANMS"),"Invalid explicitly selected SSAT.");
                    SkeletalAppearanceDecoder.Strings(parts[0].Payload.Span,1);
                    var animations=parts[1].Children;Require(animations.Count>=1&&animations[0].Tag=="INFO"&&animations[0].Payload.Length==2,"Invalid SSAT animation count.");
                    int count=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(animations[0].Payload.Span);
                    Require(count>0&&count==animations.Count-1,"SSAT animation count mismatch.");candidates=animations.Skip(1).ToArray();
                }
                else
                {
                    Require(template.IsForm("SPAT")&&parts.Count>=1&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==1,"Explicit route requires SSAT/SPAT.");
                    Require(parts[0].Payload.Span[0]>0&&parts[0].Payload.Span[0]==parts.Count-1,"SPAT animation count mismatch.");candidates=parts.Skip(1).ToArray();
                }
                Require(child>=0&&child<candidates.Count&&candidates[child].Tag=="FORM","Explicit animation child outside counted forms.");template=candidates[child];
            }
        }
        var scales=new List<float>();int depth=0;
        while(template.IsForm("TSCL"))
        {
            Require(++depth<=8,"Animation proxy wrapper depth budget.");var parts=Version(template);
            Require(parts.Count==2&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==4&&parts[1].Tag=="FORM","Invalid time-scale envelope.");
            float scale=BitConverter.Int32BitsToSingle(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(parts[0].Payload.Span));
            Require(float.IsFinite(scale)&&scale>0&&scale<=1000,"Invalid bounded animation time scale.");scales.Add(scale);template=parts[1];
        }
        Require(template.IsForm("PXAT"),"Selected animation requires a supported proxy envelope.");var proxy=Version(template);
        Require(proxy.Count is 1 or 2&&proxy[0].Tag=="INFO","Invalid animation proxy content.");
        string path=LegacyAssetPath.Normalize(SkeletalAppearanceDecoder.Strings(proxy[0].Payload.Span,1)[0]);
        Require(path.StartsWith("appearance/animation/",StringComparison.Ordinal),"Animation proxy clip path required.");
        var result=new LogicalAnimationProxyDescription(path,Array.AsReadOnly(scales.ToArray()),proxy.Count==2?proxy[1]:null);
        Require(result.EffectiveTimeScale>0&&double.IsFinite(result.EffectiveTimeScale)&&result.EffectiveTimeScale<=1e6,"Combined animation scale budget.");return result;
    }
    private static IReadOnlyList<IffNode> Version(IffNode node)
    {Require(node.Children.Count==1&&node.Children[0].IsForm("0000"),"Unsupported animation proxy version.");return node.Children[0].Children;}
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
