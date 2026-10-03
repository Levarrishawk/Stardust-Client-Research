"""Bounded accounting/hash oracle for the R15 native retention receipt (no original asset reads)."""
import hashlib,json,re,struct,sys
from pathlib import Path
lines=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
r=json.loads(next(x.split('=',1)[1] for x in lines if x.startswith('retention_receipt=')))
assert lines[-1].startswith('PASS:')
loads=r['Loads'];assert len(loads)==3
expected=[(101,2937962913,39,4686371,10,80),(202,2907007746,27,1401794,7,62),(202,2907007746,27,1401794,7,62)]
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
for load,(oid,crc,reads,source_bytes,textures,mips) in zip(loads,expected):
    b=bytes.fromhex(load['Wire']);assert len(b)==47
    assert struct.unpack_from('<I',b,2)[0]==0xFE89DDEA
    assert struct.unpack_from('<Q',b,6)[0]==oid and struct.unpack_from('<I',b,42)[0]==crc
    raw=struct.unpack_from('<7I',b,14)
    assert raw==(0x80000000 if oid==101 else 0,0,0,0x3F800000,*[struct.unpack('<I',struct.pack('<f',v))[0] for v in ((10,20,30) if oid==101 else (40,50,60))])
    assert b[46]==0 and load['ArchiveClosed']
    assert (load['ExternalReads'],load['SourceBytes'])==(reads,source_bytes)
    sources=load['Sources'];assert len({s['Path'] for s in sources})==len(sources)==reads
    assert sum(s['Bytes'] for s in sources)==source_bytes
    assert all(s['Bytes']>0 and re.fullmatch('[0-9A-F]{64}',s['Sha256']) for s in sources)
    images=load['Textures'];assert len({t['Path'] for t in images})==len(images)==textures
    assert sum(len(t['Mips']) for t in images)==mips
    assert all(re.fullmatch('[0-9A-F]{64}',h) for t in images for h in t['Mips'])
assert loads[1]==loads[2], 'Same terminal create/source/mip/decoded closure after recreation'
assert len(r['States'])==2
for state,bits in zip(r['States'],[0xBFE00000,0x80000000]):
    assert state['HeightBits']==bits and state['LoaderCalls']==2 and state['HumanCurrent'] and state['TerminalCurrent']
    assert state['HumanFingerprint']==loads[0]['Fingerprint'] and state['TerminalFingerprint']==loads[1]['Fingerprint']
assert set(r['Negatives'])=={'changed-create-101','changed-create-202','swapped-recipe-101','swapped-recipe-202','changed-creature-option','changed-static-option'}
assert r['LoaderCalls']==3 and all(r[k] for k in ['ReplacementTicketDistinct','OldTerminalRejected','HumanRetained','CloseDrained'])
print(json.dumps({'passed':True,'loads':3,'initial_sources':66,'total_source_reads':93,'initial_source_bytes':6088165,'total_source_bytes':7489959,'initial_mips':142,'terminal_repeat_mips':62,'state_updates':2,'gate_negatives':6,'source_manifest_hashes':[digest(l['Sources']) for l in loads],'mip_manifest_hashes':[digest(l['Textures']) for l in loads],'scope':'Independent wire/accounting/hash expectations; native code checks references/ticket currentness. No encoded-origin or renderer attestation.'},indent=2))
