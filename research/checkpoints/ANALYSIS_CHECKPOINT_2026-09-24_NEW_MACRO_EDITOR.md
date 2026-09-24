# New Macro editor — local profile mutation, not a packet family

## Evidence boundary

The original `C:\Stardust-DEV\Stardust.exe` has RTTI `SwgCuiNewMacro` and vtables at `0x015F6E1C` (callback face) and `0x015F6EA0` (mediator face). This block covers the screen-specific input-to-manager path, extending but not re-counting the earlier [client macro manager](ANALYSIS_CHECKPOINT_2026-09-21_CLIENT_MACRO_MANAGER.md) work. Decompiled output is reconstructed pseudocode.

Constructor `0x00FB60E0` binds `buttonok`, `buttoncancel`, `textname`, `texttext`, `volumeicon`, and `sampleicon`; it also initializes the icon picker from local UI/icon resources. It does not establish a network session. Destructor `0x00FB68D0` releases the owned icon/picker state and calls common mediator teardown.

The callback `0x00FB6C80` has two direct control branches: Cancel closes the mediator, while OK reads the name and macro text, performs local validation against the existing macro collection and input characters, then selects either `client_macro_add` (`0x006A0180`) or `client_macro_modify` (`0x006A0410`) depending on whether an existing macro record was supplied. The add/modify calls include the selected icon state and request persistence; the add branch subsequently invokes the known text save path (`0x006A0690`). The callback's rejection paths use local message boxes; the exact localized text and all allowed-character rules have not been fully normalized here. One comparison appears to lower-case strings for duplicate-name validation, but exact acceptance of substrings versus whole names needs a targeted helper check.

The prior manager analysis already establishes `profiles/.../macros.txt` as primary persistence, legacy `macros.iff` import, and client-side slash-command execution. Thus this UI is a disposable editor for **local profile data**. It does not itself serialize a new SWG message. A macro's later network effect depends on the individual command text, which is a separate path.

**Confirmed:** RTTI/vtables, widget names, OK/Cancel branches, add/modify function targets, and local persistence handoff. **Strongly inferred:** name uniqueness is case-insensitive. **Unknown:** exact validation policy and icon catalog coverage. No new opcode or whole-client percentage is claimed.
