"""Independent count-free CIDM byte oracle and exact namespace projection; no proprietary rows exported."""
import contextlib, hashlib, json, struct, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
root, config, output = map(Path, sys.argv[1:])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper() == '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
    for priority, archive in archives(root, config):
        data = Index(stack.enter_context(archive.open('rb'))).lookup('customization/customization_id_manager.iff')
        if data is not None:
            break
    else:
        raise AssertionError('Missing dictionary')
assert (priority, archive.name, len(data)) == (53, 'stardust_3_01.tre', 3697)
pin = hashlib.sha256(data).hexdigest().upper()
assert pin == 'FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2'
# Exact byte offsets/declared extents for this one original envelope, independent of native IFF parsing.
assert data[:4] == b'FORM' and struct.unpack_from('>I', data, 4)[0] == len(data)-8 and data[8:12] == b'CIDM'
assert data[12:16] == b'FORM' and struct.unpack_from('>I', data, 16)[0] == len(data)-20 and data[20:24] == b'0001'
assert data[24:28] == b'DATA' and struct.unpack_from('>I', data, 28)[0] == len(data)-32
rows = []; cursor = 32
while cursor < len(data):
    ident = struct.unpack_from('<H', data, cursor)[0]; cursor += 2
    end = data.index(0, cursor); full = data[cursor:end].decode('ascii'); cursor = end+1
    prefix = next((p for p in ('/private/', '/shared_owner/') if full.startswith(p) and len(full)>len(p)), None)
    rows.append(dict(Id=ident, FullName=full, Projection='Projected' if prefix else 'UnsupportedNamespace', VariableName=full[len(prefix):] if prefix else None, FactoryFlag=prefix=='/private/' if prefix else None))
assert len(rows)==121 and [r['Id'] for r in rows]==list(range(1,122)) and len({r['FullName'] for r in rows})==121
assert rows[117]['FullName']=='private/alternate_shader_blade' and rows[117]['Projection']=='UnsupportedNamespace'
lines=output.read_text(encoding='utf-8-sig').splitlines(); assert lines[-1].startswith('PASS:')
native=json.loads(next(line.split('=',1)[1] for line in lines if line.startswith('cidm=')))
assert native['Rows']==rows and (native['Priority'],native['Bytes'],native['Sha256'])==(priority,len(data),pin)
assert native['InputOwned'] and native['ArchiveClosed'] and native['NegativeCases']==len(native['Cases']) and len(set(native['Cases']))==len(native['Cases'])
print(json.dumps(dict(passed=True, rows=121, bytes=len(data), priority=priority, sha256=pin, projected=sum(r['Projection']=='Projected' for r in rows), unprojected=sum(r['Projection']=='UnsupportedNamespace' for r in rows), negative_cases=native['NegativeCases'], original_rows_match=True, id118_preserved=True),indent=2))
