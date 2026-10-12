"""Build a private Windows x64/D3D12 offline original-character package; no sockets or legacy executable."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pe_x64(path):
    with path.open('rb') as stream:
        if stream.read(2) != b'MZ':
            raise ValueError('Expected PE executable')
        stream.seek(0x3c)
        offset = struct.unpack('<I', stream.read(4))[0]
        stream.seek(offset)
        if stream.read(4) != b'PE\0\0' or struct.unpack('<H', stream.read(2))[0] != 0x8664:
            raise ValueError('Exported executable is not AMD64')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--terrain', action='store_true', help='Export bounded original-resource Tatooine base terrain roaming entry')
    parser.add_argument('--mos-taike', action='store_true', help='Export original bounded Mos Taike district entry')
    for name in ('dotnet', 'godot', 'packages', 'templates', 'asset-root', 'config', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    output.relative_to((ROOT / 'local-private').resolve())
    if output.exists():
        raise ValueError('Fresh private output required')
    paths = {name: getattr(args, name.replace('-', '_')).resolve(strict=True)
             for name in ('dotnet', 'godot', 'packages', 'templates', 'asset-root', 'config')}
    for path in paths.values():
        if any(p.is_symlink() or getattr(p.stat(), 'st_file_attributes', 0) & 0x400 for p in (path, *path.parents)):
            raise ValueError('Local non-reparse inputs required')
    template = paths['templates'] / 'windows_release_x86_64.exe'
    pe_x64(template)
    version = subprocess.check_output([str(paths['godot']), '--version'], text=True).strip()
    if not version.startswith('4.7.2.stable.mono.'):
        raise ValueError('The checked-in SDK requires Godot 4.7.2 stable .NET')
    # Existing independent oracle derives effective original template/SAT/idle/walk/run identities.
    spec = importlib.util.spec_from_file_location('installed_peer', ROOT / 'poc/round84-tests/Assets/peer.py')
    peer = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(peer)
    expected = peer.installed_expected(paths['asset-root'], paths['config'])
    profile = dict(Version=1, AssetRoot=str(paths['asset-root']), ConfigurationPath=str(paths['config']),
                   ObjectPath=peer.OBJECT,
                   Idle=dict(TablePath=peer.TABLE, LogicalName=peer.IDLE),
                   Walk=dict(TablePath=peer.TABLE, LogicalName='loop_standing', ExplicitChildRoute=[0, 1]),
                   Run=dict(TablePath=peer.TABLE, LogicalName='loop_standing', ExplicitChildRoute=[0, 2]))
    output.mkdir(parents=True)
    stage = output / 'source'
    listing = subprocess.check_output(['git', '-C', str(ROOT), 'ls-files', '-co', '--exclude-standard', '-z']).decode().split('\0')
    sources = {}
    for name in sorted(set(listing)):
        if not name.startswith(('poc/Stardust.Core/', 'poc/Stardust.Godot/')) or any(
                part in ('bin', 'obj', '.godot') for part in Path(name).parts):
            continue
        original = ROOT / name
        target = stage / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(original, target)
        sources[name] = digest(original)
    project = stage / 'poc/Stardust.Godot'
    package = output / 'client'
    package.mkdir()
    (package / 'character-launch.json').write_text(json.dumps(profile, indent=2) + '\n', encoding='utf-8')
    (output / 'original-preparation.json').write_text(json.dumps(expected, indent=2) + '\n')
    cache = output / 'packages'
    shutil.copytree(paths['packages'], cache)
    nupkgs = paths['godot'].parent / 'GodotSharp/Tools/nupkgs'
    nuget = '<configuration><packageSources><clear/><add key="bundled" value="' + escape(str(nupkgs), {'"': '&quot;'}) + '"/></packageSources></configuration>\n'
    config = output / 'NuGet.Config'
    config.write_text(nuget)
    appdata = output / 'appdata'
    (appdata / 'NuGet').mkdir(parents=True)
    (appdata / 'NuGet/NuGet.Config').write_text(nuget)
    env = os.environ.copy()
    env.update(APPDATA=str(appdata), DOTNET_CLI_HOME=str(output / 'dotnet-home'),
               DOTNET_ROOT=str(paths['dotnet'].parent), NUGET_PACKAGES=str(cache),
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_TELEMETRY_OPTOUT='1',
               DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', MSBUILDDISABLENODEREUSE='1',
               PATH=str(paths['dotnet'].parent) + os.pathsep + env['PATH'])
    template_path = str(template).replace('\\', '/')
    (project / 'export_presets.cfg').write_text('''[preset.0]
name="Windows x64 DX12 Offline"
platform="Windows Desktop"
runnable=true
advanced_options=false
dedicated_server=false
custom_features="''' + ('stardust_offline_mos_taike' if args.mos_taike else 'stardust_offline_terrain' if args.terrain else 'stardust_offline_character') + '''"
export_filter="all_resources"
include_filter=""
exclude_filter=""
export_path=""
script_export_mode=2

[preset.0.options]
custom_template/release="''' + template_path + '''"
binary_format/architecture="x86_64"
binary_format/embed_pck=false
debug/export_console_wrapper=1
dotnet/include_scripts_content=false
dotnet/include_debug_symbols=false
dotnet/embed_build_outputs=false
''', encoding='utf-8')
    commands = []

    def run(command, label, timeout=240):
        with (output / (label + '.log')).open('w', encoding='utf-8') as log:
            result = subprocess.run([str(item) for item in command], cwd=project, env=env,
                                    stdout=log, stderr=subprocess.STDOUT, timeout=timeout)
        commands.append(dict(Label=label, ExitCode=result.returncode))
        (output / 'commands.json').write_text(json.dumps(commands, indent=2) + '\n')
        if result.returncode:
            raise RuntimeError(label + ' failed; inspect its private log')

    run([paths['dotnet'], 'build', project / 'Stardust.Godot.csproj', '--configfile', config,
         '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:TreatWarningsAsErrors=true', '--nologo'], 'build')
    run([paths['dotnet'], 'new', 'sln', '--name', 'Stardust.Godot'], 'solution')
    run([paths['dotnet'], 'sln', 'Stardust.Godot.sln', 'add', 'Stardust.Godot.csproj',
         '../Stardust.Core/Stardust.Core.csproj'], 'solution-projects')
    run([paths['godot'], '--headless', '--path', project, '--editor', '--import', '--quit'], 'import')
    run([paths['godot'], '--headless', '--path', project, '--export-release',
         'Windows x64 DX12 Offline', package / 'Stardust.Client.exe'], 'export', timeout=600)
    if 'ERROR:' in (output / 'export.log').read_text(encoding='utf-8'):
        raise RuntimeError('Godot export reported an error despite its exit code; inspect export.log')
    executable = package / 'Stardust.Client.exe'
    pe_x64(executable)
    if not (package / 'Stardust.Client.pck').is_file() or not list(package.glob('data_*')):
        raise RuntimeError('Complete executable/PCK/.NET data export missing')
    for name, sha in sources.items():
        if digest(ROOT / name) != sha:
            raise RuntimeError('Public source changed during packaging')
    (package / 'README.txt').write_text('Stardust Client - Windows x64 / Direct3D 12 offline character\n\n'
        'Double-click Stardust.Client.exe. The supplied original installation must remain at the paths\n'
        'in character-launch.json. Drag to orbit; wheel to zoom; R to reset; Play idle pauses animation.\n'
        'Keep the EXE, PCK and data directory together. .NET runtime is included. No SDK is needed.\n'
        'This build displays the original animated human with authored texture/palette defaults.\n'
        'Face generation, live login and general gameplay are outside this bounded launch.\n'
        'A real x64 Forward+ D3D12 device is mandatory; fallback is rejected.\n', encoding='utf-8')
    if args.terrain:
        (package / 'README.txt').write_text('Stardust Client - Windows x64 / Direct3D 12 offline terrain\n\n'
            'Double-click Stardust.Client.exe. Keep EXE, PCK, profile and data directory together. No SDK needed.\n'
            'WASD move; Shift run; right-drag orbit; wheel zoom; P pause; R reset; Escape close.\n'
            'Original Tatooine base height and original human/idle/walk/run assets, 256 x 256 metres.\n'
            'PARTIAL GENERATOR: later geographic height layers are omitted, so final planet elevations differ.\n'
            'Original base-family texture uses deterministic first entry; blending/flora/water pending.\n'
            'Offline; no socket or live login. Real x64 Forward+ D3D12 is required.\n', encoding='utf-8')
    files = {str(path.relative_to(package)): dict(Bytes=path.stat().st_size, Sha256=digest(path))
             for path in sorted(package.rglob('*')) if path.is_file()}
    receipt = dict(Version=1, Engine=version, ProcessTarget='AMD64', RenderingTarget='d3d12/forward_plus',
                   PackageFiles=files, PublicSources=sources,
                   ConfigurationSha256=digest(paths['config']), OriginalPreparation=expected,
                   ToolInputs={name: digest(path) for name, path in paths.items() if path.is_file()},
                   TemplateSha256=digest(template), RuntimeVerified=False)
    if args.mos_taike:
        (package / 'README.txt').write_text('Stardust - x64 / Direct3D12 offline Mos Taike\n\n'
            'Double-click Stardust.Client.exe. Keep EXE, PCK, profile and data directory together. No SDK needed.\n'
            'WASD move; Shift run; right-drag orbit; wheel zoom; P pause; R reset; Escape close.\n'
            'Original west/central Mos Taike district:160x160m, original height rules and snapshot placements.\n'
            'Original animated human idle/walk/run. Modern finite arithmetic/material policy; no original numeric parity claim.\n'
            'Conservative mesh obstacle boxes; buildings are closed exteriors. Interiors, flora, shader blending, live gameplay pending.\n'
            'Offline; no socket/login. Real x64 Forward+ D3D12 required. Original installation paths in character-launch.json.\n',encoding='utf-8')
        receipt['PackageFiles']={str(path.relative_to(package)):dict(Bytes=path.stat().st_size,Sha256=digest(path))
                                 for path in sorted(package.rglob('*')) if path.is_file()}
    receipt['Entry'] = 'original_mos_taike_district' if args.mos_taike else 'partial_original_terrain' if args.terrain else 'original_character'
    (output / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print('Built private x64 offline package: ' + str(executable))
    print('Runtime D3D12/animation verification is a separate gate; build receipt does not claim it.')


if __name__ == '__main__':
    main()
