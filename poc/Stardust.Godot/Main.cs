using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Proof;
public partial class Main : Node
{
    public override void _Ready()
    {
        if (!System.Environment.Is64BitProcess) { GD.PushError("Stardust client requires a 64-bit host process"); GetTree().Quit(1); return; }
        IReadOnlyList<string> events = OfflineTransportScenario.Run();
        foreach (string entry in events) GD.Print($"[core] {entry}");
        if (OS.GetCmdlineUserArgs().Contains("--self-test")) { GD.Print("PASS: Godot hosted the offline SOE transport transcript without a render window"); GetTree().Quit(); return; }
        BuildWorld(); BuildInterface(events);
    }
    private void BuildWorld()
    {
        WorldEnvironment world = new();
        world.Environment = new Environment { BackgroundMode = Environment.BGMode.Color, BackgroundColor = new Color("#122433"), AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color("#9ac4d8"), AmbientLightEnergy = 0.65f };
        AddChild(world);
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -30, 0), LightEnergy = 1.25f, ShadowEnabled = true });
        PlaneMesh plane = new() { Size = new Vector2(80, 80), Material = new StandardMaterial3D { AlbedoColor = new Color("#426348"), Roughness = 0.92f } };
        AddChild(new MeshInstance3D { Mesh = plane });
        CapsuleMesh capsule = new() { Height = 3.0f, Radius = 0.55f, Material = new StandardMaterial3D { AlbedoColor = new Color("#54c7d9"), Metallic = 0.2f } };
        AddChild(new MeshInstance3D { Mesh = capsule, Position = new Vector3(0, 1.5f, 0) });
        Camera3D camera = new() { Position = new Vector3(8, 6, 10), Current = true }; camera.LookAtFromPosition(camera.Position, new Vector3(0, 1, 0)); AddChild(camera);
    }
    private void BuildInterface(IReadOnlyList<string> events)
    {
        CanvasLayer layer = new(); AddChild(layer);
        ColorRect header = new() { Color = new Color(0.02f, 0.08f, 0.12f, 0.93f), OffsetRight = 1280, OffsetBottom = 76 }; layer.AddChild(header);
        Label title = new() { Text = "STARDUST 64-BIT CLIENT — GODOT FEASIBILITY PROOF", Position = new Vector2(28, 18) }; title.AddThemeFontSizeOverride("font_size", 24); header.AddChild(title);
        PanelContainer panel = new() { Position = new Vector2(32, 120), Size = new Vector2(470, 345) }; layer.AddChild(panel);
        VBoxContainer stack = new(); stack.AddThemeConstantOverride("separation", 12); panel.AddChild(stack);
        Label heading = new() { Text = "Offline transport boundary validated" }; heading.AddThemeFontSizeOverride("font_size", 20); stack.AddChild(heading);
        stack.AddChild(new Label { Text = "Godot host → engine-neutral SOE core → fixture-driven login state", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        foreach (string entry in events) stack.AddChild(new Label { Text = $"✓  {entry}" });
        stack.AddChild(new HSeparator());
        stack.AddChild(new Label { Text = "This offline transcript uses synthetic credentials and no network access. Legacy asset decoding is outside this milestone.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }
}
