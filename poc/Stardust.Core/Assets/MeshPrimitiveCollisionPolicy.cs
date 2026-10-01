namespace Stardust.Core.Assets;

/// <summary>Recovered static-shader predicates for an explicitly selected implementation.
/// Does not select graphics capabilities or establish actor collision semantics.</summary>
public readonly record struct MeshPrimitiveCollisionFlags(bool OpaqueFirstPass, bool SelectorOne)
{
    public bool RequiresCollisionData => OpaqueFirstPass || SelectorOne;
    public bool AcceptsSelector(uint selector) => selector switch
    {
        0 => OpaqueFirstPass,
        1 => SelectorOne,
        _ => false
    };
}

public static class MeshPrimitiveCollisionPolicy
{
    public static MeshPrimitiveCollisionFlags FromInvariantCandidates(StaticEffectDescription effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if(effect.Implementations.Count>32)throw new InvalidDataException("Collision candidate budget.");
        if(effect.Implementations.Count==0)return default;
        var flags=FromSelectedImplementation(effect.Implementations[0]);
        foreach(var candidate in effect.Implementations.Skip(1))
            if(FromSelectedImplementation(candidate)!=flags)throw new InvalidDataException("Collision predicates depend on implementation selection.");
        return flags;
    }
    public static MeshPrimitiveCollisionFlags FromSelectedImplementation(StaticEffectImplementation? implementation)
    {
        if (implementation is null) return default;
        if (implementation.Version is not ("0004" or "0005" or "0006" or "0007"))
            throw new InvalidDataException("Unsupported collision implementation version.");
        bool opaque = false;
        if (implementation.Passes.Count != 0)
        {
            var fields = implementation.Passes[0].Fields;
            if (!fields.TryGetValue(0x20, out uint blend) || !fields.TryGetValue(0x30, out uint alphaTest) || blend > 1 || alphaTest > 1)
                throw new InvalidDataException("Invalid first-pass collision predicate fields.");
            opaque = blend == 0 && alphaTest == 0;
        }
        // IMPL/0004 derives both runtime bytes from the first-pass predicate.
        // Later versions load DATA byte 6 into the selector-one getter at +0x15.
        bool selectorOne = implementation.Version == "0004" ? opaque : implementation.Flag6 switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Missing or invalid selector-one flag.")
        };
        return new(opaque, selectorOne);
    }
}
