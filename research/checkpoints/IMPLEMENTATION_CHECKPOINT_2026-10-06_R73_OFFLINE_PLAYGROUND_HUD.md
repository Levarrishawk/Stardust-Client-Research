# Round73 offline playground HUD

Date: October6,2026. Question: can an opt-in owned display show bounded supplied movement
status and controls while preserving actual existing world input, and retire cleanly on
callback/lifetime failure? Stopping condition: independent actual-text agreement,
ownership/input/negative proof, inspected private GPU capture and sealed scope handoff.

## Inputs and tools

Accepted72 input:4371 frozen files. Manifest SHA-256
`138f9b7dcbbc43bf7be0a564beb5ffd0771747307f1c72ee108d46a0fef742a5`; archive
`5848eb383951c9a452a3ddf013c23e30132ed8876579101273f277b1d7f302ea`.
Trusted sync preserves HEAD/index/private/unrelated material. Executable is only hashed
to verify `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`;
the replay executes only replacement scenes and reads no original assets. Cached Python,
.NET8 and Godot4.7.2 .NET x64 use offline cached packages. No sibling73 API is imported.

The [independent producer](../../poc/round73-tests/Rendering/verify_picker.py) writes eight
synthetic states and their expected captions before native execution and stages the
unchanged accepted orbit host from the verified archive. The
[verification scene](../../poc/Stardust.Godot/TerrainOfflinePlaygroundHudVerification.cs)
constructs the actual CanvasLayer/Panel/Labels and reads their actual texts.

## Findings versus interpretation

Observation: eight captions agree exactly for Idle, Moving, Manual, Paused, Arrived,
target active/inactive, zero/maximum speed and1/6000-update bounds. Each successful
refresh invokes the callback once and commits actual text before diagnostics/count.
Interpretation: the HUD displays caller-supplied status without owning simulation time.
Confidence: Confirmed bounded replacement behavior. Consequence: caller supplies correct
actor/session association and fixed10ms update policy; public state carries no provenance.

Observation: update fields remain admitted diagnostics, while visible status shows
RemainingUpdates*0.01 formatted invariant0.0s. Speed shows0.00 without an unproved unit.
The title/controls contain the requested offline preview and modern supplied movement
labels and familiar keyboard/mouse controls. Confidence: Confirmed actual labels and
independent expected text. Consequence: no user-facing model/tool/reasoning jargon or
implementation counters are introduced into the playground.

Observation: every owned Control is Ignore/None. Actual viewport W and left-click events
inside the panel reach an underlying input probe, while right-drag reaches the frozen
orbit host and increments its actual accepted/handled input counters. Confidence:
Confirmed bounded pass-through. Consequence: overlay remains informational; world input
and GUI policy stay with existing owners.

Observation: caught source and attachment reentry, cancellation, invalid/null/throwing
source, lost owner/parent/root/label and repeated configuration retire owned nodes/style
once. Off-thread operations/getter fail before source reads or owner-thread retirement.
Valid borrowed owners survive. Confidence: Confirmed fixtures. Consequence: callbacks
are borrowed, no world/source disposal is implied, and failure removes only owned HUD.
The [durable contract](../../docs/TERRAIN_OFFLINE_PLAYGROUND_HUD_CONTRACT.md) records
Configure-once, diagnostic lifetime, partial-text effects and caller responsibility.

## Validation

Final116 headless/117 GPU checks, eight exact independently expected captions each.
Twenty-one grouped failure scenarios cover mode/speed/update/maximum/remaining/offline
domain, null/throwing source, caught Refresh/Dispose reentry, callback/initial cancellation,
freed root/owner, reparented/detached owner, callback parent loss, repeated Configure and
unconfigured Refresh. Four additional scenarios cover configure-source reentry,
configure-source cancellation, actual ChildEnteredTree attachment reentry and freed title
label. Detached initial Configure and off-thread Configure/Refresh/Dispose/Caption also
pass. Input, idempotent retirement and all verification-owned node cleanup pass.

Private GPU image was inspected. Initial tight status/control spacing was corrected to
separated sections in a608x230 panel; final640x480 image is readable and unclipped.
Original earlier receipts/captures remain ignored. Build has zero warnings/errors.
Eight runner guard cases, PowerShell syntax, Python/XML/Markdown, workspace validator,
diff whitespace, frozen4371 unchanged hashes, exactly ten new public files, HEAD/index
and private-ignore audit pass. The offline Godot root-certificate-store warning remains
with exit0; no network requested or resource-leak diagnostic observed.

No main preview, shared source, registry, ledger or unfinished sibling input changed.
No original input/UI parity, hardware capture, live socket, whole GUI coverage, responsive
layout, localization, accessibility navigation or counter-overflow execution is claimed.
Fixed10ms conversion is a caller policy, not a recovered original timing assertion.

## Precise resume point

Lead configures the HUD with the actual playground Node and a callback mapping its
accepted state to this independent immutable record. Refresh after accepted preview
updates; retain the existing world/orbit/GUI input owners. Inspect the composed main
scene overlay and time budget labels, verify actual keys/click/orbit still work, and
dispose HUD before its owner exits. No Runtime73 or Networking73 dependency is required
inside the HUD itself.
