# R29 Runtime: standalone safe shader object clock

2026-10-03. Question: can ordinary scheduler seconds/quantization/accumulated object service be implemented with explicit modern arithmetic, bounded registrations and atomic checked counters, without integrating scene/cursor or claiming original precision parity? Stopping condition: standalone clock, immutable/current receipts, independent bit/tick oracle, negative/lifecycle cases and exact scope validation.

## Inputs and identity

Supplied Round29 snapshot contains2,695 public files. Manifest SHA-256 `c43768a1a852406dc18ef4dfed27b66126c72355361c8364295efc638225897b`; archive `268128b23b05c8e77c2ce359e957057b303f3f920362f24ad879f189ffbb3d22`. Full archive identity/member names/unique normalized containment/every size/hash and archive/target/all ancestor symlink/junction checks preceded overlay. Only snapshot-listed bytes were written, and all2,695 postcopy hashes matched. HEAD/index and unlisted/private files were preserved. Checkout HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`.

Orientation: START/latest accepted28, status/ledger/unknown and canonical terminology, [accepted ordinary clock contract](../../docs/SWITCH_TEXTURE_CLOCK_CONTRACT.md), existing runtime cursor, relevant numeric/camera-clock tools. No executable VAs or original bytes were used; prior static research is correlation evidence. Build uses cached .NET/Core dependencies, task APPDATA and own no-source NuGet.Config; oracle uses bundled Python/PYTHONUTF8=1. No downloads, wallclock/timer, original execution, live contact, assets/archive loading or main-checkout writes.

## Evidence, confidence and implementation consequence

Confirmed implementation: [durable contract](../../docs/SHADER_OBJECT_CLOCK_CONTRACT.md) defines double seconds*1000 then truncate/min1, finite/exclusive2^64/config-cap preflight before ulong cast, checked monotonic ticks/revisions/registration tokens, and double(tick difference)*float32 inverse then float32 elapsed storage. Preparation includes bounded map/candidate allocation before assignments. Failed calls preserve exact global/object receipts. Registration at current tick and reset preserving tick/token counters are explicit modern policy; no legacy initialization semantics are inferred.

Confirmed freshness: global mutations supersede global receipts; Advance stales object current receipts but retains their last-service facts. Object current status depends on exact owner/stored identity and ThroughTick equal to global tick, not unrelated same-tick global revision. Same-tick Service returns the exact old receipt and elapsed value; no new delivery or revision. Native identity checks and independent row comparison establish this boundary. Consumers must not replay the old nonzero elapsed value.

Confirmed fixture arithmetic:390 native checks/36 cases, all157 independent Python operations agree. Twenty-two same-tick cases, seven invalid/capacity/range failures, six argument failures, eight checked overflow failures, and five closed writes are covered; malformed constructors are separate. Large exclusive-bound neighbor accepts18,446,742,802,399,232,000 ticks, rounded threshold/above reject safely. A configured ulong cap that rounds upward as double is separately checked before cast and rejects its one-tick-too-large candidate. Two0.0015 advances yield2 ticks versus one0.003 yielding3. Float320.005 yields4 ticks under chosen double multiplication; an initial diagnostic assumption of5 was corrected after the independent exact comparison, with no production change.

Modern policy, not parity: seconds input/quantization/accumulation is correlated with accepted ordinary-time static evidence. Original PC24/FISTP precision, invalid/overflow/wrap and first-registration policy remain unclaimed. Class metadata explicitly reports OriginalPC24Parity=false. No scheduler callbacks or historical implementation path (omitted) integration are added.

## Reproduction and validation

Own runner (external historical reference) builds offline with cached packages and flags NuGetAudit=false, UseSharedCompilation=false, UseAppHost=false. Build passes zero warnings/errors.

```text
dotnet <Runtime.dll> poc/round29-tests/Runtime/ordered-fixtures.json <clock-receipt.json>
python poc/round29-tests/Runtime/verify.py <clock-receipt.json>
```

The oracle uses Python integer truncation/range/sums and float32 bit conversion over independent synthetic input bits; every global/object/current/delivery row is compared. It does not execute production code as its reference. Native tests include capacities/duplicates, missing noops, divergent/skipped service, revision/tick/token overflow, registration at now, remove/recreate/reset, foreign owners, exact immutable receipts and closed cleanup. All failure references remain stable. No broad unchanged historical implementation path (omitted) tests are warranted for an additive isolated class.

Workspace validator, new-document relative links, git diff --check, UTF-8/LF/trailing-whitespace and exact scope audits pass. Focused snapshot diff and git status inspected. Verified scope: four allowed new artifacts plus five own Runtime29 runner files; all2,695 baseline files unchanged. Private logs/receipts/build output remain ignored. Lead owns canonical status/session reconciliation outside this allowlist. No staging, commit, push or publication.

## Resume

Lead reviews standalone policy and can integrate a separately bounded clock/cursor actor, using receipt identity to suppress same-tick delivery and explicit registration/resource lifecycle. Verify accumulated seconds/selected-frame boundaries and cleanup. Keep original first-registration and PC24 parity questions separate. Worker stops pending acceptance.
