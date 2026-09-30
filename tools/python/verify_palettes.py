"""Independent RIFF palette fields, opaque ARGB and zero-index fallback agreement across effective winners."""
import argparse,contextlib,hashlib,json,struct,subprocess,collections
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index

def parse(data):
    assert 24<=len(data)<=4120 and data[:4]==b'RIFF' and data[8:16]==b'PAL data'
    riff,size=struct.unpack_from('<I',data,4)[0],struct.unpack_from('<I',data,16)[0];version,count=struct.unpack_from('<HH',data,20)
    assert version>>8==3 and count<=1024 and size==4+count*4 and riff==size+12 and 20+size<=len(data)
    entries=[tuple(data[24+i*4:28+i*4]) for i in range(count)];trailing=data[20+size:]
    packed=[0xff000000|(r<<16)|(g<<8)|b for r,g,b,flag in entries]
    records=bytearray(struct.pack('<Hi',version,count))
    for entry,color in zip(entries,packed):records.extend(bytes(entry));records.extend(struct.pack('<I',color))
    records.extend(struct.pack('<i',len(trailing)));records.extend(trailing)
    queries=bytearray()
    for index in list(range(count))+[-2147483648,-1,count,count+1,2147483647]:
        queries.extend(struct.pack('<i?',index,bool(count)))
        if count:
            actual=index if 0<=index<count else 0;queries.extend(struct.pack('<iIB',actual,packed[actual],entries[actual][3]))
    return dict(version=version,entries=entries,trailing=len(trailing),trailing_nonzero=sum(v!=0 for v in trailing),diagnostics=int(version&255!=0),
        records_sha256=hashlib.sha256(records).hexdigest().upper(),selections_sha256=hashlib.sha256(queries).hexdigest().upper())

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('output',type=Path);p.add_argument('--core-dll',type=Path);p.add_argument('--cshd-inventory',type=Path);a=p.parse_args()
    private=(Path(__file__).resolve().parents[2]/'local-private').resolve();assert a.output.resolve().is_relative_to(private)
    a.output.mkdir(parents=True,exist_ok=True);seen=set();result={};masks=0
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for priority,index in indices:
            for path,record in index.entries.items():
                if not path.endswith('.pal') or path in seen:continue
                seen.add(path)
                if not record[1]:masks+=1;continue
                data=index.lookup(path);result[path]=dict(priority=priority,sha256=hashlib.sha256(data).hexdigest().upper(),**parse(data))
    members=a.output/'members.txt';member_bytes=('\n'.join(sorted(result))+'\n').encode('ascii');members.write_bytes(member_bytes);pin=hashlib.sha256(member_bytes).hexdigest().upper()
    inventory=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),mounts=len(indices),members_sha256=pin,members=result)
    (a.output/'inventory.json').write_text(json.dumps(inventory,indent=2),encoding='utf-8')
    if a.core_dll:
        output=subprocess.run(['dotnet',str(a.core_dll),'--palette-list',str(a.root),'--configuration',str(a.configuration),'--members',str(members),'--expected-members-sha256',pin],capture_output=True,text=True,check=True,timeout=120).stdout
        for path,v in result.items():
            expected=f'palette_member={path} priority={v["priority"]} sha256={v["sha256"]} version={v["version"]:04X} entries={len(v["entries"])} trailing={v["trailing"]} diagnostics={v["diagnostics"]} records_sha256={v["records_sha256"]} selections_sha256={v["selections_sha256"]}'
            assert expected in output,('Native palette disagreement',path)
        assert f'PASS palette_inventory members={len(result)}' in output
        (a.output/'native-agreement.txt').write_text(output,encoding='utf-8')
    if a.cshd_inventory:
        shaders=json.loads(a.cshd_inventory.read_text(encoding='utf-8'));assert shaders['configuration_sha256']==inventory['configuration_sha256']
        refs={factor[3] for shader in shaders['members'].values() for factor in shader['factors']};missing=sorted(refs-result.keys())
        out_of_range=[(name,factor[0],factor[3],factor[4]) for name,shader in shaders['members'].items() for factor in shader['factors'] if factor[3] in result and not 0<=factor[4]<len(result[factor[3]]['entries'])]
        (a.output/'shader-references.json').write_text(json.dumps(dict(paths=len(refs),missing=missing,out_of_range_defaults=out_of_range),indent=2),encoding='utf-8')
        print(f'CSHD palette_paths={len(refs)} missing={len(missing)} out_of_range_authored_defaults={len(out_of_range)}')
    print(f'PASS mounts={len(indices)} palettes={len(result)} masks={masks} entries={sum(len(v["entries"]) for v in result.values())} queries={sum(len(v["entries"])+5 for v in result.values())} trailing_files={sum(v["trailing"]>0 for v in result.values())} members_sha256={pin} native_agreement={bool(a.core_dll)}')
    print('entry_counts='+str(dict(sorted(collections.Counter(len(v['entries']) for v in result.values()).items()))))
if __name__=='__main__':main()
