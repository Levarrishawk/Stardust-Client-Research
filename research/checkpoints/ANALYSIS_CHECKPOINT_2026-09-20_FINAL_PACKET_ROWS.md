# Analysis Checkpoint: Final Server Packet-Inventory Rows

## Factory crate quantity delta

`FactoryCrateObjectDeltaMessage3` emits `FCYT:3` index 7 as U32 quantity/use count. This is correct. The baseline's two consecutive zero integers at the visible-component position encode one container field (count plus update counter), so the logical indices are customization 4, components 5, options 6, quantity 7, damage 8, maximum condition 9, and visibility 10. Source-level `insert*()` count had made the index appear off by one.

The active producers are factory output and crate split/extraction state changes. A replacement should reuse the ordinary Tangible view-3 codecs under the `FCYT` tag and publish quantity changes to inventory/container presentation.

## Dormant harvester damage delta

`HarvesterObjectDeltaMessage3` has no construction site in the current source tree. Its helper mutates server condition damage and would emit tag `ITNO`, view 3, index 10. Active harvesters use `HINO`/`INO H`-family baselines and Installation/Tangible composition; therefore this unused helper has a confirmed tag/schema mismatch and must not be copied or activated. Condition damage should use the correct active object tag and inherited Tangible condition index for the instantiated client class.

## Active harvester synchronized-UI baseline

`HarvesterObjectMessage7` is actively sent when an authorized operator opens the machinery UI, whenever operator state refreshes, and after hopper operations. It is a dynamic `HINO:7` synchronized-UI payload consumed by the client harvester resource page (`/res.HarvesterPage`). Its server byte grammar is:

1. U8 version/presence value `1`.
2. Two resource-ID list snapshots, each `U32 count + U32 updateCounter + count × U64 resourceId`.
3. Resource-name snapshot: `U32 count + U32 updateCounter + count × ASCII name`.
4. Resource-type snapshot in the same shape with ASCII type.
5. U64 active resource-spawn ID.
6. U8 installation active.
7. S32 displayed extraction rate.
8. F32 maximum/specified extraction rate.
9. F32 actual/current extraction rate.
10. F32 current hopper quantity/capacity value named `getHopperSize()` by the server.
11. S32 maximum hopper size.
12. U8 hopper presence/count marker (`0`, otherwise list size + 1), followed by the `HopperList` snapshot subcodec.
13. U8 condition percentage, currently constant 100.

The executable's generic view-7 package mechanism provides the bounded dynamic decoder; resource-manager/UI strings and the independently mapped `MessageQueueHarvesterResourceData` path corroborate the subsystem. The nested `HopperList` element byte grammar remains delegated to its existing server serializer and should be captured as a separate fixture before implementing the full harvester UI. This is an explicitly bounded nested-codec follow-up, not an unknown top-level packet route.

## Confidence

- Factory index and effect: **Confirmed**.
- Harvester damage helper being unused and structurally incompatible: **Confirmed**.
- Harvester view-7 producer sites, top-level field order and dynamic UI subsystem: **Confirmed**.
- Exact nested HopperList element layout and semantic name of the leading U8: **Incomplete/bounded**.

