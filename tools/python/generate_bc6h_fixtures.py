"""Standards synthetic BC6H fixtures; independent reference lives in the Godot bcdec gate."""
import argparse
import hashlib
import random
import struct
from pathlib import Path
from verify_modern_dds_fixtures import header


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path);args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    rng=random.Random(20260930)
    modes=(0,1,2,6,10,14,18,22,26,30,3,7,11,15)
    fixtures={}
    for dxgi in (95,96):
        for mode in modes:
            width,height,count=256,4,9
            payload=[]
            for level in range(count):
                for index in range(((max(1,width>>level)+3)//4)*((max(1,height>>level)+3)//4)):
                    value=int.from_bytes(rng.randbytes(16),'little')
                    bits=2 if mode in (0,1) else 5
                    value=(value&~((1<<bits)-1))|mode
                    # All 32 partitions are sampled; the field follows bit 77 in two-region modes.
                    if mode not in (3,7,11,15):value=(value&~(31<<77))|((index%32)<<77)
                    payload.append(value.to_bytes(16,'little'))
            fixtures[f'dxgi_{dxgi}_mode_{mode:02}']=(width,height,count,b''.join(payload),dxgi)
        for mode in (19,23,27,31):
            value=(int.from_bytes(rng.randbytes(16),'little')&~31)|mode
            fixtures[f'dxgi_{dxgi}_reserved_{mode}']=(4,4,1,value.to_bytes(16,'little'),dxgi)
        fixtures[f'dxgi_{dxgi}_clipped_3x5']=(3,5,1,b'\x03'+rng.randbytes(15)+b'\x0f'+rng.randbytes(15),dxgi)
        fixtures[f'dxgi_{dxgi}_zero_1x1']=(1,1,1,bytes(16),dxgi)
    for name,(width,height,count,payload,dxgi) in fixtures.items():
        data=bytearray(header(b'DX10',payload,dxgi));struct.pack_into('<II',data,12,height,width)
        struct.pack_into('<I',data,28,count);struct.pack_into('<I',data,144,3)
        target=args.output/(name+'.dds')
        if target.exists():
            if target.read_bytes()!=data:raise ValueError('Existing fixture differs; refusing overwrite')
        else:
            with target.open('xb') as stream:stream.write(data)
        print(name+' sha256='+hashlib.sha256(data).hexdigest().upper())
    print(f'PASS generated={len(fixtures)} synthetic BC6H fixtures, 14 valid modes, reserved modes, signed/unsigned, mips and clipped edges')


if __name__=='__main__':main()
