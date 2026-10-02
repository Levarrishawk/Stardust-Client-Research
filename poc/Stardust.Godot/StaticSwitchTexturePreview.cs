using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Proof;

/// <summary>Fixed two-frame original static object inspection; manual policy, no switch timer.</summary>
public partial class StaticSwitchTexturePreview : Node
{
    public const string ShaderPath = "shader/anim_mission_terminal.sht";
    public const string ShaderPin = "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62";
    public static ObjectTemplateCatalog Catalog() => new(new[] { new ObjectTemplateCatalogEntry(2907007746, "object/tangible/terminal/shared_terminal_mission.iff",
        "E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281", "74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021", SceneObjectResourceRecipe.StaticAppearance) });
    public static SceneCreateObjectByCrcMessage CreateFixture()
    {
        byte[] bytes = SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, BitConverter.Int32BitsToSingle(int.MinValue), .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, 2907007746, 255));
        var create = SceneObjectLifecycleCodec.DecodeCreate(bytes); Array.Clear(bytes); return create;
    }
    public static void ValidateChoice(int frame, int detail)
    { if (frame is < 0 or > 1 || detail is < 0 or > 3) throw new ArgumentOutOfRangeException("Explicit frame0/1 and one of four serialized detail ordinals required."); }
    public static object FrameEvidence(SceneStaticResources result, int frame, IReadOnlyDictionary<string, int> counts)
    {
        string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
        var meshes = result.Appearance.Meshes;
        var materials = meshes.Values.SelectMany(m => m.Materials).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var textures = meshes.Values.SelectMany(m => m.Textures).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var switched = materials[ShaderPath].SwitchTexture ?? throw new InvalidDataException("Selected SWTS metadata missing.");
        return new { frame, result.ExternalReads, result.SourceBytes, counts, sources = result.Sources.Values, wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(result.Create)),
            baseSource = switched.BaseShaderSource, selectedPath = switched.Description.Frames[frame].TexturePath,
            raw0 = switched.Description.RawDtstWord0, raw1 = switched.Description.RawDtstWord1, frames = switched.Description.Frames,
            meshes = meshes.Select(p => new { path = p.Key, vertices = p.Value.Mesh.VertexCount, triangles = p.Value.Mesh.TriangleCount,
                hash = StaticMeshVerification.ComputeGeometrySha256(p.Value.Mesh), shaders = p.Value.Materials.Keys, textures = p.Value.Textures.Keys, effects = p.Value.Effects.Keys }),
            materials = materials.Select(p => new { path = p.Key, effect = p.Value.References.EffectPath, textures = p.Value.References.Textures.Select(t => new { t.Tag, t.SourceFlag, t.AssetPath }) }),
            textures = textures.Select(p => new { path = p.Key, mips = p.Value.Mips.Select(m => Hash(m.Rgba.Span)).ToArray() }) };
    }
    private OwnedBackgroundAssetLoad<SceneStaticResources[]>? load;
    private SceneStaticResources[]? sources;
    private readonly List<GeometryAppearancePreview> adapters = new();
    private readonly List<Resource> owned = new();
    private Label? status;
    private PreviewOrbitCamera? camera;
    private int selectedFrame, selectedDetail = 3;
    private bool disposed;

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key) { int at = Array.IndexOf(args, key); if (at < 0 || at + 1 >= args.Length || args.Count(s => s == key) != 1) throw new ArgumentException("Missing/duplicate switch preview option."); return args[at + 1]; }
        GetTree().AutoAcceptQuit = false;
        string root = Read("--asset-root"); byte[] configuration = File.ReadAllBytes(Read("--asset-config"));
        if (Convert.ToHexString(SHA256.HashData(configuration)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin differs.");
        bool cancel = args.Contains("--switch-cancel"), fixture = args.Contains("--switch-self-test"); string? capture = args.Contains("--switch-capture") ? Read("--switch-capture") : null;
        if (cancel && (fixture || capture is not null) || capture is not null && (fixture || DisplayServer.GetName() == "headless") || cancel && DisplayServer.GetName() != "headless" || DisplayServer.GetName() == "headless" && !fixture && !cancel) throw new ArgumentException("Separate bounded modes required.");
        if (capture is not null) { ValidateCapture(capture); ValidateCapture(SecondCapture(capture)); }
        var counts = new[] { new Dictionary<string, int>(), new Dictionary<string, int>() }; int ownerThread = System.Environment.CurrentManagedThreadId, workerThread = ownerThread, closed = 0;
        var firstLookup = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        byte[] ownedConfiguration = configuration.ToArray();
        var request = new OwnedBackgroundAssetLoad<SceneStaticResources[]>(token =>
        {
            workerThread = System.Environment.CurrentManagedThreadId;
            try
            {
                using var text = new StringReader(Encoding.UTF8.GetString(ownedConfiguration)); using var overlay = ConfiguredTreOverlay.Open(root, text);
                var result = new SceneStaticResources[2];
                for (int frame = 0; frame < 2; frame++)
                {
                    int index = frame;
                    TreLookupResult Resolve(string path)
                    {
                        token.ThrowIfCancellationRequested();
                        if (cancel && firstLookup.TrySetResult(true)) { if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Cancellation deadline."); token.ThrowIfCancellationRequested(); }
                        counts[index][path] = counts[index].GetValueOrDefault(path) + 1; return overlay.Lookup(path);
                    }
                    result[frame] = SceneStaticResourceReader.Load(CreateFixture(), Catalog(), Resolve, SceneStaticMaterialPolicy.RequireComplete, token,
                        switchTextureFrame: new(ShaderPath, ShaderPin, frame));
                    if (result[frame].ExternalReads != 27 || result[frame].SourceBytes != 1401794 || counts[frame].Count != 27 || counts[frame].Values.Any(n => n != 1) || result[frame].Appearance.Meshes.Count != 4 || result[frame].Appearance.Diagnostics.Count != 0) throw new InvalidDataException("Pinned complete frame closure differs.");
                }
                return result;
            }
            finally { Volatile.Write(ref closed, 1); }
        }); load = request;
        try
        {
            var transfer = request.TakeAsync(); if (cancel && await Task.WhenAny(firstLookup.Task, request.WorkerCompletion) == firstLookup.Task) request.Cancel();
            sources = await transfer; if (disposed || !IsInsideTree()) return;
            if (closed != 1 || workerThread == ownerThread || System.Environment.CurrentManagedThreadId != ownerThread) throw new AssetPreviewException("Archive/owner transfer differs.");
        }
        catch (OperationCanceledException) when (cancel)
        {
            if (closed != 1 || adapters.Count != 0) throw new AssetPreviewException("Canceled switch preview published resources.");
            GD.Print("PASS: switch canceled first_lookup=true archives_closed=true gpu_publications=0"); await ShutdownAsync(); GetTree().Quit(); return;
        }
        finally { request.Dispose(); load = null; }
        for (int frame = 0; frame < 2; frame++)
        {
            GD.Print("switch_frame=" + JsonSerializer.Serialize(FrameEvidence(sources![frame], frame, counts[frame])));
            var adapter = new GeometryAppearancePreview(sources[frame].Appearance.Geometry, sources[frame].Appearance.Meshes, allowMainTextureOnly: true); adapters.Add(adapter);
            adapter.Root.Visible = false; AddChild(adapter.Root);
            var expectedDiagnostics = sources[frame].Appearance.Meshes.Where(p => p.Value.Materials.ContainsKey(ShaderPath)).Select(p => p.Key + ": " + ShaderPath + ": Material is outside opaque MAIN inspection policy.").ToHashSet(StringComparer.Ordinal);
            if (adapter.UniqueGpuTextures != 7 || adapter.MeshInstances != 4 || adapter.NeutralSurfaces != 2 || adapter.MaterialDiagnostics.Count != 2 || expectedDiagnostics.Count != 2 || !expectedDiagnostics.SetEquals(adapter.MaterialDiagnostics)) throw new AssetPreviewException("Unexpected terminal material boundary.");
            BindSwitchMain(adapter, sources[frame]);
        }
        GD.Print("PASS: switch original frames=2 sources_each=27 bytes_each=1401794 archives_closed=true worker_transfer=true gpu_textures=14 mesh_instances=8");
        BuildControls(); Select(0, 3);
        if (fixture || capture is not null)
        {
            VerifyRetained();
            if (capture is not null) await CaptureAsync(Path.GetFullPath(capture));
            await ShutdownAsync(); GD.Print("PASS: switch teardown roots_released=true adapters_disposed=true worker_observed=true"); GetTree().Quit();
        }
    }
    private static string SecondCapture(string capture) => Path.Combine(Path.GetDirectoryName(capture)!, Path.GetFileNameWithoutExtension(capture) + "-frame1.png");
    private static void ValidateCapture(string path)
    {
        string full = Path.GetFullPath(path), prefix = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(full) != ".png" || File.Exists(full) || !Directory.Exists(Path.GetDirectoryName(full))) throw new ArgumentException("Fresh private PNG pair required.");
    }
    private IEnumerable<MeshInstance3D> Meshes(Node root)
    { foreach (Node child in root.GetChildren()) { if (child is MeshInstance3D mesh) yield return mesh; foreach (var nested in Meshes(child)) yield return nested; } }
    private void BindSwitchMain(GeometryAppearancePreview adapter, SceneStaticResources source)
    {
        StandardMaterial3D? material = null; int bound = 0;
        foreach (var node in Meshes(adapter.Root))
        {
            var mesh = source.Appearance.Meshes[node.GetMeta("original_mesh_path").AsString()];
            if (node.Mesh.GetSurfaceCount() != mesh.Mesh.Primitives.Count) throw new AssetPreviewException("Terminal omitted authored surfaces.");
            for (int surface = 0; surface < mesh.Mesh.Primitives.Count; surface++)
            {
                if (mesh.Mesh.Primitives[surface].ShaderPath != ShaderPath) continue;
                var description = mesh.Materials[ShaderPath];
                if (description.References.EffectPath != "effect/a_emis_full.eft" || description.SwitchTexture is not { } switched || switched.Selection.ShaderPath != ShaderPath || switched.Selection.ShaderSha256 != ShaderPin) throw new AssetPreviewException("Only the pinned SWTS screen effect may be overridden.");
                var binding = description.References.Textures.Single(t => t.Tag == 0x4D41494E);
                if (binding.SourceFlag != 0 || binding.AssetPath != switched.Description.Frames[switched.Selection.FrameIndex].TexturePath || description.Parameters.CoordinateSets[0x4D41494E] != 0) throw new AssetPreviewException("Explicit MAIN/UV0 binding required.");
                if (material is null)
                {
                    // Built-in Godot material is an explicit unshaded inspection policy, not original EFCT translation.
                    material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoTexture = adapter.GetOwnedTexture(mesh.Textures[binding.AssetPath]) };
                    owned.Add(material);
                }
                node.SetSurfaceOverrideMaterial(surface, material); bound++;
            }
        }
        if (bound != 2) throw new AssetPreviewException("Expected two original SWTS surfaces per frame.");
    }
    private void Select(int frame, int detail)
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("Main-thread selection required.");
        ValidateChoice(frame, detail); ObjectDisposedException.ThrowIf(disposed, this);
        var level = sources![frame].Appearance.Geometry.Root.Detail!.Levels[detail]; float distance = level.LowerDistance + (level.UpperDistance - level.LowerDistance) * .5f;
        // Existing selector remains unchanged; the caller explicitly chooses its band midpoint.
        foreach (var adapter in adapters) adapter.Select(distance);
        for (int index = 0; index < 2; index++) adapters[index].Root.Visible = index == frame;
        var visible = adapters.SelectMany(a => Meshes(a.Root)).Where(m => m.IsVisibleInTree()).ToArray();
        if (visible.Length != 1 || visible[0].GetMeta("original_mesh_path").AsString() != level.ChildPath) throw new AssetPreviewException("Explicit detail selected another mesh.");
        selectedFrame = frame; selectedDetail = detail; Refresh();
    }
    private void Refresh() { if (status is not null) status.Text = $"Frame {selectedFrame} | serialized LOD {selectedDetail} | 27 sources / 1,401,794 bytes each | fixed 14 textures / 8 meshes"; }
    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e") }; owned.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        var bounds = adapters[0].Bounds; camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .55f }; AddChild(camera); camera.Configure(bounds.GetCenter(), Math.Max(bounds.Size.Length() * .65f, 1));
        // Face the authored SWTS surface; capture then retains this exact camera for both frames.
        var finest = sources![0].Appearance.Meshes[sources[0].Appearance.Geometry.Root.Detail!.Levels[3].ChildPath!];
        var normals = finest.Mesh.Primitives.Where(p => p.ShaderPath == ShaderPath).SelectMany(p => p.Vertices).Select(v => new Vector3(v.Normal.X, v.Normal.Y, -v.Normal.Z)).ToArray();
        Vector3 outward = normals.Aggregate(Vector3.Zero, (a, b) => a + b).Normalized(); if (outward.LengthSquared() < .5f) outward = Vector3.Back;
        float radius = Math.Max(bounds.Size.Length() * .65f, 1); Vector3 side = new(outward.Z, 0, -outward.X);
        camera.Position = bounds.GetCenter() + outward * radius * 2.4f + Vector3.Up * radius * .35f + side * radius * .45f; camera.LookAt(bounds.GetCenter());
        var layer = new CanvasLayer(); AddChild(layer); var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 155 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | ORIGINAL TERMINAL / EXPLICIT SWTS FRAME", Position = new(24, 10) }); status = new Label { Position = new(24, 40) }; panel.AddChild(status);
        void Button(string text, int x, Action action) { var b = new Button { Text = text, Position = new(x, 76), Size = new(110, 30) }; panel.AddChild(b); b.Pressed += action; }
        Button("Frame 0", 24, () => Select(0, selectedDetail)); Button("Frame 1", 148, () => Select(1, selectedDetail));
        for (int lod = 0; lod < 4; lod++) { int index = lod; Button("LOD " + index, 292 + lod * 124, () => Select(selectedFrame, index)); }
        panel.AddChild(new Label { Text = "Manual frames | SWTS: built-in unshaded MAIN | effect/timing unknown | drag to orbit, scroll to zoom, R reset", Position = new(24, 120) }); Refresh();
    }
    private string Fingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var adapter in adapters)
        {
            hash.AppendData(BitConverter.GetBytes(adapter.Root.GetInstanceId()));
            foreach (var mesh in Meshes(adapter.Root))
            {
                hash.AppendData(BitConverter.GetBytes(mesh.Mesh.GetInstanceId()));
                for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                {
                    var material = mesh.GetSurfaceOverrideMaterial(surface) ?? mesh.Mesh.SurfaceGetMaterial(surface); hash.AppendData(BitConverter.GetBytes(material.GetInstanceId()));
                    ImageTexture texture;
                    if (material is ShaderMaterial shader) { hash.AppendData(BitConverter.GetBytes(shader.Shader.GetInstanceId())); texture = (ImageTexture)shader.GetShaderParameter("albedo_texture").AsGodotObject(); }
                    else texture = (ImageTexture)((StandardMaterial3D)material).AlbedoTexture;
                    hash.AppendData(BitConverter.GetBytes(texture.GetInstanceId()));
                    using var image = texture.GetImage(); hash.AppendData(image.GetData());
                    string path = mesh.GetMeta("original_mesh_path").AsString(); StaticMeshPreview.VerifyUploadedGeometry((ArrayMesh)mesh.Mesh, sources![adapters.IndexOf(adapter)].Appearance.Meshes[path].Mesh);
                }
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private void VerifyRetained()
    {
        string before = Fingerprint(); int selections = 0;
        for (int repeat = 0; repeat < 8; repeat++) for (int frame = 0; frame < 2; frame++) for (int detail = 0; detail < 4; detail++)
        { Select(frame, detail); if (Fingerprint() != before || adapters.Sum(a => a.UniqueGpuTextures) != 14 || adapters.Sum(a => a.MeshInstances) != 8) throw new AssetPreviewException("Selection recreated or changed resources."); selections++; }
        foreach (var choice in new[] { (-1, 0), (2, 0), (0, -1), (0, 4) }) { bool rejected = false; try { Select(choice.Item1, choice.Item2); } catch (ArgumentOutOfRangeException) { rejected = true; } if (!rejected || Fingerprint() != before) throw new AssetPreviewException("Invalid choice mutated resources."); }
        Select(0, 3); GD.Print($"PASS: switch retained selections={selections} negative_choices=4 geometry_unchanged=true resource_ids_fixed=true textures14=true mip_readback=true");
    }
    private async Task CaptureAsync(string capture)
    {
        var transform = camera!.GlobalTransform; string fingerprint = Fingerprint(); var images = new List<Image>();
        try
        {
            for (int frame = 0; frame < 2; frame++)
            {
                Select(frame, 3);
                for (int wait = 0; wait < 3; wait++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var image = GetViewport().GetTexture().GetImage(); images.Add(image);
                if (image.SavePng(frame == 0 ? capture : SecondCapture(capture)) != Error.Ok) throw new IOException("Capture pair failed.");
                if (camera.GlobalTransform != transform || Fingerprint() != fingerprint) throw new AssetPreviewException("Camera/geometry/resources changed between frames.");
            }
            int changed = 0; for (int y = 180; y < images[0].GetHeight(); y++) for (int x = 0; x < images[0].GetWidth(); x++) if (images[0].GetPixel(x, y) != images[1].GetPixel(x, y)) changed++;
            if (changed < 10) throw new AssetPreviewException("Original screen frame difference not visible outside UI.");
            GD.Print("switch_capture=" + JsonSerializer.Serialize(new { width = images[0].GetWidth(), height = images[0].GetHeight(), regionY = 180, changed, camera_fixed = true, detail = 3, geometry_fixed = true }));
            GD.Print("PASS: switch GPU capture frames=2 ui_excluded=true rendered_pixels_differ=true camera_lod_fixed=true");
        }
        finally { foreach (var image in images) image.Dispose(); }
    }
    private async Task ShutdownAsync()
    {
        if (disposed) return; disposed = true; var request = load; request?.Dispose(); if (request is not null) await request.WorkerCompletion.ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default); load = null;
        foreach (var adapter in adapters) { ulong id = adapter.Root.GetInstanceId(); adapter.Root.Free(); adapter.Dispose(); if (GodotObject.InstanceFromId(id) is not null) throw new AssetPreviewException("Terminal root survived teardown."); } adapters.Clear(); sources = null;
        foreach (var resource in owned) resource.Dispose(); owned.Clear();
    }
    private async Task CloseAsync() { await ShutdownAsync(); GetTree().Quit(); }
    public override void _Notification(int what) { if (what == NotificationWMCloseRequest && !disposed) _ = CloseAsync(); }
    public override void _ExitTree() { if (!disposed) _ = ShutdownAsync().ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default); }
}

