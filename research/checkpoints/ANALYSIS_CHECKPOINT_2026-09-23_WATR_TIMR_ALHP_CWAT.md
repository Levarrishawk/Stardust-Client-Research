# Watcher, timer, hardpoint alignment, and clone-weapon actions (overnight block 16)

Four remaining lower-frequency action types have direct registrations and effective assets:

| Tag and template | Effective asset | INFO layout | Observed behavior |
| --- | --- | --- | --- |
| `WATR/0000` `WatcherActionTemplate` (`0x00402560`) | `fire_turret_react.pst` | Empty | Runtime constructor `0x0040AC00` uses vtable `0x015DC650`; instruction trace at `0x0040AB00` returns true exactly while watcher at `+8` contains a non-null referenced object. It is a lifetime gate, not water rendering. |
| `TIMR/0000` `TimerPlaybackActionTemplate` (`0x004027E0`) | `wait_play_1.pst` | One four-byte tag `EMIT` | Parser `0x00402A70`; update `0x0040AD30` looks up a float via `0x00407FD0`, accumulates frame time at runtime `+0x10`, and continues while elapsed is below the value. Negative/missing lookup behavior needs a fixture. |
| `ALHP/0000` `AlignToHardpointActionTemplate` (`0x006009A0`) | `change_posture_align.pst` | Two 16-bit actor selectors `0/1`, string tag `TNPH` | Parser `0x00600C30`; runtime update `0x006FC2A0` resolves both actors, reads a hardpoint-name string via `0x00407D10`, gets hardpoint transform from target appearance, combines world transforms, adjusts source object transform, and has a `RemoteCreatureController`-specific branch. |
| `CWAT/0000` `CloneWeaponActionTemplate` (`0x005FF830`) | three throw scripts | One 16-bit actor selector `0`, tags `TOBF`/`TNPH` | Parser `0x005FFB90`; template update `0x005FF910` resolves an original weapon/held object or a string-specified template, calls object clone/create helpers, and, when actor appearance is `SkeletalAppearance2`, attaches a cloned alternate-wearable record using the hardpoint/name tag. |

`WATR` is the playback tag for a **watcher**, not the identically spelled water render phase tag. Its update was not defined as a Ghidra function but was confirmed by x86 disassembly `0x0040AB00..0x0040AB18`. `TIMR` is a local playback timer; it is not network time synchronization. `ALHP` manipulates visual/object transform state; the remote-controller path deserves further analysis before assuming all actors are treated identically. `CWAT` does not yet establish the precise clone ownership/destruction handoff; this is relevant to the subsequent grenade/sword cleanup actions.

Confidence: **confirmed** for registrations, version-0 binary layouts, watcher predicate, elapsed-time comparison, hardpoint transform composition, and skeletal alternate-wearable call. **Strongly inferred** for clone/held-weapon role from template name plus attachment path. **Unknown**: missing-value edges and ownership/cross-action lifetime.
