using System.Text.Json;
using Stardust.Core.Configuration;

if (args.Length != 2) throw new ArgumentException("Expected fixtures.json receipt.json.");
using JsonDocument fixtures = JsonDocument.Parse(File.ReadAllBytes(args[0]));
JsonElement f = fixtures.RootElement;
string root = f.GetProperty("Root").GetString()!;
string config = f.GetProperty("Config").GetString()!;
int negatives = 0, cleanup = 0;
void Check(bool valid) { if (!valid) throw new Exception("Preflight assertion failed."); }
void Reject(Action action)
{
    try { action(); }
    catch (Exception error) when (error is InvalidDataException or ArgumentException or IOException or ObjectDisposedException)
    { negatives++; return; }
    throw new Exception("Expected rejection.");
}
void Closed(string file)
{
    using FileStream exclusive = new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
}
List<string> paths = ["OBJECT\\TEST.IFF", "masked.iff", "missing.iff"];
OriginalInstallationInputs input = OriginalInstallationInputs.Open(root, config, paths);
OriginalInstallationReceipt synthetic = input.Receipt;
Check(!synthetic.AllResourcesPresent && synthetic.MountCount == 2);
paths.Clear();
Check(synthetic.Resources.Count == 3);
Reject(input.RequireAllPresent);
try { ((IList<OriginalInstallationResource>)synthetic.Resources).Clear(); throw new Exception("Mutable receipt."); }
catch (NotSupportedException) { }
var borrowed = input.BorrowedOverlay;
input.Dispose(); input.Dispose();
Reject(() => _ = input.BorrowedOverlay);
Reject(input.RequireAllPresent);
Reject(() => borrowed.Lookup("object/test.iff"));
Check(input.Receipt == synthetic);
Closed(Path.Combine(root, "low.tre")); Closed(Path.Combine(root, "high.tre")); cleanup++;
foreach (string path in new[] { "", "../a", "a/../b", "./a", "a/./b", "/a", "\\a", "\\\\host\\a", "a//b", "C:a", "https://host/a", "file:///a", "a\0b", " a", "a ", "a\nb", new string('a', 1025), "a\u0080" })
    Reject(() => OriginalInstallationInputs.Open(root, config, [path]));
foreach (string path in new[] { "https://host/a", "file:///a", "\\\\host\\a", "//host/a", "C:relative", "relative", root + "/../a", root + "/./a", root + "/a\0b" })
{
    Reject(() => OriginalInstallationInputs.Open(path, config, ["object/test.iff"]));
    Reject(() => OriginalInstallationInputs.Open(root, path, ["object/test.iff"]));
}
Reject(() => OriginalInstallationInputs.Open(root, config, Array.Empty<string>()));
Reject(() => OriginalInstallationInputs.Open(root, config, ["object/test.iff", "OBJECT\\TEST.IFF"]));
Reject(() => OriginalInstallationInputs.Open(root, config, Enumerable.Range(0, 129).Select(i => $"{i}.iff")));
foreach (string name in new[] { "large.cfg", "utf8.cfg", "empty.cfg", "duplicate.cfg", "remote.cfg", "malformed.cfg" })
    Reject(() => OriginalInstallationInputs.Open(root, Path.Combine(root, name), ["object/test.iff"]));
Reject(() => OriginalInstallationInputs.Open(root, Path.Combine(root, "broken.cfg"), ["object/test.iff", "broken.iff"]));
Closed(Path.Combine(root, "low.tre")); Closed(Path.Combine(root, "broken.tre")); cleanup++;
Reject(() => OriginalInstallationInputs.Open(root, Path.Combine(root, "badarchive.cfg"), ["object/test.iff"]));
Closed(Path.Combine(root, "low.tre")); Closed(Path.Combine(root, "high.tre")); Closed(Path.Combine(root, "bad.tre")); cleanup++;
using (OriginalInstallationInputs boundary = OriginalInstallationInputs.Open(root, Path.Combine(root, "boundary.cfg"),
    Enumerable.Range(0, 128).Select(i => $"{i}.iff")))
    Check(boundary.Receipt.Resources.Count == 128 && boundary.Receipt.ConfigurationBytes == 65536);
using (OriginalInstallationInputs positive = OriginalInstallationInputs.Open(root, config, ["object/test.iff"]))
{
    positive.RequireAllPresent();
    Check(positive.Receipt.AllResourcesPresent);
}
Closed(Path.Combine(root, "low.tre")); Closed(Path.Combine(root, "high.tre")); cleanup++;
OriginalInstallationReceipt? original = null;
if (f.TryGetProperty("Original", out JsonElement real))
{
    string[] requested = real.GetProperty("Expected").EnumerateArray().Select(row => row.GetProperty("LogicalPath").GetString()!).ToArray();
    using OriginalInstallationInputs installation = OriginalInstallationInputs.Open(real.GetProperty("Root").GetString()!, real.GetProperty("Config").GetString()!, requested);
    installation.RequireAllPresent();
    original = installation.Receipt;
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new { Synthetic = synthetic, Original = original,
    NegativeCases = negatives, CleanupChecks = cleanup }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS preflight negatives={negatives}, cleanup={cleanup}, original={original is not null}");
