using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record DetailAppearanceLevel(uint Id, float LowerDistance, float UpperDistance, string? ChildPath);
public sealed record DetailVisibilityGeometry(IReadOnlyList<Vector3> Vertices, IReadOnlyList<uint> Indices);
public sealed record DetailAppearancePolicy(uint Flags, DetailVisibilityGeometry? Geometry);
public sealed record DetailAppearanceDescription(string Version, byte? Pivot, IReadOnlyList<DetailAppearanceLevel> Levels,
    IReadOnlyDictionary<string, DetailAppearancePolicy> Policies, IReadOnlyList<string> Diagnostics, IffNode Metadata,AppearanceHardpointTable? Hardpoints=null);

/// <summary>Owned deployed DTLA 0005-0007 structure. Authored bands are preserved; no runtime selection policy is inferred.</summary>
public static class DetailAppearanceDecoder
{
    public static DetailAppearanceDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root = IffDocument.Parse(source).Root;
        Require(root.IsForm("DTLA") && root.Children.Count == 1, "Invalid detail appearance envelope.");
        IffNode version = root.Children[0];
        Require(version.FormType is "0005" or "0006" or "0007", "Unsupported detail appearance version.");
        IReadOnlyList<IffNode> parts = version.Children;
        int cursor = 0;
        Require(parts.Count >= 5 && parts[cursor].IsForm("APPR") && parts[cursor].Children.Count == 1 &&
            parts[cursor].Children[0].IsForm("0003"), "Unsupported detail appearance base.");
        cursor++;
        byte? pivot = null;
        if (version.FormType != "0005")
        {
            Require(parts[cursor].Tag == "PIVT" && parts[cursor].Payload.Length == 1 && parts[cursor].Payload.Span[0] <= 1,
                "Invalid detail pivot.");
            pivot = parts[cursor++].Payload.Span[0];
        }
        Require(cursor < parts.Count && parts[cursor].Tag == "INFO", "Missing detail bands.");
        ReadOnlySpan<byte> info = parts[cursor++].Payload.Span;
        Require(info.Length > 0 && info.Length % 12 == 0 && info.Length / 12 <= 256, "Invalid detail band count.");
        List<DetailAppearanceLevel> levels = new();
        Dictionary<uint,int> ids = new();
        List<string> diagnostics = new();
        for (int offset = 0; offset < info.Length; offset += 12)
        {
            uint id = U32(info[offset..]); float lower = F32(info[(offset+4)..]), upper = F32(info[(offset+8)..]);
            Require(float.IsFinite(lower) && float.IsFinite(upper) && ids.TryAdd(id, levels.Count), "Invalid or duplicate detail band.");
            if (lower > upper) diagnostics.Add($"ReversedInterval:{id}");
            levels.Add(new(id,lower,upper,null));
        }
        Require(cursor < parts.Count && parts[cursor].IsForm("DATA"), "Missing detail children.");
        IffNode children = parts[cursor++];
        Require(children.Children.Count <= 512, "Detail child budget exceeded.");
        foreach (IffNode child in children.Children)
        {
            Require(child.Tag == "CHLD" && child.Payload.Length >= 6, "Invalid detail child.");
            ReadOnlySpan<byte> bytes = child.Payload.Span;
            uint id = U32(bytes);
            Require(ids.TryGetValue(id,out int index), "Detail child has no band.");
            string path = StaticEffectDecoder.ReadNamePrefix(bytes[4..],allowTail:false);
            if (!path.StartsWith("appearance/",StringComparison.Ordinal)) path = "appearance/" + path;
            if (levels[index].ChildPath is not null) diagnostics.Add($"DuplicateChild:{id}");
            // Explicit deterministic duplicate policy: retain the last authored path and report the irregularity.
            levels[index] = levels[index] with { ChildPath = path };
        }
        foreach (DetailAppearanceLevel level in levels)
            if (level.ChildPath is null) diagnostics.Add($"MissingChild:{level.Id}");
        Dictionary<string,DetailAppearancePolicy> policies = new(StringComparer.Ordinal);
        string[] expected = version.FormType == "0007" ? new[]{"RADR","TEST","WRIT"} : new[]{"TEST","WRIT"};
        foreach (string tag in expected)
        {
            Require(cursor < parts.Count && parts[cursor].IsForm(tag), "Missing or unordered detail policy.");
            policies.Add(tag, ReadPolicy(parts[cursor++]));
        }
        Require(cursor == parts.Count, "Unknown detail appearance child.");
        return new(version.FormType!,pivot,levels.AsReadOnly(),
            new System.Collections.ObjectModel.ReadOnlyDictionary<string,DetailAppearancePolicy>(policies),diagnostics.AsReadOnly(),root,AppearanceHardpointDecoder.DecodeBase(parts[0]));
    }

    internal static DetailAppearancePolicy ReadPolicy(IffNode policy)
    {
        Require(policy.Children.Count is 1 or 2 && policy.Children[0].Tag == "INFO" && policy.Children[0].Payload.Length == 4,
            "Invalid detail policy envelope.");
        uint flags = U32(policy.Children[0].Payload.Span);
        DetailVisibilityGeometry? geometry = null;
        if (policy.Children.Count == 2)
        {
            IffNode idtl = policy.Children[1];
            Require(idtl.IsForm("IDTL") && idtl.Children.Count == 1 && idtl.Children[0].IsForm("0000"), "Unsupported detail visibility geometry.");
            IReadOnlyList<IffNode> chunks = idtl.Children[0].Children;
            Require(chunks.Count == 2 && chunks[0].Tag == "VERT" && chunks[1].Tag == "INDX", "Invalid visibility geometry chunks.");
            ReadOnlySpan<byte> vertices = chunks[0].Payload.Span, indices = chunks[1].Payload.Span;
            Require(vertices.Length % 12 == 0 && indices.Length % 4 == 0 && vertices.Length / 12 <= 100_000 && indices.Length / 4 <= 300_000,
                "Invalid visibility geometry widths/budgets.");
            Vector3[] positions = new Vector3[vertices.Length / 12]; uint[] values = new uint[indices.Length / 4];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new(F32(vertices[(i*12)..]),F32(vertices[(i*12+4)..]),F32(vertices[(i*12+8)..]));
                Require(float.IsFinite(positions[i].X) && float.IsFinite(positions[i].Y) && float.IsFinite(positions[i].Z), "Nonfinite visibility vertex.");
            }
            for (int i = 0; i < values.Length; i++) { values[i] = U32(indices[(i*4)..]); Require(values[i] < positions.Length, "Visibility index out of range."); }
            geometry = new(Array.AsReadOnly(positions),Array.AsReadOnly(values));
        }
        return new(flags,geometry);
    }
    private static uint U32(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static float F32(ReadOnlySpan<byte> bytes) => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
    private static void Require(bool valid,string message) => StaticEffectDecoder.Require(valid,message);
}
