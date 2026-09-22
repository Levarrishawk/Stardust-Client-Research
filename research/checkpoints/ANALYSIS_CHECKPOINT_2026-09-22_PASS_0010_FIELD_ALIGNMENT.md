# Analysis checkpoint: `PASS/0010` field alignment

Date: 2026-09-22  
Scope: all 108 installed raw `PASS/0010` DATA records, compared with executable-supported `PASS/0009`

## Result

The `PASS/0010` extension is now structurally aligned. Its 57-byte DATA chunk is the complete 56-byte `PASS/0009` layout with one new U8/Boolean inserted at byte offset `0x04`. Bytes `0x00..0x03` retain their `0009` positions; each former `0009` byte at `0x04..0x37` moves to `0010` offset `0x05..0x38`.

This eliminates the prior risk that the extra byte might be an unknown suffix or variable payload and provides a safe forward decoder. Its historical semantic/render-state name is not asserted.

## Versioned layout

```text
PASS/0009 DATA:
    bytes 00..03   existing prefix fields
    bytes 04..37   remaining existing fields

PASS/0010 DATA:
    bytes 00..03   same existing prefix fields
    byte  04       new Boolean state_0010_04
    bytes 05..38   exact former 0009 bytes 04..37
```

The existing `PASS/0009` decoder reads its fields explicitly, so the replacement should add an explicit `0010` reader that consumes the inserted byte and then reuses the semantic mapping of the shifted suffix. It must not copy the payload into a packed host structure.

## Corpus distribution

Across all 108 raw `PASS/0010` records:

- `state_0010_04 = 0`: 104 passes;
- `state_0010_04 = 1`: four passes.

The four true cases are:

- two passes in `shader/ui_bar_glow.sht`;
- one pass in `shader/wter_lava_02.sht`;
- one pass in `shader/wter_lava_02_still.sht`.

Names alone are insufficient to label the state. The clustering makes it a visually meaningful candidate for targeted validation, but calling it glow, lava, distortion, emissive, or any specific DX state without a consumer is not justified.

Full child-program inspection narrows the role further:

- both true `ui_bar_glow` passes use programmable emissive-add masked-color
  pixel programs whose names explicitly end in `bloom` (`ps20` and `ps11`);
- the false fallback pass in the same shader uses fixed-function `PFFP/STAG`;
- the true lava pass uses `3d_lava_heat_render.psh`;
- the other lava passes, including normal lava color/emissive programs, are
  false.

This is strong evidence that the flag marks a programmable auxiliary
post-process contribution/write (bloom or heat), rather than an ordinary blend,
depth or texture state. The exact historical API name and render-target packing
remain unknown, so the IR name should stay provisional, e.g.
`auxiliaryPostProcessWrite`.

## Reachability context

None of these three true-flag shader paths appears in the existing literal effective-reference table. That does not prove they are dead: UI/style and environment systems can select resources through indirection or composed names. The eight `PASS/0010` shader paths with confirmed literal asset referrers all use false for the inserted state in the observed passes, so their initial visual compatibility can use the `0009` suffix semantics plus a preserved false field.

## Replacement contract

```cpp
struct LegacyPassDescription {
    // existing renderer-neutral PASS/0009 fields
    bool pass0010State04 = false;
};
```

- Decode the field only for version 0010+.
- Preserve it in typed IR and any diagnostic/round-trip representation.
- Treat false as the established behavior for directly referenced affected shader families.
- Use conspicuous targeted fixtures for the three true-flag shaders.
- Do not assign a DX12/Vulkan state until either a later compatible consumer or controlled visual comparison establishes its effect.

## Confidence

- insertion offset and suffix alignment: **Confirmed by all 108 payloads**;
- Boolean domain/distribution: **Confirmed**;
- three true-flag asset identities: **Confirmed**;
- auxiliary post-process write role: **Strongly inferred from all true cases**;
- exact historical name and target/channel semantics: **Unknown**.
