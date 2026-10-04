using System.Collections.ObjectModel;
using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Geometry;

/// <summary>Owned immediate noise element under explicit conditional nearest-even controls.</summary>
public sealed class TerrainSeededNoiseKernel : IDisposable
{
    public const int TableEntries = 514;
    public const int TableBytes = (514 * 4 * 4) + (322 * 4);
    public const int BuildWork = 20000;
    public const int MaximumQueries = 4096;
    public readonly record struct Coordinate(uint XBits, uint ZBits);
    public readonly record struct Sample(ulong Magnitude, bool Negative, int Exponent, uint? InspectionBits);
    public sealed record TableSnapshot(ReadOnlyCollection<uint> Permutation, ReadOnlyCollection<uint> Scalar,
        ReadOnlyCollection<uint> GradientPairs, int Draws, int Steps);
    private sealed record Tables(uint[] Permutation, uint[] Scalar, uint[] Gradients, int Draws, int Steps);
    private readonly int owner = Environment.CurrentManagedThreadId;
    private readonly int tablePrecision;
    private Tables? tables;
    private bool busy;
    public int TablePrecision => tablePrecision;
    private TerrainSeededNoiseKernel(Tables values, int precision) { tables = values; tablePrecision = precision; }
    private static Number I(long value) => new(new BigInteger(value), 0);
    private static void Precision(int value) { if(value is not (24 or 53 or 64)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Budget(int actual, int required, int maximum)
    { if(actual < required || actual > maximum) throw new ArgumentOutOfRangeException(nameof(actual), "Explicit bounded budget required."); }
    private static uint Store(Number value)
    { uint bits = value.ToFloatBits(); if((bits & 0x7f800000) == 0x7f800000) throw new InvalidDataException("Nonfinite normalization."); return bits; }
    private static BigInteger Truncate(Number value) => value.Exponent >= 0 ? value.Mantissa << value.Exponent : value.Mantissa / (BigInteger.One << -value.Exponent);
    private sealed class Generator
    {
        private int seed, last;
        private readonly int precision;
        private readonly int[] shuffle = new int[322];
        internal int Draws, Steps;
        internal Generator(int rawSeed, int p) { seed = unchecked(-rawSeed); precision = p; }
        private void Advance()
        { int q = seed / 127773; long next = 16807L * (seed - q * 127773L) - 2836L * q; if(next < 0) next += 2147483647; seed = checked((int)next); Steps++; }
        internal int Next()
        {
            if(seed <= 0 || last == 0)
            { seed = Math.Max(1, unchecked(-seed)); for(int j = 329; j >= 0; j--) { Advance(); if(j < 322) shuffle[j] = seed; } last = shuffle[0]; }
            Advance(); int index = checked((int)Truncate(Number.Divide(I(last), I(6669205), precision)));
            if(index is < 0 or >= 322) throw new InvalidDataException("Shuffle index outside owned table.");
            last = shuffle[index]; shuffle[index] = seed; Draws++; return last;
        }
    }
    public static TerrainSeededNoiseKernel Create(int signedSeed, int tablePrecision, int tableByteBudget, int workBudget, CancellationToken cancellation = default)
    {
        Precision(tablePrecision); Budget(tableByteBudget, TableBytes, 1048576); Budget(workBudget, BuildWork, 1048576);
        if(signedSeed == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(signedSeed), "Modulus seed is outside modern eligibility.");
        cancellation.ThrowIfCancellationRequested();
        var generator = new Generator(signedSeed, tablePrecision);
        var permutation = new uint[TableEntries]; var scalar = new uint[TableEntries]; var gradients = new uint[TableEntries * 2];
        for(int i = 0; i < 256; i++)
        {
            cancellation.ThrowIfCancellationRequested(); permutation[i] = (uint)i;
            scalar[i] = Store(Number.Multiply(I(generator.Next() % 512 - 256), Number.FromBits(0x3b800000), tablePrecision));
            uint xb = Store(Number.Multiply(I(generator.Next() % 512 - 256), Number.FromBits(0x3b800000), tablePrecision));
            uint zb = Store(Number.Multiply(I(generator.Next() % 512 - 256), Number.FromBits(0x3b800000), tablePrecision));
            Number x = Number.FromBits(xb), z = Number.FromBits(zb);
            Number sum = Number.Add(Number.Multiply(z, z, tablePrecision), Number.Multiply(x, x, tablePrecision), tablePrecision);
            if(sum.Mantissa.IsZero) throw new InvalidDataException("Zero gradient has no eligible normalization.");
            Number reciprocal = Number.Divide(I(1), Number.SquareRoot(sum, tablePrecision), tablePrecision);
            gradients[2*i] = Store(Number.Multiply(reciprocal, x, tablePrecision)); gradients[2*i+1] = Store(Number.Multiply(reciprocal, z, tablePrecision));
        }
        for(int i = 255; i >= 1; i--)
        { cancellation.ThrowIfCancellationRequested(); int j = generator.Next() % 256; (permutation[i], permutation[j]) = (permutation[j], permutation[i]); }
        for(int i = 0; i < 258; i++)
        { permutation[256+i] = permutation[i]; scalar[256+i] = scalar[i]; gradients[2*(256+i)] = gradients[2*i]; gradients[2*(256+i)+1] = gradients[2*i+1]; }
        cancellation.ThrowIfCancellationRequested();
        return new(new Tables(permutation, scalar, gradients, generator.Draws, generator.Steps), tablePrecision);
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId != owner) throw new InvalidOperationException("Kernel belongs to its creating thread.");
        if(tables is null) throw new ObjectDisposedException(nameof(TerrainSeededNoiseKernel));
        if(busy) throw new InvalidOperationException("Kernel operation already active."); busy = true;
    }
    public TableSnapshot CopyTables(int byteBudget, CancellationToken cancellation = default)
    {
        Enter(); try
        {
            Budget(byteBudget, 514 * 16, 1048576); cancellation.ThrowIfCancellationRequested(); var t = tables!;
            var p = Array.AsReadOnly((uint[])t.Permutation.Clone()); var s = Array.AsReadOnly((uint[])t.Scalar.Clone()); var g = Array.AsReadOnly((uint[])t.Gradients.Clone());
            cancellation.ThrowIfCancellationRequested(); return new(p, s, g, t.Draws, t.Steps);
        } finally { busy = false; }
    }
    public ReadOnlyCollection<Sample> Query(ReadOnlySpan<Coordinate> coordinates, int queryPrecision, int byteBudget, int workBudget,
        bool includeInspection = false, CancellationToken cancellation = default)
    {
        Enter(); try
        {
            Precision(queryPrecision);
            if(coordinates.Length > MaximumQueries) throw new ArgumentOutOfRangeException(nameof(coordinates));
            // Input copy (8 bytes) plus conservative retained result allowance (32 bytes).
            Budget(byteBudget, checked(coordinates.Length * 40), MaximumQueries * 40);
            Budget(workBudget, checked(coordinates.Length * 256), MaximumQueries * 256);
            cancellation.ThrowIfCancellationRequested(); var owned = coordinates.ToArray(); var output = new Sample[owned.Length];
            for(int i = 0; i < owned.Length; i++) { cancellation.ThrowIfCancellationRequested(); output[i] = Evaluate(owned[i], queryPrecision, includeInspection); }
            cancellation.ThrowIfCancellationRequested(); return Array.AsReadOnly(output);
        } finally { busy = false; }
    }
    private Sample Evaluate(Coordinate coordinate, int p, bool inspection)
    {
        Number Add(Number x, Number z) => Number.Add(x, z, p);
        Number Mul(Number x, Number z) => Number.Multiply(x, z, p);
        Number bx = Add(Number.FromBits(coordinate.XBits), I(4096)), bz = Add(Number.FromBits(coordinate.ZBits), I(4096));
        int Floor(Number value)
        {
            BigInteger integer = Truncate(value);
            if(integer < int.MinValue || integer > int.MaxValue) throw new InvalidDataException("Lattice conversion outside signed32 eligibility.");
            if(value.Mantissa.Sign < 0 && value.Exponent < 0 && value.Mantissa % (BigInteger.One << -value.Exponent) != 0) integer--;
            if(integer < int.MinValue) throw new InvalidDataException("Negative lattice floor outside signed32 eligibility."); return (int)integer;
        }
        int ix = Floor(bx), iz = Floor(bz);
        Number x = Number.FromBits(Store(Add(bx, I(-(long)ix)))), z = Add(bz, I(-(long)iz)), dx = Add(x, I(-1)), dz = Add(z, I(-1));
        int x0 = ix & 255, x1 = unchecked(ix+1) & 255, z0 = iz & 255, z1 = unchecked(iz+1) & 255; var t = tables!;
        int Corner(int xx, int zz) => (int)t.Permutation[(int)t.Permutation[xx]+zz];
        Number Dot(int index, Number xx, Number zz) => Add(Mul(zz, Number.FromBits(t.Gradients[2*index+1])), Mul(xx, Number.FromBits(t.Gradients[2*index])));
        Number Fade(Number v) => Mul(Mul(Add(I(3), Add(v,v).Negate()), v), v);
        Number Lerp(Number f, Number a, Number b) => Add(Mul(Add(b,a.Negate()), f), a);
        Number sx = Fade(x), sz = Fade(z);
        Number result = Lerp(sz, Lerp(sx,Dot(Corner(x0,z0),x,z),Dot(Corner(x1,z0),dx,z)), Lerp(sx,Dot(Corner(x0,z1),x,dz),Dot(Corner(x1,z1),dx,dz)));
        BigInteger magnitude = BigInteger.Abs(result.Mantissa); int exponent = result.Exponent;
        if(magnitude.IsZero) exponent = 0; else while(magnitude.IsEven) { magnitude >>= 1; exponent++; }
        if(magnitude > ulong.MaxValue) throw new InvalidDataException("Retained significand exceeds unsigned64 result.");
        return new((ulong)magnitude, result.Negative, exponent, inspection ? Store(result) : null);
    }
    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId != owner) throw new InvalidOperationException("Kernel belongs to its creating thread.");
        if(busy) throw new InvalidOperationException("Cannot dispose active operation."); tables = null;
    }
}
