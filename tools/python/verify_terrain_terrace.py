"""Independent exact-rational oracle with declared binary64/F32 rounding points.

Checks target recovered structure under a modern policy, never original x87 parity.
"""
import argparse
import json
import struct
from fractions import Fraction
from pathlib import Path


def f32(bits):
    return struct.unpack('<f', struct.pack('<I', bits))[0]


def bits(value):
    return struct.unpack('<I', struct.pack('<f', value))[0]


def wide(value):
    return Fraction.from_float(float(value))


def narrow(value):
    return Fraction.from_float(f32(bits(float(value))))


def verify(receipt):
    assert receipt['Passed'] and receipt['Checks'] >= 700
    negative_multiple = False
    for sample in receipt['Samples']:
        original = Fraction.from_float(f32(sample['OriginalHeightBits']))
        amount = Fraction.from_float(f32(sample['InfluenceBits']))
        step = Fraction.from_float(f32(sample['HeightBits']))
        fraction = Fraction.from_float(f32(sample['FractionBits']))
        if amount <= 0:
            assert not sample['Applied']
            assert sample['ResultHeightBits'] == sample['OriginalHeightBits']
            continue
        # Exact signed remainder using truncation-toward-zero quotient, independently
        # of host fmod/% and the C# evaluator.
        remainder = original - int(original / step) * step
        subtrahend = wide(step + remainder) if original < 0 else remainder
        low = wide(original - subtrahend)
        low_store = narrow(low)
        mid = narrow(wide(low + wide(step * fraction)))
        high = narrow(wide(low + step))
        ramp = original > mid
        if ramp:
            ratio = wide(wide(original - mid) / wide(high - mid))
            next_height = wide(low_store + wide(wide(high - low_store) * ratio))
        else:
            next_height = low
        expected = bits(float(wide(original + wide(wide(next_height - original) * amount))))
        assert sample['Applied'] and sample['Ramp'] == ramp
        assert sample['LowHeightBits'] == bits(float(low_store))
        assert sample['MidHeightBits'] == bits(float(mid))
        assert sample['HighHeightBits'] == bits(float(high))
        assert sample['ResultHeightBits'] == expected, sample
        negative_multiple |= original < 0 and remainder == 0 and low == original - step
    assert negative_multiple
    original = receipt.get('Original')
    if original:
        assert original['TerrainSha256'] == '6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
        assert [(r['LayerIndex'], r['EnabledBits'], r['FractionBits'], r['HeightBits']) for r in original['Rules']] == [
            (8, 0, 0x3e5d2f1b, 0x41f00000), (49, 1, 0x3e70a3d7, 0x41200000)]
    return {'passed': True, 'samples': len(receipt['Samples']), 'original_records': 2 if original else 0,
            'original_numeric_parity': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('receipt', type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(json.loads(args.receipt.read_text()))))
