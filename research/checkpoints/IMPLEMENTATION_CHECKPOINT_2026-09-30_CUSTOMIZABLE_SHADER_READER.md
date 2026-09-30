# Customizable shader reader — 2026-09-30

## Bounded question and inputs

What CSHD structure is actually deployed, and can an owned parser preserve its texture/palette controls without conflating inline values and applied state? Closed for deployed version 0001 structure; runtime customization remains separate.

Original executable SHA rechecked earlier this checkpoint chain: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,48 mounts. Independent effective shader scan yields5,173 CSHD0001 winners and pinned member-list SHA `9B2FEE105ABF0CABC41E7A79DADE3526BF612D888A41712A8B49C331F274576A`. Original bytes, detailed inventories and instruction ranges remain private.

## Observation, confidence and consequences

**Confirmed:** inline SSHT followed by optional TXTR/TFAC; no deployed MATR or version 0000. Choice DATA starts with signed I16 count. TX1D carries U32 tag/I16 first/I16 count/exact NUL variable/U8 flag/I16 default. PAL carries variable/U8 flag/U32 tag/palette path/I32 default. Direct version 1 loader0077CE70, TX1D loader0077BD80 and PAL loader0077C020 consume those fields exactly; independent corpus reconstruction agrees with native descriptions for all winners.

**Confirmed:** 5,446 texture choices,856 selections,7,908 factors and 243 referenced palette names. Two texture variable uses have ranges7 and 6 with the same name/namespace/default0; they remain distinct operations. Flags select private/shared-owner namespace, supported by exact runtime lookup branch007859FD–00785A16, global prefix constructors00785340/00785450 and strings188F570/186B880. Name+Boolean matching is explicit in0077D640. Texture apply0077BEE0 clamps per-operation and indexes the shared choices; template apply0077C4A0 visits operation families in order.

**Unclosed:** zero-initialized instance values in007857D0 do not establish that authored defaults have already been applied. The YT-1300 inline s06 textures differ from authored default0 choices. Runtime variable registration, palette behavior and exact lifecycle remain independent probes. No flattening or claim of original customized rendering follows from parser acceptance.

## Validation and stopping condition

Native owned-source, exact-name, signed/default/raw-flag fixtures and complete source truncations pass. Invalid selection ranges, unknown versions/records reject. Full independent ordered-field/inline-payload agreement passes on 5,173 shaders. Baseline smoke passes and build has zero warnings/errors. Workspace validation and diff checks follow closure. Scores, packets and stable-name counts remain unchanged; no source assets or generated proprietary evidence were promoted.

Canonical [contract](../../docs/CUSTOMIZABLE_SHADER_DECODER_CONTRACT.md). Next exact probe: bounded authored texture-selection inspection with explicit state provenance; preserve palette controls and verify selected original textures independently before attaching exterior bundles.
