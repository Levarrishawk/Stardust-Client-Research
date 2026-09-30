"""Independent installed DTII rows, authored box and 100-step normal-door motion/contact oracle."""
import argparse, contextlib, hashlib, json, math, struct, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

TABLE = 'datatables/appearance/door_style.iff'
TABLE_SHA = 'C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7'
LEAF_SHA = '86B5E5DB2B96C77FE8A262F07C8D26F538525B537217CE24B6FCE2CEF9AD3A57'
MESH_SHA = '8EE34404E8A7517BB51265970A6495A2E6A3AE0A6695619FD37D2F10674A7148'
def f32(v): return struct.unpack('<f', struct.pack('<f', v))[0]
def strings(raw, offset, count):
    values=[]
    for _ in range(count):
        end=raw.index(0,offset); values.append(raw[offset:end].decode('ascii'));offset=end+1
    return values,offset
def table_rows(raw):
    root,=nodes(raw);assert root[0]==b'DTII';version,=root[1];assert version[0]==b'0001'
    cols,types,rows=version[1];assert [cols[0],types[0],rows[0]]==[b'COLS',b'TYPE',b'ROWS']
    count=struct.unpack_from('<I',cols[1])[0];assert count==18
    names,end=strings(cols[1],4,count);assert end==len(cols[1])
    kinds,end=strings(types[1],0,count);assert end==len(types[1]) and kinds==list('ssssbffffffffbssss')
    raw=rows[1];count=struct.unpack_from('<I',raw)[0];offset=4;result=[]
    keys=['name','frame','primary','secondary','flip','x','y','z','open','close','spring','smooth','radius','force','e0','e1','e2','e3']
    for _ in range(count):
        values=[]
        for kind in kinds:
            if kind=='s': v,offset=strings(raw,offset,1);values.append(v[0])
            else: values.append(struct.unpack_from('<f' if kind=='f' else '<i',raw,offset)[0]);offset+=4
        r=dict(zip(keys,values));r['move']=[r.pop(k) for k in ['x','y','z']];r['effects']=[r.pop('e'+str(i)) for i in range(4)];result.append(r)
    assert offset==len(raw) and len({r['name'] for r in result})==count
    return result
def walk(tree):
    for tag,value in tree:
        yield tag,value
        if isinstance(value,list):yield from walk(value)
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    pins={}
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        def read(member):
            for index in indices:
                raw=index.lookup(member)
                if raw is not None:
                    assert raw, 'masked input';pins[member]=hashlib.sha256(raw).hexdigest().upper();return raw
            raise ValueError('Missing input '+member)
        rows=table_rows(read(TABLE));assert pins[TABLE]==TABLE_SHA
        selected=next(r for r in rows if r['name']=='cantina_door')
        apt=read(selected['primary']);assert pins[selected['primary']]==LEAF_SHA
        mesh_path=next(value.rstrip(b'\0').decode('ascii') for tag,value in walk(nodes(apt)) if tag==b'NAME')
        mesh=read(mesh_path)
        assert pins[mesh_path]==MESH_SHA
        appr=next(value for tag,value in walk(nodes(mesh)) if tag==b'APPR')
        box=next(value for tag,value in walk(appr) if tag==b'BOX ');hi=struct.unpack_from('<3f',box);lo=struct.unpack_from('<3f',box,12)
    run=subprocess.run(['dotnet',str(a.core_dll),'--door-style',str(a.root),'--configuration',str(a.configuration),'--style','cantina_door','--table-sha256',TABLE_SHA,'--leaf-sha256',LEAF_SHA],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-door.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    def records(prefix):return [json.loads(line[len(prefix):]) for line in run.stdout.splitlines() if line.startswith(prefix)]
    def near(x,y):
        if isinstance(x,dict):assert x.keys()==y.keys();[near(x[k],y[k]) for k in x]
        elif isinstance(x,(list,tuple)):assert len(x)==len(y);[near(u,v) for u,v in zip(x,y)]
        elif isinstance(x,float):assert abs(x-y)<=max(1e-7,abs(x)*1e-7),(x,y)
        else:assert x==y,(x,y)
    near(rows,records('door_table=')[0]);asset,=records('door_asset=');near(lo,asset['min']);near(hi,asset['max']);assert not asset['fallback']
    near(selected,asset['style']);assert {s['Path']:s['Sha256'] for s in asset['sources']}==pins
    frames=records('door_frame=');assert len(frames)==100
    fraction=0.;request=0;blocked=clear=0
    for i,r in enumerate(frames):
        opening=i-request<=1;raw=fraction+(f32(.025)/selected['open'] if opening else -f32(.025)/selected['close'])
        event=0 if fraction==0 and raw>0 else 1 if fraction<1 and raw>=1 else 2 if fraction==1 and raw<1 else 3 if fraction>0 and raw<=0 else -1
        fraction=f32(min(1,max(0,raw)));powered=fraction**(1/selected['spring']);curve=f32(powered*(1-selected['smooth'])+(1-math.cos(f32(math.pi)*powered))*.5*selected['smooth'])
        translation=[f32(v*curve) for v in selected['move']];retained=int(i<40)
        if retained:request=i
        near(fraction,r['fraction']);near(curve,r['curve']);near(translation,r['translation']);assert r['opening']==opening and r['transition']==event and r['retained']==retained
        assert r['active']==(fraction>0)
        assert r['effect']==('' if event<0 else selected['effects'][event])
        # Independent axial segment vs translated box expanded by actor half-widths.
        hit=lo[0]+translation[0]-.25<=0<=hi[0]+translation[0]+.25 and lo[1]-.9<=1<=hi[1]+.9
        status='Blocked' if hit else 'Clear';assert r['contact']['status']==status
        parameter=(lo[2]-.25+2)/4 if hit else 1.;near(parameter,r['contact']['parameter']);near([0,1,-2+4*parameter],r['contact']['center'])
        blocked+=int(hit);clear+=int(not hit)
    summary=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=pins,rows=len(rows),frames=len(frames),blocked=blocked,clear=clear,minimum=lo,maximum=hi)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(f'PASS original_rows={len(rows)} independent_frames={len(frames)} blocked={blocked} clear={clear} sources={len(pins)}')
if __name__=='__main__':main()
