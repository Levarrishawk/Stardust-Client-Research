# Explicit offline playground settings

[TerrainOfflinePlaygroundSettings](../poc/Stardust.Core/Session/TerrainOfflinePlaygroundSettings.cs) is an immutable engine-neutral modern offline policy. Its private constructor and get-only properties prevent public initializer/setter bypass. It creates no world, driver, engine, network or persistent configuration effects. It requires no owner thread and can be shared across threads after creation.

`Create(int version = 1, int maximumUpdates = 6000, int maximumClickAttempts = 128, long stepMicroseconds = 10000, float walkSpeed = 3, float runSpeed = 4, float arrivalRadius = .001f, bool holdMissingDestination = true)` validates every value before returning a settings object. Invalid values throw `ArgumentOutOfRangeException`. `Parse(ReadOnlySpan<byte> utf8)` accepts at most1,024 bytes and returns the same validated type. `CreateBudget()` returns a fresh actual [TerrainOfflineSessionBudget](../poc/Stardust.Core/Session/TerrainOfflineSessionBudget.cs), mapping MaximumUpdates to MaximumUpdates and MaximumClickAttempts to MaximumTargets, without changing an existing budget or requested count.

JSON requires exactly these eight case-sensitive property names. Every field is mandatory; parser defaults are deliberately absent.

| Property | JSON token | Accepted value | Create default |
| --- | --- | --- | --- |
| Version | Integer number | 1 | 1 |
| MaximumUpdates | Integer number | 1..6000 | 6000 |
| MaximumClickAttempts | Integer number | 1..128 | 128 |
| StepMicroseconds | Integer number | 10000 | 10000 |
| WalkSpeed | Number | Finite binary32, greater than0, at most10 | 3 |
| RunSpeed | Number | Finite binary32, WalkSpeed..10 | 4 |
| ArrivalRadius | Number | Finite binary32, 1e-4f..1 | .001f |
| HoldMissingDestination | Boolean | true or false | true |

Float values are rounded to binary32 by JSON conversion before range/order validation. The native movement consumers use those same floats. A positive subnormal walk speed is accepted when run speed is at least that value; an underflowed zero walk speed rejects. Integer fields require integer lexical tokens supported by Int32 conversion: `1.0`, exponential integer spelling and quoted numbers reject. Hold accepts only true/false tokens. There is no implicit string/number/bool conversion.

Before JSON decoding, throwing UTF-8 validation checks all raw bytes, including keys and unused source text. JSON allows one root object at depth1, no comments or trailing commas. Unknown, missing, duplicate decoded names (including escaped duplicate names), null, wrong token types, invalid UTF-8, malformed/truncated JSON, nested objects/arrays, oversize documents, BOM and trailing non-whitespace/multiple documents reject. Standard JSON leading/trailing ASCII whitespace is allowed within the byte cap. Legal escaped spellings of required property names are decoded before schema/duplicate checks. The parser reports only `InvalidDataException("Invalid offline playground settings schema or values.")`; it does not propagate source-bearing parser diagnostics.

MaximumClickAttempts is an explicit caller attempt limit. The actual budget only maps its number to the existing successful-target capacity; callers must still count attempted clicks at the input boundary. Walk/run selection, session ownership, click ownership and host persistence are caller responsibilities. Pass parsed maximum counts and CreateBudget explicitly to the point driver, StepMicroseconds to driver/controller, WalkSpeed/RunSpeed to movement policy and HoldMissingDestination to its [opt-in destination guard](TERRAIN_OFFLINE_DESTINATION_GUARD_CONTRACT.md). The settings class implements no such side effects itself.

[Native scenarios](../poc/Stardust.Core.SmokeTests/TerrainOfflinePlaygroundSettingsScenarios.cs) and an [independent Python reference](../poc/round77-tests/Runtime/verify_settings.py) compare seven positive documents,117 malformed/schema/range rejections, exact float bits and fresh actual budget values. Expected fixtures are written before native execution. Cases include default/min/max/custom values, reversed/escaped names, ASCII whitespace and an exact1,024-byte document. Native Create rejection covers20 invalid configurations including nonfinite floats; private construction/get-only properties and cross-thread immutable use are checked. A parsed maximum3/click1 policy drives the actual frozen76 point driver/controller for three exact grounded float steps, including the final zero Idle publication and no extra terminal tick. Native43 checks/138 negatives pass; one offline build has zero warnings/errors. No redundant long movement/cadence/default corpus or host/GPU run is claimed. All frozen production/default profiles remain hash-identical. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R77_PLAYGROUND_SETTINGS.md).
