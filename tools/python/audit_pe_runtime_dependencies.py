"""Read-only bounded PE dependency inference. Never loads an audited DLL.

Writes metadata only to an explicitly supplied private output directory.
Application-directory then explicit x86-system-directory search is an inference,
not a Windows loader trace (KnownDLLs/SxS/PATH/package policy are not emulated).
"""
from __future__ import annotations
import argparse
from dataclasses import dataclass, field
import hashlib
import json
from pathlib import Path
import struct
import sys
from collections import deque

WORKSPACE = Path(__file__).resolve().parents[2]


def dll_name(name):
    if not name or any(c in name for c in "\\/:"):
        raise ValueError("unsupported module name")
    name = name.casefold()
    return name if name.endswith('.dll') else name + '.dll'


def api_contract(name):
    value = name.casefold().removesuffix('.dll')
    return value.startswith(('api-', 'ext-'))


def parse_apiset_v6(data):
    """Bounds-checked v6 namespace; alias selection is retained per importer."""
    def words(offset, count):
        if offset < 0 or offset + count * 4 > len(data):
            raise ValueError('API-set bounds')
        return struct.unpack_from('<' + 'I' * count, data, offset)

    version, size, flags, count, entries, hashes, factor = words(0, 7)
    if version != 6 or size < 28 or size > len(data) or count > 16384:
        raise ValueError('unsupported API-set namespace')
    data = data[:size]

    def string(offset, length):
        if length % 2 or offset + length > len(data):
            raise ValueError('API-set string bounds')
        return data[offset:offset + length].decode('utf-16-le').casefold()

    result = {}
    for index in range(count):
        _, name_offset, name_length, hashed, values, value_count = words(entries + 24 * index, 6)
        if value_count > 1024:
            raise ValueError('API-set value limit')
        name = string(name_offset, name_length)
        if name in result:
            raise ValueError('duplicate API-set contract')
        hosts = []
        for item in range(value_count):
            _, alias_offset, alias_length, host_offset, host_length = words(values + 20 * item, 5)
            hosts.append((string(alias_offset, alias_length), string(host_offset, host_length)))
        result[name] = hosts
    return result


def select_api_host(mapping, name, importer):
    values = mapping.get(name.casefold().removesuffix('.dll'))
    if values is None:
        return None
    importer = importer.casefold().removesuffix('.dll')
    aliases = [host for alias, host in values if alias and alias.removesuffix('.dll') == importer]
    defaults = [host for alias, host in values if not alias]
    candidates = aliases or defaults
    if len(candidates) != 1 or not candidates[0]:
        return None
    return dll_name(candidates[0])


@dataclass(frozen=True)
class Import:
    dll: str
    symbol: str | int
    kind: str = 'import'


@dataclass
class Module:
    key: str
    name: str
    machine: int = 0x14c
    exports: dict = field(default_factory=dict)  # name/ordinal -> None or forwarder
    imports: list = field(default_factory=list)
    metadata: dict = field(default_factory=dict)


class FileRepository:
    def __init__(self, runtime, system, schema):
        import pefile
        self.pefile = pefile
        self.runtime = runtime.resolve()
        self.system = system.resolve()
        self.cache = {}
        self.catalog = {}
        self.collisions = []
        for folder in (self.runtime, self.system):
            for path in folder.iterdir():
                if path.is_file() and path.suffix.casefold() == '.dll':
                    key = path.name.casefold()
                    if key in self.catalog:
                        self.collisions.append(key)
                    else:
                        self.catalog[key] = str(path.resolve())
        schema_blob = schema.read_bytes()
        pe = pefile.PE(data=schema_blob, fast_load=True)
        try:
            sections = [section for section in pe.sections if section.Name.rstrip(b'\0') == b'.apiset']
            if len(sections) != 1:
                raise ValueError('expected one API-set section')
            self.apisets = parse_apiset_v6(sections[0].get_data())
        finally:
            pe.close()
        self.schema = {'path': str(schema), 'sha256': hashlib.sha256(schema_blob).hexdigest().upper(),
                       'namespace_version': 6, 'contracts': len(self.apisets)}

    def resolve(self, name, importer):
        name = dll_name(name)
        if api_contract(name):
            name = select_api_host(self.apisets, name, importer)
            if name is None:
                return None
        return self.catalog.get(name)

    def load(self, key):
        if key in self.cache:
            return self.cache[key]
        path = Path(key)
        blob = path.read_bytes()
        pe = self.pefile.PE(data=blob, fast_load=True)
        try:
            pe.parse_data_directories(directories=[0, 1, 13])
            exports = {}
            for symbol in getattr(getattr(pe, 'DIRECTORY_ENTRY_EXPORT', None), 'symbols', []):
                value = symbol.forwarder.decode('ascii') if symbol.forwarder else None
                # An EAT zero slot is not an exported callable address.
                if not symbol.address:
                    continue
                exports[symbol.ordinal] = value
                if symbol.name:
                    exports[symbol.name.decode('ascii')] = value
            imports = []
            for attribute, kind in [('DIRECTORY_ENTRY_IMPORT', 'import'), ('DIRECTORY_ENTRY_DELAY_IMPORT', 'delay')]:
                for descriptor in getattr(pe, attribute, []):
                    dll = descriptor.dll.decode('ascii')
                    for symbol in descriptor.imports:
                        imports.append(Import(dll, symbol.name.decode('ascii') if symbol.name else symbol.ordinal, kind))
            metadata = {'path': str(path), 'name': path.name, 'bytes': len(blob),
                        'sha256': hashlib.sha256(blob).hexdigest().upper(),
                        'machine': f'{pe.FILE_HEADER.Machine:04X}', 'pe_magic': f'{pe.OPTIONAL_HEADER.Magic:04X}',
                        'classification': 'application' if path.is_relative_to(self.runtime) else 'x86_windows',
                        'import_symbols': len(imports), 'parse_warnings': pe.get_warnings()[:8]}
            module = Module(key, path.name, pe.FILE_HEADER.Machine, exports, imports, metadata)
            self.cache[key] = module
            return module
        finally:
            pe.close()


class Auditor:
    def __init__(self, repository, max_modules=512, max_edges=100000, max_forwarders=32):
        self.repository = repository
        self.max_modules = max_modules
        self.max_edges = max_edges
        self.max_forwarders = max_forwarders
        self.modules = {}
        self.pending = deque()
        self.edges = []
        self.errors = []
        self.counts = {'direct': 0, 'forwarded': 0, 'delay': 0, 'apiset_requests': 0}

    def module(self, key):
        if key in self.modules:
            return self.modules[key]
        if len(self.modules) >= self.max_modules:
            raise ValueError('module budget')
        module = self.repository.load(key)
        self.modules[key] = module
        self.pending.append(module)
        if module.machine != 0x14c:
            self.errors.append({'kind': 'wrong_machine', 'module': module.name, 'machine': module.machine})
        return module

    def symbol(self, module, symbol, chain=()):
        token = (module.key, symbol)
        if token in chain:
            return 'forwarder_cycle', []
        if len(chain) >= self.max_forwarders:
            return 'forwarder_limit', []
        if module.machine != 0x14c:
            return 'wrong_machine', []
        if symbol not in module.exports:
            return 'missing_export', []
        forwarder = module.exports[symbol]
        if forwarder is None:
            return 'ok', []
        dll, separator, target = forwarder.rpartition('.')
        if not separator or not target:
            return 'invalid_forwarder', []
        if target.startswith('#'):
            try:
                target = int(target[1:])
            except ValueError:
                return 'invalid_forwarder', []
        key = self.repository.resolve(dll, module.name)
        hop = {'from': module.name, 'dll': dll, 'symbol': target}
        if key is None:
            return 'missing_forwarder_module', [hop]
        status, rest = self.symbol(self.module(key), target, chain + (token,))
        return status, [hop] + rest

    def run(self, roots):
        for key in roots:
            self.module(key)
        return self.drain()

    def drain(self):
        while self.pending:
            owner = self.pending.popleft()
            for item in owner.imports:
                if len(self.edges) >= self.max_edges:
                    raise ValueError('edge budget')
                if api_contract(item.dll):
                    self.counts['apiset_requests'] += 1
                key = self.repository.resolve(item.dll, owner.name)
                edge = {'from': owner.name, 'from_key': owner.key, 'requested': item.dll,
                        'symbol': item.symbol, 'kind': item.kind}
                if item.kind == 'delay':
                    self.counts['delay'] += 1
                if key is None:
                    edge.update(status='missing_module', forwarders=[])
                else:
                    target = self.module(key)
                    status, forwarders = self.symbol(target, item.symbol)
                    edge.update(status=status, resolved=target.name, resolved_key=key, forwarders=forwarders)
                    if forwarders:
                        self.counts['forwarded'] += 1
                    else:
                        self.counts['direct'] += 1
                self.edges.append(edge)
                if edge['status'] != 'ok':
                    self.errors.append(edge)
        return self

    def report(self):
        self.drain()  # late dynamic forwarder discovery must not hide host imports
        application_keys = {key for key, module in self.modules.items()
                            if module.metadata.get('classification') == 'application'}
        application_edges = [edge for edge in self.edges if edge['from_key'] in application_keys]
        return {'version': 1, 'mode': 'static_inference_not_loader_trace',
                'counts': {'modules': len(self.modules), 'edges': len(self.edges), 'errors': len(self.errors), **self.counts},
                'application_import_counts': {'modules': len(application_keys), 'symbols': len(application_edges),
                                             'unresolved': sum(edge['status'] != 'ok' for edge in application_edges),
                                             'delay': sum(edge['kind'] == 'delay' for edge in application_edges)},
                'modules': [module.metadata or {'name': module.name} for module in self.modules.values()],
                'errors': self.errors, 'edges': self.edges}


def sha256(blob):
    return hashlib.sha256(blob).hexdigest().upper()


def common_arguments(parser):
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--system', type=Path, required=True,
                        help='explicit x86 system DLL directory; no automatic WOW64 redirection')
    parser.add_argument('--schema', type=Path, required=True)
    parser.add_argument('--expected-schema-sha256', required=True)
    parser.add_argument('--root', action='append', required=True, metavar='RELATIVE_PATH=SHA256',
                        help='repeat for every explicit application root, including dynamic providers')
    parser.add_argument('--dynamic', action='append', default=[], metavar='ROOT_NAME!EXPORT',
                        help='explicit root export; #N selects an ordinal')
    parser.add_argument('--out', type=Path, required=True,
                        help='fresh output directory below this repository local-private')
    parser.add_argument('--pefile-path', type=Path,
                        help='optional Python package directory, relative to the research root or absolute')


def pinned_roots(runtime, specifications):
    runtime = runtime.resolve()
    roots = {}
    for specification in specifications:
        relative, separator, expected = specification.rpartition('=')
        path = (runtime / relative).resolve()
        if (not separator or not relative or Path(relative).is_absolute() or
                not path.is_relative_to(runtime) or path == runtime or
                len(expected) != 64 or any(c not in '0123456789abcdefABCDEF' for c in expected)):
            raise ValueError('root must be a runtime-relative path and a 64-digit SHA-256')
        if str(path) in roots:
            raise ValueError('duplicate root')
        roots[str(path)] = expected.upper()
    return roots


def private_output(path):
    out = path.resolve()
    private = (WORKSPACE / 'local-private').resolve()
    if out == private or not out.is_relative_to(private) or out.exists():
        raise ValueError('output must be a fresh directory below research local-private')
    return out


def run_audit(args, source_blobs=None, source_dynamic=None):
    """All inputs are read as data; no audited executable/DLL is loaded.

    Captured source blobs may add a bounded set of literal dynamic exports.
    Final checks detect a changed backing file, not arbitrary transient rewrites.
    """
    out = private_output(args.out)
    roots = pinned_roots(args.runtime, args.root)
    if args.pefile_path:
        package_path = (WORKSPACE / args.pefile_path).resolve()
        if not package_path.is_dir():
            raise ValueError('pefile package directory absent')
        sys.path.insert(0, str(package_path))
    for path, expected in roots.items():
        if sha256(Path(path).read_bytes()) != expected:
            raise ValueError('root hash mismatch: ' + Path(path).name)
    if sha256(args.schema.read_bytes()) != args.expected_schema_sha256.upper():
        raise ValueError('API-set schema hash mismatch')
    repository = FileRepository(args.runtime, args.system, args.schema)
    if repository.schema['sha256'] != args.expected_schema_sha256.upper():
        raise ValueError('parsed API-set schema identity mismatch')
    auditor = Auditor(repository).run(list(roots))
    for key, expected in roots.items():
        if auditor.modules[key].metadata['sha256'] != expected:
            raise ValueError('parsed root identity mismatch')
    by_name = {}
    for key in roots:
        name = Path(key).name.casefold()
        if name in by_name:
            raise ValueError('explicit roots have ambiguous basenames')
        by_name[name] = auditor.modules[key]
    requested = list(source_dynamic or [])
    for specification in args.dynamic:
        name, separator, export = specification.partition('!')
        if not separator or not name or not export:
            raise ValueError('dynamic request must be ROOT_NAME!EXPORT')
        requested.append((name, int(export[1:]) if export.startswith('#') else export))
    dynamic = []
    for name, export in requested:
        if name.casefold() not in by_name:
            raise ValueError('dynamic provider must be an explicitly pinned root: ' + name)
        status, chain = auditor.symbol(by_name[name.casefold()], export)
        dynamic.append({'module': name, 'export': export, 'status': status, 'forwarders': chain})
    # report() drains even hosts discovered only through these dynamic exports.
    report = auditor.report()
    report.update(root_hashes=roots, dynamic_exports=dynamic, api_set_schema=repository.schema,
                  application_system_name_collisions=sorted(repository.collisions),
                  limits={'max_modules': auditor.max_modules, 'max_edges': auditor.max_edges,
                          'max_forwarders': auditor.max_forwarders},
                  search_policy=['application directory', 'explicit x86 Windows directory'],
                  scope_limits=['Static inference, not a Windows loader trace',
                                'KnownDLL/SxS/PATH/package/already-loaded policy not emulated',
                                'Exact API-set v6 names and importer aliases only; version aliases not modeled',
                                'Optional/delay Windows branches are exploratory, not client failure counts',
                                'Dynamic export presence does not prove execution, ABI or semantics',
                                'Arbitrary dynamic/provider discovery is outside the enumerated requests',
                                'No clean-install, redistribution-license, process-memory or GPU claim'])
    captured = source_blobs or {}
    report['source_identity'] = {str(path): sha256(blob) for path, blob in captured.items()}
    for module in auditor.modules.values():
        if sha256(Path(module.key).read_bytes()) != module.metadata['sha256']:
            raise ValueError('audited module changed during audit')
    for path, blob in captured.items():
        if path.read_bytes() != blob:
            raise ValueError('source bind list changed during audit')
    if sha256(args.schema.read_bytes()) != repository.schema['sha256']:
        raise ValueError('API-set schema changed during audit')
    for key, expected in roots.items():
        if sha256(Path(key).read_bytes()) != expected:
            raise ValueError('root changed during audit')
    report['identity_after_matches'] = True
    summary = {'counts': report['counts'], 'application_import_counts': report['application_import_counts'],
               'dynamic_exports': len(dynamic),
               'dynamic_missing': [entry for entry in dynamic if entry['status'] != 'ok'],
               'identity_after_matches': True}
    out.mkdir(parents=True)
    (out / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    (out / 'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary))
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    common_arguments(parser)
    run_audit(parser.parse_args())


if __name__ == '__main__':
    main()

