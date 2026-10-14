# Standalone static switch-texture visual

Round29 Rendering, 2026-10-03. This opt-in adapter joins the accepted [frame bundle](SWITCH_TEXTURE_FRAME_BUNDLE_CONTRACT.md), [modern cursor](DELTA_TIME_TEXTURE_CURSOR_CONTRACT.md), and [owned static visual](SCENE_STATIC_VISUAL_CONTRACT.md). It changes neither the default preview nor world/factory/pump publication. Original scheduler, x87 parity, effects, and first-visible semantics are not implemented by this join.

## API and ownership

```csharp
new SceneStaticSwitchTextureVisual(SceneStaticVisual inner,
    SceneStaticResources source, SwitchTextureFrameBundle bundle,
    int maximumCatchUpSteps=4096);
bool Advance(float suppliedSeconds);
bool Reset();
bool Tick();
bool VerifySelected(bool gpu);
DeltaTimeTextureCursorSnapshot Snapshot { get; }
bool CursorsSynchronized { get; }
ulong SelectedTextureId { get; }
int UniqueUploads { get; }
int ImageCopies { get; }
long UploadedBytes { get; }
bool IsRetired { get; }
void Dispose();
```

The wrapper borrows the exact live terminal visual and immutable bundle. It owns two synchronized cursors, eight unique-path CPU image copies, and its GPU cache. It does not own the inner visual, registry, archive, asynchronous load, or world lifetime. Every call requires its creating managed thread and the prototype host main thread. The caller must serialize access and supply a non-reentrant lifetime predicate through the inner visual. The standalone preview owns and drains its worker, closes its registry, and disposes wrapper before inner visuals.

Normal disposal restores the inner visual's original frame0 binding before releasing cached textures and cursors. It is idempotent. A destroyed/generation-stale inner visual retires itself; wrapper Tick/Advance then disposes its cache and returns false. Dispose does not close a borrowed registry or bundle. Unexpected binding/commit failures retire the wrapper and inner visual, allowing resources to be released without promising GPU rollback.

The internal binding seam uses an exclusive owner token: `AcquireTerminalBinding`, `TryBindTerminalTexture`, and `ReleaseTerminalBinding`. It is enabled only when the visual was explicitly constructed under the existing exact terminal frame0 inspection policy. A second wrapper or unsupported visual cannot claim it. Binding changes the existing shared unshaded screen material's albedo texture, touching exactly its two screen surfaces; no geometry/material object is replaced. Owner/lifetime checks occur before the binding and again at the final gate.

## Input validation and upload boundary

Before GPU allocation, ValidateInputs checks the complete original terminal frame0 descriptor, CRC2907007746, four LODs, two accepted SWTS screen surfaces, shader path/pin, base source identity, raw DTST words, eight ordered frames, MAIN replacement correspondence, and the full immutable bundle layout. The exact shader SHA is `B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62`; count8 and interval bits3DCCCCCD are required. Frame images are128x64 RGBA, eight mips, no retained terminal bytes. Source/bundle frame0 decoded bytes must agree before the exclusive binding is acquired. Invalid work limits also reject before GPU allocation.

Each unique frame path is copied via CopyImage once before the first upload. Eight images total349,536 RGBA bytes/64mips. A bounded path cache uploads each image once; repeated authored paths share an upload, while distinct paths retain distinct GPU IDs even for equal bytes. The exact pinned original shader has eight distinct paths. A separately labeled synthetic alias/equal-byte-distinct-path fixture tests the same upload helper without widening the original terminal policy.

This standalone cache deliberately uploads its own frame0, in addition to the inner visual's seven retained static textures. It therefore has15 textures across inner plus wrapper, rather than claiming a deduplicated scene union. The standalone preloads the complete static closure (27reads/1,401,794source bytes) and the frame-input bundle separately (10reads). Those37lookups are deliberately separate transactions; main Assets29 owns any later union/accounting optimization.

No Advance/Reset operation performs an archive lookup, image copy, upload, mesh/material allocation, or cache rebuild. Cursor operations allocate their accepted immutable receipts. Diagnostic GPU readback and fingerprints allocate temporary images/byte buffers; those operations are separate from playback updates.

## Time and failure atomicity

There is no automatic `_Process` clock. The caller explicitly supplies finite nonnegative float32 seconds and interprets the original float32 interval in matching seconds. The standalone fixture uses authored interval-sized synthetic steps, zero, and a17-step multiple wrap. It bypasses the original1,000-tick scheduler described in the [clock contract](SWITCH_TEXTURE_CLOCK_CONTRACT.md). Double-intermediate/float32-persistent arithmetic is explicitly modern; original x87 precision, scheduler quantization, partition identity and first-visible behavior remain unclaimed.

Both cursors begin identical. Advance/Reset first stages the inactive cursor using the accepted core API. Validation/budget/counter errors occur before engine mutation; the active receipt and current texture remain unchanged. After another lifetime gate, the adapter binds the cached candidate texture, swaps active/staged cursors, and repeats the same operation on the former active cursor to restore synchronization. Success requires matching index, residual bits, revision and cumulative advances.

If lifetime expires after staging, the active receipt remains unchanged and resources retire. If an engine setter fails or the second cursor commit unexpectedly fails (including immutable-receipt allocation failure), the adapter retires itself and the inner visual. This is retirement, not an asserted rollback across the engine boundary. No failed call is reported as success. The fixture injects an actual disposed-material binding failure after staging and verifies retirement, alongside preclaim byte mismatch and late-stale gates.

## Separate verification oracles

Existing SceneStaticVisual.Verify/Fingerprint are unchanged: both continue to observe the selected texture. At wrapper frame0, Verify succeeds because the separately uploaded bytes match; Fingerprint differs because that upload has a different ID. After normal wrapper disposal, the original fingerprint is restored. Verify intentionally does not accept a nonzero selected frame as the original frame0 closure.

New StructureFingerprint excludes only the selected screen texture, retaining root/node/mesh/material IDs and every other texture identity, shader identity, and optional mip bytes. VerifyGeometry independently checks uploaded original geometry. VerifySelected checks the exact selected binding and, in GPU mode, dimensions, format, all eight mip bytes and cursor synchronization. These new proofs do not relax the existing default oracle.

The standalone preview (external historical reference) verifies all8selected CPU/GPU chains against the independent Round28 Python BC decoder/overlay oracle,32LOD selections, retained pose/geometry/material identities, and another hidden static visual's unchanged full fingerprint. It tests invalid/nonfinite/negative/work-overflow deltas, zero/noop,17-step wrapping, reset, owner rejection, exclusive claim, preclaim failure followed by valid construction, destroy/generation retirement, late-stale staging, actual disposed-material engine failure and idempotent cleanup. Synthetic failure inputs are labeled in the fixture. The other actor is hidden to avoid overlapping capture geometry.

## Reproduction and results

Use the cached prototype host4.7.2.NET x64 and offline NuGet feed. Build the existing prototype host project with a private NuGetLockFilePath and private APPDATA/package directory; the checked-in lock pins are left unchanged. Then run Invoke-SwitchVisual.ps1 (external historical reference) headlessly and with `-Gpu -CapturePrefix <fresh-private-PNG-prefix>`. Each session is hidden and capped at45seconds. Generate a private independent frame oracle with the accepted Round28 probe (external historical reference) (`probe <archive-root> <configuration> <private-output-json>`), and run Verify-SwitchVisual.py (external historical reference) with both logs, oracle, and capture prefix.

Only the exact root-certificate-store diagnostic and its exact prototype host source line may appear once before the first frame record. All other warnings/errors and any diagnostic after frame records fail verification. No original executable or server is contacted. Captures0/3/7 and all proprietary-derived receipts remain private.

Build, headless/GPU runs,64independent mip hashes in each mode, and default static headless/GPU/cancellation regressions passed. The default strict policy/fingerprint behavior remained intact. Detailed executed commands, failure history and audit are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R29_STATIC_SWITCH_TEXTURE_VISUAL.md).

Next: lead joins this explicit wrapper to an opt-in host path using accepted scene-union resources, retaining the default frame0 behavior and readiness gates. Live playback clocks, scheduler emulation, original effect translation, visibility-first ordering and generalized animated shaders remain outside this milestone.
