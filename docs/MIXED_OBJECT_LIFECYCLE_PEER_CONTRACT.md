# Independent human/terminal lifecycle peer

Round14-N provides an independently authored deterministic offline peer for two catalog recipes, against the accepted lifecycle, envelope, placement, catalog and registry APIs. It introduces no protocol or world API. All wire messages are synthetic, and TANO3 fields are deliberately opaque synthetic bytes with no recovered schema claim.

## Fixture and API

`MixedObjectLifecyclePeerScenarios.Run(string peerPath, string exportPath)` is a public smoke entry point in `Stardust.Core.SmokeTests`. It accepts one version-1 JSON peer (at most 1 MiB), exports JSON receipts, and closes its registries/transports on success and failure. The standalone [runner](../../SWG-Godot-Client/poc/round14-tests/Networking/Networking.csproj) takes peer JSON and export JSON as its two arguments. The [Python producer/oracle](../../SWG-Godot-Client/poc/round14-tests/Networking/peer.py) handpacks and independently decodes bytes, implements bitwise legacy path checksum, seeded SOE CRC/transform/framing and a separate registry policy model. It never imports production codecs or reads their source.

The generator reads the accepted [catalog profile](../../SWG-Godot-Client/poc/preview-profiles/scene-object-catalog.json), whose SHA-256 is `B945D2D4695FF37D54343F11A7DF3539A5AB36A2E5D13246969B04F92657492D`. Both path checksums are independently verified: human `2937962913`, terminal `2907007746`. Catalog pins remain configuration assertions; this peer neither loads nor verifies original asset content.

Synthetic IDs are human `0xF122334455667788` and terminal `0xFE02030405060708`. Creates are whole 47-byte APPLICATION messages, preserving quaternion X/Y/Z/W, wire position X/Z/Y, template CRC and raw flags (255/165). Finite bit patterns include negative zero and the smallest positive subnormal. Placement receipts preserve all seven raw bits and translation X/Y/Z reordering, using the accepted explicit Reject zero-quaternion policy. Placement normalization is modern presentation policy.

The twelve ordered applications are:

1. Create human, then terminal.
2. Terminal TANO3 baseline, then delta, with raw count 65535. Opaque fields contain a SYNTHETIC marker; baseline application length is 587 bytes and payload fits the explicit 768-byte envelope budget.
3. End-baselines human, then terminal.
4. Contain human in terminal with arrangement `0xFFFFFFFE`; detach human with arrangement `0x80000001`.
5. Destroy human; recreate the identical create; end its baselines.
6. Replay terminal's byte-identical create, proving idempotence and preservation.

The baseline remains opaque and does not mutate the registry. End-baselines means accepted registry finalization only, not resource readiness. Containment invalidates a revision handle while retaining the creation incarnation. Human recreate invalidates its old incarnation and preserves the terminal's exact immutable snapshot. The fixture uses scene generation 42; stale/future generations reject without mutation. Reset to 43 invalidates both old incarnations, retains the monotonic revision counter, and rejects generation-42 replay. New epoch creates/finalization and closure are exported separately. Registry GUIDs are checked for foreign ownership locally and omitted from deterministic receipts.

## Transport and failure

The producer handpacks a synthetic session response: connection `0x12345678`, seed `0xA1B2C3D4`, CRC width 2, selectors 1/4, maximum datagram size 496. Thirteen reliable channel-0 packets include a two-fragment baseline. Sixteen deliveries include corruption, reordering, duplicates and final duplicate replay. Accepted framing/reliability/reassembly produce exactly twelve ordered application records; the independent oracle verifies each datagram, decrypted packet, delivery batch and ACK/out-of-order control. No sockets or live negotiation occur.

Each supported application is checked at every truncated prefix, with appended bytes and all six altered count/opcode bytes: 1,006 rejection probes. Exact payload budget and one-byte-short rejection are checked for the opaque baseline. Input-buffer clearing and opaque-copy clearing prove DTO/envelope ownership.

Ten fresh late-failure cases begin with both valid creates and human containment, then inject truncated create/baseline/delta/end messages, NaN create, unknown CRC create, wrong envelope size or a containment cycle. Rejection leaves the preceding registry snapshots unchanged. The harness then explicitly closes both registry and a transport retaining a future packet, prevents a following end-baselines operation, and rejects transport replay after close. Fatal closure is a modern test harness policy, not an inference about original client recovery. Whole-transcript rollback is not claimed: previously accepted records remain valid observations before explicit closure.

## Evidence, reproduction and next use

Observation: native and independent receipts match exact bytes, raw floats, object IDs/CRCs, message order, revision/creation epochs and failure results. Confidence: Confirmed for this bounded offline implementation behavior. Existing recovered wire evidence remains in the [lifecycle contract](SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md), [replication envelope contract](REPLICATION_ENVELOPE_CODEC_CONTRACT.md) and [transport specification](SOE_TRANSPORT_PROTOCOL.md); registry transitions follow its documented [modern policy](SCENE_OBJECT_REGISTRY_CONTRACT.md). No new executable evidence, names, packet coverage, real occurrence, TANO3 schema or live interoperability is established.

From the repository root, using cached Python/.NET 8 dependencies and a sources-cleared offline NuGet configuration:

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r14'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round14 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round14/NuGet.Config
& $python poc/round14-tests/Networking/peer.py --generate local-private/round14/peer.json --catalog poc/preview-profiles/scene-object-catalog.json
dotnet restore poc/round14-tests/Networking/Networking.csproj --configfile local-private/round14/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round14-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round14/peer.json local-private/round14/export.json
& $python poc/round14-tests/Networking/peer.py --peer local-private/round14/peer.json --verify local-private/round14/export.json
```

Generated peers/exports belong in ignored storage. With the pinned profile and deterministic UTF-8/LF JSON, peer SHA-256 is `6FE67378479CBF7A9D763E2CBBA6D95CB42577C87E71B3EDFC0683E2CF8707A8`. Export keys: CatalogSha256, Transport, PacketsHex, WiresHex, Records (Message/Snapshots), Placements, MalformedProbes, Negatives, Epochs and IdentityChecks. The generator's Applications array can seed future human/terminal world integration directly; opaque records must remain opaque. Lead-owned shared smoke registration may call the entry point above. The next bounded probe is asynchronous mixed-recipe world publication with one-object recreate and stale-incarnation delivery; this peer does not depend on that unfinished API.
