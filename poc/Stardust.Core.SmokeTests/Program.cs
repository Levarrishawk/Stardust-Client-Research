using System.Buffers.Binary;
using Stardust.Core.Proof;
using Stardust.Core.Protocol;
foreach (string entry in ProofScenario.Run()) Console.WriteLine(entry);
byte[] malformed = new byte[MessageCodec.HeaderSize];
BinaryPrimitives.WriteUInt16LittleEndian(malformed.AsSpan(sizeof(uint)), 1);
try { MessageCodec.Decode(malformed); throw new Exception("Malformed input was unexpectedly accepted."); }
catch (InvalidDataException) { Console.WriteLine("Malformed input rejected"); }
Console.WriteLine("PASS: engine-neutral lifecycle and codec smoke test");
