# Harvester hopper codec closure

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust-3 source

## Result

The nested hopper snapshot in dynamic `HINO:7` is now byte-complete. `HopperList::insertToMessage` writes:

1. `U32 elementCount`;
2. `U32 updateCounter`;
3. for each element, `U64 resourceSpawnObjectId` followed by `F32 quantity`.

The outer `HarvesterObjectMessage7` writes one marker byte immediately before that snapshot: zero for an empty hopper, otherwise `elementCount + 1`. This differs from the older `InstallationObjectMessage7`, whose corresponding marker is merely zero/nonzero. The exact historical label of the marker is not established, so a replacement must preserve the observed value rule rather than reinterpret it as a Boolean.

`HopperList` also supplies the incremental list grammar used by compatible delta-vector machinery:

- add: list header, `U8 1`, `U16 index`, `U64 resourceSpawnObjectId`, `F32 quantity`;
- replace: list header, `U8 2`, `U16 index`, `U64 resourceSpawnObjectId`, `F32 quantity`.

The update counter advances by the declared update count. The server stores `ResourceContainer` references internally but deliberately serializes the associated `ResourceSpawn` ID and the container quantity; a 64-bit client must therefore keep the wire ID at exactly 64 bits and must not serialize a native pointer or engine object handle.

## Compatibility consequence

The full top-level and nested `HINO:7` payload needed for the harvester resource/hopper UI is now specified. A future implementation can decode the snapshot without packet capture. The marker's unpublished source-level name is naming-only uncertainty.

## Confidence

- Snapshot byte grammar: **Confirmed by code**.
- Incremental add/replace grammar: **Confirmed by code**.
- Resource ID meaning and quantity meaning: **Confirmed by code**.
- Outer marker numeric rule: **Confirmed by code**.
- Historical semantic name of outer marker: **Unknown, non-material**.

## Evidence

- `MMOCoreORB/src/server/zone/objects/installation/HopperList.h`
- `MMOCoreORB/src/server/zone/packets/harvester/HarvesterObjectMessage7.h`
- `MMOCoreORB/src/server/zone/packets/installation/InstallationObjectMessage7.h`
- active refresh producers in `HarvesterObjectImplementation.cpp`, `GeneratorObjectImplementation.cpp`, and `EmptyHopperCallback.h`
