using System.Text.Json;
using Stardust.Core.Protocol;

int checks = 0, negatives = 0;
void Check(bool condition) { if (!condition) throw new InvalidOperationException("Synthetic codec assertion failed."); checks++; }
void Reject<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { negatives++; return; }
    throw new InvalidOperationException("Expected admission rejection.");
}
using var fixture = JsonDocument.Parse(File.ReadAllBytes(args[0]));
var source = fixture.RootElement;
Check(ZoneAuthenticationCodec.ClientIdOpcode == source.GetProperty("ClientIdOpcode").GetUInt32());
Check(ZoneAuthenticationCodec.PermissionsOpcode == source.GetProperty("PermissionsOpcode").GetUInt32());
foreach (var row in source.GetProperty("ClientIdCases").EnumerateArray())
{
    byte[] credential = Convert.FromHexString(row.GetProperty("CredentialHex").GetString()!);
    byte[] prior = credential.ToArray();
    string version = row.GetProperty("Version").GetString()!;
    byte[] output = ZoneAuthenticationCodec.EncodeClientId(credential, version);
    Check(Convert.ToHexString(output) == row.GetProperty("ExpectedHex").GetString());
    Check(credential.SequenceEqual(prior));
    credential[0] ^= 0xFF; Check(output[14] == prior[0]);
    output[14] ^= 0xFF; Check(credential[0] == (byte)(prior[0] ^ 0xFF));
    var second = ZoneAuthenticationCodec.EncodeClientId(prior, version);
    Check(!ReferenceEquals(output, second) && Convert.ToHexString(second) == row.GetProperty("ExpectedHex").GetString());
    Array.Clear(output); Array.Clear(second); Array.Clear(credential); Array.Clear(prior);
}
foreach (var row in source.GetProperty("PermissionCases").EnumerateArray())
{
    byte[] raw = Convert.FromHexString(row.GetProperty("Hex").GetString()!);
    var permissions = ZoneAuthenticationCodec.DecodePermissions(raw);
    var flags = row.GetProperty("Flags").EnumerateArray().Select(v => v.GetBoolean()).ToArray();
    Check(permissions == new ZonePermissions(flags[0], flags[1], flags[2], flags.Length == 4 ? flags[3] : null));
    Array.Fill(raw, (byte)0xFF);
    Check(permissions == new ZonePermissions(flags[0], flags[1], flags[2], flags.Length == 4 ? flags[3] : null));
}
foreach (var row in source.GetProperty("MalformedPermissions").EnumerateArray())
{
    byte[] raw = Convert.FromHexString(row.GetString()!);
    Reject<InvalidDataException>(() => ZoneAuthenticationCodec.DecodePermissions(raw));
}
foreach (int length in new[] { 0, 1, 2, 3, 65537 })
    Reject<ArgumentOutOfRangeException>(() => ZoneAuthenticationCodec.EncodeClientId(new byte[length], "A"));
Reject<ArgumentNullException>(() => ZoneAuthenticationCodec.EncodeClientId(new byte[4], null!));
foreach (string version in new[] { "", new string('a', 257) })
    Reject<ArgumentOutOfRangeException>(() => ZoneAuthenticationCodec.EncodeClientId(new byte[4], version));
foreach (string version in new[] { "é", "A\u0080", "\uD800", "\0", "\u001F", "\u007F" })
    Reject<ArgumentException>(() => ZoneAuthenticationCodec.EncodeClientId(new byte[4], version));
File.WriteAllText(args[1], JsonSerializer.Serialize(new { Passed = true, Checks = checks, Rejections = negatives, Synthetic = true, SocketCreated = false }));
Console.WriteLine($"PASS checks={checks} rejections={negatives}");
