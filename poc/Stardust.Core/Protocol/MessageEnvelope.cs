namespace Stardust.Core.Protocol;
public readonly record struct MessageEnvelope(uint Opcode, ReadOnlyMemory<byte> Payload);
