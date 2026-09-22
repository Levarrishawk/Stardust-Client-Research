# EMTR local-space, collision, and wind controls

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Findings

Runtime tracing closes most of the six common floats and three booleans loaded after an emitter's optional sound name.

| Member | Meaning | Confidence and evidence |
|---|---|---|
| `+0x1E4` | particles remain in emitter/local space | Confirmed: position preparation transforms every particle through the current owner transform when set; world wind and terrain-conformance branches are bypassed |
| `+0x1C8` | terrain/world collision enabled | Confirmed: gates segment/surface queries through `TerrainObject` during simulation |
| `+0x1C9` | kill particle on collision | Confirmed: after a collision it copies lifetime to elapsed age, causing normal expiry |
| `+0x1CC` | collision surface/height tolerance | Strongly inferred: added to the test position before the vertical intersection comparison |
| `+0x1D0/+0x1D4` | randomized collision response coefficient A min/max | Confirmed as random range and collision-response use; exact editor label unknown |
| `+0x1D8/+0x1DC` | randomized collision response coefficient B min/max | Confirmed as random range and collision-response use; exact editor label unknown |
| `+0x1E0` | wind resistance | Confirmed: the applied global wind vector is scaled by `1 - value`; default 1 produces no wind acceleration |

Emitter initialization `0x007A2180` samples coefficient A uniformly from `+0x1D0..+0x1D4` into runtime `+0x330`, and coefficient B from `+0x1D8..+0x1DC` into `+0x334`. Collision handling in `0x007A4840` uses the two sampled coefficients on separate components of the reflected/adjusted motion. The broad behavior is established, but the original editor's friction/bounce terminology is not recoverable from this evidence alone.

`+0x23C/+0x240` and `+0x244/+0x248` are normalized min/max pairs with setters that swap reversed inputs. The latter pair defaults to `-1/-1`; getters substitute global limits when a member is negative. No direct simulation or render call is present in this executable, indicating an indirect policy/editor API or dormant path. They remain serialized range overrides with unknown editorial purpose rather than receiving speculative names.

## Replacement consequences

Local-space particles must retain local coordinates and be transformed by the current emitter/owner transform during preparation. World-space particles accept global wind and terrain collision. Collision coefficients must be sampled once per emitter instance as the legacy runtime does, rather than independently per impact. Kill-on-collision should expire through the normal lifetime path so attachments and pool release remain consistent.

