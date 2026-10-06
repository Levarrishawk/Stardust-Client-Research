using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Owns only synthetic bootstrap transport and root session; scene/FIFO owners are borrowed.</summary>
public sealed class OfflineSoeRootMotionSessionOwner : IDisposable
{
    private readonly SoeClientTransport transport;
    private readonly SceneWorldOfflineRootMotionSession session;
    private readonly string requestHex,responseHex;
    private readonly int owner=Environment.CurrentManagedThreadId;
    private bool disposed;
    public OfflineSoeRootMotionSessionOwner(SceneWorldCoordinator world,SceneWorldObjectMotionStore store,
        SceneWorldClientRootMotionQueue queue,uint connectionId,uint seed,int maxSamples=6000,
        uint counterBase=0,uint timestampBase=0,uint flags=0)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(store);ArgumentNullException.ThrowIfNull(queue);
        _=world.Snapshots();
        if(world.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady))
            throw new InvalidDataException("Active offline world required.");
        var bootstrap=OfflineSoeSessionBootstrap.Create(connectionId,seed);
        SceneWorldOfflineRootMotionSession? root=null;
        try
        {
            root=new(world,store,queue,bootstrap.Transport,seed,maxSamples,counterBase,timestampBase,flags,
                maxPendingPackets:8,maxPendingBytes:496,maximumDatagram:496);
            transport=bootstrap.Transport;session=root;requestHex=bootstrap.RequestHex;responseHex=bootstrap.ResponseHex;
        }
        catch{try{root?.Dispose();}finally{bootstrap.Transport.Close();}throw;}
    }
    private void Guard()
    {if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Offline root owner thread required.");}
    public SoeClientTransport Transport{get{Guard();return transport;}}
    public SceneWorldOfflineRootMotionSession Session{get{Guard();return session;}}
    public string RequestHex{get{Guard();return requestHex;}}
    public string ResponseHex{get{Guard();return responseHex;}}
    public bool IsDisposed{get{Guard();return disposed;}}
    public void Dispose()
    {
        Guard();if(disposed)return;disposed=true;
        try{session.Dispose();}finally{transport.Close();}
    }
}
