# Round19 customization ingress - October2,2026

## Question and stopping condition

What exact outer representation and client callback/storage boundary carry inherited CREO:3/TANO:3 field4, and what proven bridge reaches the shader/blueprint variableName+factoryFlag values? Stop at the proven chain and precise remaining typed-value probe; do not create a speculative inner codec or reanalyze closed manager tables.

## Inputs and identity

Round19 public manifest has2,346 files, SHA-256 `6c642fef4824b7b07cd596e2be2fe1041f6286a505dc4a64a356938b3453e047`; ZIP `4add5c5a3869686632288e4cb259406ee50174a44f3a9c715fdb8a87eda93893`. Exact unique member set, size/hash, path containment and absence of archive/destination symlinks were verified before writes; all written members verified afterward. Snapshot HEAD1fc55dd89cd23ce3af49e3fe471210bdebe9735c is authoritative separately from preserved checkout HEAD65de8c88141071744dc75594be126b3bab3696a5. Ignored/unlisted files and empty index preserved.

Before relying on VAs, C:/Stardust-DEV/Stardust.exe SHA-256 was checked as `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static instructions only, no original execution. Existing docs/checkpoints, curated names/matrix, current baseline/mixed/component codecs and prior synthetic fixtures were inspected. No new original assets were extracted or player/account values inspected.

Server correlation HEAD9a8529ef8291efc5ca1b41d9927f0e88d64b79bb, with exact file SHA-256:

| Path below local Stardust-3 | SHA-256 |
| --- | --- |
| MMOCoreORB/src/server/zone/objects/scene/variables/CustomizationVariables.cpp | 1526ae6a6f658f0f6cff081fd24ab0039164c1fa9bd68baa01bcfcc656febcc7 |
| MMOCoreORB/src/templates/customization/CustomizationIdManager.cpp | 80943cc53faee7a27266317543133fec99bcef070b8be910e5ec1647d80d3aeb |
| MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectMessage3.h | 7bab8834c50b2bca12a93eaa274a35d05e6a4429e01cd03f87ab6f6c053edd7e |
| MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectDeltaMessage3.h | c7883c45e68863d8addbbc197389d798a7c43fec0453d3d5327d79ed8383ed58 |
| MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage3.h | 37ed8b3748c02e6e7254f68f0094196849982cdad1dd898fd71423e14b80f90e |
| MMOCoreORB/utils/engine3/MMOEngine/src/system/net/Packet.h | afa34f57495f5021472b7b2f33fdc3b3fd4d384e9722690dca85dcf921d8ca11 |

## Evidence and findings

The [durable contract](../../docs/CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) contains exact observations, confidence and compatibility consequences without raw decompiler output. Key new connection:00641030/00641180 byte comparison invokes0063F880; nonempty callback resolves associated data and calls00B33020, then00B33780 and owner virtual+3C.00B33020 unescapes and calls00B33E30, whose version1 path calls00B33F70. The record reader masks ID&7F, chooses1/2-byte width, calls CIDM resolver00B54D50 then named-variable lookup00B32890 and setter vtable+10 at00B3418C. This chain is Confirmed static evidence; semantic property/data naming is Strongly inferred.

The decoder-owned outer byte string is distinct from packed records. Current CREO baseline preserves exact bytes and extended prefix choice; generic envelopes preserve TANO/delta opaque bytes; typed scalar/mixed/component delta parsers do not apply field4. Confirmed by source and synthetic execution. Compatibility consequence: do not imply a replicated customization bridge from inspection-profile APIs or opaque retention.

Original record setters are called during iteration; no final decoded-tail check appears on success. Empty callback returns without data application. Missing live variable skips its value after diagnosis. ID/sign high bit proves width, while typed setter conversion/clamping remains unresolved. Server parser's transactional clearing/final EOS check and signed S16 interpretation are correlation, not proof of identical client failure semantics. Consequence: stricter atomic malformed rejection must be labeled replacement policy; do not silently promote an earlier recommendation into recovered original behavior.

Existing CIDM evidence supplies121 installed ID/full-name rows and ACST typed variable construction; existing shader/blueprint instructions supply true=/private/,false=/shared_owner/ consumer prefix. Proven identity bridge is ID to exact full name to live variable to exact consumer prefix/name match. FactoryFlag is independent of the wire high bit. ID118 spelling anomaly must remain verbatim. Full typed integer conversion/getter/mount behavior and original player values remain Unknown.

## Reproduction

Static tool dependencies already exist in main ignored toolchain. Initial worktree invocation lacked pefile; setting PYTHONPATH to the existing package directory resolved it without install/download. Use the same pinned executable and bundled Python:

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONPATH = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages'
$env:PYTHONUTF8 = '1'
Get-FileHash C:/Stardust-DEV/Stardust.exe -Algorithm SHA256
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00641030 0x00641300
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x0063F880 0x0063F8D0
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B33020 0x00B332C9
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B33E30 0x00B342DF
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B54D50 0x00B54F30
& $python tools/python/disasm_range.py C:/Stardust-DEV/Stardust.exe 0x00B32890 0x00B328C9
```

Keep detailed disassembly ignored/private; commands print only for bounded local inspection. No proprietary corpus is promoted. Existing manager/shared-customization checkpoints remain the resource/namespace evidence, avoiding repeated extraction.

Standalone commands are in the durable contract. A fixture-authoring layout error (extra U32 before visibility) was corrected before final tests; it was not a production change. Six exact positive cases and947 malformed/budget/unsupported probes pass; Python independently verifies lengths, bytes and receipts. Tests retain even invalid inner bytes deliberately, proving outer preservation without claiming inner execution. They exercise owned copies and exact opaque TANO/delta round trips, not a typed TANO codec. Runner build has zero warnings/errors.

Workspace/link validator and git diff --check passed; exactly five allowed new files, no changes among2,346 baseline bytes. Index remains empty and checkout HEAD unchanged. Shared codecs/options/frozen face policy/Program/projects/navigation/ledgers/registries untouched. No Git mutation, original execution, live contact, captures, download or publication. Research89%/readiness94%,2602 names and345 matrix rows unchanged.

## Handoff and exact next probe

New paths: docs/CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md; this checkpoint; poc/round19-tests/Networking/Networking.csproj, Program.cs, peer.py. The next static task is resolve the concrete ranged-int/palette variable vtable+10 targets at00B3418C, trace1/2-byte conversion, bounds/failure and getter+1C, and verify a compact independently authored value set. Then a separate bounded implementation can add strict atomic inner decode and current-incarnation field4 delta state. This does not block explicit supplied face inspection. Worker stops after this one milestone; lead owns shared reconciliation.
