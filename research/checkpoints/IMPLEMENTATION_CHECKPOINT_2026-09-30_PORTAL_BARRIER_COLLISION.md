# Portal barrier collision milestone — 2026-09-30

## Bounded question and completion

Which runtime predicates and geometry activate the portal permission barrier, and can an actor volume stop on its source floor before a cell transfer? Completed for a normal-controller primitive and offline level-source actor-box inspection. Physical leaf clearance and full collision ordering remain explicit next work.

## Evidence and identity

Original executable SHA-256 was rechecked: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 TREE mounts. Effective 291-member manifest SHA-256: `B76BD8546F0BA91DBFC326D2EA333FF92D81D31A84B95B827F649CF81E77368E`. YT-1300 root SHA-256: `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`.

Orientation included the previous transfer session, durable contracts, current registries and September 12 door-state/runtime checkpoints. Static tools: existing `disasm_range.py`, `find_call_xrefs.py`, Ghidra function inventory, pefile/capstone and direct PE RTTI/vtable/constant reads. Relevant ranges: `01290560–01290E30`, `012A49C0–012A5106`, `00B52D20–00B52F30`, `00B53900–00B539B3`, `00B437B0–00B43CF4`, `00B47172–00B471CD`, `01290EC0–01291001`, `012616B0–01261780`. Raw instruction output stays private; no original executable was run.

The [durable contract](../../docs/PORTAL_BARRIER_COLLISION_CONTRACT.md) records instruction observations, confidence, corrected field association, controller transition order, collision vtable gates and compatibility consequences. The initially considered universal SolidByte inversion was disproved by its sole PRTO/0002 caller. Supported versions use LeadingByte exclusion and nonzero SolidByte eligibility; WindingByte is separate. Radius-equality pruning is now recorded precisely. Barrier activity is permission-based, not an invented leaf-fraction threshold.

Four names added: `012A4C40`, `012A4E60`, `012A50A0`, `012A4A30`. Registry now **2,597 distinct addresses**; no duplicates. Behavioral/readiness scores remain **89% / 94%** and the packet matrix remains **345**. No protocol boundary changed.

## Implementation and gates

New engine-neutral files: `PortalBarrierCollision.cs`, `PortalDoorController.cs`, `PortalBarrierFloorWalker.cs`. Synthetic/native probe: `PortalBarrierScenarios.cs`. Prototype host adds Actor bounds, using a documented 0.5×1.8×0.5 inspection box. Level-source barriers can stop progress before the portal and retain the source owner. Nonlevel integration stops explicitly; no live actor contacts or original style controller inputs are claimed.

> Historical prototype-specific command or code example omitted from the research-only documentation.


The verifier independently rereads effective original bytes, records source SHA pins, prepares a hashed manifest, then compares native eligibility, frame/bounds and first-contact parameters/positions/normals against analytic thin-axis probes. Results: **291 layouts / 7,642 references / 7,231 eligible candidates / 21,693 independent probes**, all agree. Original transformed geometry comparisons use `1e-4` bounds/frame tolerance, `2e-5` parameter tolerance and `2e-4` center tolerance; these are validation tolerances, not hidden collision allowances.

Full smoke tests pass and prototype host builds with zero warnings/errors. Bounded offline launcher `tools/powershell/Invoke-StardustAssetPreview.ps1` with pinned YT-1300, `-Interior -InteriorFloors -InteriorPaths -FloorOnly -Cell 1` passes headless and GPU. Actor contact adapter: **12 level links / 24 probes**, two nonlevel sample paths excluded with source-retaining stops. GPU capture was visually inspected; controls fit without overlap. Existing 14-link transfer and surface/path/coordinate gates remain passing. The prototype host certificate-store startup diagnostic remains nonfatal.

Detailed inventories, logs and capture remain under ignored private generated evidence. No asset bytes or raw decompiler corpora were promoted. No server contact, repository publication, staging or commits occurred.

## Remaining uncertainty and precise next probe

The recovered collision predicates do not close global actor solver call ordering. The prototype deliberately sequences its own source-cell sweep before owner transfer. Physical leaf geometry, paired/force-field behavior, trigger handle lifecycle, original style-table loading, nonlevel projected movement and destination-cell barriers remain unimplemented.

Next: read one original named door-style row with exact source identity; follow `01290560` and `012902D0` to define trigger and animated leaf collision extents. Implement one fixture-backed style with contact-triggered motion and clearance. Keep current permission dynamic and avoid replacing full physical clearance with a fraction threshold.

Final workspace gate: 771 Markdown files, zero broken links, zero private-evidence links, 2,597 canonical address rows with zero duplicates, 345 packet rows. `git diff --check` passes; final status was inspected and the staging area is empty. Final headless actor adapter reports 12 checked links, 24 current-policy probes and two explicit nonlevel stops.
