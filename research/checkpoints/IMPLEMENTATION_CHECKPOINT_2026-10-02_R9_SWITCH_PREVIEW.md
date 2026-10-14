# R9-RENDER-01: retained original static terminal frames

Date: October2,2026. Question: can accepted explicit SWTS frame0/1 complete the original static object pipeline and display a visible terminal-screen difference in prototype host while manual LOD/frame selection preserves GPU resources and geometry? Stop condition: original/native/independent audit, fixed-camera/UI-excluded GPU capture pair, headless/cancel/negative/teardown and exact-scope handoff pass.

## Inputs and orientation

Read AGENTS/START_HERE, accepted [Round8](../sessions/PARALLEL_DEVELOPMENT_ROUND_8_2026-10-02.md), static/SWTS contracts, relevant prior checkpoints/tools and current ledgers. Historical no-Round9 statements are superseded by the new explicit Rounds9–12 authorization. This assignment covers Round9 only. No executable addresses were used.

Accepted baseline: HEAD `65de8c88141071744dc75594be126b3bab3696a5`,1,963 files; ZIP SHA `da266e684b8318aa605b0ef8730331915f7ba803bdccb2b693a2402ad9017dd8`; manifest SHA `29bdfd7c8bac3898aa219d68d0b9189b518c7f7934a1b1c1e248fddf494a4b56`. Verified safe relative paths and every file hash before/after synchronization; ignored/unlisted files preserved.

Original configured `C:/Stardust-DEV` terminal, object/appearance/SWTS pins and frame selection are in the [durable contract](../../docs/STATIC_SWITCH_TEXTURE_PREVIEW_CONTRACT.md). Tools: .NET8.0.300, prototype host4.7.2 .NET, Python/Pillow, PowerShell; compatibility OpenGL/NVIDIA RTX4070 SUPER. No toolchain downloads or lock edits.

## Findings, interpretation and consequence

Confirmed original-byte/CPU observation: each selected frame loads four inherited objects through APT/DTLA and all four material meshes,27 read-once sources /1,401,794 bytes,seven textures/62 mips. Geometry hashes and vertex/triangle counts agree across frames; original selected DDS and mip0 pixels differ. Independent Python reconstructs the inheritance/appearance route, validates every source identity/priority and original geometry/mip output. Borrowed resolver buffers can be cleared without changing owned native outputs.

Confirmed modern GPU observation: two fixed adapters preload14 textures/8 mesh instances.64 manual frame/LOD selections retain resource IDs, uploaded geometry and mip data. Four invalid selections preserve the fingerprint. Seven native choice/pin/index negatives reject with zero invalid-preflight reads. Real first-lookup cancellation closes archives and publishes no GPU geometry. Eight launcher negatives, including a preexisting second capture, reject before launch. Teardown frees native roots and disposes adapters/overrides; the worker is observed.

The accepted opaque adapter rejects the known `a_emis_full.eft` on two SWTS surfaces per frame. Exact diagnostic-set checks reject any other gap. A lead-approved bounded inspection uses one built-in unshaded MAIN material per frame on only the exact pinned SWTS surfaces, reusing existing uploaded textures and owning/disposal of overrides. The UI labels the policy. No existing adapter or shader code changes. This is not original emissive/blend fidelity or complete effect support; CPU completeness and inspection rendering remain separate measures.

Confirmed capture: finest serialized LOD3 and fixed camera/environment/resources show the full terminal. Both GPU and independent PNG comparison find6,934 changed pixels below the UI, bounded by `[615,316,717,393)` within the visible screen. Source/decoded-pixel difference is independently checked separately. Identical rendered images reject despite otherwise valid exports. Label changes cannot satisfy a row180..719 comparison. Original timing/initial-frame semantics remain Unknown; frame and LOD choices are modern policy.

Initial build errors came from the wrong namespace for the existing geometry hash helper; corrected to `Stardust.Core.Proof`. Initial headless checks exposed the adapter's expected opaque-effect restriction. The scoped built-in policy resolved inspection without broadening opaque support or changing shaders. Failed/intermediate logs are excluded from final receipts. Final builds have zero warnings/errors. Known prototype host certificate-store diagnostics persist in passing logs.

## Reproduction and final evidence

Use the exact build/launch commands in the durable contract. Final private paths relative to checkout:

- Native: `local-private/round9/native.txt`.
- GPU: `local-private/round9/switch-textures/preview-341457d4c54141c29337a7847d34b622.log`.
- Headless: `local-private/round9/switch-textures/preview-195c76075a1a4056afdca5b835f0057c.log`.
- Cancellation: `local-private/round9/switch-textures/preview-d65aa8fea43b480ab762f0b98b3012e7.log`.
- Captures: `local-private/round9/terminal-current.png` and `local-private/round9/terminal-current-frame1.png`.
- Independent audit: `local-private/round9/oracle.json`; option results `local-private/round9/options.txt`; identical-pair rejection `local-private/round9/identical-capture-rejection.txt`.

Run the own Python verifier with root/configuration plus those native/GPU/headless/cancel/PNG paths in order. Replacing the second PNG argument with the first must exit nonzero. These private filenames are reproduction arguments, not links or curated asset exports.

## Scope and handoff

Nine additions: two prototype host files, launcher, contract/checkpoint, four own diagnostic files. The only existing edit is Main.cs's single byte-preserving dispatch branch. Removing it reproduces baseline bytes. Shared readers/adapters/shaders/projects/runners/status/ledgers/curated registries remain unchanged. New public files use UTF8/LF; generated logs/exports/images remain ignored. All1,963 baseline hashes audited; whitespace/workspace links validated; index empty and HEAD unchanged. Private changed-path manifest and receipt contain exact per-file/evidence hashes.

No main checkout edit, worker messaging, staging, commit, push, publication, downloads, original executable execution, socket/DNS/server contact, packet capture or fuzzing occurred. The image pair is a replacement viewport screenshot. No timer, live placement, original shader equation or research coverage claim follows.

Exact next action: lead review/acceptance of R9-RENDER-01, then an explicit Round10 assignment. Stop/idle after this one milestone. Any future automatic SWTS scheduler investigation must establish the raw DTST words and executable ownership separately after executable identity verification.
