# Flora residency, construction, and randomness

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Dynamic radial residency

Confirmed behavior from `ClientRadialFloraManager` / `ClientDynamicRadialFloraManager` vtables and functions `0x0085AF20`, `0x0085B880`, `0x00858AB0`, and `0x0085B2B0`:

- Initial coverage is a jittered square lattice clipped to a circular radius. Lattice spacing, per-cell inset and radius are stored independently. Each candidate gets two legacy-RNG uniform samples across `spacing - 2*inset`.
- The manager retains fixed 16-byte residency records containing a node pointer, horizontal position and flag byte.
- Camera motion does not destroy and recreate the grid. A node farther than the configured radius is wrapped by the displacement from the previous coverage center to the current center, then re-evaluated.
- Bit 1 of the record flags is the active/generated state. Moving out or failing the flora-family query disables the node; a successful query reactivates it. Bit 0 tracks a second per-location classification returned through the manager callback.
- Active nodes are height-adjusted against terrain and have orientation reconstructed from the local terrain normal. A family/node virtual can opt into a distinct height-query branch.
- A global flora-enable/config change walks all records and invokes their configuration callback; a radius change rebuilds coverage.
- Destruction clears every node and the coverage structures; this is not time-based eviction.

## Dynamic node random phase

Superseding raw-register analysis in `ANALYSIS_CHECKPOINT_2026-09-21_STATIC_FLORA_RNG_CLOSURE.md` corrects this paragraph: `0x00AB8E10` rotates the node transform by yaw `x + z`; it is not an RNG seeder. Node `+0x48` then receives a normalized sample from the shared RNG object at `0x01947188`, so phase is not independently position-seeded.

The accumulated animation clock remains a double advanced by frame delta (`0x008576A0`). Prepared instances are sorted, partitioned into groups of at most 500, wrapped in a flora shader primitive, and submitted through the terrain render phase (`0x008576C0`). The 500 limit is an implementation batch cap, not an asset constraint.

## Static non-collidable runtime

`ClientStaticRadialFloraManager` uses the same camera-centered residency concept but owns full appearance objects. The exact fade policy is now confirmed: objects are invisible outside `[minimumDistance, maximumDistance]`; the first and last 25% of that interval use smoothstep ramps, with full opacity through the middle band. A zero minimum distance keeps the near band fully visible. Local collision helpers iterate active flora appearances for client-side picking/effects only.

## Static-collidable object construction

`0x00B5E780` is invoked when a completed ClientChunk is installed (`0x0084A840` and `0x0085EFD0`) and by explicit static-flora region refresh paths (`0x00B5EF90`, `0x00B5F190`). Confirmed sequence:

1. derive a 16-metre cell and reject stale/replaced chunks;
2. take two normalized RNG samples to jitter a candidate inside the cell inset;
3. reject outside world bounds or excluded terrain;
4. query terrain height and static-collidable flora family;
5. reject the manager’s sentinel family;
6. sample family scale when enabled;
7. allocate a client Object and appearance from the selected template;
8. apply random yaw, uniform scale, terrain position, and an auxiliary object component/property;
9. attach to world/render ownership and insert the object into the cell residency map.

The refresh map is keyed by linearized cell coordinates and explicitly destroys prior cell contents before regeneration.

## Randomness compatibility boundary

Superseding raw-register analysis confirms that `0x00B5E780` creates and seeds a private RNG state locally for every cell. The seed is the negated linearized 16-metre cell key. Therefore byte-for-byte static flora placement does not depend on unrelated global generator call order. A replacement has two legitimate modes:

- **strict legacy placement:** reproduce the shuffled Park–Miller generator and per-cell sample order;
- **behavioral placement:** use a deterministic per-cell stream while preserving family weights, density, transforms and exclusion rules.

Original asset compatibility requires the family/query formats in both modes. Pixel-identical vegetation placement requires the strict mode and remains a validation target.

## Confidence

- Residency recycling, flag transitions, radius behavior, height/orientation update, 16-metre static cells, construction sequence, dynamic seed expression, and fade curve: **confirmed by code**.
- Individual unpublished field/method names: **provisional**.
- Exact static-collidable RNG ownership, seed and call order: **confirmed by the superseding closure checkpoint**.
