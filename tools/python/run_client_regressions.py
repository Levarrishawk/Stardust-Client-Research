"""Run a curated offline client regression set in a fresh private workspace."""
import argparse
import ctypes
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import stat
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PROJECTS = [
    {'id': 'core-smoke', 'project': 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj', 'args': []},
    {'id': 'runtime82-manifest', 'project': 'poc/round82-tests/Runtime/Runtime.csproj', 'args': ['{output}']},
    {'id': 'runtime83-session', 'project': 'poc/round83-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'assets83-preflight', 'project': 'poc/round83-tests/Assets/Assets.csproj', 'args': ['{fixture}', '{receipt}']},
    {'id': 'runtime84-scene-bridge', 'project': 'poc/round84-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'assets84-creature-preparation', 'project': 'poc/round84-tests/Assets/Assets.csproj', 'args': ['{fixture}', '{receipt}']},
    {'id': 'runtime85-selected-ingress', 'project': 'poc/round85-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime86-player-motion', 'project': 'poc/round86-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime87-disconnect', 'project': 'poc/round87-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime88-client-run', 'project': 'poc/round88-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime89-client-application', 'project': 'poc/round89-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime90-controlled-movement', 'project': 'poc/round90-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'runtime91-endpoint-approval', 'project': 'poc/round91-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'networking91-endpoint-handoff', 'project': 'poc/round91-tests/Networking/Networking.csproj', 'args': ['{fixture}']},
    {'id': 'runtime92-controlled-launch', 'project': 'poc/round92-tests/Runtime/Runtime.csproj', 'args': ['{fixture}']},
    {'id': 'assets88-launch-profile', 'project': 'poc/round88-tests/Assets/Assets.csproj', 'args': ['{fixture}', '{receipt}']},
]
PYTHON_SUITES = [
    ('runtime81-verifier', 'poc/round81-tests/Runtime/test_verifier.py', []),
    ('networking81-producer', 'poc/round81-tests/Networking/replay.py', ['--profile', '{profile}', '--output-dir', '{output}']),
    ('networking82-producer', 'poc/round82-tests/Networking/replay.py', ['--profile', '{profile}', '--output', '{output}']),
    ('networking83-authentication', 'poc/round83-tests/Networking/replay.py', ['--dotnet', '{dotnet}', '--output', '{output}']),
    ('networking84-injected-pump', 'poc/round84-tests/Networking/replay.py', ['--dotnet', '{dotnet}', '--packages', '{packages}', '--output', '{output}']),
    ('runner83-tests', 'poc/round83-tests/Rendering/test_runner.py', []),
]
OPTIONAL = [
    ('runtime80-terrain-restart', 'Requires explicitly supplied pinned runtime/terrain fixture recipes; not run by this command.'),
    ('networking80-native-restart', 'Historical replay hardcodes cache/runtime and tree build outputs; not invoked by this runner.'),
    ('rendering80-84-godot', 'Requires explicit Godot and original inputs; run the documented selected-player join separately. No GPU or Godot invocation here.'),
    ('original-installation', 'Original-resource suites need explicit installation and fixture inputs; no inferred historical vectors.'),
]


def local_path(value):
    if not value or '\0' in value or '://' in value or value.startswith(('\\\\', '//')):
        raise ValueError('Explicit local absolute path required')
    path = Path(value)
    if not path.is_absolute():
        raise ValueError('Explicit local absolute path required')
    return Path(os.path.abspath(path))


def no_links(path):
    for ancestor in (path, *path.parents):
        try:
            info = ancestor.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('Linked path rejected')


def fresh_output(value, root):
    path = local_path(value)
    if not path.is_relative_to(root / 'local-private') or path == root / 'local-private':
        raise ValueError('Fresh output beneath repository local-private required')
    no_links(path)
    if path.exists():
        raise ValueError('Fresh output required; existing output is never removed')
    for ancestor in path.parents:
        if ancestor.exists() and not ancestor.is_dir():
            raise ValueError('Non-directory output ancestor rejected')
    return path


def projects_from(path):
    if path is None:
        return [dict(row, args=list(row['args'])) for row in DEFAULT_PROJECTS]
    if path.stat().st_size > 65536:
        raise ValueError('Project list exceeds byte bound')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError('Duplicate project property')
            result[key] = value
        return result
    rows = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique)
    if type(rows) is not list or not 1 <= len(rows) <= 32:
        raise ValueError('One to 32 exact project entries required')
    ids = {s[0] for s in PYTHON_SUITES}
    for row in rows:
        if type(row) is not dict or set(row) != {'id', 'project', 'args'}:
            raise ValueError('Exact id/project/args project entry required')
        ident, project, args = row['id'], row['project'], row['args']
        if type(ident) is not str or not re.fullmatch('[a-z][a-z0-9-]{0,63}', ident) or ident in ids:
            raise ValueError('Unique bounded suite id required')
        ids.add(ident)
        if type(project) is not str or '\\' in project or ':' in project or '\0' in project or not project.startswith('poc/') or not project.endswith('.csproj') or any(p in ('', '.', '..') for p in project.split('/')):
            raise ValueError('Exact repository-relative poc project required')
        if type(args) is not list or len(args) > 64 or any(type(a) is not str or len(a) > 4096 or '\0' in a for a in args):
            raise ValueError('Bounded literal argument array required')
        placeholders = ('{output}', '{profile}', '{fixture}') if (ident, project) in (
            ('runtime83-session', 'poc/round83-tests/Runtime/Runtime.csproj'),
            ('runtime84-scene-bridge', 'poc/round84-tests/Runtime/Runtime.csproj'),
            ('runtime85-selected-ingress', 'poc/round85-tests/Runtime/Runtime.csproj'),
            ('runtime86-player-motion', 'poc/round86-tests/Runtime/Runtime.csproj'),
            ('runtime87-disconnect', 'poc/round87-tests/Runtime/Runtime.csproj'),
            ('runtime88-client-run', 'poc/round88-tests/Runtime/Runtime.csproj'),
            ('runtime89-client-application', 'poc/round89-tests/Runtime/Runtime.csproj'),
            ('runtime90-controlled-movement', 'poc/round90-tests/Runtime/Runtime.csproj'),
            ('runtime92-controlled-launch', 'poc/round92-tests/Runtime/Runtime.csproj'),
            ('networking91-endpoint-handoff', 'poc/round91-tests/Networking/Networking.csproj'),
            ('runtime91-endpoint-approval', 'poc/round91-tests/Runtime/Runtime.csproj')) else ('{output}', '{profile}')
        if (ident, project) in (('assets83-preflight', 'poc/round83-tests/Assets/Assets.csproj'),
                               ('assets84-creature-preparation', 'poc/round84-tests/Assets/Assets.csproj'),
                               ('assets88-launch-profile', 'poc/round88-tests/Assets/Assets.csproj')):
            placeholders = ('{fixture}', '{receipt}')
        if any('{' in a or '}' in a for a in args if a not in placeholders):
            raise ValueError('Only whole output/profile placeholders allowed')
    return rows


class WindowsJob:
    """A suspended child joins a kill-on-close Job before any test code executes."""
    def __init__(self):
        from ctypes import wintypes as w
        class Basic(ctypes.Structure):
            _fields_ = [('processTime', ctypes.c_longlong), ('jobTime', ctypes.c_longlong), ('flags', w.DWORD),
                        ('minWorking', ctypes.c_size_t), ('maxWorking', ctypes.c_size_t), ('processLimit', w.DWORD),
                        ('affinity', ctypes.c_size_t), ('priority', w.DWORD), ('scheduling', w.DWORD)]
        class Extended(ctypes.Structure):
            _fields_ = [('basic', Basic), ('io', ctypes.c_ulonglong * 6), ('processMemory', ctypes.c_size_t),
                        ('jobMemory', ctypes.c_size_t), ('peakProcess', ctypes.c_size_t), ('peakJob', ctypes.c_size_t)]
        self.api = ctypes.WinDLL('kernel32', use_last_error=True)
        self.api.CreateJobObjectW.argtypes = [ctypes.c_void_p, w.LPCWSTR]
        self.api.CreateJobObjectW.restype = w.HANDLE
        for name, argtypes in [('SetInformationJobObject', [w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD]),
                               ('AssignProcessToJobObject', [w.HANDLE, w.HANDLE]), ('CloseHandle', [w.HANDLE])]:
            fn = getattr(self.api, name)
            fn.argtypes = argtypes
            fn.restype = w.BOOL
        self.handle = self.api.CreateJobObjectW(None, None)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())
        info = Extended()
        info.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        if not self.api.SetInformationJobObject(self.handle, 9, ctypes.byref(info), ctypes.sizeof(info)):
            self.close()
            raise ctypes.WinError(ctypes.get_last_error())

    def start(self, process):
        from ctypes import wintypes as w
        if not self.api.AssignProcessToJobObject(self.handle, int(process._handle)):
            raise ctypes.WinError(ctypes.get_last_error())
        class ThreadEntry(ctypes.Structure):
            _fields_ = [('size', w.DWORD), ('usage', w.DWORD), ('thread', w.DWORD), ('owner', w.DWORD),
                        ('basePriority', w.LONG), ('deltaPriority', w.LONG), ('flags', w.DWORD)]
        self.api.CreateToolhelp32Snapshot.argtypes = [w.DWORD, w.DWORD]
        self.api.CreateToolhelp32Snapshot.restype = w.HANDLE
        self.api.Thread32First.argtypes = self.api.Thread32Next.argtypes = [w.HANDLE, ctypes.POINTER(ThreadEntry)]
        self.api.OpenThread.argtypes = [w.DWORD, w.BOOL, w.DWORD]
        self.api.OpenThread.restype = w.HANDLE
        self.api.ResumeThread.argtypes = [w.HANDLE]
        self.api.ResumeThread.restype = w.DWORD
        snapshot = self.api.CreateToolhelp32Snapshot(4, 0)
        if snapshot == ctypes.c_void_p(-1).value:
            raise ctypes.WinError(ctypes.get_last_error())
        resumed = False
        try:
            entry = ThreadEntry()
            entry.size = ctypes.sizeof(entry)
            found = self.api.Thread32First(snapshot, ctypes.byref(entry))
            while found:
                if entry.owner == process.pid:
                    thread = self.api.OpenThread(2, False, entry.thread)
                    if thread:
                        try:
                            resumed = self.api.ResumeThread(thread) != 0xffffffff
                        finally:
                            self.api.CloseHandle(thread)
                    break
                found = self.api.Thread32Next(snapshot, ctypes.byref(entry))
        finally:
            self.api.CloseHandle(snapshot)
        if not resumed:
            raise OSError('Suspended job child could not be resumed')

    def close(self):
        if self.handle:
            self.api.CloseHandle(self.handle)
            self.handle = None


def run_command(command, cwd, env, log, timeout):
    """No shell; bounded duration, with timeout termination of this process tree."""
    started = time.monotonic()
    status = 'failed'
    timed_out = False
    cleanup = None
    process = None
    job = None
    with log.open('wb') as stream:
        try:
            job = WindowsJob() if os.name == 'nt' else None
            process = subprocess.Popen(command, cwd=cwd, env=env, stdout=stream, stderr=subprocess.STDOUT,
                                       start_new_session=os.name != 'nt', creationflags=4 if job else 0, shell=False)
            if job:
                job.start(process)
            try:
                code = process.wait(timeout=timeout)
                status = 'passed' if code == 0 else 'failed'
            except subprocess.TimeoutExpired:
                timed_out = True
                status = 'timeout'
                if os.name == 'nt':
                    job.close()
                    cleanup = {'api': 'Windows JobObject kill-on-close', 'ownedProcessId': process.pid}
                else:
                    os.killpg(process.pid, signal.SIGKILL)
                if process.poll() is None:
                    process.kill()
                code = process.wait(timeout=10)
        except (OSError, subprocess.SubprocessError) as error:
            stream.write((type(error).__name__ + ': ' + str(error)).encode('utf-8', errors='replace'))
            code = None
        finally:
            if job:
                job.close()
            if process is not None and process.poll() is None:
                process.kill()
                process.wait(timeout=10)
    return {'command': command, 'cwd': str(cwd), 'status': status, 'exitCode': code, 'timedOut': timed_out,
            'seconds': round(time.monotonic() - started, 3), 'log': str(log), 'timeoutCleanup': cleanup}


def stage_sources(root, workspace):
    rows = []
    # Source-only staging also preserves relative Compile/ProjectReference/Python imports.
    for top in ('poc', 'tools/python'):
        for source in sorted((root / top).rglob('*')):
            relative = source.relative_to(root)
            if any(part in ('bin', 'obj', '__pycache__', '.godot', 'local-private') for part in relative.parts):
                continue
            if source.is_file() and source.suffix.lower() in ('.cs', '.csproj', '.py', '.json', '.config', '.props', '.targets'):
                no_links(source)
                raw = source.read_bytes()
                target = workspace / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(raw)
                rows.append({'path': relative.as_posix(), 'sha256': hashlib.sha256(raw).hexdigest(), 'bytes': len(raw)})
    # The accepted PTAT fixture author imports the archive CRC helper outside
    # tools/python. Preserve this exact dependency and its identity too.
    source = root / 'tools/tre_crc_vectors.py'
    no_links(source)
    if source.is_file():
        raw = source.read_bytes()
        target = workspace / 'tools/tre_crc_vectors.py'
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)
        rows.append({'path': 'tools/tre_crc_vectors.py', 'sha256': hashlib.sha256(raw).hexdigest(), 'bytes': len(raw)})
    return rows


def execute(args, root=ROOT):
    output = fresh_output(args.output, root)
    dotnet, python, packages = [local_path(v) for v in (args.dotnet, args.python, args.packages)]
    for executable in (dotnet, python):
        no_links(executable)
        if not executable.is_file():
            raise ValueError('Required local executable missing: ' + str(executable))
    no_links(packages)
    for name, version in [('bcnencoder.net', '2.3.0'), ('communitytoolkit.highperformance', '8.4.0')]:
        folder = packages / name / version
        no_links(folder)
        if not folder.is_dir() or not list(folder.glob('*.nupkg')):
            raise ValueError('Required offline cached package missing: ' + name + '/' + version)
    rows = projects_from(local_path(args.projects) if args.projects else None)
    python_suites = list(PYTHON_SUITES)
    default_ids = [row['id'] for row in DEFAULT_PROJECTS] + [item[0] for item in PYTHON_SUITES]
    if args.suites:
        selected = args.suites.split(',')
        available = {row['id'] for row in rows} | {item[0] for item in PYTHON_SUITES}
        if not selected or len(selected) != len(set(selected)) or any(ident not in available for ident in selected):
            raise ValueError('Exact unique known suite IDs required')
        rows = [row for row in rows if row['id'] in selected]
        python_suites = [item for item in python_suites if item[0] in selected]
    if not 0.1 <= args.timeout <= 3600:
        raise ValueError('Timeout must be 0.1..3600 seconds')
    output.mkdir(parents=True)
    report = {'schema': 1, 'status': 'running', 'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'synthetic': True, 'liveCompatibilityVerified': False, 'originalResourcesVerified': False,
              'inputs': {'dotnet': str(dotnet), 'python': str(python), 'packages': str(packages), 'projectOverride': args.projects, 'suiteOverride': args.suites},
              'projectList': rows, 'commands': [],
              'suites': [{'id': row['id'], 'status': 'not-run', 'kind': 'dotnet'} for row in rows] +
                        [{'id': item[0], 'status': 'not-run', 'kind': 'python'} for item in python_suites],
              'omittedDefaults': [{'id': ident, 'status': 'skipped-unverified', 'accepted': False, 'reason': 'Omitted by explicit reviewed suite/project override'}
                                  for ident in default_ids if ident not in {item['id'] for item in rows} | {item[0] for item in python_suites}],
              'optional': [{'id': ident, 'status': 'skipped-unverified', 'reason': reason, 'accepted': False} for ident, reason in OPTIONAL]}
    def save():
        (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    def call(suite, command, cwd):
        receipt = run_command(command, cwd, env, output / (str(len(report['commands'])).zfill(2) + '-' + suite + '.log'), args.timeout)
        report['commands'].append(receipt)
        save()
        if receipt['status'] != 'passed':
            raise RuntimeError(suite + ' ' + receipt['status'])
        return receipt
    try:
        workspace = output / 'workspace'
        sources = stage_sources(root, workspace)
        source_bytes = (json.dumps(sources, indent=2) + '\n').encode('utf-8')
        (output / 'source-manifest.json').write_bytes(source_bytes)
        report['sourceManifest'] = {'path': str(output / 'source-manifest.json'), 'files': len(sources), 'sha256': hashlib.sha256(source_bytes).hexdigest()}
        cache = output / 'packages'
        for name in ('bcnencoder.net', 'communitytoolkit.highperformance'):
            # Reject links throughout the package copy, not merely the version root.
            for entry in (packages / name).rglob('*'):
                no_links(entry)
            shutil.copytree(packages / name, cache / name)
        config = output / 'NuGet.Config'
        config.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n', encoding='utf-8')
        env = os.environ.copy()
        env.update(APPDATA=str(output / 'appdata'), DOTNET_CLI_HOME=str(output / 'dotnet-home'), NUGET_PACKAGES=str(cache),
                   DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false',
                   PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1', MSBUILDDISABLENODEREUSE='1')
        nuget = output / 'appdata/NuGet/NuGet.Config'
        nuget.parent.mkdir(parents=True)
        shutil.copyfile(config, nuget)
        result_root = workspace / 'local-private/results'
        result_root.mkdir(parents=True)
        profile = result_root / 'synthetic-profile.json'
        call('synthetic-profile', [str(python), str(workspace / 'poc/round83-tests/Rendering/synthetic_profile.py'), str(profile)], workspace)
        for row in rows:
            ident = row['id']
            suite = next(item for item in report['suites'] if item['id'] == ident)
            suite['status'] = 'running'
            project = workspace / row['project']
            if not project.is_file():
                raise ValueError('Required project missing: ' + row['project'])
            fixture = result_root / 'runtime83-fixtures-before-native.json'
            runtime83 = ident == 'runtime83-session'
            assets83 = ident == 'assets83-preflight'
            runtime84 = ident == 'runtime84-scene-bridge'
            assets84 = ident == 'assets84-creature-preparation'
            runtime85 = ident == 'runtime85-selected-ingress'
            runtime86 = ident == 'runtime86-player-motion'
            runtime87 = ident == 'runtime87-disconnect'
            runtime88 = ident == 'runtime88-client-run'
            runtime89 = ident == 'runtime89-client-application'
            runtime90 = ident == 'runtime90-controlled-movement'
            runtime91 = ident == 'runtime91-endpoint-approval'
            networking91 = ident == 'networking91-endpoint-handoff'
            runtime92 = ident == 'runtime92-controlled-launch'
            assets88 = ident == 'assets88-launch-profile'
            if runtime83:
                if row['project'] != 'poc/round83-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime83-session requires its exact fixture/native/verify pipeline')
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round83-tests/Runtime/fixtures.py'), '--out', str(fixture),
                                          '--client-id-opcode', '0xD5899226', '--permissions-opcode', '0xE00730E5'], workspace)
            if assets83:
                if row['project'] != 'poc/round83-tests/Assets/Assets.csproj' or row['args'] != ['{fixture}', '{receipt}']:
                    raise ValueError('assets83-preflight requires its exact fixture/native/verify pipeline')
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round83-tests/Assets/peer.py'), 'generate', str(result_root / ident)], workspace)
                fixture = result_root / ident / 'fixtures.json'
            if runtime84:
                if row['project'] != 'poc/round84-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime84-scene-bridge requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime84-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round84-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if assets84:
                if row['project'] != 'poc/round84-tests/Assets/Assets.csproj' or row['args'] != ['{fixture}', '{receipt}']:
                    raise ValueError('assets84-creature-preparation requires its exact fixture/native/verify pipeline')
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round84-tests/Assets/peer.py'), 'generate', str(result_root / ident)], workspace)
                fixture = result_root / ident / 'fixtures.json'
            if runtime85:
                if row['project'] != 'poc/round85-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime85-selected-ingress requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime85-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round85-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime86:
                if row['project'] != 'poc/round86-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime86-player-motion requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime86-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round86-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime87:
                if row['project'] != 'poc/round87-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime87-disconnect requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime87-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round87-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime88:
                if row['project'] != 'poc/round88-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime88-client-run requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime88-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round88-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime89:
                if row['project'] != 'poc/round89-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime89-client-application requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime89-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round89-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime90:
                if row['project'] != 'poc/round90-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime90-controlled-movement requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime90-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round90-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime91:
                if row['project'] != 'poc/round91-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime91-endpoint-approval requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime91-endpoint-approval-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round91-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if networking91:
                if row['project'] != 'poc/round91-tests/Networking/Networking.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('networking91-endpoint-handoff requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'networking91-endpoint-handoff-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round91-tests/Networking/fixtures.py'), '--out', str(fixture)], workspace)
            if runtime92:
                if row['project'] != 'poc/round92-tests/Runtime/Runtime.csproj' or row['args'] != ['{fixture}']:
                    raise ValueError('runtime92-controlled-launch requires its exact fixture/native/verify pipeline')
                fixture = result_root / 'runtime92-fixtures-before-native.json'
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round92-tests/Runtime/fixtures.py'), '--out', str(fixture)], workspace)
            if assets88:
                if row['project'] != 'poc/round88-tests/Assets/Assets.csproj' or row['args'] != ['{fixture}', '{receipt}']:
                    raise ValueError('assets88-launch-profile requires its exact fixture/native/verify pipeline')
                call(ident + '-fixtures', [str(python), str(workspace / 'poc/round88-tests/Assets/peer.py'), 'generate', str(result_root / ident)], workspace)
                fixture = result_root / ident / 'fixtures.json'
            # Compact private build paths avoid Windows path limits when a
            # regression runner is itself staged beneath a consolidated run.
            # Suite identity remains in command labels and report entries.
            artifacts = output / 'artifacts' / str(rows.index(row))
            call(ident + '-build', [str(dotnet), 'build', str(project), '--artifacts-path', str(artifacts), '--configfile', str(config),
                                  '-p:NuGetAudit=false', '-p:UseSharedCompilation=false', '-p:UseAppHost=false', '-p:TreatWarningsAsErrors=true', '--nologo'], workspace)
            # AssemblyName may differ from the project filename; inspect the isolated output.
            candidates = list((artifacts / 'bin' / project.stem / 'debug').glob('*.runtimeconfig.json'))
            if len(candidates) != 1:
                raise ValueError('Exactly one executable runtimeconfig required: ' + ident)
            assembly = candidates[0].with_name(candidates[0].name.removesuffix('.runtimeconfig.json') + '.dll')
            substitutions = {'{output}': str(result_root / ident), '{profile}': str(profile), '{fixture}': str(fixture),
                             '{receipt}': str(result_root / ident / 'receipt.json')}
            native = call(ident, [str(dotnet), str(assembly), *[substitutions.get(a, a) for a in row['args']]], workspace)
            if runtime83:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round83-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime83-verified.json')], workspace)
            if assets83:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round83-tests/Assets/peer.py'), 'verify', str(result_root / ident)], workspace)
            if runtime84:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round84-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime84-verified.json')], workspace)
            if assets84:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round84-tests/Assets/peer.py'), 'verify', str(result_root / ident)], workspace)
            if runtime85:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round85-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime85-verified.json')], workspace)
            if runtime86:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round86-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime86-verified.json')], workspace)
            if runtime87:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round87-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime87-verified.json')], workspace)
            if runtime88:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round88-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime88-verified.json')], workspace)
            if runtime89:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round89-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime89-verified.json')], workspace)
            if runtime90:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round90-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime90-verified.json')], workspace)
            if runtime91:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round91-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime91-endpoint-approval-verified.json')], workspace)
            if networking91:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round91-tests/Networking/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'networking91-endpoint-handoff-verified.json')], workspace)
                call(ident + '-negative-verifier', [str(python), str(workspace / 'poc/round91-tests/Networking/test.py'), '--fixtures', str(fixture), '--result', native['log']], workspace)
            if runtime92:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round92-tests/Runtime/verify.py'), '--fixtures', str(fixture),
                                        '--result', native['log'], '--out', str(result_root / 'runtime92-verified.json')], workspace)
            if assets88:
                call(ident + '-verify', [str(python), str(workspace / 'poc/round88-tests/Assets/peer.py'), 'verify', str(result_root / ident)], workspace)
            suite['status'] = 'passed'
        for ident, script, literals in python_suites:
            suite = next(item for item in report['suites'] if item['id'] == ident)
            suite['status'] = 'running'
            values = {'{output}': str(result_root / ident), '{profile}': str(profile), '{dotnet}': str(dotnet), '{packages}': str(cache)}
            call(ident, [str(python), str(workspace / script), *[values.get(v, v) for v in literals]], workspace)
            suite['status'] = 'passed'
        report['status'] = 'passed'
        report['claim'] = 'Only the explicitly executed offline suites passed. Optional skips remain unverified.'
        return 0
    except Exception as error:
        report['status'] = 'failed'
        report['failure'] = type(error).__name__ + ': ' + str(error)
        for suite in report['suites']:
            if suite['status'] == 'running':
                suite['status'] = 'failed'
        return 1
    finally:
        report['completedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        save()
        print(report['status'].upper() + ' report=' + str(output / 'report.json'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ('dotnet', 'python', 'packages', 'output'):
        parser.add_argument('--' + key, required=True)
    parser.add_argument('--projects', help='Reviewed exact id/project/args JSON array; replaces default dotnet list')
    parser.add_argument('--suites', help='Explicit comma-separated reviewed subset; omissions are unverified, never silently accepted')
    parser.add_argument('--timeout', type=float, default=180)
    args = parser.parse_args()
    try:
        return execute(args)
    except (ValueError, OSError) as error:
        print('REJECTED ' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
