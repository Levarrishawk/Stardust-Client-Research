"""Runner rejection and real subprocess failure/timeout tests; no client sockets."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('regressions', ROOT / 'tools/python/run_client_regressions.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class Tests(unittest.TestCase):
    def setUp(self):
        private = ROOT / 'local-private'
        private.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix='runner-tests-', dir=private)
        self.path = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def test_real_nonzero_and_literal_arguments(self):
        log = self.path / 'failed.log'
        literal = 'space & $literal; `whoami`'
        result = runner.run_command([sys.executable, '-c', 'import sys;print(sys.argv[1]);sys.exit(7)', literal], ROOT, os.environ.copy(), log, 5)
        self.assertEqual(result['status'], 'failed')
        self.assertEqual(result['exitCode'], 7)
        self.assertFalse(result['timedOut'])
        self.assertIn(literal, log.read_text())

    def test_real_timeout_kills_child(self):
        marker = self.path / 'child-survived.txt'
        child = 'import time,pathlib;time.sleep(1.5);pathlib.Path(' + repr(str(marker)) + ').write_text("survived")'
        parent = 'import subprocess,sys,time;subprocess.Popen([sys.executable,"-c",sys.argv[1]]);print("spawned",flush=True);time.sleep(10)'
        result = runner.run_command([sys.executable, '-c', parent, child], ROOT, os.environ.copy(), self.path / 'timeout.log', .5)
        self.assertEqual(result['status'], 'timeout')
        self.assertTrue(result['timedOut'])
        self.assertLess(result['seconds'], 5)
        time.sleep(1.7)
        self.assertFalse(marker.exists(), 'timeout must terminate the child too')

    def test_missing_executable_reports_failure(self):
        result = runner.run_command([str(self.path / 'absent-executable')], ROOT, os.environ.copy(), self.path / 'missing.log', 1)
        self.assertEqual(result['status'], 'failed')
        self.assertIsNone(result['exitCode'])
        self.assertFalse(result['timedOut'])

    def test_output_boundaries_and_preservation(self):
        with self.assertRaises(ValueError):
            runner.fresh_output(str(ROOT / 'poc/rejected-output'), ROOT)
        with self.assertRaises(ValueError):
            runner.fresh_output(str(self.path), ROOT)
        kept = self.path / 'keep.txt'
        kept.write_text('unchanged')
        with self.assertRaises(ValueError):
            runner.fresh_output(str(kept / 'child'), ROOT)
        self.assertEqual(kept.read_text(), 'unchanged')
        for value in ('relative', 'https://example.invalid', '//server/share', 'x\0y'):
            with self.assertRaises(ValueError):
                runner.local_path(value)

    def test_exact_project_list(self):
        path = self.path / 'projects.json'
        valid = dict(id='reviewed83', project='poc/round83-tests/Runtime/Runtime.csproj', args=['{output}', 'literal space & $'])
        path.write_text(json.dumps([valid]))
        self.assertEqual(runner.projects_from(path), [valid])
        bad = [[], [valid, valid], [dict(valid, project='../outside.csproj')], [dict(valid, project='poc/../outside.csproj')],
               [dict(valid, project='poc/a\\b.csproj')], [dict(valid, project='poc/C:a.csproj')],
               [dict(valid, args='shell command')], [dict(valid, args=['prefix{output}'])],
               [dict(valid, unknown=True)], [dict(valid, id='runner83-tests')], [dict(valid, id=True)],
               [dict(valid, args=[None])]]
        for value in bad:
            with self.subTest(value=value):
                path.write_text(json.dumps(value))
                with self.assertRaises(ValueError):
                    runner.projects_from(path)
        path.write_text('[{"id":"a","id":"b","project":"poc/a.csproj","args":[]}]')
        with self.assertRaises(ValueError):
            runner.projects_from(path)

    def test_optional_skips_are_unverified(self):
        self.assertEqual([v['id'] for v in runner.DEFAULT_PROJECTS][2:],
                         ['runtime83-session', 'assets83-preflight', 'runtime84-scene-bridge', 'assets84-creature-preparation'])
        self.assertIn('networking83-authentication', [v[0] for v in runner.PYTHON_SUITES])
        self.assertIn('networking84-injected-pump', [v[0] for v in runner.PYTHON_SUITES])
        self.assertTrue(any(v[0] == 'rendering80-84-godot' for v in runner.OPTIONAL))


if __name__ == '__main__':
    unittest.main()
