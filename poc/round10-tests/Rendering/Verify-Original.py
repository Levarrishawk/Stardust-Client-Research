"""Independent effective-overlay byte, priority, and SHA audit; emits no asset bytes."""
import contextlib
import hashlib
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index

root, configuration, output = map(Path, sys.argv[1:])
assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
assert output.stat().st_size < 1048576
rows = [json.loads(line[len('source='):]) for line in output.read_text(encoding='utf-8-sig').splitlines() if line.startswith('source=')]
assert len(rows) == len({r['Path'] for r in rows}) == 37
assert sum(r['Bytes'] for r in rows) == 4683848
with contextlib.ExitStack() as stack:
    indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(root, configuration)]
    for row in rows:
        for priority, index in indices:
            data = index.lookup(row['Path'])
            if data is not None:
                break
        else:
            raise ValueError('Missing effective source ' + row['Path'])
        assert priority == row['Priority'] and len(data) == row['Bytes'] and hashlib.sha256(data).hexdigest().upper() == row['Sha256'], row['Path']
print('PASS: independently read effective overlay priorities/bytes/SHA; 37 sources / 4683848 bytes; no original runtime claim.')
