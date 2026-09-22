# Tangible Visible-Component Width Correction

Target: `C:\Stardust-DEV\Stardust.exe` and current Stardust server source

## Corrected conclusion

The previously reported `TANO:3[5]` four-byte-versus-eight-byte compatibility defect does **not** exist. Client and server both encode each visible-component value as one signed 32-bit integer.

This correction is based on complete constructor registration order, exact registry selection, MSVC RTTI, and the actual snapshot/delta codec vtables. It supersedes the contrary conclusion in the 2026-09-15 TangibleObject report and any derivative status/ledger text.

## How the false association occurred

The TangibleObject constructor builds several adjacent AutoDelta containers. Two were conflated:

- object `+0x328`, vtable `0x015E05E0`, is RTTI
  `Archive::AutoDeltaSet<int, Archive::DefaultObjectType>`;
- object `+0x2F0`, vtable `0x015E0614`, is RTTI
  `Archive::AutoDeltaVector<CachedNetworkId, Archive::DefaultObjectType>`.

The eight-byte codec at `0x006421A0/0x006423E0` belongs to the `CachedNetworkId` vector at `+0x2F0`, not the visible-component set. `CachedNetworkId` is an eight-byte wire identity in this executable.

## Constructor registration proof

`tangible_object_construct_VA_0063EE10` registers fields with two different inherited view registries:

- calls to `0x005554A0` forward to the registry embedded at object `+0x124`;
- the final call to `0x005554C0` forwards to the distinct registry at object `+0x144`.

The `+0x124` sequence includes `+0x328` in the exact TANO view-3 position corresponding to visible components. The adjacent `+0x2F0` `CachedNetworkId` vector is passed to `0x005554C0`, proving it belongs to the other view and cannot supply the TANO:3 index-5 codec.

## Exact visible-component codec

The `AutoDeltaSet<int>` vtable at `0x015E05E0` supplies:

- `0x00642B80`: snapshot serialization, writing count U32, update counter U32, then **4 bytes per set key**;
- `0x00642F00`: snapshot parsing, reading the same two headers and **4 bytes per set key**;
- `0x00642BE0`: delta serialization, writing command records whose value-bearing forms carry **4-byte keys**;
- `0x00643050`: corresponding delta parser.

This matches current server `AutoDeltaSet<int>` / `TypeInfo<int32>` serialization exactly. There is no second client U32 and no baseline misalignment when the set is nonempty.

## Replacement contract

```text
TANO view 3 index 5:
  snapshot: U32 count, U32 updateCounter, count * S32 componentId
  delta:    AutoDeltaSet command stream with S32 componentId values
```

The replacement must preserve the 32-bit wire component ID. Native x64 pointer size is irrelevant.

## Confidence

- Field object address, distinct registry destinations, RTTI identities, vtable ownership, and four-byte codecs: **Confirmed by code**.
- Server agreement: **Confirmed by source**.
- Historical human-readable meaning of each integer component ID beyond “visible component”: data-domain detail, not a wire uncertainty.

