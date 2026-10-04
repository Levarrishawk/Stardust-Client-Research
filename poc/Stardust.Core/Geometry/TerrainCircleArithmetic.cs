using System.Numerics;

namespace Stardust.Core.Geometry;

// Finite, wide-exponent nearest-even arithmetic for the recovered circle graph only.
internal static class TerrainCircleArithmetic
{
    internal readonly record struct Number(BigInteger Mantissa, int Exponent, bool NegativeZero = false)
    {
        internal bool Negative => Mantissa.Sign < 0 || (Mantissa.IsZero && NegativeZero);
        internal static Number FromBits(uint bits)
        {
            int encoded = (int)((bits >> 23) & 255);
            if (encoded == 255) throw new InvalidDataException("Nonfinite circle arithmetic operand.");
            uint fraction = bits & 0x7fffff;
            BigInteger m = encoded == 0 ? fraction : fraction | 0x800000;
            bool negative = (bits & 0x80000000) != 0;
            return new(negative ? -m : m, encoded == 0 ? -149 : encoded - 150, m.IsZero && negative);
        }
        internal Number Negate() => new(-Mantissa, Exponent, Mantissa.IsZero && !NegativeZero);
        internal static Number Add(Number a, Number b, int precision)
        {
            int e = Math.Min(a.Exponent, b.Exponent);
            BigInteger m = (a.Mantissa << (a.Exponent-e)) + (b.Mantissa << (b.Exponent-e));
            return new Number(m, e, m.IsZero && a.Mantissa.IsZero && b.Mantissa.IsZero && a.Negative && b.Negative).Round(precision);
        }
        internal static Number Subtract(Number a, Number b, int precision) => Add(a, b.Negate(), precision);
        internal static Number Multiply(Number a, Number b, int precision) =>
            new Number(a.Mantissa*b.Mantissa, a.Exponent+b.Exponent,
                (a.Mantissa.IsZero || b.Mantissa.IsZero) && (a.Negative ^ b.Negative)).Round(precision);
        internal static int Compare(Number a, Number b)
        {
            int e = Math.Min(a.Exponent, b.Exponent);
            return (a.Mantissa << (a.Exponent-e)).CompareTo(b.Mantissa << (b.Exponent-e));
        }
        private static BigInteger ShiftRound(BigInteger m, int shift)
        {
            if (shift <= 0) return m << -shift;
            BigInteger q = m >> shift, remainder = m - (q << shift), half = BigInteger.One << (shift-1);
            return remainder > half || (remainder == half && !q.IsEven) ? q+1 : q;
        }
        internal Number Round(int precision)
        {
            if (Mantissa.IsZero) return this;
            BigInteger m = BigInteger.Abs(Mantissa);
            int shift = checked((int)m.GetBitLength()) - precision;
            if (shift <= 0) return this;
            BigInteger q = ShiftRound(m, shift);
            return new(Mantissa.Sign < 0 ? -q : q, Exponent+shift);
        }
        internal static Number Divide(Number a, Number b, int precision)
        {
            if (b.Mantissa.IsZero) throw new InvalidDataException("Unresolved circle division exception.");
            bool negative = a.Negative ^ b.Negative;
            if (a.Mantissa.IsZero) return new(BigInteger.Zero, 0, negative);
            BigInteger n = BigInteger.Abs(a.Mantissa), d = BigInteger.Abs(b.Mantissa);
            int floor = checked((int)n.GetBitLength() - (int)d.GetBitLength());
            bool below = floor >= 0 ? n < (d << floor) : (n << -floor) < d;
            if (below) floor--;
            int exponent = a.Exponent-b.Exponent, quantum = exponent+floor-precision+1;
            int shift = exponent-quantum;
            if (shift >= 0) n <<= shift; else d <<= -shift;
            BigInteger q = BigInteger.DivRem(n, d, out BigInteger remainder);
            int comparison = (remainder << 1).CompareTo(d);
            if (comparison > 0 || (comparison == 0 && !q.IsEven)) q++;
            return new(negative ? -q : q, quantum);
        }
        internal static Number SquareRoot(Number a, int precision)
        {
            if (a.Mantissa.Sign < 0) throw new InvalidDataException("Unresolved circle square-root exception.");
            if (a.Mantissa.IsZero) return a;
            int floor = a.Exponent+checked((int)a.Mantissa.GetBitLength())-1;
            int rootFloor = floor >= 0 ? floor/2 : (floor-1)/2;
            int quantum = rootFloor-precision+1, shift = a.Exponent-2*quantum;
            BigInteger n = a.Mantissa, d = BigInteger.One;
            if (shift >= 0) n <<= shift; else d <<= -shift;
            BigInteger q = IntegerSquareRoot(n/d);
            BigInteger midpoint = (q << 1)+1;
            int comparison = (n << 2).CompareTo(d*midpoint*midpoint);
            if (comparison > 0 || (comparison == 0 && !q.IsEven)) q++;
            return new(q, quantum);
        }
        private static BigInteger IntegerSquareRoot(BigInteger n)
        {
            if (n.IsZero) return n;
            BigInteger x = BigInteger.One << checked(((int)n.GetBitLength()+1)/2);
            for (int i=0; i<32; i++)
            {
                BigInteger y = (x+n/x) >> 1;
                if (y >= x) return x;
                x = y;
            }
            throw new InvalidDataException("Circle square-root work invariant exceeded.");
        }
        internal uint ToFloatBits()
        {
            uint sign = Negative ? 0x80000000u : 0;
            if (Mantissa.IsZero) return sign;
            BigInteger m = BigInteger.Abs(Mantissa);
            int floor = Exponent+checked((int)m.GetBitLength())-1;
            int quantum = Math.Max(-149, floor-23);
            BigInteger q = ShiftRound(m, quantum-Exponent);
            if (q.IsZero) return sign;
            int length = checked((int)q.GetBitLength()), roundedFloor = quantum+length-1;
            if (roundedFloor > 127) return sign | 0x7f800000;
            if (roundedFloor < -126) return sign | (uint)q;
            BigInteger normalized = length > 24 ? q >> (length-24) : q << (24-length);
            return sign | ((uint)(roundedFloor+127) << 23) | ((uint)normalized & 0x7fffff);
        }
    }
}
