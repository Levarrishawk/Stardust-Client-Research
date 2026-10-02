"""Independent raw VTXA COLOR0 order and ARGB-to-RGBA audit; no asset output."""
import hashlib
import struct
from verify_static_mesh_slice import nodes, inspect


def verify(raw, rows):
    colored=vertices=0
    assert len(rows)==5
    for row in rows:
        data=raw[row['path']];inspect(data)
        root,=nodes(data);version,=root[1];sps,=version[1][1][1]
        colors=bytearray();rgba=bytearray();count=total=0
        for _, group in sps[1][1:]:
            for _, primitive in group[2:]:
                _,vtxa,*_=primitive;vv,=vtxa[1];vi,vd=vv[1]
                declaration,n=struct.unpack('<II',vi[1]);stride=len(vd[1])//n if n else 0;total+=n
                if declaration==0x110D:
                    for i in range(n):
                        value,=struct.unpack_from('<I',vd[1],i*stride+24);colors.extend(struct.pack('<I',value))
                        rgba.extend(struct.pack('<4f',((value>>16)&255)/255,((value>>8)&255)/255,(value&255)/255,(value>>24)/255));count+=1
        assert (total,count)==(row['vertices'],row['colored'])
        assert hashlib.sha256(colors).hexdigest().upper()==row['rawSha256']
        assert hashlib.sha256(rgba).hexdigest().upper()==row['rgbaSha256']
        colored+=count;vertices+=total
    assert (vertices,colored)==(72717,72495)
    return dict(original_static_color_meshes=len(rows),vertices=vertices,colored=colored,all_raw_and_normalized_rgba_hashes_equal=True)
