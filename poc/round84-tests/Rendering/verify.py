"""Pre-native expected scope/counts/root metadata and independent receipt/image checks."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.dont_write_bytecode = True


def expected(args):
    result = dict(Checks=39 + (20 + int(args.gpu) if args.asset_root else 0), Gpu=args.gpu,
                  OriginalCount=int(bool(args.asset_root)), ArchiveCalls=2 if args.asset_root else 0,
                  Position=[1.5, 2, 3], ObjectId=42, SyntheticPeer=True, OriginalExecutable=False, LiveCompatibility=False, NativeExit=args.native_exit, NativeFree=args.native_free)
    if args.asset_root:
        sys.path.insert(0, str(ROOT / 'tools/python'))
        from verify_textured_mesh_bundle import Index
        from pst_reference_audit import configured_archives
        archives = configured_archives(Path(args.asset_root), Path(args.config))
        for priority, path in archives:
            with path.open('rb') as stream:
                raw = Index(stream).lookup('object/creature/player/shared_human_male.iff')
                if raw is not None:
                    result.update(RootObjectSha256=hashlib.sha256(raw).hexdigest().upper(), RootObjectBytes=len(raw),
                                  RootPriority=priority, Mounts=len(archives), ConfigurationSha256=hashlib.sha256(Path(args.config).read_bytes()).hexdigest().upper())
                    break
        else:
            raise ValueError('Required explicit original object missing')
    target = Path(args.out)
    with target.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print('PASS authored expectation and optional independent original metadata before native')


def verify(args):
    e = json.loads(Path(args.expected).read_text())
    path = Path(args.native)
    n = json.loads(path.read_text())
    assert Path(args.expected).stat().st_mtime < path.stat().st_mtime
    assert n['Passed'] is True and type(n['Checks']) is int and n['Checks'] == e['Checks']
    for key in ('Gpu', 'SyntheticPeer', 'OriginalExecutable', 'LiveCompatibility'):
        assert type(n[key]) is bool and n[key] is e[key]
    assert type(n['ArchiveCalls']) is int and n['ArchiveCalls'] == n['ArchivesClosed'] == e['ArchiveCalls']
    assert len(n['Originals']) == e['OriginalCount']
    pixels = None
    if n['Originals']:
        original = n['Originals'][0]
        assert type(original['ObjectId']) is int and original['ObjectId'] == e['ObjectId']
        assert original['Position'] == e['Position'] and original['RootObjectSha256'] == e['RootObjectSha256']
        assert original['Published'] is True and type(original['Frames']) is int and original['Frames'] > 0
        assert type(original['TextureCount']) is int and original['TextureCount'] > 0
        close_observation = 'native free abort reports retired root and drains jobs' if e['NativeFree'] else 'native removal frees original root and drains jobs' if e['NativeExit'] else 'controlled close frees original root and drains jobs'
        assert close_observation in n['Observations']
        if e['NativeFree']:
            assert 'native free abort drains sources; successful visual disposal counter remains incomplete' in n['Observations']
        if e['Gpu']:
            from PIL import Image
            with Image.open(path.with_suffix('.png')) as image:
                assert image.size == (1280, 720)
                rgb = image.convert('RGB')
                colors = rgb.getcolors(1280 * 720)
                background = max(colors)[1]
                pixels = sum(count for count, color in colors if sum(abs(a-b) for a,b in zip(color, background)) > 20)
                assert pixels > 250, 'Selected actor must occupy independently visible image pixels'
    receipt = dict(Passed=True, ExpectedBeforeNative=True, Checks=n['Checks'], OriginalMetadataCompared=bool(n['Originals']),
                   GpuForegroundPixels=pixels, SyntheticPeer=True, LiveCompatibility=False)
    with Path(args.out).open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['expected', 'verify'])
    for key in ('out', 'asset-root', 'config', 'expected', 'native'):
        parser.add_argument('--' + key)
    parser.add_argument('--gpu', action='store_true')
    parser.add_argument('--native-exit', action='store_true')
    parser.add_argument('--native-free', action='store_true')
    args = parser.parse_args()
    expected(args) if args.mode == 'expected' else verify(args)
