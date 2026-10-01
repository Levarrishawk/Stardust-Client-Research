# October 1, 2026 — Persistent sphere and physical doors

## Bounded question

Can an explicit sphere volume drive the existing persistent three-cell scene through captured physical leaves while preserving directional permission and lifecycle rules? Stop at engine-neutral controller integration and the original route; preview activation follows separately.

## Implementation

`PortalDoorSphereFloorWalker` captures each shared physical leaf once before external permission callbacks, retains directional barrier selection and contact metadata, and delegates precise movement to the captured sphere-box floor adapter. Invalid starts do not call permission; bounded sphere inputs reject before callbacks. It does not advance controllers.

`InteriorDoorActorScene` accepts optional immutable `ActorSphereVolume(WorldCenterOffset, Radius)`. Existing box constructor callers remain compatible. Sphere mode rejects simultaneous box or step policy inputs explicitly; it does not silently substitute box steps. Contact sensors use the sphere center. Per-tick external permission evaluation still finishes before controller/actor mutation. Closing safety uses an explicitly conservative world-axis cube enclosing the sphere, with shifted feet so all offset components are retained; movement itself uses rounded sphere contact.

## Validation and original inputs

Full native smoke passes. Synthetic sphere scene covers80 repeated transfers through two leaves under two owner poses, denial/retreat, open unknown permission, failed callback/reentrant reset atomicity, stale retention, reset/dispose and guarded close/retreat release. New shape/radius/step-conflict negative gates pass. Captured door-query tests cover slope contact, reverse policy, unknown/closed barrier, shared snapshot before callback reset, invalid starts and callback failure.

`--original-interior-sphere` resolves nine unchanged original POB/table/APT/LOD/mesh/floor sources plus the five human template IFFs. The original profile supplies center(0,1,0), radius0.5.2,880 ticks over two owner poses produce24 transfers and182 physical/barrier contact blocks. Requested forward/reverse/recovery endpoints are reached; denied and unknown phases gain no target entry. Original frames report2,698 accepted/182 portal-blocked; a closed physical leaf can remain the first contact and therefore obscure a later permission-barrier status.

Independent `verify_original_interior_sphere.py` audits14 effective source identities/95,306 bytes, inherited sphere dimensions, all2,880 returned floor planes, world placement and phase/transfer/contact invariants. This is a frame audit, not an independent dynamic controller implementation. The route contains zero closing-hold ticks; positive conservative closing protection is covered by synthetic tests and requires a separate original occupied-threshold probe.

## Confidence and compatibility limits

Confirmed bounded modern integration and original asset-driven frame results. Original actor controller reachability, extent selection and complete body/head clearance remain separate. Room triangle guards are not enabled in this persistent scene; the previous original sphere/triangle route remains a separate probe. No step/slide policy is implemented for sphere mode. No new names or score changes.

## Reproduction and next action

Run full native smoke and pinned `--original-interior-sphere ROOT --configuration CONFIG --scene-spec SPEC --expected-sha256 SPEC_HASH`, then the frame audit with the configuration pin. Specification SHA-256 `12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA`; configuration pin is unchanged from the [portal profile probe](IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_SPHERE_PROFILE.md). Generated detail remains ignored.

Next: load the pinned object-root profile in the existing background content worker, expose an explicit preview sphere option with matching visuals, and validate original occupied-threshold closing behavior.
