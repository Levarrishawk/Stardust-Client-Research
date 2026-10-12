"""Verify frozen self-contained DX12 terrain export, expected rejections and independent displayed-triangle grounding."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import shutil
import subprocess
import struct

ROOT = Path(__file__).resolve().parents[2]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--build', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--gpu', action='store_true')
    p.add_argument('--original-snapshot',type=Path,help='Pinned original snapshot required for Mos Taike placement verification')
    a = p.parse_args()
    build, out = a.build.resolve(), a.output.resolve()
    out.relative_to((ROOT / 'local-private').resolve())
    if out.exists():
        raise ValueError('Fresh private verification output required')
    out.mkdir(parents=True)
    package = build / 'client'
    receipt = json.loads((build / 'build-receipt.json').read_text())
    assert receipt['Entry'] in ('partial_original_terrain','original_mos_taike_district')
    town=receipt['Entry']=='original_mos_taike_district'
    if town and not a.original_snapshot:raise ValueError('Original snapshot input required')
    def frozen():
        for name, identity in receipt['PackageFiles'].items():
            path = package / name
            assert path.stat().st_size == identity['Bytes']
            assert hashlib.sha256(path.read_bytes()).hexdigest() == identity['Sha256']
    frozen()
    for name in ('coreclr.dll', 'hostfxr.dll', 'System.Private.CoreLib.dll'):
        assert list(package.rglob(name)), name
    env = {k: v for k, v in os.environ.items() if not k.startswith(('DOTNET', 'GODOT', 'NUGET'))}
    env['PATH'] = os.pathsep.join(value for value in env['PATH'].split(os.pathsep) if 'toolchains' not in value.lower())
    cases = []
    def run(name, engine, arguments, expected=0, executable=None):
        log = out / (name + '.log')
        with (out / (name + '-console.log')).open('w') as stream:
            result = subprocess.run([str(executable or package / 'Stardust.Client.exe'), *engine,
                '--log-file', str(log), '--', *arguments], cwd=out, env=env, stdout=stream, stderr=subprocess.STDOUT, timeout=180)
        text = log.read_text(encoding='utf-8')
        assert result.returncode == expected, (name, result.returncode, text[-1500:])
        cases.append(dict(Name=name, ExitCode=result.returncode, ExpectedExit=expected))
        return text
    cpu = run('cpu', ['--headless'], ['--terrain-self-test'])
    assert 'PASS: terrain CPU seams' in cpu
    assert '"Seams":234' in cpu and '"Patches":256' in cpu and '"Triangles":2048' in cpu
    cancel = run('cancel', ['--headless'], ['--terrain-cancel'])
    assert 'PASS: terrain cancellation drained worker before GPU publication' in cancel and '"Patches":0' in cancel
    assert 'D3d12Accepted\":false' in run('headless-refusal', ['--headless'], [], 1)
    assert 'Unsupported terrain launch option' in run('bad-option', ['--headless'], ['--unsupported'], 1)
    invalid = out / 'invalid-profile-client'
    shutil.copytree(package, invalid)
    profile = json.loads((invalid / 'character-launch.json').read_text())
    profile['Version'] = 999
    (invalid / 'character-launch.json').write_text(json.dumps(profile))
    run('bad-profile', ['--headless'], ['--terrain-self-test'], 1, invalid / 'Stardust.Client.exe')
    if a.gpu:
        fallback = run('opengl-refusal', ['--rendering-method', 'gl_compatibility', '--rendering-driver', 'opengl3'], ['--terrain-self-test'], 1)
        assert '"Driver":"opengl3"' in fallback and 'D3d12Accepted\":false' in fallback
        proof = out / 'gpu-proof'
        proof.mkdir()
        log = run('gpu', [], ['--offline-terrain-proof', str(proof)])
        assert 'PASS: exported DX12 terrain walk/run/idle GPU proof' in log and '"Disposed":true' in log
        result = json.loads((proof / 'terrain-proof.json').read_text())
        assert result['Backend'] == 'd3d12' and result['ActualProcessUpdates'] > 50
        assert [s['Clip'] for s in result['Motion']] == ['walk', 'run', 'idle']
        assert abs(result['WalkDistance'] - 3 * result['WalkSeconds']) < .001
        assert abs(result['RunDistance'] - 6 * result['RunSeconds']) < .001
        assert result['MaximumGroundError'] < 1e-5
        if town:
            assert result['AppliedHeightLayers']==[0,3,4,5,146,148,151,153,155,157,159,161]
            assert result['Town']['Rendered']==94 and result['Town']['Buildings']==5 and len(result['Town']['Omissions'])==9
            raw=a.original_snapshot.read_bytes()
            assert hashlib.sha256(raw).hexdigest().upper()==result['Town']['Sha256']
            def blocks(start,end):
                while start<end:
                    tag=raw[start:start+4];size=struct.unpack_from('>I',raw,start+4)[0];stop=start+8+size
                    assert stop<=end
                    yield tag,start,stop
                    start=stop
                assert start==end
            top,=blocks(12,len(raw));nods,names=list(blocks(top[1]+12,top[2]))
            names_raw=raw[names[1]+8:names[2]];count=struct.unpack_from('<i',names_raw)[0];templates=names_raw[4:].split(b'\0')
            assert len(templates)==count+1
            originals={}
            def visit(record,parent=None):
                _,start,stop=record;version,=blocks(start+12,stop);data,*children=blocks(version[1]+12,version[2]);values=struct.unpack('<iiiiffffffffI',raw[data[1]+8:data[2]])
                if parent is None and values[1]==values[3]==0 and 3720<=values[8]<=3880 and 2280<=values[10]<=2440:originals[values[0]]=values
                for child in children:visit(child,values[0])
            for node in blocks(nods[1]+12,nods[2]):visit(node)
            selected=result['Town']['Placements'];omitted=result['Town']['Omissions']
            assert set(originals)=={p['Id'] for p in selected}|{p['Id'] for p in omitted}
            f32=lambda v:struct.pack('<f',v)
            for placement in selected:
                values=originals[placement['Id']]
                assert templates[values[2]].decode()==placement['ObjectPath']
                assert all(f32(x)==f32(y) for x,y in zip(values[8:11],placement['Position']))
                assert all(f32(x)==f32(y) for x,y in zip(values[4:8],placement['QuaternionWXYZ']))
                assert values[12]==placement['PortalLayoutCrc']
            assert 'SweptCrossingRejected\":true' in log and 'TangentPassed\":true' in log and 'StartClear\":true' in log
            cases.append(dict(Name='independent-original-snapshot-placement',Pass=True,Placements=len(selected),Omissions=len(omitted)))
        geometry = json.loads((proof / 'terrain-geometry.json').read_text())
        # System.Text.Json emits shortest float32 round-trip decimals. Recover those stored float32
        # coordinates before the independent binary64 barycentric calculation, especially at town XYZ.
        stored_float=lambda value:struct.unpack('<f',struct.pack('<f',value))[0]
        for patch in geometry:
            patch['Positions']=[[stored_float(value) for value in point] for point in patch['Positions']]
        def height(x, z):
            for patch in geometry:
                points, indices = patch['Positions'], patch['Indices']
                for i in range(0, len(indices), 3):
                    a, b, c = [points[index] for index in indices[i:i+3]]
                    den = (b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
                    wa = ((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den
                    wb = ((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den
                    wc = 1-wa-wb
                    if min(wa, wb, wc) >= -1e-8:
                        return wa*a[1]+wb*b[1]+wc*c[1]
            raise AssertionError('No supplied triangle')
        for observation in result['Motion']:
            x, y, z = map(stored_float,observation['Position'])
            assert abs(y-height(x,z)) < 1e-5
        from PIL import Image, ImageChops
        images = [Image.open(proof / f'terrain-{i}.png').convert('RGB') for i in range(3)]
        assert all(image.width >= 1000 and image.height >= 600 for image in images)
        assert ImageChops.difference(images[0],images[1]).getbbox() is not None
        # Guard the presentation regression found during visual review: unreadable white-on-white UI and clipped sand.
        for image in images:
            hud = list(image.crop((0,0,image.width,118)).getdata())
            terrain_pixels = list(image.crop((0,image.height//2,image.width,image.height)).getdata())
            assert sum(max(rgb)<100 for rgb in hud)/len(hud) > .5, 'Dark readable HUD background required'
            assert sum(min(rgb)>250 for rgb in terrain_pixels)/len(terrain_pixels) < .25, 'Terrain highlight clipping'
        cases.append(dict(Name='independent-triangle-grounding-and-distance',Pass=True))
    frozen()
    (out / 'verification.json').write_text(json.dumps(dict(Version=1,Cases=cases,GpuExecuted=a.gpu,PackageUnchanged=True),indent=2))
    print(f'PASS: {len(cases)} terrain export checks; GPU executed={a.gpu}; package unchanged')


if __name__ == '__main__':
    main()
