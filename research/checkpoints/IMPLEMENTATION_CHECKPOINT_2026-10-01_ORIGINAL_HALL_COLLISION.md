# October 1: original hall geometry and supported body stops

## Question and inputs

Can the recovered eligibility rule supply an unchanged original triangle set that produces independently verified supported movement stops? Stop at extraction identity, bounded original requests, stable holds/retreat and independent contact/floor agreement. Reuse Mustafar cell6; no asset edits or broad corpus scan.

POB SHA `79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818`; configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,48 descending mounts. Hall mesh SHA `8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B`; hall FLOR SHA `42D9ADF10068796F6113D9371BBCDCF6DEF17B4027D96BF5063F61C39814EFEA`. Inputs pass through original POB references into the hall appearance/material graph and floor. No object-template spawning or original executable execution.

## Implementation and finding

`StaticMeshCollisionGeometryReader` extracts owned indexed triangles with stable source ordinal IDs, explicit eligibility and exact degeneracy accounting. It validates indices/positions and resource limits, rejects missing shader predicates and never silently truncates. `FromInvariantCandidates` permits source selection only when every implementation candidate agrees; materially differing candidate predicates reject. The earlier [source trace](ANALYSIS_CHECKPOINT_2026-10-01_MESH_COLLISION_SELECTION.md) establishes both hall predicates are true. Hall extraction:1,984 source/eligible/nondegenerate triangles, zero degenerates. Canonical extraction hash `0BB4237A347FA1ED88D83BF4DCC4A66E2ED9BB26F2E4E00A0D82007DB10D02C0`.

Initial original probes exposed two issues. First, later entry across a coplanar finite floor-triangle edge could report a lateral collision although a fixed separating plane prevented penetration throughout the sweep. Tangent-enabled queries now ignore that case; closed-contact queries retain it. Second, authored visible floor triangles differ from FLOR by a few float32 units (for example triangle343 reaches Y17.76976776123047 versus FLOR Y17.769765853881836). Exact body-bottom alignment created initial overlap and unstable repeated contact. These observations are retained; they are not asset modification requirements.

The path/walk/step/portal triangle APIs now accept explicit `feetClearance`, default0, finite range0–0.05 metres. Body center is feet Y + half-height + clearance, and adoption subtracts that same offset. Original probe uses0.0001 metres (0.1 mm), a declared modern policy; it shifts the whole body box upward without changing its dimensions. Existing default semantics are preserved. No original tolerance claim follows.

## Validation

The probe uses both original floor-triangle centroids, four directions each and two body half-widths (0.25/0.65, height0.9, depth0.25):16 requests. Four wide-body requests stop on original wall triangles;12 stop at authored floor boundaries. Narrow bodies reach the floor boundary first, an explicit observed ordering. Every result has a reusable floor locator;1,600 repeated requests remain stable and16 retreats reach their starting point.

`tools/python/verify_original_hall_collision.py` independently rereads pinned original mesh/floor bytes, matches extraction SHA/counts, checks72 floor-plane points and94 conservatively selected LP narrow-phase cases. All16 outcomes/contact IDs/segments agree; maximum contact-parameter difference4.107825191113079e-15. No original payloads are written to public data.

Native tangent-entry/closed-contact, authored micro-overlap, explicit clearance, negative clearance, portal target-floor-height and step-source-rollback gates pass. Existing full native smoke and Godot build pass. After the kernel change, prior512 straight sweeps,512 precise paths and512 portal LP cases all pass with unchanged pins/outcome counts. Workspace/link and diff checks pass.

Reproduce native `--original-hall-collision ROOT --configuration CONFIG --member appearance/thm_must_mining_outpost.pob --expected-sha256 POB_SHA`, saving output under ignored evidence. Run the original-hall verifier with ROOT, CONFIG, that output and the pinned configuration hash. All asset-derived coordinates/source identities in the probe output remain private.

## Confidence, limits and next action

**Confirmed by execution and independent original parsing:** this replacement body/floor collision slice and its explicit clearance. Original actor selector, tolerances and collision ordering remain Unknown. The Godot actor has not yet adopted triangles; no visual host claim.2,599 names,345 packets and research scores89%/94% unchanged.

Next: conservative static-triangle acceleration with explicit candidate/work budgets, validated against exhaustive contact. Then accept both20,501-triangle room sets and compose portal/door movement without truncating original obstacles.
