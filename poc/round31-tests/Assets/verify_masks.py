"""Independent MAPS version-form fixtures and one pinned Tatooine mask oracle."""
import contextlib, hashlib, json, math, struct, sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SOURCE='6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
MAP='D3F7B55C110D6454E328507743FA0CF4A126CD6D8A802DC040752543CC18AD9D'
PATH='terrain/tatooine.trn'
def digest(b):return hashlib.sha256(b).hexdigest().upper()
def chunk(tag,b):return tag+struct.pack('>I',len(b))+b
def form(tag,b=b''):return chunk(b'FORM',tag+b)
def encode(world=0x42F70000,meters=0x3F400000,row=2,dimension=10,w=None,s=None,parts=None,version=b'0001'):
    w=bytes((i*37+1)&255 for i in range(20)) if w is None else w
    s=bytes((255-i*19)&255 for i in range(20)) if s is None else s
    parts=[chunk(b'DATA',struct.pack('<4I',world,meters,row,dimension)),chunk(b'WMAP',w),chunk(b'SMAP',s)] if parts is None else parts
    return form(version,b''.join(parts))
DEFAULT=dict(MaximumInputBytes=16777216,MaximumBitDimension=8192,MaximumBytesPerRow=1048576,MaximumMaskBytes=8388608)
def fixtures():
    cases=[]
    def add(name,b,kind='None',limits=None):cases.append(dict(Name=name,Hex=b.hex().upper(),Reject=kind,Limits=limits or DEFAULT))
    full=encode();exact=dict(MaximumInputBytes=len(full),MaximumBitDimension=10,MaximumBytesPerRow=2,MaximumMaskBytes=20)
    add('exact-padded-nonaligned-grid',full,limits=exact);add('empty',form(b'0000'));add('empty-zero-mask-cap',form(b'0000'),limits={**DEFAULT,'MaximumMaskBytes':0})
    add('positive-subnormals',encode(world=1,meters=1));add('single-bit-grid',encode(world=0x3F800000,meters=0x40000000,row=1,dimension=1,w=b'\x01',s=b'\x80'))
    for k in exact:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
    for field in ('world','meters'):
        for bits in (0,0x80000000,0xBF800000,0x7F800000,0xFF800000,0x7FC00000):add('bad-'+field+'-'+str(bits),encode(**{field:bits}),'Input')
    for row,dim in ((0,10),(2,0),(1,10),(0xFFFFFFFF,10),(2,0xFFFFFFFF),(1048576,32768)):
        add('bad-shape-'+str(row)+'-'+str(dim),encode(row=row,dimension=dim),'Input')
    data=chunk(b'DATA',struct.pack('<ffII',123.5,.75,2,10));water=chunk(b'WMAP',bytes(20));seed=chunk(b'SMAP',bytes(20))
    for i,parts in enumerate(([water,data,seed],[data,seed,water],[data,water],[data,water,seed,seed],[chunk(b'DATA',bytes(15)),water,seed],[chunk(b'DATA',bytes(17)),water,seed],[data,chunk(b'WMAP',bytes(19)),seed],[data,water,chunk(b'SMAP',bytes(21))],[data,form(b'WMAP'),seed],[chunk(b'DATX',bytes(16)),water,seed])):
        add('bad-records-'+str(i),encode(parts=parts),'Input')
    add('unknown-version',encode(version=b'0002'),'Input');add('literal-MAPS-wrapper',form(b'MAPS',full),'Input');add('empty-with-record',form(b'0000',data),'Input');add('trailing-byte',full+b'\0','Input');add('second-root',full+form(b'0000'),'Input')
    for i in range(len(full)):add('truncated-prefix-'+str(i),full[:i],'Input')
    huge=bytearray(full);struct.pack_into('>I',huge,4,0xFFFFFFFF);add('negative-looking-u32-extent',huge,'Input')
    for k,v in [('MaximumInputBytes',11),('MaximumInputBytes',67108865),('MaximumBitDimension',0),('MaximumBitDimension',32769),('MaximumBytesPerRow',0),('MaximumBytesPerRow',1048577),('MaximumMaskBytes',-1),('MaximumMaskBytes',33554433)]:
        add('invalid-limit-'+k+str(v),full,'Options',{**DEFAULT,k:v})
    return cases
def records(b,start,end):
    while start<end:
        assert end-start>=8;tag=b[start:start+4];size=struct.unpack_from('>I',b,start+4)[0];stop=start+8+size;assert stop<=end
        yield tag,b[start+8:stop],start,stop;start=stop
    assert start==end
def unpack(b):
    root,=records(b,0,len(b));assert root[0]==b'FORM';version=root[1][:4].decode()
    if version=='0000':assert len(root[1])==4;return dict(Version=version,WorldSizeBits=0,MetersPerBitBits=0,BytesPerRow=0,BitDimension=0,BytesPerMask=0,WmapSha256=digest(b''),SmapSha256=digest(b'')),b'',b''
    chunks=list(records(b,12,len(b)));assert [c[0] for c in chunks]==[b'DATA',b'WMAP',b'SMAP'];world,meters,row,dimension=struct.unpack('<4I',chunks[0][1]);w,s=chunks[1][1],chunks[2][1]
    assert len(w)==len(s)==row*dimension
    return dict(Version=version,WorldSizeBits=world,MetersPerBitBits=meters,BytesPerRow=row,BitDimension=dimension,BytesPerMask=len(w),WmapSha256=digest(w),SmapSha256=digest(s)),w,s
def bit(mask,row,dim,x,y):return 0<=x<dim and 0<=y<dim and bool(mask[y*row+x//8]&(1<<(x%8)))
def samples(meta,w,s):
    d=meta['BitDimension'];row=meta['BytesPerRow'];coords=[(-1,0),(0,-1),(0,0),(7,0),(8,0),(9,0),(d-1,d-1),(d,d-1),(1024,1024)]
    result=[]
    for kind,mask in [('Wmap',w),('Smap',s)]:
        chosen=list(coords)
        for i,value in enumerate(mask):
            if value:
                x=(i%row)*8+(value&-value).bit_length()-1;y=i//row
                if x<d:chosen.append((x,y));break
        result += [dict(Kind=kind,X=x,Y=y,Value=bit(mask,row,d,x,y)) for x,y in chosen]
    return result
def original(root,cfg):
    assert digest(cfg.read_bytes())==CONFIG
    with contextlib.ExitStack() as stack:
        for priority,file in archives(root,cfg):
            index=Index(stack.enter_context(file.open('rb')));raw=index.lookup(PATH)
            if raw is not None:break
        else:raise AssertionError(PATH)
    assert digest(raw)==SOURCE and len(raw)==1188446 and priority==42
    top,=records(raw,0,len(raw));assert top[0]==b'FORM' and top[1][:4]==b'PTAT'
    version,=records(raw,12,len(raw));assert version[1][:4]==b'0014'
    matches=[r for r in records(raw,version[2]+12,version[3]) if r[0]==b'FORM' and r[1][:4]==b'0001'];maps,=matches
    encoded=raw[maps[2]:maps[3]];assert digest(encoded)==MAP and len(encoded)==1048628
    meta,w,s=unpack(encoded)
    return dict(Root=root.as_posix(),Configuration=cfg.as_posix(),ConfigSha256=CONFIG,Path=PATH,Priority=priority,SourceBytes=len(raw),SourceSha256=SOURCE,MapOffset=maps[2],MapBytes=len(encoded),MapSha256=MAP,WorldSize=struct.unpack('<f',struct.pack('<I',meta['WorldSizeBits']))[0],MetersPerBit=struct.unpack('<f',struct.pack('<I',meta['MetersPerBitBits']))[0],Metadata=meta,Points=samples(meta,w,s))
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('MAPS fixtures:',len(fixtures()));sys.exit()
root,cfg,output=map(Path,sys.argv[1:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);assert len(r['Cases'])==len(fixtures())
for f,a in zip(fixtures(),r['Cases']):
    assert a['Name']==f['Name'] and a['Reject']==f['Reject']
    if f['Reject']=='None':assert a['Metadata']==unpack(bytes.fromhex(f['Hex']))[0]
meta,w,s=unpack(encode())
for point in r['GridPoints']:assert point['Value']==bit(w if point['Kind']=='Wmap' else s,meta['BytesPerRow'],meta['BitDimension'],point['X'],point['Y'])
for q in r['Rectangles']:
    d=meta['BitDimension'];x0,x1=max(0,min(q['X0'],q['X1'])),min(d-1,max(q['X0'],q['X1']));y0,y1=max(0,min(q['Y0'],q['Y1'])),min(d-1,max(q['Y0'],q['Y1']))
    expected=any(bit(w if q['Kind']=='Wmap' else s,meta['BytesPerRow'],d,x,y) for y in range(y0,y1+1) for x in range(x0,x1+1));assert q['Value']==expected
assert r['Original']==original(root,cfg) and r['OwnershipChecks']==4 and r['QueryRejections']==5
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),rejected=sum(f['Reject']!='None' for f in fixtures()),grid_points=len(r['GridPoints']),rectangles=len(r['Rectangles']),query_rejections=5,ownership_checks=4,original=r['Original']),indent=2))
