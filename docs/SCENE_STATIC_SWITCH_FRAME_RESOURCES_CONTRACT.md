# Explicit all-frame static scene resources

`SceneStaticResourceReader.Load` appends optional `SceneStaticSwitchFramePolicy? switchFrames = null`. `SceneStaticResourceOptions` appends the same optional constructor argument and exposes `SwitchFrames`. Existing constructors/calls and the default selected-frame route remain valid. `SceneStaticResources.SwitchFrames` is an optional immutable [frame bundle](SWITCH_TEXTURE_FRAME_BUNDLE_CONTRACT.md).

```csharp
var options = new SceneStaticResourceOptions(
    SceneStaticMaterialPolicy.RequireComplete, explicitPinnedSelection,
    switchFrames: new SceneStaticSwitchFramePolicy());
var resources = SceneObjectResourceRouter.Load(create, catalog, options, resolve, token);
```

## Policy and load boundary

`SceneStaticSwitchFramePolicy` holds optional `SwitchTextureFrameBundleLimits Limits`, `MaximumRetainedMips` (default4,096; range1..8,192) and `MaximumPendingWeight` (default268,435,456; range1..536,870,912). Bundle limits retain their accepted ranges. Options snapshot the immutable records. The route requires complete material policy and an explicit pinned selected SWTS; the requested shader must actually occur in the appearance. Selection may be any valid authored index. No implicit SWTS discovery or fallback occurs.

The existing profile/geometry/material load runs first through its unchanged outer `OwnedAssetSourceCache`. Modern positive signed count/reference equality and finite positive interval, all raw authored paths and base named paths, every selected tag/source-flag binding, and the full unique source-count union are preflighted. Additional authored DDS paths are read through that same outer cache. Cached shader/base/selected DDS are not externally read again. Every unique path is read at most once. The full encoded union is reserved before bundle decoding. Missing/masked/bad/pin/cap/callback/cancellation failures publish no completion.

Per-source caps and encoded bundle/pending growth caps are checked around additional callbacks before source-cache copying. Existing cached sources are checked against the frame-input caps before new reads. Remaining global decoded bytes and retained mips narrow bundle limits before each accepted bundle decoder's bounded work. Pending weight reserves all encoded bytes plus appearance pixels before narrowing the frame decoded budget. These are logical retained-input limits, not peak CLR/GPU memory estimates; geometry, descriptors, metadata, transient decoder/copy storage and allocator overhead remain excluded.

A same-thread recursive static load poisons the outer call even when the callback swallows the exception, with finally recovery. Independent threads remain supported. Cancellation runs before/after callbacks, between resource requests and before completion return; bounded synchronous parsing/hash/copy work does not poll internally. External callback side effects and concurrent mutations are outside rollback. No archive work or frame decoding is added to ticks.

## Retained storage and pure completion checks

The appearance's selected `DdsImage` and the bundle's immutable selected frame pixels are **separate retained representations**. They both count toward global decoded bytes/mips/textures. Source-path bytes count once. Repeated authored frame paths share one bundle image/charge; equal-byte distinct paths stay separate. Default appearance accounting is unchanged.

`SceneObjectResourceAccountingReceipt` adds get-only `SwitchFrames`, `SwitchFrameTextures`, `SwitchFrameMips`, `SwitchFrameDecodedBytes`. Overall `DecodedTextures`, `Mips`, `DecodedTextureBytes` and `PendingWeight` include the bundle representation; default new counters are zero. `PendingWeight` remains checked source+decoded+generated-face bytes. Measurement uses immutable lengths and passive metadata, without source I/O, decoding, pixel scanning or clone accessors.

Completion validation requires exact option/bundle presence, its private load association, policy/selected index, pinned shader/base/source records, exact source/image union, raw count/interval words, every ordered tag/path, canonical image aliases, mip/dimension/byte caps and global retained counters. The loader's private association captures immutable selected pixel and source-metadata hashes. Validation hashes existing appearance spans and compares them with this proof; it does not copy bundle pixels or call its clone accessors. Corresponding material bindings/base metadata and SWTS metadata must agree. Work and payload extents are bounded before hash traversal. Public record `with` copies do not bypass create/catalog/source/appearance/options checks, and selected backing-array mutation rejects. A swapped bundle requires its own loader association. Validation cannot independently attest archive origin from metadata; production trusts accepted readers, with source/pixel provenance verified separately by the oracle.

## Original evidence and validation

| Route | Unique sources | Source bytes | Retained texture representations | Retained mips | Decoded bytes | Pending weight |
|---|---:|---:|---:|---:|---:|---:|
| Default terminal frame0 | 27 | 1,401,794 | 7 | 62 | 2,140,836 | 3,542,630 |
| Explicit all-frame terminal | 34 | 1,479,410 | 15 | 126 | 2,490,372 | 3,969,782 |

The bundle contributes8 unique images/64 mips/349,536 bytes. Frame0 remains128x64/eight mips/43,692 bytes in each separate retained representation. Seven additional paths are authored frames02..08, each11,088 encoded bytes. Source identities are unchanged from [pinned frame evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R28_SWITCH_FRAME_BUNDLE.md). No overwritten default DDS is read; the existing complete material effect closure is retained.

Native scenarios (external historical reference) and independent oracle (external historical reference) verify exact source union/storage, all64 frame mip hashes, default accounting, aliases/equal-byte identities, exact/one-less caps, intermediate growth, malformed/count/interval/missing/masked/late failures, record association/binding/raw-word faults, mutable selected pixels, borrowed cleanup, cancellation/reentry and eight concurrent loads. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R29_STATIC_SWITCH_FRAME_RESOURCES.md) records precise counts and commands. These are Confirmed modern implementation observations; original animation/render/effect parity and world publication are separate milestones.
