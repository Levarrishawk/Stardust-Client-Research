# CreateTicketMessage legacy-tail closure

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The constructor at `0x00ADFA90` returns with `ret 0x20`, accepting eight explicit arguments, and registers nine archive fields in wire order. Vtable identities can be cross-checked against previously decoded chat messages (`0x015EAD30` Unicode, `0x015DD5D4` ASCII, `0x015EB3E8` U32 and `0x015DE44C` byte/Boolean).

| Order | Type | Constructor source | Current Stardust use |
|---:|---|---|---|
| 1 | ASCII | argument 1 | player name |
| 2 | U32 | argument 2 | category ID |
| 3 | U32 | argument 3 | subcategory ID |
| 4 | Unicode | argument 4 | ticket body |
| 5 | Unicode | argument 5 | ignored legacy metadata |
| 6 | Unicode | argument 6 | ignored legacy metadata |
| 7 | ASCII | argument 7 | ignored legacy metadata |
| 8 | U32 | constructor constant zero | ignored |
| 9 | U8/Boolean | argument 8 | ignored legacy flag |

The general caller at `0x005CAF30` forwards all eight arguments and sends reliably. The bug-submission UI route at `0x013B8F20` builds a formatted report containing `Severity`, `Game System`, `Repeatable`, `Bug Type`, and `StationId` labels; at its `0x013B97EC` construction site both extra Unicode arguments point to an empty global string, the U32 is the constructor's zero, and the final flag is true. The seventh ASCII argument is populated from the local player/account context. This proves that the tail is real serialized legacy metadata rather than uninitialized padding.

Current Stardust's callback intentionally parses only fields 1-4 and ignores the remainder. Therefore the interoperable minimum request is the consumed prefix; a faithful original-client encoder can emit all nine fields in the order above. Historical business labels for the three text fields are not required by current Stardust and remain naming-only uncertainty.

## x64 consequence

All numeric wire fields retain their fixed widths. The three Unicode values use the established Archive Unicode codec and the ASCII values use the Archive ASCII codec; none may inherit host `wchar_t`, `size_t`, pointer, or native Boolean width.

## Confidence

- Nine-field order and wire types: **Confirmed by executable code**.
- Reliable send and two UI construction routes: **Confirmed by executable code**.
- Current four-field server consumption: **Confirmed by server source**.
- Exact historical labels for fields 5-7 and 9: **Unknown, non-material to current-server interoperability**.

## Evidence

- constructor `0x00ADFA90..0x00ADFD16`
- general caller `0x005CAF30`, construction site `0x005CB03F`
- bug UI handler `0x013B8F20`, construction site `0x013B97EC`
- `CreateTicketMessageCallback.h`
