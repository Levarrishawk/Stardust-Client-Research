"""Independent raw-name TRE lookup, mesh geometry and all texture-mip pixel agreement. No asset output files."""
import argparse
import contextlib
import hashlib
import re
import struct
import subprocess
import sys
from pathlib import Path
from verify_static_mesh_slice import nodes, inspect as inspect_mesh
from verify_dds_pixels import inspect as inspect_dds
from scan_tre_sound_references import expand
from audit_tre_filename_crc import normalize

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from tre_crc_vectors import swg_crc


class Index:
    def __init__(self,stream):
        self.stream=stream
        header=stream.read(36)
        if len(header)!=36 or header[:8]!=b"EERT5000": raise ValueError("Unsupported TRE header")
        count,offset,rk,rs,nk,ns,nu=struct.unpack_from("<7I",header,8)
        if count>1_000_000 or count*24>64*1024*1024 or max(rs,ns,nu)>64*1024*1024: raise ValueError("TRE index budget")
        stream.seek(offset)
        rows=expand(rk,stream.read(rs if rk else count*24),count*24)
        names=expand(nk,stream.read(ns if nk else nu),nu)
        self.entries={}
        for i in range(count):
            crc,size,position,kind,packed,start=struct.unpack_from("<6I",rows,i*24)
            name=names[start:names.index(0,start)]
            if swg_crc(name)!=crc: raise ValueError("Raw TRE index checksum mismatch")
            key=name.decode("ascii")
            if key in self.entries: raise ValueError("Duplicate raw TRE name")
            self.entries[key]=(crc,size,position,kind,packed)

    def lookup(self,name):
        record=self.entries.get(name)
        if record is None or record[0]!=swg_crc(name.encode("ascii")): return None
        crc,size,position,kind,packed=record
        if size==0: raise ValueError("Required logical asset is masked")
        if size>64*1024*1024: raise ValueError("Probe payload budget")
        self.stream.seek(position)
        return expand(kind,self.stream.read(size if kind==0 else packed),size)


def name(value):
    if not value or value[-1]!=0: raise ValueError("Invalid resource name")
    return normalize(value[:-1]).decode("ascii")


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root",type=Path);parser.add_argument("configuration",type=Path)
    parser.add_argument("member");parser.add_argument("expected_sha256");parser.add_argument("core_project",type=Path)
    args=parser.parse_args()
    mounts=[]
    for line in args.configuration.read_text().splitlines():
        match=re.fullmatch(r"\s*searchTree_00_(\d+)\s*=\s*([^\s;]+)\s*(?:;.*)?",line)
        if match:
            filename=match[2]
            if any(c in filename for c in "/\\:") or not filename.lower().endswith(".tre"):raise ValueError("Unsupported archive name")
            mounts.append((int(match[1]),filename))
    if len({priority for priority,_ in mounts})!=len(mounts):raise ValueError("Duplicate priorities")
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context((args.root/filename).open("rb")))) for priority,filename in sorted(mounts,reverse=True)]
        def lookup(path):
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:return priority,data
            raise ValueError("Missing required resource")
        priority,mesh_bytes=lookup(args.member)
        if hashlib.sha256(mesh_bytes).hexdigest().upper()!=args.expected_sha256.upper():raise ValueError("Original mesh identity mismatch")
        geometry=inspect_mesh(mesh_bytes)
        root,=nodes(mesh_bytes);version,=root[1];sps=version[1][1];sps_version,=sps[1]
        shader_names={name(group[1][0][1]) for group in sps_version[1][1:]}
        texture_names=set();effect_names=set()
        for shader_name in sorted(shader_names):
            _,shader=lookup(shader_name);root,=nodes(shader);version,=root[1]
            effect,=[value for tag,value in version[1] if tag==b"NAME"]
            effect_path=name(effect);effect_names.add(effect_path if "/" in effect_path else "effect/"+effect_path)
            txms,=[value for tag,value in version[1] if tag==b"TXMS"]
            for _,children in txms:
                binding_version,=children
                texture,=[value for tag,value in binding_version[1] if tag==b"NAME"]
                texture_names.add(name(texture))
        expected={}
        for texture_name in sorted(texture_names):
            _,texture=lookup(texture_name)
            # The standalone mip verifier prints hashes; keep this bundle output compact.
            with contextlib.redirect_stdout(__import__("io").StringIO()):hashes=inspect_dds(texture)
            for level,digest in enumerate(hashes):expected[(texture_name,level)]=digest
            print(f"texture={texture_name} mips={len(hashes)} sha256={hashlib.sha256(texture).hexdigest().upper()}")
        for effect in effect_names:
            _,payload=lookup(effect);root,=nodes(payload)
            if root[0]!=b"EFCT":raise ValueError("Invalid effect envelope")
        result=subprocess.run(["dotnet","run","--no-restore","--project",str(args.core_project),"--",
            "--mesh-overlay",str(args.root),"--configuration",str(args.configuration),"--member",args.member,
            "--expected-sha256",args.expected_sha256],capture_output=True,text=True,check=True,timeout=120)
        actual={(path,int(level)):digest for path,level,digest in re.findall(
            r"^texture_mip=(\S+) level=(\d+) rgba_sha256=([0-9A-F]{64})$",result.stdout,re.MULTILINE)}
        if actual!=expected:raise ValueError("Bundle native/reference texture mip mismatch")
        if geometry["geometry_sha256"] not in result.stdout:raise ValueError("Bundle geometry mismatch")
        print(f"PASS: mounts={len(indices)} mesh_priority={priority} shaders={len(shader_names)} textures={len(texture_names)} effects={len(effect_names)} matched_texture_mips={len(expected)} geometry_sha256={geometry['geometry_sha256']}")


if __name__=="__main__":main()
