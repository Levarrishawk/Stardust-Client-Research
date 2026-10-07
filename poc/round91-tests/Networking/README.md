# Independent approved-endpoint handoff replay

Synthetic, socket-free Round91 oracle. Python independently authors token,
cluster, status and character records, then applies the accepted Round14 SOE
framing oracle. The accepted Round88 asset producer supplies a synthetic
installation profile. Production codecs do not generate the expected handoff
bytes.

The29 cases cover canonical and case-folded hostname mapping, an explicit IPv4
literal mapping, unrelated ping port, hostname/literal/fixed-peer/zone-port/
galaxy mismatches, duplicate and missing records, malformed operator mappings,
and refusal to substitute an IP literal for an approved hostname. Four valid
cases must produce exact protected authentication and selection frames. All25
rejections must acquire no zone port or emit zone credential/selection bytes.
Mapping preflight rejections also preserve CharacterList, login credentials,
selected identity, endpoint, login ownership and session clock. Duplicate
cluster/status records may fail earlier during accepted login parsing.

## Reproduce

Use the bundled Python and .NET8 with explicit private paths:

```powershell
& $python poc/round91-tests/Networking/fixtures.py --out $private/fixtures.json
& $dotnet build poc/round91-tests/Networking/Networking.csproj --artifacts-path $private/artifacts --configfile poc/round91-tests/Networking/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
& $dotnet $private/artifacts/bin/Networking/debug/Networking.dll $private/fixtures.json > $private/native.json
& $python poc/round91-tests/Networking/verify.py --fixtures $private/fixtures.json --result $private/native.json --out $private/receipt.json
```

Set `NUGET_PACKAGES` to the accepted offline package cache; this configuration
has no package sources. Set APPDATA and DOTNET_CLI_HOME to private directories,
disable telemetry/certificate generation, and retain build/native failures.
The producer requires a fresh ignored output path. Do not use original assets,
credentials or endpoints for this test.

Before integration, Networking borrows exactly Runtime91 application/session/
approval files into an ignored stage with separate hashes. Frozen public Core
is preserved. Such dependency inputs are development evidence until Runtime
seals them; the lead must replay this test against the accepted final files.
Normal reproduction after integration uses the accepted public Core.

Success asserts deterministic port retirement, application drain/finalization,
exclusive archive reopen and redacted errors. It proves a bounded local
advertisement-to-approved-peer correspondence policy, not DNS ownership,
deployed identity, live authorization or interoperability.
