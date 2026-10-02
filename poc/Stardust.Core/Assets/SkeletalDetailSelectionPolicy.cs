using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Modern centered-sphere projected-diameter thresholds, not original client LOD rules.</summary>
public sealed class SkeletalDetailSelectionPolicy
{
    public IReadOnlyList<double> DescendingThresholds { get; }
    public double HysteresisFraction { get; }
    public SkeletalDetailSelectionPolicy(IReadOnlyList<double> descendingThresholds,double hysteresisFraction)
    {
        ArgumentNullException.ThrowIfNull(descendingThresholds);
        var values=descendingThresholds.ToArray();
        if(values.Length!=3||values.Any(v=>!double.IsFinite(v)||v<=0||v>1e9)||values[0]<=values[1]||values[1]<=values[2])
            throw new ArgumentException("Three strictly descending finite positive pixel thresholds, at most1e9, required.",nameof(descendingThresholds));
        if(!double.IsFinite(hysteresisFraction)||hysteresisFraction<0||hysteresisFraction>.49)
            throw new ArgumentOutOfRangeException(nameof(hysteresisFraction));
        DescendingThresholds=new ReadOnlyCollection<double>(values);HysteresisFraction=hysteresisFraction;
    }
}

public readonly record struct SkeletalDetailProjectionSample(double BoundingRadius,double CenterDepth,double VerticalFovDegrees,int ViewportHeight);
public readonly record struct SkeletalDetailSelection(int Detail,double? ProjectedDiameterPixels,bool NearCamera);

/// <summary>Caller-owned serial state; invalid samples leave Current unchanged. No assets, time or renderer.</summary>
public sealed class SkeletalDetailSelector
{
    public SkeletalDetailSelectionPolicy Policy { get; }
    public SkeletalDetailSelection? Current { get; private set; }
    public SkeletalDetailSelector(SkeletalDetailSelectionPolicy policy)
    {ArgumentNullException.ThrowIfNull(policy);Policy=policy;}
    public void Reset()=>Current=null;
    public SkeletalDetailSelection Select(SkeletalDetailProjectionSample sample)
    {
        if(!double.IsFinite(sample.BoundingRadius)||sample.BoundingRadius<=0||sample.BoundingRadius>1e6||
           !double.IsFinite(sample.CenterDepth)||sample.CenterDepth<0||sample.CenterDepth>1e9||
           !double.IsFinite(sample.VerticalFovDegrees)||sample.VerticalFovDegrees<1||sample.VerticalFovDegrees>179||
           sample.ViewportHeight<1||sample.ViewportHeight>32768)
            throw new ArgumentOutOfRangeException(nameof(sample),"Invalid bounded sphere/projection input.");
        SkeletalDetailSelection next;
        if(sample.CenterDepth<=sample.BoundingRadius)next=new(0,null,true);
        else
        {
            double pixels=(sample.BoundingRadius/sample.CenterDepth)*sample.ViewportHeight/Math.Tan(sample.VerticalFovDegrees*Math.PI/360);
            if(!double.IsFinite(pixels)||pixels<=0)throw new ArgumentOutOfRangeException(nameof(sample),"Invalid projected diameter.");
            var thresholds=Policy.DescendingThresholds;int detail=Current?.Detail??0;
            if(Current is null)
                while(detail<3&&pixels<thresholds[detail])detail++;
            else
            {
                while(detail<3&&pixels<thresholds[detail]*(1-Policy.HysteresisFraction))detail++;
                while(detail>0&&pixels>=thresholds[detail-1]*(1+Policy.HysteresisFraction))detail--;
            }
            next=new(detail,pixels,false);
        }
        Current=next;return next;
    }
}
