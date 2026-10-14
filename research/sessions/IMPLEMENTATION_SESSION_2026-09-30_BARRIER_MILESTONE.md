# Portal barrier milestone session — 2026-09-30

Continues the [portal transfer milestone](IMPLEMENTATION_SESSION_2026-09-30_PORTAL_TRANSFER_MILESTONE.md). Completed the bounded permission-barrier actor-contact slice and normal door-controller primitives.

Read the [contract](../../docs/PORTAL_BARRIER_COLLISION_CONTRACT.md) and [checkpoint](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_BARRIER_COLLISION.md). Actor bounds can stop before transfer while retaining the source floor owner; changing current permission resumes motion. Unsupported nonlevel requests stop explicitly. Physical leaf clearance remains separate.

Validation: full smoke tests and warning-free build; independent agreement across 291 layouts, 7,642 references and 21,693 geometry/policy probes. Prototype host verifies 12 level links with 24 contact/recovery probes, two explicit nonlevel stops, two-pick actor controls and existing transfer/path/GPU gates. Capture visually reviewed. Four canonical names bring the registry to 2,597; research scores and packet coverage unchanged.

New insight: runtime eligibility uses LeadingByte exclusion and SolidByte support, while WindingByte is separate. The inversion pass belongs only to unsupported PRTO/0002. Permission barrier activity does not read visual opening fraction; raw collision mode 1 has distinct handling.

Next milestone: one original style's contact-triggered motion and physical leaf clearance. Start at style-table loading, `01290560` trigger extent and `012902D0` appearance ownership. Global actor solver ordering remains a research gap. No commits/publication or live-server contact; proprietary detail remains private and unrelated changes remain preserved.

Final workspace gate: 771 Markdown files, zero broken links, zero private-evidence links, 2,597 canonical address rows with zero duplicates, 345 packet rows. `git diff --check` passes; final status was inspected and the staging area is empty. Final headless actor adapter reports 12 checked links, 24 current-policy probes and two explicit nonlevel stops.
