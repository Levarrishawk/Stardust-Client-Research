"""Pinned static CachedNetworkId state audit; examples are models, never original execution."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
MASK=0xffffffff

def reference(counter,ids,extent,incoming,commands):
    """Small synthetic model of established raw rules, not a replacement state component."""
    assert len(commands)<=16 and len(ids)<=16 and extent<=64
    ids=list(ids);skip=min((counter-incoming+extent)&MASK,extent);cursor=0;events=[]
    for op,index,values in commands:
        assert cursor<extent and 0<=op<=4 and 0<=index<=65535 and len(values)<=16
        ignored=cursor<skip;cursor=(cursor+1+(len(values) if op==3 else 0))&MASK
        if ignored:continue
        if op==0:
            if index>=len(ids):continue
            old=ids.pop(index);counter=(counter+1)&MASK;events+=['remove','general']
        elif op==1:
            ids.extend([0]*max(0,index-len(ids)));ids.insert(index,values[0]);counter=(counter+1)&MASK;events+=['add','general']
        elif op==2:
            if index<len(ids) and ids[index]==values[0]:continue
            ids.extend([0]*max(0,index+1-len(ids)));ids[index]=values[0];counter=(counter+1)&MASK;events+=['set','general']
        elif op==3:
            ids=list(values);counter=(counter+1+len(values))&MASK;events+=['general']
        elif ids:
            events+=['remove']*len(ids)+['general'];ids=[];counter=(counter+1)&MASK
    assert cursor>=extent
    return dict(Counter=counter,Ids=ids,Skip=skip,RawUnits=cursor,ConditionalEvents=events)

def examples():
    cases=[
        ('contiguous_add',(1,[7],1,2,[(1,1,[9])]),(2,[7,9],0,1,['add','general'])),
        ('duplicate_skip',(2,[7],1,2,[(1,1,[9])]),(2,[7],1,1,[])),
        ('future_gap_skip',(1,[7],1,3,[(1,1,[9])]),(1,[7],1,1,[])),
        ('wrap_add',(MASK,[],1,0,[(1,0,[9])]),(0,[9],0,1,['add','general'])),
        ('equal_set_then_add',(10,[7],2,12,[(2,0,[7]),(1,1,[9])]),(11,[7,9],0,2,['add','general'])),
        ('invalid_remove',(10,[7],1,11,[(0,3,[])]),(10,[7],0,1,[])),
        ('add_grows_zero',(0,[7],1,1,[(1,3,[9])]),(1,[7,0,0,9],0,1,['add','general'])),
        ('set_grows_zero_even_equal_default',(0,[7],1,1,[(2,3,[0])]),(1,[7,0,0,0],0,1,['set','general'])),
        ('clear_empty',(5,[],1,6,[(4,0,[])]),(5,[],0,1,[])),
        ('clear_nonempty',(5,[7,9],1,6,[(4,0,[])]),(6,[],0,1,['remove','remove','general'])),
        ('assign_empty',(5,[],1,6,[(3,0,[])]),(6,[],0,1,['general'])),
        ('assign_equal',(5,[7,9],3,8,[(3,0,[7,9])]),(8,[7,9],0,3,['general'])),
        ('assign_overshoot',(5,[7],1,6,[(3,0,[9,9])]),(8,[9,9],0,3,['general'])),
        ('skip_crosses_assignment',(6,[7],4,9,[(3,0,[9,9]),(4,0,[])]),(7,[],1,4,['remove','general'])),
        ('partial_overlap',(2,[7],2,3,[(1,1,[8]),(1,1,[9])]),(3,[7,9],1,2,['add','general'])),
        ('zero_extent',(7,[7],0,999,[]),(7,[7],0,0,[]))]
    rows=[]
    for name,args,expected in cases:
        actual=reference(*args);assert tuple(actual.values())==expected,(name,actual,expected)
        rows.append(dict(Name=name,**actual))
    return rows

def audit(path,packages):
    raw=path.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
    if digest!=EXPECTED:raise ValueError('Executable SHA-256 mismatch; no VA analysis performed')
    sys.path.insert(0,str(packages))
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
    read=lambda va,n:pe.get_data(va-base,n)
    vtable=struct.unpack('<13I',read(0x015E0614,52))
    assert (vtable[0],vtable[2],vtable[6],vtable[7],vtable[8])==(0x00643740,0x006421A0,0x006423E0,0x00642A20,0x00641850)
    def rtti(va):
        col=struct.unpack('<I',read(va-4,4))[0];td=struct.unpack('<I',read(col+12,4))[0]
        return read(td+8,200).split(b'\0')[0].decode()
    assert rtti(0x015E0614)=='.?AV?$AutoDeltaVector@VCachedNetworkId@@UDefaultObjectType@Archive@@@Archive@@'
    assert rtti(0x015DC648)=='.?AV?$Watcher@VObject@@@@'
    anchors=[
        (0x00642445,'mov','eax, dword ptr [esi + 0x18]'),(0x00642448,'sub','eax, ecx'),(0x0064244D,'add','eax, ecx'),(0x0064244F,'cmp','eax, ecx'),(0x00642454,'jbe','0x64245b'),(0x00642456,'mov','dword ptr [ebp - 0x18], ecx'),
        (0x006424B0,'inc','dword ptr [ebp - 0x1c]'),(0x00642544,'inc','eax'),(0x00642553,'cmp','dword ptr [ebp - 0x1c], ecx'),(0x00642556,'jae','0x64264f'),
        (0x00642599,'call','dword ptr [edx + 0x1c]'),(0x0064259C,'cmp','dword ptr [ebp - 0x18], eax'),(0x0064259F,'jae','0x64263d'),(0x006425CF,'call','0x643f60'),(0x006425DF,'inc','edx'),(0x006425E7,'mov','dword ptr [esi + 0x18], edx'),(0x006425F7,'call','0x643c80'),(0x006425FE,'call','0x43e860'),(0x00642610,'call','0x644e90'),(0x00642617,'call','0x644e70'),
        (0x006426F6,'call','0x643f60'),(0x006426FB,'inc','dword ptr [esi + 0x18]'),(0x0064270C,'cmp','eax, edi'),(0x0064270E,'jae','0x642716'),(0x00642711,'call','0x643cd0'),(0x00642728,'call','0x643a90'),(0x0064272F,'call','0x43e860'),(0x0064273B,'call','0x644eb0'),(0x00642742,'call','0x644e70'),
        (0x006427AE,'cmp','edi, edx'),(0x006427B3,'jae','0x6427d8'),(0x006427CB,'call','0xb2fff0'),(0x006427D2,'jne','0x64263a'),(0x00642813,'inc','dword ptr [esi + 0x18]'),(0x00642826,'jb','0x642831'),(0x00642828,'lea','edx, [edi + 1]'),(0x0064282C,'call','0x643cd0'),(0x0064283D,'call','0xb2fe70'),(0x00642850,'call','0xb2fee0'),(0x00642857,'call','0x43e860'),(0x0064286A,'call','0x644ed0'),(0x00642871,'call','0x644e70'),
        (0x0064297A,'inc','ecx'),(0x0064297F,'call','0x43ceb0'),(0x006429AB,'call','0x641d20'),(0x006429BA,'call','0xb300e0'),(0x006429D4,'call','0x5e8bc0'),(0x006429EA,'call','0x5e5950'),(0x006429F6,'call','0x641850'),
        (0x00641871,'cmp','eax, dword ptr [esi + 0x10]'),(0x00641874,'je','0x641984'),(0x006418D6,'inc','dword ptr [esi + 0x18]'),(0x006418FF,'call','eax'),(0x00641942,'call','0xb300e0'),(0x00641951,'mov','dword ptr [esi + 0x10], ebx'),(0x0064195E,'call','0x118a7c0'),(0x00641973,'call','esi'),
        (0x00641DB0,'inc','dword ptr [esi + 0x18]'),(0x00641DB8,'je','0x641ed1'),(0x00641E2D,'call','0x644ad0'),(0x00641E76,'call','0x5e8bc0'),(0x00641F53,'inc','dword ptr [eax + 0x18]'),(0x00641F72,'jb','0x641ef0'),(0x00641F83,'call','0x118a7c0'),(0x00641F9B,'call','eax'),
        (0x00643D06,'call','0xb2fcc0'),(0x00643D92,'sub','ecx, eax'),(0x00643DB8,'call','0xb2fe70'),(0x00643E88,'call','0x645800'),(0x00643F13,'call','0x5e5800'),(0x00645815,'call','0x5e81b0'),(0x005E81DE,'call','0xb2fe70'),
        (0x00B2FC70,'push','0'),(0x00B2FC72,'push','0'),(0x00B2FC74,'mov','ecx, 0x1946ee8'),(0x00B2FC79,'call','0xaa4900'),(0x00AA490B,'mov','dword ptr [eax], ecx'),(0x00AA490D,'mov','dword ptr [eax + 4], edx'),(0x00B2FCD9,'push','0x1946ee8'),(0x00B2FCE0,'call','0xaa4920'),(0x00B2FCE5,'mov','dword ptr [esi + 0xc], 0'),
        (0x00B2FFF8,'cmp','edx, dword ptr [eax]'),(0x00B2FFFF,'cmp','ecx, dword ptr [eax + 4]'),(0x00B2FE94,'call','0xaa4920'),(0x00B2FEC1,'call','0xaa36d0'),(0x00B2FF11,'call','0xaa37c0'),(0x00B2FF24,'call','0xaa36d0'),(0x00B30127,'call','0xaa37c0'),(0x00B30134,'mov','dword ptr [esi + 4], 0'),
        (0x00643746,'call','0x6414c0'),(0x006414F4,'call','0xac1640'),(0x0064150F,'call','0xac1640'),(0x00641533,'call','0xb300e0'),(0x0064156E,'call','0x402480'),(0x00641592,'call','0xb300e0'),(0x006415BC,'call','0x402480'),
        (0x00642207,'call','0x5e8bc0'),(0x00642221,'call','dword ptr [eax + 0xc]'),(0x00642261,'add','eax, 0x18'),(0x00642265,'call','0x118b030'),(0x00642333,'call','eax'),
        (0x0063EEE7,'mov','dword ptr [edi + 0x28], ebx'),(0x0063EEEA,'mov','dword ptr [edi + 0x2c], ebx'),(0x0063EEED,'mov','dword ptr [edi + 0x30], ebx'),(0x0063EEF0,'mov','dword ptr [edi + 0x34], ebx'),(0x00644E7D,'jmp','dword ptr [eax + 4]'),(0x00644EA8,'call','dword ptr [eax + 4]'),(0x00644EC8,'call','dword ptr [eax + 4]'),(0x00644EEC,'call','dword ptr [eax + 4]')]
    for va,mnemonic,operand in anchors:
        i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(mnemonic,operand),(hex(va),i.mnemonic,i.op_str)
    default_section=next(s for s in pe.sections if s.VirtualAddress<=0x01946EE8-base<s.VirtualAddress+s.Misc_VirtualSize)
    assert 0x01946EE8-base-default_section.VirtualAddress>=default_section.SizeOfRawData
    modeled=examples()
    return dict(ExeSha256=digest,InstructionAnchors=len(anchors),RttiClasses=2,DefaultIdBits='0000000000000000',
                CounterRule='min(U32(local-incoming+rawExtent),rawExtent)',StateImplementationAdded=False,OriginalExecuted=False,
                SyntheticReferenceExamples=modeled)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('exe',type=Path);p.add_argument('--packages',type=Path,required=True);a=p.parse_args()
    print(json.dumps(audit(a.exe,a.packages),indent=2))
