"""Real-process orchestration surrogates, not client-run native evidence."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('runner88', ROOT / 'tools/python/run_client_regressions.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
ID = 'runtime88-client-run'
PROJECT = 'poc/round88-tests/Runtime/Runtime.csproj'


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='runner88-tests-', dir=ROOT / 'local-private')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.requests = []
        self.failure = None
        self.hang = None
        self.timeout = 10
        self.original_run = runner.run_command
        for name, version in [('bcnencoder.net', '2.3.0'), ('communitytoolkit.highperformance', '8.4.0')]:
            p = self.root / 'cache' / name / version
            p.mkdir(parents=True)
            (p / 'authored.nupkg').write_text('orchestration cache placeholder; never restored')
        scripts = self.root / 'authored/poc/round88-tests/Runtime'
        scripts.mkdir(parents=True)
        (scripts / 'Runtime.csproj').write_text('<Project />')
        (scripts / 'fixtures.py').write_text(
            'import json,sys\nfrom pathlib import Path\n'
            'p=Path(sys.argv[sys.argv.index("--out")+1]);'
            'p.write_text(json.dumps({"Authored":True,"Token":"independent-before-native"}))\n')
        (scripts / 'verify.py').write_text(
            'import json,sys\nfrom pathlib import Path\n'
            'a=lambda k:Path(sys.argv[sys.argv.index(k)+1]);f=a("--fixtures");n=a("--result");'
            'assert json.loads(f.read_text())["Token"]==json.loads(n.read_text())["Observed"];'
            'assert f.stat().st_mtime_ns<n.stat().st_mtime_ns;'
            'a("--out").write_text(json.dumps({"Verified":True,"Surrogate":True}))\n')
        profile = self.root / 'authored/poc/round83-tests/Rendering'
        profile.mkdir(parents=True)
        (profile / 'synthetic_profile.py').write_text('import sys\nfrom pathlib import Path\nPath(sys.argv[1]).write_text("{}")\n')
        self.build = self.root / 'build.py'
        self.build.write_text(
            'import sys\nfrom pathlib import Path\n'
            'p=Path(sys.argv[1])/"bin/Runtime/debug";p.mkdir(parents=True);'
            '(p/"Runtime.runtimeconfig.json").write_text("{}");'
            '(p/"Runtime.dll").write_text(' + repr(
                'import json,sys\nfrom pathlib import Path\n'
                'f=Path(sys.argv[1]);v=json.loads(f.read_text());assert v["Authored"] is True;'
                'print(json.dumps({"Observed":v["Token"],"Surrogate":True}))\n') + ')\n')

    def stage(self, root, destination):
        shutil.copytree(self.root / 'authored', destination)
        return []

    def command(self, command, cwd, env, log, timeout):
        self.requests.append(command)
        self.assertTrue(Path(env['NUGET_PACKAGES']).is_relative_to(self.root / 'local-private'))
        self.assertTrue(Path(env['DOTNET_CLI_HOME']).is_relative_to(self.root / 'local-private'))
        self.assertEqual((Path(env['APPDATA']) / 'NuGet/NuGet.Config').read_text(),
                         '<configuration><packageSources><clear /></packageSources></configuration>\n')
        if self.failure and any(self.failure in part.replace('\\', '/') for part in command):
            actual = [sys.executable, '-c', 'import sys;sys.exit(9)']
        elif self.hang and any(self.hang in part.replace('\\', '/') for part in command):
            marker = self.root / 'survived.txt'
            child = 'import time,pathlib;time.sleep(1);pathlib.Path(' + repr(str(marker)) + ').write_text("survived")'
            parent = 'import subprocess,sys,time;subprocess.Popen([sys.executable,"-c",sys.argv[1]]);print("spawned",flush=True);time.sleep(10)'
            actual = [sys.executable, '-c', parent, child]
        elif command[1] == 'build':
            actual = [sys.executable, str(self.build), command[command.index('--artifacts-path') + 1]]
        else:
            actual = command
        return self.original_run(actual, cwd, env, log, timeout)

    def execute(self, suites=ID, projects=None):
        args = argparse.Namespace(output=str(self.root / 'local-private/result'), dotnet=sys.executable,
                                  python=sys.executable, packages=str(self.root / 'cache'),
                                  projects=projects, suites=suites, timeout=self.timeout)
        with patch.object(runner, 'stage_sources', self.stage), patch.object(runner, 'run_command', self.command):
            code = runner.execute(args, self.root)
        return code, json.loads((Path(args.output) / 'report.json').read_text())

    def test_fixture_native_verifier_execute_in_order(self):
        code, report = self.execute()
        self.assertEqual(code, 0)
        self.assertEqual(report['suites'], [{'id': ID, 'status': 'passed', 'kind': 'dotnet'}])
        self.assertEqual(len(self.requests), 5)  # profile, independent author, build, native, verifier
        self.assertTrue(Path(self.requests[1][1]).as_posix().endswith('round88-tests/Runtime/fixtures.py'))
        self.assertEqual(self.requests[2][1], 'build')
        self.assertTrue(self.requests[3][1].endswith('Runtime.dll'))
        self.assertTrue(Path(self.requests[4][1]).as_posix().endswith('round88-tests/Runtime/verify.py'))
        self.assertEqual(self.requests[1][-2], '--out')
        self.assertEqual(self.requests[3][-1], self.requests[1][-1])
        self.assertEqual(self.requests[4][2::2], ['--fixtures', '--result', '--out'])
        verified = Path(self.requests[4][-1])
        self.assertEqual(json.loads(verified.read_text()), {'Verified': True, 'Surrogate': True})
        self.assertFalse(report['liveCompatibilityVerified'])
        self.assertFalse(report['originalResourcesVerified'])
        self.assertTrue(all('socket' not in ' '.join(c).lower() and '://' not in ' '.join(c) for c in self.requests))

    def test_nested_build_paths_preserve_inherited_pipelines(self):
        # Match the 124-character staged root of the failed consolidated run.
        # These are the unchanged inherited real-process surrogate pipelines.
        with tempfile.TemporaryDirectory(prefix='depth-', dir=ROOT / 'local-private') as temporary:
            base = Path(temporary)
            nested = ROOT if len(str(base)) >= 124 else base / ('n' * (124 - len(str(base)) - 1))
            (nested / 'local-private').mkdir(parents=True, exist_ok=True)
            for number in (85, 86):
                with self.subTest(round=number):
                    source = ROOT / f'poc/round{number}-tests/Rendering/test_pipeline.py'
                    inherited_spec = importlib.util.spec_from_file_location(f'nested{number}', source)
                    inherited = importlib.util.module_from_spec(inherited_spec)
                    inherited_spec.loader.exec_module(inherited)
                    with patch.object(inherited, 'ROOT', nested):
                        case = inherited.PipelineTests()
                        case.setUp()
                        try:
                            code, report = case.execute()
                            self.assertEqual(code, 0, report.get('failure'))
                            self.assertEqual(len(case.requests), 5)
                            build = case.requests[2]
                            directory = Path(build[build.index('--artifacts-path') + 1])
                            self.assertTrue(directory.name.isdecimal())
                        finally:
                            case.doCleanups()

    def test_explicit_omission_is_not_acceptance(self):
        # Replace all native projects with a reviewed stand-in, then omit it too.
        with patch.object(runner, 'PYTHON_SUITES', [('authored-profile', 'poc/round83-tests/Rendering/synthetic_profile.py', ['{output}'])]):
            code, report = self.execute('authored-profile')
        self.assertEqual(code, 0)
        omitted = next(r for r in report['omittedDefaults'] if r['id'] == ID)
        self.assertEqual(omitted['status'], 'skipped-unverified')
        self.assertIs(omitted['accepted'], False)
        self.assertEqual(len(self.requests), 2)

    def test_failure_stops_before_native_or_verifier(self):
        self.failure = 'round88-tests/Runtime/fixtures.py'
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(report['status'], 'failed')
        self.assertEqual(len(self.requests), 2)
        self.assertEqual(report['commands'][-1]['exitCode'], 9)
        self.assertEqual(report['suites'][0]['status'], 'failed')

    def test_native_failure_never_runs_verifier(self):
        self.failure = 'Runtime.dll'
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 4)
        self.assertEqual(report['suites'][0]['status'], 'failed')

    def test_verifier_failure_prevents_acceptance(self):
        self.failure = 'round88-tests/Runtime/verify.py'
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 5)
        self.assertEqual(report['suites'][0]['status'], 'failed')
        self.assertEqual(report['commands'][-1]['exitCode'], 9)

    def test_missing_required88_project_fails(self):
        (self.root / 'authored' / PROJECT).unlink()
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 1)
        self.assertIn('Required project missing', report['failure'])

    def test_build_failure_stops_before_native(self):
        self.failure = 'build'
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 3)
        self.assertEqual(report['commands'][-1]['exitCode'], 9)
        self.assertEqual(report['suites'][0]['status'], 'failed')

    def test_pipeline_timeout_retires_descendant_and_stops(self):
        self.hang = 'round88-tests/Runtime/fixtures.py'
        self.timeout = .3
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 2)
        self.assertEqual(report['commands'][-1]['status'], 'timeout')
        self.assertTrue(report['commands'][-1]['timedOut'])
        self.assertIn('spawned', Path(report['commands'][-1]['log']).read_text())
        self.assertEqual(report['suites'][0]['status'], 'failed')
        import time
        time.sleep(1.2)
        self.assertFalse((self.root / 'survived.txt').exists())

    def test_reserved_pipeline_rejects_override(self):
        projects = self.root / 'projects.json'
        projects.write_text(json.dumps([{'id': ID, 'project': PROJECT, 'args': []}]))
        code, report = self.execute(projects=str(projects))
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 1)
        self.assertIn('exact fixture/native/verify pipeline', report['failure'])


class AssetPipelineTests(unittest.TestCase):
    """Independent author/native receipt/peer comparison; actual subprocess surrogates."""
    def setUp(self):
        self.case = PipelineTests()
        self.case.setUp()
        self.addCleanup(self.case.doCleanups)
        self.root = self.case.root
        self.number = 0
        self.ident = 'assets88-launch-profile'
        self.project = 'poc/round88-tests/Assets/Assets.csproj'
        scripts = self.root / 'authored/poc/round88-tests/Assets'
        scripts.mkdir(parents=True)
        (scripts / 'Assets.csproj').write_text('<Project />')
        (scripts / 'peer.py').write_text(
            'import json,sys\nfrom pathlib import Path\n'
            'p=Path(sys.argv[2]);f=p/"fixtures.json";n=p/"receipt.json"\n'
            'if sys.argv[1]=="generate":\n'
            ' p.mkdir();f.write_text(json.dumps({"Token":"asset-authored-before-native"}))\n'
            'else:\n'
            ' assert json.loads(f.read_text())["Token"]==json.loads(n.read_text())["Observed"]\n'
            ' assert f.stat().st_mtime_ns<n.stat().st_mtime_ns\n'
            ' (p/"independent.json").write_text(json.dumps({"Verified":True,"Surrogate":True}))\n')
        native = ('import json,sys\nfrom pathlib import Path\n'
                  'v=json.loads(Path(sys.argv[1]).read_text());'
                  'Path(sys.argv[2]).write_text(json.dumps({"Observed":v["Token"]}));print("surrogate native")\n')
        self.case.build.write_text(
            'import sys\nfrom pathlib import Path\n'
            'p=Path(sys.argv[1])/"bin/Assets/debug";p.mkdir(parents=True);'
            '(p/"Assets.runtimeconfig.json").write_text("{}");'
            '(p/"Assets.dll").write_text(' + repr(native) + ')\n')

    def execute(self, suites=None, projects=None):
        self.number += 1
        args = argparse.Namespace(output=str(self.root / ('local-private/result-' + str(self.number))),
                                  dotnet=sys.executable, python=sys.executable, packages=str(self.root / 'cache'),
                                  projects=projects, suites=suites or self.ident, timeout=self.case.timeout)
        self.case.requests.clear()
        with patch.object(runner, 'stage_sources', self.case.stage), patch.object(runner, 'run_command', self.case.command):
            code = runner.execute(args, self.root)
        return code, json.loads((Path(args.output) / 'report.json').read_text())

    def test_peer_native_peer_order_and_exact_cli(self):
        code, report = self.execute()
        self.assertEqual(code, 0)
        requests = self.case.requests
        self.assertEqual(len(requests), 5)
        self.assertTrue(Path(requests[1][1]).as_posix().endswith('round88-tests/Assets/peer.py'))
        self.assertEqual(requests[1][2], 'generate')
        self.assertEqual(requests[2][1], 'build')
        self.assertTrue(requests[3][1].endswith('Assets.dll'))
        directory = Path(requests[1][3])
        self.assertEqual(requests[3][-2:], [str(directory / 'fixtures.json'), str(directory / 'receipt.json')])
        self.assertEqual(requests[4][2:], ['verify', str(directory)])
        self.assertIs(json.loads((directory / 'independent.json').read_text())['Verified'], True)
        self.assertEqual(report['suites'][0]['status'], 'passed')

    def test_each_stage_failure_prevents_acceptance(self):
        for failure, count in [('generate', 2), ('build', 3), ('Assets.dll', 4), ('verify', 5)]:
            with self.subTest(stage=failure):
                self.case.failure = failure
                code, report = self.execute()
                self.assertEqual(code, 1)
                self.assertEqual(len(self.case.requests), count)
                self.assertEqual(report['suites'][0]['status'], 'failed')

    def test_missing_assets88_project_fails(self):
        (self.root / 'authored' / self.project).unlink()
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.case.requests), 1)
        self.assertIn('Required project missing', report['failure'])

    def test_omitted_assets_is_unverified(self):
        with patch.object(runner, 'PYTHON_SUITES', [('authored-profile', 'poc/round83-tests/Rendering/synthetic_profile.py', ['{output}'])]):
            code, report = self.execute('authored-profile')
        self.assertEqual(code, 0)
        omitted = next(r for r in report['omittedDefaults'] if r['id'] == self.ident)
        self.assertEqual((omitted['status'], omitted['accepted']), ('skipped-unverified', False))

    def test_reserved_assets_arguments_reject(self):
        projects = self.root / 'projects.json'
        projects.write_text(json.dumps([dict(id=self.ident, project=self.project, args=[])]))
        code, report = self.execute(projects=str(projects))
        self.assertEqual(code, 1)
        self.assertEqual(len(self.case.requests), 1)
        self.assertIn('exact fixture/native/verify pipeline', report['failure'])

    def test_asset_author_timeout_kills_spawned_child(self):
        self.case.hang = 'generate'
        self.case.timeout = .3
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.case.requests), 2)
        self.assertEqual(report['commands'][-1]['status'], 'timeout')
        self.assertIn('spawned', Path(report['commands'][-1]['log']).read_text())
        import time
        time.sleep(1.2)
        self.assertFalse((self.root / 'survived.txt').exists())


if __name__ == '__main__':
    unittest.main()
