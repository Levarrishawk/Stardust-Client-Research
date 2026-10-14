# R30 Runtime: clock-to-cursor delivery adapter

2026-10-03. Question: can bounded once-only per-object clock service drive accepted local cursors without modifying clock/cursor/world code and without fake rollback? Stopping condition: standalone ownership/current API, explicit retained-versus-terminal failure policy, independent bit/tick/frame oracle and scope/validation handoff.

## Inputs and provenance

Round30 snapshot2,725 files. Manifest SHA-256 `ba8f50e9b3c9fb5b378e5136bbbbfc0bcd14c2a9088cc401cef04d1ad90ded48`; archive `58909621e901a194eb73ba54011b51a45399e819fe57128f3234e51af27d3c0a`. Full archive/member uniqueness/names/size/hash, containment, archive symlinks and target/all-ancestor symlink/junction checks preceded listed-byte overlay. All2,725 postcopy hashes matched. Unlisted/private bytes and HEAD/index were preserved; checkout HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`.

Orientation read START/latest accepted29, status/ledger/unknown terminology, relevant durable clock/cursor contracts and checkpoints/tools. No executable addresses/original bytes were used. Tools: cached offline .NET/Core packages, isolated task APPDATA/no-source NuGet, bundled Python with PYTHONUTF8=1. No downloads, original execution, live contact, assets/archive access or main-checkout changes.

## Findings and confidence

Confirmed implementation: [contract](../../docs/SHADER_TEXTURE_PLAYBACK_CONTRACT.md) and adapter (external historical reference) define owned bounded clock/cursors, modern seconds/registration binding, checked adapter revisions, immutable/current publication, explicit global Advance and per-object Service. Same-tick service returns exact existing publication and never redelivers historical nonzero elapsed. Advance preserves last-service/cursor facts while invalidating freshness; divergent objects accumulate independently. Remove/reset/close deterministically retire owned registrations/cursors.

Confirmed failure boundary: accepted clock/cursor APIs remain unchanged. Lead accepted explicit terminal fault after successful underlying registration/service consumption. Cursor catch-up and cursor revision failures close/drain the clock and every cursor, invalidate all publications and make the adapter terminal. Clock diagnostic retains consumed revision/tick; adapter revision stays last success. Pre-consumption clock/adapter/construction failures retain exact state. This is a modern retirement policy, not atomic rollback, original behavior, or a claim that allocation exhaustion is dynamically tested.

Confirmed native147 checks/14 cases and independent79 operation comparisons. Two consumed-service failure cases terminally retire all objects; clock registration count0/adapter registry0/publication freshnessfalse are executed assertions, and source close loop was inspected for each cursor. Same-tick nonzero elapsed/wrap/residual, divergent/skipped delivery, zero/minimum tick, removal/re-register/reset, capacity/invalid input, clock/token/adapter/cursor overflow, owner and close are covered. No numerical mismatches or build failures occurred in these required gates.

Unknown/deferred: original PC24/FISTP parity/first-visible behavior and combined prepared clock/cursor transactions. Engine/resource actor integration remains lead-owned. No research/function/protocol coverage denominator changes.

## Reproduction and validation

Own runner (external historical reference) uses cached packages and NuGetAudit=false, UseSharedCompilation=false, UseAppHost=false. Exact commands from repository root:

```text
dotnet restore poc/round30-tests/Runtime/Runtime.csproj --configfile poc/round30-tests/Runtime/NuGet.Config --packages <cached-package-directory> -p:NuGetAudit=false
dotnet build poc/round30-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round30-tests/Runtime/bin/Debug/net8.0/Runtime.dll poc/round30-tests/Runtime/ordered-fixtures.json <playback-receipt.json>
python poc/round30-tests/Runtime/verify.py <playback-receipt.json>
```

Build zero warnings/errors; native/independent oracle pass. Private logs are restore.txt, build-final.txt, playback-native.txt, playback-receipt.json and oracle-final.json under this round's ignored evidence directory. Workspace validator, relative links, git diff --check, UTF-8/LF/trailing whitespace and scope audits pass. Generic Windows reparse attributes are absent on all inspected final targets/ancestors. Focused diff and git status inspected. Verified scope four allowed additions plus five own runner files; all2,725 baseline files remain byte-identical and no shared canonical files change. Generated products remain ignored; no Git staging/commit/push/publication. No optional expanded testing after required gates.

## Next action

Lead reviews terminal-retirement policy and can bind current immutable frame selection to a separately validated actor/resource lifetime. Do not retry consumed elapsed after fault; create a new owner. If retained atomic multi-owner delivery becomes required, request exact scope for prepared clock/cursor transaction APIs in a subsequent milestone. Worker stops for acceptance.
