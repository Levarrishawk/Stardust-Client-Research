"""Independent Fraction/Decimal oracle for explicit modern second-curve receipts.

This verifies recovered decisions/stores and modern approximation accuracy; it
does not establish the original x87 logarithm/power/control-word results.
"""
import argparse
import json
import struct
from decimal import Decimal, localcontext
from fractions import Fraction
from pathlib import Path


def f32(bits):
    return struct.unpack('<f', struct.pack('<I', bits))[0]


def bits32(value):
    return struct.unpack('<I', struct.pack('<f', value))[0]


def f64(bits):
    return struct.unpack('<d', struct.pack('<Q', bits))[0]


def dyadic(magnitude, exponent):
    return Fraction(magnitude) * Fraction(2) ** exponent


def verify(receipt):
    assert receipt['Passed'] and receipt['Checks'] >= 200
    low = Fraction.from_float(f64(0x3F50624DD2F1A9FC))
    high = Fraction.from_float(f64(0x3FEFF7CED916872B))
    branch_witnesses = {}
    maximum_error = 0.0
    samples = receipt['Samples']
    with localcontext() as context:
        context.prec = 90
        # Decimal transcendental implementation independent of .NET Math.Log/Pow.
        denominator = Decimal.from_float(f32(bits32(float(Decimal('0.5').ln()))))
        for sample in samples:
            retained = dyadic(sample['InputMagnitude'], sample['InputExponent'])
            if sample['InputNegative']:
                retained = -retained
            branch = 'Zero' if retained < low else 'One' if retained > high else 'Lower' if retained < Fraction(1, 2) else 'Upper'
            assert sample['Branch'] == branch, sample['Name']
            projected = -0.0 if sample['InputNegative'] and not sample['InputMagnitude'] else float(retained)
            assert bits32(projected) == sample['InputF32'], sample['Name']
            if sample['Name'].startswith('threshold'):
                branch_witnesses.setdefault(sample['InputF32'], set()).add(branch)
            if branch in ('Zero', 'One'):
                expected = 0.0 if branch == 'Zero' else 1.0
                assert sample['ExponentF32Bits'] is None
            else:
                parameter = Decimal.from_float(f32(sample['ParameterBits']))
                # Recovered mandatory F32 exponent spill, rather than a host-wide exponent.
                exponent_bits = bits32(float((Decimal(1) - parameter).ln() / denominator))
                assert sample['ExponentF32Bits'] == exponent_bits, sample['Name']
                exponent = Decimal.from_float(f32(exponent_bits))
                base_snapshot = Decimal.from_float(f32(sample['InputF32']))
                base = 2 * (base_snapshot if branch == 'Lower' else 1 - base_snapshot)
                power = context.power(base, exponent)
                expected = float(power / 2 if branch == 'Lower' else 1 - power / 2)
            actual = f64(sample['OutputBinary64Bits'])
            error = abs(actual - expected)
            maximum_error = max(maximum_error, error)
            assert error <= 4e-15, (sample['Name'], actual, expected)
            assert Fraction.from_float(actual) == dyadic(sample['OutputMagnitude'], sample['OutputExponent'])
            assert bits32(actual) == sample['OutputF32']
    # Each low/half/high center and neighbors share a projection yet require two decisions.
    assert sum(len(branches) > 1 for branches in branch_witnesses.values()) == 3
    assert all(any(s['Branch'] == branch for s in samples) for branch in ('Zero', 'One', 'Lower', 'Upper'))
    print(json.dumps(dict(Passed=True,IndependentSamples=len(samples),ThresholdProjectionWitnesses=3,
                          OriginalFamilies=receipt['OriginalFamilies'],MaximumAbsoluteError=maximum_error,
                          OriginalNumericParity=False)))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--receipt', type=Path, required=True)
    arguments = parser.parse_args()
    verify(json.loads(arguments.receipt.read_text(encoding='utf-8-sig')))
