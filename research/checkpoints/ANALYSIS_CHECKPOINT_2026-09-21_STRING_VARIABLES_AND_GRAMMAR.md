# Analysis Checkpoint: String Variables and Grammar

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block closes the architectural bridge from server `StringIdChatParameter` prose packages through original STF lookup, participant substitution, pronoun/grammar selection and final Unicode presentation. It does not assign speculative names to every historical grammar code.

## Wire prose package

The current Stardust server's `StringIdChatParameter::addToPacketStream` supplies the exact compatible field order:

```text
ASCII16 base STF file
U32     zero filler
ASCII16 base STF key

TU: U64 objectId, ASCII16 STF file, U32 zero filler, ASCII16 STF key, Unicode16 literal
TT: U64 objectId, ASCII16 STF file, U32 zero filler, ASCII16 STF key, Unicode16 literal
TO: U64 objectId, ASCII16 STF file, U32 zero filler, ASCII16 STF key, Unicode16 literal

U32 DI
F32 DF
U8  trailing legacy byte
```

Each TU/TT/TO parameter may therefore be supplied by object identity, nested StringId, literal Unicode, or fallback combinations. The class contains an `NU` member but the active packet writer does not serialize it. A replacement decoder must follow the emitted byte order, not the C++ member order.

## Placeholder expansion

`cui_string_variables_expand_VA_008AD1E0` loads the original `grammar` STF table, scans the localized Unicode base message for three-character substitutions beginning with `%`, and interprets them as:

```text
% <grammar-element code> <participant code>
```

Participant codes are confirmed as `U`, `T`, and `O`, selecting the TU, TT and TO parameter/object slots respectively. Unknown participant codes and unknown grammar-element codes are diagnosed and preserved/fallback-rendered rather than silently assigned invented meanings.

The executable recognizes exactly eleven grammar-element codes through a static table:

| Code | Internal selector |
|---|---:|
| `T` | 0 |
| `N` | 1 |
| `S` | 2 |
| `O` | 3 |
| `P` | 4 |
| `F` | 5 |
| `R` | 6 |
| `H` | 7 |
| `M` | 8 |
| `K` | 9 |
| `D` | 10 |

Selectors 0 and 1 are directly observed producing the participant's display name and first token/name component. Selectors tied to pronouns query `grammar.stf` entries `pronoun_subjective`, `pronoun_objective`, and `pronoun_possessive` and choose variants using creature grammar/gender state. The remaining selectors are retained as stable codes pending exhaustive label recovery.

`string_grammar_select_viewpoint_VA_00980C00` selects localized raw-message variants according to whether the local player is the message source, target, or neither and whether subject/object identities coincide. It applies fallback categories before declaring a missing grammar string. This is why the same social/prose key can render differently for first-, second- and third-person observers.

The effective English `string/en/grammar.stf` is selected from `mtg_patch_013_configurable_02.tre`, contains five entries, and provides the three pronoun tables plus shipped test/support data.

## System-message connection

The already-mapped `ChatSystemMessage` opcode `0x6D2A6413` supports literal Unicode or a length-delimited instance of this structured package. `NpcConversationMessage`, spatial chat, socials, combat prose and numerous UI notifications also converge on the same expansion machinery. This is a shared presentation service, not a chat-only parser.

## Replacement contract

- Implement one bounded `LocalizedProsePackage` decoder with the exact server field order and explicit U64 object IDs.
- Resolve base and nested StringIds through the original locale/fallback STF cache.
- Resolve objects by stable ID at formatting time; tolerate unavailable objects and use nested/literal fallbacks.
- Preserve `%XY` codes as a compatibility grammar IR. Do not bake English pronouns or word order into packet handlers.
- Preserve viewpoint-sensitive selection and case-specific participant naming.
- Bound all length-prefixed strings and recursive/nested substitutions; malformed prose is untrusted network input and must not crash the client.

## Confidence

- Wire order/types, `%XY` token width, U/T/O participant routing, eleven grammar codes, three pronoun tables and viewpoint fallback architecture: **Confirmed by executable/server code/assets**.
- Display-name/first-name selectors 0/1: **Confirmed by code**.
- Historical human-readable names for selectors `F/R/H/M/K/D` and all fallback enum labels: **Unknown; stable numeric/code identity is preserved**.

