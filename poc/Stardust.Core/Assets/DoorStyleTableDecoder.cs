using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record DoorStyle(string Name, string FrameAppearance, string PrimaryAppearance, string SecondaryAppearance, int FlipSecondaryRaw,
    Vector3 Movement, float OpenSeconds, float CloseSeconds, float Springiness, float Smoothness, float TriggerRadius, int ForceFieldRaw,
    string OpenBeginEffect, string OpenEndEffect, string CloseBeginEffect, string CloseEndEffect)
{
    public bool IsForceField => ForceFieldRaw != 0;
    public string Effect(DoorTransitionEvent transition) => transition switch { DoorTransitionEvent.OpenBegin => OpenBeginEffect, DoorTransitionEvent.OpenEnd => OpenEndEffect, DoorTransitionEvent.CloseBegin => CloseBeginEffect, DoorTransitionEvent.CloseEnd => CloseEndEffect, _ => "" };
}
public sealed record DoorStyleSelection(DoorStyle Style, bool UsedFallback);
public sealed class DoorStyleTable
{
    public IReadOnlyList<DoorStyle> Rows { get; }
    private readonly Dictionary<string, DoorStyle> names;
    internal DoorStyleTable(List<DoorStyle> rows) { Rows = rows.AsReadOnly(); names = rows.ToDictionary(r => r.Name, StringComparer.Ordinal); }
    public DoorStyleSelection Resolve(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0) throw new ArgumentException("Unnamed portals use a separate no-style path.", nameof(name));
        return names.TryGetValue(name, out DoorStyle? style) ? new(style, false) : new(Rows[0], true);
    }
}

/// <summary>Owned narrow DTII/0001 door-style schema. Original row data remains caller-provided.</summary>
public static class DoorStyleTableDecoder
{
    private static readonly string[] Columns = { "doorStyleName", "frameAppearance", "doorAppearance", "doorAppearance2", "doorFlip2", "doorMoveX", "doorMoveY", "doorMoveZ", "openTime", "closeTime", "springiness", "smoothness", "triggerRadius", "isForceField", "openBeginEffect", "openEndEffect", "closeBeginEffect", "closeEndEffect" };
    private static readonly string[] Types = { "s", "s", "s", "s", "b", "f", "f", "f", "f", "f", "f", "f", "f", "b", "s", "s", "s", "s" };
    public static DoorStyleTable Decode(ReadOnlySpan<byte> source)
    {
        IffNode root = IffDocument.Parse(source, new(MaximumBytes: 1_048_576)).Root;
        Require(root.IsForm("DTII") && root.Children.Count == 1 && root.Children[0].IsForm("0001"), "Unsupported door table envelope.");
        var parts = root.Children[0].Children;
        Require(parts.Count == 3 && parts[0].Tag == "COLS" && parts[1].Tag == "TYPE" && parts[2].Tag == "ROWS", "Invalid door table chunks.");
        ReadOnlySpan<byte> cols = parts[0].Payload.Span; int cursor = 0;
        Require(Integer(cols, ref cursor) == 18, "Door table requires 18 columns.");
        foreach (string column in Columns) Require(String(cols, ref cursor) == column, "Door table column mismatch.");
        Require(cursor == cols.Length, "Trailing door table columns.");
        ReadOnlySpan<byte> types = parts[1].Payload.Span; cursor = 0;
        foreach (string type in Types) Require(String(types, ref cursor) == type, "Door table type mismatch.");
        Require(cursor == types.Length, "Trailing door table types.");
        ReadOnlySpan<byte> data = parts[2].Payload.Span; cursor = 0; int count = Integer(data, ref cursor);
        Require(count is > 0 and <= 4096, "Door table row budget.");
        List<DoorStyle> rows = new(); HashSet<string> names = new(StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            string name = String(data, ref cursor), frame = String(data, ref cursor), primary = String(data, ref cursor), secondary = String(data, ref cursor);
            Require(name.Length > 0 && names.Add(name), "Empty or duplicate door style name.");
            int flip = Integer(data, ref cursor); Vector3 movement = new(Float(data, ref cursor), Float(data, ref cursor), Float(data, ref cursor));
            float open = Float(data, ref cursor), close = Float(data, ref cursor), spring = Float(data, ref cursor), smooth = Float(data, ref cursor), radius = Float(data, ref cursor);
            int force = Integer(data, ref cursor);
            rows.Add(new(name, frame, primary, secondary, flip, movement, open, close, spring, smooth, radius, force,
                String(data, ref cursor), String(data, ref cursor), String(data, ref cursor), String(data, ref cursor)));
        }
        Require(cursor == data.Length, "Trailing door table row bytes.");
        return new(rows);
    }
    private static int Integer(ReadOnlySpan<byte> bytes, ref int cursor)
    {
        Require(cursor <= bytes.Length - 4, "Truncated door table scalar."); int value = BinaryPrimitives.ReadInt32LittleEndian(bytes[cursor..]); cursor += 4; return value;
    }
    private static float Float(ReadOnlySpan<byte> bytes, ref int cursor)
    { float value = BitConverter.Int32BitsToSingle(Integer(bytes, ref cursor)); Require(float.IsFinite(value), "Nonfinite door table scalar."); return value; }
    private static string String(ReadOnlySpan<byte> bytes, ref int cursor)
    {
        int length = bytes[cursor..].IndexOf((byte)0); Require(length is >= 0 and <= 1024, "Invalid door table string length.");
        var value = bytes.Slice(cursor, length); foreach (byte b in value) Require(b is >= 32 and <= 126, "Invalid door table ASCII.");
        cursor += length + 1; return Encoding.ASCII.GetString(value);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
