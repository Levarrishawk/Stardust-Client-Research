"""Independent double hierarchy and full-weight shader oracle for validated original clip samples.

Called by verify_skeletal_descriptors after source and numerical sampling audits.
This executes replacement arithmetic only, never Stardust.exe.
"""
import math
import struct
from verify_static_mesh_slice import nodes
from verify_skeleton_bind_pose import skeletons, quaternion, rotation, multiply, pose


def verify(raw, appearance, samples, names_rows, pose_rows, vertex_rows):
    selected = appearance['Skeletons'][0]['Path']
    joints = []
    for reference in appearance['Skeletons']:
        path = reference['Path']; basic = skeletons(raw[path])[0]; names, parents, arrays = basic
        attach = reference['AttachmentTransform'].lower(); parent = -1
        if attach: parent, = [i for i,j in enumerate(joints) if j['name'].lower() == attach]
        inverses = pose(basic, None if parent < 0 else joints[parent]['inverse']); start = len(joints)
        for source, name in enumerate(names):
            q = quaternion(quaternion(arrays[b'RPST'][source],arrays[b'BPRO'][source]),arrays[b'RPRE'][source])
            local = rotation(q,arrays[b'BPTR'][source]); global_parent = parent if parents[source] < 0 else start+parents[source]
            world = local if global_parent < 0 else multiply(joints[global_parent]['world'],local)
            joints.append(dict(path=path,source=source,name=name.decode('ascii'),parent=global_parent,
                               arrays=arrays,local=local,world=world,inverse=inverses[source]))
    assert len(joints) == 53
    expected_pose = set(); expected_vertex = set(); matrix_error = vertex_error = 0
    for frame in (0,30,59.5,90,120):
        named = {name.lower():(q,p) for (time,name),(q,p) in samples.items() if time == frame}
        body_names = {j['name'].lower() for j in joints if j['path'] == selected}
        unused = [name for time,name in samples if time == frame and name.lower() not in body_names]
        missing = [j['name'] for j in joints if j['path'] == selected and j['name'].lower() not in named]
        assert names_rows[frame] == dict(frame=frame,path=selected,unused=unused,missing=missing)
        worlds = []
        for index,joint in enumerate(joints):
            local = joint['local']
            if joint['path'] == selected and joint['name'].lower() in named:
                xyzw,translation = named[joint['name'].lower()]; clip = [xyzw[3],*xyzw[:3]]
                arrays = joint['arrays']; source = joint['source']
                q = quaternion(arrays[b'RPST'][source],quaternion(quaternion(clip,arrays[b'BPRO'][source]),arrays[b'RPRE'][source]))
                norm = math.sqrt(sum(v*v for v in q)); q = [v/norm for v in q]
                local = rotation(q,[a+b for a,b in zip(arrays[b'BPTR'][source],translation)])
            world = local if joint['parent'] < 0 else multiply(worlds[joint['parent']],local); worlds.append(world)
            key = frame,index; expected_pose.add(key); native = pose_rows[key]
            assert native['path'] == joint['path'] and native['source'] == joint['source']
            for field,matrix in (('local',local),('world',world)):
                expected = [v for row in matrix[:3] for v in row]
                assert len(native[field]) == 12
                matrix_error = max(matrix_error,*[abs(a-b) for a,b in zip(native[field],expected)])
        for path in ('appearance/mesh/hum_m_body_l0.mgn','appearance/mesh/hum_m_head_l0.mgn'):
            root, = nodes(raw[path]); version, = root[1]; fields = dict(version[1][:6])
            paths = [p.decode('ascii') for p in fields[b'SKTM'].split(b'\0')[:-1]]
            transform_names = [n.decode('ascii') for n in fields[b'XFNM'].split(b'\0')[:-1]]
            mapping = []
            for name in transform_names:
                match, = [i for i,j in enumerate(joints) if j['path'] in paths and j['name'].lower() == name.lower()]; mapping.append(match)
            palette = [multiply(worlds[i],joints[i]['inverse']) for i in mapping]
            points = list(struct.iter_unpack('<3f',fields[b'POSN'])); counts = list(struct.unpack('<'+'i'*len(points),fields[b'TWHD']))
            weights = list(struct.iter_unpack('<If',fields[b'TWDT'])); starts = []; cursor = 0
            for count in counts: starts.append(cursor); cursor += count
            assert cursor == len(weights)
            chunks = dict(version[1]); normals = list(struct.iter_unpack('<3f',chunks[b'NORM']))
            shaders = [dict(records) for tag,records in version[1] if tag == b'PSDT']
            for shader,records in enumerate(shaders):
                pmap = [v for (v,) in struct.iter_unpack('<I',records[b'PIDX'][4:])]
                nmap = [v for (v,) in struct.iter_unpack('<I',records[b'NIDX'])]; assert len(pmap) == len(nmap)
                for vertex,(pindex,nindex) in enumerate(zip(pmap,nmap)):
                    key = frame,path,shader,vertex; expected_vertex.add(key); native = vertex_rows[key]
                    group = weights[starts[pindex]:starts[pindex]+counts[pindex]]
                    for field,vector,translated in (('position',points[pindex],True),('normal',normals[nindex],False)):
                        expected = [sum(weight*(sum(palette[index][component][axis]*vector[axis] for axis in range(3))+(palette[index][component][3] if translated else 0)) for index,weight in group) for component in range(3)]
                        assert len(native[field]) == 3
                        vertex_error = max(vertex_error,*[abs(a-b) for a,b in zip(native[field],expected)])
    assert set(names_rows) == {0,30,59.5,90,120} and set(pose_rows) == expected_pose and set(vertex_rows) == expected_vertex
    assert matrix_error < 2e-5 and vertex_error < 2e-5
    return dict(animated_joint_poses=len(pose_rows),shader_vertices=len(vertex_rows),maximum_matrix_error=matrix_error,
                maximum_vertex_error=vertex_error,unused=['rifle'],missing=['hold_l'],agreement=True,original_execution=False)
