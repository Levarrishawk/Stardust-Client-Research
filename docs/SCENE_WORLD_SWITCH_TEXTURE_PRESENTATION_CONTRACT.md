# World switch-texture presentation

The [Godot presentation adapter](../poc/Stardust.Godot/SceneWorldSwitchTexturePresentation.cs) owns its inner presentation and the accepted [eight-frame terminal cache](SCENE_STATIC_SWITCH_TEXTURE_VISUAL_CONTRACT.md). It borrows the exact resource publication and underlying static visual through those owners. Construction requires matching publication identity, complete static resources and an explicit all-frame bundle. Constructor failure releases temporary playback/cache ownership before the caller disposes its inner presentation.

## Supplied time and optional object clock

Default playback passes supplied finite nonnegative seconds directly to the cached selector. An optional fixed step supports deterministic fixtures. Playing starts false; paused Tick still checks the inner presentation and cache lifetime. Apply updates the inner presentation and checks the selector without advancing it.

The explicit object-clock option owns one [ShaderTexturePlayback](SHADER_TEXTURE_PLAYBACK_CONTRACT.md) registration. AdvancePlayback first advances its modern clock, services the object once, then delivers that elapsed value to the visual. Exact selector index, residual bits, revision and cumulative advances must match the playback publication. Same-tick service preserves the exact visual receipt and does not replay historical nonzero elapsed time. Zero supplied seconds advances the clock's minimum one tick.

ResetPlayback resets the visual, removes its clock registration and registers a fresh token at the current tick. Cursor revision and cumulative-advance seeds match the independently reset visual. This is explicit modern reset behavior. The preview advances this local clock only while Playing; it is a deterministic supplied-time proof, not the original shared global frame clock, culling policy or first-visible-frame observation.

## Failure and retirement

Invalid clock Advance preflight retains both clock and visual receipts. Once time advances, service, selector or GPU mismatch retires the whole presentation. Failure after a successful visual reset also retires if re-registration cannot complete. No consumed delivery is retried and no rollback is claimed. Tick failure retires through the host lifecycle. Owner-thread checks apply to every mutation.

Dispose closes the optional clock, releases the texture adapter, then disposes the inner presentation and retains its drain task. Nested finally blocks attempt every cleanup even if an earlier stage throws. Disposal is idempotent; cache and clock registrations are empty afterward. The adapter does not close a borrowed world or reload assets.

## Verification

Round29 supplied-time integration remains valid. Round30 main headless and hidden GPU proofs pass fourteen states against [an independent integer/float-bit oracle](../poc/round30-tests/Integration/verify_clocked.py). The oracle computes quantized ticks, float32 inverse-scale elapsed time and selector results from fixture input bits. It checks all eight original mip identities, exact same-tick retention, reset token change, invalid-input retention, unchanged geometry/pose/publication/caption, eight initial GPU uploads, no update archive reads and complete cleanup. Earlier typed TANO3 and named CREO3 checks are chained. Known pre-frame startup diagnostics retain their existing exact allowances.

Confidence: Confirmed for these bounded replacement outputs and ownership checks. Original PC24 precision, complete material rendering, first visible frame and live interoperability remain unclaimed. See [Round30 acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R30_CLOCK_AND_DEFENDER_STAGES.md).

## Round31 actual consumed-time failure

Headless and hidden GPU main-host proofs advance all eight cached frames, then supply500seconds. Clock service consumes500000ticks, the cursor rejects its catch-up budget, and the entire presentation retires with unchanged visual receipt. Clock registrations and all8cached textures drain; no retry is attempted. [Independent fixture oracle](../poc/round31-tests/Integration/verify_clock_fault.py) checks these facts and chains previous typed-name/visibility/state oracles. This does not claim rollback of consumed clock time or original numeric parity.
