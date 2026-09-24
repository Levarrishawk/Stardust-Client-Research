# Nebula command parser — local renderer tuning boundary

The original `C:\Stardust-DEV\Stardust.exe` contains RTTI `SwgCuiCommandParserNebula`, vtable `0x015F9478`, constructor `0x010D0CA0`, and dispatch `0x010D0D50`. The constructor registers the literal root `nebula`; `SwgCuiCommandParserDefault` constructs and installs it at `0x01010980`. This block covers the child parser, not the larger default parser. Decompiled output is pseudocode.

The dispatch first checks for an active local scene/context (`0x00425200`); without one it returns handled but performs no adjustment. It then recognizes these named tokens:

| Token | Observed argument and local effect |
|---|---|
| `orientedPercent` | F32 to `0x00531210`, followed by `0x00531230(1)` regeneration/dirty signal |
| `density` | F32 to `0x00911AA0`, then regeneration |
| `range` | F32 to `0x00911AD0`, then regeneration |
| `numShells` | integer to `0x00531220`, then regeneration |
| `regenerate` | regeneration directly |
| `randomSeedOffset` | integer to `0x00531240`, then regeneration |
| `colorVariance` | three F32 arguments, builds four-F32 value with leading `1.0`, passes to `0x00531250`, then regeneration |
| `quadGenerationRadius` | F32 to `0x00531280`, then regeneration |
| `renderNear`, `renderFar` | boolean from leading `0` versus nonzero text to `0x00531290`/`0x005312A0` |
| `reload` | string argument to `0x0052EBA0` |

One additional comparison at `PTR_DAT_015F9444` returns handled without a visible effect; its literal token was not resolved and is not named here. Unrecognized tokens return unhandled. The listed calls are local client nebula/preferences/renderer helpers in this inspected method; there is no direct send or packet construction. This does not rule out other paths responding to `CreateNebulaLightningMessage`, already mapped separately.

**Confirmed:** RTTI, root registration, literal command comparisons, argument widths and direct helper targets. **Strongly inferred:** these are diagnostic controls for local nebula rendering. **Unknown:** exact lifetime/persistence of each setting and whether the unexplained token is a status/usage request. No network opcode, general rendering formula or whole-client percentage is claimed.
