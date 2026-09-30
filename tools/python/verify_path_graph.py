"""Independent deployed PGRF0001 fields and directed range-table agreement."""
import argparse,collections,contextlib,hashlib,json,math,struct,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

def inspect(graph):
    version,=graph;assert version[0]==b'0001';chunks=version[1];assert [tag for tag,_ in chunks]==[b'META',b'PNOD',b'PEDG',b'ECNT',b'ESTR']
    meta,pnod,pedg,ecnt,estr=[value for _,value in chunks];assert len(meta)==4
    def records(value,stride):
        n,=struct.unpack_from('<I',value);assert len(value)==4+n*stride;return value[4:]
    ns=list(struct.iter_unpack('<4i4f',records(pnod,32)));es=list(struct.iter_unpack('<2i2f',records(pedg,16)));counts=[x[0] for x in struct.iter_unpack('<i',records(ecnt,4))];starts=[x[0] for x in struct.iter_unpack('<i',records(estr,4))]
    assert len(counts)==len(starts)==len(ns);covered=set()
    for n in ns:assert all(math.isfinite(x) for x in n[4:]) and n[7]>=0
    for e in es:assert 0<=e[0]<len(ns) and 0<=e[1]<len(ns) and all(math.isfinite(x) and x>=0 for x in e[2:])
    for i,(count,start) in enumerate(zip(counts,starts)):
        assert count>=0 and (count==0 and -1<=start<=len(es) or count>0 and 0<=start and start+count<=len(es))
        for edge in range(start,start+count):assert edge not in covered and es[edge][0]==i;covered.add(edge)
    assert covered==set(range(len(es)))
    return dict(type=struct.unpack('<i',meta)[0],nodes=len(ns),edges=len(es),geometry_sha256=hashlib.sha256(meta+pnod+pedg+ecnt+estr).hexdigest().upper()),ns,es,counts,starts

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('floor_inventory',type=Path);p.add_argument('portal_inventory',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    inv=json.loads(a.floor_inventory.read_text(encoding='utf-8'))+json.loads(a.portal_inventory.read_text(encoding='utf-8'));records=[];types=collections.Counter();node_types=collections.Counter();empty_minus_one=0;id_ordinal_mismatch=0
    with contextlib.ExitStack() as stack:
        indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(a.root,a.configuration)]
        for item in inv:
            if item.get('graph_sha256',item.get('path_graph_sha256')) is None:continue
            for priority,index in indices:
                data=index.lookup(item['member'])
                if data is not None:break
            assert hashlib.sha256(data).hexdigest().upper()==item['sha256'];root,=nodes(data);version,=root[1];graph=next(value for tag,value in version[1] if tag==b'PGRF');record,ns,es,counts,starts=inspect(graph)
            records.append(dict(member=item['member'],priority=priority,sha256=item['sha256'],**record));types[record['type']]+=1;node_types.update(n[3] for n in ns);empty_minus_one+=sum(c==0 and s==-1 for c,s in zip(counts,starts));id_ordinal_mismatch+=sum(n[0]!=i for i,n in enumerate(ns))
    ordered=sorted(records,key=lambda r:r['member']);(a.private_output/'graphs.json').write_text(json.dumps(ordered,indent=2),encoding='utf-8');manifest=a.private_output/'members.txt';manifest.write_text('\n'.join(r['member'] for r in ordered)+'\n',encoding='utf-8');digest=hashlib.sha256(manifest.read_bytes()).hexdigest().upper()
    print(f'members_sha256={digest} graphs={len(records)} nodes={sum(r["nodes"] for r in records)} edges={sum(r["edges"] for r in records)} types={dict(types)} node_types={dict(node_types)} empty_minus_one={empty_minus_one} id_ordinal_mismatch={id_ordinal_mismatch}',flush=True)
    result=subprocess.run(['dotnet',str(a.core_dll),'--path-graph-list',str(a.root),'--configuration',str(a.configuration),'--members',str(manifest),'--expected-members-sha256',digest],capture_output=True,text=True,check=True,timeout=90);(a.private_output/'native-path-graph-agreement.txt').write_text(result.stdout,encoding='utf-8')
    native={r['member']:r for r in map(json.loads,(line[11:] for line in result.stdout.splitlines() if line.startswith('path_graph=')))};assert native=={r['member']:r for r in ordered}
    print(f'PASS independent PGRF graphs={len(records)}')
if __name__=='__main__':main()
