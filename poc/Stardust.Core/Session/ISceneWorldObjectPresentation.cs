namespace Stardust.Core.Session;

/// <summary>Engine adapter for one exact resource publication; state freshness is checked separately.</summary>
public interface ISceneWorldObjectPresentation : IDisposable
{
    ScenePreparedObjectPublication Publication { get; }
    bool Tick(double seconds);
    bool Apply(SceneWorldObjectSnapshot snapshot);
}
