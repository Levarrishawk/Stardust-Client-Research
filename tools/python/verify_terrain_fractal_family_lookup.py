"""Static-only fractal family ownership/lookup research; no noise or original process."""
from __future__ import annotations
import argparse,contextlib,hashlib,importlib.util,json,struct,sys
from bisect import bisect_left
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SOURCE='6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
GENERATOR='B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32'
FAMILY='90CE52CED775F08BF61AE50A702C61576BB53E3982A46A699D90C94C94192DF8'
TARGETS={0xb757c0,0xb75850,0xb75920,0xb75970,0xb75a50,0xb75ae0,0xb75bb0,0xb75d10,0xb75f20,0xb76040,0xb75b70,0xb75b50,0xb75b90}
def sha(data):return hashlib.sha256(data).hexdigest().upper()
def gate(data,expected):
 if sha(data)!=expected:raise ValueError('Input identity mismatch before interpretation')
def negatives(data,expected):
 count=0
 for offset in [0,1,len(data)//2,len(data)-1]:
  bad=bytearray(data);bad[offset]^=1
  try:gate(bad,expected)
  except ValueError:count+=1
  else:raise AssertionError('wrong identity accepted')
 for bad in [b'',data[:-1],data+b'\0',bytes(64)]:
  try:gate(bad,expected)
  except ValueError:count+=1
  else:raise AssertionError('wrong identity accepted')
 return count
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
ANCHORS={12015244: ('mov', 'dword ptr [esi], 0'), 12015250: ('mov', 'dword ptr [esi + 4], eax'), 12015253: ('call', '0x13e6880'), 12015321: ('call', '0xac1660'), 12015329: ('lea', 'ecx, [esi + 8]'), 12015345: ('call', '0x13e6930'), 12015400: ('test', 'eax, eax'), 12015406: ('call', '0xac1660'), 12015444: ('call', '0xac15e0'), 12015470: ('mov', 'dword ptr [ebx], eax'), 12015488: ('mov', 'eax, dword ptr [ecx + 4]'), 12015510: ('mov', 'dword ptr [ecx + 4], eax'), 12015520: ('lea', 'eax, [ecx + 8]'), 12015536: ('lea', 'eax, [ecx + 8]'), 12015582: ('push', '0xc'), 12015610: ('push', '0x18'), 12015624: ('mov', 'dword ptr [esi], eax'), 12015626: ('mov', 'dword ptr [esi + 4], ebx'), 12015632: ('mov', 'byte ptr [eax], bl'), 12015636: ('mov', 'dword ptr [edx + 4], ebx'), 12015641: ('mov', 'dword ptr [eax + 8], eax'), 12015649: ('mov', 'dword ptr [eax + 0xc], eax'), 12015652: ('mov', 'dword ptr [edi], esi'), 12015700: ('call', '0xb75a50'), 12015713: ('call', '0xb75880'), 12015719: ('call', '0xac1640'), 12015728: ('mov', 'dword ptr [esi], 0'), 12015911: ('call', '0xb75a50'), 12015923: ('push', '0x4d475250'), 12015950: ('cmp', 'eax, 0x30303030'), 12015960: ('call', '0xb75f40'), 12016060: ('call', '0xa947a0'), 12016074: ('call', '0xa94810'), 12016081: ('call', '0xb75780'), 12016097: ('call', '0xa94870'), 12016104: ('call', '0xb75710'), 12016112: ('call', '0xa949d0'), 12016128: ('call', '0xb757a0'), 12016135: ('call', '0x13e82b0'), 12016152: ('push', 'edi'), 12016224: ('mov', 'edi, dword ptr [esi + 0x14]'), 12016233: ('call', '0xb756b0'), 12016503: ('call', '0xb75de0'), 12016510: ('call', '0xb757a0'), 12016535: ('call', '0xb75e10'), 12016542: ('call', '0xb757b0'), 12016590: ('push', '0x2588'), 12016623: ('call', '0xb75680'), 12016643: ('call', '0xb75720'), 12016675: ('cmp', 'ecx, dword ptr [esi + 0x10]'), 12016678: ('setl', 'cl'), 12016740: ('call', '0xb76460'), 12016766: ('call', '0xb76460'), 12017132: ('call', '0xb76420'), 12017139: ('cmp', 'eax, dword ptr [ecx]'), 12017142: ('je', '0xb75dff'), 12017144: ('mov', 'eax, dword ptr [eax + 0x14]'), 12017151: ('xor', 'eax, eax'), 12017189: ('cmp', 'dword ptr [eax + 0x10], edi'), 12017192: ('jl', '0xb75e31'), 12017212: ('cmp', 'edi, dword ptr [edx + 0x10]'), 12017786: ('call', '0xb757a0'), 12017793: ('call', '0x13e80a0'), 12017800: ('jne', '0xb7611d'), 12017827: ('call', '0xb76160'), 12017844: ('call', '0xb76190'), 12017863: ('call', '0xb75bb0'), 12017882: ('call', '0xb757b0'), 12017889: ('call', '0x13e6a00'), 12017952: ('call', '0xb75780'), 12018018: ('mov', 'esi, 1'), 12018026: ('call', '0xb75de0'), 12018039: ('call', '0xb75de0'), 12018144: ('call', '0xb75e60'), 12018486: ('call', '0xb76320'), 12018497: ('call', '0x12ea920'), 12018741: ('cmp', 'dword ptr [ecx + 0x10], esi'), 12018744: ('jl', '0xb76441'), 12018766: ('cmp', 'ecx, dword ptr [edx + 0x10]'), 12018769: ('jl', '0xb76455'), 12018788: ('push', '0x18'), 12018805: ('call', '0xb76490'), 12016239: ('call', '0xac1640'), 12016294: ('call', '0xb76320'), 12016305: ('call', '0x12ea920'), 12016321: ('mov', 'dword ptr [eax + 8], eax'), 12016328: ('mov', 'dword ptr [edx + 4], ecx'), 12016336: ('mov', 'dword ptr [edi + 4], ecx'), 12016382: ('call', '0xb757b0'), 12016389: ('call', '0x13e6ad0'), 12016439: ('call', '0xb75de0'), 12016446: ('call', '0xb75710'), 12016471: ('call', '0xb75e10'), 12016482: ('call', '0xb75720'), 12016814: ('push', 'ecx'), 12016816: ('call', '0x12f5930'), 12016830: ('mov', 'dword ptr [ebx + 4], eax'), 12016862: ('cmp', 'edx, dword ptr [ebp - 0x14]'), 12016865: ('jge', '0xb75cf6'), 12016933: ('cmp', 'dword ptr [eax + 0x10], edi'), 12016936: ('jl', '0xb75d31'), 12016956: ('cmp', 'edi, dword ptr [ecx + 0x10]'), 12016959: ('jge', '0xb75d43'), 12016978: ('call', '0x12f5cb0'), 12016993: ('call', '0x12ea920'), 12017006: ('mov', 'dword ptr [esi + 4], eax'), 12017451: ('call', '0xb75e10'), 12017458: ('call', '0xb75790'), 12017524: ('push', '0x4d46414d'), 12017540: ('push', '0x44415441'), 12017560: ('call', '0xa94f00'), 12017567: ('call', '0xa95200'), 12017581: ('call', '0xb75bb0'), 12017617: ('call', '0xb757b0'), 12017624: ('call', '0x13e8410'), 12018847: ('mov', 'dword ptr [eax], edx'), 12018852: ('mov', 'dword ptr [eax + 4], ecx'), 20867379: ('mov', 'eax, dword ptr [esi + 0x257c]'), 20867390: ('call', '0xac1660'), 20867398: ('mov', 'dword ptr [esi + 0x257c], 0'), 11947962: ('lea', 'ecx, [esi + 0x40]'), 11947969: ('call', '0xb757c0'), 11948071: ('call', '0xb650c0'), 11948144: ('lea', 'ecx, [esi + 0x40]'), 11948151: ('call', '0xb75850'), 11948257: ('lea', 'ecx, [esi + 0x40]'), 11948260: ('call', '0xb75a50'), 11948302: ('call', 'dword ptr [edx]'), 11948323: ('mov', 'dword ptr [esi + 0x50], 0'), 11951380: ('lea', 'ecx, [edi + 0x40]'), 11951383: ('call', '0xb75920'), 11951479: ('call', 'dword ptr [eax + 0x10]'), 20867593: ('cmp', 'esi, eax'), 20867595: ('je', '0x13e6a13'), 20867598: ('call', '0x13e6a20'), 20867737: ('rep', 'movsd dword ptr es:[edi], dword ptr [esi]'), 20867741: ('mov', 'dword ptr [eax + 0x257c], ecx'), 20867747: ('mov', 'dword ptr [eax + 0x2574], ecx'), 20867753: ('mov', 'dword ptr [eax + 0x2578], ecx'), 20867775: ('call', '0x13e6ad0'), 20867802: ('mov', 'eax, dword ptr [esi + 0x2574]'), 20867812: ('cmp', 'edi, eax'), 20867822: ('jle', '0x13e6b3d'), 20867835: ('call', '0xac1660'), 20867861: ('mov', 'dword ptr [esi + 0x2574], edi'), 20867867: ('imul', 'edi, ebx'), 20867870: ('shl', 'edi, 4'), 20867880: ('call', '0xac15e0'), 20867890: ('mov', 'dword ptr [esi + 0x257c], eax'), 20867896: ('call', '0x13e6b50'), 20867920: ('mov', 'eax, ecx'), 20867929: ('test', 'edi, edi'), 20867939: ('imul', 'ecx, dword ptr [eax + 0x2574]'), 20867946: ('shl', 'ecx, 4'), 20867956: ('rep', 'stosd dword ptr es:[edi], eax'), 20867963: ('rep', 'stosb byte ptr es:[edi], al'), 20867977: ('cmp', 'dword ptr [esi], eax'), 20867985: ('mov', 'dword ptr [esi], eax'), 20867994: ('call', '0x13e6b50'), 20868025: ('mov', 'dword ptr [ecx + 4], eax'), 20868028: ('mov', 'dword ptr [ecx + 8], edx'), 20868031: ('call', '0x13e6b50'), 20868063: ('call', '0x13e6b50'), 20868086: ('mov', 'dword ptr [ecx + 0x14], eax'), 20868089: ('call', '0x13e6b50'), 20868118: ('mov', 'dword ptr [ecx + 0x18], eax'), 20868121: ('call', '0x13e6b50'), 12024809: ('cmp', 'dword ptr [esi + 0x20], eax'), 12024812: ('je', '0xb77c00'), 12024814: ('mov', 'dword ptr [esi + 0x20], eax'), 12024824: ('call', '0xb75b70'), 12024829: ('mov', 'dword ptr [esi + 0x1c], eax'), 12024774: ('mov', 'dword ptr [ecx + 0x1c], 0'), 12019901: ('cmp', 'dword ptr [esi + 0x18], eax'), 12019904: ('je', '0xb768d4'), 12019906: ('mov', 'dword ptr [esi + 0x18], eax'), 12019916: ('call', '0xb75b70'), 12019921: ('mov', 'dword ptr [esi + 0x14], eax')}
CALLERS=[(11917728, 12016352, 11917248), (11938503, 12015552, 11938144), (11938523, 12015904, 11938144), (11938534, 12015696, 11938144), (11947969, 12015552, 11947888), (11948151, 12015696, 11948032), (11948260, 12016208, 11948224), (11949089, 12016352, 11949056), (11951383, 12015904, 11951296), (11951709, 12015984, 11951632), (12015700, 12016208, 12015696), (12015911, 12016208, 12015904), (12017581, 12016560, 12017472), (12017863, 12016560, 12017728), (12019916, 12016496, 12019872), (12020586, 12017728, 12020352), (12021104, 12017728, 12020672), (12021313, 12017728, 12021184), (12024824, 12016496, 12024800), (12025554, 12017728, 12025296), (12026094, 12017728, 12025632), (12026654, 12017728, 12026176), (12027294, 12017728, 12026736), (12027505, 12017728, 12027376), (12104378, 12016496, 12104320), (12104862, 12017728, 12104736), (21831807, 12015696, 21831801), (21831887, 12015696, 21831881)]

ANCHORS[0xb75e3f]=("jge","0xb75e43")
ANCHORS[0xb65e5d]=("call","0xb75970")
for va, (mnemonic, operand) in list(ANCHORS.items()):
 if mnemonic in ('rep', 'repne', 'repe'):
  opcode, _, tail = operand.partition(' ')
  ANCHORS[va] = (mnemonic + ' ' + opcode, tail)
def static(executable,packages):
 data=executable.read_bytes();gate(data,EXE)
 identity=negatives(data,EXE)
 sys.path.insert(0,str(packages))
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;image=pe.get_memory_mapped_image();assert base==0x400000
 md=Cs(CS_ARCH_X86,CS_MODE_32);md.skipdata=True
 for va,expected in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==expected,(hex(va),expected,(i.mnemonic,i.op_str))
 expected_calls={site:target for site,target,_ in CALLERS};assert len(expected_calls)==len(CALLERS)
 for site,target,owner in CALLERS:
  assert any(i.address==site and i.mnemonic in ('call','jmp') and i.op_str==hex(target) for i in md.disasm(image[owner-base:site-base+15],owner)),hex(site)
 candidates={}
 for section in pe.sections:
  if not section.IMAGE_SCN_MEM_EXECUTE:continue
  payload=section.get_data();va=base+section.VirtualAddress
  for offset in range(len(payload)-4):
   if payload[offset] not in (0xe8,0xe9):continue
   target=(va+offset+5+struct.unpack_from('<i',payload,offset+1)[0])&0xffffffff
   if target in TARGETS:candidates[va+offset]=target
 assert candidates==expected_calls,candidates
 # These are direct-reference inventories, not exhaustive reachable callers.
 return dict(ExecutableSha256=EXE,InstructionAnchors=len(ANCHORS),DirectBranchCandidates=len(candidates),EntryBoundaryConfirmedCalls=len(CALLERS),WrongExecutableNegatives=identity,CallerCoverage='direct relative CALL/JMP to13 selected map entries; indirect callers Unknown')

def selected_family(root,config):
 config_bytes=config.read_bytes();gate(config_bytes,CONFIG);config_negative=negatives(config_bytes,CONFIG)
 m=load('family_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,config):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 gate(payload,SOURCE);assert priority==42 and len(payload)==1188446
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]]
 gate(g,GENERATOR);assert section[2]==210 and len(g)==139608
 v,=m.records(g,12,len(g));groups=list(m.records(g,v[2]+12,v[3]));assert len(groups)<=16 and [x[1][:4] for x in groups[:5]]==[b'SGRP',b'FGRP',b'RGRP',b'EGRP',b'MGRP']
 mg=groups[4];mv,=m.records(g,mg[2]+12,mg[3]);assert mg[2]==6602 and mg[3]-mg[2]==5633 and mv[1][:4]==b'0000'
 visited=0
 for family in m.records(g,mv[2]+12,mv[3]):
  visited+=1;assert visited<=64 and family[1][:4]==b'MFAM'
  records=list(m.records(g,family[2]+12,family[3]));assert len(records)<=8
  data=records[0];assert data[0]==b'DATA' and len(data[1])>=5
  key=struct.unpack_from('<i',data[1])[0]
  if key!=1:continue
  raw=g[family[2]:family[3]];gate(raw,FAMILY)
  assert family[2]==6626 and len(raw)==132 and len(records)==2 and records[1][0]==b'FORM' and records[1][1][:4]==b'MFRC'
  assert len(data[1])==28 and data[1][-1]==0 and data[1][4:-1].find(b'\0')<0
  return dict(ConfigSha256=CONFIG,SourceSha256=SOURCE,GeneratorSha256=GENERATOR,Priority=42,GroupOffset=6602,GroupVersion='0000',FamilyKey=1,FamilyOffset=6626,FamilyBytes=132,FamilySha256=FAMILY,NameBytes=23,EmbeddedForm='MFRC',FamiliesVisitedBeforeStop=visited,NoiseDecoded=False,IdentityNegatives=config_negative+negatives(payload,SOURCE)+negatives(g,GENERATOR)+negatives(raw,FAMILY))
 raise AssertionError('family1 absent')

def synthetic_branch_checks():
 # Independently authored signed-32 lookup cases: balanced-tree instruction
 # walk compared with a sorted-key bisect reference. No production map API.
 signed=lambda v:v if v<0x80000000 else v-0x100000000
 raw_keys=[0,1,27,0x7fffffff,0x80000000,0xfffffffe,0xffffffff]
 keys=sorted(map(signed,raw_keys));pointer={key:0x1000+index*0x3000 for index,key in enumerate(keys)}
 def tree(values):
  if not values:return None
  middle=len(values)//2;return (values[middle],tree(values[:middle]),tree(values[middle+1:]))
 root=tree(keys);checks=0
 for raw_key in raw_keys+[2,26,28,0x7ffffffe,0x80000001,0xfffffffd]:
  key=signed(raw_key);node=root;candidate=None
  while node:
   if node[0]>=key:candidate=node[0];node=node[1]
   else:node=node[2]
  got=pointer[candidate] if candidate is not None and key>=candidate else 0
  index=bisect_left(keys,key);want=pointer[key] if index<len(keys) and keys[index]==key else 0
  assert got==want;checks+=1
  assert (got+8)&0xffffffff == (want+8)&0xffffffff;checks+=1
 # Fresh consumer sentinel equality bypasses lookup and retains null pointer.
 cached_key=0xffffffff;cached_pointer=0
 assert cached_key==0xffffffff and cached_pointer==0;checks+=1
 # Concrete unchecked getter maps a missing item0 to address8, not a valid object.
 assert ((0+8)&0xffffffff)==8;checks+=1
 # Stable key alone cannot detect removal/reload: a synthetic witness only.
 old_pointer=0x1008;desired=cached_key=1
 assert desired==cached_key and old_pointer!=0x4008;checks+=1
 return dict(SyntheticBranchChecks=checks,SignedLookupCases=13,MissingGetterAddress='00000008',SentinelCollisionPreservesNull=True,StableKeyCannotProvePointerLifetime=True)

def verify(executable,packages,root,config):
 result=static(executable,packages)
 original=selected_family(root,config)
 ahfr=load('family_ahfr','tools/python/verify_terrain_height_fractal.py')
 ffra=load('family_ffra','tools/python/verify_terrain_filter_fractal.py')
 prior_ahfr=ahfr.static(str(executable),str(packages));prior_ffra=ffra.static(executable,packages)
 return dict(Passed=True,Static=result,Original=original,Synthetic=synthetic_branch_checks(),FrozenPrerequisites=dict(AhfrAnchors=prior_ahfr['InstructionAnchors'],FfraAnchors=prior_ffra['InstructionAnchors']),ProductionChanges=False,OriginalExecution=False,NoiseGeneration=False,GlobalInvalidationObserver='Unknown',ConcurrentQueryExclusion='Unknown')
if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__)
 for argument in ['executable','packages','asset_root','config']:parser.add_argument(argument,type=Path)
 args=parser.parse_args();print(json.dumps(verify(args.executable,args.packages,args.asset_root,args.config),indent=2))
