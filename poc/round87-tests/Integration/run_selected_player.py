"""Explicit original-installation offline join; authored injected peers, no sockets."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]


def main():
    spec = importlib.util.spec_from_file_location('bounded_runner', ROOT / 'tools/python/run_client_regressions.py')
    runner = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(runner)
    p = argparse.ArgumentParser()
    for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config', 'output'):
        p.add_argument('--' + name, required=True)
    p.add_argument('--gpu', action='store_true')
    a = p.parse_args()
    paths = {name: runner.local_path(getattr(a, name.replace('-', '_'))) for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config')}
    for path in paths.values():
        runner.no_links(path)
        if not path.exists():
            raise ValueError('Explicit input missing')
    out = runner.fresh_output(a.output, ROOT)
    out.mkdir(parents=True)
    stage = out / 'source'
    listing = subprocess.check_output(['git', '-C', str(ROOT), 'ls-files', '-co', '--exclude-standard', '-z']).decode().split('\0')
    hashes = {}
    prefixes = ('poc/Stardust.Core/', 'poc/Stardust.Godot/', 'tools/',
                'poc/round14-tests/Networking/', 'poc/round15-tests/Networking/', 'poc/round83-tests/Runtime/', 'poc/round83-tests/Assets/',
                'poc/round85-tests/Networking/', 'poc/round84-tests/Networking/', 'poc/round25-tests/Networking/', 'poc/round32-tests/Networking/', 'poc/round38-tests/Networking/', 'poc/round41-tests/Networking/', 'poc/round42-tests/Runtime/', 'poc/round37-tests/Networking/', 'poc/round84-tests/Assets/', 'poc/round87-tests/Integration/', 'poc/round86-tests/Networking/', 'poc/round87-tests/Networking/')
    for name in sorted(set(listing)):
        if not name.startswith(prefixes) or any(part in ('bin', 'obj', '.godot', '__pycache__') for part in Path(name).parts):
            continue
        source = ROOT / name
        runner.no_links(source)
        if not source.is_file():
            raise ValueError('Source missing')
        raw = source.read_bytes()
        hashes[name] = hashlib.sha256(raw).hexdigest()
        target = stage / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)
    project = stage / 'poc/Stardust.Godot'
    for name in ('NativeSessionVerification.cs', 'NativeSessionVerification.tscn'):
        raw = (Path(__file__).parent / name).read_bytes()
        hashes['poc/round87-tests/Integration/' + name] = hashlib.sha256(raw).hexdigest()
        (project / name).write_bytes(raw)
    observation = stage / 'poc/round85-tests/Networking/Observation.cs'
    # The emitter only projects observed values; Python owns the expectations.
    (project / 'Networking85Observation.cs').write_bytes(observation.read_bytes())
    (project / 'Networking86Observation.cs').write_bytes((stage / 'poc/round86-tests/Networking/Observation.cs').read_bytes())
    packages = out / 'packages'
    for child in paths['packages'].rglob('*'):
        runner.no_links(child)
    shutil.copytree(paths['packages'], packages)
    sdk_source = paths['godot'].parent / 'GodotSharp/Tools/nupkgs'
    if not sdk_source.is_dir():
        raise ValueError('Bundled Godot SDK packages missing')
    config = out / 'NuGet.Config'
    from xml.sax.saxutils import escape
    config.write_text('<configuration><packageSources><clear/><add key="bundled" value="' + escape(str(sdk_source), {'"': '&quot;'}) + '"/></packageSources></configuration>\n')
    appdata = out / 'appdata/NuGet'
    appdata.mkdir(parents=True)
    shutil.copyfile(config, appdata / 'NuGet.Config')
    env = os.environ.copy()
    env.update(APPDATA=str(out / 'appdata'), DOTNET_CLI_HOME=str(out / 'dotnet-home'), NUGET_PACKAGES=str(packages),
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               MSBUILDDISABLENODEREUSE='1', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1')
    commands = []
    (out / 'source-manifest.json').write_text(json.dumps(hashes, indent=2) + '\n')

    def run(command, name):
        result = runner.run_command([str(x) for x in command], stage, env, out / (name + '.log'), 180)
        commands.append(result)
        (out / 'commands.json').write_text(json.dumps(commands, indent=2) + '\n')
        if result['status'] != 'passed':
            raise RuntimeError(name + ': ' + result['status'])

    object_path = 'object/creature/player/shared_human_male.iff'
    # Use the independently authored preparation peer's CRC implementation, not Core.
    peer_spec = importlib.util.spec_from_file_location('installation_peer', stage / 'poc/round84-tests/Assets/peer.py')
    peer = importlib.util.module_from_spec(peer_spec)
    peer_spec.loader.exec_module(peer)
    template_crc = peer.preflight.crc(object_path.encode())
    run([sys.executable, stage / 'poc/round84-tests/Assets/peer.py', 'generate', stage / 'local-private/installation-oracle',
         '--asset-root', paths['asset-root'], '--config', paths['config']], 'installation-oracle')
    expected_preparation = json.loads((stage / 'local-private/installation-oracle/fixtures.json').read_text())['Original']['Expected']
    run([sys.executable, stage / 'poc/round87-tests/Networking/produce.py',
         '--template-crc', hex(template_crc), '--object-id', '0x1122334455667788',
         '--object-path', object_path, '--terrain-path', 'terrain/tatooine.trn',
         '--output', stage / 'local-private/transcript'], 'producer')
    transcript = stage / 'local-private/transcript/transcript.json'
    run([paths['dotnet'], 'build', project / 'Stardust.Godot.csproj', '--configfile', config,
         '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:UseAppHost=false', '-p:TreatWarningsAsErrors=true', '--nologo'], 'build')
    run([paths['godot'], '--headless', '--path', project, '--editor', '--import', '--quit'], 'import')
    modes = [('success-headless', 'success', True), ('cancel-headless', 'cancel', True), ('terrain-failure-headless', 'terrain-failure', True), ('send-error-headless','send-error',True), ('send-timeout-headless','send-timeout',True)]
    if a.gpu:
        modes.append(('success-gpu', 'success', False))
    receipts = []
    for name, mode, headless in modes:
        target = out / name
        command = [paths['godot']]
        if headless:
            command += ['--headless']
        command += ['--path', project, 'res://NativeSessionVerification.tscn', '--', paths['asset-root'], paths['config'], transcript, target, mode]
        run(command, name)
        receipt = json.loads((target / 'receipt.json').read_text())
        if receipt['Passed'] is not True:
            raise ValueError('Joined verification rejected')
        if mode in ('success','send-error','send-timeout'):
            run([sys.executable, stage / 'poc/round87-tests/Networking/verify.py', '--transcript', transcript, '--result', target / 'observations.json', '--output', stage / 'local-private' / (name + '-independent-state.json')], name + '-independent-state')
            resources = json.loads((target / 'resources.json').read_text())
            if resources['Preparation'] != expected_preparation:
                raise ValueError('Independent original preparation mismatch')
            sys.path.insert(0, str(stage / 'tools/python'))
            from verify_textured_mesh_bundle import Index
            from pst_reference_audit import configured_archives
            import contextlib
            with contextlib.ExitStack() as stack:
                indexes = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in
                           configured_archives(paths['asset-root'], paths['config'])]
                def read(logical):
                    for priority, index in indexes:
                        value = index.lookup(logical)
                        if value is not None:
                            return value, priority
                    raise ValueError('Original source missing in independent audit')
                for logical, identity in resources['ClosureSources'].items():
                    raw, priority = read(logical)
                    if identity != dict(Path=logical, Priority=priority, Bytes=len(raw), Sha256=hashlib.sha256(raw).hexdigest().upper()):
                        raise ValueError('Independent loaded resource identity mismatch')
                terrain, _ = read('terrain/tatooine.trn')
                if resources['TerrainSha256'] != hashlib.sha256(terrain).hexdigest().upper():
                    raise ValueError('Independent terrain identity mismatch')
        elif mode != 'malformed-correction' and json.loads((target / 'captured.json').read_text()).get('ReadyWire') is not None:
            raise ValueError('Negative case emitted ready')
        receipts.append(receipt)
    (out / 'verification.json').write_text(json.dumps(dict(Passed=True, SourceHashes=hashes, Commands=commands,
        Receipts=receipts, LiveCompatibility=False, FullTerrainReadiness=False), indent=2) + '\n')
    print(json.dumps(dict(Passed=True, Cases=len(receipts), GPU=a.gpu, Output=str(out))))


if __name__ == '__main__':
    main()
