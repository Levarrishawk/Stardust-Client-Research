using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public enum SessionPhase { Offline, Connecting, Connected, Stopped }
public sealed class SessionModel
{
    public const uint ServerHelloOpcode = 0x53444849;
    public SessionPhase Phase { get; private set; } = SessionPhase.Offline;
    public string LastEvent { get; private set; } = "Created";
    public void Start() { Require(SessionPhase.Offline); Phase = SessionPhase.Connecting; LastEvent = "Connection requested"; }
    public void Apply(MessageEnvelope message)
    {
        Require(SessionPhase.Connecting);
        if (message.Opcode != ServerHelloOpcode) throw new InvalidDataException($"Unexpected mock opcode 0x{message.Opcode:X8}.");
        if (!message.Payload.Span.SequenceEqual("STARDUST"u8)) throw new InvalidDataException("Mock server identity did not match.");
        Phase = SessionPhase.Connected; LastEvent = "Known server response parsed";
    }
    public void Stop() { if (Phase is SessionPhase.Stopped) return; Phase = SessionPhase.Stopped; LastEvent = "Clean shutdown"; }
    private void Require(SessionPhase required) { if (Phase != required) throw new InvalidOperationException($"Expected {required}, found {Phase}."); }
}
