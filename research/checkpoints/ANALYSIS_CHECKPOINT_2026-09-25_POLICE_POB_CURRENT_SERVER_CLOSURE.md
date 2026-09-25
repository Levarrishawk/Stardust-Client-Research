# Missing-floor police POB current-server closure

Date: 2026-09-25. Question A08. Scope: current Stardust source and configured client overlay.

The thirteen unresolved floor references remain confined to `appearance/mun_impl_military_base_police_station_s01.pob`. A fresh exact-name source search finds no current object-template, screenplay, spawn, or `portalLayoutFilename` use of that layout. The only current database occurrence is the `interior_interior` environment row keyed `mun_impl_military_base_police_station_s01`; that table supplies interior acoustics/material defaults after a layout is selected and is not a spawn or appearance dependency.

In contrast, current live object definitions explicitly use `appearance/mun_all_military_base_police_station_impl_s01.pob` at six `portalLayoutFilename` sites across military, POI headquarters, and faction-perk headquarters templates. The similarly named working layout must not be conflated with the broken `mun_impl_...` layout.

Combined with the prior decompressed 45,427-resource scan—which found the broken basename only in the POB itself, the interior table, and three caches—this closes A08 for the **current server/configuration** as a conditional diagnostic rather than a reachable gameplay blocker. CRC-only/admin/future content remains possible, so the asset stays in the corpus and the replacement must report missing floors safely instead of fabricating navigation geometry.

Confidence: **confirmed** for current source literal/template selection and configured payload references; **strongly inferred** unreachable in ordinary current-server play; **unknown** for future, admin, synthesized, or CRC-only selection.
