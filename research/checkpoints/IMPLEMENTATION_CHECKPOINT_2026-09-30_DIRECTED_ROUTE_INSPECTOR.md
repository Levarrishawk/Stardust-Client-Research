# Directed route inspector — 2026-09-30

## Question

Can an owned modern directed route query match an independent graph algorithm and expose original floor/layout graph geometry in the offline host? Closed; recovered original cost semantics are outside this question.

## Policy and evidence

The [durable route contract](../../docs/DIRECTED_PATH_ROUTE_CONTRACT.md) defines Euclidean weights, deterministic Dijkstra ties, availability snapshots and explicit boundaries. Inputs are the already independently verified deployed PGRF corpus. Route fixture SHA `6F0DDDF46B177B4386FA1A55C1ED9D26403586DD04BC5C946702D55F831FA779`; configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

`verify_path_routes.py` uses independently authored Bellman–Ford relaxation and verifies every returned explicit edge, endpoint, availability and total distance. **25,847 probes across 3,909 nonempty graphs agree; 21,605 reachable.** Five deployed graphs are empty. Compared costs use 1e-8 relative/absolute floor tolerance. Synthetic exact tests cover directed asymmetry, equal-route priority, zero cycles, unavailable nodes, nearest ties, invalid endpoints/masks/nonfinite values and caller mutation.

Optional scene path graphs are engine neutral and share decoded floor resources. `verify_portal_scene.py --paths` independently confirms **eight path graphs, seven floors/149 triangles and 21 sources** for the pinned original YT-1300, alongside all prior geometry hashes. Prototype host verifies all graph node transforms, edge positions and sample route uploads; all eight sample routes are reachable. Hidden GPU floor/graph capture passes and was visually inspected: hall1 route 0→7 uses four edges over 12.797 units. These are modern inspection units/costs, not a gameplay movement claim.

Additional static reader chain is confirmed: registration 012E1060 installs factory 012E10A0 via setter 0128DF20; getter 0128DF30 returns it. Factory accepts PGRF, constructs a 36-byte SimplePathGraph with initial type 3, invokes virtual reader and post-load slot. Reader 012E1C40 dispatches v0001 to 012E2120 and v0000 to 012E20A0; unknown versions diagnose failure. Adjacent 012E21D0 writes. Original graph lifecycle/cost consumers remain separate.

## Next

Audit floor global-node keys against layout graph node ordinals and paired portal geometry across every layout. Preserve disagreements before adding any cross-cell route connection. Original permissions/door state/floor walking remain open.
