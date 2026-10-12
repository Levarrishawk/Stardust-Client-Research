"""Conservative Mos Taike height-scope audit over original TRN bytes and a decoded layer inventory."""
import argparse,struct,json,hashlib
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--terrain',type=Path,required=True)
parser.add_argument('--layers',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
a=parser.parse_args()
raw=a.terrain.read_bytes();g=raw.index(b'TGEN')-8
j=json.loads(a.layers.read_text());layers=j['Layers']
terrain_sha=hashlib.sha256(raw).hexdigest()
assert terrain_sha=='6039d0c15f9ccb518d72ae01a1abdf41fcff2153b5cbf7eb3ba6e6c634a3a427'
generator_end=g+8+struct.unpack_from('>I',raw,g+4)[0]
assert hashlib.sha256(raw[g:generator_end]).hexdigest().upper()==j['GeneratorSha256']
print('TerrainSha256='+terrain_sha+' LayersSha256='+hashlib.sha256(a.layers.read_bytes()).hexdigest())
box=(3705,3895,2265,2455)
def polygon_intersects(pts):
    corners=[(box[0],box[2]),(box[1],box[2]),(box[1],box[3]),(box[0],box[3])]
    if any(box[0]<=x<=box[1] and box[2]<=z<=box[3] for x,z in pts):return True
    def orient(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    edges=list(zip(pts,pts[1:]+pts[:1]))
    for a,b in edges:
        for c,d in zip(corners,corners[1:]+corners[:1]):
            if max(a[0],b[0])<min(c[0],d[0]) or min(a[0],b[0])>max(c[0],d[0]) or max(a[1],b[1])<min(c[1],d[1]) or min(a[1],b[1])>max(c[1],d[1]):continue
            if orient(a,b,c)*orient(a,b,d)<=0 and orient(c,d,a)*orient(c,d,b)<=0:return True
    for x,z in corners:
        inside=False
        for (ax,az),(bx,bz) in edges:
            if (az>z)!=(bz>z) and x<(bx-ax)*(z-az)/(bz-az)+ax:inside=not inside
        if inside:return True
    return False
def blocks(pos,end):
    while pos<end:
        size=struct.unpack_from('>I',raw,pos+4)[0];stop=pos+8+size;assert stop<=end
        yield raw[pos:pos+4],pos+8,stop
        pos=stop
    assert pos==end
def fields(s):
    at=g+s['Offset'];end=at+s['EncodedBytes'];_,v,e=list(blocks(at+12,end))[0]
    chunks=list(blocks(v+4,e));_,h,he=chunks[0];_,hv,hve=list(blocks(h+4,he))[0];_,d,de=list(blocks(hv+4,hve))[0]
    enabled=struct.unpack_from('<I',raw,d)[0]
    tag,a,b=chunks[1]
    if tag==b'FORM':
        assert raw[a:a+4]==b'DATA'
        matches=[(x,y) for t,x,y in blocks(a+4,b) if t==b'DATA'];assert len(matches)==1
        a,b=matches[0]
    return enabled,raw[a:b]
def boundary(s):
    enabled,d=fields(s)
    if not enabled:return None
    t=s['FormType'];v=s['Version']
    if t=='BCIR' and v=='0002':
        x,z,r,ft,f=struct.unpack('<fffIf',d)
        nx=max(box[0],min(x,box[1]));nz=max(box[2],min(z,box[3]))
        outside=(nx-x)**2+(nz-z)**2>r*r
        inside=max((xx-x)**2+(zz-z)**2 for xx in box[:2] for zz in box[2:])<(r*(1-f))**2
        return outside,inside
    if t=='BREC' and v=='0003':
        x1,z1,x2,z2=struct.unpack_from('<4f',d)
        # Rotation/extension semantics cannot be discarded for conservative exclusion.
        flags=struct.unpack_from('<2I',d,24)
        if any(flags):return False,False
        return max(x1,x2)<box[0] or min(x1,x2)>box[1] or max(z1,z2)<box[2] or min(z1,z2)>box[3],False
    if t=='BPOL' and v in ('0005','0006'):
        n=struct.unpack_from('<I',d)[0];pts=[struct.unpack_from('<2f',d,4+i*8) for i in range(n)]
        if not pts:return False,False
        return not polygon_intersects(pts),False
    return False,False
def zero(layer):
    if not layer['Header']['EnabledBits']:return 'disabled'
    bs=[boundary(s) for s in layer['Children'] if s['FormType'] in ('BCIR','BREC','BPOL','BPLN')]
    bs=[b for b in bs if b is not None]
    if bs:
        if layer['AdtaWordBits'][0] and any(b[1] for b in bs):return 'inverted boundary fully inside'
        if not layer['AdtaWordBits'][0] and all(b[0] for b in bs):return 'outside all boundary bounds'
    return None
result=[]
for layer in layers:
    height=[s for s in layer['Children'] if (s['FormType'] or '').startswith('AH') or s['FormType'] in ('AROA','ARIV')]
    if not height:continue
    chain=[];n=layer
    while True:
        reason=zero(n)
        if reason:chain.append((n['Index'],reason));break
        if n['ParentIndex'] is None:break
        n=layers[n['ParentIndex']]
    roadbounds=[]
    if not chain and all(s['FormType']=='AROA' for s in height):
        for s in height:
            assert s['Version']=='0005'
            enabled,d=fields(s);count=struct.unpack_from('<I',d)[0]
            assert count>=2 and len(d)==4+count*8+24
            pts=[struct.unpack_from('<2f',d,4+i*8) for i in range(count)];width=struct.unpack_from('<f',d,4+count*8)[0];assert width>=0
            r=(min(x for x,z in pts)-width,max(x for x,z in pts)+width,min(z for x,z in pts)-width,max(z for x,z in pts)+width)
            outside=r[1]<box[0] or r[0]>box[1] or r[3]<box[2] or r[2]>box[3]
            roadbounds.append(dict(Offset=s['Offset'],Bounds=r,Outside=outside,Enabled=enabled))
        if all(r['Outside'] or not r['Enabled'] for r in roadbounds):chain.append((layer['Index'],'all roads own padded extent disjoint'))
    row=dict(Index=layer['Index'],Name=layer['Name'],ExcludedBy=chain,HeightTypes=[s['FormType'] for s in height],RoadBounds=roadbounds)
    result.append(row)
    if not chain:print(row)
candidates=[r['Index'] for r in result if not r['ExcludedBy']]
assert candidates==[0,3,4,5,146,148,151,153,155,157,159,161],candidates
a.output.write_text(json.dumps(dict(TerrainSha256=terrain_sha,LayersSha256=hashlib.sha256(a.layers.read_bytes()).hexdigest(),Bounds=box,Policy='Conservative spatial exclusion using source bytes and decoded hierarchy; candidates are not confirmed active. Unknown shapes are never excluded.',Layers=result),indent=2))
print('height layers',len(result),'excluded',sum(bool(r['ExcludedBy']) for r in result))
