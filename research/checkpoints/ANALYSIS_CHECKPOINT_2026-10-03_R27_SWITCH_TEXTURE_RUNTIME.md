# Round27 SWTS runtime evidence

Date: October 3, 2026. Bounded question: What do the terminal SWTS/DTST two raw words mean, what is initial selector state, and who updates/owns it? Stopping condition: direct original static reader/runtime/lifecycle chain, real asset correlation, or explicit unresolved scheduler boundary. No original execution, live contact, downloads, or implementation changes.

## Inputs and method

Verified original executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` before VAs. Round27 snapshot manifest `75b3e15dd2e77bd5b3a8d98787dd47383c69b0e57529c64c39f0edf4d27cbe53`, ZIP `f45e0b56654da4dba5c3bf19497c2f9b47d712a1e9c5d9ec5a145ce14ddf9f5c`, 2,637 unique members fully size/hash/containment/symlink validated before overlay and postverified. Snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bde9735c`; isolated worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`, empty index.

Read START/latest26, status/ledger/UNKNOWN, original switch-resource, SWSH and EFCT checkpoints, existing registry and static tooling. Used bundled Python with existing cached pefile/Capstone and `find_vtables_by_rtti.py`, `disasm_range.py`; reused existing private switch-shader decompilation as a lead and checked original instructions. All disassembly, probe output, original shader bytes, and detailed receipts remain ignored. No third-party analogous implementation was used as client proof.

## Findings and confidence

The [durable contract](../../docs/SWITCH_TEXTURE_RUNTIME_CONTRACT.md) records exact VA chains, ownership, observed instructions, and compatibility consequences. Confirmed: SWTS registration/factory; DTST class RTTI and two-word reader; signed frame count and float interval; initial index/accumulator zero; callback-driven repeated interval subtraction with wraparound; template-owned variants and per-wrapper owned selector protected by retained parent; selected-index forwarding. The pinned real terminal has count8, float bits3DCCCCCD, and eight ordered MAIN texture references. Its first query before update selects `_01.dds`.

Unknown: external clock units, pause/culling/thread policy, time between construction/first update/draw, full original malformed-IFF acceptance, and original shader visual equations. Do not promote approximate0.1 to seconds/10Hz, count equality to a universal parser rule, or frame zero to the first actual live rendered frame. Static nonpositive-interval lack of progress is a compatibility risk, not an execution-based hang result.

The prior SWSH checkpoint misattributes `0x00785C30/50` indexed forwarding to SWSH: they are in the RTTI-backed SWTS vtable. Corresponding SWSH forwarding is `0x007860A0/C0`; SWSH update is `0x00786080`. Both wrapper families still use selectors. Lead should reconcile this exact address correction and preserve prior historical interpretation; worker did not edit canonical registry/UNKNOWN or old checkpoint.

## Proposed stable names for lead reconciliation

These are proposals, not registered aliases. Check existing address rows before adoption and retain any earlier name/history.

| VA | Proposed name | Evidence |
| --- | --- | --- |
| `0x0077EBC0` | `shader_swts_register_VA_0077EBC0` | SWTS tag/factory/registry call |
| `0x0077ECD0` | `shader_swts_template_factory_VA_0077ECD0` | 36-byte allocation, constructor call |
| `0x0077ED30` | `shader_swts_template_construct_VA_0077ED30` | RTTI-backed vtable and load |
| `0x0077EEC0` | `shader_swts_version_dispatch_VA_0077EEC0` | Outer0000 branch |
| `0x0077EF30` | `shader_swts_load_v0000_VA_0077EF30` | Base/selector/ordered variants |
| `0x0077EDE0` | `shader_swts_template_destroy_VA_0077EDE0` | Selector/variant/base release |
| `0x00B5A0C0` | `shader_switcher_template_dispatch_VA_00B5A0C0` | DTST and other discriminators |
| `0x00B5A3F0` | `shader_dtst_template_read_VA_00B5A3F0` | DTST0000 two raw reads |
| `0x00B5A4F0` | `shader_dtst_runtime_create_VA_00B5A4F0` | Interval/count argument transfer |
| `0x00B5ACA0` | `shader_dtst_runtime_construct_VA_00B5ACA0` | RTTI, count/interval, zero state |
| `0x00B5AD20` | `shader_dtst_runtime_update_VA_00B5AD20` | Float accumulator/catch-up/wrap |
| `0x00B5AD80` | `shader_dtst_current_index_VA_00B5AD80` | +16 accessor |
| `0x00B5AD90` | `shader_dtst_frame_count_VA_00B5AD90` | +12 accessor |
| `0x00785B00` | `shader_swts_runtime_construct_VA_00785B00` | Owned selector/borrowed vector |
| `0x00785B90` | `shader_swts_runtime_destroy_VA_00785B90` | Selector deletion/base release |
| `0x00785C10` | `shader_swts_runtime_update_VA_00785C10` | Selector update virtual call |
| `0x00785C30` | `shader_swts_forward_selected_operation_a_VA_00785C30` | Index and selected+14 forwarding |
| `0x00785C50` | `shader_swts_forward_selected_operation_b_VA_00785C50` | Index and selected+18 forwarding |

Existing canonical names are `SwitchShader_forwardSelectedOperationA_VA_00785C30` and `SwitchShader_forwardSelectedOperationB_VA_00785C50`. Preserve those former interpretations in the reconciliation history: the structural forwarding evidence remains valid, while RTTI places the functions in SwitchTextureShader. The proposed replacements retain generic operation A/B because exact original public method names are Unknown. Class attribution and selected-child dispatch are Confirmed. No worker registry rename was performed.

## Validation and precise resume

Own static verifier (external historical reference) passes original hash gating, six RTTI identities, 17 disassembly ranges, and pinned475-byte terminal grammar/DTST/eight textures. Synthetic wrong-executable and wrong-shader negatives reject without a receipt. Workspace links/private-link scan, Python syntax, whitespace/EOF, focused diff check, baseline hash/scope/HEAD/index audit complete. Only two assigned documents and the own verifier were added; all2,637 baseline files remain byte-identical. No historical implementation path (omitted) implementation tests are warranted because implementation is unchanged.

Next probe: trace the caller of RTTI-backed primitive update slot2 at `0x0077E2C0` upward through ShaderPrimitiveSet/appearance frame scheduling, identify the origin/unit of its float argument and first-update/draw order, then document pause/culling/thread behavior before a bounded selector implementation. Keep direct SWTS and SWSH vtable attribution separate. Lead should reconcile proposed canonical addresses and the old SWSH citation. Worker stops at this milestone.

Lead reconciliation: adopted18 proposed canonical names,16 new addresses, and preserved former two SwitchShader names in UNKNOWN/history. Current registry2618/zero duplicates; no coverage percentage change. Main independent pinned static verifier passes17 ranges/six RTTI identities/terminal bytes. Outer scheduler remains the precise next probe.

## Numerical interpretation correction recorded in Round28

The historical local-instruction observation remains. Use of x87 registers alone did not establish extended intermediate precision. [Pinned outer clock/control-word evidence](../../docs/SWITCH_TEXTURE_CLOCK_CONTRACT.md) finds reachable PC00/RC00 configuration, selecting24-bit significand and round-to-nearest; actual callback word and alternate contexts remain unobserved. The living runtime contract now uses this bounded statement. Initial query zero still does not establish first visible frame.
