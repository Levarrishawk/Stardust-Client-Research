"""Independent ray samples for the pinned three-cell portal visibility fixture."""
import argparse
import contextlib
import hashlib
import json
import math
import struct
import subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def hit(eye,direction,triangle):
    a,b,c=triangle;e1=sub(b,a);e2=sub(c,a);p=cross(direction,e2);det=dot(e1,p)
    if abs(det)<1e-10:return None
    t=sub(eye,a);u=dot(t,p)/det
    if u< -1e-8 or u>1+1e-8:return None
    q=cross(t,e1);v=dot(direction,q)/det
    if v< -1e-8 or u+v>1+1e-8:return None
    distance=dot(e2,q)/det
    return distance if distance>1e-6 else None

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','member','sha256','core_dll','private_output'):p.add_argument(name)
    a=p.parse_args();out=Path(a.private_output).resolve();out.relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());out.mkdir(parents=True,exist_ok=True)
    with contextlib.ExitStack() as stack:
        data=None
        for priority,path in archives(Path(a.root),Path(a.configuration)):
            value=Index(stack.enter_context(path.open('rb'))).lookup(a.member)
            if value is not None:data=value;break
        assert data and hashlib.sha256(data).hexdigest().upper()==a.sha256.upper()
        layout=inspect(data)
    selected={5,6,7};geometry={}
    for cell in selected:
        for reference in layout['cells'][cell]['portals']:
            if reference['target_cell'] not in selected:continue
            index=reference['geometry_index'];g=layout['geometries'][index];raw=bytes.fromhex(g['vertex_hex']);vertices=list(struct.iter_unpack('<fff',raw));indices=g['indices']
            geometry[index]=[tuple(vertices[i] for i in indices[start:start+3]) for start in range(0,len(indices),3)]
    command=['dotnet',a.core_dll,'--portal-visibility',a.root,'--configuration',a.configuration,'--member',a.member,'--expected-sha256',a.sha256]
    run=subprocess.run(command,capture_output=True,text=True,timeout=60);(out/'native.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    records=[json.loads(line.split('=',1)[1]) for line in run.stdout.splitlines() if line.startswith('portal_visibility=')];assert len(records)==6
    rays=0
    for record in records:
        eye=record['eye'];sign=record['forward'];visible={record['cell']}
        for triangles in geometry.values():
            for triangle in triangles:
                for i in range(9):
                    for j in range(9-i):
                        weights=(i/8,j/8,(8-i-j)/8);target=tuple(sum(w*v[axis] for w,v in zip(weights,triangle)) for axis in range(3));direction=sub(target,eye);length=math.sqrt(dot(direction,direction))
                        if length==0:continue
                        direction=tuple(v/length for v in direction)
                        if direction[2]*sign<=0 or abs(direction[0])>direction[2]*sign+1e-8 or abs(direction[1])>direction[2]*sign+1e-8:continue
                        rays+=1;cell=record['cell'];last=0;visited={cell}
                        for _ in range(3):
                            candidates=[]
                            for ref in layout['cells'][cell]['portals']:
                                if ref['target_cell'] not in selected or ref['target_cell'] in visited:continue
                                for tri in geometry[ref['geometry_index']]:
                                    t=hit(eye,direction,tri)
                                    if t is not None and last+1e-6<t<100:candidates.append((t,ref['target_cell']))
                            if not candidates:break
                            last,cell=min(candidates);visible.add(cell);visited.add(cell)
        assert not record['fallback'] and sorted(visible)==record['cells'],(record,visible)
    print(f'PASS original_views={len(records)} sampled_rays={rays} selected_cells=3 sha256={a.sha256.upper()}')

if __name__=='__main__':main()
