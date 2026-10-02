# R12-RENDER-01: world observer presentation and palette choices

Date: 2026-10-02. Question: can the offline inspector apply accepted R11 coalesced world state before acknowledgement, independently guard resource/state lifetime, and switch strictly prepared palette factors without extra source reads or GPU replacement? Stop condition: reproducible positive and negative state/source/pixel evidence plus scoped handoff. No original executable execution or live networking was used.

## Inputs and tools

Accepted baseline: 2,077 public files, HEAD `65de8c88141071744dc75594be126b3bab3696a5`. Baseline ZIP SHA-256 `21b81fe530b5e5d6e056014fe6d56c85e2be73a5e195fbc67f021f1c3ceae335`; manifest SHA-256 `4bb617a17cde0ae3cd13ebddb06afa2141ed29d35938028689113440b9cf2766`. Every archive member and resulting baseline file was hash-verified before edits. Scope is measured against that manifest, not the intentionally dirty HEAD tree.

Static original input: `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Accepted catalog and human-locomotion profiles; palette `human-scene-colors.json` SHA-256 `72BE9FC64AEA1A0EB15DAA2B43E31CBD272BB01CBA61E78C5F190E04D3556726`. Tools: existing .NET 8.0.300, Godot 4.7.2 Mono, cached Python/Pillow, source-cleared offline NuGet restore. No downloads or executable-address claims.

## Evidence and interpretation

The canonical behavior and reproduction interface are in [the preview contract](../../docs/OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md). [Native tests](../../poc/round12-tests/Rendering/Program.cs) independently hand-frame the 496-byte peer and use the accepted scalar world API. Four observed batches have mutation revisions 5/6/7/8 and replication revisions 2/3/4/5, preserving object incarnation and handle. The [world oracle](../../poc/round12-tests/Rendering/Verify-World.py) derives these from the synthetic transcript and checks actual GPU/headless/native output. Apply precedes acknowledgement; held stale acknowledgement preserves one pending ID and a retry consumes latest state. Resource restart retires presentation independently of mutation delivery. Selection/failure/close retire globally; destruction/recreation rejects the prior incarnation. No callbacks or batch history are retained. Confidence: **Confirmed** for the bounded consumer and fixture.

Wrong-thread drain/apply/ack all reject before metadata changes. A discovered ordering issue was corrected and rechecked with prior acknowledgement still usable. Unsupported negative-zero Height stays valid raw world state but cannot alter the accepted transform or acknowledge its pending batch; a valid update retries successfully. Multi-actor presentation is sequential rather than atomic. Confidence: **Confirmed**; consequence: callers must retain pending work after rejection.

The strict profile is parsed/applied on the resource worker before publication, from defensively owned bytes. The [original oracle](../../poc/round12-tests/Rendering/Verify-Original.py) independently reads the original 39-source overlay, five inherited templates, two programs, four factors, ten textures/80 mips, palette indices and flags. Both authored/profile five-entry palette selections agree with original records. Shared closure checks reject changed Create identity before material writes. GPU factor switching uses existing materials; source fingerprints and GPU IDs remain fixed. Confidence: **Confirmed** for this profile/closure; arbitrary forged descriptor graphs are not authenticated by reference checks.

Final captures: Height changes 28,119 actor pixels, widths 132→204, heights 237→367, warm pixels 10,569/25,209. Palette choice changes 25,184 pixels at identical extent `(549,50,753,417)` in the crop below UI row 180. All three whole-actor images were visually inspected. Confidence: **Confirmed** for replacement behavior; modern Height multiplier and authored shader preview do not establish original Height units, posture, lighting or render fidelity.

## Validation and handoff

Godot build passes with zero warnings/errors. Native nineteen negatives, strict malformed profile bytes and range/domain/cancellation cases, foreign/stale/state/resource/thread/lifecycle/drain/ack checks, GPU/headless profile runs, cancellation, default-profile headless regression and eight prelaunch failures pass. Independent source/palette and wire/state/pixel oracles pass. Root-certificate-store startup diagnostic is nonfatal; no positive-run implementation exception was observed.

Private logs, captures, focused baseline diff and hash receipts remain under ignored `local-private/round12/`. Public test files are independently authored and contain no proprietary source corpus. Only the five dispatched existing files and this checkpoint plus seven owned test files are changed against baseline. Shared Core, Main.cs, SceneCreatureVisual, registries/status/ledgers and R12 Runtime/Assets work are untouched.

Remaining uncertainty: original Height semantics, full-face and wire customization, live behavior and exact original rendering. Next action: lead integrate the bounded handoff and run its own acceptance. Worker stops idle after Round 12; no Round 13 investigation is authorized.
