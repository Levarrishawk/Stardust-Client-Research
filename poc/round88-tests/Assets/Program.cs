using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;

if (args.Length != 2) throw new ArgumentException("Expected fixtures.json receipt.json.");
using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(args[0]));
JsonElement f = document.RootElement;
string root = f.GetProperty("Root").GetString()!;
ReplacementLaunchProfile Decode(JsonElement value) => ReplacementLaunchProfileCodec.Decode(Encoding.UTF8.GetBytes(value.GetRawText()));
ReplacementLaunchProfile profile = Decode(f.GetProperty("Profile"));
int negatives = 0, jsonRejected = 0, cleanup = 0;
void Check(bool value) { if (!value) throw new Exception("Launch assertion failed."); }
void Reject(Action action)
{
    try { action(); }
    catch (Exception error) when (error is InvalidDataException or IOException or ArgumentException or ObjectDisposedException or AppearanceGraphBudgetException)
    { negatives++; return; }
    throw new Exception("Expected launch rejection.");
}
void Closed(string directory)
{
    using FileStream exclusive = new(Path.Combine(directory, "data.tre"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    cleanup++;
}
object Describe(InstalledCreaturePreparation creature) => new { Catalog = creature.Catalog.Entries.Values.Single(), creature.Sources,
    Idle = creature.Options.Idle, Walk = creature.Options.Movement["walk"], Run = creature.Options.Movement["run"] };
foreach (JsonElement invalid in f.GetProperty("InvalidJson").EnumerateArray())
{
    Reject(() => ReplacementLaunchProfileCodec.Decode(Encoding.UTF8.GetBytes(invalid.GetString()!))); jsonRejected++;
}
Reject(() => ReplacementLaunchProfileCodec.Decode(new byte[] { 0xff }));
byte[] valid = Encoding.UTF8.GetBytes(f.GetProperty("Profile").GetRawText());
byte[] boundary = Enumerable.Repeat((byte)' ', 65536).ToArray(); valid.CopyTo(boundary, 0);
Check(ReplacementLaunchProfileCodec.Decode(boundary).Version == 1);
Reject(() => ReplacementLaunchProfileCodec.Decode(new byte[65537]));
Reject(() => ReplacementLaunchProfileCodec.Decode(Array.Empty<byte>()));
List<int> caller = [0, 1];
ReplacementLaunchProfile manual = profile with { Walk = profile.Walk with { ExplicitChildRoute = caller } };
ReplacementLaunchPreparation owner = ReplacementLaunchPreparation.Open(manual);
caller[1] = 2;
Check(owner.Profile.Walk.ExplicitChildRoute!.SequenceEqual(new[] { 0, 1 }));
Check(owner.Creature.Options.Movement["walk"].ExplicitChildRoute!.SequenceEqual(new[] { 0, 1 }));
Check(!ReferenceEquals(owner.Profile, manual));
try { ((IList<int>)owner.Profile.Walk.ExplicitChildRoute!)[0] = 1; throw new Exception("Mutable profile route."); }
catch (NotSupportedException) { }
OriginalInstallationInputs borrowed = owner.Inputs;
InstalledCreaturePreparation historical = owner.Creature;
object synthetic = Describe(historical);
owner.Dispose(); owner.Dispose();
Reject(() => _ = owner.Inputs); Reject(() => _ = borrowed.BorrowedOverlay);
Check(owner.Creature == historical && owner.Profile.Version == 1);
Closed(Path.Combine(root, "good"));
using (ReplacementLaunchPreparation alias = ReplacementLaunchPreparation.Open(profile with { ObjectPath = "OBJECT\\ROOT.IFF" }))
    Check(alias.Profile.ObjectPath == "object/root.iff");
foreach (ReplacementLaunchProfile bad in new[] { profile with { Version = 2 }, profile with { AssetRoot = "C:relative" },
    profile with { ConfigurationPath = "https://host/config" }, profile with { ObjectPath = "../object/root.iff" },
    profile with { Idle = profile.Idle with { LogicalName = "default" } },
    profile with { Walk = profile.Walk with { ExplicitChildRoute = new[] { -1 } } },
    profile with { Run = profile.Run with { ExplicitChildRoute = Enumerable.Repeat(0, 33).ToArray() } },
    profile with { ConfigurationPath = Path.Combine(root, "absent.cfg") } })
    Reject(() => ReplacementLaunchPreparation.Open(bad));
Reject(() => ReplacementLaunchPreparation.Open(null!));
Reject(() => ReplacementLaunchPreparation.Open(profile with { Idle = null! }));
foreach (string name in new[] { "rootmissing", "rootmasked", "appearancemissing", "tablemasked", "missing", "masked", "parentmissing", "parentmasked", "cycle", "family", "appearance", "duplicate", "undeclared", "badtable", "limit" })
{
    string directory = Path.Combine(root, name);
    Reject(() => ReplacementLaunchPreparation.Open(profile with { AssetRoot = directory, ConfigurationPath = Path.Combine(directory, "mounts.cfg") }));
    Closed(directory);
}
ReplacementLaunchProfile nine = profile with { Walk = profile.Walk with { ExplicitChildRoute = Enumerable.Repeat(0, 9).ToArray() } };
// Codec admission is independently bounded at 32; installed preparation keeps its existing limit of eight.
Check(ReplacementLaunchProfileCodec.Decode(JsonSerializer.SerializeToUtf8Bytes(nine)).Walk.ExplicitChildRoute!.Count == 9);
Reject(() => ReplacementLaunchPreparation.Open(nine)); Closed(Path.Combine(root, "good"));
ReplacementLaunchProfile thirtyTwo = profile with { Walk = profile.Walk with { ExplicitChildRoute = Enumerable.Repeat(4095, 32).ToArray() } };
Check(ReplacementLaunchProfileCodec.Decode(JsonSerializer.SerializeToUtf8Bytes(thirtyTwo)).Walk.ExplicitChildRoute!.Count == 32);
object? original = null;
if (f.TryGetProperty("OriginalProfile", out JsonElement real))
{
    using ReplacementLaunchPreparation actual = ReplacementLaunchPreparation.Open(Decode(real));
    actual.Inputs.RequireAllPresent(); original = Describe(actual.Creature);
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new { Synthetic = synthetic, Original = original,
    JsonRejected = jsonRejected, NegativeCases = negatives, CleanupChecks = cleanup }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS launch JSON={jsonRejected} negatives={negatives} cleanup={cleanup} original={original is not null}");
