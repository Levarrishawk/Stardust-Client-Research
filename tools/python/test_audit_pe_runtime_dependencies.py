"""Synthetic module graphs/namespace bytes only; no real PE or loader execution."""
import struct
import unittest
from unittest.mock import patch
from types import SimpleNamespace
from pathlib import Path
import hashlib
import tempfile
from audit_pe_runtime_dependencies import (Auditor, FileRepository, Import, Module, dll_name,
                                          parse_apiset_v6, select_api_host, pinned_roots, private_output)
from audit_source_runtime_dependencies import source_exports


class Repository:
    def __init__(self, modules, apisets=None):
        self.modules = {module.key: module for module in modules}
        self.apisets = apisets or {}

    def resolve(self, name, importer):
        name = dll_name(name)
        if name.startswith(('api-', 'ext-')):
            name = select_api_host(self.apisets, name, importer)
        return name if name in self.modules else None

    def load(self, key):
        return self.modules[key]


def module(name, exports=None, imports=None, machine=0x14c):
    return Module(name, name, machine, exports or {}, imports or [])


def namespace_bytes():
    # One v6 contract, one default host. Little-endian structural bytes and
    # independent string regions; no installed Windows input is read.
    contract = 'api-test-l1-1-0'.encode('utf-16-le')
    host = 'host.dll'.encode('utf-16-le')
    start = 28 + 24 + 20
    size = start + len(contract) + len(host)
    return (struct.pack('<7I', 6, size, 0, 1, 28, 0, 0) +
            struct.pack('<6I', 0, start, len(contract), len(contract), 52, 1) +
            struct.pack('<5I', 0, 0, 0, start + len(contract), len(host)) + contract + host)


class Tests(unittest.TestCase):
    def audit(self, imports, others, **limits):
        root = module('root.dll', imports=imports)
        return Auditor(Repository([root] + others), **limits).run(['root.dll'])

    def test_name_and_ordinal(self):
        a = self.audit([Import('dep.dll', 'F'), Import('dep.dll', 7)], [module('dep.dll', {'F': None, 7: None})])
        self.assertEqual(a.report()['counts']['errors'], 0)
        self.assertEqual(len(a.modules), 2)

    def test_missing_module(self):
        self.assertEqual(self.audit([Import('absent', 'F')], []).errors[0]['status'], 'missing_module')

    def test_missing_export(self):
        self.assertEqual(self.audit([Import('dep', 'F')], [module('dep.dll')]).errors[0]['status'], 'missing_export')

    def test_exports_case_sensitive(self):
        self.assertEqual(self.audit([Import('DEP.DLL', 'f')], [module('dep.dll', {'F': None})]).errors[0]['status'], 'missing_export')

    def test_wrong_bitness(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': None}, machine=0x8664)])
        self.assertTrue(any(error.get('status') == 'wrong_machine' for error in a.errors))

    def test_named_forwarder(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': 'host.G'}), module('host.dll', {'G': None})])
        self.assertEqual(len(a.errors), 0)
        self.assertEqual(a.counts['forwarded'], 1)
        self.assertEqual(a.edges[0]['forwarders'][0]['symbol'], 'G')

    def test_ordinal_forwarder(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': 'host.#9'}), module('host.dll', {9: None})])
        self.assertEqual(len(a.errors), 0)

    def test_invalid_forwarder(self):
        for forward in ['invalid', 'host.#oops', 'host.']:
            with self.subTest(forward=forward):
                a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': forward})])
                self.assertEqual(a.errors[0]['status'], 'invalid_forwarder')

    def test_forwarder_cycle(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': 'host.G'}), module('host.dll', {'G': 'dep.F'})])
        self.assertEqual(a.errors[0]['status'], 'forwarder_cycle')

    def test_module_import_cycle(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': None}, [Import('dep', 'F')])])
        self.assertEqual(len(a.errors), 0)
        self.assertEqual(len(a.modules), 2)

    def test_forwarder_budget(self):
        a = self.audit([Import('dep', 'F')], [module('dep.dll', {'F': 'host.G'}), module('host.dll', {'G': None})], max_forwarders=1)
        self.assertEqual(a.errors[0]['status'], 'forwarder_limit')

    def test_module_and_edge_budgets(self):
        with self.assertRaisesRegex(ValueError, 'module budget'):
            self.audit([Import('dep', 'F')], [module('dep.dll')], max_modules=1)
        with self.assertRaisesRegex(ValueError, 'edge budget'):
            self.audit([Import('dep', 'F')], [module('dep.dll')], max_edges=0)

    def test_delay_classification(self):
        a = self.audit([Import('dep', 'F', 'delay')], [module('dep.dll', {'F': None})])
        self.assertEqual(a.counts['delay'], 1)
        self.assertEqual(a.edges[0]['kind'], 'delay')

    def test_api_defaults_and_aliases(self):
        mapping = {'api-test-l1-1-0': [('', 'default.dll'), ('owner.dll', 'alias.dll')]}
        self.assertEqual(select_api_host(mapping, 'API-TEST-L1-1-0.dll', 'OWNER.DLL'), 'alias.dll')
        self.assertEqual(select_api_host(mapping, 'api-test-l1-1-0', 'other.dll'), 'default.dll')
        self.assertIsNone(select_api_host(mapping, 'api-unknown', 'owner.dll'))

    def test_api_forwarder(self):
        repo = Repository([module('root.dll', imports=[Import('dep', 'F')]),
                           module('dep.dll', {'F': 'api-test-l1-1-0.G'}), module('host.dll', {'G': None})],
                          {'api-test-l1-1-0': [('', 'host.dll')]})
        self.assertEqual(len(Auditor(repo).run(['root.dll']).errors), 0)

    def test_namespace(self):
        self.assertEqual(parse_apiset_v6(namespace_bytes()), {'api-test-l1-1-0': [('', 'host.dll')]})

    def test_namespace_truncations(self):
        data = namespace_bytes()
        for end in range(len(data)):
            with self.subTest(end=end), self.assertRaises(ValueError):
                parse_apiset_v6(data[:end])

    def test_namespace_invalid_fields(self):
        for offset, value in [(0, 5), (4, 0xffffffff), (12, 0xffffffff), (16, 0xffffffff), (36, 3), (44, 0xffffffff)]:
            data = bytearray(namespace_bytes())
            struct.pack_into('<I', data, offset, value)
            with self.subTest(offset=offset), self.assertRaises((ValueError, UnicodeError)):
                parse_apiset_v6(bytes(data))

    def test_namespace_declared_size_shorter_than_header(self):
        for size in range(28):
            with self.subTest(size=size), self.assertRaises(ValueError):
                parse_apiset_v6(struct.pack('<7I', 6, size, 0, 0, 28, 0, 0))

    def test_late_dynamic_forwarder_drains_host_imports(self):
        root = module('root.dll', {'Dynamic': 'host.Entry'})
        host = module('host.dll', {'Entry': None}, [Import('absent', 'Required')])
        a = Auditor(Repository([root, host])).run(['root.dll'])
        self.assertEqual(len(a.edges), 0)
        self.assertEqual(a.symbol(root, 'Dynamic')[0], 'ok')
        # A direct dynamic symbol can exist while its newly discovered host
        # cannot complete imports. Publishing must drain, not hide that edge.
        report = a.report()
        self.assertEqual(report['counts']['errors'], 1)
        self.assertEqual(report['errors'][0]['from'], 'host.dll')
        self.assertEqual(report['errors'][0]['status'], 'missing_module')

    def test_module_parse_and_hash_share_one_blob(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'synthetic.dll'
            original, changed = b'synthetic parsed generation', b'different on-disk generation'
            path.write_bytes(original)
            parsed = []
            def parse(*args, data=None, **kwargs):
                parsed.append(data)
                return SimpleNamespace(FILE_HEADER=SimpleNamespace(Machine=0x14c),
                    OPTIONAL_HEADER=SimpleNamespace(Magic=0x10b),
                    parse_data_directories=lambda **kw: path.write_bytes(changed),
                    get_warnings=lambda: [], close=lambda: None)
            repo = FileRepository.__new__(FileRepository)
            repo.pefile=SimpleNamespace(PE=parse);repo.cache={};repo.runtime=Path(folder)
            result=repo.load(str(path))
            self.assertEqual(parsed, [original])
            self.assertEqual(result.metadata['bytes'], len(original))
            self.assertEqual(result.metadata['sha256'], hashlib.sha256(original).hexdigest().upper())
            self.assertNotEqual(result.metadata['sha256'], hashlib.sha256(path.read_bytes()).hexdigest().upper())

    def test_schema_parse_and_hash_share_one_blob(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'synthetic-schema.dll'
            original=namespace_bytes();path.write_bytes(original);parsed=[]
            def parse(*args, data=None, **kwargs):
                parsed.append(data)
                def get_data():
                    path.write_bytes(b'different schema generation')
                    return original
                return SimpleNamespace(sections=[SimpleNamespace(Name=b'.apiset',get_data=get_data)],close=lambda:None)
            with patch.dict('sys.modules', {'pefile': SimpleNamespace(PE=parse)}):
                repo=FileRepository(Path(folder),Path(folder),path)
            self.assertEqual(parsed,[original])
            self.assertEqual(repo.schema['sha256'],hashlib.sha256(original).hexdigest().upper())
            self.assertNotEqual(repo.schema['sha256'],hashlib.sha256(path.read_bytes()).hexdigest().upper())

    def test_path_names_rejected(self):
        for name in ['../host', r'C:\host', 'sub/host']:
            with self.subTest(name=name), self.assertRaises(ValueError):
                dll_name(name)

    def test_pinned_roots_require_containment_and_hash(self):
        with tempfile.TemporaryDirectory() as folder:
            runtime = Path(folder)
            digest = 'A' * 64
            result = pinned_roots(runtime, ['child/root.dll=' + digest.lower()])
            self.assertEqual(result, {str((runtime / 'child/root.dll').resolve()): digest})
            for specification in ['../outside.dll=' + digest, 'root.dll=bad', '=' + digest,
                                  str(runtime / 'root.dll') + '=' + digest]:
                with self.subTest(specification=specification), self.assertRaises(ValueError):
                    pinned_roots(runtime, [specification])
            with self.assertRaisesRegex(ValueError, 'duplicate'):
                pinned_roots(runtime, ['root.dll=' + digest, 'root.dll=' + digest])

    def test_output_requires_fresh_private_descendant(self):
        with tempfile.TemporaryDirectory() as folder:
            workspace = Path(folder)
            private = workspace / 'local-private'
            private.mkdir()
            with patch('audit_pe_runtime_dependencies.WORKSPACE', workspace):
                self.assertEqual(private_output(private / 'new/receipt'), (private / 'new/receipt').resolve())
                for path in [private, workspace / 'public', private / '../escape']:
                    with self.subTest(path=path), self.assertRaises(ValueError):
                        private_output(path)
                occupied = private / 'existing'
                occupied.mkdir()
                with self.assertRaises(ValueError):
                    private_output(occupied)

    def test_source_adapter_captures_literal_inputs_without_loading(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder)
            inputs = {
                'src/engine/client/library/clientGraphics/src/Bink/BinkDLL.cpp': b'_bind("_BinkOpen@8");\n_bind("_BinkOpen@8");',
                'src/engine/client/library/clientGraphics/src/Bink/BinkVideo.h': b'_bind("_BinkClose@4");',
                'src/engine/shared/library/sharedDebug/src/win32/DebugHelp.cpp': b'  GPA(module, MiniDumpWriteDump);\n',
            }
            for relative, blob in inputs.items():
                path = source / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(blob)
            blobs, requests = source_exports(source, 'renderer.dll', 'movie.dll', 'debug.dll')
            self.assertEqual(requests, [('renderer.dll', 'GetApi'), ('movie.dll', '_BinkClose@4'),
                                       ('movie.dll', '_BinkOpen@8'), ('debug.dll', 'MiniDumpWriteDump')])
            self.assertEqual(blobs, {source / path: blob for path, blob in inputs.items()})
            next(iter(blobs)).write_bytes(b'replaced after capture')
            self.assertEqual(blobs[source / next(iter(inputs))], next(iter(inputs.values())))

    def test_source_adapter_empty_extraction_rejects(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder)
            for relative in ['src/engine/client/library/clientGraphics/src/Bink/BinkDLL.cpp',
                             'src/engine/client/library/clientGraphics/src/Bink/BinkVideo.h',
                             'src/engine/shared/library/sharedDebug/src/win32/DebugHelp.cpp']:
                path = source / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(b'// no literal bindings')
            with self.assertRaisesRegex(ValueError, 'extraction empty'):
                source_exports(source, 'renderer.dll', 'movie.dll', 'debug.dll')


if __name__ == '__main__':
    unittest.main(verbosity=2)
