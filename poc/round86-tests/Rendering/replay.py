"""Private frozen-source native selected-player adapter component runner."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import runpy
import shutil
import sys

ROOT = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser()
    for key in ('dotnet', 'godot', 'packages', 'output'):
        parser.add_argument('--' + key, required=True)
    parser.add_argument('--asset-root')
    parser.add_argument('--config')

    parser.add_argument('--native-exit', action='store_true')
    parser.add_argument('--native-free', action='store_true')
    args = parser.parse_args()
    args.gpu = False
    if not args.asset_root or not args.config:
        parser.error('Motion adapter proof requires explicit original installation inputs')
    if args.native_exit and args.native_free:
        parser.error('Native removal and native-free abort are alternatives')
    if (args.native_exit or args.native_free) and not args.asset_root:
        parser.error('Original lifecycle proof requires explicit installation inputs')
    helpers = runpy.run_path(str(ROOT / 'tools/python/run_client_regressions.py'))
    local, safe = helpers['local_path'], helpers['no_links']
    output = helpers['fresh_output'](args.output, ROOT)
    dotnet, godot, packages = [local(v) for v in (args.dotnet, args.godot, args.packages)]
    for exe in (dotnet, godot):
        safe(exe)
        if not exe.is_file():
            raise ValueError('Required native runtime missing')
    if bool(args.asset_root) != bool(args.config):
        raise ValueError('Both explicit installation inputs required')
    if args.asset_root:
        for value in (args.asset_root, args.config):
            safe(local(value))
    output.mkdir(parents=True)
    stage = output / 'workspace'
    source_manifest = helpers['stage_sources'](ROOT, stage)
    (output / 'source-manifest.json').write_text(json.dumps(source_manifest, indent=2) + '\n')
    # Source staging uses only exact files, never broad Core globs including generated obj.
    core = stage / 'poc/Stardust.Core'
    project = output / 'native/Player'
    project.mkdir(parents=True)
    source_receipts = []
    for source in sorted((stage / 'poc/Stardust.Godot').glob('*.cs')):
        shutil.copyfile(source, project / source.name)
        source_receipts.append({'path': source.relative_to(stage).as_posix(), 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
    for name in ('PlayerVerification.cs', 'Player.csproj', 'Player.tscn', 'project.godot'):
        shutil.copyfile(Path(__file__).parent / name, project / name)
    # csproj relative reference targets a source-only private copy.
    shutil.copytree(core, project.parent / 'Stardust.Core')
    config = output / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
    for name in ('bcnencoder.net', 'communitytoolkit.highperformance', 'godot.net.sdk', 'godot.sourcegenerators', 'godotsharp', 'godotsharpeditor'):
        folder = packages / name
        safe(folder)
        for child in folder.rglob('*'):
            safe(child)
        shutil.copytree(folder, output / 'packages' / name)
    env = os.environ.copy()
    env.update(APPDATA=str(output / 'appdata'), DOTNET_CLI_HOME=str(output / 'dotnet-home'), NUGET_PACKAGES=str(output / 'packages'),
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
               PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1', MSBUILDDISABLENODEREUSE='1')
    nuget = output / 'appdata/NuGet/NuGet.Config'
    nuget.parent.mkdir(parents=True)
    shutil.copyfile(config, nuget)
    report = {'Passed': False, 'Gpu': args.gpu, 'OriginalRequested': bool(args.asset_root), 'Commands': [], 'Sources': source_receipts}
    def call(label, command, timeout=120):
        receipt = helpers['run_command'](command, output, env, output / (label + '.log'), timeout)
        report['Commands'].append(receipt)
        if receipt['status'] != 'passed':
            raise RuntimeError(label + ' ' + receipt['status'])
    try:
        peer = output / 'peer-before-native.json'
        call('peer', [sys.executable, str(Path(__file__).with_name('peer.py')), str(peer)])
        expectation = output / 'expected-before-native.json'
        command = [sys.executable, str(Path(__file__).with_name('verify.py')), 'expected', '--out', str(expectation)]
        if args.gpu:
            command += ['--gpu']
        if args.asset_root:
            command += ['--asset-root', str(local(args.asset_root)), '--config', str(local(args.config))]
        if args.native_exit:
            command += ['--native-exit']
        if args.native_free:
            command += ['--native-free']
        call('expected', command)
        call('build', [str(dotnet), 'build', str(project / 'Player.csproj'), '--artifacts-path', str(output / 'artifacts'), '--configfile', str(config),
                       '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '--nologo'])
        call('import', [str(godot), '--headless', '--editor', '--path', str(project), '--import'])
        command = [str(godot)] + ([] if args.gpu else ['--headless']) + ['--path', str(project), '--rendering-method', 'gl_compatibility', '--', '--peer', str(peer), '--out', str(output / 'native.json')]
        if args.asset_root:
            command += ['--asset-root', str(local(args.asset_root)), '--config', str(local(args.config))]
        if args.native_exit:
            command += ['--native-exit']
        if args.native_free:
            command += ['--native-free']
        call('native', command)
        call('verify', [sys.executable, str(Path(__file__).with_name('verify.py')), 'verify', '--expected', str(expectation),
                        '--native', str(output / 'native.json'), '--out', str(output / 'independent-receipt.json')])
        native = json.loads((output / 'native.json').read_text())
        assert native['Passed'] and native['Gpu'] is args.gpu and native['ArchiveCalls'] == native['ArchivesClosed']
        assert len(native['Originals']) == int(bool(args.asset_root))
        native_log = (output / 'native.log').read_text()
        assert 'leaked' not in native_log.lower() and 'still in use' not in native_log.lower()
        assert 'parent node is busy' not in native_log.lower() and 'busy setting up children' not in native_log.lower() and 'failed method' not in native_log.lower()
        assert peer.stat().st_mtime < (output / 'native.json').stat().st_mtime
        if args.asset_root:
            original = native['Originals'][0]
            assert original['ObjectId'] == 42 and original['Position'] == [1.5, 2, 3] and original['Published'] is True and original['TextureCount'] > 0
        report.update(Passed=True, Native=native, PeerSha256=hashlib.sha256(peer.read_bytes()).hexdigest(), IndependentApplicationsBeforeNative=True,
                      OriginalExecutable=False, LiveCompatibility=False)
        print('PASS selected-player component checks=' + str(native['Checks']))
    finally:
        (output / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')


if __name__ == '__main__':
    main()
