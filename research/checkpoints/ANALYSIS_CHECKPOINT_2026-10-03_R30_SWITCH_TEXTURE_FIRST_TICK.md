# Round 30: SWTS fresh registration and first material query

Date: 2026-10-03. Question: does the first eligible object-clock service
necessarily deliver elapsed time, and can a mission-terminal SWTS advance before
its first draw? Stop when construction, registration, scheduler comparison, and
default material-query relationships are established and the remaining ordering
gap is precisely identified. Static analysis only.

## Inputs and tools

- Baseline executable SHA-256:
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
- Round 30 reviewed snapshot: 2725 manifest files, snapshot HEAD
  `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`.
- Archive SHA-256:
  `58909621e901a194eb73ba54011b51a45399e819fe57128f3234e51af27d3c0a`;
  manifest SHA-256:
  `ba8f50e9b3c9fb5b378e5136bbbbfc0bcd14c2a9088cc401cef04d1ad90ded48`.
- Python, cached pefile/Capstone, repository range disassembler, prior Round
  27/28 static exports/call maps, and curated name registry. No downloaded tools.
- [Own static verifier](../../poc/round30-tests/Rendering/Verify-SwitchFirstTick.py)
  rejects executable identity mismatch before PE loading or VA use. Detailed
  range receipts and exploratory output remain private and are not linked here.

All archive member identities, sizes, hashes, target paths, and ancestor
symlink/reparse containment were checked before overlay. Worker HEAD and empty
index were preserved. The worker overlay is not a commit of the snapshot HEAD.

## Findings and evidence

The [durable contract](../../docs/SWITCH_TEXTURE_FIRST_TICK_CONTRACT.md) records
the observations separately from interpretation and implementation consequences.

1. **Confirmed:** `00B21B80` starts with no clock record. Registration
   `00B40070` passes current global tick words into lazy record creation
   `00B24670`; `00B57190` stores them at record `+00/+04`. An existing record
   bypasses creation, so repeated registration does not reset the tick.
2. **Confirmed:** registration queues through `00B40A80`; it does not directly
   invoke object alter. `00B40ED0` skips its virtual callback when last/global
   ticks are equal. Otherwise it writes the current tick and delivers its
   binary32 elapsed delta. A fresh object selected after a normal subsequent
   tick advance has positive elapsed time, under monotonic non-wrap conditions.
   Queue admission itself does not guarantee such a callback.
3. **Confirmed:** scene insertion `00561230` calls world entry then clock
   registration. Appearance assignment/owner attachment are separate. Object
   destructor frees the record; queue removal is a distinct operation.
4. **Confirmed alternate path:** child alteration synchronizes its tick through
   `00B40A50` then receives the parent's delta (`00B231ED/00B231FD`). Do not
   generalize the top-level same-tick suppression to every direct child call.
5. **Confirmed:** Mesh primitive-set creation depends on template `+38` being
   ready. Deferred completion calls `00793360` at `00796822`. Ready set creation
   reaches local primitive shader fetch `007707A0`, SWTS template factory
   `0077EBF0`, and runtime construction `00785B00`.
6. **Confirmed default query:** Mesh enqueue -> sorter -> callback `00773F50`
   -> primitive `0077E370` -> SWTS `00785C30` -> selector `00B5AD80` reads the
   current index. The query does not advance DTST. Selector constructor starts
   at index/residual 0; elapsed update may precede a later draw.
7. **Unknown:** actual first visible index of an original mission-terminal
   instance. Deferred completion's thread/frame phase, first eligible update,
   first enqueue, pause/culling, alternate owners, and callback overrides are
   not closed. No runtime observation is claimed.

This closes the bounded structural question, not runtime first-frame ordering.
There is no new corpus count, packet/protocol claim, coverage percentage, engine
implementation, or canonical function rename. Suggested names only (if later
accepted and reconciled): `object_ensure_clock_record_VA_00B24670`,
`object_clock_record_construct_VA_00B57190`,
`object_clock_register_VA_00B40070`, and
`scene_register_object_clock_VA_00561230`. The curated registry is unchanged.

## Reproduction and validation

Run with the documented baseline executable and a directory containing cached
`pefile` and `capstone`:

```powershell
python .\poc\round30-tests\Rendering\Verify-SwitchFirstTick.py --exe <baseline-exe> --packages <cached-packages> --receipt .\local-private\round30\static-receipt.json
python .\tools\python\validate_organized_workspace.py .
git diff --check
```

Use a fresh private receipt path. The verifier checks 33 bounded ranges,
72 exact instruction sites, and seven RTTI/vtable slots. Wrong identity and a
truncated synthetic input must fail before VA interpretation. These are static
identity/instruction checks, not execution tests of scheduler timing.

## Resume point

Trace the caller/owner of deferred mesh-template completion around
`00796809/00796822` and establish its thread/frame phase. Then join a specific
ordinary GroundScene object's construction and `00561230` registration to its
first ready-set enqueue. Until then preserve the conditional outcomes in the
contract and leave the actual first-visible index Unknown. No further rendering
milestone is undertaken in this round.
