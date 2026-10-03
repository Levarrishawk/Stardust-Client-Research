using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Accepted generic coordinator driver; no presentation consumer or protocol implementation.</summary>
public static class SceneMixedPresentationPeerScenarios
{
    private static void Check(bool value,string why) { if(!value)throw new Exception(why); }
    private static string Hex(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(bytes);
    private static object? ObjectView(SceneWorldObjectSnapshot? v)=>v is null?null:new
    {
        ObjectId=v.Object.Handle.ObjectId,v.Object.Handle.Revision,v.Object.Incarnation.CreationRevision,
        RegistryGeneration=v.Object.Handle.SceneGeneration,Phase=v.Object.Phase.ToString(),
        CreateHex=Hex(SceneObjectLifecycleCodec.EncodeCreate(v.Placement.RawCreate)),
        ScalarRevision=v.Replication?.Revision,
        Scalars=v.Replication is {} r?(object)new {r.Scalars.Posture,r.Scalars.Rank,r.Scalars.LinkId,
            HeightBits=BitConverter.SingleToUInt32Bits(r.Scalars.Height),r.Scalars.Shock,r.Scalars.States}:null,
        OpaqueHex=v.Envelopes.Select(e=>Hex(e.CopyOpaqueFields())).ToArray()
    };
    private static object? BatchView(SceneWorldChangeBatch? b)=>b is null?null:new
    {b.Generation,b.MutationRevision,Entries=b.Entries.Select(e=>new {e.ObjectId,Kind=(int)e.Kind,Current=ObjectView(e.Current)}).ToArray()};
    private static object TicketView(SceneObjectResourceTicket t)=>new {t.Handle.ObjectId,t.Handle.Revision,t.Attempt};
    private sealed record Job(int Index,SceneObjectResourceRequest Request,CancellationToken Token,TaskCompletionSource<SceneObjectResources> Completion);
    public sealed record Step(string Name,string Op,string? Key,int? Job);
    public sealed record Peer(int Version,string Label,Dictionary<string,string> Applications,string[] Initial,string SessionResponse,string[] Transcript,Step[] Actions);

    public static void Run(string originalRoot,string configurationPath,string catalogPath,string bankPath,string peerPath,string exportPath)
    {
        byte[] fixture=File.ReadAllBytes(peerPath);Check(fixture.Length<=1048576,"Fixture byte cap");
        var peer=JsonSerializer.Deserialize<Peer>(fixture)??throw new InvalidDataException("Peer required");
        Check(peer.Version==1&&peer.Initial.Length==7&&peer.Actions.Length<=64,"Bounded v1 shape");
        byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        var human=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate"));
        var terminal=SceneObjectLifecycleCodec.DecodeCreate(Bytes("terminalCreate"));
        Check(human.ObjectId!=terminal.ObjectId&&human.ObjectId>long.MaxValue&&terminal.ObjectId>long.MaxValue,"Independent high IDs");
        byte[] configuration=File.ReadAllBytes(configurationPath);
        Check(Hex(SHA256.HashData(configuration))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration identity");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(catalogPath));
        using var bankFile=File.OpenRead(bankPath);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>
        {
            [SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true),
            [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))
        };
        int archiveScopes=0,originalJobs=0,owner=Environment.CurrentManagedThreadId;
        SceneObjectResources LoadOriginal(SceneCreateObjectByCrcMessage create)
        {
            return Task.Factory.StartNew(()=>
            {
                Interlocked.Increment(ref originalJobs);Interlocked.Increment(ref archiveScopes);
                try
                {
                    using var text=new StringReader(System.Text.Encoding.UTF8.GetString(configuration));
                    using var overlay=ConfiguredTreOverlay.Open(originalRoot,text);
                    Check(Environment.CurrentManagedThreadId!=owner,"Original worker ownership");
                    return SceneObjectResourceRouter.Load(create,catalog,options[catalog.Require(create.TemplateCrc).Recipe],overlay.Lookup);
                }
                finally {Interlocked.Decrement(ref archiveScopes);}
            },CancellationToken.None,TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,TaskScheduler.Default).GetAwaiter().GetResult();
        }
        // Sealed completions come from the accepted original driver, never fabricated constructors.
        var resources=new Dictionary<SceneObjectResourceRecipe,SceneObjectResources>
        {
            [SceneObjectResourceRecipe.CreatureDetail0]=LoadOriginal(human),
            [SceneObjectResourceRecipe.StaticAppearance]=LoadOriginal(terminal)
        };
        Check(archiveScopes==0,"All original archive scopes close before deterministic scheduling");
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,16,1024),32,16384);
        SceneWorldCoordinator NewWorld(Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>> loader)=>
            new(catalog,options,loader,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:2,maximumOutstandingJobs:3,
                maximumStagedMessages:16,maximumEnvelopes:8,maximumEnvelopeBytes:4096,maximumPayloadBytes:1024,replicationPolicy:policy);
        long StageDirect(SceneWorldCoordinator w,bool bad=false)
        {
            long g=w.BeginSelection(human.ObjectId).Generation;
            foreach(string key in peer.Initial)Check(w.Accept(g,Bytes(key)),"Initial current stage");
            if(bad)Check(w.Accept(g,Bytes("lateIntactBad")),"Intact outer bad tail stages");
            return g;
        }
        var jobs=new List<Job>();
        Task<SceneObjectResources> Controlled(SceneObjectResourceRequest request,CancellationToken token)
        {var completion=new TaskCompletionSource<SceneObjectResources>();jobs.Add(new(jobs.Count,request,token,completion));return completion.Task;}
        using var world=NewWorld(Controlled);
        long generation=world.BeginSelection(human.ObjectId).Generation;
        var transport=new SoeClientTransport();transport.BeginSession(0x12345678,496);transport.AcceptSessionResponse(Convert.FromHexString(peer.SessionResponse));
        var delivered=new List<string>();var events=new List<object>();var held=new Dictionary<string,SceneWorldChangeBatch>();
        var publications=new List<ScenePreparedObjectPublication>();
        bool stateDoesNotReload=false,oldRetired=false,humanPreserved=false,foreignRejected=false,familyClears=false;
        ScenePreparedObjectPublication? humanPublication=null;SceneWorldReplicationSnapshot? humanState=null;string? humanObservation=null;
        using var foreign=NewWorld((q,ct)=>Task.FromResult(resources[q.Options.Recipe]));
        long foreignGeneration=StageDirect(foreign);foreign.CompleteMinimumSceneBarrier(foreignGeneration,true);
        var foreignBatch=foreign.DrainChanges(foreignGeneration)!;
        try
        {
            foreach(var step in peer.Actions)
            {
                object? result=null;
                switch(step.Op)
                {
                    case "stage":
                        int framingRejects=0;
                        foreach(string wire in peer.Transcript)
                        {
                            var response=transport.AcceptDatagram(Convert.FromHexString(wire));if(response.RejectedFraming)framingRejects++;
                            foreach(byte[] app in response.ApplicationPayloads)
                            {delivered.Add(Hex(app));Check(world.Accept(generation,app),"Ordered application stage");Array.Clear(app);}
                        }
                        Check(framingRejects==1&&delivered.SequenceEqual(peer.Initial.Select(k=>peer.Applications[k])),"Corrupt/reordered peer exact stage order");result=true;break;
                    case "barrier":result=Hex(world.CompleteMinimumSceneBarrier(generation,true)??throw new Exception("Ready required"));break;
                    case "accept":
                        result=world.Accept(generation,Bytes(step.Key!));
                        if(step.Name=="state-while-delayed")stateDoesNotReload=jobs.Count==2&&world.OutstandingJobs==2&&world.Snapshots().Single(v=>v.Object.Handle.ObjectId==human.ObjectId).Replication!.Scalars.Height==BitConverter.UInt32BitsToSingle(0x3FB33333);
                        if(step.Name=="terminal-only-destroy")
                        {Check(humanPublication is not null&&world.IsCurrent(humanPublication)&&humanState is not null&&world.IsCurrent(humanState),"Independent human remains current");}
                        break;
                    case "hold":held[step.Key!]=world.DrainChanges(generation)!;result=BatchView(held[step.Key!]);break;
                    case "ack":result=world.AcknowledgeChanges(held[step.Key!]);break;
                    case "foreign":
                        ulong currentMutation=world.DrainChanges(generation)!.MutationRevision;
                        while(foreignBatch.MutationRevision<currentMutation)
                        {foreign.Accept(foreignGeneration,Bytes("heightA"));foreignBatch=foreign.DrainChanges(foreignGeneration)!;}
                        Check(foreignBatch.Generation==generation&&foreignBatch.MutationRevision==currentMutation,"Foreign batch otherwise matches current generation/revision");
                        result=world.AcknowledgeChanges(foreignBatch);break;
                    case "complete":
                        var job=jobs[step.Job!.Value];job.Completion.SetResult(resources[job.Request.Options.Recipe]);result=true;break;
                    case "poll":
                        var poll=world.PollObjectResources();publications.AddRange(poll.Prepared);
                        result=new {Prepared=poll.Prepared.Select(p=>TicketView(p.Ticket)).ToArray(),Retired=poll.Retired.Select(r=>new {r.Ticket.Handle.ObjectId,r.Ticket.Handle.Revision,r.Ticket.Attempt,Kind=r.Kind.ToString(),HasResources=r.Resources is not null}).ToArray()};
                        if(step.Name=="old-retired")oldRetired=poll.Prepared.Count==0&&poll.Retired.Count==1&&poll.Retired[0].Resources is null&&poll.Retired[0].Kind==SceneObjectResourceCompletionKind.Stale;
                        if(step.Name=="publish-current")foreignRejected=publications.All(p=>!foreign.IsCurrent(p));
                        if(step.Name=="terminal-replacement-start")
                        {Check(jobs.Count==4&&jobs[3].Request.Options.Recipe==SceneObjectResourceRecipe.StaticAppearance,"Only terminal reload");}
                        if(step.Name=="terminal-replacement-publish")
                        {
                            humanPreserved=humanPublication is not null&&world.IsCurrent(humanPublication)&&humanState is not null&&world.IsCurrent(humanState)&&
                                JsonSerializer.Serialize(ObjectView(world.Snapshots().Single(v=>v.Object.Handle.ObjectId==human.ObjectId)))==humanObservation;
                        }
                        break;
                    case "fail":
                        Check(world.DrainChanges(generation)!.Entries.Count>0,"Pending state before family failure");
                        bool rejected=false;try{world.Accept(generation,Bytes(step.Key!));}catch(InvalidDataException){rejected=true;}
                        familyClears=rejected&&world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0&&world.DrainChanges(generation) is null&&publications.All(p=>!world.IsCurrent(p));result=rejected;break;
                    case "close":world.Close();result=true;break;
                    default:throw new InvalidDataException("Unknown peer action");
                }
                if(step.Name=="terminal-opaque-state")
                {
                    humanPublication=publications.Single(p=>p.Ticket.Handle.ObjectId==human.ObjectId);
                    humanState=world.FindReplication(generation,human.ObjectId)!;
                    humanObservation=JsonSerializer.Serialize(ObjectView(world.Snapshots().Single(v=>v.Object.Handle.ObjectId==human.ObjectId)));
                }
                events.Add(new {step.Name,Result=result,State=world.State.ToString(),world.Generation,world.OutstandingJobs,
                    Objects=world.Snapshots().Select(ObjectView).ToArray(),
                    Calls=jobs.Select(j=>new {JobIndex=j.Index,j.Request.Snapshot.Handle.ObjectId,j.Request.Snapshot.Handle.Revision,
                        j.Request.Snapshot.Incarnation.CreationRevision,RegistryGeneration=j.Request.Snapshot.Handle.SceneGeneration,
                        Completed=j.Completion.Task.IsCompleted,Canceled=j.Token.IsCancellationRequested}).ToArray(),
                    Publications=publications.Select(p=>new {p.Ticket.Handle.ObjectId,p.Ticket.Handle.Revision,p.Ticket.Attempt,Current=world.IsCurrent(p)}).ToArray(),
                    Pending=BatchView(world.DrainChanges(generation))});
            }
        }
        finally
        {
            world.Close();transport.Close();foreach(var j in jobs)j.Completion.TrySetException(new Exception("Synthetic close drain"));
            world.DrainCompletion.GetAwaiter().GetResult();Array.Clear(configuration);
        }
        bool lateNoReady=false,lateNoBatch=false,faultNoPrefix=false,faultNoBatch=false,staleBatchRejected=false;
        using(var late=NewWorld((q,ct)=>Task.FromResult(resources[q.Options.Recipe])))
        {
            long g=StageDirect(late,true);bool returned=false;
            try {late.CompleteMinimumSceneBarrier(g,true);returned=true;}catch(InvalidDataException){}
            lateNoReady=!returned&&late.State==ZoneSceneTransitionState.Failed&&late.Snapshots().Count==0&&late.OutstandingJobs==0;
            lateNoBatch=late.DrainChanges(g) is null;
        }
        var faultJobs=new List<TaskCompletionSource<SceneObjectResources>>();var faultRequests=new List<SceneObjectResourceRequest>();
        using(var fault=NewWorld((q,ct)=>{faultRequests.Add(q);var task=new TaskCompletionSource<SceneObjectResources>();faultJobs.Add(task);return task.Task;}))
        {
            long g=StageDirect(fault);fault.CompleteMinimumSceneBarrier(g,true);fault.PollObjectResources();
            faultJobs[0].SetResult(resources[faultRequests[0].Options.Recipe]);faultJobs[1].SetException(new Exception("Synthetic current fault"));
            bool returned=false;try{fault.PollObjectResources();returned=true;}catch(InvalidDataException){}
            faultNoPrefix=!returned&&fault.State==ZoneSceneTransitionState.Failed&&fault.Snapshots().Count==0&&fault.OutstandingJobs==0;
            faultNoBatch=fault.DrainChanges(g) is null;
        }
        foreign.BeginSelection(human.ObjectId);staleBatchRejected=!foreign.AcknowledgeChanges(foreignBatch)&&foreign.DrainChanges(foreign.Generation) is null;
        bool closedDrained=world.DrainCompletion.IsCompleted&&world.OutstandingJobs==0&&archiveScopes==0;
        Check(stateDoesNotReload&&oldRetired&&humanPreserved&&foreignRejected&&familyClears&&closedDrained&&lateNoReady&&lateNoBatch&&faultNoPrefix&&faultNoBatch&&staleBatchRejected,"All lifecycle/negative observations");
        var output=new {Events=events,Delivered=delivered,OriginalJobs=originalJobs,ArchiveScopes=archiveScopes,FinalOutstanding=world.OutstandingJobs,
            Driver=resources.OrderBy(r=>(int)r.Key).Select(r=>new {r.Value.Create.ObjectId,Recipe=r.Key.ToString(),CreateHex=Hex(SceneObjectLifecycleCodec.EncodeCreate(r.Value.Create)),r.Value.ExternalReads,r.Value.SourceBytes}).ToArray(),
            Checks=new {StateDoesNotReload=stateDoesNotReload,OldCompletionRetired=oldRetired,HumanUnchangedOnTerminalRecreate=humanPreserved,
                ForeignPublicationRejected=foreignRejected,FamilyFailureClearsPending=familyClears,ClosedDrained=closedDrained},
            Negatives=new {LateIntactNoReady=lateNoReady,LateIntactNoBatch=lateNoBatch,CurrentFaultNoPreparedPrefix=faultNoPrefix,
                CurrentFaultNoBatch=faultNoBatch,StaleGenerationBatchRejected=staleBatchRejected}};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
        File.WriteAllBytes(exportPath,JsonSerializer.SerializeToUtf8Bytes(output,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS mixed presentation peer: {events.Count} deterministic events, {jobs.Count} controlled loader calls, two original closures, atomic negative and drain checks");
    }
}
