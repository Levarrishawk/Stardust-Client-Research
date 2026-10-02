"""Independent exact original VSH include graph and shared source identity audit."""
import hashlib
import posixpath
import re


def verify(raw,identities):
    expected={'vertex_program/a_specmap_bump_vs20_for_ps20.vsh':'0159BA23D91FA517A6BDF16488B4C95A0CA599DC448AF12CEACC48C3448362D6','vertex_program/include/functions.inc':'CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15','vertex_program/include/vertex_shader_constants.inc':'C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9','shared_program/functions.inc':'A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A'}
    assert identities==expected
    visited=set();edges=[]
    def walk(path,active):
        assert path not in active
        if path in visited:return
        assert path in expected and hashlib.sha256(raw[path]).hexdigest().upper()==expected[path];visited.add(path)
        for include in re.findall(r'^\s*#\s*include\s*"([^"]+)"',raw[path].decode('ascii'),re.M):
            target=include if include in expected else posixpath.normpath(posixpath.join(posixpath.dirname(path),include));assert target in expected
            edges.append([path,target]);walk(target,active|{path})
    walk('vertex_program/a_specmap_bump_vs20_for_ps20.vsh',set())
    assert visited==set(expected) and len(edges)==3
    return dict(original_vertex_sources=len(visited),include_edges=len(edges),source_bytes=sum(len(raw[p]) for p in visited),all_hashes_verified=True,legacy_compiler_execution=False)
