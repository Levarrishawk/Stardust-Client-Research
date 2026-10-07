"""Explicit original-resource production-host controls with authored offline peers; no physical OS input."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]


def main():
    spec = importlib.util.spec_from_file_location('bounded_runner', ROOT / 'tools/python/run_client_regressions.py')
    runner = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(runner)
    p = argparse.ArgumentParser()
    for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config', 'output', 'runtime-root', 'runtime-pins'):
        p.add_argument('--' + name, required=True)
    p.add_argument('--gpu', action='store_true')
    a = p.parse_args()
    paths = {name: runner.local_path(getattr(a, name.replace('-', '_'))) for name in ('dotnet', 'godot', 'packages', 'asset-root', 'config', 'runtime-root', 'runtime-pins')}
    for path in paths.values():
        runner.no_links(path)
        if not path.exists():
            raise ValueError('Explicit input missing')
    expected_paths = {'poc/Stardust.Core/Configuration/ReplacementControlledLaunch.cs', 'poc/Stardust.Core/Session/ReplacementControlledAttempt.cs', 'poc/Stardust.Core/Session/ReplacementEndpointApproval.cs'}
    borrowed = json.loads(paths['runtime-pins'].read_text())
    # The public consolidator pins current reviewed Core sources explicitly.
    # Worker delivery seals remain private lead evidence, not launch inputs.
    pins_raw = paths['runtime-pins'].read_bytes()
    if set(borrowed) != expected_paths:
        raise ValueError('Exact reviewed Runtime92 pins required')
    dependency_bytes = {}
    for name, pin in borrowed.items():
        source = paths['runtime-root'] / name
        runner.no_links(source)
        raw = source.read_bytes()
        if hashlib.sha256(raw).hexdigest() != pin:
            raise ValueError('Runtime92 source pin mismatch')
        dependency_bytes[name] = raw
    out = runner.fresh_output(a.output, ROOT)
    out.mkdir(parents=True)
    stage = out / 'source'
    listing = subprocess.check_output(['git', '-C', str(ROOT), 'ls-files', '-co', '--exclude-standard', '-z']).decode().split('\0')
    hashes = {}
    prefixes = ('poc/Stardust.Core/', 'poc/Stardust.Godot/', 'tools/',
                'poc/round14-tests/Networking/', 'poc/round15-tests/Networking/', 'poc/round83-tests/Runtime/', 'poc/round83-tests/Assets/',
                'poc/round85-tests/Networking/', 'poc/round84-tests/Networking/', 'poc/round25-tests/Networking/', 'poc/round32-tests/Networking/', 'poc/round38-tests/Networking/', 'poc/round41-tests/Networking/', 'poc/round42-tests/Runtime/', 'poc/round37-tests/Networking/', 'poc/round84-tests/Assets/', 'poc/round90-tests/Rendering/', 'poc/round92-tests/Integration/', 'poc/round86-tests/Networking/', 'poc/round87-tests/Networking/')
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
    for name in ('HostVerification.cs','HostVerification.tscn'):
        (project/name).write_bytes((stage/'poc/round90-tests/Rendering'/name).read_bytes())
    for name in ('BootstrapVerification.cs', 'BootstrapVerification.tscn'):
        raw = (Path(__file__).parent / name).read_bytes()
        hashes['poc/round92-tests/Integration/' + name] = hashlib.sha256(raw).hexdigest()
        (project / name).write_bytes(raw)
    for name, raw in dependency_bytes.items():
        target = stage / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)
    (out / 'borrowed-dependencies.json').write_text(json.dumps(borrowed, indent=2) + '\n', encoding='utf-8')
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
    run([sys.executable, stage / 'poc/round92-tests/Integration/produce.py',
         '--template-crc', hex(template_crc), '--object-id', '0x1122334455667788',
         '--object-path', object_path, '--terrain-path', 'terrain/tatooine.trn',
         '--output', stage / 'local-private/transcript'], 'producer')
    profile = dict(Version=1, AssetRoot=str(paths['asset-root']), ConfigurationPath=str(paths['config']),
                   ObjectPath=object_path,
                   Idle=dict(TablePath='appearance/lat/all_m.lat',LogicalName='emt_standing_idle_ambient_1_ag'),
                   Walk=dict(TablePath='appearance/lat/all_m.lat',LogicalName='loop_standing',ExplicitChildRoute=[0,1]),
                   Run=dict(TablePath='appearance/lat/all_m.lat',LogicalName='loop_standing',ExplicitChildRoute=[0,2]))
    generated = json.loads((stage/'local-private/transcript/transcript.json').read_text())
    profile = dict(Version=1, Profile=profile, ClientVersion='20260817-SD:01',
                   Login=dict(Hostname='login.invalid',Ipv4Address=0xC0000201,Port=44452),
                   Zone=dict(Hostname='zone.invalid',Ipv4Address=0xC0000202,Port=44453),
                   GalaxyId=7,ApprovedCharacterId=0x1122334455667788,LoginConnectionId=generated['LoginId'],ZoneConnectionId=generated['ZoneId'],
                   PhaseBudgetMilliseconds=30000,OverallBudgetMilliseconds=120000,DisconnectTimeoutMilliseconds=1000,
                   MinimumReadinessApproved=True,MovementPrerequisites=dict(FreshStampZero=True,MatchingValidatedPosition=True,MovementPrerequisitesValidated=True))
    profile_path = stage / 'local-private/controlled-launch.json'
    profile_path.write_text(json.dumps(profile,indent=2)+'\n',encoding='utf-8')
    profile_hash = hashlib.sha256(profile_path.read_bytes()).hexdigest().upper()
    transcript = stage / 'local-private/transcript/transcript.json'
    run([paths['dotnet'], 'build', project / 'Stardust.Godot.csproj', '--configfile', config,
         '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:UseAppHost=false', '-p:TreatWarningsAsErrors=true', '--nologo'], 'build')
    run([paths['godot'], '--headless', '--path', project, '--editor', '--import', '--quit'], 'import')
    modes = [(mode + '-headless', mode, True) for mode in ('success', 'cancel', 'terrain-failure', 'send-error', 'send-timeout', 'stop', 'window-close', 'native-exit', 'credentials-timeout', 'list-phase-timeout', 'readiness-phase-timeout', 'overall-timeout', 'startup-rollback', 'startup-rollback-fault', 'cleanup-fault')]
    if a.gpu:
        modes.append(('success-gpu', 'success', False))
    run([paths['godot'],'--headless','--path',project,'--quit-after','20','res://ReplacementClientBootstrap.tscn','--',profile_path], 'default-refusal')
    if 'bootstrap_state=RefusedNoTransport' not in (out/'default-refusal.log').read_text():
        raise ValueError('Direct scene refusal missing')
    for label,args in (('missing',[]),('relative',['relative.json']),('extra',[str(profile_path),'extra'])):
        run([paths['godot'],'--headless','--path',project,'--quit-after','20','res://ReplacementClientBootstrap.tscn','--',*args], 'invalid-'+label)
        if 'bootstrap_state=InvalidInput' not in (out/('invalid-'+label+'.log')).read_text():
            raise ValueError('Invalid direct startup not refused')
    receipts = []
    for name, mode, headless in modes:
        target = out / name
        command = [paths['godot']]
        if headless:
            command += ['--headless']
        command += ['--path', project, 'res://BootstrapVerification.tscn', '--', paths['asset-root'], paths['config'], transcript, target, mode, profile_path]
        run(command, name)
        receipt = json.loads((target / 'receipt.json').read_text())
        if receipt['Passed'] is not True:
            raise ValueError('Joined verification rejected')
        expected = json.loads(transcript.read_text())
        if not mode.startswith('startup-'):
            captured = json.loads((target / 'wire-digests.json').read_text())
            def digest(value):
                return hashlib.sha256(bytes.fromhex(value)).hexdigest().upper()
            if mode in ('success','stop','window-close','native-exit','cleanup-fault','send-error','send-timeout') and not {'AuthWire','SelectWire','ReadyWire','Movement0'}.issubset(captured):
                raise ValueError('Required protected emissions missing')
            for key, value in captured.items():
                if key.startswith('Movement'):
                    wanted = expected['ExpectedMovementWires'][int(key[len('Movement'):])]
                else:
                    wanted = expected['Expected' + key]
                if value != digest(wanted):
                    raise ValueError('Independent protected wire digest mismatch: ' + key)
            if mode in ('success','stop','window-close','native-exit','cleanup-fault','send-error','send-timeout'):
                count = 1 if mode == 'stop' else 3
                observations = json.loads((target / 'observations.json').read_text())
                if [struct.pack('<3f', *v) for v in observations['Positions']] != [struct.pack('<3f', *v) for v in expected['ExpectedActorLocalPositions'][:count]]:
                    raise ValueError('Actual actor positions mismatch')
                if len(observations['Times']) != count or any(b-a<400 for a,b in zip(observations['Times'],observations['Times'][1:])):
                    raise ValueError('Actual movement cadence mismatch')
                resources = json.loads((target / 'resources.json').read_text())
                if resources['ControlledLaunchSha256'] != profile_hash or resources['Preparation'] != expected_preparation:
                    raise ValueError('Independent original preparation/launch identity mismatch')
            if mode in ('credentials-timeout','list-phase-timeout') and receipt['ZoneFactoryCalls'] != 0:
                raise ValueError('Timeout acquired zone transport')
            if mode == 'terrain-failure' and not receipt['ObservedProviderFault']:
                raise ValueError('Provider fault not observed')
            if mode == 'cleanup-fault' and not receipt['ObservedCleanupFault']:
                raise ValueError('Cleanup fault not observed')
        receipts.append(receipt)
    legacy_profile=stage/'local-private/legacy-profile.json'
    legacy_profile.write_text(json.dumps(profile['Profile']))
    legacy_transcript=stage/'local-private/legacy-transcript'
    run([sys.executable,stage/'poc/round90-tests/Rendering/produce.py','--template-crc',hex(template_crc),'--object-id','0x1122334455667788','--object-path',object_path,'--terrain-path','terrain/tatooine.trn','--output',legacy_transcript], 'legacy-producer')
    legacy_receipts=[]
    for mode in ('success','cancel','terrain-failure','send-error','send-timeout','stop','window-close','native-exit'):
        target=out/('legacy-'+mode)
        run([paths['godot'],'--headless','--path',project,'res://HostVerification.tscn','--',paths['asset-root'],paths['config'],legacy_transcript/'transcript.json',target,mode,legacy_profile], 'legacy-'+mode)
        receipt=json.loads((target/'receipt.json').read_text())
        if not receipt['Passed']:
            raise ValueError('Legacy host regression')
        legacy_receipts.append(receipt)
    # Independently audit the installed bytes actually loaded by the success case.
    resources=json.loads((out/'success-headless/resources.json').read_text())
    sys.path.insert(0,str(stage/'tools/python'))
    from verify_textured_mesh_bundle import Index
    from pst_reference_audit import configured_archives
    import contextlib
    with contextlib.ExitStack() as stack:
        indexes=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in configured_archives(paths['asset-root'],paths['config'])]
        def read(logical):
            for priority,index in indexes:
                value=index.lookup(logical)
                if value is not None:
                    return value,priority
            raise ValueError('Original source absent')
        for logical,identity in resources['ClosureSources'].items():
            raw,priority=read(logical)
            if identity!=dict(Path=logical,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper()):
                raise ValueError('Original loaded closure mismatch')
        terrain,_=read('terrain/tatooine.trn')
        if resources['TerrainSha256']!=hashlib.sha256(terrain).hexdigest().upper():
            raise ValueError('Original terrain identity mismatch')
    for name, pin in hashes.items():
        if hashlib.sha256((ROOT / name).read_bytes()).hexdigest() != pin:
            raise ValueError('Public sources changed during replay')
    for name, pin in borrowed.items():
        if hashlib.sha256((paths['runtime-root']/name).read_bytes()).hexdigest() != pin:
            raise ValueError('Reviewed dependency changed during replay')
    (out / 'verification.json').write_text(json.dumps(dict(Passed=True, SourceHashes=hashes, BorrowedDependencies=borrowed, RuntimeInputPinsSha256=hashlib.sha256(pins_raw).hexdigest(), DefaultSceneRefused=True, Commands=commands,
        Receipts=receipts, LegacyReceipts=legacy_receipts, GPU=dict(Requested=a.gpu, Status='passed' if a.gpu else 'skipped-unverified', Accepted=a.gpu),
        LiveCompatibility=False, FullTerrainReadiness=False), indent=2) + '\n')
    print(json.dumps(dict(Passed=True, Cases=len(receipts), GPU=a.gpu, Output=str(out))))


if __name__ == '__main__':
    main()
