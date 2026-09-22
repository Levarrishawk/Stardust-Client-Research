# Analysis checkpoint: CuiNotepad persistence

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

`CuiNotepad` is a local per-profile text editor. It resolves `profiles/<profile>/notes.txt`, loads it when activated and saves it when deactivated. It has no network message, server authority or replicated state.

The reader caps input at `0x200001` bytes. Ordinary files are converted from narrow text; files whose first byte is `0xFF` are treated as the client's wide-text variant and decoded from the remaining bytes. The writer emits narrow text up to `0x100000` bytes when possible; otherwise it emits a leading `0xFF` followed by at most `0x200000` bytes of 16-bit text.

## Stable provisional functions

| Address | Provisional name | Behavior | Confidence |
|---|---|---|---|
| `0x013BC6C0` | `cui_notepad_construct_VA_013BC6C0` | constructs mediator and binds required text widget | Confirmed |
| `0x013BC800` | `cui_notepad_destroy_VA_013BC800` | releases mediator resources | Confirmed |
| `0x013BC850` | `cui_notepad_activate_load_VA_013BC850` | activates and loads bounded `notes.txt` content | Confirmed |
| `0x013BCA70` | `cui_notepad_deactivate_save_VA_013BCA70` | deactivates and saves narrow or marked wide content | Confirmed |
| `0x013BC440` | `cui_notepad_build_profile_path_VA_013BC440` | builds the active profile path ending in `notes.txt` | Strong |

## Replacement implication

Preserve filename, profile scoping, marker compatibility and size limits if original profiles must round-trip. Keep this optional feature behind platform/profile storage, never the protocol layer. Internal text can remain Unicode with the legacy codec only at the file boundary.

## Remaining uncertainty

The historical source-level name of the `0xFF` convention is unavailable; its byte behavior is sufficient for compatibility.

