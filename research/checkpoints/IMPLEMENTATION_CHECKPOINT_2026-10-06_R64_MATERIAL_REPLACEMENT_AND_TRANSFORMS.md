# Round64 supplied terrain material, playback replacement and full transforms

Investigation and implementation started October5 Eastern; UTC records are October6,2026. Bounded question: can the accepted retained terrain/current-plan host replace its actual owned playback, consume one explicitly pinned SSHT/MAIN resource and preserve full directional transform bytes without losing earlier geometry, source or cleanup guarantees? Stop after independent worker replay and one bounded joined headless/GPU trace plus relevant rejection/regression gates. No original executable execution or live contact.

## Inputs and tools

Frozen4049 public files: manifest `fa32e144c1db8c65a735c2c230ef1e34ece292ce21edf7b057af5aacc85420a6`, ZIP `d1e6ead66694bf94452382eab0953bb8c98924abf1d0b30e4a5a8c69e031d719`. Original executable SHA-256 remains `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration `swgemu_live.cfg` SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` and original human template SHA `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243` remain pinned.

The terrain fixture is the sealed accepted sparse first-curve modern evaluation of the one original Tatooine TGEN/SGRP occurrence. Generator SHA `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`; SGRP1977 bytes SHA `C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628`. No new terrain corpus is surveyed. The independent runtime recipes remain sealed accepted62 inputs.

Cached Python/static PE tools, .NET8, offline cached packages and prototype host4.7.2.NET perform static original analysis and replacement CPU/headless/GPU execution. Original archive roots are read-only. Detailed fixtures, receipts, capture and hashes remain ignored. Each worker owns exact new paths; lead owns shared host/canonical integration. No staging, commit, push, download, packet capture or publication.

## Four independently reviewed milestones

### Second SFAM string

[Contract and byte verifier](../../docs/TERRAIN_SHADER_FAMILY_SECOND_STRING_CONTRACT.md): setter00B69390 copies the second string including NUL into family+0x14 and clears/releases a previous pointer; getter00B69370 returns that stored pointer. Selected31-byte accessor00B6B480 calls unresolved00B69860, uses EAX as array index and returns the selected getter result. **Confirmed** byte/pointer flow; allocation/release roles **Strongly inferred**; selector validity, lifetime teardown, resource meaning and runtime reachability **Unknown**. A raw direct-rel32 scan finding zero accessor callers does not prove it unreachable. No open/fetch boundary is proved.

Main independent replay passes56 instructions,42 operand anchors,15 original byte-preservation witnesses,111 negative guards and12 independent copy/pointer checks. Six distinct leads and six raw sites stay under the separate12 caps. No resolver or canonical semantic names are invented. Next: bound the complete getter caller containing00B6AE0F/00B6AE18 and stop at its first unresolved forward/resource boundary.

### Explicit supplied terrain material

[Engine-neutral closure](../../docs/TERRAIN_SUPPLIED_STATIC_MATERIAL_CONTRACT.md) uses the existing SSHT/DDS decoders with an exact caller shader pin, one named static MAIN binding and only two reads. It retains source identity and metadata, owns decoded mip bytes, and exports detached images. Modern texture-times-vertex-color is explicit; optional authored MAIN diffuse uses white when absent. Effects, unrelated textures, original shader execution and original terrain association remain outside this implementation.

Main replay passes82 cases:14 accepted,68 expected rejections and248 ownership checks. The reused original diagnostic shader `shader/defaultemis.sht` is171 bytes at priority38, SHA `BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7`. Its MAIN `texture/defaulttexture.dds` is22000 bytes at priority53, SHA `FFC82EDF27C4CDC26DF680EF7C7D74E9B5EC6C1A685E44A0E0B5DD491F6A981C`; decoded BC2 output is128x128, eight mips,87380 bytes. All eight mip hashes match the independent Pillow reference. This existing terminal-base diagnostic has **no established terrain association**. Encoded sources total22171 bytes; effect/unrelated texture reads zero.

### Owned replacement slot

[Slot contract](../../docs/SCENE_WORLD_TERRAIN_PLAYBACK_SLOT_CONTRACT.md) delegates actual frozen63 Bind and advancement, borrows fixed exact world/store, and owns the installed binding. Invalid or unavailable candidates preserve the old reference/revision; successful replacement retires old children before exposing a new revision. A synchronous temporary two-binding construction window is explicit. Bind can already publish borrowed motion before a later failure; no rollback or peak-allocation guarantee is claimed. Rare post-return cleanup, revision overflow and reentry branches are source-inspected where no real public injection exists.

Main independent replay passes three precision traces, five successful replacements each,36 numeric step comparisons,306 native checks and75 negative/lifecycle checks. It covers completed-plan replacement, changed surface height, authoritative correction, source/store retirement, actual child identity/disposal and borrowed survival. Original collision, units, movement, physics and gameplay parity remain **Unknown**.

### Directional full transform frames

[Codec contract](../../docs/OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md) implements the complete62/70-byte ObjectController0x71/0xF1 frames with explicit direction, fixed little-endian fields, raw counter/object/parent/flags and quaternion X/Y/Z/W, position X/Z/Y and signed speed. Finite F32 admission is modern policy; zero/non-unit finite quaternion and negative speed remain raw. Exact raw F32 bits, including signed zero, are retained; no normalization, world application or SOE framing occurs.

**Correction / Confirmed current-server observation:** `MMOCoreORB/src/server/zone/packets/object/transform/Transform.h:15` declares U32 `timeStamp`; parsers at48/58 use `parseInt` for outer0x16. This is separate from executable outer AutoVariable<float> storage. Lead amended the initially symmetric F32 assignment to outbound U32 timestamp and inbound F32 scalar. Timestamp words with float NaN/Infinity bit patterns must remain admissible. Original sender conversion/units are not established by server parsing. Shared protocol/packet/schema/coverage records are reconciled; the matrix still contains the same documented345 rows.

Main independent handpacked Python/native replay passes32 positive frames and4000 malformed checks. Direction-safe factories/accessors and detached encoded buffers are covered. No captured traffic, live session or authoritative world effect is claimed.

## Lead host consumption

New material adapter (external historical reference) loads the actual engine-neutral closure once from the original configured overlay before scene pumping and closes its own overlay before its ready marker. Its two reads are accounted separately from the unchanged three scene archive jobs. After all native terrain admission, it uploads one ImageTexture shared by16 existing patch adapters. Every adapter retains original geometry, base UV, index order, supplied vertex color/normal and the one native-Z reflection; it owns its mesh/shader/material and borrows the shared texture. Material uniforms and exact mip upload/download bytes are checked. Disposal releases all patch borrowers before the one texture. Modern opaque lit RGB, repeat/nearest base-UV sampling and DDS-sRGB hint translation are supplied host policy; original effect/sampler/alpha/lighting parity and rendered-pixel shader arithmetic are unproved.

Version5 explicitly selects the actual replacement slot adapter (external historical reference). At global step30 it checks an invalid999us step and foreign-composition source preserve the old binding/revision, builds the remaining30 steps from the original paired reader and previous stored speed, replaces at the exact current clock, and observes old driver/playback disposal and old receipt retirement. The new receipt/counters start locally at zero. No source reload, terrain rebuild or scene asset reload occurs. Final inactive stop and owned slot retirement precede borrowed source/store/terrain teardown.

Both headless and GPU runs pass independent replacement (external historical reference), material (external historical reference), and full-frame (external historical reference) peers:60 grounded positions/transitions,400 heights/normals,85 surface queries (81 hits/four misses),104 rays (98 hits/six misses) and one actual camera hit remain valid. The host trace stays within one patch/chunk; seam coverage comes from separate accepted geometry/native suites. The slot peer checks raw segmented counters and actual render-policy labels first, then projects only those labels/indices for the established independent full-trace numerical oracle. It does not seed expected numeric values from native output.

The offline transform adapter projects actual native driver pose into a caller-selected quaternion and emits240 frames:120 root and120 synthetic parent envelopes across both directions. Sixty parent outbound words deliberately use0x7FC00000 as an integer timestamp probe. Parent ID zero, flags and microsecond-to-millisecond mapping are explicit diagnostic inputs; no sender timing/parent-coordinate policy is recovered, and no frame is sent or applied. The independent peer reconstructs all positional/speed words from the sealed arithmetic/terrain model, permits either sign of mathematical quaternion zero, and compares full bytes. Worker fixtures separately prove exact signed-zero preservation.

The GPU capture was inspected: human and terminal remain visible on a patterned green diagnostic field. It proves host presentation only; it is not original terrain material fidelity. historical implementation path (omitted) builds have zero final warnings/errors; default smoke reports18 events/zero failures.

## Rejection and retained regressions

Joined rejection peer (external historical reference) confirms received-NaN and late-direction failures publish no slot/owned playback/new playback motion, drain16 terrain visuals, one shared texture, the light, actors and all three archive jobs. A wrong shader pin rejects before terrain/GPU/scene job allocation, with zero jobs and actors. Prior Version4 direct terrain playback and Version3 planar current playback pass their unchanged independent peers.

Corrections retained in private logs: initial lead build had two nullable-reference errors, fixed before execution; the old geometry oracle required the old render label, corrected by explicit raw-policy validation plus geometry-only projection; a prior-mode wrapper initially lacked its private current-nominal fixture, then replayed after copying the sealed input. Networking UTF-8 mojibake was corrected by its worker before final hash/replay. None is reported as an original behavior failure.

## Remaining uncertainty and next action

Original terrain family resource mapping, material blending/effect execution, collision/terrain arithmetic, locomotion policy, source units and full-transform world application remain separate open questions. Next round should trace the concrete second-string caller, connect full root correction to typed world motion, and add a bounded terrain ray/input consumer or original family material binding only after supporting evidence. Do not repeat this accepted suite without a changed boundary or concrete risk. Lead closes final scope/link/HEAD/index records before the next dispatch.

## Final acceptance

Accepted2026-10-06 03:15:28UTC.61 scoped changes (38 additions/23 existing),4087 public git-listed files,29 final worker hashes,1674 Markdown files/zero broken or private links,2626 function rows/zero duplicates and345 packet rows. Scoped syntax/UTF-8/whitespace/project XML/diff checks pass. HEAD/index and unrelated/private bytes are preserved. Timing and exact next scope are in the [round session](../sessions/PARALLEL_DEVELOPMENT_ROUND_64_2026-10-06.md). Final records are serialized after the gate timestamp.
