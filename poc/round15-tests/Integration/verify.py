"""Independent mixed-world state/source receipt oracle; no runtime or original packet capture."""
import json, struct, sys
from pathlib import Path
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
receipt=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'))
A=0xDABC001122334455; B=0xCABD998877665544
assert set(peer)=={'start','ready','human_create','human_end','human_destroy','terminal_create','terminal_end','terminal_destroy','human_baseline','human_height','terminal_baseline','terminal_delta','terminal_bad_family','late_bad'}
for prefix,oid,crc,x in [('human',A,2937962913,-1.5),('terminal',B,2907007746,1.5)]:
    raw=bytes.fromhex(peer[prefix+'_create'])
    fields=struct.unpack('<HIQ7fIB',raw)
    assert len(raw)==47 and fields[:3]==(5,0xFE89DDEA,oid)
    assert fields[3:10]==(0,0,0,1,x,0,0) and fields[10:]==(crc,255)
for name,oid,op,tag in [('human_baseline',A,0x68A75F0C,0x4352454F),('human_height',A,0x12862153,0x4352454F),('terminal_baseline',B,0x68A75F0C,0x54414E4F),('terminal_delta',B,0x12862153,0x54414E4F)]:
    raw=bytes.fromhex(peer[name]); count,actual_op,actual_oid,actual_tag,view,size=struct.unpack_from('<HIQIBI',raw)
    assert (count,actual_op,actual_oid,actual_tag,view)==(5,op,oid,tag,3) and size==len(raw)-23
assert receipt['Flags'] and all(value is True for value in receipt['Flags'].values())
loads=receipt['Loads']
assert len(loads)==3 and [x['Id'] for x in loads].count(A)==1 and [x['Id'] for x in loads].count(B)==2
for load in loads:
    prefix='human' if load['Id']==A else 'terminal'
    reads,total=(39,4686371) if prefix=='human' else (27,1401794)
    assert load['Reads']==reads and load['Bytes']==total and load['CreateHex']==peer[prefix+'_create']
    sources=load['Sources']
    assert len(sources)==reads and sum(s['Bytes'] for s in sources)==total
    assert len({s['Path'] for s in sources})==reads
    assert all(s['Bytes']>0 and len(s['Sha256'])==64 and len(bytes.fromhex(s['Sha256']))==32 for s in sources)
    root='object/creature/player/shared_human_male.iff' if prefix=='human' else 'object/tangible/terminal/shared_terminal_mission.iff'
    pin='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243' if prefix=='human' else 'E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281'
    assert next(s['Sha256'] for s in sources if s['Path']==root)==pin
states={s['Stage']:s for s in receipt['States']}
assert set(states)=={'initial','updated','recreated'}
for stage in states.values():
    assert len(stage['Objects'])==2
    h=next(o for o in stage['Objects'] if o['Id']==A); t=next(o for o in stage['Objects'] if o['Id']==B)
    assert h['Kind']=='CreatureDetail0' and t['Kind']=='StaticAppearance' and h['Typed'] and not t['Typed']
    assert t['OpaqueCount']==(1 if stage['Stage']=='initial' else 2 if stage['Stage']=='updated' else 0)
h0=next(o for o in states['initial']['Objects'] if o['Id']==A)
h1=next(o for o in states['updated']['Objects'] if o['Id']==A)
h2=next(o for o in states['recreated']['Objects'] if o['Id']==A)
t0=next(o for o in states['initial']['Objects'] if o['Id']==B)
t1=next(o for o in states['updated']['Objects'] if o['Id']==B)
t2=next(o for o in states['recreated']['Objects'] if o['Id']==B)
assert h0['Creation']==h1['Creation']==h2['Creation']
assert t0['Creation']==t1['Creation'] and t2['Creation']>t1['Creation']
assert h0['HeightBits']==0x3f800000 and h1['HeightBits']==h2['HeightBits']==struct.unpack('<I',struct.pack('<f',1.4))[0]
print('PASS: independent exact create/envelope/recipe/source totals, typed creature plus opaque terminal, stable unaffected incarnation and one-object recreation')

adapters=receipt['Adapters']
assert len(adapters)==3 and all(a['Disposals']==1 and a['Applications']>0 for a in adapters)
assert len([a for a in adapters if a['Id']==A])==1
assert next(a['HeightBits'] for a in adapters if a['Id']==A)==struct.unpack('<I',struct.pack('<f',1.4))[0]
assert len([a for a in adapters if a['Id']==B])==2
print('PASS: changed IDs, generic adapters, latest state, one static replacement, exactly-once retirement')
