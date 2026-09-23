# Missing-floor police POB reachability audit

Date: 2026-09-23. Effective configured overlay and current Stardust source.

## Result

The one portal layout with thirteen unresolved floor references,
`appearance/mun_impl_military_base_police_station_s01.pob`, has no direct
literal reference in any scanned effective client object-template `.iff` or
`.apt` payload, nor an exact `portalLayoutFilename` reference in current
Stardust server scripts. A read-only scan of 45,427 effective candidate
resources found the POB basename in only five resources:

1. the POB itself (its child appearance references);
2. `datatables/interior/interior.iff` (environment row key);
3. `misc/cache_small.iff`;
4. `misc/cache_medium.iff`;
5. `misc/cache_large.iff`.

The cache hits name a related component mesh path and are not a route from a
spawned object template to this POB. The interior table row can provide
acoustics/material defaults if the layout is used, but likewise does not
establish a spawn. Current server scripts instead reference
`mun_all_military_base_police_station_impl_s01.pob` for comparable imperial
police-station templates.

## Confidence and limit

The literal-path census and current source search are **confirmed**. The
conclusion that the broken layout is probably outside common deployed play is
**strongly inferred**, not proven. CRC-only, generated, admin-spawned, or
future dynamically selected resources could bypass literal references.
Maintain the missing-floor diagnostic and safe v0004 null-floor behavior; do
not silently fabricate geometry or remove the POB from the asset corpus.

Evidence: previous `PRTO_FLOR_REFERENCE_CLOSURE` checkpoint; read-only scan
script `local-private/session-2026-09-23-marathon/scan_police_pob_refs.py`;
current `MMOCoreORB/bin/scripts/object/building/military/objects.lua`.
