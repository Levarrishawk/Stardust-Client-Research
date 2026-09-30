"""Compare owned CMPA decoding to the independent corpus parser; retain no asset bytes."""
import argparse
import contextlib
import csv
import hashlib
import re
import struct
import subprocess
from pathlib import Path
from cmpa_corpus_inventory import parse
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import inspect,nodes
from verify_dds_pixels import inspect as inspect_dds
from dtla_corpus_inventory import parse as parse_detail


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path)
    p.add_argument('member');p.add_argument('expected_sha256');p.add_argument('core_dll',type=Path)
    p.add_argument('--inventory',type=Path,help='Existing CMPA inventory CSV; verify every listed component instead of member')
    p.add_argument('--geometry',action='store_true',help='Also verify a fully supported static component graph and all sorted variants')
    p.add_argument('--textured',action='store_true',help='Also verify all static shader dependencies and decoded texture mips')
    a=p.parse_args();mounts=[]
    for line in a.configuration.read_text().splitlines():
        match=re.fullmatch(r'\s*searchTree_00_(\d+)\s*=\s*([^\s;]+)\s*(?:;.*)?',line)
        if match:
            if any(c in match[2] for c in '/\\:'):raise ValueError('Unsupported archive path')
            mounts.append((int(match[1]),match[2]))
    if len({p for p,_ in mounts})!=len(mounts):raise ValueError('Duplicate mount priority')
    with contextlib.ExitStack() as stack:
        indices=[(p,Index(stack.enter_context((a.root/n).open('rb')))) for p,n in sorted(mounts,reverse=True)]
        members=sorted({row['component'] for row in csv.DictReader(a.inventory.open())}) if a.inventory else [a.member]
        for member in members:
            verify(member,indices,a)
        print(f'PASS verified_components={len(members)}')


def verify(member,indices,a):
    for priority,index in indices:
        blob=index.lookup(member)
        if blob is not None:break
    else:raise ValueError('Missing component input')
    digest=hashlib.sha256(blob).hexdigest().upper()
    if not a.inventory and digest!=a.expected_sha256.upper():raise ValueError('Component identity mismatch')
    version,parts,auxiliary=parse(blob)
    output=subprocess.run(['dotnet',str(a.core_dll),'--component-overlay',str(a.root),'--configuration',str(a.configuration),
        '--member',member,'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=60).stdout
    header=f'component={member} priority={priority} bytes={len(blob)} sha256={digest} version={version} parts={len(parts)}'
    if header not in output:raise ValueError('Native/reference component header mismatch')
    for i,(name,matrix) in enumerate(parts):
        path=name if name.startswith('appearance/') else 'appearance/'+name
        bits=''.join(f'{struct.unpack("<I",struct.pack("<f",value))[0]:08X}' for value in matrix)
        if f'component_part={i} child={path} transform_bits={bits}' not in output:
            raise ValueError('Native/reference component transform mismatch')
    print(f'PASS component={member} priority={priority} sha256={digest} version={version} parts={len(parts)} transform_floats={12*len(parts)} mounts={len(indices)}')
    if a.geometry or a.textured:verify_geometry(member,digest,indices,a)


def verify_geometry(member,digest,indices,a):
    output=subprocess.run(['dotnet',str(a.core_dll),'--geometry-graph',str(a.root),'--configuration',str(a.configuration),
        '--member',member,'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=60).stdout
    sources={};meshes=set();instances=0;variants=0
    def read(path):
        if path in sources:return sources[path]
        for _,index in indices:
            blob=index.lookup(path)
            if blob is not None:sources[path]=blob;return blob
        raise ValueError('Missing static graph input')
    def walk(path,depth,ancestors):
        nonlocal instances,variants
        if depth>32 or path in ancestors:raise ValueError('Reference graph depth/cycle')
        blob=read(path);family=blob[8:12]
        if family==b'MESH':
            value=inspect(blob);instances+=1
            expected=f'graph_mesh={path} depth={depth} vertices={value["vertices"]} triangles={value["triangles"]} geometry_sha256={value["geometry_sha256"]}'
            sorted_expected=f'graph_sorted={path} variants={value["sorted_variants"]} sha256={value["sorted_indices_sha256"]}'
            if expected not in output or sorted_expected not in output:raise ValueError('Static graph geometry/sorted hash mismatch')
            if path not in meshes:
                print(f'PASS graph_mesh={path} vertices={value["vertices"]} triangles={value["triangles"]} sorted_variants={value["sorted_variants"]} geometry_sha256={value["geometry_sha256"]} sorted_sha256={value["sorted_indices_sha256"]}')
                meshes.add(path);variants+=value['sorted_variants']
            return
        if family==b'CMPA':children=[name for name,_ in parse(blob)[1]]
        elif family==b'DTLA':children=parse_detail(blob)[3]
        else:raise ValueError('Reference verification requires fully supported CMPA/DTLA/MESH graph')
        for child in children:
            walk(child if child.startswith('appearance/') else 'appearance/'+child,depth+1,ancestors+[path])
    walk(member,0,[])
    for path,blob in sources.items():
        identity=hashlib.sha256(blob).hexdigest().upper()
        if f'graph_source={path} bytes={len(blob)} sha256={identity}' not in output:raise ValueError('Static graph source identity mismatch')
    if f'PASS geometry_graph sources={len(sources)} mesh_instances={instances} diagnostics=0' not in output:
        raise ValueError('Static graph aggregate/omission mismatch')
    print(f'PASS graph_sources={len(sources)} mesh_instances={instances} unique_meshes={len(meshes)} sorted_variants={variants}')
    if a.textured:verify_textured(member,digest,indices,a,sources,meshes)


def verify_textured(member,digest,indices,a,sources,meshes):
    def read(path):
        if path in sources:return sources[path]
        for _,index in indices:
            blob=index.lookup(path)
            if blob is not None:sources[path]=blob;return blob
        raise ValueError('Missing material reference')
    def name(blob):return blob.rstrip(b'\0').decode('ascii').lower().replace('\\','/')
    shaders=set();textures=set();effects=set()
    for path in meshes:
        root,=nodes(read(path));version,=root[1];sps=version[1][1];sps_version,=sps[1]
        for _,group in sps_version[1][1:]:
            if group[0][0]!=b'NAME':raise ValueError('Inline shader outside verifier slice')
            shaders.add(name(group[0][1]))
    for path in shaders:
        root,=nodes(read(path));assert root[0]==b'SSHT';version,=root[1]
        effect,=[value for tag,value in version[1] if tag==b'NAME'];effect=name(effect)
        effects.add(effect if '/' in effect else 'effect/'+effect)
        for tag,maps in version[1]:
            if tag!=b'TXMS':continue
            for _,binding in maps:
                binding_version,=binding
                texture,=[value for tag,value in binding_version[1] if tag==b'NAME'];textures.add(name(texture))
    for path in effects:
        root,=nodes(read(path));assert root[0]==b'EFCT'
    expected={}
    for path in sorted(textures):
        with contextlib.redirect_stdout(__import__('io').StringIO()):hashes=inspect_dds(read(path))
        expected.update({(path,i):digest for i,digest in enumerate(hashes)})
    output=subprocess.run(['dotnet',str(a.core_dll),'--textured-graph',str(a.root),'--configuration',str(a.configuration),
        '--member',member,'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=60).stdout
    actual={(path,int(level)):value for path,level,value in re.findall(r'^graph_texture=(\S+) mip=(\d+) sha256=([0-9A-F]{64})$',output,re.MULTILINE)}
    if actual!=expected:raise ValueError('Shared graph native/reference texture mismatch')
    for path,blob in sources.items():
        if f'material_source={path} bytes={len(blob)} sha256={hashlib.sha256(blob).hexdigest().upper()}' not in output:
            raise ValueError('Shared graph material identity mismatch')
    summary=f'PASS textured_graph sources={len(sources)} material_meshes={len(meshes)} textures={len(textures)} mips={len(expected)} diagnostics=0'
    if summary not in output:raise ValueError('Shared graph material aggregate/omission mismatch')
    print(f'PASS material_sources={len(sources)} meshes={len(meshes)} shaders={len(shaders)} effects={len(effects)} textures={len(textures)} matched_mips={len(expected)}')

if __name__=='__main__':main()
