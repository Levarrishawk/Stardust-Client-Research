"""Bounded complete offline regressions and an explicit original-resource join."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import stat
import sys

ROOT = Path(__file__).resolve().parents[2]
JOIN_SCRIPTS = {n: f'poc/round{n}-tests/Integration/run_selected_player.py' for n in (87, 88, 89)}
JOIN_SCRIPTS[90] = 'poc/round90-tests/Rendering/replay.py'
JOIN_SCRIPTS[91] = 'poc/round91-tests/Integration/replay.py'
JOIN_SCRIPTS[92] = 'poc/round92-tests/Integration/replay.py'
JOIN_SCRIPTS[93] = 'poc/round93-tests/Rendering/replay.py'
COMPONENT_TIMEOUT_SECONDS = 900


def helpers(root):
    path = root / 'tools/python/run_client_regressions.py'
    # Check the fixed bootstrap before importing any caller-tree source.
    for ancestor in (path, *path.parents):
        try:
            info = ancestor.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('Linked public runner rejected')
    if not path.is_file():
        raise ValueError('Reviewed public runner missing')
    spec = importlib.util.spec_from_file_location('validation_regressions', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def source_snapshot(root, runner):
    rows = []
    extensions = {'.cs', '.csproj', '.py', '.json', '.config', '.props', '.targets', '.tscn', '.godot'}
    for top in ('poc', 'tools/python'):
        for path in sorted((root / top).rglob('*')):
            relative = path.relative_to(root)
            if any(part in ('bin', 'obj', '__pycache__', '.godot', 'local-private') for part in relative.parts):
                continue
            if path.is_file() and path.suffix.lower() in extensions:
                runner.no_links(path)
                raw = path.read_bytes()
                rows.append({'path': relative.as_posix(), 'sha256': hashlib.sha256(raw).hexdigest(), 'bytes': len(raw)})
    dependency = root / 'tools/tre_crc_vectors.py'
    runner.no_links(dependency)
    if dependency.is_file():
        raw = dependency.read_bytes()
        rows.append({'path': 'tools/tre_crc_vectors.py', 'sha256': hashlib.sha256(raw).hexdigest(), 'bytes': len(raw)})
    return rows


def read_json(path, runner):
    runner.no_links(path)
    return json.loads(path.read_text(encoding='utf-8-sig'))


def require_core(receipt, expected):
    if receipt.get('status') != 'passed' or receipt.get('omittedDefaults') != []:
        raise ValueError('Complete current default regressions required')
    suites = receipt.get('suites')
    if type(suites) is not list or [s.get('id') for s in suites] != expected or any(s.get('status') != 'passed' for s in suites):
        raise ValueError('Every required default suite must actually pass')
    if receipt.get('liveCompatibilityVerified') is not False:
        raise ValueError('Offline evidence boundary required')


def require_join(receipt, gpu):
    if receipt.get('Passed') is not True or receipt.get('LiveCompatibility') is not False:
        raise ValueError('Actual offline joined verification required')
    if type(receipt.get('SourceHashes')) is not dict or not receipt['SourceHashes']:
        raise ValueError('Joined source identities required')
    commands = receipt.get('Commands')
    if type(commands) is not list or not commands or any(c.get('status') != 'passed' for c in commands):
        raise ValueError('Joined child commands must all pass')
    cases = receipt.get('Receipts')
    if type(cases) is not list or any(c.get('Passed') is not True or c.get('Clean') is not True or
                                    c.get('OriginalResources') is not True for c in cases):
        raise ValueError('Joined cases require successful verification and cleanup')
    required = {'success', 'cancel', 'terrain-failure', 'send-error', 'send-timeout'}
    if not required.issubset({c.get('Mode') for c in cases}):
        raise ValueError('Accepted joined success and negative cases required')
    screenshots = [c for c in cases if c.get('Screenshot') is True]
    if gpu and not any(c.get('Mode') == 'success' for c in screenshots):
        raise ValueError('Requested GPU success evidence missing')
    if not gpu and screenshots:
        raise ValueError('Unexpected GPU evidence in headless request')


def verify_join_sources(receipt, root, runner):
    for relative, pin in receipt['SourceHashes'].items():
        if type(relative) is not str or '\\' in relative or ':' in relative or not relative.startswith(('poc/', 'tools/')):
            raise ValueError('Exact public joined source path required')
        if any(part in ('', '.', '..', 'local-private', '.git') for part in relative.split('/')):
            raise ValueError('Joined source escape rejected')
        path = root / relative
        runner.no_links(path)
        if not path.is_file() or type(pin) is not str or hashlib.sha256(path.read_bytes()).hexdigest() != pin:
            raise ValueError('Joined source identity mismatch')


def require_standalone_join(receipt, gpu):
    """Round93 reports intentional operation/cleanup faults as observed outcomes."""
    if (receipt.get('Passed') is not True or receipt.get('LiveCompatibility') is not False or
            receipt.get('SyntheticOffline') is not True or receipt.get('SocketCreated') is not False or
            receipt.get('RealUdpBranch') != 'source-reviewed-unexecuted'):
        raise ValueError('Standalone offline transport evidence required')
    if not receipt.get('SourceHashes') or not receipt.get('Commands') or any(
            c.get('ExpectedOutcomePassed') is not True or c.get('timedOut') is not False or
            c.get('ExpectedExitCode') not in (0, 1) or c.get('exitCode') != c.get('ExpectedExitCode') or
            c.get('status') != ('passed' if c.get('ExpectedExitCode') == 0 else 'failed')
            for c in receipt['Commands']):
        raise ValueError('Standalone child commands and identities required')
    cases = receipt.get('Receipts', [])
    modes = [c.get('Mode') for c in cases]
    identities = [c.get('Case', c.get('Mode')) for c in cases]
    required = {'success', 'movement-disabled', 'list-phase-timeout', 'readiness-phase-timeout',
                'overall-timeout', 'startup-rollback', 'startup-rollback-fault', 'send-error',
                'cleanup-fault', 'diagnostic-write-failure', 'cancel', 'window-close'}
    if not required.issubset(modes) or len(identities) != len(set(identities)):
        raise ValueError('Standalone required bounded outcomes missing or duplicated')
    for case in cases:
        mode = case['Mode']
        if case.get('LocalDrainCompleted') is not True:
            raise ValueError('Actual standalone ownership completion required')
        if mode == 'diagnostic-write-failure':
            if case.get('DiagnosticFailed') is not True or case.get('Outcome') != 'Finalized':
                raise ValueError('Diagnostic failure must preserve finalized run outcome')
            continue
        expected = {'startup-rollback': 'StartupRejected', 'startup-rollback-fault': 'RollbackFault',
                    'send-error': 'RunFault', 'cleanup-fault': 'CleanupFault'}.get(mode, 'Finalized')
        if case.get('SyntheticOffline') is not True or case.get('Outcome') != expected:
            raise ValueError('Standalone typed outcome mismatch')
        if mode.startswith('startup-'):
            if (case.get('StartupRollbackCompleted') is not True or
                    case.get('StartupRollbackSucceeded') != (mode == 'startup-rollback')):
                raise ValueError('Actual standalone rollback outcome required')
        elif (case.get('ApplicationFinalized') is not True or
              case.get('LocalDrainSucceeded') != (mode != 'cleanup-fault')):
            raise ValueError('Actual standalone drain outcome required')
        if mode == 'send-error' and (case.get('OperationTaskSucceeded') is not False or
                                     case.get('CleanupFailureTypes') != []):
            raise ValueError('Operation failure conflated with cleanup')
        if mode in ('success', 'movement-disabled') and case.get('PlayerPublished') is not True:
            raise ValueError('Actual original player publication required')
    if receipt.get('DirectStartupCases') != 4 or receipt.get('Injected92', {}).get('Passed') is not True:
        raise ValueError('Default refusal and injected API compatibility required')
    evidence = receipt.get('GPU', {})
    if gpu and not (evidence.get('Requested') is True and evidence.get('Accepted') is True and
                    any(c.get('GPU') is True and c.get('Mode') == 'success' for c in cases)):
        raise ValueError('Requested standalone GPU evidence missing')
    if not gpu and evidence.get('Accepted') is True:
        raise ValueError('Unexpected standalone GPU evidence')


def execute(args, root=ROOT):
    runner = helpers(root)
    if type(args.join_round) is not int or args.join_round not in JOIN_SCRIPTS or type(args.gpu) is not bool:
        raise ValueError('Exact reviewed join round and GPU flag required')
    paths = {name: runner.local_path(getattr(args, name)) for name in
             ('dotnet', 'python', 'godot', 'packages', 'asset_root', 'config')}
    for name, path in paths.items():
        runner.no_links(path)
        if not (path.is_dir() if name in ('packages', 'asset_root') else path.is_file()):
            raise ValueError('Required explicit local input missing: ' + name)
    output = runner.fresh_output(args.output, root)
    core_script = root / 'tools/python/run_client_regressions.py'
    join_script = root / JOIN_SCRIPTS[args.join_round]
    for path in (core_script, join_script):
        runner.no_links(path)
        if not path.is_file():
            raise ValueError('Reviewed public entry missing: ' + path.relative_to(root).as_posix())
    sources = source_snapshot(root, runner)
    expected = [r['id'] for r in runner.DEFAULT_PROJECTS] + [r[0] for r in runner.PYTHON_SUITES]
    output.mkdir(parents=True)
    (output / 'source-manifest.json').write_text(json.dumps(sources, indent=2) + '\n', encoding='utf-8')
    config = output / 'appdata/NuGet/NuGet.Config'
    config.parent.mkdir(parents=True)
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n', encoding='utf-8')
    env = os.environ.copy()
    env.update(APPDATA=str(output / 'appdata'), DOTNET_CLI_HOME=str(output / 'dotnet-home'),
               NUGET_PACKAGES=str(output / 'packages'), PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1',
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', MSBUILDDISABLENODEREUSE='1')
    report = dict(schema=1, status='running', startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  joinRound=args.join_round, commands=[], components={'core': 'not-run', 'join': 'not-run'},
                  gpu={'requested': args.gpu, 'status': 'not-run' if args.gpu else 'skipped-unverified', 'accepted': False},
                  requiredDefaults=expected, liveCompatibilityVerified=False, sourceHashesUnchanged=False)
    def save():
        (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    def unchanged():
        if source_snapshot(root, runner) != sources:
            raise ValueError('Public source identities changed during validation')
    def call(name, command):
        report['components'][name] = 'running'
        save()
        receipt = runner.run_command(command, root, env, output / (name + '.log'), COMPONENT_TIMEOUT_SECONDS)
        report['commands'].append(receipt)
        save()
        if receipt['status'] != 'passed':
            raise RuntimeError(name + ': ' + receipt['status'])
    try:
        call('core', [str(paths['python']), str(core_script), '--dotnet', str(paths['dotnet']), '--python', str(paths['python']),
                      '--packages', str(paths['packages']), '--output', str(output / 'core')])
        require_core(read_json(output / 'core/report.json', runner), expected)
        unchanged()
        report['components']['core'] = 'passed'
        command = [str(paths['python']), str(join_script)]
        for flag, key in (('dotnet', 'dotnet'), ('godot', 'godot'), ('packages', 'packages'), ('asset-root', 'asset_root'), ('config', 'config')):
            command += ['--' + flag, str(paths[key])]
        if args.join_round in (90, 91):
            pacer = root / 'poc/Stardust.Core/Session/ReplacementControlledMovement.cs'
            runner.no_links(pacer)
            command += ['--movement-source', str(pacer), '--movement-sha256', hashlib.sha256(pacer.read_bytes()).hexdigest()]
        if args.join_round == 92:
            dependencies = ('poc/Stardust.Core/Configuration/ReplacementControlledLaunch.cs',
                            'poc/Stardust.Core/Session/ReplacementControlledAttempt.cs',
                            'poc/Stardust.Core/Session/ReplacementEndpointApproval.cs')
            pins = {}
            for relative in dependencies:
                dependency = root / relative
                runner.no_links(dependency)
                pins[relative] = hashlib.sha256(dependency.read_bytes()).hexdigest()
            pin_file = output / 'reviewed-runtime92-pins.json'
            pin_file.write_text(json.dumps(pins, indent=2) + '\n', encoding='utf-8')
            command += ['--runtime-root', str(root), '--runtime-pins', str(pin_file)]
        command += ['--output', str(output / 'join')]
        if args.join_round == 93:
            command += ['--python', str(paths['python'])]
        if args.gpu:
            command += ['--gpu']
        call('join', command)
        joined = read_json(output / 'join/verification.json', runner)
        if args.join_round == 93:
            require_standalone_join(joined, args.gpu)
        else:
            require_join(joined, args.gpu)
        verify_join_sources(joined, root, runner)
        unchanged()
        report['components']['join'] = 'passed'
        report['sourceHashesUnchanged'] = True
        if args.gpu:
            report['gpu'].update(status='passed', accepted=True)
        report['status'] = 'passed'
        return 0
    except Exception as error:
        report['status'] = 'failed'
        report['failure'] = type(error).__name__ + ': ' + str(error)
        for name, status in report['components'].items():
            if status == 'running':
                report['components'][name] = 'failed'
        return 1
    finally:
        report['completedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        save()
        print(report['status'].upper() + ' report=' + str(output / 'report.json'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('dotnet', 'python', 'godot', 'packages', 'asset-root', 'config', 'output'):
        parser.add_argument('--' + name, required=True)
    parser.add_argument('--gpu', action='store_true')
    parser.add_argument('--join-round', type=int, choices=(87, 88, 89, 90, 91, 92, 93), default=93)
    try:
        return execute(parser.parse_args())
    except (ValueError, OSError) as error:
        print('REJECTED ' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
