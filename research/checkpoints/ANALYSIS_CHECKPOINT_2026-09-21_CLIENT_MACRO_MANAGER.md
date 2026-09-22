# Client Macro Manager

## Scope

This block closes the local macro record, profile paths, text and legacy-IFF persistence, mutation notifications, and command-execution boundary. Macros are client profile data; they are not a network message family.

## Confirmed profile paths and versioning

- `client_macro_build_text_path_VA_0069F770` resolves the active player's `profiles/.../macros.txt`.
- `client_macro_build_legacy_iff_path_VA_0069F980` resolves the parallel `profiles/.../macros.iff`.
- The text header is `version: 0000`.
- The IFF hierarchy is `ORCM/MRTN/0000`; each record is an `ENTR` form/chunk under the version form.

The install/reload path clears registrations and records, attempts `macros.txt`, and falls back to `macros.iff` only if the text load fails. The legacy reader validates the IFF and its version before importing records.

## Record model

The in-memory record stride is `0x40` bytes. Confirmed logical fields are:

| Offset | Meaning | Evidence |
|---|---|---|
| `+0x00` | numeric macro ID | serialized as U32 in IFF and decimal in text |
| `+0x04` | generated internal name | `custom_macro_%04d` creation and lookup |
| `+0x10` | user/display name | serialized first among the four strings |
| `+0x1c` | command text | console execution input; serialized fourth |
| `+0x28` | icon/specifier string | serialized second; exact historical member name inferred from UI role |
| `+0x34` | color string | serialized third; new records default to `#ffffff` |

The ID generator accepts an explicit nonzero ID while loading, otherwise advances a client-local counter and generates the internal name.

## Text codec

`client_macro_save_text_VA_006A0690` writes the version header and one five-value record per macro: decimal ID followed by the four user fields. The writer escapes record-sensitive characters. The loader tokenizes the same four fields, reverses escaping for command text, reconstructs the generated internal name, and reports malformed or unsupported-version lines. Command text is bounded to approximately `0x400` bytes by the legacy implementation.

The exact delimiter/escape helper source names are unavailable, but save/load symmetry is confirmed by their shared helper paths.

## Legacy IFF codec

`client_macro_save_legacy_iff_VA_006A1720` writes:

```text
FORM ORCM
  FORM MRTN
    FORM 0000
      ENTR: U32 id, string(+0x10), string(+0x28), string(+0x34), string(+0x1c)
      ...
```

`client_macro_load_legacy_iff_VA_006A1120` validates `ORCM/MRTN`, selects version `0000`, and delegates record import. It is a migration/fallback path, not the primary current format.

## Mutation and presentation events

- Add publishes `ClientMacroManager::Messages::Added` and optionally persists.
- Modify publishes `Modified`, updates the registered command when applicable, and optionally persists.
- Remove unregisters the command when applicable, publishes `Removed`, and optionally persists.
- RTTI confirms `SwgCuiCommandBrowser` and `SwgCuiToolbar` as presentation subscribers; toolbar also consumes changed/reset notifications.

## Execution boundary

`client_macro_execute_VA_0069FD00` passes the stored command string into the client's internal console-command parser. One mode prepends `/pause 0.1;`; a related helper produces `/pause 0.25;`. This is not an operating-system shell boundary. Network effects occur only if an individual slash command follows its ordinary gameplay request path.

For a replacement client, parse macro commands through the same typed command registry used by chat/toolbar actions. Never hand macro text to an OS shell, scripting runtime, or unrestricted reflection layer.

## Replacement implications

- Preserve import compatibility for both `macros.txt` and `macros.iff` if original player profiles are a migration requirement.
- Preserve numeric IDs and internal `custom_macro_%04d` identity because toolbars and command-browser state can refer to them.
- Keep the wire protocol independent: the macro system persists and expands local commands; it does not define a new packet.
- Replace fixed buffers with bounded dynamic strings while enforcing explicit record and command limits.
- Preserve the observable Added/Modified/Removed/Changed/Reset event contract, not the original transceiver implementation.

## Confidence

- **Confirmed:** paths, version, IFF tags, record stride, ID/internal-name behavior, field serialization order, default color, text-first/IFF-fallback load policy, console-parser execution boundary, and mutation events.
- **Strongly inferred:** `+0x28` icon and `+0x34` color business labels, based on defaults and UI use.
- **Unknown:** original C++ member names and the precise historical escape-helper names.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `decompiled-macro-manager/`
- `macro_manager_addresses.txt`
- `manager_event_graph.tsv`

