namespace Stardust.Core.Session;

/// <summary>Optional owned asynchronous work; the task remains observable after disposal.</summary>
public interface ISceneWorldObjectPresentationDrain
{
    Task DrainCompletion { get; }
}
