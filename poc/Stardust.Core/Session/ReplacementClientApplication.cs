using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>One prepared human application; engine presentation remains caller-owned.</summary>
public sealed class ReplacementClientApplication : IDisposable
{
    public sealed class StartupFailure : Exception
    {
        public string FailureType { get; }
        public Task RollbackCompletion { get; }
        internal StartupFailure(Exception error, Task rollback) : base("Application startup failed: " + error.GetType().Name)
        { FailureType = error.GetType().Name; RollbackCompletion = rollback; }
    }
    private readonly int owner = Environment.CurrentManagedThreadId;
    private readonly object lookupGate = new();
    private readonly List<OwnedPort> ports = [];
    private readonly List<string> cleanupFailures = [];
    private readonly List<Task> immediateFailures = [];
    private readonly List<Task> resourceTasks = [], minimumTasks = [];
    private readonly Func<ReplacementConnection, DatagramPeer, IDatagramPort> portFactory;
    private readonly DatagramPeer zonePeer;
    private readonly ReplacementEndpointApproval? endpointApproval;
    private readonly ReplacementLaunchPreparation preparation;
    private readonly ReplacementClientSession session;
    private readonly SceneWorldCoordinator world;
    private readonly SceneWorldObjectMotionStore motions;
    private readonly ReplacementDatagramPump pump;
    private readonly ReplacementClientRun run;
    private Task? presentation;
    private bool busy, finalized;
    private int closing;
    private string? failure;

    public ReplacementClientApplication(ReplacementLaunchProfile profile, LoginClientOptions options,
        DatagramPeer loginPeer, DatagramPeer zonePeer,
        Func<ReplacementConnection, DatagramPeer, IDatagramPort> portFactory,
        Func<CmdStartSceneMessage, ProceduralTerrainInput, Func<string, TreLookupResult>, CancellationToken, Task> minimumResources,
        SceneWorldClientRootMotionPolicy rootPolicy, SceneCreateZeroQuaternionPolicy zeroPolicy = SceneCreateZeroQuaternionPolicy.Reject,
        ReplacementEndpointApproval? endpointApproval = null)
    {
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(portFactory);
        ArgumentNullException.ThrowIfNull(minimumResources); ArgumentNullException.ThrowIfNull(rootPolicy);
        if (loginPeer.Port != options.Port || zonePeer.Port == 0) throw new ArgumentException("Explicit pinned peer ports required.");
        endpointApproval?.ValidateLogin(options, loginPeer);
        this.endpointApproval = endpointApproval;
        this.portFactory = portFactory; this.zonePeer = zonePeer;
        ReplacementLaunchPreparation? prepared = null; ReplacementClientSession? s = null;
        SceneWorldCoordinator? w = null; SceneWorldObjectMotionStore? m = null;
        ReplacementDatagramPump? p = null; ReplacementClientRun? r = null;
        try
        {
            prepared = ReplacementLaunchPreparation.Open(profile); preparation = prepared;
            var creature = prepared.Creature;
            w = new(creature.Catalog, new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>
                { [SceneObjectResourceRecipe.CreatureDetail0] = creature.Options },
                (request, token) =>
                {
                    Owner();var task=Task.Run(() => SceneObjectResourceRouter.Load(request.Snapshot.Create,
                        request.Catalog, request.Options, path => Resolve(path, token), token), token);
                    resourceTasks.Add(task);return task;
                }, zeroPolicy,
                maximumObjects: 1, maximumOutstandingJobs: 1, maximumStagedMessages: 256,
                maximumStagedBytes: 1048576, maximumEnvelopes: 256, maximumEnvelopeBytes: 1048576,
                replicationPolicy: new(SceneWorldReplicationMode.CreatureView3Scalars, new(1024,1024,1024,128,16384),16,16384),
                creatureView4FullPolicy: new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:1)),
                creatureView6FullPolicy: new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:1)));
            m = new(w,1); s = new(options);
            OwnedPort login = Acquire(ReplacementConnection.Login, loginPeer);
            p = new(s,login,loginPeer);
            r = new(s,p,w,m,(message, token) =>
            {
                var terrain = Resolve(message.Terrain,token);
                if (terrain.Kind != TreLookupKind.Present || terrain.Payload is null) throw new InvalidDataException("Minimum PTAT missing.");
                var decoded = ProceduralTerrainInput.Decode(terrain.Payload, new());
                token.ThrowIfCancellationRequested();
                var task=minimumResources(message,decoded,path=>Resolve(path,token),token)
                    ?? throw new InvalidOperationException("Minimum provider task required.");
                minimumTasks.Add(task);return task;
            },rootPolicy,zeroPolicy);
            session=s;world=w;motions=m;pump=p;run=r;
        }
        catch (Exception error)
        {
            Volatile.Write(ref closing,1);
            var rollback = new List<Task>();
            void Release(Action action) { try { action(); } catch (Exception e) { rollback.Add(Task.FromException(new InvalidOperationException(e.GetType().Name))); } }
            if(r is not null)Release(r.Dispose);if(p is not null){Release(p.Dispose);rollback.Add(p.DrainCompletion);}
            foreach(var port in ports){Release(port.Dispose);rollback.Add(port.Completion);}
            if(w is not null)Release(w.Close);if(m is not null)Release(m.Dispose);if(s is not null)Release(s.Dispose);
            // Startup exposes no world/presentation and never starts selection, resource work or minimum tasks.
            // Only injected port rollback can be asynchronous here; it does not borrow installation inputs.
            if(prepared is not null)Release(prepared.Dispose);
            throw new StartupFailure(error,Task.WhenAll(rollback));
        }
    }
    public ReplacementClientSession Session { get { Current();return session; } }
    public SceneWorldCoordinator World { get { Current();return world; } }
    public SceneWorldObjectMotionStore Motions { get { Current();return motions; } }
    public ReplacementClientRun Run { get { Current();return run; } }
    public ReplacementLaunchProfile Profile { get { Owner();return preparation.Profile; } }
    public InstalledCreaturePreparation Creature { get { Owner();return preparation.Creature; } }
    public bool Closing { get { Owner();return closing!=0; } }
    public bool IsFinalized { get { Owner();return finalized; } }
    public string? FailureType { get { Owner();return failure??run.FailureType; } }
    public IReadOnlyList<string> CleanupFailures { get { Owner();return cleanupFailures.ToArray(); } }
    public Task DrainCompletion
    {
        get { Owner();return Task.WhenAll(new[]{run.DrainCompletion,world.DrainCompletion,pump.DrainCompletion,presentation??Task.CompletedTask}
            .Concat(ports.Select(p=>p.Completion)).Concat(immediateFailures).Concat(resourceTasks.Select(ObserveRetiredWork)).Concat(minimumTasks.Select(ObserveRetiredWork))); }
    }
    public void StartLogin(uint id,long now)=>Operation(()=>run.StartLogin(id,now));
    public void Authenticate(string user,string password,long now)=>Operation(()=>run.Authenticate(user,password,now));
    public void SelectCharacter(ulong objectId,uint zoneId,long now)
    {
        Enter();try
        {
            Active();if(run.Generation!=0)throw new InvalidOperationException("One application selection required.");
            if(endpointApproval is not null)endpointApproval.ValidateZone(session.PreflightZone(objectId,zoneId),zonePeer);
            try { var port=Acquire(ReplacementConnection.Zone,zonePeer);run.SelectCharacter(objectId,zoneId,port,zonePeer,now); }
            catch(Exception error){failure??=error.GetType().Name;Close();throw;}
        }finally{busy=false;}
    }
    public ReplacementClientRunTick Tick(long now)
    {
        Enter();try
        {
            Active();var tick=run.Tick(now);
            if(tick.FailureType is not null){failure??=tick.FailureType;Close();}
            return tick;
        }finally{busy=false;}
    }
    public void BeginDisconnect(long now,long timeoutMilliseconds=3000,ushort reasonWord=0x0006)
        =>Operation(()=>run.BeginDisconnect(now,timeoutMilliseconds,reasonWord));
    public void BeginClose(Task presentationCompletion)
    {
        Enter();try{BindPresentation(presentationCompletion);Close();}finally{busy=false;}
    }
    public bool TryFinalizeClose(Task presentationCompletion)
    {
        Enter();try
        {
            BindPresentation(presentationCompletion);if(finalized)return true;Close();
            Task drain=DrainCompletion;if(!drain.IsCompleted)return false;
            if(!Monitor.TryEnter(lookupGate))return false;
            try
            {
                try{drain.GetAwaiter().GetResult();}
                catch(Exception error)
                {
                    if(drain.Exception is { } aggregate)
                        cleanupFailures.AddRange(aggregate.Flatten().InnerExceptions.Select(e=>e.GetType().Name));
                    else cleanupFailures.Add(error.GetType().Name);
                }
                // Every async borrower and the caller's presentation task is completed, including faults.
                Cleanup(motions.Dispose);Cleanup(session.Dispose);Cleanup(preparation.Dispose);finalized=true;return true;
            }finally{Monitor.Exit(lookupGate);}
        }finally{busy=false;}
    }
    public void Dispose(){Enter();try{Close();}finally{busy=false;}}
    public override string ToString(){Owner();return $"replacement_application_closing={closing!=0} finalized={finalized}";}
    private void BindPresentation(Task completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if(presentation is not null&&!ReferenceEquals(presentation,completion))throw new InvalidOperationException("Exact presentation completion required.");
        presentation=completion;
    }
    private void Operation(Action action)
    {
        Enter();try
        {
            Active();try{action();}catch(Exception error)
            {
                if(run.FailureType is not null||pump.FailureType is not null)
                {failure??=run.FailureType??pump.FailureType??error.GetType().Name;Close();}
                throw;
            }
        }finally{busy=false;}
    }
    private async Task ObserveRetiredWork(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (OperationCanceledException) when (task.IsCanceled && Volatile.Read(ref closing) != 0) { }
    }
    private void Close()
    {
        if(closing!=0)return;Volatile.Write(ref closing,1);
        Cleanup(run.Dispose);Cleanup(pump.Dispose);Cleanup(world.Close);Cleanup(session.Dispose);
        foreach(var port in ports)Cleanup(port.Dispose);
        // Motion/preparation remain retained until presentation and every async borrower completes.
    }
    private void Cleanup(Action action)
    {
        try{action();}catch(Exception error){cleanupFailures.Add(error.GetType().Name);immediateFailures.Add(Task.FromException(new InvalidOperationException(error.GetType().Name)));}
    }
    private TreLookupResult Resolve(string path,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();lock(lookupGate)
        {
            if(Volatile.Read(ref closing)!=0)throw new ObjectDisposedException(nameof(ReplacementClientApplication));
            token.ThrowIfCancellationRequested();return preparation.Inputs.BorrowedOverlay.Lookup(path);
        }
    }
    private OwnedPort Acquire(ReplacementConnection connection,DatagramPeer peer)
    {
        IDatagramPort raw=portFactory(connection,peer)??throw new InvalidOperationException("Factory port required.");
        if(ports.Any(p=>ReferenceEquals(p.Inner,raw)))throw new InvalidOperationException("Distinct factory ports required.");
        var port=new OwnedPort(raw);ports.Add(port);return port;
    }
    private void Owner(){if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Application owner thread required.");}
    private void Current(){Owner();ObjectDisposedException.ThrowIf(finalized,this);}
    private void Active(){Current();if(closing!=0)throw new InvalidOperationException("Application is retiring.");}
    private void Enter(){Owner();if(busy)throw new InvalidOperationException("Application reentry.");busy=true;}
    private sealed class OwnedPort(IDatagramPort inner):IDatagramPort
    {
        public IDatagramPort Inner {get;}=inner;
        public Task Completion {get;private set;}=Task.CompletedTask;
        private bool disposed;
        public ValueTask SendAsync(DatagramPeer peer,ReadOnlyMemory<byte> bytes,CancellationToken token)=>Inner.SendAsync(peer,bytes,token);
        public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)=>Inner.ReceiveAsync(token);
        public ValueTask DisposeAsync(){Dispose();return new(Completion);}
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            try{Completion=Inner.DisposeAsync().AsTask();}catch(Exception error){Completion=Task.FromException(new InvalidOperationException(error.GetType().Name));}
        }
    }
}
