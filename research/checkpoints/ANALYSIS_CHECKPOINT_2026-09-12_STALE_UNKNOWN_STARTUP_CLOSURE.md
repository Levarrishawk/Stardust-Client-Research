# Startup and stale-unknown closure

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

A mechanical audit of the 674 address rows in `UNKNOWN_FUNCTIONS.md` found only
nine rows still containing the words `Unknown` or `Tentative`. Six were stale
duplicates or missing-label entries whose behavior had already been resolved.
This checkpoint closes the five startup rows and records the two already-closed
terrain/wind rows. “Unknown function” is therefore no longer an accurate reading
of this ledger; almost every row is an address-stable provisional name with known
principal behavior.

## Startup functions

| Address | Stable name | Evidence and behavior | Confidence |
|---:|---|---|---|
| `0x00401000` | `startup_misc_override_load` | opens TreeFile path `misc/override.cfg`, obtains buffer/length through virtuals, passes them to override/config ingestion, releases buffer and file | Confirmed broad role |
| `0x00401950` | `setup_client_animation_install` | scope string is `SetupClientAnimation::install`; calls seven ordered animation/playback registration installers | Confirmed |
| `0x004019D0` | `playback_script_manager_exit_registration` | scope `PlaybackScriptManager::install`; registers cleanup callback `0x004020A0` under `PlaybackScriptManager`; callback clears its global retained object vector | Confirmed |
| `0x00426300` | `game_feature_bits_get` | returns the mask stored by setter `0x00426320`; setter diagnostic is `GameFeatureBits: %s`; bootstrap derives downstream install feature flags from bits `1`, `0x10`, and `0x200` | Confirmed |
| `0x00421490` | `setup_client_direct_input_install` | scope string `SetupClientDirectInput::install`; loads `ClientDirectInput` options and installs platform DirectInput callbacks | Confirmed |

The DirectInput configuration block explicitly includes `useJoysticks`,
`useJoystick`, `useMouse`, `windowsKeyEnabledWhenFullscreen`,
`windowsKeyEnabledWhenWindowed`, `keyboardTranslationTable`, and
`enableForceFeedback`. This is an input-platform installer, not a generic window
installer.

## Other stale tentative rows

- `0x008628E0` is already confirmed as the ClientChunk environment-family sampler
  over byte grid `+0xD4` and PTAT family lookup.
- `0x00844AA0` has confirmed setter behavior: store, normalize, and publish the wind
  vector. The unavailable original class/member name does not make its semantics
  unknown.

## Remaining literal uncertainty mentions

Two rows use “unknown” only in bounded edge-policy descriptions:

- `0x0046E720`: historical names of two result metadata fields are unavailable,
  while command removal behavior is confirmed.
- `0x00AC9660`: principal ObjectController envelope parsing is confirmed; behavior
  for unsupported nested payload types still merits defensive-policy analysis.

Neither is an unidentified function. Future audits will classify unresolved
fields and malformed-input policy separately from function behavioral coverage.
