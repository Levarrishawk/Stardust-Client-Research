"""Independent original DTLA bands, child identities and mesh geometry agreement. No asset output."""
import argparse
import contextlib
import hashlib
import re
import subprocess
from pathlib import Path
from dtla_corpus_inventory import parse
from apt_redirect_inventory import parse_apt
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import inspect


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root',type=Path);parser.add_argument('configuration',type=Path)
    parser.add_argument('member');parser.add_argument('expected_sha256');parser.add_argument('core_project',type=Path)
    args=parser.parse_args()
    mounts=[]
    for line in args.configuration.read_text().splitlines():
        match=re.fullmatch(r'\s*searchTree_00_(\d+)\s*=\s*([^\s;]+)\s*(?:;.*)?',line)
        if match:
            if any(c in match[2] for c in '/\\:'):raise ValueError('Unsupported archive path')
            mounts.append((int(match[1]),match[2]))
    if len({p for p,_ in mounts})!=len(mounts):raise ValueError('Duplicate mount priority')
    with contextlib.ExitStack() as stack:
        indices=[(p,Index(stack.enter_context((args.root/n).open('rb')))) for p,n in sorted(mounts,reverse=True)]
        def lookup(path):
            for p,index in indices:
                blob=index.lookup(path)
                if blob is not None:return p,blob
            raise ValueError('Required appearance absent')
        priority,blob=lookup(args.member)
        digest=hashlib.sha256(blob).hexdigest().upper()
        if digest!=args.expected_sha256.upper():raise ValueError('Original appearance identity mismatch')
        terminal=args.member;terminal_digest=digest
        if blob[8:12]==b'APT ':
            version,terminal=parse_apt(blob)
            if version!='0000' or terminal.endswith('.apt'):raise ValueError('Unsupported APT reference')
            terminal_priority,terminal_blob=lookup(terminal)
            terminal_digest=hashlib.sha256(terminal_blob).hexdigest().upper()
            bundle=subprocess.run(['dotnet','run','--no-restore','--project',str(args.core_project),'--',
                '--detail-bundle',str(args.root),'--configuration',str(args.configuration),'--member',args.member,
                '--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120).stdout
            for path,payload,source_priority in ((args.member,blob,priority),(terminal,terminal_blob,terminal_priority)):
                expected=f'appearance_source={path} priority={source_priority} bytes={len(payload)} sha256={hashlib.sha256(payload).hexdigest().upper()}'
                if expected not in bundle:raise ValueError('Appearance redirect native/reference identity mismatch')
            print(f'PASS APT root={args.member} target={terminal} target_sha256={terminal_digest}')
            blob=terminal_blob
        values=parse(blob)
        bands=list(zip(values[5],values[6]))
        child_map={int.from_bytes(bytes.fromhex(raw),'little'):path for raw,path in zip(values[8],values[3])}
        output=subprocess.run(['dotnet','run','--no-restore','--project',str(args.core_project),'--',
            '--detail-overlay',str(args.root),'--configuration',str(args.configuration),'--member',terminal,
            '--expected-sha256',terminal_digest],capture_output=True,text=True,check=True,timeout=120).stdout
        for identifier,(lower,upper) in bands:
            path=child_map[identifier]
            if not path.startswith('appearance/'):path='appearance/'+path
            expected=f'level_id={identifier} lower={lower:g} upper={upper:g} child={path}'
            if expected not in output:raise ValueError('Authored band/native mismatch')
            child_priority,child=lookup(path)
            identity=hashlib.sha256(child).hexdigest().upper()
            geometry=inspect(child)
            summary=f'child_mesh={path} priority={child_priority} sha256={identity} vertices={geometry["vertices"]} triangles={geometry["triangles"]} batches={geometry["batches"]}'
            if summary not in output:raise ValueError('Child mesh/native mismatch')
            if f'child_geometry={path} sha256={geometry["geometry_sha256"]}' not in output:
                raise ValueError('Child geometry hash/native mismatch')
            print(f'PASS child={path} vertices={geometry["vertices"]} triangles={geometry["triangles"]} geometry_sha256={geometry["geometry_sha256"]}')
        for tag,count_vertices,count_indices,valid in values[11]:
            if not valid:raise ValueError('Reference visibility indices invalid')
            key=tag.split(':')[1]
            if not re.search(fr'^policy={key} flags=\d+ vertices={count_vertices} indices={count_indices}$',output,re.MULTILINE):
                raise ValueError('Policy geometry/native mismatch')
        print(f'PASS appearance_root_identity={digest} root_priority={priority} detail_identity={terminal_digest} bands={len(bands)} indexed_policy_geometries={len(values[11])} mounts={len(indices)}')


if __name__=='__main__':main()
