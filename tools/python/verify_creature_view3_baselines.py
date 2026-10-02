"""Independent synthetic CREO:3 wire oracle; no executable or server contact."""
import argparse
import json
import struct
from pathlib import Path


def fixture(count=0):
    p = lambda fmt, *v: struct.pack('<' + fmt, *v)
    string = lambda b, extended=False: (p('HI', 65535, len(b)) if extended else p('H', len(b))) + b
    ints = lambda counter, values: p('II', len(values), counter) + p('i' * len(values), *values)
    body = p('I', 0x80000000)
    body += string(bytes([102, 128, 0]), True) + p('I', 0xF1223344) + string(bytes([107, 255]))
    body += p('IHHH', 3, 0xD800, 65, 0xDCFF) + p('I', 0xFE020304)
    body += string(bytes([0, 254, 128]), True) + ints(0xF1234567, [-1, -2147483648, 17])
    body += p('IIIIBBBQfIQ', 0xF0112233, 0xFFFFFFFF, 0x81223344, 0x76543210, 165, 255, 254, 0xFE02030405060708, -1.75, 0xF1223344, 0xF0E1D2C3B4A59687)
    body += ints(0xFA010203, [-2, 2147483647, -2])
    return p('HIQIBIH', 5, 0x68A75F0C, 0xF122334455667788, 0x4352454F, 3, len(body) + 2, count) + body


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--generate', type=Path)
    parser.add_argument('--verify', type=Path)
    args = parser.parse_args()
    if args.generate:
        args.generate.mkdir(parents=True, exist_ok=True)
        (args.generate / 'input.bin').write_bytes(fixture())
    if args.verify:
        for count, name in [(0, 'baseline.bin'), (65535, 'baseline-count65535.bin')]:
            assert (args.verify / name).read_bytes() == fixture(count), name
        data = json.loads((args.verify / 'snapshot.json').read_text())
        expected = dict(ObjectId=0xF122334455667788, DeclaredCount=0, ComplexityBits=0x80000000,
            NameFile='668000', NameFileExtended=True, NameSpacer=0xF1223344, NameKey='6BFF', NameKeyExtended=False,
            CustomNameUnits=[0xD800,65,0xDCFF], Volume=0xFE020304, Customization='00FE80', CustomizationExtended=True,
            ComponentCounter=0xF1234567, Components=[-1,-2147483648,17], Options=0xF0112233, UseCount=0xFFFFFFFF,
            ConditionDamage=0x81223344, MaximumCondition=0x76543210, Visibility=165, Posture=255, Rank=254,
            LinkId=0xFE02030405060708, HeightBits=0xBFE00000, Shock=0xF1223344, States=0xF0E1D2C3B4A59687,
            WoundCounter=0xFA010203, Wounds=[-2,2147483647,-2], DecodedBytes=119)
        assert data == expected, (data, expected)
        print('Independent wire and all eighteen snapshot fields verified.')


if __name__ == '__main__':
    main()
