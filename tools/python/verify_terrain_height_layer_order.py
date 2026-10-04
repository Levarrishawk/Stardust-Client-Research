"""Identity-gated selected Tatooine FHGT topology and original recursive shared-height driver; research only."""
import argparse,contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SOURCE='6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
GENERATOR='B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32'
ROOT=Path(__file__).resolve().parents[2]
def identity(data,expected,label,size=None):
 if (size is not None and len(data)!=size) or hashlib.sha256(data).hexdigest().upper()!=expected:
  raise ValueError(label+' identity mismatch; rejected before decoding')
 return expected

def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
ANCHORS={
    # Selected preceding FSLP derived-cache demand predicate.
    0x00B79020: ('mov', 'al, 1'),
    0x00B79022: ('ret', ''),
    # fresh load preparation, top-level ordering and AHFR constructor identity
    0x00B651A2: ('mov', 'ecx, dword ptr [ebx + 0x50]'),
    0x00B651A5: ('mov', 'edx, dword ptr [ebx + 0x4c]'),
    0x00B651AB: ('mov', 'dword ptr [edx + ecx*4], eax'),
    0x00B651AE: ('inc', 'dword ptr [ebx + 0x50]'),
    0x00B65D77: ('call', 'dword ptr [eax + 0x10]'),
    0x00B65D7D: ('call', '0xb65130'),
    0x00B65D85: ('jne', '0xb65d48'),
    0x00B65C7B: ('cmp', 'eax, 0x30303030'),
    0x00B65C80: ('je', '0xb65c94'),
    0x00B65C97: ('call', '0xb65cc0'),
    0x00B65CAC: ('call', '0xb653c0'),
    0x00B767E3: ('push', '0x41484652'),
    0x00B767EA: ('call', '0xb63440'),
    0x00B767F1: ('mov', 'dword ptr [esi + 0x14], eax'),
    0x00B767FA: ('mov', 'dword ptr [esi + 0x18], 0xffffffff'),
    0x00B76808: ('mov', 'dword ptr [esi], 0x15ed698'),
    # load and append
    0x00B64836: ('call', '0xb64bb0'),
    0x00B64C76: ('call', '0xb79d00'),
    0x00B64C7F: ('jne', '0xb64c70'),
    0x00B79D31: ('call', '0xb63830'),
    0x00B79D5C: ('call', '0xb63990'),
    0x00B79D84: ('call', '0xb63af0'),
    0x00B79DCE: ('call', '0xb64550'),
    0x00B638A2: ('mov', 'ecx, dword ptr [ebx + 0x18]'),
    0x00B638A5: ('mov', 'edx, dword ptr [ebx + 0x14]'),
    0x00B638AB: ('mov', 'dword ptr [edx + ecx*4], eax'),
    0x00B638AE: ('inc', 'dword ptr [ebx + 0x18]'),
    0x00B645C2: ('mov', 'ecx, dword ptr [ebx + 0x3c]'),
    0x00B645C5: ('mov', 'edx, dword ptr [ebx + 0x38]'),
    0x00B645CB: ('mov', 'dword ptr [edx + ecx*4], eax'),
    0x00B645CE: ('inc', 'dword ptr [ebx + 0x3c]'),
    0x00B63A02: ('mov', 'ecx, dword ptr [ebx + 0x24]'),
    0x00B63A05: ('mov', 'edx, dword ptr [ebx + 0x20]'),
    0x00B63A0B: ('mov', 'dword ptr [edx + ecx*4], eax'),
    0x00B63A0E: ('inc', 'dword ptr [ebx + 0x24]'),
    0x00B63B62: ('mov', 'ecx, dword ptr [ebx + 0x30]'),
    0x00B63B65: ('mov', 'edx, dword ptr [ebx + 0x2c]'),
    0x00B63B6B: ('mov', 'dword ptr [edx + ecx*4], eax'),
    0x00B63B6E: ('inc', 'dword ptr [ebx + 0x30]'),
    # generator initial state
    0x00B65406: ('mov', 'al, byte ptr [ecx + 0x60]'),
    0x00B65414: ('jne', '0xb65426'),
    0x00B65416: ('mov', 'byte ptr [ecx + 0x60], 1'),
    0x00B6541E: ('add', 'ecx, 0x40'),
    0x00B65421: ('call', '0xb75ae0'),
    0x00B65426: ('mov', 'edx, dword ptr [ebx + 0x18]'),
    0x00B65429: ('mov', 'ecx, dword ptr [edx + 4]'),
    0x00B6542C: ('imul', 'ecx, dword ptr [edx]'),
    0x00B6542F: ('mov', 'edi, dword ptr [edx + 8]'),
    0x00B6543A: ('xor', 'eax, eax'),
    0x00B6543C: ('rep stosd', 'dword ptr es:[edi], eax'),
    0x00B6571A: ('call', '0xb65280'),
    0x00B65284: ('mov', 'eax, dword ptr [ecx + 0x54]'),
    0x00B65291: ('lea', 'esi, [ecx + 0x54]'),
    0x00B652EE: ('mov', 'dword ptr [ebx + ecx*4], 0x3f800000'),
    0x00B65324: ('mov', 'byte ptr [edi + 0x570], al'),
    0x00B6532A: ('mov', 'byte ptr [edi + 0x571], al'),
    0x00B65355: ('mov', 'ecx, dword ptr [eax + 0x4c]'),
    0x00B65358: ('mov', 'ecx, dword ptr [ecx + ebx*4]'),
    0x00B6535B: ('call', '0xb62f20'),
    0x00B65362: ('je', '0xb6536b'),
    0x00B65364: ('push', 'edi'),
    0x00B65365: ('push', 'esi'),
    0x00B65366: ('call', '0xb63d20'),
    0x00B65374: ('jl', '0xb65352'),
    # preparation and recursive gates
    0x00B653C4: ('mov', 'byte ptr [esi + 0x60], 0'),
    0x00B653C8: ('call', '0xb65f20'),
    0x00B653DC: ('call', '0xb62f20'),
    0x00B653E3: ('je', '0xb653ea'),
    0x00B653E7: ('call', 'dword ptr [edx + 4]'),
    0x00B63D3F: ('mov', 'ecx, dword ptr [esi + 0x48]'),
    0x00B63D46: ('mov', 'eax, dword ptr [esi + 0x4c]'),
    0x00B63D4B: ('je', '0xb6428e'),
    0x00B63D56: ('mov', 'ebx, dword ptr [ebp + 0xc]'),
    0x00B63D59: ('je', '0xb63dab'),
    0x00B63DBB: ('mov', 'byte ptr [ebp - 0xd], 1'),
    0x00B63DC1: ('mov', 'byte ptr [ebp - 0xd], 0'),
    0x00B63E0F: ('mov', 'al, byte ptr [ebp - 0xd]'),
    0x00B63E14: ('jne', '0xb63e53'),
    0x00B63F05: ('mov', 'byte ptr [ebp - 0xf], al'),
    0x00B63F08: ('jne', '0xb6420f'),
    0x00B64150: ('mov', 'byte ptr [ebp - 0xf], 1'),
    0x00B6420D: ('je', '0xb6424b'),
    0x00B64214: ('jle', '0xb6424b'),
    0x00B64220: ('mov', 'edx, dword ptr [esi + 0x38]'),
    0x00B64223: ('mov', 'ecx, dword ptr [edx + edi*4]'),
    0x00B64226: ('call', '0xb62f20'),
    0x00B6422D: ('je', '0xb64243'),
    0x00B64234: ('mov', 'eax, dword ptr [ebp + 8]'),
    0x00B64237: ('jne', '0xb6423c'),
    0x00B64239: ('lea', 'eax, [esi + 0x70]'),
    0x00B6423C: ('push', 'ebx'),
    0x00B6423D: ('push', 'eax'),
    0x00B6423E: ('call', '0xb63d20'),
    0x00B64249: ('jl', '0xb64220'),
    # derived-cache demand and dirty notification
    0x00B63E77: ('call', 'dword ptr [edx + 0x18]'),
    0x00B63E7C: ('jne', '0xb63e8e'),
    0x00B63E8E: ('mov', 'al, byte ptr [ebx + 0x570]'),
    0x00B63E96: ('je', '0xb63ea8'),
    0x00B63E99: ('call', '0xb65730'),
    0x00B63EA1: ('mov', 'byte ptr [ebx + 0x570], 0'),
    0x00B63ED1: ('call', 'dword ptr [edx + 0x1c]'),
    0x00B63ED6: ('je', '0xb63ef2'),
    0x00B63ED8: ('mov', 'al, byte ptr [ebx + 0x571]'),
    0x00B63EE0: ('je', '0xb63ef2'),
    0x00B63EE3: ('call', '0xb65af0'),
    0x00B63EEB: ('mov', 'byte ptr [ebx + 0x571], 0'),
    0x00B64197: ('call', 'dword ptr [edx + 0x14]'),
    0x00B6419C: ('je', '0xb641a8'),
    0x00B641A1: ('mov', 'byte ptr [eax + 0x570], 1'),
    0x00B641B0: ('call', 'dword ptr [edx + 0x18]'),
    0x00B641B8: ('je', '0xb641c1'),
    0x00B641BA: ('mov', 'byte ptr [ebx + 0x571], 1'),
    0x00B63420: ('xor', 'al, al'),
    0x00B63422: ('ret', ''),
    0x00B63430: ('xor', 'al, al'),
    0x00B63432: ('ret', ''),
    0x00B76980: ('mov', 'al, 1'),
    0x00B76982: ('ret', ''),
    # AHFR current height and local lookup cache
    0x00B768B1: ('test', 'ah, 0x41'),
    0x00B768B4: ('jne', '0xb76962'),
    0x00B768BA: ('mov', 'eax, dword ptr [esi + 0x1c]'),
    0x00B768BD: ('cmp', 'dword ptr [esi + 0x18], eax'),
    0x00B768C0: ('je', '0xb768d4'),
    0x00B768C2: ('mov', 'dword ptr [esi + 0x18], eax'),
    0x00B768C6: ('mov', 'eax, dword ptr [ebp + 0x1c]'),
    0x00B768C9: ('mov', 'ecx, dword ptr [eax + 0x58]'),
    0x00B768CC: ('call', '0xb75b70'),
    0x00B768D1: ('mov', 'dword ptr [esi + 0x14], eax'),
    0x00B768E9: ('call', '0x13e7c00'),
    0x00B768F1: ('mov', 'eax, dword ptr [ebp + 0x1c]'),
    0x00B768F4: ('mov', 'eax, dword ptr [eax + 0x18]'),
    0x00B768F7: ('mov', 'ecx, dword ptr [eax]'),
    0x00B768F9: ('mov', 'edx, dword ptr [eax + 8]'),
    0x00B768FC: ('imul', 'ecx, edi'),
    0x00B76902: ('add', 'ecx, ebx'),
    0x00B7690A: ('fld', 'dword ptr [edx + ecx*4]'),
    0x00B7694D: ('mov', 'eax, dword ptr [ebp + 0x1c]'),
    0x00B76950: ('mov', 'eax, dword ptr [eax + 0x18]'),
    0x00B76953: ('mov', 'ecx, dword ptr [eax]'),
    0x00B76955: ('mov', 'edx, dword ptr [eax + 8]'),
    0x00B76958: ('imul', 'ecx, edi'),
    0x00B7695B: ('add', 'ecx, ebx'),
    0x00B7695F: ('fstp', 'dword ptr [edx + ecx*4]'),
    # product grid is distinct
    0x00B641D0: ('mov', 'eax, dword ptr [esi + 0x70]'),
    0x00B641D6: ('imul', 'eax, dword ptr [ebp - 0x1c]'),
    0x00B641DA: ('fmul', 'dword ptr [ebp - 0x2c]'),
    0x00B641E0: ('mov', 'ecx, dword ptr [esi + 0x78]'),
    0x00B641E3: ('fstp', 'dword ptr [ecx + eax*4]'),
}

def static(exe,packages):
 data=exe.read_bytes();identity(data,EXE,'Executable')
 preparation=load('height_order_preparation','tools/python/verify_terrain_layer_preparation.py').verify(exe,packages)
 fh=load('height_order_fhgt','poc/round42-tests/Assets/verify_height_filter.py').static(str(exe),str(packages))
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,want in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want,(i.mnemonic,i.op_str))
 tables=[(0x015ED698,(0xb76820,0xb62f90,0xb62fa0,0xb76fa0,0xb768a0,0xb76980,0xb63480,0xb76990)),(0x015ED6E0,(0xb776f0,0xb62f90,0xb777e0,0xb77aa0,0xb65fe0,0xb77730,0xb63420,0xb63430))]
 assert struct.unpack_from('<2I',image,0x015ED744-base)==(0xb79020,0xb63430)
 for va,want in tables:assert struct.unpack_from('<8I',image,va-base)==want
 for vt,col,td,name in [(0x015ED698,0x016262C0,0x018B8A30,b'.?AVAffectorHeightFractal@@\0'),(0x015ED6E0,0x01626358,0x018B8A78,b'.?AVFilterHeight@@\0')]:
  assert struct.unpack_from('<I',image,vt-base-4)[0]==col
  assert struct.unpack_from('<I',image,col-base+12)[0]==td
  assert image[td-base+8:td-base+8+len(name)]==name
 return dict(ExecutableSha256=EXE,InstructionAnchors=len(ANCHORS),Preparation=preparation,HeightFilter=fh,RttiObjects=2,VtableEntries=18,RecursiveDriver='00B63D20',GeneratorDriver='00B65280',GeneratorInitialization='00B65400',OriginalExecuted=False,ActiveFloatingControls='Unknown')

def topology(data):
 """Bounded frame walk stops at the first FHGT; does not decode later layers or numeric sibling rules."""
 records_read=0
 def records(start,end):
  nonlocal records_read
  while start<end:
   if end-start<8:raise ValueError('Truncated IFF header')
   tag=data[start:start+4];size=struct.unpack_from('>I',data,start+4)[0];stop=start+8+size
   if stop>end:raise ValueError('IFF extent')
   records_read+=1
   if records_read>512:raise ValueError('Selected prefix record budget')
   yield dict(Tag=tag,Type=data[start+8:start+12] if tag==b'FORM' else None,Start=start,End=stop)
   start=stop
  if start!=end:raise ValueError('IFF boundary')
 def children(row):
  if row['Tag']!=b'FORM' or row['End']-row['Start']<12:raise ValueError('Expected form')
  return list(records(row['Start']+12,row['End']))
 def version(row):
  v,=children(row);return v
 def enabled(header):
  hv=version(header);d,=children(hv)
  if header['Type']!=b'IHDR' or hv['Type'] not in (b'0000',b'0001') or d['Tag']!=b'DATA' or d['End']-d['Start']<12:raise ValueError('Header structure')
  return struct.unpack_from('<I',data,d['Start']+8)[0]
 def brief(row):return dict(Tag=row['Type'].decode(),Offset=row['Start'],Bytes=row['End']-row['Start'],Sha256=hashlib.sha256(data[row['Start']:row['End']]).hexdigest().upper())
 top,=records(0,len(data));tv=version(top)
 if top['Type']!=b'TGEN' or tv['Type']!=b'0000':raise ValueError('TGEN identity')
 groups=children(tv);lyrs=next(x for x in groups if x['Type']==b'LYRS');layers=[];selected=None
 def visit(row,parent,depth):
  nonlocal selected
  if depth>16 or len(layers)>=64:raise ValueError('Selected layer prefix budget')
  v=version(row);parts=children(v)
  if row['Type']!=b'LAYR' or v['Type']!=b'0003' or parts[1]['Tag']!=b'ADTA':raise ValueError('Selected LAYR layout')
  flags=list(struct.unpack_from('<3I',data,parts[1]['Start']+8));index=len(layers)
  layer=dict(Index=index,Parent=parent,Enabled=enabled(parts[0]),Flags=flags,Section=brief(row),OwnRules=[],Children=[brief(x) for x in parts[2:]])
  layers.append(layer)
  # Runtime groups families: own affectors complete before child recursion, independent of raw interleaving.
  for ordinal,child in enumerate(parts[2:],2):
   if child['Type']==b'LAYR':continue
   cv=version(child);cp=children(cv);rule=dict(Ordinal=ordinal,Enabled=enabled(cp[0]),Version=cv['Type'].decode(),Section=brief(child));layer['OwnRules'].append(rule)
  for ordinal,child in enumerate(parts[2:],2):
   if child['Type']==b'FHGT':selected=dict(Layer=index,Ordinal=ordinal,Section=brief(child));return True
   if child['Type']==b'LAYR' and visit(child,index,depth+1):return True
  return False
 for row in children(lyrs):
  if visit(row,None,0):break
 if selected is None:raise ValueError('Selected FHGT missing')
 return dict(Layers=layers,Selected=selected,RecordsRead=records_read,SelectedFhgtOccurrences=1,OriginalAssetInputs=1,LaterLayerSemanticsDecoded=False)

def occurrence(root,config):
 identity(config.read_bytes(),CONFIG,'Config')
 m=load('height_order_frozen_archive','poc/round33-tests/Assets/verify_tgen_input.py')
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,config):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
  else:raise ValueError('Pinned terrain absent')
 identity(payload,SOURCE,'Terrain source',1188446)
 v,=m.records(payload,12,len(payload));_,section,_=m.records(payload,v[2]+12,v[3]);data=payload[section[2]:section[3]]
 if section[2]!=210:raise ValueError('Generator offset')
 identity(data,GENERATOR,'Generator',139608);result=topology(data)
 selected=result['Selected'];assert selected==dict(Layer=4,Ordinal=5,Section=dict(Tag='FHGT',Offset=14204,Bytes=97,Sha256='61773ADF9E1765EEAB0D87844C0E9779578FDAAA22D276103426D4EBAC20B99C'))
 assert [(x['Index'],x['Parent'],x['Enabled'],x['Flags'],x['Section']['Offset']) for x in result['Layers']]==[(0,None,1,[0,0,0],12247),(1,0,1,[0,0,0],12810),(2,0,1,[0,0,0],13059),(3,0,1,[1,0,0],13364),(4,0,1,[1,0,0],13786)]
 want=[['AHFR','ASCN','ACRF','AENV'],['ACRF'],['FSLP','AFDN'],['BCIR','FFRA','AHFR'],['BCIR','BCIR','FFRA','FHGT','AHFR']]
 assert [[r['Section']['Tag'] for r in x['OwnRules']] for x in result['Layers']]==want
 assert all(rule['Enabled']==1 for layer in result['Layers'] for rule in layer['OwnRules'])
 assert not any(rule['Section']['Tag']=='AHCN' for layer in result['Layers'] for rule in layer['OwnRules'])
 assert priority==42
 result.update(ConfigSha256=CONFIG,SourceSha256=SOURCE,GeneratorSha256=GENERATOR,GeneratorOffset=210,Priority=priority,SelectedLayerContainsAhcn=False,PrecedingSelectedPrefixContainsAhcn=False,ActualPriorHeightWriter='AHFR',HeightValueParity='Unknown')
 return result

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--asset-root',type=Path,required=True);p.add_argument('--config',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);args=p.parse_args()
 if 'local-private' not in args.receipt.parts:p.error('Private receipt required')
 # Both identities precede optional module loading, VA decoding and archive lookup.
 identity(args.exe.read_bytes(),EXE,'Executable');identity(args.config.read_bytes(),CONFIG,'Config')
 result=dict(Static=static(args.exe,args.packages),Occurrence=occurrence(args.asset_root,args.config),ResearchOnly=True,GeneratedHeightParity=False,OtherAssetsRead=0)
 args.receipt.parent.mkdir(parents=True,exist_ok=True);args.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(dict(InstructionAnchors=result['Static']['InstructionAnchors'],SelectedFhgtOccurrences=1,OriginalAssetInputs=1,SelectedLayer=4,AhcnInSelectedPath=False,ProductionChanged=False)))
if __name__=='__main__':main()
