# Analysis Checkpoint: Empty and Compatibility Dynamic Views

Eight baseline/delta rows are bounded without inventing gameplay semantics.

- `LairObjectMessage3` is exactly inherited `TANO:3`; it appends no lair-specific fields.
- `ManufactureSchematicObjectDeltaMessage6` targets confirmed MSCO view-6 resource-slot U8 index 5.
- `ManufactureSchematicObjectMessage7` declares a view-7 package but writes no payload fields. Views 8 and 9 declare zero fields and are empty compatibility baselines.
- `TangibleObjectMessage7` writes two U64 zeros into its dynamic view-7 payload; no Tangible semantic consumer was recovered. Views 8 and 9 are zero-field empty baselines.

The executable dispatches view 7 through its dynamic package wrapper and views 8/9 through fixed streams. A replacement should accept these bounded empty/zero forms and retain compatibility values for diagnostics without fabricating domain fields.

Server payloads, view dispatch, MSCO index 5, and absence of additional fixed fields are **Confirmed**. The original meaning of the two TANO view-7 zeros is **Unknown/dormant**.

