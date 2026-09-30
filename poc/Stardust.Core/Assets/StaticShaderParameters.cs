using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record StaticShaderParameters(IReadOnlyDictionary<uint, IReadOnlyList<float>> Materials,
    IReadOnlyDictionary<uint, byte> CoordinateSets);

/// <summary>MATS and TCSS scalar slice. Material component semantics remain unassigned; preserves all 17 floats.</summary>
public static class StaticShaderParameterDecoder
{
    public static StaticShaderParameters Decode(StaticShaderReferenceDescription shader)
    {
        IReadOnlyList<IffNode> children = shader.Metadata.Children[0].Children;
        Dictionary<uint, IReadOnlyList<float>> materials = new(); Dictionary<uint, byte> coordinates = new();
        IffNode[] mats = children.Where(node => node.IsForm("MATS")).ToArray();
        IffNode[] sets = children.Where(node => node.IsForm("TCSS")).ToArray();
        StaticEffectDecoder.Require(mats.Length <= 1 && sets.Length <= 1, "Duplicate shader parameter form.");
        if (mats.Length == 1)
        {
            StaticEffectDecoder.Require(mats[0].Children.Count == 1 && mats[0].Children[0].IsForm("0000"), "Unsupported material map.");
            IReadOnlyList<IffNode> pairs = mats[0].Children[0].Children;
            StaticEffectDecoder.Require(pairs.Count % 2 == 0 && pairs.Count <= 128, "Invalid material map cardinality.");
            for (int i = 0; i < pairs.Count; i += 2)
            {
                StaticEffectDecoder.Require(pairs[i].Tag == "TAG " && pairs[i].Payload.Length == 4 && pairs[i + 1].Tag == "MATL" && pairs[i + 1].Payload.Length == 68, "Invalid material tag/scalars.");
                uint tag = BinaryPrimitives.ReadUInt32LittleEndian(pairs[i].Payload.Span); float[] values = new float[17];
                for (int field = 0; field < 17; field++)
                {
                    values[field] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(pairs[i + 1].Payload.Span[(field * 4)..]));
                    StaticEffectDecoder.Require(float.IsFinite(values[field]), "Nonfinite material component.");
                }
                StaticEffectDecoder.Require(materials.TryAdd(tag, Array.AsReadOnly(values)), "Duplicate material tag.");
            }
        }
        if (sets.Length == 1)
        {
            StaticEffectDecoder.Require(sets[0].Children.Count == 1 && sets[0].Children[0].Tag == "0000", "Unsupported coordinate map.");
            ReadOnlySpan<byte> data = sets[0].Children[0].Payload.Span;
            StaticEffectDecoder.Require(data.Length % 5 == 0 && data.Length <= 320, "Invalid coordinate map extent.");
            for (int i = 0; i < data.Length; i += 5)
                StaticEffectDecoder.Require(data[i + 4] < 16 && coordinates.TryAdd(BinaryPrimitives.ReadUInt32LittleEndian(data[i..]), data[i + 4]), "Invalid or duplicate coordinate set.");
        }
        return new(new ReadOnlyDictionary<uint,IReadOnlyList<float>>(materials), new ReadOnlyDictionary<uint,byte>(coordinates));
    }
}
