namespace Stardust.Core.Assets;

/// <summary>Distance-only DTLA selection over coarse-to-fine upper-bound order.
/// Callers provide the already-computed distance origin, previous index and availability; no engine state is retained.</summary>
public sealed class DetailAppearanceSelector
{
    public DetailAppearanceSelector(DetailAppearanceDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        if(description.Levels.Count is < 1 or > 8) throw new InvalidDataException("Runtime detail level count is outside the exposed slice.");
        Levels = Array.AsReadOnly(description.Levels.OrderByDescending(level=>level.UpperDistance).ToArray());
    }
    public IReadOnlyList<DetailAppearanceLevel> Levels {get;}

    public int Select(float distance, int previousIndex, IReadOnlyList<bool> available,
        float distanceScale = 1, float upperExtension = 0)
    {
        ArgumentNullException.ThrowIfNull(available);
        if(!float.IsFinite(distance) || distance < 0 || !float.IsFinite(distanceScale) || distanceScale <= 0 ||
            !float.IsFinite(upperExtension) || upperExtension < 0 || previousIndex < -1 || previousIndex >= Levels.Count || available.Count != Levels.Count)
            throw new ArgumentOutOfRangeException(nameof(distance),"Invalid bounded detail selection input.");
        // Use wider arithmetic to keep finite user-supplied factors from overflowing intermediate bounds.
        int index = Math.Max(previousIndex,0), direction = 0;
        for(int probes=0;probes<=Levels.Count;probes++)
        {
            DetailAppearanceLevel level = Levels[index];
            double upper = ((double)level.UpperDistance + upperExtension)*distanceScale;
            double lower = (double)level.LowerDistance*distanceScale;
            if(distance > upper)
            {
                if(direction == 1 || index == 0) break;
                index--;direction=-1;continue;
            }
            if(distance < lower && direction != -1)
            {
                if(index == Levels.Count-1) break;
                index++;direction=1;continue;
            }
            break;
        }
        if(available[index]) return index;
        // Neighboring loaded children: prefer the coarser/lower-index neighbor at an equal offset.
        for(int offset=1;offset<Levels.Count;offset++)
        {
            if(index-offset>=0 && available[index-offset]) return index-offset;
            if(index+offset<Levels.Count && available[index+offset]) return index+offset;
        }
        return -1;
    }
}
