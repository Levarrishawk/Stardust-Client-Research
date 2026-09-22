# One-Hour Analysis Session: ASHT Policy and Music Runtime

Date: 2026-09-12  
Requested duration: one hour, with the active unit completed before stopping  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

This session completed three connected units rather than leaving a partial trace:

1. ASHT link/action selection and physical-track interruption policy.
2. `PlayMusicMessage` wire-to-audio routing, including server-source correlation.
3. `GameMusicManager` global and per-environment asset contracts, state transitions, channel layering, and category-13 content reachability.

No replacement-client implementation or repository product code was created. All analysis artifacts remained in the ignored local staging workspace for publication to `D:\Development\Experimental-Client`.

## Principal findings

### Animation state links and actions

- ASHT links do not expose an RTTI-backed predicate subclass family in this executable. They store a target `AnimationStatePath` and optional logical transition animation.
- Version 0/1 links use a signed-16 path count and legacy animation reference/index; version 2 stores the logical animation name directly.
- Link lookup returns the first stored path match. During a hierarchy transition the first chosen link replaces the physical track; later link animations queue, all with `0.15` transition time.
- Direct actions replace rather than queue. `EditableBasicAnimationAction +0x29` is confirmed `useAddTrack`.
- Movement actions choose between `movementAction` and `noMovementAction` using exact nonzero `loco_vel` components.

### PlayMusicMessage

- Server and client agree on ASCII16 path, U64 target, 32-bit selector, and trailing byte after CRC `0x04270D8A`.
- The listener uses only path and selector. Zero selects handler `0x006C9300`; nonzero selects `0x006C93B0`. Target and trailing byte are parsed but unused here.
- Both handlers fade/stop their prior held AudioId with scalar `1.0`, play the new path through the ordinary `.snd` pipeline, and retain the new AudioId.
- Selector zero is a temporary override of the state-driven music holder. Local `SpaceDeath` uses it; the manager resumes normal selection after playback ends.
- All current Stardust server construction sites retain default selector `1`, so server traffic uses the separate nonzero event channel.

### Music assets and runtime policy

- `scene/game_music_manager.iff` is `GMUS/0000` with vector-backed ENTR, EXIT, DETH, INCP, SENT, SEXI and singleton WATR chunks.
- Ground combat, death/incapacitation, space combat, and day/night transition predicates map to a confirmed nine-way state table.
- Space entry randomly chooses a SENT index and space exit uses the corresponding SEXI index, falling back to element zero if the arrays mismatch.
- `datatables/environment/<planet>.iff` directly names the seven per-environment audio fields: two day ambient sounds, two night ambient sounds, first music, sunrise music, and sunset music.
- The manager owns separate ambient layers 1/2, principal state music, and server-event audio.
- First music is selected once per newly encountered outdoor environment-family or interior override object; the client retains separate visited-object sets rather than replaying it every update.
- The outdoor/interior predicate is containment-based: it resolves the object's cell-property owner and compares it with the global world cell. Outdoor families provide two ambient layers; an interior override supplies ambient layer 1 and first music while suppressing the second default layer.
- GMUS WATR is parsed and released but has no playback/reference site in this executable; it is inactive legacy configuration in this build.

### Category 13

- A scan of 203,617 effective TRE winners found only two direct category-13 path references: `sound/han_94.snd` and `sound/han_95.snd` in `quest/npe_new_main_quest.qst`, both `musicOnActivate` values.
- Stardust.exe has no `.qst` format string and constructs runtime quest-task paths as `datatables/questtask/<name>.iff`. No compiled NPE quest-task table or matching Han reference exists in the installed overlay.
- These are strongly editor/source-artifact references, not demonstrated runtime use. Category 13 can still be named dynamically by a server or referenced non-textually, so it is not declared unreachable.
- `PlayMusicMessage` uses the ordinary sound runtime and provides no category-13 gain bypass. A new client must model category 13 safely instead of copying the original out-of-bounds category-table read.

## Evidence artifacts

- `ANALYSIS_CHECKPOINT_2026-09-12_ASHT_LINK_ACTION_POLICY.md`
- `ANALYSIS_CHECKPOINT_2026-09-12_PLAY_MUSIC_AUDIO_ROUTE.md`
- `ANALYSIS_CHECKPOINT_2026-09-12_GAME_MUSIC_MANAGER.md`
- updated `PACKET_HANDLER_MAP.md`
- updated `PLATFORM_INPUT_AUDIO_ARCHITECTURE.md`
- updated `STARDUST_STATIC_ANALYSIS_LOG.md`
- `category13_sound_references.csv`
- `all_sound_references.csv`
- reusable TRE reference/name/context scanners added to the local tool set

## Confidence

- PlayMusic wire order, selector branch, held channels, and normal audio submission: **Confirmed**.
- GMUS tag/storage mapping and nine-way selection actions: **Confirmed**.
- Environment datatable field-to-runtime-slot mapping: **Confirmed**.
- Day/night polarity and sunrise/sunset transition states: **Confirmed behaviorally**; original method name provisional.
- `.qst` records as non-runtime source artifacts for this executable: **Strongly inferred**.
- Intended category-13 mixer gain: **Unknown**.

## Progress estimate

- Expanded comprehensive whole-client analysis: **68%**.
- Replacement-relevant implementation/readiness research: **88%**.

The one-point movement in each measure is conservative. This block closed an important animation policy correction, one fully implementation-grade server message route, and the data-driven music subsystem; it did not expand the long-tail UI/gameplay handler inventory enough to justify a larger increase.

## Recommended next block

Continue audio/environment closure at the boundary immediately adjacent to this work:

1. identify the exact class/schema supplying the smaller interior-cell audio override object (its containment predicate and precedence are now known);
2. correlate weather-table sound fields with their separate runtime start/stop transitions; GMUS WATR itself is now proven unused in this build;
3. then return to the next high-value named-message or replication-handler family.

This preserves continuity while avoiding over-investment in original Miles implementation details that do not constrain the replacement architecture.
