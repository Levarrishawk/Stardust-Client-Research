"""Real child-process aggregate/safety tests; authored stand-ins, no native proof."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('validation88', ROOT / 'tools/python/run_replacement_client_validation.py')
validation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validation)

CORE_STUB = r'''
if __name__ == '__main__':
    control=json.loads((ROOT/'control.json').read_text())
    out=Path(sys.argv[sys.argv.index('--output')+1]);out.mkdir(parents=True)
    (out/'argv.json').write_text(json.dumps(sys.argv[1:]))
    if control.get('core_exit'):sys.exit(9)
    rows=[dict(id=r['id'],status='passed') for r in DEFAULT_PROJECTS]+[dict(id=r[0],status='passed') for r in PYTHON_SUITES]
    if control.get('core_omit'):rows.pop()
    if not control.get('missing_core_receipt'):
        (out/'report.json').write_text(json.dumps(dict(status='passed',omittedDefaults=[],suites=rows,liveCompatibilityVerified=False)))
    if control.get('mutate_source'):
        (ROOT/'poc/round87-tests/Integration/run_selected_player.py').write_text('# source changed\n')
'''

JOIN_STUB = '''import json,sys,hashlib,subprocess,time
from pathlib import Path
root=Path(__file__).resolve().parents[3];control=json.loads((root/'control.json').read_text())
out=Path(sys.argv[sys.argv.index('--output')+1]);out.mkdir(parents=True)
(out/'argv.json').write_text(json.dumps(sys.argv[1:]))
if control.get('join_exit'):sys.exit(7)
if control.get('join_hang'):
    child='import time,pathlib;time.sleep(1);pathlib.Path('+repr(str(out/'survived.txt'))+').write_text("survived")'
    subprocess.Popen([sys.executable,'-c',child]);print('spawned',flush=True);time.sleep(10)
cases=[dict(Passed=True,Clean=True,OriginalResources=True,Mode=m,Screenshot=False) for m in ('success','cancel','terrain-failure','send-error','send-timeout')]
if '--gpu' in sys.argv and not control.get('missing_gpu'):
    cases.append(dict(Passed=True,Clean=True,OriginalResources=True,Mode='success',Screenshot=True))
if control.get('dirty_join'):cases[0]['Clean']=False
relative=Path(__file__).relative_to(root).as_posix();pin=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
if control.get('wrong_hash'):pin='0'*64
if not control.get('missing_join_receipt'):
    (out/'verification.json').write_text(json.dumps(dict(Passed=True,LiveCompatibility=False,SourceHashes={relative:pin},Commands=[dict(status='passed')],Receipts=cases)))
'''


class StandaloneReceiptTests(unittest.TestCase):
    def receipt(self):
        modes = ('success', 'movement-disabled', 'list-phase-timeout', 'readiness-phase-timeout',
                 'overall-timeout', 'startup-rollback', 'startup-rollback-fault', 'send-error',
                 'cleanup-fault', 'diagnostic-write-failure', 'cancel', 'window-close')
        cases = []
        for mode in modes:
            outcome = {'startup-rollback': 'StartupRejected', 'startup-rollback-fault': 'RollbackFault',
                       'send-error': 'RunFault', 'cleanup-fault': 'CleanupFault'}.get(mode, 'Finalized')
            cases.append(dict(Mode=mode, Outcome=outcome, SyntheticOffline=True,
                              LocalDrainCompleted=True, LocalDrainSucceeded=mode != 'cleanup-fault',
                              ApplicationFinalized=True, StartupRollbackCompleted=True,
                              StartupRollbackSucceeded=mode == 'startup-rollback',
                              OperationTaskSucceeded=mode != 'send-error', CleanupFailureTypes=[],
                              DiagnosticFailed=mode == 'diagnostic-write-failure', PlayerPublished=True))
        return dict(Passed=True, LiveCompatibility=False, SyntheticOffline=True, SocketCreated=False,
                    RealUdpBranch='source-reviewed-unexecuted', SourceHashes={'authored': 'fixture'},
                    Commands=[dict(status='passed', ExpectedOutcomePassed=True, timedOut=False,
                                   ExpectedExitCode=0, exitCode=0)], Receipts=cases, DirectStartupCases=4,
                    Injected92=dict(Passed=True), GPU=dict(Requested=False, Accepted=False))

    def test_observed_operation_cleanup_and_diagnostic_failures_are_distinct(self):
        validation.require_standalone_join(self.receipt(), False)
        for mode, field, value in (('send-error', 'Outcome', 'CleanupFault'),
                                   ('cleanup-fault', 'LocalDrainSucceeded', True),
                                   ('startup-rollback-fault', 'StartupRollbackCompleted', False),
                                   ('diagnostic-write-failure', 'Outcome', 'CleanupFault'),
                                   ('success', 'LocalDrainCompleted', False),
                                   ('movement-disabled', 'PlayerPublished', False)):
            with self.subTest(mode=mode, field=field):
                receipt = self.receipt()
                next(c for c in receipt['Receipts'] if c['Mode'] == mode)[field] = value
                with self.assertRaises(ValueError):
                    validation.require_standalone_join(receipt, False)

    def test_incomplete_or_live_evidence_cannot_pass_offline_aggregate(self):
        for field, value in (('SocketCreated', True), ('SyntheticOffline', False),
                             ('RealUdpBranch', 'executed'), ('DirectStartupCases', 0)):
            receipt = self.receipt()
            receipt[field] = value
            with self.assertRaises(ValueError):
                validation.require_standalone_join(receipt, False)
        receipt = self.receipt()
        receipt['Receipts'].pop()
        with self.assertRaises(ValueError):
            validation.require_standalone_join(receipt, False)
        with self.assertRaises(ValueError):
            validation.require_standalone_join(self.receipt(), True)

    def test_expected_nonzero_exit_and_gpu_require_actual_observations(self):
        receipt = self.receipt()
        receipt['Commands'].append(dict(status='failed', ExpectedOutcomePassed=True,
                                        timedOut=False, ExpectedExitCode=1, exitCode=1))
        validation.require_standalone_join(receipt, False)
        receipt['Commands'][-1]['timedOut'] = True
        with self.assertRaises(ValueError):
            validation.require_standalone_join(receipt, False)
        receipt = self.receipt()
        success = next(c for c in receipt['Receipts'] if c['Mode'] == 'success')
        success['Case'] = 'success'
        receipt['Receipts'].append(dict(success, Case='success-gpu', GPU=True))
        receipt['GPU'] = dict(Requested=True, Accepted=True)
        validation.require_standalone_join(receipt, True)
        receipt['Receipts'][-1]['LocalDrainCompleted'] = False
        with self.assertRaises(ValueError):
            validation.require_standalone_join(receipt, True)


class ValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='validation88 space-', dir=ROOT / 'local-private')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        tools = self.root / 'tools/python'
        tools.mkdir(parents=True)
        original = (ROOT / 'tools/python/run_client_regressions.py').read_text()
        (tools / 'run_client_regressions.py').write_text(original.split("\nif __name__ == '__main__':")[0] + '\n' + CORE_STUB, encoding='utf-8')
        for number in (87, 88):
            path = self.root / validation.JOIN_SCRIPTS[number]
            path.parent.mkdir(parents=True)
            path.write_text(JOIN_STUB, encoding='utf-8')
        (self.root / 'cache').mkdir()
        (self.root / 'assets').mkdir()
        (self.root / 'assets/mount.cfg').write_text('authored; no original resources')
        self.control = {}
        self.number = 0

    def args(self, **overrides):
        self.number += 1
        values = dict(dotnet=sys.executable, python=sys.executable, godot=sys.executable,
                      packages=str(self.root / 'cache'), asset_root=str(self.root / 'assets'),
                      config=str(self.root / 'assets/mount.cfg'), output=str(self.root / f'local-private/result-{self.number}'),
                      gpu=False, join_round=87)
        values.update(overrides)
        return argparse.Namespace(**values)

    def run_validation(self, **overrides):
        (self.root / 'control.json').write_text(json.dumps(self.control))
        args = self.args(**overrides)
        code = validation.execute(args, self.root)
        return code, json.loads((Path(args.output) / 'report.json').read_text()), Path(args.output)

    def test_complete_core_join_exact_argv_and_disjoint_outputs(self):
        code, report, out = self.run_validation()
        self.assertEqual(code, 0)
        self.assertEqual(report['components'], {'core': 'passed', 'join': 'passed'})
        self.assertTrue(report['sourceHashesUnchanged'])
        self.assertEqual(report['gpu'], {'requested': False, 'status': 'skipped-unverified', 'accepted': False})
        core = json.loads((out / 'core/argv.json').read_text())
        joined = json.loads((out / 'join/argv.json').read_text())
        self.assertEqual(core[::2], ['--dotnet', '--python', '--packages', '--output'])
        self.assertEqual(joined[::2], ['--dotnet', '--godot', '--packages', '--asset-root', '--config', '--output'])
        self.assertEqual(core[-1], str(out / 'core'))
        self.assertEqual(joined[-1], str(out / 'join'))
        self.assertEqual(joined[joined.index('--asset-root')+1], str(self.root / 'assets'))
        self.assertNotIn('--suites', core)
        self.assertEqual(len(report['requiredDefaults']), 22)
        self.assertFalse(report['liveCompatibilityVerified'])

    def test_gpu_pass_requires_actual_extra_receipt(self):
        code, report, out = self.run_validation(gpu=True, join_round=88)
        self.assertEqual(code, 0)
        self.assertEqual(report['gpu'], {'requested': True, 'status': 'passed', 'accepted': True})
        self.assertIn('--gpu', json.loads((out / 'join/argv.json').read_text()))
        self.control['missing_gpu'] = True
        code, report, _ = self.run_validation(gpu=True)
        self.assertEqual(code, 1)
        self.assertFalse(report['gpu']['accepted'])

    def test_child_failures_stop_or_fail_aggregate(self):
        for name in ('core_exit', 'join_exit'):
            with self.subTest(child=name):
                self.control = {name: True}
                code, report, out = self.run_validation()
                self.assertEqual(code, 1)
                self.assertEqual(report['status'], 'failed')
                if name == 'core_exit':
                    self.assertEqual(report['components']['join'], 'not-run')
                    self.assertFalse((out / 'join').exists())

    def test_zero_exit_without_honest_receipts_fails(self):
        for name in ('missing_core_receipt', 'missing_join_receipt', 'core_omit', 'dirty_join', 'wrong_hash'):
            with self.subTest(receipt=name):
                self.control = {name: True}
                code, report, _ = self.run_validation()
                self.assertEqual(code, 1)
                self.assertEqual(report['status'], 'failed')

    def test_public_source_mutation_stops_before_join(self):
        self.control['mutate_source'] = True
        code, report, out = self.run_validation()
        self.assertEqual(code, 1)
        self.assertIn('source identities changed', report['failure'])
        self.assertFalse((out / 'join').exists())

    def test_reviewed_entry_missing_rejects_before_output(self):
        (self.root / validation.JOIN_SCRIPTS[88]).unlink()
        args = self.args(join_round=88)
        with self.assertRaisesRegex(ValueError, 'Reviewed public entry missing'):
            validation.execute(args, self.root)
        self.assertFalse(Path(args.output).exists())

    def test_explicit_local_input_and_output_boundaries(self):
        for overrides in [dict(dotnet='relative'), dict(config='https://example.invalid/input'),
                          dict(packages='//server/share'), dict(join_round=94),
                          dict(output=str(self.root / 'poc/rejected'))]:
            with self.subTest(overrides=overrides):
                with self.assertRaises(ValueError):
                    validation.execute(self.args(**overrides), self.root)
        kept = self.root / 'local-private/kept'
        kept.mkdir(parents=True)
        marker = kept / 'marker.txt'
        marker.write_text('preserved')
        with self.assertRaises(ValueError):
            validation.execute(self.args(output=str(kept)), self.root)
        self.assertEqual(marker.read_text(), 'preserved')

    def test_linked_input_rejected(self):
        link = self.root / 'linked-cache'
        target = self.root / 'cache'
        self.assertTrue(link.parent.resolve().is_relative_to(self.root.resolve()))
        self.assertTrue(target.resolve().is_relative_to(self.root.resolve()))
        if os.name == 'nt':
            result = subprocess.run(['cmd', '/c', 'mklink', '/J', str(link), str(target)], capture_output=True)
            if result.returncode:
                self.skipTest('Private junction creation unavailable: ' + result.stderr.decode(errors='replace'))
        else:
            link.symlink_to(target, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, 'Linked path rejected'):
            validation.execute(self.args(packages=str(link)), self.root)

    def test_outer_timeout_kills_join_descendant(self):
        self.control['join_hang'] = True
        with patch.object(validation, 'COMPONENT_TIMEOUT_SECONDS', .5):
            code, report, out = self.run_validation()
        self.assertEqual(code, 1)
        self.assertEqual(report['commands'][-1]['status'], 'timeout')
        self.assertIn('spawned', (out / 'join.log').read_text())
        time.sleep(1.2)
        self.assertFalse((out / 'join/survived.txt').exists())


if __name__ == '__main__':
    unittest.main()
