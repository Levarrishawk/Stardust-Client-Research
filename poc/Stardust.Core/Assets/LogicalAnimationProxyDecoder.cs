namespace Stardust.Core.Assets;

public sealed record LogicalAnimationProxyDescription(string ClipPath,IReadOnlyList<float> TimeScales,IffNode? PriorityMetadata)
{public double EffectiveTimeScale=>TimeScales.Aggregate(1.0,(product,value)=>product*value);}
public sealed record LogicalAnimationClipSelection(string TablePath,string LogicalName,string ClipSha256);
public sealed record SelectedLogicalAnimationClip(LogicalAnimationClipSelection Selection,LogicalAnimationProxyDescription Proxy,IffNode Clip);

/// <summary>Bounded proxy/time-scale selection, without priority execution or animation evaluation.</summary>
public static class LogicalAnimationProxyDecoder
{
    public static LogicalAnimationProxyDescription Decode(IffNode template)
    {
        ArgumentNullException.ThrowIfNull(template);var scales=new List<float>();int depth=0;
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
