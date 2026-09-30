using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record IffReadLimits(int MaximumBytes = 16 * 1024 * 1024,
    int MaximumNodes = 100_000, int MaximumDepth = 32);

public sealed class IffNode
{
    internal IffNode(string tag, string? formType, int offset, ReadOnlyMemory<byte> payload, IffNode[] children)
    {
        Tag = tag; FormType = formType; Offset = offset; Payload = payload;
        Children = Array.AsReadOnly(children);
    }
    public string Tag { get; }
    public string? FormType { get; }
    public int Offset { get; }
    public ReadOnlyMemory<byte> Payload { get; }
    public IReadOnlyList<IffNode> Children { get; }
    public bool IsForm(string type) => Tag == "FORM" && FormType == type;
}

/// <summary>Owned bounded IFF tree. Big-endian sizes; SWG records advance without RIFF-style padding.</summary>
public sealed class IffDocument
{
    private IffDocument(IffNode root) => Root = root;
    public IffNode Root { get; }

    public static IffDocument Parse(ReadOnlySpan<byte> source, IffReadLimits? limits = null)
    {
        limits ??= new();
        if (limits.MaximumBytes <= 0 || limits.MaximumNodes <= 0 || limits.MaximumDepth is < 1 or > 128)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (source.Length > limits.MaximumBytes) throw new InvalidDataException("IFF exceeds the byte limit.");
        byte[] owned = source.ToArray();
        int nodes = 0;
        IffNode[] roots = ParseChildren(owned, 0, owned.Length, 0, limits, ref nodes);
        if (roots.Length != 1 || roots[0].Tag != "FORM") throw new InvalidDataException("IFF requires one root FORM.");
        return new(roots[0]);
    }

    private static IffNode[] ParseChildren(byte[] source, int start, int end, int depth, IffReadLimits limits, ref int count)
    {
        if (depth > limits.MaximumDepth) throw new InvalidDataException("IFF nesting exceeds the depth limit.");
        List<IffNode> children = new();
        int cursor = start;
        while (cursor < end)
        {
            if (end - cursor < 8) throw new InvalidDataException("Truncated IFF header.");
            if (++count > limits.MaximumNodes) throw new InvalidDataException("IFF exceeds the node limit.");
            string tag = ReadTag(source.AsSpan(cursor, 4));
            uint size = BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(cursor + 4, 4));
            int payload = cursor + 8;
            if (size > (uint)(end - payload)) throw new InvalidDataException("IFF child exceeds its parent extent.");
            int stop = payload + (int)size;
            string? form = null;
            IffNode[] nested = Array.Empty<IffNode>();
            if (tag == "FORM")
            {
                if (size < 4) throw new InvalidDataException("IFF FORM lacks its type tag.");
                form = ReadTag(source.AsSpan(payload, 4));
                payload += 4;
                nested = ParseChildren(source, payload, stop, depth + 1, limits, ref count);
            }
            children.Add(new(tag, form, cursor, source.AsMemory(payload, stop - payload), nested));
            cursor = stop;
        }
        return children.ToArray();
    }

    private static string ReadTag(ReadOnlySpan<byte> tag)
    {
        foreach (byte value in tag)
            if (value is < 0x20 or > 0x7E) throw new InvalidDataException("IFF tag is not printable ASCII.");
        return Encoding.ASCII.GetString(tag);
    }
}
