"""Real-process orchestration surrogates, not player-motion native evidence."""
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
spec = importlib.util.spec_from_file_location('runner86', ROOT / 'tools/python/run_client_regressions.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
ID = 'runtime86-player-motion'
PROJECT = 'poc/round86-tests/Runtime/Runtime.csproj'


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='runner86-tests-', dir=ROOT / 'local-private')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.requests = []
        self.failure = None
        self.original_run = runner.run_command
        for name, version in [('bcnencoder.net', '2.3.0'), ('communitytoolkit.highperformance', '8.4.0')]:
            p = self.root / 'cache' / name / version
            p.mkdir(parents=True)
            (p / 'authored.nupkg').write_text('orchestration cache placeholder; never restored')
        scripts = self.root / 'authored/poc/round86-tests/Runtime'
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
        elif command[1] == 'build':
            actual = [sys.executable, str(self.build), command[command.index('--artifacts-path') + 1]]
        else:
            actual = command
        return self.original_run(actual, cwd, env, log, timeout)

    def execute(self, suites=ID, projects=None):
        args = argparse.Namespace(output=str(self.root / 'local-private/result'), dotnet=sys.executable,
                                  python=sys.executable, packages=str(self.root / 'cache'),
                                  projects=projects, suites=suites, timeout=10)
        with patch.object(runner, 'stage_sources', self.stage), patch.object(runner, 'run_command', self.command):
            code = runner.execute(args, self.root)
        return code, json.loads((Path(args.output) / 'report.json').read_text())

    def test_fixture_native_verifier_execute_in_order(self):
        code, report = self.execute()
        self.assertEqual(code, 0)
        self.assertEqual(report['suites'], [{'id': ID, 'status': 'passed', 'kind': 'dotnet'}])
        self.assertEqual(len(self.requests), 5)  # profile, independent author, build, native, verifier
        self.assertTrue(Path(self.requests[1][1]).as_posix().endswith('round86-tests/Runtime/fixtures.py'))
        self.assertEqual(self.requests[2][1], 'build')
        self.assertTrue(self.requests[3][1].endswith('Runtime.dll'))
        self.assertTrue(Path(self.requests[4][1]).as_posix().endswith('round86-tests/Runtime/verify.py'))
        self.assertEqual(self.requests[1][-2], '--out')
        self.assertEqual(self.requests[3][-1], self.requests[1][-1])
        self.assertEqual(self.requests[4][2::2], ['--fixtures', '--result', '--out'])
        verified = Path(self.requests[4][-1])
        self.assertEqual(json.loads(verified.read_text()), {'Verified': True, 'Surrogate': True})
        self.assertFalse(report['liveCompatibilityVerified'])
        self.assertFalse(report['originalResourcesVerified'])
        self.assertTrue(all('socket' not in ' '.join(c).lower() and '://' not in ' '.join(c) for c in self.requests))

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
        self.failure = 'round86-tests/Runtime/fixtures.py'
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
        self.failure = 'round86-tests/Runtime/verify.py'
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 5)
        self.assertEqual(report['suites'][0]['status'], 'failed')
        self.assertEqual(report['commands'][-1]['exitCode'], 9)

    def test_missing_required85_project_fails(self):
        (self.root / 'authored' / PROJECT).unlink()
        code, report = self.execute()
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 1)
        self.assertIn('Required project missing', report['failure'])

    def test_reserved_pipeline_rejects_override(self):
        projects = self.root / 'projects.json'
        projects.write_text(json.dumps([{'id': ID, 'project': PROJECT, 'args': []}]))
        code, report = self.execute(projects=str(projects))
        self.assertEqual(code, 1)
        self.assertEqual(len(self.requests), 1)
        self.assertIn('exact fixture/native/verify pipeline', report['failure'])


if __name__ == '__main__':
    unittest.main()
