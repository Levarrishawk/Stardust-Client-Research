using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;

if (args.Length != 2) throw new ArgumentException("Expected fixtures.json receipt.json.");
using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(args[0]));
JsonElement fixture = document.RootElement;
string root = fixture.GetProperty("Root").GetString()!;
const string table = "appearance/lat/all_m.lat", idleName = "emt_standing_idle_ambient_1_ag";
InstalledAnimationChoice idle = new(table, idleName), walk = new(table, "loop_standing", new[] { 0, 1 }),
    run = new(table, "loop_standing", new[] { 0, 2 });
int negatives = 0;
void Check(bool value) { if (!value) throw new Exception("Preparation assertion failed."); }
void Reject(Action action)
{
    try { action(); }
    catch (Exception error) when (error is InvalidDataException or IOException or ArgumentException or ObjectDisposedException or AppearanceGraphBudgetException)
    { negatives++; return; }
    throw new Exception("Expected preparation rejection.");
}
OriginalInstallationInputs Open(string name) => OriginalInstallationInputs.Open(Path.Combine(root, name),
    Path.Combine(root, name, "mounts.cfg"), ["object/root.iff"]);
object Describe(InstalledCreaturePreparation preparation) => new
{
    Catalog = preparation.Catalog.Entries.Values.Single(), preparation.Sources,
    Idle = preparation.Options.Idle, Walk = preparation.Options.Movement["walk"], Run = preparation.Options.Movement["run"]
};
using OriginalInstallationInputs inputs = Open("good");
List<int> mutable = [0, 1];
InstalledCreaturePreparation prepared = InstalledCreaturePreparation.Create(inputs, "OBJECT\\ROOT.IFF", idle,
    walk with { ExplicitChildRoute = mutable }, run);
mutable[1] = 2;
Check(prepared.Options.Movement["walk"].ExplicitChildRoute!.SequenceEqual(new[] { 0, 1 }));
Check(prepared.Options.LoadAlbedoFactors && !prepared.Options.LoadAuthoredMovementInputs && prepared.Options.FaceInspection is null);
Check(prepared.Options.MaximumResources == 256 && prepared.Options.MaximumSourceBytes == 67108864);
Check(prepared.Sources.Count == 7 && prepared.Catalog.Entries.Count == 1);
try { ((IDictionary<string, AssetSourceIdentity>)prepared.Sources).Clear(); throw new Exception("Mutable sources."); }
catch (NotSupportedException) { }
try { ((IList<int>)prepared.Options.Movement["walk"].ExplicitChildRoute!)[1] = 2; throw new Exception("Mutable route."); }
catch (NotSupportedException) { }
Check(inputs.BorrowedOverlay.Lookup("object/root.iff").Kind == TreLookupKind.Present);
foreach (string name in new[] { "rootmissing", "rootmasked", "appearancemissing", "tablemasked", "missing", "masked", "parentmissing", "parentmasked", "cycle", "family", "appearance", "duplicate", "undeclared", "badtable", "limit" })
{
    using OriginalInstallationInputs variant = Open(name);
    Reject(() => InstalledCreaturePreparation.Create(variant, "object/root.iff", idle, walk, run));
    Check(variant.BorrowedOverlay.Lookup("object/root.iff").Kind == (name == "rootmissing" ? TreLookupKind.Missing : name == "rootmasked" ? TreLookupKind.Masked : TreLookupKind.Present));
}
foreach (string path in new[] { "../object/root.iff", "/object/root.iff", "https://host/a", "object/a/../root.iff", "object/root.iff\0", "appearance/test.sat" })
    Reject(() => InstalledCreaturePreparation.Create(inputs, path, idle, walk, run));
foreach (InstalledAnimationChoice bad in new[] { idle with { LogicalName = "absent" }, idle with { LogicalName = "default" },
    idle with { TablePath = "appearance/lat/missing.lat" }, idle with { TablePath = "../appearance/lat/all_m.lat" },
    idle with { LogicalName = "" }, idle with { ExplicitChildRoute = new[] { 0 } },
    walk with { ExplicitChildRoute = null }, walk with { ExplicitChildRoute = new[] { 0 } },
    walk with { ExplicitChildRoute = new[] { 0, 3 } }, walk with { ExplicitChildRoute = new[] { -1 } },
    walk with { ExplicitChildRoute = Enumerable.Repeat(0, 9).ToArray() } })
    Reject(() => InstalledCreaturePreparation.Create(inputs, "object/root.iff", bad, walk, run));
// A later archive modification discovers new pins; earlier preparation remains unchanged and rejects pinned loading.
string changeRoot = Path.Combine(root, "mutation"); Directory.CreateDirectory(changeRoot);
File.Copy(Path.Combine(root, "good/data.tre"), Path.Combine(changeRoot, "data.tre"));
File.Copy(Path.Combine(root, "good/mounts.cfg"), Path.Combine(changeRoot, "mounts.cfg"));
OriginalInstallationInputs mutation = OriginalInstallationInputs.Open(changeRoot, Path.Combine(changeRoot, "mounts.cfg"), ["object/root.iff"]);
InstalledCreaturePreparation before = InstalledCreaturePreparation.Create(mutation, "object/root.iff", idle, walk, run);
string beforeHash = before.Options.Movement["walk"].ClipSha256;
mutation.Dispose();
Reject(() => InstalledCreaturePreparation.Create(mutation, "object/root.iff", idle, walk, run));
byte[] archive = File.ReadAllBytes(Path.Combine(changeRoot, "data.tre"));
byte[] needle = System.Text.Encoding.ASCII.GetBytes("synthetic walk");
int offset = archive.AsSpan().IndexOf(needle); Check(offset >= 0); archive[offset] = (byte)'S';
File.WriteAllBytes(Path.Combine(changeRoot, "data.tre"), archive);
using OriginalInstallationInputs changed = OriginalInstallationInputs.Open(changeRoot, Path.Combine(changeRoot, "mounts.cfg"), ["object/root.iff"]);
InstalledCreaturePreparation after = InstalledCreaturePreparation.Create(changed, "object/root.iff", idle, walk, run);
Check(after.Options.Movement["walk"].ClipSha256 != beforeHash && before.Options.Movement["walk"].ClipSha256 == beforeHash);
changed.Dispose();
int parentOffset = archive.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes("object/base.iff"));
Check(parentOffset >= 0); archive[parentOffset] = (byte)'O';
File.WriteAllBytes(Path.Combine(changeRoot, "data.tre"), archive);
using OriginalInstallationInputs rootChanged = OriginalInstallationInputs.Open(changeRoot, Path.Combine(changeRoot, "mounts.cfg"), ["object/root.iff"]);
Reject(() => SharedCreatureTemplateProfileReader.Load(rootChanged.BorrowedOverlay.Lookup, "object/root.iff", before.Catalog.Entries.Values.Single().ObjectSha256));
object? original = null; IReadOnlyDictionary<string, AssetSourceIdentity>? closure = null; int vertices = 0;
if (fixture.GetProperty("Original").ValueKind != JsonValueKind.Null)
{
    JsonElement real = fixture.GetProperty("Original");
    string objectPath = real.GetProperty("ObjectPath").GetString()!;
    using OriginalInstallationInputs installation = OriginalInstallationInputs.Open(real.GetProperty("Root").GetString()!,
        real.GetProperty("Config").GetString()!, [objectPath]);
    InstalledCreaturePreparation actual = InstalledCreaturePreparation.Create(installation, objectPath, idle, walk, run);
    original = Describe(actual);
    uint crc = actual.Catalog.Entries.Keys.Single();
    SceneObjectResources loaded = SceneObjectResourceRouter.Load(new(0x0102030405060708, 0, 0, 0, 1, 0, 0, 0, crc, 0),
        actual.Catalog, actual.Options, installation.BorrowedOverlay.Lookup);
    closure = loaded.Sources;
    vertices = loaded.Creature!.Creature.Geometries!.Values.Sum(geometry => geometry.Skin.Positions.Count);
    Check(vertices > 0 && loaded.Creature.Creature.PreparedAnimation is not null);
    Check(loaded.Creature.Creature.AdditionalAnimations!.Count == 2);
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new { Synthetic = Describe(prepared), Original = original,
    ClosureSources = closure, ClosureGeometryVertices = vertices, NegativeCases = negatives }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS installed creature preparation negatives={negatives}, original={original is not null}, vertices={vertices}");
