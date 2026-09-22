# Documentation Unknown Reconciliation (2026-09-22)

## Purpose

The historical `UNKNOWN_FUNCTIONS.md` retained an early “High-priority unknowns” list even after later focused blocks closed every listed architectural boundary. This checkpoint reconciles that stale summary without deleting the chronological evidence below it.

## Reclassified items

| Historical item | Current classification | Principal evidence |
|---|---|---|
| GameNetwork listener/factory registration | Closed | network listener, application-message dispatch, ObjectController factory and complete server packet matrix checkpoints |
| transport selectors/compression/CRC | Closed for interoperability | SOE transport and optional-transform golden-vector work; unpublished labels are naming-only |
| login/cluster/character/permissions/start-scene handlers | Closed | login/zone callback and select-character/session checkpoints |
| create/destroy/baseline/delta/containment/transforms | Closed at architecture and active schemas | replication registry, object construction, per-class baseline/delta, containment and movement checkpoints |
| baseline view registry/field order | Closed for active Stardust object families | generated view registries plus server constructor correlation |
| main-loop phase order | Closed | frame schedule and GroundScene/bloom/CUI ordering checkpoints |
| raster DLL/D3D9 device lifecycle | Closed as legacy backend boundary | raster DLL API, BeginScene/EndScene/Present and reset-resource lifecycle analysis |
| TRE directory/index and IFF factories | Closed for replacement architecture | VFS/TRE runtime, filename identity and typed asset-factory checkpoints |
| thread ownership/network and async loading | Closed at architectural boundary | thread/scheduler topology, file streamer, async loader and terrain-worker lifecycle checkpoints |

## Remaining queue

No Class-C protocol, asset-container or lifecycle blocker is currently known for beginning a constrained replacement prototype. The remaining work is Class-B/C breadth in optional UI/gameplay mediators and specialized presentation fidelity. New unknowns should be added only when evidence demonstrates an observable compatibility risk, not merely because an original symbol name is unavailable.

