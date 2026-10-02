"""Independent original raw static-door basis projection and supplied view/light varying audit."""
import hashlib
import struct
import numpy as np
from verify_static_mesh_slice import nodes


def verify(raw,program,rows):
    assert len(program)==1 and program[0]['sha256']==hashlib.sha256(raw[program[0]['path']]).hexdigest().upper()
    assert program[0]['sha256']=='CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15'
    meshes={};maximum=0;zero_cases=0
    def unit(v):return v/max(np.linalg.norm(v),1e-6)
    def f32(v):return np.asarray(v,dtype=np.float32).astype(float)
    for row in rows:
        path=row['path']
        if path not in meshes:
            root,=nodes(raw[path]);version,=root[1];sv,=version[1][1][1];group=sv[1][1];primitive=group[1][2];vv,=dict(primitive[1])[b'VTXA'];info,data=vv[1];assert struct.unpack('<II',info[1])[0]==0xd205;meshes[path]=list(struct.iter_unpack('<12f',data[1]))
        v=meshes[path][row['index']];position=np.array(v[:3]);normal=np.array(v[3:6]);t=np.array(v[8:11]);b=np.cross(normal,t)*v[11];variant=row['variant'];light=np.eye(3)[2] if variant%3==0 else np.eye(3)[1] if variant%3==1 else unit(np.array([1.,2.,3.]));light=f32(light)
        camera=f32([0,1.7,-3] if variant<3 else [2,3,5]);half=unit(unit(camera-position)+light);basis=np.stack([t,b,normal]);expected_light=basis@light;expected_half=basis@half
        if np.linalg.norm(t)==0:zero_cases+=1;assert row['light'][:2]==[0,0] and row['half'][:2]==[0,0]
        maximum=max(maximum,float(np.max(abs(expected_light-f32(row['light'])))),float(np.max(abs(expected_half-f32(row['half'])))))
    assert len(rows)==660 and zero_cases==24 and maximum<2e-6
    return dict(original_vertex_cases=len(rows),raw_zero_tangent_cases=zero_cases,maximum_error=maximum,original_normalize_zero_behavior='unknown',compiled_vertex_precision='unknown')
