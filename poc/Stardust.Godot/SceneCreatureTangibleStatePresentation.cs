using System;
using Stardust.Core.Protocol;

/// <summary>Current common visibility only; raw byte and unrelated common state remain model-owned.</summary>
public static class SceneCreatureTangibleStatePresentation
{
    public static bool IsVisible(CreatureView3TangibleScalarState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Visibility != 0;
    }
}
