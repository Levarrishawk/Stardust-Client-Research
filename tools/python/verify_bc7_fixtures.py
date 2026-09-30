"""Independent BC7 mode/partition/rotation/mip corpus. Pillow reference; no library encoder is used."""
import argparse
import random
import struct
import subprocess
import sys
from pathlib import Path
from verify_modern_dds_fixtures import header


def block(mode,variant,rng):
    value=int.from_bytes(rng.randbytes(16),'little')
    value=(value & ~((1<<(mode+1))-1)) | (1<<mode)
    bits=4 if mode==0 else 6 if mode in (1,2,3,7) else 0
    if bits:
        mask=((1<<bits)-1)<<(mode+1)
        value=(value & ~mask)|((variant & ((1<<bits)-1))<<(mode+1))
    if mode in (4,5):
        rotation_bits=3 if mode==4 else 2
        mask=((1<<rotation_bits)-1)<<(mode+1)
        value=(value & ~mask)|((variant & ((1<<rotation_bits)-1))<<(mode+1))
    return value.to_bytes(16,'little')


def mode6(endpoints,selectors):
    value=1<<6;cursor=7
    for channel in range(4):
        for endpoint in endpoints:
            value|=(endpoint[channel]>>1)<<cursor;cursor+=7
    cursor+=2 # endpoint P-bits both zero; caller supplies even endpoints
    for pixel,index in enumerate(selectors):
        value|=index<<cursor;cursor+=3 if pixel==0 else 4
    if cursor!=128:raise ValueError('Independent mode6 extent mismatch')
    return value.to_bytes(16,'little')


def fixture(dxgi,width,height,payload,mips=1):
    data=bytearray(header(b'DX10',payload,dxgi))
    struct.pack_into('<II',data,12,height,width);struct.pack_into('<I',data,28,mips)
    struct.pack_into('<I',data,144,1) # straight alpha
    return bytes(data)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path);parser.add_argument('core_project',type=Path);args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    rng=random.Random(20260930)
    fixtures={}
    for mode in range(8):
        payload=b''.join(block(mode,b,rng) for level in range(9)
            for b in range(((max(1,256>>level)+3)//4)*((max(1,4>>level)+3)//4)))
        for dxgi in (98,99):fixtures[f'mode_{mode}_dxgi_{dxgi}']=fixture(dxgi,256,4,payload,9)
    fixtures['clipped_3x5']=fixture(98,3,5,block(4,7,rng)+block(7,63,rng))
    constant=mode6(((128,0,64,254),(128,0,64,254)),[0]*16)
    gradient=mode6(((0,0,64,0),(254,128,0,254)),list(range(16)))
    fixtures['mode6_constant_1x1']=fixture(99,1,1,constant)
    fixtures['mode6_gradient_4x4']=fixture(98,4,4,gradient)
    fixtures['reserved_zero_output']=fixture(98,4,4,b'\0'+rng.randbytes(15))
    print('mode6_constant_hex='+constant.hex().upper())
    print('mode6_gradient_hex='+gradient.hex().upper())
    for name,content in fixtures.items():
        target=args.output/(name+'.dds')
        if target.exists():
            if target.read_bytes()!=content:raise ValueError('Existing independent fixture differs; refusing replacement')
        else:
            with target.open('xb') as stream:stream.write(content)
        result=subprocess.run([sys.executable,str(Path(__file__).with_name('verify_dds_pixels.py')),str(target),
            '--core-project',str(args.core_project)],capture_output=True,text=True,timeout=120)
        if result.returncode:raise RuntimeError(result.stdout[-2000:]+result.stderr[-2000:])
        print(name+': '+result.stdout.splitlines()[-1],flush=True)
    print(f'PASS: {len(fixtures)} independent BC7 fixtures, all eight modes, partitions, rotations, sRGB, mips and edges')


if __name__=='__main__':main()
