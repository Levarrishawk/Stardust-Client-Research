using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class ConfigurationAndPrivacyScenarios
{
    public static void Run(byte[] response, byte[] token, byte[] error)
    {
        const string valid = """
            {"host":"peer.invalid","port":44453,"clientVersion":"20260817-SD:01",
             "maximumDatagramSize":496,"sessionRetryMilliseconds":100,
             "sessionTimeoutMilliseconds":500,"reliableRetransmitMilliseconds":200,
             "loginResponseTimeoutMilliseconds":30000}
            """;
        byte[] json = Encoding.UTF8.GetBytes(valid);
        LoginClientOptions options = LoginClientConfiguration.Parse(json);
        Check(options.Host == "peer.invalid" && options.Port == 44453 && options.SessionTimeout.TotalMilliseconds == 500,
            "configuration values");
        using (MemoryStream source = new(json))
            Check(LoginClientConfiguration.Load(source).ClientVersion == options.ClientVersion, "stream configuration");
        foreach (string invalid in new[] { "{}", "[]", "{", valid.Replace("44453", "0"),
            valid.Replace("44453", "\"44453\""), valid.Replace("496", "-1"),
            valid.Replace("\"host\":", "\"host\":\"duplicate\",\"host\":"),
            valid.Replace("\"maximumDatagramSize\":496", "\"password\":\"must-never-appear\""),
            valid.Replace("peer.invalid", "host with spaces"), valid.Replace("20260817-SD:01", "v\\n") })
        {
            try { LoginClientConfiguration.Parse(Encoding.UTF8.GetBytes(invalid)); throw new Exception("Invalid settings accepted."); }
            catch (InvalidDataException failure)
            {
                Check(!failure.Message.Contains("must-never-appear") && failure.InnerException is null,
                    "configuration errors omit source text");
            }
        }
        using (MemoryStream large = new(new byte[LoginClientConfiguration.MaximumConfigurationBytes + 1]))
        {
            try { LoginClientConfiguration.Load(large); throw new Exception("Oversized settings accepted."); }
            catch (InvalidDataException) { }
        }
        List<string> events = new();
        using (LoginClientCoordinator client = new(options, value => events.Add(LoginDiagnostics.Encode(value))))
        {
            client.Start(0x12345678, 0);
            client.AcceptIncoming(response);
            client.Authenticate("private-user", "private-password", 0);
            client.AcceptIncoming(Application(token, 0));
            client.AcceptIncoming(Application(error, 1));
            Check(client.Session.State == LoginClientState.Failed, "diagnostic fatal state");
        }
        string log = string.Join('\n', events);
        foreach (string forbidden in new[] { "private-user", "private-password", "peer.invalid", "44453", "bad", "5444332211" })
            Check(!log.Contains(forbidden, StringComparison.Ordinal), "diagnostic redaction");
        using (JsonDocument entry = JsonDocument.Parse(events[^2]))
            Check(entry.RootElement.GetProperty("outcome").GetString() == "RemoteFailure", "typed failure diagnostic");
        using (LoginClientCoordinator badSink = new(options, _ => throw new IOException("private sink failure")))
        {
            badSink.Start(0x12345678, 0);
            badSink.AcceptIncoming(response);
            badSink.Authenticate("u", "p", 0);
            Check(badSink.DiagnosticFailures == 3 && badSink.Session.State == LoginClientState.AwaitingResponses,
                "sink failure cannot interrupt protocol");
        }
        VerifyOwnedBufferCleanup(response, token, error);
        Console.WriteLine("PASS: strict bounded configuration, redacted structured diagnostics, sink failure isolation, and owned-buffer zeroing");
    }

    private static void VerifyOwnedBufferCleanup(byte[] response, byte[] token, byte[] error)
    {
        SoeReliableChannel cumulative = new();
        byte[] callerPacket = cumulative.CreateDataPacket(new byte[] { 1, 2, 3 }, 0);
        byte[] retained = RetainedPacket(cumulative);
        cumulative.ApplyCumulativeAcknowledgement(0);
        Check(retained.All(value => value == 0) && callerPacket.Any(value => value != 0), "cumulative ACK ownership zeroing");
        SoeReliableChannel selective = new();
        selective.CreateDataPacket(new byte[] { 1, 2, 3 }, 0);
        retained = RetainedPacket(selective);
        selective.ApplySelectiveOutOfOrderAcknowledgement(0);
        Check(retained.All(value => value == 0), "selective ACK zeroing");

        SoeClientTransport queued = new();
        queued.BeginSession(0x12345678, 496);
        queued.AcceptSessionResponse(response);
        queued.QueueReliableMessage(Enumerable.Repeat((byte)0xAA, 100_000).ToArray());
        object pending = ((Array)Field(queued, "pending")).GetValue(0)!;
        byte[] ownedQueue = (byte[])pending.GetType().GetProperty("Bytes")!.GetValue(pending)!;
        queued.PumpQueuedDatagrams(0);
        queued.Close();
        Check(ownedQueue.All(value => value == 0), "queued fragment cancellation zeroing");

        using LoginClientSession session = new();
        session.BeginTransport(0x12345678, 496);
        session.AcceptSessionResponse(response);
        session.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
        session.AcceptDatagram(Application(token, 0));
        byte[] ownedCredential = (byte[])Field(session, "credentialBlob");
        session.AcceptDatagram(Application(error, 1));
        Check(ownedCredential.All(value => value == 0) && session.CredentialLength == 0,
            "fatal error zeros the owned token allocation");
        Check(session.AccountId == 0 && session.StationId == 0 && session.Username.Length == 0,
            "failure clears stale identity state");
        using LoginClientSession malformed = new();
        malformed.BeginTransport(0x12345678, 496);
        malformed.AcceptSessionResponse(response);
        malformed.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
        malformed.AcceptDatagram(Application(token, 0));
        ownedCredential = (byte[])Field(malformed, "credentialBlob");
        try { malformed.AcceptDatagram(Application(new byte[6], 1)); throw new Exception("Malformed application accepted."); }
        catch (InvalidDataException) { }
        Check(ownedCredential.All(value => value == 0) && malformed.State == LoginClientState.Failed,
            "malformed application zeros previously accepted credential");
    }

    private static byte[] RetainedPacket(SoeReliableChannel channel)
    {
        object entry = ((IEnumerable)Field(channel, "outbound")).Cast<object>().Single();
        object retained = entry.GetType().GetProperty("Value")!.GetValue(entry)!;
        return (byte[])retained.GetType().GetProperty("Packet")!.GetValue(retained)!;
    }

    // Test-only inspection of our own allocations; no diagnostic exposes these bytes.
    private static object Field(object value, string name)
        => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;

    private static byte[] Application(byte[] body, ushort sequence)
    {
        byte[] packet = new byte[4 + body.Length];
        packet[1] = 9;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), sequence);
        body.CopyTo(packet, 4);
        return SoeDatagramCodec.EncodeUncompressed(packet, 0xA1B2C3D4, 2, 496);
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception($"Configuration/privacy scenario failed: {label}.");
    }
}
