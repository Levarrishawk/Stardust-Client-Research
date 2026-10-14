# Held customization versus current body state

The standalone customization preview (external historical reference) has an explicit `--customization-height-race` mode. `--customization-race-retire generation|destroy` chooses its final held-worker stale-lifetime case; default is generation. Existing normal mode is preserved. No production wrapper change was required: the accepted [appearance transaction](SCENE_WORLD_CREATURE_CUSTOMIZATION_VISUAL_CONTRACT.md) updates material selections/generated texture without restoring an earlier root/body state.

## Controlled sequence

The preview loads the accepted original-resource closure and supplied full zero/varied profiles. A controlled noncooperative CPU gate holds revision 2 while pending revisions 3/4 replace each other. Before releasing it, the fixture applies two independently authored **scalar-only** CREO:3 packets through the existing scalar codec/world. The first selects Height 1.25; the second selects Height 1.6, Posture 7, Rank 42, LinkId `0xF102030405060799`, Shock `0xDEADBEEF`, and States `0xFEDCBA9876543211`. These are synthetic replay values, not recovered player state. This proof does not depend on the same-round combined customization/scalar codec or runtime.

Each packet is applied to the live visual while CPU composition remains held. The walk clock advances to frame 4.5, focus suspension freezes it, and presentation is then paused for the commit comparison. The expected root transform is derived from current native motion and current Height once: origin `(-1.5,0.2,-0.1)` and diagonal basis 1.6. Exact root equality is asserted in the host; Height bits are `0x3FCCCCCD`. Before/after receipt comparison preserves all six scalar values, replication revision 3, source revision 4/eight values, uploaded pose/normal/topology/UVs, clock, mesh/material/shader/original-texture ownership. Only accepted appearance revision changes from 1 to 4, and generated MAIN texture bytes change from zero to varied. Peak active CPU composers remains one.

The existing subsequent source failure/recovery, movement, focus, run, clock reset and explicit appearance reset gates still run. Final idle reset uses Height 1 and retains the latest other five scalar values, the same ownership, and the varied face. Final held revision 23 is retired by destroy or selection generation loss. No stale appearance commit occurs; accepted revision remains 22. Drain stays incomplete after disposal until the controlled noncooperative job is released. Final counts balance one visual root allocation/disposal and one archive close.

Only Height currently affects root scale in this boundary; the other five values are verified as current typed body/model state. This does not claim posture-driven pose rendering, linked-variable behavior, live wire integration, or legacy renderer parity.

## Independent evidence

The producer (external historical reference) extends the Rendering18 fixture family with two scalar packets authored using Python byte packing, independently of production encoding. The native scenario (external historical reference) checks exact decode/encode, scalar state, finite Height bits and truncation/update limits: 134 assertions. The own runner also passes the accepted source adapter's 97 assertions and prior one-composer/latest-pending ownership regression.

The Python audit (external historical reference) independently parses latest scalar bytes and compares held/committed/reset receipts. It independently verifies original-byte face preparation and all five composition layers for both supplied profiles. GPU MAIN readback SHA-256 equals accepted output hashes: zero `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808`, varied `60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5`.

The race camera uses FOV 40 at `(-1.5,1.3,-6)` looking at `(-1.5,1.3,0)`. Captures at the held and committed stages use identical live Height/pose/clock. Their appearance differs at 36,972 pixels; silhouette/background are identical. The ordinary Height/idle reset changes 30,005 silhouette-mask pixels. These pixel results corroborate a changed appearance at retained current geometry, rather than comparing poses across advancing asynchronous time.

## Reproduction

Use bundled Python, cached .NET historical implementation path (omitted) 4.7.2 Mono, task-private APPDATA, and the existing package cache. No downloads or original executable execution are involved.

> Historical prototype-specific command or code example omitted from the research-only documentation.


For the GPU destroy case add `-Retirement destroy -CapturePath` with a fresh absolute private PNG. The launcher (external historical reference) passes the explicit race arguments and prints private log/export paths. Captures include ordinary base/varied plus `-held`, `-committed`, `-reset` siblings. Run `Verify.py EXPORT PEER LOG [CAPTURE_STEM]`; omission of the stem audits receipts/original composition without claiming rendered pixels. Native package restore uses the own no-sources config and populated private cache; prototype host build uses its existing cached SDK.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_HEIGHT_RACE.md) records input identity, validation, confidence, remaining uncertainty and lead's next mixed-wire join.
