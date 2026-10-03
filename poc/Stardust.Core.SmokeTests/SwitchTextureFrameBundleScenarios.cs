using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SwitchTextureFrameBundleScenarios
{
    private sealed record Resource(string Path, string Kind, string? Hex);
    private sealed record Fixture(string Name, string RootPath, string RootSha256, Resource[] Resources,
        SwitchTextureFrameBundleLimits Limits, string Reject, string[] ExpectedCalls);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static object Image(SwitchTextureFrameImage i) => new { Format = i.Format.ToString(), ColorSpace = i.ColorSpace.ToString(), AlphaMode = i.AlphaMode.ToString(), i.DxgiFormat,
        Mips = i.Mips.Select(m => new { m.Width, m.Height, Bytes = m.DecodedBytes, Sha256 = Hash(m.CopyRgba()) }), i.RetainedTerminalBytes };
    private static object Receipt(SwitchTextureFrameBundle b) => new { b.ShaderPath, b.BaseShaderPath, b.RawDtstWord0, b.RawDtstWord1, b.FrameCount, IntervalBits = BitConverter.SingleToUInt32Bits(b.Interval),
        Frames = b.Frames.Select(f => new { f.Index, f.TextureTag, f.TexturePath }), Images = b.Images.ToDictionary(p => p.Key, p => Image(p.Value), StringComparer.Ordinal), b.Sources, b.SourceBytes, b.DecodedBytes, b.MipCount };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/frame-bundle fixtures required");
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[2]))!; var cases = new List<object>();
        Dictionary<string, TreLookupResult> Resources(Fixture f) => f.Resources.ToDictionary(r => r.Path, r => new TreLookupResult(Enum.Parse<TreLookupKind>(r.Kind), r.Hex is null ? null : Convert.FromHexString(r.Hex), 88), StringComparer.Ordinal);
        SwitchTextureFrameBundle Build(Fixture f) { var resources = Resources(f); return SwitchTextureFrameBundle.Load(p => resources[p], f.RootPath, f.RootSha256, f.Limits); }
        foreach (var f in fixtures)
        {
            var resources = Resources(f); var calls = new List<string>();
            TreLookupResult Lookup(string path) { calls.Add(path); return resources.GetValueOrDefault(path, new(TreLookupKind.Missing)); }
            try { var bundle = SwitchTextureFrameBundle.Load(Lookup, f.RootPath, f.RootSha256, f.Limits); Check(f.Reject == "None", "Expected rejection: " + f.Name); cases.Add(new { f.Name, Reject = "None", Calls = calls, Bundle = Receipt(bundle) }); }
            catch (InvalidDataException error) { Check(f.Reject == "Input", "Unexpected input reject: " + f.Name + ": " + error.Message); cases.Add(new { f.Name, Reject = "Input", Calls = calls }); }
            catch (AppearanceGraphBudgetException error) { Check(f.Reject == "Input", "Unexpected source budget: " + f.Name + ": " + error.Message); cases.Add(new { f.Name, Reject = "Input", Calls = calls }); }
            catch (ArgumentOutOfRangeException) { Check(f.Reject == "Options", "Unexpected option reject: " + f.Name); cases.Add(new { f.Name, Reject = "Options", Calls = calls }); }
            Check(calls.SequenceEqual(f.ExpectedCalls), "Read count/order: " + f.Name);
            foreach (var r in f.Resources.Where(r => r.Hex is not null)) Check(Convert.ToHexString(resources[r.Path].Payload!) == r.Hex, "Caller source bytes unchanged");
        }
        var exact = fixtures[0]; var ready = Resources(exact); var prior = Build(exact); string fingerprint = JsonSerializer.Serialize(Receipt(prior)); int checks = 0;
        void Assert(bool value, string message) { Check(value, message); checks++; }
        void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
        TreLookupResult Resolve(string path) { Check(ready.ContainsKey(path), "No unrelated default/effect I/O"); return ready[path]; }
        var alias = Build(fixtures.Single(f => f.Name == "alias-path")); Assert(ReferenceEquals(alias.Frames[0].Image, alias.Frames[2].Image) && alias.Images.Count == 2 && alias.DecodedBytes == 16 && alias.MipCount == 3 && alias.Sources.Count == 4, "Repeated authored path aliases counted once");
        var identical = Build(fixtures.Single(f => f.Name == "equal-bytes-distinct-paths")); Assert(!ReferenceEquals(identical.Frames[0].Image, identical.Frames[1].Image) && identical.DecodedBytes == 24 && identical.MipCount == 4, "Distinct paths retain separate equal-byte identities");
        var borrowed = Resources(exact); var owned = SwitchTextureFrameBundle.Load(p => { if (p == "texture/b.dds") foreach (string earlier in new[] { exact.RootPath, "shader/base.sht", "texture/a.dds" }) Array.Clear(borrowed[earlier].Payload!); return borrowed[p]; }, exact.RootPath, exact.RootSha256, exact.Limits);
        foreach (var source in borrowed.Values) Array.Clear(source.Payload!); Assert(JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Snapshot before later callback reuse and input clearing");
        byte[] rgbaCopy = prior.Frames[0].Image.Mips[0].CopyRgba(); Array.Clear(rgbaCopy); var imageCopy = prior.Frames[0].Image.CopyImage(); Check(MemoryMarshal.TryGetArray(imageCopy.Mips[0].Rgba, out var segment), "Owned copy array"); Array.Clear(segment.Array!);
        Assert(JsonSerializer.Serialize(Receipt(prior)) == fingerprint, "Copy accessors cannot mutate image pixels");
        var switchCopy = prior.CopySwitchDescription(); var baseCopy = prior.CopyBaseDescription(); Check(MemoryMarshal.TryGetArray(switchCopy.Metadata.Payload, out segment), "Switch copy memory"); Array.Clear(segment.Array!); Check(MemoryMarshal.TryGetArray(baseCopy.Metadata.Payload, out segment), "Base copy memory"); Array.Clear(segment.Array!);
        Assert(prior.CopySwitchDescription().Frames.Count == 2 && prior.CopyBaseDescription().Textures.Count == 1 && JsonSerializer.Serialize(Receipt(prior)) == fingerprint, "Private shader metadata not exposed through returned copies");
        bool ReadOnly(Action change) { try { change(); } catch (NotSupportedException) { return true; } return false; }
        Assert(ReadOnly(() => ((IDictionary<string, SwitchTextureFrameImage>)prior.Images).Clear()) && ReadOnly(() => ((IList<SwitchTextureFrameInput>)prior.Frames).Clear()) && ReadOnly(() => ((IList<SwitchTextureFrameMip>)prior.Frames[0].Image.Mips).Clear()) && ReadOnly(() => ((IDictionary<string, AssetSourceIdentity>)prior.Sources).Clear()), "Read-only bundle collections");
        var failure = new IOException("controlled"); bool same = false; try { SwitchTextureFrameBundle.Load(p => p == "texture/b.dds" ? throw failure : Resolve(p), exact.RootPath, exact.RootSha256); } catch (IOException e) { same = ReferenceEquals(e, failure); } Assert(same, "Later callback exception identity");
        Throws<InvalidOperationException>(() => SwitchTextureFrameBundle.Load(p => { SwitchTextureFrameBundle.Load(Resolve, exact.RootPath, exact.RootSha256); return Resolve(p); }, exact.RootPath, exact.RootSha256));
        Throws<InvalidOperationException>(() => SwitchTextureFrameBundle.Load(p => { try { SwitchTextureFrameBundle.Load(Resolve, exact.RootPath, exact.RootSha256); } catch (InvalidOperationException) { } return Resolve(p); }, exact.RootPath, exact.RootSha256));
        using (var cancelled = new CancellationTokenSource()) { cancelled.Cancel(); int reads = 0; Throws<OperationCanceledException>(() => SwitchTextureFrameBundle.Load(p => { reads++; return Resolve(p); }, exact.RootPath, exact.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 0, "Precancel no reads"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => SwitchTextureFrameBundle.Load(p => { reads++; cancelled.Cancel(); return Resolve(p); }, exact.RootPath, exact.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 1, "First callback cancellation"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => SwitchTextureFrameBundle.Load(p => { reads++; if (p == "texture/b.dds") cancelled.Cancel(); return Resolve(p); }, exact.RootPath, exact.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 4, "Last callback cancellation rejects publication"); }
        Assert(JsonSerializer.Serialize(Receipt(SwitchTextureFrameBundle.Load(Resolve, exact.RootPath, exact.RootSha256, exact.Limits))) == fingerprint && JsonSerializer.Serialize(Receipt(prior)) == fingerprint, "Guard recovery/prior bundle stable");
        Assert(SwitchTextureFrameBundle.Load(p => { var other = Task.Run(() => SwitchTextureFrameBundle.Load(Resolve, exact.RootPath, exact.RootSha256)).GetAwaiter().GetResult(); Check(other.FrameCount == 2, "Independent callback thread"); return Resolve(p); }, exact.RootPath, exact.RootSha256).FrameCount == 2, "Thread-local guard");
        Check(checks == 14, "Controlled check accounting"); Parallel.For(0, 32, _ => Check(JsonSerializer.Serialize(Receipt(Build(exact))) == fingerprint, "Independent concurrent bundles"));
        var pins = new Dictionary<string, (int Priority, int Bytes, string Sha)>(StringComparer.Ordinal)
        {
            ["shader/anim_mission_terminal.sht"] = (38,475,"B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62"),
            ["shader/defaultemis.sht"] = (38,171,"BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7")
        };
        string[] ddsPins = ["6BA1EC54844D2614DCAF9C37441C7373D63786F2F3B0D43B953F9A0F15BCC938","226A16D33CCFFE97EA9BA07B13F13D4B08C973006FC99C9959C670CCDE382ABD","87D602AF5538E04B55B71570F3736AAE4782C953A6E6F7D2CD2BBD3CE60C7B95","347572A7A980F2A29775300C2D68AD72986C7E3264E6347B2E769D49B807C897","829D371B0AFE48C607F2CA4AD0CD0A9B1E46372364896637072491A47B1A5886","698E795675D992628CB5F92441E5508057AA7F479EF9BA2DA5DBDBE0CD27716C","913AB9059C0F1EEF20F5EC4573F564490418ADAE517742B117AC52736F588E95","828EB4C1CB4590F9BDFD45992BA117B63D323791AC3DE1E8560A3FB032B36652"];
        for (int i = 0; i < 8; i++) pins.Add($"texture/anim_mission_terminal_{i + 1:00}.dds", (34,11088,ddsPins[i]));
        var originalCalls = new List<string>(); var originals = new Dictionary<string, TreLookupResult>(StringComparer.Ordinal); SwitchTextureFrameBundle original;
        using (var config = File.OpenText(args[1])) using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            original = SwitchTextureFrameBundle.Load(p => { Check(pins.TryGetValue(p, out var pin), "Only ten authorized original sources"); originalCalls.Add(p); var source = overlay.Lookup(p); Check(source.Kind == TreLookupKind.Present && source.Payload is not null && source.Priority == pin.Priority && source.Payload.Length == pin.Bytes && Hash(source.Payload) == pin.Sha, "Original source priority/size/SHA pin"); originals.Add(p, source); return source; },
                "shader/anim_mission_terminal.sht", pins["shader/anim_mission_terminal.sht"].Sha, new(8,10,89350,349536,64,475,new(128,11088,43692)));
        }
        foreach (var source in originals.Values) Array.Clear(source.Payload!);
        Check(original.Images.Count == 8 && original.Sources.Count == 10 && original.FrameCount == 8 && original.RawDtstWord1 == 0x3DCCCCCD && original.Images.Values.All(i => i.Format == DdsFormat.Bc2 && i.Mips.Count == 8), "Original exact complete frame-input bundle");
        Emit("frame_bundle_synthetic", new { Cases = cases, ControlledChecks = checks, ConcurrentLoads = 32, Immutable = true });
        Emit("frame_bundle_original", new { Bundle = Receipt(original), Calls = originalCalls, ArchivesClosed = true });
        Console.WriteLine("PASS: immutable bounded SWTS frame inputs, exact path aliases, modern eligibility and all eight original DDS/mip identities.");
    }
}
