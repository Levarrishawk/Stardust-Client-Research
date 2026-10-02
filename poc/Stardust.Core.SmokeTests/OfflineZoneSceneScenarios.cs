using Stardust.Core.Proof;
namespace Stardust.Core.SmokeTests;
public static class OfflineZoneSceneScenarios
{
    public static void Run()
    {
        OfflineZoneSceneTranscript transcript = OfflineZoneSceneScenario.Run();
        if (transcript.FinalState != "Ready" || transcript.ReadyPublications != 1 || transcript.Staged.Count != 2)
            throw new InvalidDataException("Offline scene transcript did not complete.");
        Console.WriteLine("R3-NET-01 independent peer/reliable scene/barrier/retry/cancellation scenarios passed.");
    }
}
