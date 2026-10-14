# Round 19 supplied face inspection — 2026-10-02

Question: does the supplied human face profile generate and bind the exact head slot-0 MAIN texture while preserving other materials and resource identities through locomotion and Height changes? Stop after the standalone preview, independent oracle, cancellation proof, and bounded handoff. The durable result is the [inspection contract](../../docs/SCENE_CREATURE_FACE_INSPECTION_CONTRACT.md).

## Inputs and method

Authoritative snapshot: HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, 2,346 files. Snapshot manifest SHA-256 `6c642fef4824b7b07cd596e2be2fe1041f6286a505dc4a64a356938b3453e047`; ZIP SHA-256 `4add5c5a3869686632288e4cb259406ee50174a44f3a9c715fdb8a87eda93893`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Overlay members were preflighted and copied with exact size/hash checks. No executable addresses were used; this is an asset/implementation inspection, not additional executable research.

Original input is the read-only installed overlay rooted at `C:/Stardust-DEV`, configuration `swgemu_live.cfg`, SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Profile and output identities, budget semantics, camera, and reproducible commands are specified in the durable contract. Tools: existing .NET 8 SDK, prototype host 4.7.2 Mono, bundled Python with NumPy/Pillow, baseline descriptor/builder/visual verification APIs. No original executable execution, server contact, downloads, or publication occurred.

## Observations and interpretation

- **Confirmed:** default loading reads 39 sources / 4,686,371 bytes; generated loading reads 76 / 5,940,390, within 128 / 32 MiB encoded limits. All generated sources are exported only to an ignored private destination. Decoded acceptance is checked before publication under a separate 64 MiB policy.
- **Confirmed:** raw head TRTS parsing agrees with the generated binding: `hum_m_head_l0.mgn`, slot 0 MAIN, face shader/renderer. Missing profile key, extra profile key, and wrong destination tag each fail before publication.
- **Confirmed:** Python independently decodes original source bytes and reproduces all five cumulative layer hashes and 65,536 output pixels / 262,144 RGBA bytes. Final digest: `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808`.
- **Confirmed:** GPU verification checks actual texture resource binding and readback bytes. Five inspected surface identities remain stable across eight walk/run frame selections, Height 1.4, and reset. Factor records remain equal between default and generated descriptors.
- **Confirmed:** fixed-frame PNG comparison changes 1,967 pixels, bounds `(614,148)-(662,203)`. Body and sampled upper-cap pixels are identical. **Interpretation:** the generated face changes the intended bounded visible region under this camera and supplied profile.
- **Confirmed:** cancellation publishes zero roots, closes one archive, and drains the entered worker. Successful teardown balances two allocations/disposals and two archive closes.

Implementation consequence: retain the baseline generated-face binding and ownership route in a production adapter; do not apply a global tint or replace all head/body materials. Keep supplied values explicit, distinguish encoded and decoded budgets, and drain canceled workers before declaring completion. These results do not recover default customization values or establish legacy driver parity.

## Validation and remaining questions

Build succeeds without warnings/errors. Headless and GPU previews pass; the independent composition/binding/pixel oracle passes; the separate cancellation preview passes. Existing prototype host certificate-store startup diagnostic is nonfatal. Source changes are restricted to the standalone preview and own round-19 runner; no shared navigation, Core, status, registry, or ledger edits are part of this worker milestone. Baseline workspace/scope checks are recorded in the final handoff.

Next probe: the lead can join this inspection evidence with the separately implemented production resource-reader policy and verify its opt-in route using the same supplied profile and negative cases. Other profile values, shutdown under forced termination, and decoder peak-memory accounting remain outside this milestone. No research/readiness/function/protocol completion denominator changes are claimed.
