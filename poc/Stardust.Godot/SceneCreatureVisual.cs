using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Main-thread owned visual for one exact registry revision; caller supplies engine placement.</summary>
public sealed class SceneCreatureVisual : IDisposable
{
    private SceneCreatureResources? resources;
    private SkeletalMeshPreview? mesh;
    private AnimationPreviewClock? idle;
    private bool disposed, moving;
    private Func<bool>? identityCheck;
    public Node3D Root { get; }
    public bool IsRetired => disposed;
    public bool IsPublished { get; private set; }
    public double IdleFrame => idle?.Frame ?? 0;
    public int TextureCount => mesh?.TextureCount ?? 0;
    public Aabb Bounds => mesh?.Bounds ?? default;
    public static int Allocations { get; private set; }
    public static int Disposals { get; private set; }

    // Pure identity check shared by native diagnostics and every Godot publication/update guard.
    public static bool Matches(SceneObjectRegistry owner, SceneObjectHandle handle, SceneCreatureResources source)
    {
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(source);
        if (!owner.IsCurrent(handle)) return false;
        var snapshot = owner.Find(handle.ObjectId);
        return snapshot is not null && SceneObjectLifecycleCodec.EncodeCreate(snapshot.Create).AsSpan().SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(source.Create));
    }
    public static void ValidateEngineTransform(Transform3D value)
    {
        var vectors = new[] { value.Origin, value.Basis.X, value.Basis.Y, value.Basis.Z };
        if (vectors.Any(v => !float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z)))
            throw new ArgumentException("Finite explicit engine transform required.");
        if (Math.Max(Math.Abs(value.Origin.X), Math.Max(Math.Abs(value.Origin.Y), Math.Abs(value.Origin.Z))) > 1e6f)
            throw new ArgumentException("Engine origin budget.");
        var axes = vectors.Skip(1).ToArray();
        if (axes.Any(v => v.Length() < 1e-4f || v.Length() > 1e4f) || Math.Abs(axes[0].Normalized().Dot(axes[1].Normalized())) > 1e-4f || Math.Abs(axes[0].Normalized().Dot(axes[2].Normalized())) > 1e-4f || Math.Abs(axes[1].Normalized().Dot(axes[2].Normalized())) > 1e-4f)
            throw new ArgumentException("Finite nonsingular orthogonal engine basis required; no source quaternion conversion.");
    }
    private static void MainThread() { if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("Creature visual belongs to the Godot main thread."); }
    public static bool MatchesWorld(SceneWorldCoordinator world, ScenePreparedCreaturePublication publication)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(publication);
        return world.IsCurrent(publication) && SceneObjectLifecycleCodec.EncodeCreate(publication.Resources.Create).AsSpan().SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(publication.Placement.RawCreate));
    }
    public static SceneCreatureVisual? TryPublish(SceneObjectRegistry owner, SceneObjectHandle handle, SceneCreatureResources source,
        Transform3D engineTransform, Node parent, bool visible = true, Action? beforePublication = null, Func<bool>? currentAttempt = null)
        => TryPublishChecked(source, engineTransform, parent, visible, beforePublication, () => Matches(owner, handle, source) && currentAttempt?.Invoke() != false);
    public static SceneCreatureVisual? TryPublishWorld(SceneWorldCoordinator world, ScenePreparedCreaturePublication publication,
        Transform3D engineTransform, Node parent, bool visible = true, Action? beforePublication = null, Func<bool>? currentAttempt = null)
    {
        ArgumentNullException.ThrowIfNull(publication);
        return TryPublishChecked(publication.Resources, engineTransform, parent, visible, beforePublication, () => MatchesWorld(world, publication) && currentAttempt?.Invoke() != false);
    }
    /// <summary>Explicit caller-owned lifetime guard for accepted resource publications from another host boundary.</summary>
    public static SceneCreatureVisual? TryPublishCurrent(SceneCreatureResources source, Transform3D engineTransform, Node parent,
        Func<bool> current, bool visible = true, Action? beforePublication = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(current);
        return TryPublishChecked(source, engineTransform, parent, visible, beforePublication, current);
    }
    private static SceneCreatureVisual? TryPublishChecked(SceneCreatureResources source, Transform3D engineTransform, Node parent,
        bool visible, Action? beforePublication, Func<bool> check)
    {
        MainThread(); ArgumentNullException.ThrowIfNull(parent); ValidateEngineTransform(engineTransform);
        if (!GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree()) throw new ArgumentException("Live parent required.");
        if (!check()) return null; // No Godot allocations before exact identity validation.
        SceneCreatureVisual? candidate = null;
        try
        {
            candidate = new(source, engineTransform, check);
            beforePublication?.Invoke();
            if (!check()) { candidate.Dispose(); return null; }
            parent.AddChild(candidate.Root);
            if (!check()) { candidate.Dispose(); return null; }
            candidate.Root.Visible = visible; candidate.IsPublished = true; return candidate;
        }
        catch { candidate?.Dispose(); throw; }
    }
    private SceneCreatureVisual(SceneCreatureResources source, Transform3D transform, Func<bool> check)
    {
        resources = source; identityCheck = check;
        Root = new Node3D { Name = "OwnedSceneCreature", Visible = false, Transform = transform }; Allocations++;
        try
        {
            mesh = new(source.Creature); Root.AddChild(mesh.Root);
            var clip = source.Creature.PreparedAnimation ?? throw new InvalidDataException("Prepared idle required.");
            idle = new(clip.FrameRate, clip.EndFrame, clip.ProxyTimeScale); idle.SetPlaying(true); mesh.SetOriginalFrame(0);
        }
        catch { Dispose(); throw; }
    }
    private bool Current()
    {
        MainThread(); if (disposed) return false;
        if (!identityCheck!()) { Dispose(); return false; }
        return true;
    }
    public bool Tick(double seconds)
    {
        if (!Current()) return false;
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > 60) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!moving && idle!.Advance(Math.Min(seconds, .25))) mesh!.SetOriginalFrame(idle.Frame);
        return true;
    }
    public bool SetEngineTransform(Transform3D value) { if (!Current()) return false; ValidateEngineTransform(value); Root.Transform = value; return true; }
    public bool SetVisible(bool value) { if (!Current()) return false; Root.Visible = value; return true; }
    public bool SetFocused(bool value) { if (!Current()) return false; idle!.SetFocused(value); return true; }
    public bool SetPlaying(bool value) { if (!Current()) return false; idle!.SetPlaying(value); return true; }
    public bool SeekIdle(double frame) { if (!Current()) return false; idle!.Seek(frame); moving = false; mesh!.SetOriginalFrame(frame); return true; }
    public bool ResumeIdle() { if (!Current()) return false; moving = false; mesh!.SetOriginalFrame(idle!.Frame); return true; }
    public bool SetMovement(string key, double frame, double seconds)
    {
        if (!Current()) return false;
        if (key is not ("walk" or "run") || !double.IsFinite(frame) || frame < 0 || !double.IsFinite(seconds) || seconds < 0 || seconds > 60 || frame > resources!.Creature.AdditionalAnimations![key].EndFrame)
            throw new ArgumentException("Explicit owned walk/run frame and finite transition time required.");
        mesh!.SetMovementFrame(key, frame, seconds); moving = true; return true;
    }
    public bool Verify(bool gpu) { if (!Current()) return false; mesh!.VerifyBoundary(gpu); return true; }
    public string? Fingerprint(bool pose = false, bool gpu = false)
    {
        if (!Current()) return null;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(System.Text.Encoding.ASCII.GetBytes(mesh!.GeometryFingerprint(pose)));
        hash.AppendData(BitConverter.GetBytes(Root.GetInstanceId()));
        foreach (MeshInstance3D node in mesh.Root.GetChildren())
            for (int surface = 0; surface < node.Mesh.GetSurfaceCount(); surface++)
            {
                var material = (ShaderMaterial)node.Mesh.SurfaceGetMaterial(surface);
                hash.AppendData(BitConverter.GetBytes(material.GetInstanceId())); hash.AppendData(BitConverter.GetBytes(material.Shader.GetInstanceId()));
                if (material.GetShaderParameter("albedo_texture").AsGodotObject() is ImageTexture texture)
                {
                    hash.AppendData(BitConverter.GetBytes(texture.GetInstanceId()));
                    if (gpu) { using var image = texture.GetImage(); hash.AppendData(image.GetData()); }
                }
            }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    public void Dispose()
    {
        MainThread(); if (disposed) return; disposed = true; IsPublished = false;
        mesh?.Dispose(); mesh = null; idle = null; resources = null; identityCheck = null;
        ulong id = Root.GetInstanceId(); Root.Free(); Disposals++;
        if (GodotObject.InstanceFromId(id) is not null) throw new InvalidOperationException("Creature root survived disposal.");
    }
}
