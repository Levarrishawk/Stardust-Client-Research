from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-component-presentation-guard')
backup.mkdir(exist_ok=False)
logs=backup/'incident-logs';logs.mkdir()
for p in Path(r'C:\Stardust-Experimental\diagnostics').glob('precu-ship-*.log'):shutil.copy2(p,logs/p.name)
shutil.copy2(Path(r'C:\Stardust-Experimental\diagnostics\zone-transitions.log'),logs/'zone-transitions.log')
changes={}
def edit(name,old,new):
 p=root/name
 if p not in changes:shutil.copy2(p,backup/p.name);changes[p]=p.read_text(encoding='utf-8')
 if changes[p].count(old)!=1:raise RuntimeError('Expected one anchor '+name)
 changes[p]=changes[p].replace(old,new)
edit('src/engine/client/library/clientGame/src/shared/object/PrecuShipSlotMap.h',
 '// Target selection has a distinct no-component sentinel, not a map key.',
 '''// Destruction is a presentation event: unsupported slots have no component effect.
// Keep strict map/target validation separate from this original installed-slot no-op.
inline bool destructionSlot(bool precu, int wireSlot, int &nativeSlot) {
    if (precu && (wireSlot < 0 || wireSlot > 111)) return false;
    nativeSlot = precu ? fromWire(wireSlot) : wireSlot;
    return true;
}
// Target selection has a distinct no-component sentinel, not a map key.''')
edit('src/engine/client/library/clientGame/src/shared/scene/GroundScene.cpp',
 '''		int const chassisSlot = ConfigFile::getKeyBool("ClientGame", "usePrecuReplication", false)
			? PrecuShipSlots::fromWire(destroyShipComponentMessage.getChassisSlot()) : destroyShipComponentMessage.getChassisSlot();''',
 '''		int chassisSlot = destroyShipComponentMessage.getChassisSlot();
		if (!PrecuShipSlots::destructionSlot(ConfigFile::getKeyBool("ClientGame", "usePrecuReplication", false), chassisSlot, chassisSlot))
		{
			// A decoded effect for an unsupported component must not tear down the scene.
			if (ConfigFile::getKeyBool("ClientGame", "tracePrecuShip", false))
			{
				static unsigned count = 0;
				if (count < 16)
				{
					FILE * const file = std::fopen("diagnostics/precu-ship-component-effect.log", count ? "a" : "w");
					if (file)
					{
						std::fprintf(file, "event=%u t=%.3f ignoredSlot=%d\\n", count++, Clock::getCurrentTime(), chassisSlot);
						std::fclose(file);
					}
				}
			}
			return;
		}''')
edit('tests/compatibility/ShipCombatWireTests.h',
 ' prefixes<DestroyShipComponentMessage>(destruction);',
 ''' prefixes<DestroyShipComponentMessage>(destruction);
 // Complete unsupported presentation events are no-ops, not fatal scene errors.
 // The real message parser still rejects every truncated packet prefix above.
 for(int wire=0;wire<=111;++wire) {
  int native=-7;check(PrecuShipSlots::destructionSlot(true,wire,native));
  check(native==(wire<12?wire:wire+3));
 }
 int const unsupported[]={-2147483647-1,-1,112,113,2147483647};
 for(unsigned i=0;i<sizeof(unsupported)/sizeof(unsupported[0]);++i) {
  Bytes effect=header(0x12,0x3871d784);u32(effect,202);u32(effect,0);u32(effect,unsupported[i]);f32(effect,.5f);
  Archive::ByteStream body=stream(effect);Archive::ReadIterator input=body.begin();DestroyShipComponentMessage decoded(input);
  int native=77;check(input.getSize()==0);
  check(!PrecuShipSlots::destructionSlot(true,decoded.getChassisSlot(),native)&&native==77);
  check(PrecuShipSlots::destructionSlot(false,decoded.getChassisSlot(),native)&&native==unsupported[i]);
 }
 // A rejected effect cannot poison translation of the following valid event.
 int restored=0;check(PrecuShipSlots::destructionSlot(true,12,restored)&&restored==15);''')
for p,s in changes.items():
 t=p.with_name(p.name+'.g7tmp');t.write_text(s,encoding='utf-8');t.replace(p)
print('Added bounded destruction-effect slot guard and production-linked regression cases; strict replication validation unchanged.')
