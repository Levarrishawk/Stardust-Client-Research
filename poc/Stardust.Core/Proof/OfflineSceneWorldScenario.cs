using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.Proof;

public sealed record OfflineSceneWorldTranscript(string SessionRequest, string SelectDatagram, string ReadyDatagram, string Retransmission,
    IReadOnlyList<string> DeliveredApplications, IReadOnlyList<string> Controls, int PreparedObjects, string FinalState);

/// <summary>Offline injected peer proof using the existing transport and actual lifecycle/envelope application layouts.</summary>
public static class OfflineSceneWorldScenario
{
    public const ulong ObjectId = 0xF122334455667788;
    public static (OfflineSceneWorldTranscript Transcript, ScenePreparedCreaturePublication Publication) Run(
        SceneWorldCoordinator world, IReadOnlyDictionary<string, string> peer, Action waitForLoader)
    {
        var transport = new SoeClientTransport();
        try
        {
            var selection = world.BeginSelection(ObjectId); long generation = selection.Generation;
            string request = Convert.ToHexString(transport.BeginSession(0x12345678, 128));
            transport.AcceptSessionResponse(Convert.FromHexString(peer["response"]));
            byte[] select = transport.CreateReliableDatagram(selection.CopySelectMessage(), 0);
            Check(Convert.ToHexString(select) == peer["expectedSelect"], "Independent select wire");
            var delivered = new List<string>(); var controls = new List<string>();
            void Receive(string name)
            {
                byte[] wire = Convert.FromHexString(peer[name]); var result = transport.AcceptDatagram(wire);
                controls.AddRange(result.ControlDatagrams.Select(Convert.ToHexString));
                foreach (var payload in result.ApplicationPayloads)
                {
                    delivered.Add(Convert.ToHexString(payload));
                    try { world.Accept(generation, payload); } catch { transport.Close(); throw; }
                    Array.Clear(payload);
                }
                Array.Clear(wire);
            }
            Receive("ackSelect"); Check(transport.RetainedOutboundPackets == 0, "Select acknowledged");
            byte[] bad = Convert.FromHexString(peer["peer_create"]); bad[^1] ^= 1;
            Check(transport.AcceptDatagram(bad).RejectedFraming && world.State == ZoneSceneTransitionState.Selecting, "Corrupt framing never reaches world");
            Receive("peer_create"); Check(world.StagedCount == 0, "Future sequence buffered");
            Receive("peer_start"); Receive("peer_start");
            foreach (string name in new[] { "baseline", "delta", "end", "contain" }) Receive("peer_" + name);
            Check(world.State == ZoneSceneTransitionState.Initializing && world.StagedCount == 5 && world.Snapshots().Count == 0 && world.OutstandingJobs == 0, "No pre-barrier world/load");
            Check(world.CompleteMinimumSceneBarrier(generation, false) is null && world.CompleteMinimumSceneBarrier(generation - 1, true) is null, "Explicit caller barrier/generation");
            byte[] ready = world.CompleteMinimumSceneBarrier(generation, true)!;
            Check(world.CompleteMinimumSceneBarrier(generation, true) is null && world.Snapshots().Single().Object.Phase == SceneObjectPhase.Finalized, "Ordered replay/one-shot ready");
            var snapshot = world.Snapshots().Single(); Check(snapshot.Envelopes.Count == 2 && snapshot.Envelopes.All(e => e.DeclaredCount == 99 && e.CopyOpaqueFields().SequenceEqual(new byte[] { 127, 0, 255 })), "Real owned opaque envelopes");
            byte[] readyWire = transport.CreateReliableDatagram(ready, 100); string readyHex = Convert.ToHexString(readyWire);
            Check(readyHex == peer["expectedReady"], "Independent ready wire");
            Check(transport.GetRetransmissionDatagrams(199, 100).Count == 0, "Retry deadline");
            byte[] retry = transport.GetRetransmissionDatagrams(200, 100).Single(); Check(retry.SequenceEqual(readyWire), "Exact ready retry");
            Array.Clear(readyWire); Check(Convert.ToHexString(transport.GetRetransmissionDatagrams(300, 100).Single()) == readyHex, "Owned retained reliable ready");
            Check(world.PollResources().Prepared.Count == 0 && world.OutstandingJobs == 1, "Finalized resource start");
            waitForLoader(); var publication = world.PollResources().Prepared.Single();
            Check(world.IsCurrent(publication) && world.PollResources().Prepared.Count == 0, "One-shot current resource publication");
            Receive("ackReady"); Receive("peer_serverReady"); Check(world.State == ZoneSceneTransitionState.Ready && transport.RetainedOutboundPackets == 0, "Server-ready echo/ack");
            Receive("peer_destroy"); Check(world.Snapshots().Count == 0 && !world.IsCurrent(publication), "Real destroy invalidates publication");
            return (new(request, Convert.ToHexString(select), readyHex, Convert.ToHexString(retry), Array.AsReadOnly(delivered.ToArray()), Array.AsReadOnly(controls.ToArray()), 1, world.State.ToString()), publication);
        }
        finally { transport.Close(); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidDataException("Offline world assertion: " + message); }
}
