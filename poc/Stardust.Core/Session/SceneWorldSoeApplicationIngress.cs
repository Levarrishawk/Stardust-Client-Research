using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum SceneWorldSoeApplicationIngressStatus { Accepted, RejectedFraming, StaleGeneration, ClosedWorld, TransportNotEstablished }
/// <summary>Controls transfer to caller ownership. No byte arrays are retained by the bridge.</summary>
public sealed record SceneWorldSoeApplicationIngressReceipt(SceneWorldSoeApplicationIngressStatus Status,
    int ReturnedApplications, int QueuedApplications, int QueuedBytes, IReadOnlyList<byte[]> ControlDatagrams,
    bool PeerDisconnected)
{
    public bool Accepted => Status == SceneWorldSoeApplicationIngressStatus.Accepted;
}

/// <summary>Offline SOE receive to copied pump queue; pump/world binding is caller responsibility.</summary>
public sealed class SceneWorldSoeApplicationIngress : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldPresentationPump? pump;
    private SoeClientTransport? transport;
    private readonly int maximumApplications, maximumBytes, ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, disposed, faulted;
    private ulong processedDatagrams, queuedApplications;
    public SceneWorldSoeApplicationIngress(SceneWorldCoordinator world, SceneWorldPresentationPump pump,
        SoeClientTransport transport, int maxApplicationsPerDatagram, int maxApplicationBytes)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(pump); ArgumentNullException.ThrowIfNull(transport);
        if (maxApplicationsPerDatagram is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maxApplicationsPerDatagram));
        if (maxApplicationBytes is < 1 or > 65536) throw new ArgumentOutOfRangeException(nameof(maxApplicationBytes));
        _ = world.Snapshots(); _ = pump.QueuedMessageCount;
        this.world=world;this.pump=pump;this.transport=transport;maximumApplications=maxApplicationsPerDatagram;maximumBytes=maxApplicationBytes;
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("SOE ingress owner thread required.");
        if(busy)throw new InvalidOperationException("SOE ingress reentrancy.");
        busy=true;
    }
    public bool IsFaulted { get { Enter();try{return faulted;}finally{busy=false;} } }
    public ulong ProcessedDatagramCount { get { Enter();try{return processedDatagrams;}finally{busy=false;} } }
    public ulong QueuedApplicationCount { get { Enter();try{return queuedApplications;}finally{busy=false;} } }
    public SceneWorldSoeApplicationIngressReceipt Receive(long generation, ReadOnlySpan<byte> datagram,
        CancellationToken cancellationToken = default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);cancellationToken.ThrowIfCancellationRequested();
            if(faulted)throw new InvalidOperationException("SOE ingress is terminally faulted.");
            SceneWorldSoeApplicationIngressReceipt Decline(SceneWorldSoeApplicationIngressStatus status)=>new(status,0,0,0,Array.Empty<byte[]>(),false);
            if(generation!=world!.Generation)return Decline(SceneWorldSoeApplicationIngressStatus.StaleGeneration);
            if(world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed)return Decline(SceneWorldSoeApplicationIngressStatus.ClosedWorld);
            _=world.Snapshots();_=pump!.QueuedMessageCount; // Affinity/open-generation preflight before transport mutation.
            if(transport!.State!=SoeClientTransportState.Established)return Decline(SceneWorldSoeApplicationIngressStatus.TransportNotEstablished);
            ulong next=checked(processedDatagrams+1);_=checked(queuedApplications+(ulong)maximumApplications);
            SoeInboundDatagramResult? received=null;
            bool controlsTransferred=false;
            try
            {
                received=transport.AcceptDatagram(datagram);processedDatagrams=next;
                if(received.ApplicationPayloads.Count>maximumApplications)throw new InvalidDataException("SOE ingress application count bound.");
                int bytes=0;
                foreach(var application in received.ApplicationPayloads)
                {
                    if(application.Length<1||application.Length>maximumBytes||application.Length>maximumBytes-bytes)
                        throw new InvalidDataException("SOE ingress per-application/total byte bound.");
                    bytes+=application.Length;
                }
                int queued=0;
                foreach(var application in received.ApplicationPayloads)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if(generation!=world.Generation||world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed)
                        throw new InvalidDataException("World changed after transport receive.");
                    pump.QueueApplication(generation,application);queued++;queuedApplications++;
                    Array.Clear(application); // Pump owns its copied bytes.
                }
                var receipt=new SceneWorldSoeApplicationIngressReceipt(received.RejectedFraming?SceneWorldSoeApplicationIngressStatus.RejectedFraming:SceneWorldSoeApplicationIngressStatus.Accepted,
                    received.ApplicationPayloads.Count,queued,bytes,received.ControlDatagrams,received.PeerDisconnected);
                controlsTransferred=true;return receipt;
            }
            catch { faulted=true;throw; } // Reliability and earlier queued copies are completed prefixes.
            finally
            {
                if(received is not null)
                {
                    foreach(var application in received.ApplicationPayloads)Array.Clear(application);
                    if(!controlsTransferred)foreach(var control in received.ControlDatagrams)Array.Clear(control);
                }
            }
        }finally{busy=false;}
    }
    public void Dispose()
    {Enter();try{if(disposed)return;disposed=true;world=null;pump=null;transport=null;}finally{busy=false;}}
}
