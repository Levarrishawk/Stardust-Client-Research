using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class DoorInstanceCapabilityScenarios
{
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--candidates" || args[6]!="--expected-candidates-sha256")throw new ArgumentException("Door instance probe requires pinned candidates.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Door instance candidate byte budget.");
        byte[] bytes=File.ReadAllBytes(args[5]);if(Convert.ToHexString(SHA256.HashData(bytes))!=args[7].ToUpperInvariant())throw new InvalidDataException("Candidate identity mismatch.");
        using var candidates=JsonDocument.Parse(bytes);if(candidates.RootElement.ValueKind!=JsonValueKind.Array || candidates.RootElement.GetArrayLength()>1024)throw new InvalidDataException("Door instance candidate count budget.");
        using StreamReader cfg=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],cfg);HashSet<(string,int,int)> seen=new();
        foreach(var candidate in candidates.RootElement.EnumerateArray())
        {
            string member=candidate.GetProperty("member").GetString()!,sha=candidate.GetProperty("layout_sha256").GetString()!,leafSha=candidate.GetProperty("primary_sha256").GetString()!;int cell=candidate.GetProperty("cell").GetInt32(),portal=candidate.GetProperty("portal").GetInt32();
            if(!seen.Add((member,cell,portal)))throw new InvalidDataException("Duplicate door instance candidate.");
            string? failure=null,status=null,diagnostic=null;long total=0;int owner=cell;bool passed=false;object? initial=null;object? sources=null;object? trajectory=null;object? firstInvalid=null;float fraction=0;
            try
            {
                var bundle=PortalSingleLeafDoorBundleReader.Load(overlay.Lookup,member,sha,cell,portal,"C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7",leafSha);sources=bundle.Sources.Values;
                var approach=bundle.InspectionApproach();initial=new{triangle=approach.Start.TriangleIndex,local=InteriorDoorScenarios.V(approach.Start.LocalPosition),end=InteriorDoorScenarios.V(approach.End)};
                var direction=approach.End-approach.Start.LocalPosition;direction.Y=0;direction=Vector3.Normalize(direction);
                using var actor=new PortalDoorActorSimulation(bundle,approach.Start);
                void Step(Vector3 delta)
                {
                    var before=actor.State;var after=actor.Tick(.05f,delta,true);
                    if(firstInvalid is null && after.Movement?.Movement.Status==CellFloorWalkStatus.InvalidStart)
                        firstInvalid=new{frame=after.Frame,cell=before.Locator.CellIndex,triangle=before.Locator.TriangleIndex,local=InteriorDoorScenarios.V(before.Locator.LocalPosition),prior_status=before.Movement?.Movement.Status.ToString(),prior_parameter=before.Movement?.Movement.Parameter};
                }
                for(int i=0;i<20;i++)Step(Vector3.Zero);
                for(int i=0;i<20;i++)Step(direction*.08f);
                var reached=actor.State;
                bool forward=actor.State.Locator.CellIndex==bundle.TargetCell && actor.State.TotalTransfers==1;
                for(int i=0;i<20;i++)Step(-direction*.08f);
                var s=actor.State;total=s.TotalTransfers;owner=s.Locator.CellIndex;status=s.Movement?.Movement.Status.ToString();diagnostic=s.Movement?.Diagnostic;fraction=s.Door.Controller.Fraction;
                trajectory=new{forward_owner=reached.Locator.CellIndex,forward_transfers=reached.TotalTransfers,forward_status=reached.Movement?.Movement.Status.ToString(),forward_local=InteriorDoorScenarios.V(reached.Locator.LocalPosition),final_triangle=s.Locator.TriangleIndex,final_local=InteriorDoorScenarios.V(s.Locator.LocalPosition),return_distance=Vector3.Distance(s.Locator.LocalPosition,approach.Start.LocalPosition)};
                passed=forward && total==2 && owner==cell && Vector3.Distance(s.Locator.LocalPosition,approach.Start.LocalPosition)<1e-4;
            }
            catch(InvalidDataException error){failure=error.Message;}
            Console.WriteLine("door_instance_capability="+JsonSerializer.Serialize(new{member,cell,portal,passed,failure,status,diagnostic,total,owner,fraction,initial,sources,trajectory,firstInvalid}));
        }
    }
}
