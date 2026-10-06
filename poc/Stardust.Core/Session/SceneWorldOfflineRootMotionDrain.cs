using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record SceneWorldOfflineRootMotionDrainReceipt(ushort Sequence, string ApplicationHex,
    string DatagramHex, string AckHex, ulong AcceptedCount);

/// <summary>One offline channel-zero root and independently supplied ACK; no socket or handshake.</summary>
public sealed class SceneWorldOfflineRootMotionDrain : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldClientRootMotionQueue? queue;
    private SoeClientTransport? transport;
    private SceneWorldRootMotionTransportBridge? bridge;
    private readonly uint seed;
    private readonly int maximumDatagram, owner = Environment.CurrentManagedThreadId;
    private ushort sequence;
    private ulong accepted, bridgeAccepted;
    private long lastNow = -1;
    private bool disposed, busy, faulted;
    private SceneWorldOfflineRootMotionDrainReceipt? lastReceipt;
    public SceneWorldOfflineRootMotionDrain(SceneWorldCoordinator world, SceneWorldClientRootMotionQueue queue,
        SoeClientTransport transport, uint seed, int maximumDatagram = 496)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(queue);ArgumentNullException.ThrowIfNull(transport);
        if(maximumDatagram is < 69 or > SoeClientTransport.MaximumWireDatagramSize)throw new ArgumentOutOfRangeException(nameof(maximumDatagram));
        _=world.Snapshots();_=queue.Count;
        bridge=new(queue,transport);this.world=world;this.queue=queue;this.transport=transport;
        this.seed=seed;this.maximumDatagram=maximumDatagram;
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Offline drain owner thread required.");
        if(busy)throw new InvalidOperationException("Offline drain reentrancy.");busy=true;
    }
    public ulong AcceptedCount {get{Enter();try{return accepted;}finally{busy=false;}}}
    public ulong BridgeAcceptedCount {get{Enter();try{return bridgeAccepted;}finally{busy=false;}}}
    public ushort NextSequence {get{Enter();try{return sequence;}finally{busy=false;}}}
    public long LastNowMilliseconds {get{Enter();try{return lastNow;}finally{busy=false;}}}
    public bool IsFaulted {get{Enter();try{return faulted;}finally{busy=false;}}}
    public SceneWorldOfflineRootMotionDrainReceipt? LastReceipt {get{Enter();try{return lastReceipt;}finally{busy=false;}}}
    public SceneWorldOfflineRootMotionDrainReceipt? TryDrainNext(long generation, long nowMilliseconds,
        ReadOnlySpan<byte> protectedAck, CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);cancellationToken.ThrowIfCancellationRequested();
            if(faulted)throw new InvalidOperationException("Offline drain terminally faulted.");
            if(nowMilliseconds<0||nowMilliseconds<lastNow)throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
            _=world!.Snapshots();
            if(generation!=world.Generation||world.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady)||
                transport!.State!=SoeClientTransportState.Established||transport.PendingMessageCount!=0||transport.RetainedOutboundPackets!=0||queue!.Count==0)
                return null;
            ulong nextAccepted=checked(accepted+1);
            byte[] ack=Array.Empty<byte>();
            try
            {
                if(!SoeDatagramCodec.TryDecode(protectedAck,seed,2,maximumDatagram,out ack)||ack.Length!=4||ack[0]!=0||ack[1]!=0x15||BinaryPrimitives.ReadUInt16BigEndian(ack.AsSpan(2))!=sequence)
                    throw new InvalidDataException("Protected channel-zero ACK for exact expected sequence required.");
            }finally{CryptographicOperations.ZeroMemory(ack);}
            bool transferred;
            try{transferred=bridge!.TryQueueNext(generation,cancellationToken);bridgeAccepted=bridge.AcceptedCount;}
            catch{bridgeAccepted=bridge!.AcceptedCount;if(bridge.IsFaulted)faulted=true;throw;}
            if(!transferred)return null; // Queue may have discarded stale incarnation prefixes under its own policy.
            lastNow=nowMilliseconds;
            IReadOnlyList<byte[]> datagrams=Array.Empty<byte[]>();byte[] packet=Array.Empty<byte>(),canonical=Array.Empty<byte>();
            SoeInboundDatagramResult? ackResult=null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                datagrams=transport!.PumpQueuedDatagrams(nowMilliseconds);
                if(datagrams.Count!=1||!SoeDatagramCodec.TryDecode(datagrams[0],seed,2,maximumDatagram,out packet)||
                    packet.Length!=66||packet[0]!=0||packet[1]!=9||BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2))!=sequence)
                    throw new InvalidDataException("Dedicated transport must emit one expected reliable channel-zero root.");
                var raw=ObjectControllerTransformCodec.Decode(packet.AsSpan(4),ObjectControllerTransformDirection.ClientToServer);
                if(raw.Kind!=ObjectControllerTransformKind.Root)throw new InvalidDataException("Root required.");
                canonical=ObjectControllerTransformCodec.Encode(raw);
                if(!canonical.AsSpan().SequenceEqual(packet.AsSpan(4)))throw new InvalidDataException("Canonical C2S root required.");
                string applicationHex=Convert.ToHexString(packet.AsSpan(4)),datagramHex=Convert.ToHexString(datagrams[0]),ackHex=Convert.ToHexString(protectedAck);
                ackResult=transport.AcceptDatagram(protectedAck);
                if(ackResult.RejectedFraming||ackResult.PeerDisconnected||ackResult.ApplicationPayloads.Count!=0||ackResult.ControlDatagrams.Count!=0||
                    ackResult.ReleasedOutboundPackets!=1||transport.PendingMessageCount!=0||transport.RetainedOutboundPackets!=0)
                    throw new InvalidDataException("Independent ACK must release exactly the single retained root.");
                var receipt=new SceneWorldOfflineRootMotionDrainReceipt(sequence,applicationHex,datagramHex,ackHex,nextAccepted);
                accepted=nextAccepted;sequence=unchecked((ushort)(sequence+1));lastReceipt=receipt;return receipt;
            }
            catch{faulted=true;throw;}
            finally
            {
                foreach(var datagram in datagrams)CryptographicOperations.ZeroMemory(datagram);
                CryptographicOperations.ZeroMemory(packet);CryptographicOperations.ZeroMemory(canonical);
                if(ackResult is not null){foreach(var app in ackResult.ApplicationPayloads)CryptographicOperations.ZeroMemory(app);foreach(var control in ackResult.ControlDatagrams)CryptographicOperations.ZeroMemory(control);}
            }
        }finally{busy=false;}
    }
    public void Dispose()
    {Enter();try{if(disposed)return;disposed=true;bridge!.Dispose();bridge=null;world=null;queue=null;transport=null;}finally{busy=false;}}
}
