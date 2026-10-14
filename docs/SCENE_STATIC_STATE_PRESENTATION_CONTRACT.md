# Supplied static state presentation contract

Round26, October 3, 2026. This is a bounded modern presentation policy. It does not establish a TANO baseline/delta codec, typed world store, original caption layout, font, locale, or visibility semantics.

## Inputs and ownership

SceneWorldStaticStateVisual (external historical reference) accepts the existing world, an owned `ISceneWorldObjectPresentation`, its borrowed `SceneWorldObjectVisual` base, a pure `Func<SceneWorldObjectSnapshot, SceneStaticPresentationProjection?>` source, and a pure `Func<CreatureView3NameState,string>` resolver. The immutable supplied projection contains `SceneObjectIncarnation Incarnation`, `ulong Revision`, owned immutable `CreatureView3NameState Names`, and raw `byte Visibility`. The default caption limit is 256 UTF-16 units; configured limits are 1 through 1024. `createLabel=false` preserves state policy without creating a label.

Constructor success transfers the inner presentation and optional label. Failure leaves the inner caller-owned and frees any allocated candidate label. World, base, and callbacks are borrowed. Inner and base must identify the exact same current publication, which must contain a live static visual and no creature visual. The baseline world snapshot must be opaque with no typed replication state. No existing factory or creature wrapper changes are required.

The label is a child of the static root, positioned above its existing bounds. The preview uses prototype host's default font, a gold billboard caption, and a diagnostic camera. These choices are modern inspection policy. Raw visibility zero hides the existing root and inherited caption; every nonzero value, including 255, shows them. No transform, source resource, mesh, texture, material, topology, or LOD operation is performed by the wrapper.

## Freshness and failure policy

Revision zero and wrong incarnation are invalid. Revisions may repeat only when incarnation, raw visibility, and exact Names reference are unchanged; smaller revisions fail. A greater revision may retain the same Names reference, which avoids resolver work. Equal newly allocated names are distinct cache identities. Names remain raw owned byte strings, spacer, and UTF-16 units; display output does not modify them.

Each Tick and valid Apply calls the inner operation exactly once. Stale Apply is rejected before the inner call. Current publication and snapshot identity include opaque envelope references. Source reads are guarded and repeated before and after publication; a changing result fails. Source and resolver must be pure and may be called more than once. Guard checks include world journal/current snapshot, root transform/visibility, caught nested reentry, and label parent/native lifetime. Callback side effects are detected where covered but never rolled back. This is not a sandbox for arbitrary callback I/O or unrelated native mutations.

Null projection, malformed caption, stale/reused revision, callback exception/mutation/reentry, lost label, or refresh failure records LastFailure, retires owned label/inner, and propagates the exception. Normal world generation/destroy retirement returns false. Dispose is idempotent. Retired wrappers retain publication metadata, freeze the inner drain task after disposal, and drop callbacks/world/base/current model. A held drain remains incomplete until its owner completes it. Calls require the creating prototype host main thread; rejected foreign-thread calls leave cleanup to the owner.

## Reproduction and evidence

Standalone preview (external historical reference) loads `object/tangible/terminal/shared_terminal_mission.iff` once through the existing configured overlay/resource router, using the accepted original static route and explicit pinned SWTS frame-zero MAIN/UV0 inspection policy. It obtains 27 unique sources totaling 1,401,794 bytes, four meshes, five materials, and seven decoded textures. Original effect/timing parity remains unknown. Configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

Generate-Peer.py (external historical reference) independently supplies the opaque terminal world fixture plus synthetic names from complete CREO-model baseline fixtures. The latter are explicitly supplied model values; they are never decoded as TANO or accepted into a typed tangible world. The source maps StringId diagnostic/alpha to FIRST TERMINAL and custom units to LATEST CAPTION. This is not STF resolution.

Rendering.csproj (external historical reference) compiles the actual projection record/validation from the prototype host source under `STARDUST_PROJECTION_ONLY`, without prototype host wrapper dependencies. Scenario (external historical reference) checks 31 assertions, including raw 0/1/2/255, retained Names identity, and monotonic revision rejection. It also runs 21 existing name assertions, 226 container race assertions, and the existing CPU composition queue gate. Launch.ps1 (external historical reference) runs only the replacement preview, hidden and bounded to 45 seconds. Supply cached prototype hostPath, ArchiveRoot, Configuration, FixturePath; choose generation or destroy retirement, and optionally a fresh private CapturePath stem for four PNG siblings.

Verify.py (external historical reference) takes exported evidence JSON, peer JSON, log, and optional capture stem. It independently parses synthetic raw names, verifies visibility/revision/call counts, exact source identities, four original mesh geometry hashes, all seven DDS mip hashes, and GPU readback for the four bound MAIN textures. Readback does not claim every non-MAIN shader input is rendered. Pixel checks isolate the caption above row 180, require identical body, hidden background, and restored latest frame.

Confirmed by executed tests: generation and destroy cleanup, 18 constructor/late failure probes, optional label, owner-thread rejection, stale Apply before inner, exact retained identities/pose, one archive load, label/inner disposal once, and held drain propagation. GPU comparison changed 8,816 caption pixels while preserving 46,964 body pixels. Both build and independent oracle passed. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_STATIC_STATE_PRESENTATION.md).

## Next boundary

Lead must adapt an accepted tangible-store snapshot to the supplied projection and separately validate typed world codec/store transactions. No production registration, world integration, STF preload join, live-server compatibility, or original executable VA claim is made here. This implementation uses existing durable specifications and original asset parsers; it does not rely on a newly analyzed executable address or execute the original client.
