# R6-NET-01: owned replication envelopes

Date: October2,2026. Question: can exact baseline/delta application envelopes
preserve raw counts/tag/view and owned opaque bytes under strict size/budget
checks without conflating schema parsing? Stop after codecs/independent fixtures.

## Baseline and correction

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`; HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus1855 accepted files.
Manifest SHA256 `3A1E812A4DD0D3DE1C61B8E385A30C7851918C015B7757FE316296DE4BD6D83A`;
ZIP SHA256 `0E55506A789046B8A875688C83285E27E535A51FBE09B2C28E7696C679077453`.
Verified both input hashes/every safe relative path/content before overlay, then
every output hash; ignored/unlisted data preserved and main untouched. Read
START_HERE/latest accepted Round5 report, common replication/count-tolerance and
malformed decode/view6 checkpoints, prior codecs/tools and status/ledger/functions.
Executable SHA256 matched documented baseline before citing existing parser VAs.

Initial assignment27-byte application header was blocked before editing code.
Static BaseMessage constructor proves4 SOE prefix bytes; setSize/close offsets
include them. Lead authorized correction to23-byte application header plus inner
U16 count (minimum25). [Durable contract](../../docs/REPLICATION_ENVELOPE_CODEC_CONTRACT.md)
records exact source symbols/transport extraction and byte-domain distinction.
Historical shared research stays read-only; lead will clarify living docs.

## Observation, interpretation and consequence

Observation: server framed header27 equals4 SOE+23 application; declared size
includes inner count but excludes outer header. Existing original snapshot/delta
counts are consumed without controlling opaque envelope extent. **Confirmed**
existing client/source boundary. Interpretation: DTO can retain raw declared
counts independent of schema payload. Consequence: tags not spelling-swapped,
all views retained, no field validation/readiness inferred. Strict bounds/budget
and managed-array arithmetic are **modern policy**. One-byte opaque delta tail is
valid envelope data, not a claim of valid indexed delta encoding.

## Implementation and checks

One codec/immutable owned DTO file, smoke scenarios/standalone console, independent
Python wire oracle, contract/checkpoint added. Copy getters retain ownership under
input/output mutation. Exact-size checks precede allocation; no schema fields,
registry/transition/transport files changed. Positive/raw count/view/tag cases,
all prefixes/headers/sizes/budgets/kinds/ownership cases included.

Final commands/results appended below. Lead runner call:
`ReplicationEnvelopeScenarios.Run()`. Next boundary: schema/index/value parsing;
no subsequent milestone started or live contact performed.

Final verified commands:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r6'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round6-tests/Networking/Networking.csproj --configfile local-private/round6/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round6-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- --export local-private/round6/envelopes
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_replication_envelopes.py --exports local-private/round6/envelopes
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round6/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Private NuGet.Config clears package sources; accepted cache only. Native raw
count/tag/view/budget/length/kind/ownership scenarios and independent actual-byte/
scalar/opaque output oracle pass. Both builds zero warnings/errors. Validator:
1053 Markdown/zero broken/private links,2602 names/zero duplicates,345 packet
rows. Diff check exits0; baseline conversion advisories saved privately. Broad
shared smoke executable compiled, not run. No field schema, live server, original
exe, GPU/asset/registry/scene execution. No opaque delta validity claimed.

Exact seven new files absent from accepted snapshot:

- `poc/Stardust.Core/Protocol/ReplicationEnvelopeCodec.cs`
- `poc/Stardust.Core.SmokeTests/ReplicationEnvelopeScenarios.cs`
- `poc/round6-tests/Networking/Networking.csproj`
- `poc/round6-tests/Networking/Program.cs`
- `tools/python/verify_replication_envelopes.py`
- `docs/REPLICATION_ENVELOPE_CODEC_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R6_REPLICATION_ENVELOPES.md`

All1855 baseline hashes unchanged; private baseline-relative list records exact
new hashes and scope checker verifies no unowned changes. Main/historical/shared
research and runners untouched. Lead must clarify23 application versus27 framed
bytes in living REPLICATION_SCHEMA_MAP and register the proposed smoke call.
Result ready for review; no staging/commit/publication/message to other workers
or next milestone started.
