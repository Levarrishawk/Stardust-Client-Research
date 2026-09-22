# BasicAnimationAction legacy flag closure

Scope: `C:\Stardust-DEV\Stardust.exe` and all ASH records in archives selected by `swgemu_live.cfg`.

## Confirmed executable behavior

- `EditableBasicAnimationAction` constructor paths initialize `+0x28` and `+0x29` false.
- Binary readers `0x007F7E30` and `0x007F7F40` consume `+0x28` as the first trailing U8 after action/logical-animation identification. The first reader treats the second trailing byte as optional; the latter consumes both.
- Binary writer `0x007F7C40` emits both bytes in order.
- XML reader `0x007F8020` reads only the named `useAddTrack` attribute into `+0x29`; it never sets `+0x28`.
- XML writer `0x007F7D60` emits only `useAddTrack`; it never emits `+0x28`.
- `0x007F7B50` returns `+0x28`; it has no direct call sites and its only code pointer is BasicAnimationAction vtable slot 3.
- The state-hierarchy controller queries action virtual slots 2 and 4 (logical animation and `useAddTrack`). It does not query slot 3. Movement actions delegate the corresponding queried properties to the selected child action.

## Corpus evidence

The effective overlay contains 4,601 ACTN chunks. Scanning all configured archive copies, including shadowed historical versions, produced 16,617 ACTN chunks. Every chunk has a two-byte trailer and every first byte (`+0x28`) is `00`. The second byte (`useAddTrack`) contains both `00` and `01`, validating the trailer parse.

## Classification

`+0x28` is a dormant binary-schema compatibility field in this executable/content set. Its exact historical C++ label is unavailable, but that is a Class-D original-name-only unknown, not a Class-C behavioral unknown. A replacement reader should consume and retain the byte for round-tripping, default it false for XML/current authored content, and log a nonzero value as an unsupported legacy extension until contrary evidence exists. It must not invent transition behavior for the field.

## Confidence

- Width, order, defaults, binary/XML behavior, absence of runtime use, and corpus values: Confirmed.
- Historical source-level name: Unknown and not replacement-blocking.
