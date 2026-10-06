"""Bounded PE export negative for one wrapper; no runtime reachability claim.
Metadata-only inventory, no new function-body or corpus analysis.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,struct,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
PREREQUISITE='tools/python/verify_terrain_shader_family_selector.py'
PREREQUISITE_SHA='1f120c8f398e11f7d80d56b3433316f3f65e1cf27cc5019b28b87668b1a750e3'
TARGET=0x00B6B580
APPROVED_LEADS,APPROVED_SITES=79,315
METADATA_ROW_CAP,STRING_CAP=128,256
AMENDMENT_APPROVED=True


def sha(data):return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:raise ValueError('Executable SHA mismatch before PE/VA inspection')


def sealed_gate(data):
    if sha(data)!=PREREQUISITE_SHA:raise ValueError('Frozen69 prerequisite SHA mismatch')


def dependency():
    file=REPO/PREREQUISITE;sealed_gate(file.read_bytes())
    spec=importlib.util.spec_from_file_location('export_selector69',file)
    source=importlib.util.module_from_spec(spec);spec.loader.exec_module(source)
    wrapper,records,mapping,path=source.dependency()
    return mapping,path


def limits(maximum_leads=APPROVED_LEADS,maximum_sites=APPROVED_SITES):
    for value,approved in ((maximum_leads,APPROVED_LEADS),(maximum_sites,APPROVED_SITES)):
        if type(value) is not int or not 0<=value<=approved:raise ValueError('Invalid amended metadata ceiling')


def budget(leads,sites,maximum_leads=APPROVED_LEADS,maximum_sites=APPROVED_SITES):
    limits(maximum_leads,maximum_sites)
    if len(set(leads))!=len(leads) or len(set(sites))!=len(sites):raise ValueError('Duplicate metadata inventory site/lead')
    if len(leads)>maximum_leads or len(sites)>maximum_sites:raise ValueError('Metadata inventory exceeds amended ceiling')


def export_slot(data):
    if len(data)<64:raise ValueError('Truncated DOS header')
    header=struct.unpack_from('<I',data,60)[0]
    if header+24+104>len(data):raise ValueError('Truncated export directory entry')
    optional=header+24
    size=struct.unpack_from('<H',data,header+20)[0]
    count=struct.unpack_from('<I',data,optional+92)[0]
    if size<104 or count<1:raise ValueError('Export data directory unavailable')
    at=optional+96
    return at,*struct.unpack_from('<II',data,at)


def read_string(image,rva,maximum=STRING_CAP,end_rva=None):
    if type(maximum) is not int or not 1<=maximum<=STRING_CAP:raise ValueError('String witness bound invalid')
    if not rva:raise ValueError('Null export string RVA')
    out=bytearray()
    for i in range(maximum):
        if end_rva is not None and rva+i>=end_rva:raise ValueError('Forwarder crosses export directory')
        value=image.read(image.base+rva+i,1)[0]
        if value==0:
            if not out:raise ValueError('Empty export string')
            try:return out.decode('ascii')
            except UnicodeDecodeError:raise ValueError('NonASCII export metadata')
        out.append(value)
    raise ValueError('Unterminated or over-bound export string')


def inspect_exports(data,image,target=TARGET,maximum_leads=APPROVED_LEADS,maximum_sites=APPROVED_SITES):
    limits(maximum_leads,maximum_sites)
    at,rva,size=export_slot(data)
    if not rva and not size:
        budget([target],[('header',at)],maximum_leads,maximum_sites)
        return dict(Absent=True,Exact=[],Leads=[target],Sites=[('header',at)],Rows=[],Module=None)
    if not rva or size<40 or rva+size>0x100000000:raise ValueError('Invalid export extent')
    image.read(image.base+rva,size)
    header=struct.unpack('<IIHH7I',image.read(image.base+rva,40))
    module_rva,base,functions,names,eat,npt,ordinals=header[4:]
    if not 1<=functions<=METADATA_ROW_CAP or not 0<=names<=METADATA_ROW_CAP or base+functions-1>0xffffffff:
        raise ValueError('Export count/ordinal exceeds bounded profile')
    if not eat or (names and (not npt or not ordinals)):raise ValueError('Missing export table')
    values=struct.unpack('<'+str(functions)+'I',image.read(image.base+eat,functions*4))
    pointers=struct.unpack('<'+str(names)+'I',image.read(image.base+npt,names*4)) if names else ()
    indices=struct.unpack('<'+str(names)+'H',image.read(image.base+ordinals,names*2)) if names else ()
    sites=[('header',at),('va',image.base+rva)]
    sites.extend(('va',image.base+eat+i*4) for i in range(functions))
    sites.extend(('va',image.base+npt+i*4) for i in range(names))
    sites.extend(('va',image.base+ordinals+i*2) for i in range(names))
    module=read_string(image,module_rva);sites.append(('va',image.base+module_rva))
    labels={};seen=set()
    for name_rva,index in zip(pointers,indices):
        if index>=functions:raise ValueError('Name ordinal misses export table')
        name=read_string(image,name_rva)
        if name in seen:raise ValueError('Duplicate export name')
        seen.add(name);labels.setdefault(index,[]).append(name)
        sites.append(('va',image.base+name_rva))
    rows=[];leads={target}
    for index,value in enumerate(values):
        forward=None;location=None
        if value:
            if rva<=value<rva+size:
                forward=read_string(image,value,end_rva=rva+size)
                if '.' not in forward or any(not p for p in forward.rsplit('.',1)):raise ValueError('Malformed forwarder')
                suffix=forward.rsplit('.',1)[1]
                if suffix.startswith('#') and (not suffix[1:].isdigit() or not 1<=int(suffix[1:])<=65535):raise ValueError('Malformed forwarder ordinal')
                sites.append(('va',image.base+value))
            else:
                location=image.location(image.base+value,1);leads.add(image.base+value)
        rows.append(dict(Index=index,Ordinal=base+index,Rva=value,Va=image.base+value if value else None,
                         Names=labels.get(index,[]),Forwarder=forward,Location=location))
    leads=sorted(leads);sites=list(dict.fromkeys(sites))
    budget(leads,sites,maximum_leads,maximum_sites)
    exact=[row for row in rows if row['Va']==target and row['Forwarder'] is None]
    entries=sorted({row['Va'] for row in rows if row['Va'] is not None and row['Forwarder'] is None})
    return dict(Absent=False,Exact=exact,Leads=leads,Sites=sites,Rows=rows,Module=module,DirectoryRva=rva,DirectoryBytes=size,
                FunctionRows=functions,NameRows=names,OrdinalBase=base,
                NearestLower=max((v for v in entries if v<target),default=None),NearestHigher=min((v for v in entries if v>target),default=None),
                ExportContainsTargetProved=False)


def verify(executable,packages,output,maximum_leads=APPROVED_LEADS,maximum_sites=APPROVED_SITES):
    limits(maximum_leads,maximum_sites)
    if not AMENDMENT_APPROVED:raise ValueError('Metadata-only amendment pending')
    mapping,path=dependency();destination=path.output_path(output)
    data=executable.read_bytes();gate(data);image=mapping.RawPe(data)
    value=inspect_exports(data,image,maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    if value['FunctionRows']!=78 or value['NameRows']!=78 or value['Exact'] or len(value['Leads'])!=79 or len(value['Sites'])!=315:
        raise ValueError('Pinned complete export inventory mismatch')
    sys.path.insert(0,str(packages));import pefile
    pe=pefile.PE(data=data,fast_load=True);pe.parse_data_directories(directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_EXPORT']])
    independently=[dict(Ordinal=s.ordinal,Rva=s.address,Name=s.name.decode('ascii') if s.name else None,Forwarder=s.forwarder.decode('ascii') if s.forwarder else None) for s in pe.DIRECTORY_ENTRY_EXPORT.symbols]
    expected=[dict(Ordinal=r['Ordinal'],Rva=r['Rva'],Name=n,Forwarder=r['Forwarder']) for r in value['Rows'] for n in (r['Names'] or [None])]
    if sorted(independently,key=lambda r:(r['Ordinal'],r['Name'] or ''))!=sorted(expected,key=lambda r:(r['Ordinal'],r['Name'] or '')):
        raise ValueError('Independent pefile export metadata mismatch')
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),TargetVa='00B6B580',FunctionRows=78,NameRows=78,OrdinalBase=value['OrdinalBase'],
                 ExactExportMatches=0,ExactNamedExport=False,ExactOrdinalExport=False,ExactForwardedExport=False,ExportContainsTargetProved=False,
                 ForwarderRows=sum(r['Forwarder'] is not None for r in value['Rows']),AddressLeads=len(value['Leads']),RawMetadataSites=len(value['Sites']),
                 ApprovedMetadataLeadCap=APPROVED_LEADS,ApprovedMetadataSiteCap=APPROVED_SITES,MetadataRowCap=128,StringByteCap=256,
                 NearestLower=f"{value['NearestLower']:08X}",NearestHigher=f"{value['NearestHigher']:08X}",
                 FunctionBodiesExpanded=0,TinyHelpersExpanded=0,DefensibleIndirectCallerEstablished=False,RuntimeUnreachableProved=False,
                 ContainmentLimit='PE EAT has no function extent; neighboring export addresses are not a containing body',
                 IndirectProbeLimit='No local curated terrain-group API/vtable anchor found; arbitrary data scan declined',
                 RawDirectoryRva=value['DirectoryRva'],RawDirectoryBytes=value['DirectoryBytes'],OriginalExecution=False,OriginalCorpusRead=False,
                 PriorBodySuiteReanalysis=False,ProductionResolver=False,FrozenPrerequisiteSha256=PREREQUISITE_SHA)
    path.output_path(destination);destination.mkdir(parents=True)
    (destination/'export-metadata.json').write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','output'):parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=APPROVED_LEADS);parser.add_argument('--maximum-sites',type=int,default=APPROVED_SITES)
    args=parser.parse_args();print(json.dumps(verify(args.exe,args.packages,args.output,args.maximum_leads,args.maximum_sites),indent=2))
