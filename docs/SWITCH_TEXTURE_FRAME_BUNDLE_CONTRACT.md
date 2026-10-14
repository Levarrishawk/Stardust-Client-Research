# Switch-texture frame input bundle

[SwitchTextureFrameBundle](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SwitchTextureFrameBundle.cs) owns every authored SWTS frame DDS input. It is independent of world, engine and cursor state. It reuses the accepted SWTS decoder, `StaticShaderReferences`, DDS decoder and owned source cache; no existing loader policy changes.

```csharp
var bundle = SwitchTextureFrameBundle.Load(resolve, shaderPath, shaderSha256,
    limits: new SwitchTextureFrameBundleLimits(), cancellationToken: token);
```

The callback is `Func<string, TreLookupResult>`. The shader path/pin identifies the original SWTS, not a derived material. The result exposes ordered `Frames` (index/tag/path/immutable image), ordinal unique-path `Images`, immutable `Sources`, raw DTST words, exact `FrameCount`/float32 `Interval`, and source/decoded/mip counters. Base SSHT metadata remains available through `CopyBaseDescription`; `CopySwitchDescription` also returns an independent owned description.

## Eligibility and path preflight

The [accepted runtime evidence](SWITCH_TEXTURE_RUNTIME_CONTRACT.md) confirms signed DTST count, float interval and local selection/update behavior. This bundle requires positive signed count, exact count/reference equality and finite positive interval. These are **stricter modern eligibility rules**: the original local reader/update does not establish those rejection checks, and the original selector count and variant vector length are independent. The two raw words remain unchanged, including interval bits. A positive subnormal interval is eligible here; no clock steps are performed.

SWTS/0000 NAME, exact DTST/0000 eight-byte record and ordered1..256 TEXT records use the existing decoder. Full raw base/frame paths are then validated before base resource lookup, preventing accepted legacy normalization from silently changing their identity. Paths must already be lowercase canonical ASCII beneath `shader/` with `.sht` or `texture/` with `.dds`, at most1,024 characters. Components use lowercase letters, digits, underscore and hyphen; only the final extension dot is allowed. Leading/trailing/doubled slashes, traversal, backslash, colon, NUL, spaces, high/uppercase characters and other dots reject. Terminated names are at most1,025 bytes including NUL.

The full distinct required-path set is preflighted against source-count budget after reading SWTS and before base lookup. The base must decode as the accepted external-reference SSHT profile, and every authored frame must match exactly one named source-flag-zero base texture binding, using the accepted Select validator. All named base texture paths are checked in raw form. Base parameters/other metadata are retained as source metadata; other bindings and effect resources are not loaded or interpreted as a complete material.

## Exact read and alias policy

The source union is only the pinned SWTS, its actual base SSHT and unique authored frame DDS paths. No implicit overwritten base default or effect read occurs. Each present path is read once through the owned path cache. Repeated authored paths retain repeated ordered frames but share one immutable image, source and decoded/mip charge. Distinct paths retain separate image/source identities and charges even when bytes match. This deliberately uses path identity, not content-hash deduplication.

Missing, masked, malformed, wrong-pin or later budget input rejects the entire bundle; there is no neutral/fallback partial material. Source identities report actual effective priority, bytes and SHA-256. Only the SWTS has a required API pin; the original gate independently pins all ten inputs. The result does not select an initial visible frame, implement a timer, infer clock units or prove original shader equations/material/world closure.

## Limits and accounting

| Bundle limit | Default | Allowed range |
|---|---:|---:|
| MaximumFrames | 256 | 1..256 |
| MaximumSources | 258 | 1..258 |
| MaximumSourceBytes | 134,217,728 | 1..268,435,456 |
| MaximumDecodedBytes | 134,217,728 | 0..536,870,912 |
| MaximumMips | 4,096 | 0..4,096 |
| MaximumShaderBytes | 1,048,576 | 1..1,048,576 |

`DdsLimits` defaults to the existing `DdsReadLimits`; this bundle caps dimension1..32,768, input128..67,108,864 bytes and per-DDS decoded4..268,435,456 bytes. The explicit legacy terminal-tail option is preserved. Invalid settings throw `ArgumentOutOfRangeException` before reads; pin syntax uses existing catalog argument validation. Other profile/input failures throw `InvalidDataException`, except the cache's established aggregate source `AppearanceGraphBudgetException` (IOException). Callback exceptions propagate unchanged.

Per-source shader/DDS caps are checked before cache copying. Aggregate source budget is enforced by the cache. Before decoding each unique DDS, its declared mip count is checked against remaining aggregate mips, and remaining decoded bytes narrow the per-DDS decode budget. DDS structure/dimensions/format/extents remain the accepted decoder's responsibility. Retained terminal-block bytes are added to decoded charge and checked before immutable image retention. `SourceBytes` sums all unique input bytes; `DecodedBytes` sums unique RGBA mip bytes plus retained terminal bytes; `MipCount` sums unique decoded mips. Aliases add no second charge. These are logical quantities, not peak CLR memory estimates.

## Ownership and lifecycle

Returned images/mips expose private arrays only through clone accessors. `CopyRgba`, `CopyRetainedTerminalBlock` and `CopyImage` cannot modify retained pixels. Copied existing DDS/IFF descriptions may expose their own memory-backed arrays; mutating those arrays cannot modify the bundle. Frame/image/source/mip collections are read-only, and aliased frames share the canonical immutable image.

Callbacks are not retained. The cache snapshots borrowed source arrays before later callbacks can reuse them. Failed loads publish no bundle and preserve earlier results. Cancellation is polled around callbacks, between frames and before return; bounded synchronous parser/decode/copy operations do not poll internally. Same-thread recursive Load poisons the outer call even if swallowed, with finally recovery. Independent threads may load bundles. Callback side effects/concurrent source mutation are outside rollback/detection.

## Original validation and next boundary

The original mission SWTS/base/eight DDS union is10 sources,89,350 bytes. Every frame is BC2,128x64 at mip0, eight mips and43,692 decoded RGBA bytes. Total64 mips /349,536 decoded bytes. DTST words are8 and`0x3DCCCCCD`; all frame tags are MAIN. Every source pin and every mip hash agrees with an independent configured-overlay/IFF/Pillow BC oracle. Default/effect reads are zero. [Checkpoint identities and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R28_SWITCH_FRAME_BUNDLE.md).



| Frame | Exact authored path | Mip0 dimensions | Mips | RGBA mip bytes |
|---:|---|---:|---:|---:|
| 0 | `texture/anim_mission_terminal_01.dds` | 128x64 | 8 | 43,692 |
| 1 | `texture/anim_mission_terminal_02.dds` | 128x64 | 8 | 43,692 |
| 2 | `texture/anim_mission_terminal_03.dds` | 128x64 | 8 | 43,692 |
| 3 | `texture/anim_mission_terminal_04.dds` | 128x64 | 8 | 43,692 |
| 4 | `texture/anim_mission_terminal_05.dds` | 128x64 | 8 | 43,692 |
| 5 | `texture/anim_mission_terminal_06.dds` | 128x64 | 8 | 43,692 |
| 6 | `texture/anim_mission_terminal_07.dds` | 128x64 | 8 | 43,692 |
| 7 | `texture/anim_mission_terminal_08.dds` | 128x64 | 8 | 43,692 |

Each row is computed from its independently decoded mip records, rather than inferred from the aggregate. Each chain is 128x64, 64x32, 32x16, 16x8, 8x4, 4x2, 2x1, 1x1; byte counts are 32,768 + 8,192 + 2,048 + 512 + 128 + 32 + 8 + 4 = 43,692. No terminal bytes are retained. The unchanged source identities and current native/independent per-mip agreement establish these sizes.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SwitchTextureFrameBundleScenarios.cs) and [independent fixtures/oracle](../../SWG-Godot-Client/poc/round28-tests/Assets/verify_frame_bundle.py) pass210 fixtures (eight positive/202 rejected),14 controlled checks and32 concurrent loads. Exact/one-less budgets, aliases/equal-byte distinct paths, count/interval/string/whole-path failures, late missing/masked/malformed data, retained-tail charge, clone immutability, cancellation and reentry are covered. Existing DDS and selected-switch material regressions/oracle pass.

Next join reviewed frame inputs with an independently validated cursor and a bounded engine adapter. This bundle itself introduces no animation or world-ready transition; original outer clock/x87/rendering parity remains a separate evidence boundary.
