"""Independent effective CSHD0001 field inventory and pinned native agreement; no original asset output."""
import argparse,contextlib,hashlib,json,struct,subprocess,collections
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

def parse(data):
    root,=nodes(data);assert root[0]==b'CSHD';version,=root[1];assert version[0]==b'0001'
    inline,*parts=version[1];assert inline[0]==b'SSHT'
    choices=[];selections=[];factors=[]
    def strings(payload,position,limit=1024):
        end=payload.index(0,position);value=payload[position:end];assert 0<len(value)<=limit and all(32<=c<=126 for c in value)
        return value.decode('ascii'),end+1
    for tag,children in parts:
        if tag==b'TXTR':
            (dt,payload),(ct,records)=children;assert dt==b'DATA' and ct==b'CUST';count,=struct.unpack_from('<h',payload);position=2
            for _ in range(count):name,position=strings(payload,position);choices.append(name.lower().replace('\\','/'))
            assert position==len(payload)
            for rt,record in records:
                assert rt==b'TX1D';target,first,count=struct.unpack_from('<Ihh',record);variable,position=strings(record,8,259)
                flag,default=struct.unpack_from('<Bh',record,position);assert position+3==len(record)
                assert first>=0 and count>0 and first+count<=len(choices)
                selections.append((target,first,count,variable,flag,default))
        elif tag==b'TFAC':
            for rt,record in children:
                assert rt==b'PAL ';variable,position=strings(record,0,511);flag,target=struct.unpack_from('<BI',record,position);position+=5
                palette,position=strings(record,position,511);default,=struct.unpack_from('<i',record,position);assert position+4==len(record)
                factors.append((variable,flag,target,palette.lower().replace('\\','/'),default))
        else:raise ValueError('Unsupported customization form '+repr(tag))
    canonical=bytearray()
    def word(value):canonical.extend(struct.pack('<i',value))
    def text(value):b=value.encode('utf-8');word(len(b));canonical.extend(b)
    word(len(choices))
    for choice in choices:text(choice)
    word(len(selections))
    for target,first,count,variable,flag,default in selections:
        canonical.extend(struct.pack('<Ihh',target,first,count));text(variable);canonical.extend(struct.pack('<Bh',flag,default))
    word(len(factors))
    for variable,flag,target,palette,default in factors:
        text(variable);canonical.extend(struct.pack('<BI',flag,target));text(palette);word(default)
    # Inline SSHT is first at byte24; its payload excludes the FORM type at byte32.
    assert data[24:28]==b'FORM' and data[32:36]==b'SSHT'
    inline_size=struct.unpack_from('>I',data,28)[0]
    return dict(version='0001',choices=choices,selections=selections,factors=factors,
        records_sha256=hashlib.sha256(canonical).hexdigest().upper(),inline_sha256=hashlib.sha256(data[36:32+inline_size]).hexdigest().upper())

def selection_hash(shader,forced=None):
    canonical=bytearray(struct.pack('<i',len(shader['selections'])))
    for ordinal,(tag,first,count,variable,flag,default) in enumerate(shader['selections']):
        requested=default if forced is None else forced;clamped=min(max(requested,0),count-1);path=shader['choices'][first+clamped].encode('utf-8')
        canonical.extend(struct.pack('<iIiii',ordinal,tag,requested,clamped,len(path)));canonical.extend(path);canonical.extend(struct.pack('<?',forced is None))
    return hashlib.sha256(canonical).hexdigest().upper()

def factor_hash(shader,palettes,forced=None):
    canonical=bytearray(struct.pack('<i',len(shader['factors'])))
    for ordinal,(variable,flag,tag,path,default) in enumerate(shader['factors']):
        requested=default if forced is None else forced;missing=path not in palettes
        entries=[[0,255,255,0]] if missing else palettes[path]['entries']
        canonical.extend(struct.pack('<iIi?',ordinal,tag,requested,bool(entries)))
        if entries:
            index=requested if 0<=requested<len(entries) else 0;r,g,b,raw=entries[index]
            canonical.extend(struct.pack('<iIB',index,0xFF000000|(r<<16)|(g<<8)|b,raw))
        canonical.extend(struct.pack('<??',forced is None,missing))
    return hashlib.sha256(canonical).hexdigest().upper()

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('output',type=Path);p.add_argument('--core-dll',type=Path);p.add_argument('--palette-inventory',type=Path);a=p.parse_args()
    palettes=None
    if a.palette_inventory:
        inventory=json.loads(a.palette_inventory.read_text(encoding='utf-8'))
        assert inventory['configuration_sha256']==hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),'Palette configuration differs'
        palettes=inventory['members']
    private=(Path(__file__).resolve().parents[2]/'local-private').resolve();assert a.output.resolve().is_relative_to(private),'Derived inventory must remain private'
    a.output.mkdir(parents=True,exist_ok=True);seen=set();result={};shapes=collections.Counter()
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for priority,index in indices:
            for path,record in index.entries.items():
                if not path.endswith('.sht') or path in seen:continue
                seen.add(path)
                if not record[1]:continue
                data=index.lookup(path)
                if data[8:12]!=b'CSHD':continue
                fields=parse(data);result[path]=dict(priority=priority,sha256=hashlib.sha256(data).hexdigest().upper(),**fields)
                shapes[(bool(fields['choices']),bool(fields['factors']))]+=1
    names=sorted(result);member_bytes=('\n'.join(names)+'\n').encode('ascii');members=a.output/'members.txt';members.write_bytes(member_bytes);pin=hashlib.sha256(member_bytes).hexdigest().upper()
    (a.output/'inventory.json').write_text(json.dumps(dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),mounts=len(indices),members_sha256=pin,members=result),indent=2),encoding='utf-8')
    if a.core_dll:
        output=subprocess.run(['dotnet',str(a.core_dll),'--cshd-palette-list' if palettes is not None else '--cshd-list',str(a.root),'--configuration',str(a.configuration),'--members',str(members),'--expected-members-sha256',pin],capture_output=True,text=True,check=True,timeout=120).stdout
        for path,v in result.items():
            expected=f'cshd_member={path} priority={v["priority"]} sha256={v["sha256"]} version={v["version"]} choices={len(v["choices"])} selections={len(v["selections"])} factors={len(v["factors"])} records_sha256={v["records_sha256"]} inline_sha256={v["inline_sha256"]}'
            assert expected in output,('Native customization disagreement',path)
            expected=f'cshd_selection={path} defaults_sha256={selection_hash(v)} minimum_sha256={selection_hash(v,-2147483648)} maximum_sha256={selection_hash(v,2147483647)}'
            assert expected in output,('Native customization selection disagreement',path)
            if palettes is not None:
                paths={factor[3] for factor in v['factors']};missing=paths-palettes.keys()
                expected=f'cshd_palette={path} sources={1+len(paths)-len(missing)} palettes={len(paths)} missing={len(missing)} factors={len(v["factors"])} defaults_sha256={factor_hash(v,palettes)} minimum_sha256={factor_hash(v,palettes,-2147483648)} maximum_sha256={factor_hash(v,palettes,2147483647)}'
                assert expected in output,('Native customization palette disagreement',path)
        assert f'PASS cshd_inventory members={len(result)}' in output
        (a.output/('palette-factor-agreement.txt' if palettes is not None else 'native-agreement.txt')).write_text(output,encoding='utf-8')
    print(f'PASS mounts={len(indices)} shaders={len(result)} choices={sum(len(v["choices"]) for v in result.values())} selections={sum(len(v["selections"]) for v in result.values())} factors={sum(len(v["factors"]) for v in result.values())} members_sha256={pin} native_agreement={bool(a.core_dll)}')
    print('shapes='+str(dict(shapes)))
    if palettes is not None:
        factors=[f for v in result.values() for f in v['factors']]
        print(f'PASS palette_factor_cases={3*len(factors)} missing_factor_uses={sum(f[3] not in palettes for f in factors)} invalid_authored_defaults={sum(f[3] in palettes and not 0<=f[4]<len(palettes[f[3]]["entries"]) for f in factors)}')
if __name__=='__main__':main()
