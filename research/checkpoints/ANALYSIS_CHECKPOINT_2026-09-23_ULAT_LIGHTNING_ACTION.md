# ULAT lightning action (block 2 of 3)

Scope: original `Stardust.exe`, effective `playback/use_lightning_appearance.pst`.

## Confirmed by executable code and effective asset

`0x005F65C0` registers `UseLightningActionTemplate` via playback factory registry `0x00404130`. Factory `0x005F6790` allocates a 0x40-byte template and calls `0x005F67F0`, which explicitly supports `ULAT` versions `0000`, `0001`, and `0002`; any other version is fatal. `ULAT/0002` parser `0x005F6BD0` reads two 16-bit fields into `+8,+0xC`, followed by twelve 32-bit fields into `+0x10..+0x3C`. The effective script's 52-byte `INFO` has exactly that width and carries tags `NTPA`, `TPHE`, `2PHE`, `LVEL`, `LTTA`, `CRFG`, `CRFD`, `BDNS`, `LDNS`, `EDNS`, `SUDR`, `ETAR`. The read widths/order are confirmed; tag interpretation is not yet confirmed for every field.

This script's typed variables include `NTPA=appearance/force_lightning.ltn` and sound-like path values for `BDNS`, `LDNS`, `EDNS`, plus hardpoint and scalar controls. This is concrete evidence that original-asset compatibility for this effect requires at least lightning appearance lookup and sound-resource lookup, not merely a shader replacement. The script was previously counted under 26 effective combat-manager keys; that count is manager-reference evidence, not evidence of 26 distinct runtime implementations.

`0x005F7410` is a downstream lightning-action cleanup path: it releases object references at `+0x21C/+0x224/+0x22C`, calls `0x005F6EC0`, and conditionally stops a sound handle through `0x007C99D0`/`0x007C9990`. This supports an effect with attached object and audio lifetimes. The precise construction/update route connecting all twelve tags to those fields remains open, as do effect placement, timing, and the `.ltn` loader semantics.

Provisional names: `0x005F65C0` `install_ULAT_UseLightningActionTemplate`; `0x005F67F0` `parse_ULAT_template`; `0x005F6BD0` `parse_ULAT_0002_INFO`; `0x005F7410` `cleanup_ULAT_action_resources` (last strongly inferred from operation, precise virtual role unconfirmed).
