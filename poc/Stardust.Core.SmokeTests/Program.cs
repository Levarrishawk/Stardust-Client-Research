using Stardust.Core.Configuration;
using Stardust.Core.Proof;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;

try
{
if (!Environment.Is64BitProcess) throw new Exception("Stardust client smoke tests require a 64-bit process.");
Console.WriteLine("PASS: running engine-neutral core in a 64-bit process");

// Fixed bytes from docs/SOE_TRANSPORT_TEST_VECTORS.md, independent of the encoder.
byte[] requestVector = Convert.FromHexString("00010000000212345678000001F0");
byte[] responseVector = Convert.FromHexString("000212345678A1B2C3D4020104000001F0");
SoeSessionRequest request = new(0x12345678, 496);
AssertBytes(requestVector, SoeSessionCodec.EncodeRequest(request));
if (SoeSessionCodec.DecodeRequest(requestVector) != request) throw new Exception("Request roundtrip failed.");
SoeSessionResponse response = SoeSessionCodec.DecodeResponse(responseVector, request.ConnectionId);
if (response != new SoeSessionResponse(0x12345678, 0xA1B2C3D4, 2, 1, 4, 496))
    throw new Exception("Response vector decoded incorrectly.");

// Every truncated prefix, an extra byte, wrong opcodes/version/ID, and invalid bounds.
for (int n = 0; n < requestVector.Length; n++) Reject(() => SoeSessionCodec.DecodeRequest(requestVector.AsSpan(0, n)));
for (int n = 0; n < responseVector.Length; n++) Reject(() => SoeSessionCodec.DecodeResponse(responseVector.AsSpan(0, n), request.ConnectionId));
Reject(() => SoeSessionCodec.DecodeRequest(Append(requestVector)));
Reject(() => SoeSessionCodec.DecodeResponse(Append(responseVector), request.ConnectionId));
Reject(() => SoeSessionCodec.DecodeRequest(Changed(requestVector, 1, 2)));
Reject(() => SoeSessionCodec.DecodeResponse(Changed(responseVector, 1, 1), request.ConnectionId));
Reject(() => SoeSessionCodec.DecodeRequest(Changed(requestVector, 5, 3)));
Reject(() => SoeSessionCodec.DecodeRequest(requestVector.AsSpan(0, 10).ToArray().Concat(new byte[4]).ToArray()));
Reject(() => SoeSessionCodec.DecodeResponse(responseVector, 0x12345679));
Reject(() => SoeSessionCodec.DecodeResponse(Changed(responseVector, 10, 0), request.ConnectionId));
Reject(() => SoeSessionCodec.DecodeResponse(Changed(responseVector, 10, 5), request.ConnectionId));
Reject(() => SoeSessionCodec.DecodeResponse(responseVector.AsSpan(0, 13).ToArray().Concat(new byte[4]).ToArray(), request.ConnectionId));
try { SoeSessionCodec.EncodeRequest(new SoeSessionRequest(1, 0)); throw new Exception("Zero size encoded."); }
catch (ArgumentOutOfRangeException) { }
Console.WriteLine("PASS: offline SOE session request/response vectors and malformed-input cases");

const uint crcSeed = 0x12345678;
byte[] crcInput = Convert.FromHexString("00090000");
if (SoeSeededCrc.Compute(crcInput, crcSeed) != 0xD0883317)
    throw new Exception("Seeded CRC primitive mismatch.");
foreach ((int width, string hex) in new[] { (1, "17"), (2, "3317"), (4, "D0883317") })
{
    byte[] trailer = new byte[width];
    SoeSeededCrc.WriteTrailer(crcInput, crcSeed, trailer);
    AssertBytes(Convert.FromHexString(hex), trailer);
    byte[] packet = crcInput.Concat(trailer).ToArray();
    if (!SoeSeededCrc.VerifyTrailer(packet, crcSeed, width)) throw new Exception("Valid CRC rejected.");
    packet[0] ^= 1;
    if (SoeSeededCrc.VerifyTrailer(packet, crcSeed, width)) throw new Exception("Corrupt CRC accepted.");
}
if (SoeSeededCrc.VerifyTrailer(new byte[1], crcSeed, 2) ||
    SoeSeededCrc.VerifyTrailer(crcInput, crcSeed, 0) ||
    SoeSeededCrc.VerifyTrailer(crcInput, crcSeed, 5))
    throw new Exception("Invalid CRC bounds accepted.");
Console.WriteLine("PASS: seeded CRC primitive, widths, corruption, and bounds");

byte[] transformInput = Convert.FromHexString("000900011020304050");
byte[] transformExpected = Convert.FromHexString("000978572432483828");
byte[] transformed = (byte[])transformInput.Clone();
SoeSelector4.Encrypt(transformed, crcSeed);
AssertBytes(transformExpected, transformed);
SoeSelector4.Decrypt(transformed, crcSeed);
AssertBytes(transformInput, transformed);
byte[] oneByteOpcodePacket = Convert.FromHexString("090102030405");
byte[] oneByteRoundtrip = (byte[])oneByteOpcodePacket.Clone();
SoeSelector4.Encrypt(oneByteRoundtrip, crcSeed);
SoeSelector4.Decrypt(oneByteRoundtrip, crcSeed);
AssertBytes(oneByteOpcodePacket, oneByteRoundtrip);
Reject(() => SoeSelector4.Decrypt(Array.Empty<byte>(), crcSeed));

byte[] establishedPlain = Convert.FromHexString("00090000AABB");
byte[] establishedWire = Convert.FromHexString("000978569EA9789231");
AssertBytes(establishedWire, SoeDatagramCodec.EncodeUncompressed(establishedPlain, crcSeed, 2, 496));
AssertBytes(establishedPlain, SoeDatagramCodec.Decode(establishedWire, crcSeed, 2, 496));
byte[] ackPlain = Convert.FromHexString("00150000");
byte[] ackWire = Convert.FromHexString("00157878783946");
AssertBytes(ackWire, SoeDatagramCodec.EncodeUncompressed(ackPlain, crcSeed, 2, 496));
AssertBytes(ackPlain, SoeDatagramCodec.Decode(ackWire, crcSeed, 2, 496));
for (int n = 0; n < establishedWire.Length; n++)
    Reject(() => SoeDatagramCodec.Decode(establishedWire.AsSpan(0, n), crcSeed, 2, 496));
for (int index = 0; index < establishedWire.Length; index++)
    Reject(() => SoeDatagramCodec.Decode(Changed(establishedWire, index, (byte)(establishedWire[index] ^ 1)), crcSeed, 2, 496));
Reject(() => SoeDatagramCodec.Decode(Changed(establishedWire, 4, 0), crcSeed, 2, 496));
Reject(() => SoeDatagramCodec.Decode(establishedWire, crcSeed, 2, establishedWire.Length - 1));
Reject(() => SoeDatagramCodec.Decode(establishedWire.AsSpan(0, 4), crcSeed, 2, 496));
try { SoeDatagramCodec.EncodeUncompressed(establishedPlain, crcSeed, 2, 8); throw new Exception("Oversized datagram encoded."); }
catch (InvalidDataException) { }
Console.WriteLine("PASS: selector-4 and complete uncompressed SOE datagram vectors");

byte[] compressibleTransport = new byte[204];
compressibleTransport[1] = 0x09;
byte[] compressedWire = CreateCompressedDatagram(compressibleTransport, crcSeed, 2);
AssertBytes(compressibleTransport, SoeDatagramCodec.Decode(compressedWire, crcSeed, 2, 496));
Reject(() => SoeDatagramCodec.Decode(compressedWire, crcSeed, 2, 100));
byte[] compressedPlain = compressedWire[..^2];
SoeSelector4.Decrypt(compressedPlain, crcSeed);
byte[] missingZlibTrailer = new byte[compressedPlain.Length - 4];
compressedPlain.AsSpan(0, compressedPlain.Length - 5).CopyTo(missingZlibTrailer);
missingZlibTrailer[^1] = 1;
SoeSelector4.Encrypt(missingZlibTrailer, crcSeed);
byte[] truncatedCompressedWire = new byte[missingZlibTrailer.Length + 2];
missingZlibTrailer.CopyTo(truncatedCompressedWire, 0);
SoeSeededCrc.WriteTrailer(missingZlibTrailer, crcSeed, truncatedCompressedWire.AsSpan(missingZlibTrailer.Length));
Reject(() => SoeDatagramCodec.Decode(truncatedCompressedWire, crcSeed, 2, 496));
if (SoeDatagramCodec.TryDecode(truncatedCompressedWire, crcSeed, 2, 496, out _))
    throw new Exception("Valid-CRC datagram with a missing zlib trailer was accepted.");
Console.WriteLine("PASS: bounded receive-side zlib framing");

SoeReliableChannel sender = new();
byte[] sent0 = sender.CreateDataPacket(new byte[] { 0xA0 }, 100);
byte[] sent1 = sender.CreateDataPacket(new byte[] { 0xA1 }, 100);
byte[] sent2 = sender.CreateDataPacket(new byte[] { 0xA2 }, 100);
AssertBytes(Convert.FromHexString("00090000A0"), sent0);
AssertBytes(Convert.FromHexString("00090001A1"), sent1);
AssertBytes(Convert.FromHexString("00090002A2"), sent2);
if (!sender.ApplySelectiveOutOfOrderAcknowledgement(2) || sender.RetainedCount != 2)
    throw new Exception("Selective acknowledgement did not release only sequence 2.");
IReadOnlyList<byte[]> retransmitted = sender.GetPacketsDueForRetransmission(300, 200);
if (retransmitted.Count != 2 || !retransmitted[0].AsSpan().SequenceEqual(sent0) || !retransmitted[1].AsSpan().SequenceEqual(sent1))
    throw new Exception("Timed retransmission set is incorrect.");
if (sender.ApplyCumulativeAcknowledgement(1) != 2 || sender.RetainedCount != 0)
    throw new Exception("Cumulative acknowledgement did not release sequences 0 and 1.");
SoeReliableChannel boundedSender = new(maximumOutstanding: 1);
boundedSender.CreateDataPacket(Array.Empty<byte>(), 0);
try { boundedSender.CreateDataPacket(Array.Empty<byte>(), 0); throw new Exception("Full send window accepted another packet."); }
catch (InvalidOperationException) { }
if (boundedSender.ApplyCumulativeAcknowledgement(5) != 0 || boundedSender.RetainedCount != 1)
    throw new Exception("Acknowledgement for an unsent sequence released retained data.");

SoeReliableChannel receiver = new();
SoeReliableReceiveResult future = receiver.ReceiveDataPacket(sent2);
if (future.Disposition != SoeReliableReceiveDisposition.Future || future.ControlSequence != 2)
    throw new Exception("Future sequence handling failed.");
SoeReliableReceiveResult first = receiver.ReceiveDataPacket(sent0);
if (first.DeliveredPayloads.Count != 1 || first.ControlSequence != 0)
    throw new Exception("Initial in-order delivery failed.");
SoeReliableReceiveResult second = receiver.ReceiveDataPacket(sent1);
if (second.DeliveredPayloads.Count != 2 || second.ControlSequence != 2 || receiver.ExpectedInboundSequence != 3)
    throw new Exception("Contiguous reorder drain failed.");
if (receiver.ReceiveDataPacket(sent1).Disposition != SoeReliableReceiveDisposition.Duplicate)
    throw new Exception("Duplicate sequence was not identified.");
SoeReliableChannel boundedReceiver = new(maximumReorderDistance: 2);
Reject(() => boundedReceiver.ReceiveDataPacket(Convert.FromHexString("00090003")));

if (SoeReliableChannel.ExpandNear(0xFFFF, 0) != 0x10000 ||
    SoeReliableChannel.ExpandNear(0x10000, 0xFFFF) != 0xFFFF)
    throw new Exception("Sequence wrap expansion failed.");
SoeReliableChannel wrapSender = new();
SoeReliableChannel wrapReceiver = new();
for (int sequence = 0; sequence <= 0x10000; sequence++)
{
    byte[] packet = wrapSender.CreateDataPacket(Array.Empty<byte>(), sequence);
    if (sequence == 0xFFFF) AssertBytes(Convert.FromHexString("0009FFFF"), packet);
    if (sequence == 0x10000) AssertBytes(Convert.FromHexString("00090000"), packet);
    SoeReliableReceiveResult wrapResult = wrapReceiver.ReceiveDataPacket(packet);
    if (wrapResult.Disposition != SoeReliableReceiveDisposition.InOrder)
        throw new Exception("In-order sequence failed across wrap.");
    if (wrapSender.ApplyCumulativeAcknowledgement(unchecked((ushort)sequence)) != 1)
        throw new Exception("Cumulative acknowledgement failed across wrap.");
}
if (wrapReceiver.ExpectedInboundSequence != 0x10001 || wrapSender.RetainedCount != 0)
    throw new Exception("Logical sequence state failed across wrap.");
Console.WriteLine("PASS: channel-zero ordering, selective/cumulative ACK, retransmit, and wrap logic");

byte[] loginApplication = Convert.FromHexString(
    "0400961F13410100750100700E0032303236303831372D53443A3031");
AssertBytes(loginApplication, LoginProtocolCodec.EncodeLoginClientId("u", "p", "20260817-SD:01"));
SoeReliableChannel loginSender = new();
byte[] loginTransport = loginSender.CreateDataPacket(loginApplication, 0);
byte[] loginWire = Convert.FromHexString(
    "000978563012EE492353EF495652EF395852DD096A64ED315B53C0621F69F0F1C0F704");
AssertBytes(loginWire, SoeDatagramCodec.EncodeUncompressed(loginTransport, crcSeed, 2, 496));

byte[] tokenVector = Convert.FromHexString("0400C696B2AA05000000544433221188776655010075");
LoginClientToken token = LoginProtocolCodec.DecodeLoginClientToken(tokenVector);
AssertBytes(Convert.FromHexString("5444332211"), token.CredentialBlob);
if (token.AccountId != 0x11223344 || token.StationId != 0x55667788 || token.Username != "u")
    throw new Exception("LoginClientToken decoded incorrectly.");
byte[] errorVector = Convert.FromHexString("03001AF9ABB5010045030062616401");
LoginError loginError = LoginProtocolCodec.DecodeErrorMessage(errorVector);
if (loginError != new LoginError("E", "bad", true)) throw new Exception("ErrorMessage decoded incorrectly.");
for (int n = 0; n < tokenVector.Length; n++) Reject(() => LoginProtocolCodec.DecodeLoginClientToken(tokenVector.AsSpan(0, n)));
for (int n = 0; n < errorVector.Length; n++) Reject(() => LoginProtocolCodec.DecodeErrorMessage(errorVector.AsSpan(0, n)));
Reject(() => LoginProtocolCodec.DecodeLoginClientToken(Append(tokenVector)));
Reject(() => LoginProtocolCodec.DecodeErrorMessage(Append(errorVector)));
try { LoginProtocolCodec.EncodeLoginClientId("u", "p", "bad-\u2603"); throw new Exception("Non-ASCII version encoded."); }
catch (ArgumentException) { }
Console.WriteLine("PASS: LoginClientId wire vector and bounded login response parsers");

SoeControlPacket ackControl = new(SoeControlKind.Acknowledgement, 0);
AssertBytes(ackPlain, SoeControlCodec.Encode(ackControl));
if (SoeControlCodec.Decode(ackPlain) != ackControl) throw new Exception("ACK control roundtrip failed.");
SoeControlPacket outOfOrderControl = new(SoeControlKind.OutOfOrder, 2);
AssertBytes(Convert.FromHexString("00110002"), SoeControlCodec.Encode(outOfOrderControl));
if (SoeControlCodec.Decode(Convert.FromHexString("00110002")) != outOfOrderControl)
    throw new Exception("Out-of-order control roundtrip failed.");
Reject(() => SoeControlCodec.Decode(Convert.FromHexString("00190002")));
Reject(() => SoeControlCodec.Decode(Convert.FromHexString("001100")));
SoeControlPacket channelThreeAck = new(SoeControlKind.Acknowledgement, 0x1234, 3);
AssertBytes(Convert.FromHexString("00181234"), SoeControlCodec.Encode(channelThreeAck));
if (SoeControlCodec.Decode(Convert.FromHexString("00181234")) != channelThreeAck)
    throw new Exception("Channel-three ACK control roundtrip failed.");
Console.WriteLine("PASS: four-channel reliability control packet codecs");

IReadOnlyList<string> transcript = OfflineTransportScenario.Run();
if (transcript.Count != 6) throw new Exception("Offline transcript did not reach completion.");
Console.WriteLine("PASS: offline session-to-login transcript");

SoeClientTransport clientTransport = new();
try { clientTransport.CreateReliableDatagram(Array.Empty<byte>(), 0); throw new Exception("Pre-session send accepted."); }
catch (InvalidOperationException) { }
AssertBytes(requestVector, clientTransport.BeginSession(0x12345678, 496));
clientTransport.AcceptSessionResponse(responseVector);
SoeClientTransport unsupportedSelectors = new();
unsupportedSelectors.BeginSession(0x12345678, 496);
try
{
    unsupportedSelectors.AcceptSessionResponse(Changed(responseVector, 12, 3));
    throw new Exception("Unsupported selector negotiation was accepted.");
}
catch (NotSupportedException) { }
SoeClientTransport undersizedNegotiation = new();
undersizedNegotiation.BeginSession(0x12345678, 496);
byte[] tinyResponse = (byte[])responseVector.Clone();
tinyResponse.AsSpan(13, 4).Clear();
tinyResponse[16] = 4;
Reject(() => undersizedNegotiation.AcceptSessionResponse(tinyResponse));
byte[] retainedDatagram = clientTransport.CreateReliableDatagram(new byte[] { 0xAA }, 10);
if (clientTransport.RetainedOutboundPackets != 1) throw new Exception("Reliable datagram was not retained.");
byte[] protectedAck = SoeDatagramCodec.EncodeUncompressed(ackPlain, 0xA1B2C3D4, 2, 496);
SoeInboundDatagramResult ackResult = clientTransport.AcceptDatagram(protectedAck);
if (ackResult.ReleasedOutboundPackets != 1 || clientTransport.RetainedOutboundPackets != 0)
    throw new Exception("Integrated ACK did not release the retained datagram.");
try { clientTransport.CreateReliableDatagram(new byte[490], 20); throw new Exception("Oversized reliable payload accepted."); }
catch (InvalidDataException) { }
if (clientTransport.RetainedOutboundPackets != 0)
    throw new Exception("Rejected oversized payload polluted retention state.");
if (!clientTransport.AcceptDatagram(Changed(retainedDatagram, 4, 0)).RejectedFraming)
    throw new Exception("Corrupt datagram was not discarded.");
byte[] channelThreeWire = clientTransport.CreateReliableDatagram(new byte[] { 0xBB }, 30, channel: 3);
byte[] channelThreePacket = SoeDatagramCodec.Decode(channelThreeWire, 0xA1B2C3D4, 2, 496);
if (channelThreePacket[1] != 0x0C) throw new Exception("Channel-three data opcode is incorrect.");
byte[] channelThreeAckWire = SoeDatagramCodec.EncodeUncompressed(
    SoeControlCodec.Encode(new(SoeControlKind.Acknowledgement, 0, 3)), 0xA1B2C3D4, 2, 496);
if (clientTransport.AcceptDatagram(channelThreeAckWire).ReleasedOutboundPackets != 1)
    throw new Exception("Channel-three ACK did not release its channel packet.");
SoeClientTransport isolatedChannels = EstablishedTransport(responseVector);
isolatedChannels.CreateReliableDatagram(new byte[] { 0 }, 0, channel: 0);
isolatedChannels.CreateReliableDatagram(new byte[] { 1 }, 0, channel: 1);
byte[] channelOneAckWire = SoeDatagramCodec.EncodeUncompressed(
    SoeControlCodec.Encode(new(SoeControlKind.Acknowledgement, 0, 1)), 0xA1B2C3D4, 2, 496);
if (isolatedChannels.AcceptDatagram(channelOneAckWire).ReleasedOutboundPackets != 1 ||
    isolatedChannels.RetainedOutboundPackets != 1)
    throw new Exception("Acknowledgement crossed reliable channel boundaries.");
Console.WriteLine("PASS: four-channel socket-independent transport state and ACK integration");

SoeClientTransport lossClient = EstablishedTransport(responseVector);
SoeClientTransport lossServer = EstablishedTransport(responseVector);
byte[] initiallyDropped = lossClient.CreateReliableDatagram(loginApplication, 100);
if (lossClient.GetRetransmissionDatagrams(299, 200).Count != 0)
    throw new Exception("Reliable datagram retransmitted early.");
byte[] retried = lossClient.GetRetransmissionDatagrams(300, 200).Single();
AssertBytes(initiallyDropped, retried);
SoeInboundDatagramResult serverLogin = lossServer.AcceptDatagram(retried);
AssertBytes(loginApplication, serverLogin.ApplicationPayloads.Single());
if (serverLogin.ControlDatagram is null) throw new Exception("Retransmitted login was not acknowledged.");
lossClient.AcceptDatagram(serverLogin.ControlDatagram);
if (lossClient.RetainedOutboundPackets != 0) throw new Exception("Retransmitted login remained retained after ACK.");
SoeInboundDatagramResult lateDuplicate = lossServer.AcceptDatagram(initiallyDropped);
if (lateDuplicate.ApplicationPayloads.Count != 0 || lateDuplicate.ControlDatagram is null)
    throw new Exception("Late duplicate was redelivered or not acknowledged.");
Console.WriteLine("PASS: deterministic loss, retransmission, ACK, and late-duplicate scenario");

foreach ((uint value, string encoded) in new[]
{
    (0u, "00"), (1u, "01"), (253u, "FD"), (254u, "FF00FE"),
    (255u, "FF00FF"), (65534u, "FFFFFE"), (65535u, "FFFFFF0000FFFF"),
    (0x12345678u, "FFFFFF12345678")
})
{
    byte[] vector = Convert.FromHexString(encoded);
    AssertBytes(vector, SoeVariableLengthCodec.Encode(value));
    if (SoeVariableLengthCodec.Decode(vector, out int consumed) != value || consumed != vector.Length)
        throw new Exception("Variable-length vector failed.");
    for (int n = 0; n < vector.Length; n++) Reject(() => SoeVariableLengthCodec.Decode(vector.AsSpan(0, n), out _));
}
byte[][] multiMessages = { new byte[] { 1 }, Enumerable.Repeat((byte)2, 254).ToArray() };
byte[] multipayload = SoeMultiPayloadCodec.Encode(multiMessages, 300);
IReadOnlyList<byte[]> decodedMulti = SoeMultiPayloadCodec.Decode(multipayload, 2, 300);
AssertBytes(multiMessages[0], decodedMulti[0]);
AssertBytes(multiMessages[1], decodedMulti[1]);
Reject(() => SoeMultiPayloadCodec.Decode(multipayload, 1, 300));
Reject(() => SoeMultiPayloadCodec.Decode(multipayload.AsSpan(0, multipayload.Length - 1), 2, 300));
byte[] multiTransport = new byte[4 + multipayload.Length];
multiTransport[1] = 0x09;
multipayload.CopyTo(multiTransport, 4);
byte[] multiWire = SoeDatagramCodec.EncodeUncompressed(multiTransport, 0xA1B2C3D4, 2, 496);
SoeInboundDatagramResult integratedMulti = clientTransport.AcceptDatagram(multiWire);
if (integratedMulti.ApplicationPayloads.Count != 2) throw new Exception("Integrated multipayload did not expand.");
AssertBytes(multiMessages[0], integratedMulti.ApplicationPayloads[0]);
AssertBytes(multiMessages[1], integratedMulti.ApplicationPayloads[1]);
Console.WriteLine("PASS: variable-length boundaries and bounded multipayload codec");

byte[] fragmentedMessage = Enumerable.Range(0, 600).Select(index => (byte)index).ToArray();
byte[] firstFragment = new byte[8 + 485];
firstFragment[1] = 0x0D;
BinaryPrimitives.WriteUInt32BigEndian(firstFragment.AsSpan(4), (uint)fragmentedMessage.Length);
fragmentedMessage.AsSpan(0, 485).CopyTo(firstFragment.AsSpan(8));
byte[] finalFragment = new byte[4 + fragmentedMessage.Length - 485];
finalFragment[1] = 0x0D;
BinaryPrimitives.WriteUInt16BigEndian(finalFragment.AsSpan(2), 1);
fragmentedMessage.AsSpan(485).CopyTo(finalFragment.AsSpan(4));
byte[] firstFragmentWire = SoeDatagramCodec.EncodeUncompressed(firstFragment, 0xA1B2C3D4, 2, 496);
byte[] finalFragmentWire = SoeDatagramCodec.EncodeUncompressed(finalFragment, 0xA1B2C3D4, 2, 496);
byte[] firstFragmentGoldenWire = SoeDatagramCodec.EncodeUncompressed(firstFragment, crcSeed, 2, 496);
byte[] finalFragmentGoldenWire = SoeDatagramCodec.EncodeUncompressed(finalFragment, crcSeed, 2, 496);
if (Convert.ToHexString(SHA256.HashData(firstFragmentGoldenWire)).ToLowerInvariant() !=
        "4ae6870e65d8c69dc50305226e489dc1364336ac647718a40909ff485198a6ba" ||
    Convert.ToHexString(SHA256.HashData(finalFragmentGoldenWire)).ToLowerInvariant() !=
        "0f3a79ad759b761a16aa6ed5d10c69024b062c823cadc41fe1d1cadcac04fa26")
    throw new Exception("Fragment golden hash mismatch.");

SoeClientTransport fragmentClient = EstablishedTransport(responseVector);
SoeInboundDatagramResult futureFragmentResult = fragmentClient.AcceptDatagram(finalFragmentWire);
if (futureFragmentResult.ApplicationPayloads.Count != 0)
    throw new Exception("Future fragment was delivered early.");
SoeControlPacket futureFragmentControl = SoeControlCodec.Decode(SoeDatagramCodec.Decode(
    futureFragmentResult.ControlDatagram ?? throw new Exception("Future fragment produced no control."),
    0xA1B2C3D4, 2, 496));
if (futureFragmentControl != new SoeControlPacket(SoeControlKind.OutOfOrder, 1))
    throw new Exception("Future fragment produced the wrong selective control packet.");
SoeInboundDatagramResult reassembled = fragmentClient.AcceptDatagram(firstFragmentWire);
if (reassembled.ApplicationPayloads.Count != 1) throw new Exception("Fragmented message did not complete.");
AssertBytes(fragmentedMessage, reassembled.ApplicationPayloads[0]);
SoeControlPacket fragmentAck = SoeControlCodec.Decode(SoeDatagramCodec.Decode(
    reassembled.ControlDatagram ?? throw new Exception("Reassembled message produced no ACK."),
    0xA1B2C3D4, 2, 496));
if (fragmentAck != new SoeControlPacket(SoeControlKind.Acknowledgement, 1))
    throw new Exception("Reassembled message did not cumulatively acknowledge both fragments.");
SoeClientTransport outboundFragmentClient = EstablishedTransport(responseVector);
IReadOnlyList<byte[]> boundaryDirect = outboundFragmentClient.CreateReliableDatagrams(new byte[489], 40);
if (boundaryDirect.Count != 1 || SoeDatagramCodec.Decode(boundaryDirect[0], 0xA1B2C3D4, 2, 496)[1] != 0x09)
    throw new Exception("489-byte reliable payload did not remain unfragmented.");
outboundFragmentClient.AcceptDatagram(SoeDatagramCodec.EncodeUncompressed(
    SoeControlCodec.Encode(new(SoeControlKind.Acknowledgement, 0)), 0xA1B2C3D4, 2, 496));
IReadOnlyList<byte[]> boundaryFragmented = outboundFragmentClient.CreateReliableDatagrams(new byte[490], 45);
if (boundaryFragmented.Count != 2 || SoeDatagramCodec.Decode(boundaryFragmented[0], 0xA1B2C3D4, 2, 496)[1] != 0x0D)
    throw new Exception("490-byte reliable payload did not fragment.");
outboundFragmentClient.AcceptDatagram(SoeDatagramCodec.EncodeUncompressed(
    SoeControlCodec.Encode(new(SoeControlKind.Acknowledgement, 2)), 0xA1B2C3D4, 2, 496));
outboundFragmentClient = EstablishedTransport(responseVector);
IReadOnlyList<byte[]> outboundFragments = outboundFragmentClient.CreateReliableDatagrams(fragmentedMessage, 50);
if (outboundFragments.Count != 2 || outboundFragmentClient.RetainedOutboundPackets != 2)
    throw new Exception("Outbound fragmentation did not retain exactly two packets.");
byte[] decodedFirstFragment = SoeDatagramCodec.Decode(outboundFragments[0], 0xA1B2C3D4, 2, 496);
byte[] decodedFinalFragment = SoeDatagramCodec.Decode(outboundFragments[1], 0xA1B2C3D4, 2, 496);
if (decodedFirstFragment.Length != 493 || decodedFinalFragment.Length != 119 ||
    decodedFirstFragment[1] != 0x0D || decodedFinalFragment[1] != 0x0D)
    throw new Exception("Outbound fragment sizing is incorrect.");
SoeClientTransport outboundFragmentReceiver = EstablishedTransport(responseVector);
outboundFragmentReceiver.AcceptDatagram(outboundFragments[0]);
SoeInboundDatagramResult outboundRoundtrip = outboundFragmentReceiver.AcceptDatagram(outboundFragments[1]);
AssertBytes(fragmentedMessage, outboundRoundtrip.ApplicationPayloads.Single());
byte[] seed123Response = (byte[])responseVector.Clone();
Convert.FromHexString("12345678").CopyTo(seed123Response, 6);
SoeClientTransport threeFragmentSender = EstablishedTransport(seed123Response);
byte[] threeFragmentMessage = Enumerable.Range(0, 1100).Select(index => (byte)index).ToArray();
IReadOnlyList<byte[]> threeFragmentWire = threeFragmentSender.CreateReliableDatagrams(threeFragmentMessage, 60);
string[] threeFragmentHashes =
{
    "1970b660cc4019fd8cc99cae115932c20f81fad03c05b0a0e3fa6a1131e0039f",
    "3b5dd8e17138ba36a1f414ad047df6111570cc3e2dd0d36119886c3382488ea4",
    "be71a3c08dae3227c01bd8f8d36382b480ee8360de68b983c1c5199385f01b63"
};
if (threeFragmentWire.Count != threeFragmentHashes.Length) throw new Exception("Three-fragment split count changed.");
for (int index = 0; index < threeFragmentHashes.Length; index++)
{
    string hash = Convert.ToHexString(SHA256.HashData(threeFragmentWire[index])).ToLowerInvariant();
    if (hash != threeFragmentHashes[index]) throw new Exception("Three-fragment golden hash mismatch.");
}
SoeFragmentReassembler fragmentBounds = new(maximumMessageSize: 8);
Reject(() => fragmentBounds.Accept(Convert.FromHexString("00000009")));
fragmentBounds.Accept(Convert.FromHexString("0000000201"));
Reject(() => fragmentBounds.Accept(Convert.FromHexString("0203")));
Console.WriteLine("PASS: bounded channel-zero fragment reassembly, including reordered arrival");

byte[] clusterListVector = Convert.FromHexString(
    "0200B9631CC1010000000700000008005374617264757374808FFFFF08000000");
LoginClusterList clusterList = LoginServerListCodec.DecodeClusterList(clusterListVector);
if (clusterList.Footer != 8 || clusterList.Clusters.Single() != new LoginCluster(7, "Stardust", 0xFFFF8F80))
    throw new Exception("LoginEnumCluster fixture decoded incorrectly.");
byte[] clusterStatusVector = Convert.FromHexString(
    "0300B6AE3634010000000700000009003132372E302E302E31A5ADAEAD64000000B20C000008000000808FFFFF0200000000");
LoginClusterStatus clusterStatus = LoginServerListCodec.DecodeClusterStatus(clusterStatusVector).Single();
if (clusterStatus != new LoginClusterStatus(7, "127.0.0.1", 44453, 44462, 100, 3250, 8, 0xFFFF8F80, 2, false))
    throw new Exception("LoginClusterStatus fixture decoded incorrectly.");
for (int n = 0; n < clusterListVector.Length; n++) Reject(() => LoginServerListCodec.DecodeClusterList(clusterListVector.AsSpan(0, n)));
for (int n = 0; n < clusterStatusVector.Length; n++) Reject(() => LoginServerListCodec.DecodeClusterStatus(clusterStatusVector.AsSpan(0, n)));
Reject(() => LoginServerListCodec.DecodeClusterList(clusterListVector, maximumRecords: 0));
byte[] charactersVector = Convert.FromHexString(
    "02007445EA65010000000100000041004433221188776655443322110700000001000000");
LoginCharacter character = LoginServerListCodec.DecodeCharacters(charactersVector).Single();
if (character != new LoginCharacter("A", 0x11223344, 0x1122334455667788, 7, 1))
    throw new Exception("EnumerateCharacterId fixture decoded incorrectly.");
for (int n = 0; n < charactersVector.Length; n++) Reject(() => LoginServerListCodec.DecodeCharacters(charactersVector.AsSpan(0, n)));
byte[] invalidUtf16 = (byte[])charactersVector.Clone();
invalidUtf16[14] = 0x00;
invalidUtf16[15] = 0xD8;
Reject(() => LoginServerListCodec.DecodeCharacters(invalidUtf16));
if (LoginApplicationDecoder.Decode(clusterListVector).Value is not LoginClusterList ||
    LoginApplicationDecoder.Decode(clusterStatusVector).Value is not IReadOnlyList<LoginClusterStatus> ||
    LoginApplicationDecoder.Decode(charactersVector).Value is not IReadOnlyList<LoginCharacter>)
    throw new Exception("Login application opcode dispatch failed.");
Reject(() => LoginApplicationDecoder.Decode(Convert.FromHexString("000000000000")));
Console.WriteLine("PASS: bounded login cluster, status, and character fixtures");

SoeClientTransport scheduledTransport = new();
SoeHandshakeScheduler handshake = new(scheduledTransport, retryIntervalMilliseconds: 100, timeoutMilliseconds: 300);
AssertBytes(requestVector, handshake.Start(0x12345678, 496, 1_000));
if (handshake.Poll(1_099) is not null) throw new Exception("Session request retried early.");
AssertBytes(requestVector, handshake.Poll(1_100) ?? throw new Exception("Session request was not retried."));
handshake.AcceptResponse(responseVector);
if (handshake.State != SoeHandshakeState.Established || scheduledTransport.State != SoeClientTransportState.Established)
    throw new Exception("Scheduled handshake did not establish transport.");
SoeHandshakeScheduler timeoutHandshake = new(new SoeClientTransport(), 100, 300);
timeoutHandshake.Start(1, 496, 5_000);
try { timeoutHandshake.Poll(5_300); throw new Exception("Session timeout did not fire."); }
catch (TimeoutException) { }
if (timeoutHandshake.State != SoeHandshakeState.TimedOut) throw new Exception("Session timeout state was not retained.");
Console.WriteLine("PASS: monotonic session retry and timeout scheduler");

SoeClientTransport closingTransport = EstablishedTransport(responseVector);
closingTransport.CreateReliableDatagram(loginApplication, 0);
closingTransport.Close();
if (closingTransport.State != SoeClientTransportState.Closed || closingTransport.RetainedOutboundPackets != 0)
    throw new Exception("Transport close did not clear retained packets.");
try { closingTransport.CreateReliableDatagram(Array.Empty<byte>(), 0); throw new Exception("Closed transport accepted a send."); }
catch (InvalidOperationException) { }
Console.WriteLine("PASS: deterministic transport teardown clears retained credential-bearing packets");

using (LoginClientSession loginSession = new())
{
    byte[] ownedRequest = loginSession.BeginTransport(0x12345678, 496, 100, 50, 500);
    if (loginSession.PollHandshake(149) is not null) throw new Exception("Owned login retried handshake early.");
    AssertBytes(ownedRequest, loginSession.PollHandshake(150) ?? throw new Exception("Owned login did not retry handshake."));
    loginSession.AcceptSessionResponse(responseVector);
    loginSession.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
    byte[][] loginResponses = { tokenVector, clusterListVector, clusterStatusVector, charactersVector };
    for (ushort sequence = 0; sequence < loginResponses.Length; sequence++)
        loginSession.AcceptDatagram(CreateServerApplicationDatagram(loginResponses[sequence], sequence));
    if (loginSession.State != LoginClientState.Complete || loginSession.CredentialLength != 5 ||
        loginSession.AccountId != 0x11223344 || loginSession.StationId != 0x55667788 ||
        loginSession.ClusterList?.Clusters.Count != 1 || loginSession.ClusterStatuses.Count != 1 ||
        loginSession.ClustersById[7].Name != "Stardust" || loginSession.ClusterStatusById[7].ZonePort != 44453 ||
        loginSession.Characters.Single().ObjectId != 0x1122334455667788)
        throw new Exception("Owned login session did not reach the expected complete state.");
    AssertBytes(Convert.FromHexString("5444332211"), loginSession.CopyCredentialBlob());
}
LoginClientSession failedLogin = new();
failedLogin.BeginTransport(0x12345678, 496);
failedLogin.AcceptSessionResponse(responseVector);
failedLogin.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
failedLogin.AcceptDatagram(CreateServerApplicationDatagram(errorVector, 0));
if (failedLogin.State != LoginClientState.Failed || failedLogin.LastError?.Text != "bad")
    throw new Exception("Fatal login error did not transition the owned session.");
failedLogin.Close();
LoginClientSession malformedLogin = new();
malformedLogin.BeginTransport(0x12345678, 496);
malformedLogin.AcceptSessionResponse(responseVector);
malformedLogin.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
Reject(() => malformedLogin.AcceptDatagram(CreateServerApplicationDatagram(Convert.FromHexString("000000000000"), 0)));
if (malformedLogin.State != LoginClientState.Failed)
    throw new Exception("Malformed application response did not fail the owned login session.");
Console.WriteLine("PASS: owned login state, opaque credential lifetime, completion, and fatal error path");

LoginClientOptions options = new("private.example.invalid", 44453, "20260817-SD:01");
string safeOptions = options.ToString();
if (safeOptions.Contains(options.Host, StringComparison.Ordinal) || safeOptions.Contains(options.Port.ToString(), StringComparison.Ordinal))
    throw new Exception("Safe configuration summary exposed its endpoint.");
try { _ = new LoginClientOptions("", 44453, "v"); throw new Exception("Empty host accepted."); }
catch (ArgumentException) { }
try { _ = new LoginClientOptions("host", 0, "v"); throw new Exception("Invalid port accepted."); }
catch (ArgumentOutOfRangeException) { }
try { _ = new LoginClientOptions("host", 1, "v\u2603"); throw new Exception("Non-ASCII version accepted."); }
catch (ArgumentException) { }
Console.WriteLine("PASS: validated connection options and endpoint-redacted configuration summary");

LoginClientOptions coordinatorOptions = new(
    "private.example.invalid", 44453, "20260817-SD:01",
    sessionRetryInterval: TimeSpan.FromMilliseconds(100),
    sessionTimeout: TimeSpan.FromMilliseconds(500),
    reliableRetransmitInterval: TimeSpan.FromMilliseconds(200));
using (LoginClientCoordinator coordinator = new(coordinatorOptions))
{
    LoginOutboundDatagram start = coordinator.Start(0x12345678, 1_000);
    if (start.Kind != LoginOutboundKind.SessionRequest) throw new Exception("Coordinator start kind is incorrect.");
    if (coordinator.Poll(1_099).Count != 0) throw new Exception("Coordinator retried the session early.");
    if (coordinator.Poll(1_100).Single().Kind != LoginOutboundKind.SessionRequest)
        throw new Exception("Coordinator did not emit the scheduled session retry.");
    coordinator.AcceptIncoming(responseVector);
    LoginOutboundDatagram loginRequest = coordinator.Authenticate("u", "p", 1_150);
    if (loginRequest.Kind != LoginOutboundKind.LoginRequest) throw new Exception("Coordinator login kind is incorrect.");
    if (coordinator.Poll(1_349).Count != 0 || coordinator.Poll(1_350).Single().Kind != LoginOutboundKind.Retransmission)
        throw new Exception("Coordinator reliable retry schedule is incorrect.");
    IReadOnlyList<LoginOutboundDatagram> controls = coordinator.AcceptIncoming(CreateServerApplicationDatagram(tokenVector, 0));
    if (controls.Single().Kind != LoginOutboundKind.ReliabilityControl)
        throw new Exception("Coordinator did not surface the login response ACK.");
}
Console.WriteLine("PASS: socket-independent login coordinator tick, receive, ACK, and retry flow");

IndependentPeerScenarios.Run(responseVector, tokenVector, clusterListVector, clusterStatusVector, charactersVector, errorVector);
ConfigurationAndPrivacyScenarios.Run(responseVector, tokenVector, errorVector);
TreAssetScenarios.Run();
MeshScenarios.Run();
DdsScenarios.Run();
DdsHdrScenarios.Run();
MaterialScenarios.Run();
DetailAppearanceScenarios.Run();
AppearanceResolutionScenarios.Run();
ComponentAppearanceScenarios.Run();
AppearanceExtentScenarios.Run();
AppearanceHardpointScenarios.Run();
PortalLayoutScenarios.Run();
PortalSceneScenarios.Run();
FloorMeshScenarios.Run();
FloorQueryScenarios.Run();
FloorWalkScenarios.Run();
PortalTransferScenarios.Run();
PortalBarrierScenarios.Run();
DoorLeafScenarios.Run();
InteriorDoorScenarios.Run();
PathGraphScenarios.Run();
PathRouteScenarios.Run();
PortalAssociationScenarios.Run();
CustomizableShaderScenarios.Run();
PaletteScenarios.Run();
ShaderOverlayScenarios.Run();
StatusScenarios.Run(responseVector);
HostAccountingScenarios.Run();
await LoginHostScenarios.RunAsync(responseVector, tokenVector, clusterListVector, clusterStatusVector, charactersVector, errorVector);
await JournalScenarios.RunAsync();
foreach (string entry in await OfflineHostedLoginScenario.RunAsync()) Console.WriteLine("HOST: " + entry);
if (args.Length > 0 && args[0] is "--dds-tre" or "--dds-file") DdsScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] is "--detail-overlay" or "--detail-bundle") DetailAppearanceScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] is "--component-overlay" or "--geometry-graph" or "--textured-graph") ComponentAppearanceScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--extent-list") AppearanceExtentScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] is "--cshd-list" or "--cshd-palette-list") CustomizableShaderScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--palette-list") PaletteScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--hardpoint-list") AppearanceHardpointScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--hardpoint-scene") AppearanceHardpointScenarios.ProbeSceneOptional(args);
else if (args.Length > 0 && args[0]=="--portal-associations") PortalAssociationScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--path-routes") PathRouteScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--path-graph-list") PathGraphScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--floor-queries") FloorQueryScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--floor-walks") FloorWalkScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--portal-transfers") PortalTransferScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--portal-barriers") PortalBarrierScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--door-style") DoorLeafScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--interior-door") InteriorDoorScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--floor-list") FloorMeshScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] is "--portal-scene" or "--portal-floors" or "--portal-paths" or "--portal-textures" or "--portal-textures-defaults" or "--portal-surface-paths") PortalSceneScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0]=="--portal-list") PortalLayoutScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] == "--mesh-overlay") ShaderOverlayScenarios.ProbeOptional(args);
else if (args.Length > 0 && args[0] == "--mesh-tre") MeshScenarios.ProbeOptional(args);
else TreAssetScenarios.ProbeOptional(args);

static void AssertBytes(byte[] expected, byte[] actual)
{
    if (!expected.AsSpan().SequenceEqual(actual)) throw new Exception("SOE request vector mismatch.");
}

static void Reject(Action action)
{
    try { action(); throw new Exception("Malformed SOE packet was accepted."); }
    catch (InvalidDataException) { }
}

static byte[] Changed(byte[] source, int index, byte value)
{
    byte[] copy = (byte[])source.Clone();
    copy[index] = value;
    return copy;
}

static byte[] Append(byte[] source)
{
    byte[] copy = new byte[source.Length + 1];
    source.CopyTo(copy, 0);
    return copy;
}

static byte[] CreateCompressedDatagram(byte[] transportPacket, uint seed, int crcWidth)
{
    using MemoryStream compressedBody = new();
    using (ZLibStream zlib = new(compressedBody, CompressionLevel.SmallestSize, leaveOpen: true))
        zlib.Write(transportPacket.AsSpan(2));
    byte[] body = compressedBody.ToArray();
    byte[] framed = new byte[2 + body.Length + 1];
    transportPacket.AsSpan(0, 2).CopyTo(framed);
    body.CopyTo(framed, 2);
    framed[^1] = 1;
    SoeSelector4.Encrypt(framed, seed);
    byte[] datagram = new byte[framed.Length + crcWidth];
    framed.CopyTo(datagram, 0);
    SoeSeededCrc.WriteTrailer(framed, seed, datagram.AsSpan(framed.Length));
    return datagram;
}

static SoeClientTransport EstablishedTransport(byte[] response)
{
    SoeClientTransport transport = new();
    transport.BeginSession(0x12345678, 496);
    transport.AcceptSessionResponse(response);
    return transport;
}

static byte[] CreateServerApplicationDatagram(byte[] application, ushort sequence)
{
    byte[] packet = new byte[4 + application.Length];
    packet[1] = 0x09;
    BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), sequence);
    application.CopyTo(packet, 4);
    return SoeDatagramCodec.EncodeUncompressed(packet, 0xA1B2C3D4, 2, 496);
}
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: {error.GetType().Name}: {error.Message}");
    Environment.ExitCode = 1;
}
