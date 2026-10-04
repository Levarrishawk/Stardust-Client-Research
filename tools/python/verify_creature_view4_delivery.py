"""Read-only, identity-gated CREO4 delivery source/dispatch audit. No original execution."""
import argparse,hashlib,json,re,struct,sys
from pathlib import Path
EXPECTED_EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SERVER_HEAD='9a8529ef8291efc5ca1b41d9927f0e88d64b79bb'
SERVER_PINS={'MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp': '8c69d2129ff65f81cbd24485bd5de4b7ae216c3976b70f804c4864017115da93', 'MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage4.h': 'de8dfe60dec0e62bd24b4e0beaf49ea964df59e527e7b5bd11416a3efec68ab0', 'MMOCoreORB/src/server/zone/packets/creature/CreatureObjectDeltaMessage4.h': 'c888a627885d2c7ca724a93e4945d58ee74970bc873e53b61dcc52beeccf2cdc', 'MMOCoreORB/src/server/zone/objects/scene/SceneObjectImplementation.cpp': '0d6a268510f93db15a4af06db930acfa0e359ffa71e7dfa216553d835b3c681f', 'MMOCoreORB/src/server/zone/managers/skill/SkillManager.cpp': 'd314100d0f1b44199940c3c087e0beaea548a301b803cfb762c50633b46ab545', 'MMOCoreORB/src/server/zone/objects/creature/ai/AiAgentImplementation.cpp': 'da1fa35a8497fcd76bb1490ec5444bbee2746985ba66cbd7578f20d93c223a03', 'MMOCoreORB/src/server/zone/objects/creature/ai/CreatureImplementation.cpp': '205f9b62e7c345e20343fbcefafae459b8c9942e15c0f48e29cb106a9828217a', 'MMOCoreORB/src/server/zone/objects/creature/VehicleObjectImplementation.cpp': '93bc355ee8d1631b49263d7fbbafbb69c484d878ff309e25939054d8dee09a0b', 'MMOCoreORB/src/server/zone/packets/BaseLineMessage.h': '1c1b883f63ee5ff82bf0ffb583a79cf88cda4ef02970425a182651a056261774', 'MMOCoreORB/src/server/zone/packets/DeltaMessage.h': '005f4e15fe6232d75569c37b26b9b659011ea0750605de80aa397c1593d1036a', 'MMOCoreORB/CMakeLists.txt': '7a2d82d900374108e53b3cb7694cea244432586ded058073fb211676b71fcbde', 'MMOCoreORB/src/server/zone/packets/scene/SceneObjectCloseMessage.h': '63bed12d13236262ff309105b20ab6f197f0d6bd4181926db0793aa10f6934aa'}
CLIENT_ANCHORS=[(0x51c6fa,'call','0xadbc80'),(0x51c70d,'call','0x51e690'),(0x51c725,'call','0xb380e0'),(0x51c744,'test','esi, esi'),(0x51c746,'je','0x51c756'),(0x51c751,'call','0x554cf0'),(0x51c7a9,'call','0xadb2d0'),(0x51c7bc,'call','0x51e7c0'),(0x51c7d4,'call','0xb380e0'),(0x51c7f3,'test','esi, esi'),(0x51c7f5,'je','0x51c80c'),(0x51c800,'call','0x554e90'),(0x554d24,'movzx','eax, byte ptr [edi + 0x54]'),(0x554d29,'cmp','eax, 8'),(0x554d35,'jmp','dword ptr [eax*4 + 0x554e68]'),(0x554d6c,'mov','edx, dword ptr [esi + 0xc4]'),(0x554d72,'lea','ecx, [esi + 0xc4]'),(0x554d7c,'call','dword ptr [edx + 0xc]'),(0x554ec4,'movzx','eax, byte ptr [edi + 0x54]'),(0x554ed5,'jmp','dword ptr [eax*4 + 0x554fe4]'),(0x554f1a,'add','esi, 0xc4'),(0x554f20,'jmp','0x554f03'),(0x554f0b,'call','dword ptr [eax + 0x18]'),(0x554f12,'call','dword ptr [edx + 0x1c]')]
CREATURE='MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp'
CONSTRUCTOR_ANCHORS=[
 (0x42f06c,'mov','eax, 0x3f800000'),(0x42f071,'mov','dword ptr [edi + 0xc], eax'),(0x42f074,'mov','dword ptr [edi + 0x10], eax'),
 (0x42f08e,'mov','eax, 0x3f800000'),(0x42f093,'mov','dword ptr [edi + 0xc], eax'),(0x42f096,'mov','dword ptr [edi + 0x10], eax'),
 (0x42f11d,'mov','eax, 0x3f800000'),(0x42f122,'mov','dword ptr [esi + 0x688], eax'),(0x42f128,'mov','dword ptr [esi + 0x68c], eax'),
 (0x42f147,'mov','eax, 0x3f800000'),(0x42f14c,'mov','dword ptr [esi + 0x69c], eax'),(0x42f152,'mov','dword ptr [esi + 0x6a0], eax'),
 (0x42f171,'mov','eax, 0x3f800000'),(0x42f176,'mov','dword ptr [esi + 0x6b0], eax'),(0x42f17c,'mov','dword ptr [esi + 0x6b4], eax'),
 (0x42f09f,'mov','edi, dword ptr [ebx + 8]'),
 (0x42f0a2,'push','1'),(0x42f0aa,'call','0x11ade40'),(0x42f0af,'fstp','dword ptr [ebp - 0x1c]'),(0x42f0c2,'mov','dword ptr [esi + 0x660], eax'),(0x42f0c8,'mov','dword ptr [esi + 0x664], ecx'),
 (0x42f0d8,'push','0'),(0x42f0e0,'call','0x11ade40'),(0x42f0e5,'fstp','dword ptr [ebp - 0x1c]'),(0x42f0f8,'mov','dword ptr [esi + 0x674], edx'),(0x42f0fe,'mov','dword ptr [esi + 0x678], eax'),
 (0x42f18c,'push','0'),(0x42f194,'call','0x11ae700'),(0x42f199,'fmul','dword ptr [0x15dcbd4]'),(0x42f1a5,'fmul','dword ptr [0x15dc5f0]'),(0x42f1ab,'fstp','dword ptr [ebp - 0x1c]'),(0x42f1b8,'mov','dword ptr [esi + 0x6c4], ecx'),(0x42f1be,'mov','dword ptr [esi + 0x6c8], edx'),
 (0x42f1ce,'push','0'),(0x42f1d6,'call','0x11aea90'),(0x42f1db,'fstp','dword ptr [ebp - 0x1c]'),(0x42f1ee,'mov','dword ptr [esi + 0x6d8], eax'),(0x42f1f4,'mov','dword ptr [esi + 0x6dc], ecx'),
 (0x42f204,'push','0'),(0x42f20c,'call','0x11aee20'),(0x42f211,'fstp','dword ptr [ebp - 0x1c]'),(0x42f226,'mov','dword ptr [edi + 0xc], edx'),(0x42f229,'mov','dword ptr [edi + 0x10], eax'),
 (0x42f05b,'lea','edi, [esi + 0x62c]'),(0x42f07d,'lea','edi, [esi + 0x640]'),(0x42f10e,'lea','ecx, [esi + 0x67c]'),(0x42f138,'lea','ecx, [esi + 0x690]'),(0x42f162,'lea','ecx, [esi + 0x6a4]'),
 (0x42f0b2,'lea','ecx, [esi + 0x654]'),(0x42f0e8,'lea','ecx, [esi + 0x668]'),(0x42f19f,'lea','ecx, [esi + 0x6b8]'),(0x42f1de,'lea','ecx, [esi + 0x6cc]'),(0x42f214,'lea','edi, [esi + 0x6e0]'),
 (0x42f0bd,'mov','eax, dword ptr [ebp - 0x1c]'),(0x42f0f3,'mov','edx, dword ptr [ebp - 0x1c]'),(0x42f1b3,'mov','ecx, dword ptr [ebp - 0x1c]'),(0x42f1e9,'mov','eax, dword ptr [ebp - 0x1c]'),(0x42f221,'mov','edx, dword ptr [ebp - 0x1c]')]
SCENE='MMOCoreORB/src/server/zone/objects/scene/SceneObjectImplementation.cpp'
RULES=[
(CREATURE,'CreatureObjectImplementation::sendBaselinesTo',['if (zone == nullptr)','if (player == thisPointer)','CreatureObjectMessage1','CreatureObjectMessage3','CreatureObjectMessage4','else if (isPlayerCreature())','sendSpeedAndAccelerationMods(player)','CreatureObjectMessage6']),
(CREATURE,'CreatureObjectImplementation::sendSpeedAndAccelerationMods',['player == asSceneObject() || player->isASubChildOf(asSceneObject())','updateSpeedAndAccelerationMods(sendSelf)','player->sendMessage(dcreo4)']),
(CREATURE,'CreatureObjectImplementation::broadcastSpeedAndAccelerationMods',['updateSpeedAndAccelerationMods(sendSelf)','broadcastMessage(dcreo4, sendSelf)']),
(CREATURE,'CreatureObjectImplementation::setEncumbrance',['encumbrances.get(type) == value','if (notifyClient)','startUpdate(0x02)','broadcastMessage(msg, true)']),
(CREATURE,'CreatureObjectImplementation::addSkillMod',['newMod == oldMod','if (notifyClient)','startUpdate(0x03)','sendMessage(msg)']),
(CREATURE,'CreatureObjectImplementation::setListenToID',['listenToID == id','if (!notifyClient)','updateListenToID(id)','sendMessage(codm4)']),
(CREATURE,'CreatureObjectImplementation::sendMessage',['owner.get()','ownerClient == nullptr','delete msg','ownerClient->sendMessage(msg)']),
(CREATURE,'CreatureObjectImplementation::addSpaceMissionObject',['missionObjectID <= 0','if (notifyClient)','startUpdate(0x0D)','sendMessage(delta4)','!notifyGroup']),
(CREATURE,'CreatureObjectImplementation::removeSpaceMissionObject',['!spaceMissionObjects.containsValue(missionObjectID)','if (notifyClient)','startUpdate(0x0D)','sendMessage(delta4)','!notifyGroup']),
(CREATURE,'CreatureObjectImplementation::removeAllSpaceMissionObjects',['if (notifyClient)','startUpdate(0x0D)','spaceMissionObjects.removeAll(delta4)','delta4->close()']),
(SCENE,'SceneObjectImplementation::sendTo',['!sendToClient','player == nullptr || player->getClient() == nullptr','new SceneObjectCreateMessage','link(player, containmentType)','sendBaselinesTo(player)','if (doClose)','SceneObjectImplementation::close(player)']),
(SCENE,'SceneObjectImplementation::close',['new SceneObjectCloseMessage','client->sendMessage(msg)']),
(SCENE,'SceneObjectImplementation::broadcastMessage',['sendSelf ? nullptr : asSceneObject()','broadcastMessagePrivate(message, selfObject, lockZone)']),
(SCENE,'SceneObjectImplementation::broadcastMessagePrivate',['isServerLoading()','isServerShuttingDown()','rootParent->broadcastMessagePrivate','zone == nullptr','#ifdef LOCKFREE_BCLIENT_BUFFERS','closeobjects->safeRunForEach','safeCopyReceiversTo','sceneO == selfObject']),
('MMOCoreORB/src/server/zone/objects/creature/ai/AiAgentImplementation.cpp','AiAgentImplementation::sendBaselinesTo',['CreatureObjectImplementation::sendBaselinesTo(player)']),
('MMOCoreORB/src/server/zone/objects/creature/VehicleObjectImplementation.cpp','VehicleObjectImplementation::sendMessage',['linkedCreature->getParent().get() == _this.getReferenceUnsafeStaticCast()','linkedCreature->sendMessage(msg)']),
('MMOCoreORB/src/server/zone/objects/creature/ai/CreatureImplementation.cpp','CreatureImplementation::sendMessage',['if (!isMount())','linkedCreature->sendMessage(msg)'])]
def body(text,symbol):
 start=text.index(symbol+'(');start=text.index('{',start);depth=0
 # Strip comments for brace structure without treating debug string braces as code.
 token=re.compile(r'/\*.*?\*/|//[^\n]*|"(?:\\.|[^"\\])*"|[{}]',re.S)
 for m in token.finditer(text,start):
  t=m.group()
  if t=='{':depth+=1
  elif t=='}':
   depth-=1
   if depth==0:return text[start:m.end()],text[:start].count('\n')+1,text[:m.end()].count('\n')+1
 raise ValueError('Unterminated source symbol '+symbol)
def audit(server_root,exe,packages,repo=None):
 raw=exe.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
 if digest!=EXPECTED_EXE:raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
 sources={}
 for name,expected in SERVER_PINS.items():
  data=(server_root/name).read_bytes()
  if hashlib.sha256(data).hexdigest()!=expected:raise ValueError('Changed server source identity: '+name)
  sources[name]=data.decode('utf-8-sig')
 rules=[]
 for name,symbol,needles in RULES:
  b,first,last=body(sources[name],symbol)
  for token in needles:
   if token not in b:raise ValueError('Source invariant '+symbol+' / '+token)
  rules.append(dict(Path=name,Symbol=symbol,FirstLine=first,LastLine=last,Checks=len(needles)))
 seed,_,_=body(sources[CREATURE],'CreatureObjectImplementation::sendBaselinesTo')
 assert seed.index('CreatureObjectMessage3')<seed.index('CreatureObjectMessage4')<seed.index('sendSpeedAndAccelerationMods(player)')<seed.index('CreatureObjectMessage6')
 clear,_,_=body(sources[CREATURE],'CreatureObjectImplementation::removeAllSpaceMissionObjects');assert 'sendMessage(' not in clear and 'broadcastMessage(' not in clear
 packet=sources['MMOCoreORB/src/server/zone/packets/creature/CreatureObjectDeltaMessage4.h'];speed,_,_=body(packet,'updateSpeedAndAccelerationMods');assert 'aScale == 0.f && mScale == 0.f' in speed and 'mScale == 0.f && !sendSelf' in speed and speed.count('0.1f')==2
 assert [int(x,16) for x in re.findall(r'addFloatUpdate\(0x([0-9A-Fa-f]+)',speed)]==[1,5,10]
 cmake=sources['MMOCoreORB/CMakeLists.txt'];assert re.search(r'option\(ENABLE_LOCKFREE_BCLIENT_BUFFERS[^\n]* OFF\)',cmake) and 'add_definitions(-DLOCKFREE_BCLIENT_BUFFERS)' in cmake
 # Identity gates above precede optional static-analysis imports and all VAs.
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);read=lambda va,n:pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,m,o in CLIENT_ANCHORS+CONSTRUCTOR_ANCHORS:
  i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(m,o),(hex(va),i.mnemonic,i.op_str)
 assert struct.unpack('<I',read(0x554e68+12,4))[0]==0x554d6c and struct.unpack('<I',read(0x554fe4+12,4))[0]==0x554f1a
 # Reuse accepted registration audit, without inventing another constructor schema.
 import runpy
 repo=repo or Path(__file__).resolve().parents[2]
 prerequisite=runpy.run_path(str(repo/'poc/round38-tests/Networking/static_audit.py'))['audit'](exe,packages)
 constructor=dict(InstructionAnchors=len(CONSTRUCTOR_ANCHORS),LiteralOneFields=[0,1,4,5,10],InputGetterFields=[7,8,9,11,12],CompleteInitialModel=False,LaterOverrides='Unknown')
 return dict(ExeSha256=digest,ServerHeadAtInvestigation=SERVER_HEAD,ServerFileSha256=SERVER_PINS,SourceRules=rules,SourceTokenChecks=sum(r['Checks'] for r in rules),NewClientAnchors=len(CLIENT_ANCHORS),Constructor=constructor,View4JumpTableEntries=2,PrerequisiteAnchors=prerequisite['InstructionAnchors'],Registrations=prerequisite['Registrations'],BaselineSelfOnly=True,ObserverDeltaBeforeView6=True,SpeedFields=[1,5,10],LockfreeSelfExclusionAmbiguous=True,MissionClearExplicitSend=False,OriginalExecuted=False,LiveContact=False)
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--server-root',type=Path,required=True);p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args();repo=Path(__file__).resolve().parents[2]
 if not a.receipt.resolve().is_relative_to((repo/'local-private').resolve()):p.error('Receipt must remain under ignored local-private')
 try:result=audit(a.server_root,a.exe,a.packages,repo)
 except (ValueError,AssertionError,OSError) as e:p.exit(1,str(e)+'\n')
 a.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(f"PASS {len(SERVER_PINS)} pinned server files /{len(RULES)} source rules /{len(CLIENT_ANCHORS)} direct anchors +{result['PrerequisiteAnchors']} prerequisite /14 registrations")
if __name__=='__main__':main()
