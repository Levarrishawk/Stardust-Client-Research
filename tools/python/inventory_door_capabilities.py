"""Reconcile installed door rows/POB occurrences with the bounded native normal-leaf loader. Detail is private."""
import argparse, collections, contextlib, hashlib, json, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_static_mesh_slice import nodes
from verify_single_leaf_door import TABLE, TABLE_SHA, table_rows, walk
from door_actor_reference import close
import struct

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','private_output','core_dll'):p.add_argument(name,type=Path)
    p.add_argument('--direct-only',action='store_true',help='Reproduce the former direct-mesh capability boundary.')
    a=p.parse_args();a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    command=['dotnet',str(a.core_dll),'--door-capabilities',str(a.root),'--configuration',str(a.configuration),'--table-sha256',TABLE_SHA]
    if a.direct_only:command.append('--direct-only')
    run=subprocess.run(command,capture_output=True,text=True,timeout=90);(a.private_output/'native-capabilities.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    native={r['name']:r for r in [json.loads(s[len('door_capability='):]) for s in run.stdout.splitlines() if s.startswith('door_capability=')]}
    pins={};families=collections.Counter();categories=collections.Counter();occurrences=collections.Counter();eligible=collections.Counter();records=[];details=[];seen=set();masks=0;detail_styles=detail_vertices=0;detail_names=set();candidates=[]
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        def read(member):
            for index in indices:
                raw=index.lookup(member)
                if raw is not None:
                    if not raw:return None
                    pins[member]=hashlib.sha256(raw).hexdigest().upper();return raw
            return None
        rows=table_rows(read(TABLE));assert pins[TABLE]==TABLE_SHA and len(rows)==len(native)==124
        for r in rows:
            n=native[r['name']];category='force_field' if r['force'] else 'secondary_leaf' if r['secondary'] else 'flip_without_secondary' if r['flip'] else 'missing_primary' if not r['primary'] else 'single_leaf_candidate'
            assert (n['primary'],n['secondary'],n['flip'],n['force'],n['frame'])==(r['primary'],r['secondary'],r['flip'],r['force'],r['frame'])
            terminal=None;root_path=r['primary'];appr=None
            if category=='single_leaf_candidate':
                raw=read(root_path)
                if raw is None:assert n['category']=='missing_primary_asset'
                else:
                    assert pins[root_path]==n['primary_sha256'];root,=nodes(raw);visited=set()
                    while root[0]==b'APT ':
                        assert root_path not in visited and len(visited)<16;visited.add(root_path)
                        root_path=next(v.rstrip(b'\0').decode('ascii') for tag,v in walk([root]) if tag==b'NAME');root,=nodes(read(root_path))
                    terminal=root[0].decode('ascii');families[terminal]+=1
                    if n['category']=='supported_single_leaf':
                        assert terminal in ('MESH','DTLA');appr=next(v for tag,v in walk([root]) if tag==b'APPR');extent=appr[0][1][0];assert extent[0]==b'EXBX'
                        box=next(v for tag,v in walk([extent]) if tag==b'BOX ');close(n['bounds']['max'],struct.unpack_from('<3f',box),1e-6);close(n['bounds']['min'],struct.unpack_from('<3f',box,12),1e-6)
                        if terminal=='DTLA':detail_styles+=1;detail_names.add(r['name'])
                        for source in n['bounds']['sources']:
                            source_raw=read(source['Path']);assert pins[source['Path']]==source['Sha256']
                            source_nodes=nodes(source_raw)
                            if terminal!='DTLA' or source_nodes[0][0]!=b'MESH':continue
                            lo=n['bounds']['min'];hi=n['bounds']['max']
                            for tag,value in walk(source_nodes):
                                if tag!=b'VTXA':continue
                                version,=value;assert version[0]==b'0003';info,data=version[1];declaration,count=struct.unpack('<II',info[1]);payload=data[1]
                                components=6+sum(((declaration>>(12+2*s))&3)+1 for s in range((declaration>>8)&15));stride=components*4+(4 if declaration==0x110D else 0)
                                assert len(payload)==count*stride
                                for vi in range(count):
                                    position=struct.unpack_from('<3f',payload,vi*stride);assert all(lo[i]-1e-4<=position[i]<=hi[i]+1e-4 for i in range(3));detail_vertices+=1
                    elif n['error']=='Door leaf asset slice requires a direct static mesh.':assert terminal in ('DTLA','CMPA')
                    else:assert n['category']=='unsupported_primary_asset' and n['error']=='Unsupported vertex declaration.'
            else:assert n['category']==category
            categories[n['category']]+=1;details.append(dict(style=r['name'],native_category=n['category'],terminal_family=terminal,frame_present=bool(r['frame'])))
        for index in indices:
            for member,entry in index.entries.items():
                if not member.endswith('.pob') or member in seen:continue
                seen.add(member)
                if entry[1]==0:masks+=1;continue
                raw=index.lookup(member);layout=inspect(raw);sha=hashlib.sha256(raw).hexdigest().upper();records.append(dict(member=member,sha256=sha,cells=len(layout['cells'])))
                for ci,cell in enumerate(layout['cells']):
                    for pi,r in enumerate(cell['portals']):
                        if not r['door_name']:continue
                        name=r['door_name'];category=native[name]['category'] if name in native else 'missing_named_style';occurrences[category]+=1
                        if not r['leading_byte'] and r['solid_byte']!=0:eligible[category]+=1
                        if name in detail_names and cell['floor'] and layout['cells'][r['target_cell']]['floor'] and r['transform_flag']==1:
                            other=layout['cells'][r['target_cell']];paired=[(oi,x) for oi,x in enumerate(other['portals']) if x['geometry_index']==r['geometry_index'] and x['target_cell']==ci and x['door_name']==name and x['transform_flag']==1 and x['transform_hex']==r['transform_hex']]
                            if len(paired)==1:candidates.append(dict(member=member,layout_sha256=sha,cell=ci,portal=pi,target=r['target_cell'],target_portal=paired[0][0],style=name,primary=native[name]['primary'],primary_sha256=native[name]['primary_sha256']))
                        if member=='appearance/thm_tato_cantina.pob' and name=='door_cantina_up':details.append(dict(member=member,cell=ci,portal=pi,target=r['target_cell'],style=name,transform=r['transform_hex'],layout_sha256=sha))
    summary=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),mounts=len(indices),table_sha256=TABLE_SHA,
                 table_rows=len(rows),direct_only=a.direct_only,native_categories=dict(categories),candidate_terminal_families=dict(families),detail_styles=detail_styles,detail_vertex_checks=detail_vertices,effective_layouts=len(records),masked_layouts=masks,
                 named_reference_categories=dict(occurrences),eligible_named_reference_categories=dict(eligible))
    (a.private_output/'capability-details.json').write_text(json.dumps(dict(summary=summary,details=details,layouts=records,candidates=candidates,source_pins=pins),indent=2)+'\n',encoding='utf-8')
    (a.private_output/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS: installed/native door capability inventory '+json.dumps(summary))

if __name__=='__main__':main()
