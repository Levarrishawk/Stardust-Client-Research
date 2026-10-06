"""Independent synthetic PE export fixtures and bounded metadata guards."""
from pathlib import Path
import argparse,importlib.util,json,struct,sys
from unittest.mock import patch
REPO=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('exports70',REPO/'tools/python/verify_terrain_shader_lookup_export_boundary.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)


def reject(action):
    try:action()
    except ValueError:return 1
    raise AssertionError('Expected bounded guard rejection')


def fixture():
    b=bytearray(0x800);b[:2]=b'MZ';struct.pack_into('<I',b,60,0x80);b[0x80:0x84]=b'PE\0\0'
    struct.pack_into('<HH',b,0x84,0x14c,2);struct.pack_into('<H',b,0x94,224)
    optional=0x98;struct.pack_into('<H',b,optional,0x10b);struct.pack_into('<I',b,optional+28,0x400000)
    struct.pack_into('<I',b,optional+56,0x3000);struct.pack_into('<I',b,optional+92,16)
    struct.pack_into('<II',b,optional+96,0x2000,0x300)
    for index,(name,rva,raw,size,flags) in enumerate([(b'.text',0x1000,0x200,0x200,0x60000020),(b'.rdata',0x2000,0x400,0x400,0x40000040)]):
        at=optional+224+40*index;struct.pack_into('<8s4I',b,at,name,size,rva,size,raw);struct.pack_into('<I',b,at+36,flags)
    struct.pack_into('<IIHH7I',b,0x400,0,0,0,0,0x2110,5,2,2,0x2040,0x2050,0x2060)
    struct.pack_into('<2I',b,0x440,0x1010,0x2080);struct.pack_into('<2I',b,0x450,0x2120,0x2130);struct.pack_into('<2H',b,0x460,0,1)
    for at,value in ((0x480,b'synthetic.#7\0'),(0x510,b'synthetic.dll\0'),(0x520,b'Direct\0'),(0x530,b'Forward\0')):b[at:at+len(value)]=value
    return b


def run(exe,packages,run_root):
    mapping,path=m.dependency();root=path.output_path(run_root);data=exe.read_bytes();m.gate(data);image=mapping.RawPe(data)
    guards={};guards['Identity']=sum(reject(lambda:m.gate(value)) for value in (b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]))
    source=(REPO/m.PREREQUISITE).read_bytes();guards['Source']=sum(reject(lambda:m.sealed_gate(value)) for value in (b'',source[:-1],source+b'\0'))
    guards['PeTruncation']=sum(reject(lambda:mapping.RawPe(value)) for value in (b'',b'MZ',data[:63],data[:128],data[:-1]))
    header=struct.unpack_from('<I',data,60)[0];guards['Headers']=0
    for at,length in ((60,4),(header,4),(header+4,2),(header+6,2),(header+24,2),(header+52,4)):
        changed=bytearray(data);changed[at:at+length]=bytes(length);guards['Headers']+=reject(lambda:mapping.RawPe(changed))
    authored=fixture();model=mapping.RawPe(authored);checks=0
    exact=m.inspect_exports(authored,model,0x401010)
    assert len(exact['Exact'])==1 and exact['Exact'][0]['Ordinal']==5 and exact['Exact'][0]['Names']==['Direct'];checks+=3
    assert exact['Rows'][1]['Forwarder']=='synthetic.#7' and exact['Rows'][1]['Ordinal']==6;checks+=2
    absent=m.inspect_exports(authored,model,0x401020);assert not absent['Exact'] and absent['NearestLower']==0x401010 and absent['NearestHigher'] is None;checks+=3
    no_export=bytearray(authored);struct.pack_into('<II',no_export,0xf8,0,0)
    assert m.inspect_exports(no_export,mapping.RawPe(no_export),0x401010)['Absent'];checks+=1
    alias=bytearray(authored);struct.pack_into('<H',alias,0x462,0)
    got=m.inspect_exports(alias,mapping.RawPe(alias),0x401010);assert got['Exact'][0]['Names']==['Direct','Forward'];checks+=1
    guards['ExportStructures']=0
    for at,value in ((0xf8,0xffffffff),(0xfc,39),(0x40c,0),(0x410,0xffffffff),(0x414,0),(0x414,129),(0x418,129),(0x41c,0),(0x420,0),(0x424,0),(0x450,0),(0x440,0x70000000)):
        changed=bytearray(authored);struct.pack_into('<I',changed,at,value)
        guards['ExportStructures']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    changed=bytearray(authored);struct.pack_into('<H',changed,0x460,2)
    guards['ExportStructures']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    guards['StringsAndForwarders']=0
    for value in (b'\0',b'nodot\0',b'.symbol\0',b'module.\0',b'module.#x\0',b'module.#0\0',b'module.#65536\0',b'\xff.x\0'):
        changed=bytearray(authored);changed[0x480:0x480+len(value)]=value
        guards['StringsAndForwarders']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    changed=bytearray(authored);changed[0x480:0x580]=b'x'*256
    guards['StringsAndForwarders']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    changed=bytearray(authored);struct.pack_into('<I',changed,0x444,0x22ff);changed[0x6ff]=ord('x')
    guards['StringsAndForwarders']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    changed=bytearray(authored);struct.pack_into('<I',changed,0x454,0x2120)
    guards['StringsAndForwarders']+=reject(lambda:m.inspect_exports(changed,mapping.RawPe(changed),0x401010))
    guards['MappedAndStringBounds']=sum(reject(lambda:m.read_string(image,0,value)) for value in (0,-1,257,True,1.5))
    guards['MappedAndStringBounds']+=reject(lambda:image.read(0x019AA110,4))
    guards['Budget']=0
    for key,values in (('maximum_leads',(0,12,-1,80,True,1.5)),('maximum_sites',(0,12,-1,316,True,1.5))):
        for cap in values:
            action=lambda:m.inspect_exports(data,image,**{key:cap})
            guards['Budget']+=reject(action)
    guards['Budget']+=reject(lambda:m.budget([1,1],[('va',2)]));guards['Budget']+=reject(lambda:m.budget([1],[('va',2),('va',2)]))
    root.mkdir(parents=True);sentinel=root/'sentinel.bin';sentinel.write_bytes(b'synthetic path fixture')
    guards['Paths']=0
    for value in (REPO,REPO/'docs/forbidden70',REPO/'local-private',root,REPO/'local-private/../docs/escape70',REPO/'local-private/.git/no70',sentinel/'child'):
        guards['Paths']+=reject(lambda:path.output_path(value))
    actual=Path.lstat
    class Linked:
        st_mode=0o040755
        st_file_attributes=0x400
    def linked(value,*a,**kw):return Linked() if value==root else actual(value,*a,**kw)
    with patch.object(Path,'lstat',linked):guards['Paths']+=reject(lambda:path.output_path(root/'linked-reject'))
    guards['NoPublication']=0;wrong=root/'wrong.bin';wrong.write_bytes(b'MZ synthetic mismatch');unpublished=root/'must-not-publish'
    with patch.object(mapping,'RawPe',side_effect=AssertionError('PE reached before SHA')):
        guards['NoPublication']+=reject(lambda:m.verify(wrong,packages,unpublished))
    assert not unpublished.exists()
    guards['NoPublication']+=reject(lambda:m.verify(exe,packages,unpublished,12,12));assert not unpublished.exists()
    result=m.verify(exe,packages,root/'positive');sealed={p.name:m.sha(p.read_bytes()) for p in (root/'positive').iterdir()}
    guards['NoPublication']+=reject(lambda:m.verify(exe,packages,root/'positive'))
    assert sealed=={p.name:m.sha(p.read_bytes()) for p in (root/'positive').iterdir()}
    receipt=dict(Passed=True,Metadata=result,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),IndependentSyntheticMetadataChecks=checks,OriginalExecution=False,OriginalCorpusRead=False)
    (root/'comparison-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','run-root'):p.add_argument('--'+key,type=Path,required=True)
    a=p.parse_args();run(a.exe,a.packages,a.run_root)
