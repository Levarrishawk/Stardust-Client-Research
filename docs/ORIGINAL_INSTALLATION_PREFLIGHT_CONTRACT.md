# Explicit original-installation preflight

Round83 provides an engine-neutral, read-only installation admission component in
[OriginalInstallationInputs.cs](../../SWG-Godot-Client/poc/Stardust.Core/Configuration/OriginalInstallationInputs.cs).
It accepts three explicit inputs: a local asset root, a local configuration file,
and a bounded list of logical resources. It does not depend on preview profiles,
historical command vectors, or test fixtures.

## API and ownership

```csharp
using OriginalInstallationInputs inputs = OriginalInstallationInputs.Open(
    assetRoot, configurationPath, new[] { "object/creature/player/shared_human_male.iff" });
inputs.RequireAllPresent();
OriginalInstallationReceipt receipt = inputs.Receipt;
ConfiguredTreOverlay borrowed = inputs.BorrowedOverlay;
// Loaders can use borrowed during inputs' lifetime. The owner disposes it.
```

The caller owns the returned disposable session. Its overlay is borrowed: callers
must not dispose it or retain it past the session's lifetime. Dispose is idempotent;
owner access and admission reject use after disposal, and overlay lookups reject
use after owner disposal. Receipt remains readable after disposal. Resource rows
are copied into a read-only list; later mutation of the caller's input list cannot
change it. Concurrent disposal and loading are not a supported usage contract.

`Open` returns metadata for missing and masked resources; `RequireAllPresent()`
is the explicit launch admission step. Failure of admission does not destroy an
otherwise valid session. Invalid paths, configuration, archives, or payloads fail
opening. An overlay opened before a resource-read failure is disposed on that
failure. Existing overlay/archive failure cleanup remains in effect.

## Accepted inputs and bounds

- Physical paths must be fully qualified local filesystem paths, at most 4,096
  characters. URI, UNC/device, drive-relative, control/NUL, dot/traversal-segment,
  extra-colon, and surrounding-whitespace inputs are rejected.
- Existing root, configuration, configured archive paths, and their ancestors
  must not be reparse points. This is lexical/filesystem admission, not a security
  boundary against concurrent filesystem replacement. Use stable local inputs.
- Configuration is strict UTF-8 with an optional UTF-8 BOM, at most 65,536 bytes.
  The hash covers original bytes, including a BOM if present. No configuration
  contents or endpoints are stored in the receipt.
- Resource lists contain 1 through 128 unique normalized paths. Paths are at most
  1,024 printable ASCII characters, relative, with no empty, `.` or `..` segment,
  colon, control/NUL, or surrounding whitespace. Internal backslashes and uppercase
  ASCII normalize through the existing legacy path normalizer. Duplicate normalized
  requests are rejected before archive I/O.

The existing [ConfiguredTreOverlay](../../SWG-Godot-Client/poc/Stardust.Core/Assets/ConfiguredTreOverlay.cs)
is authoritative for `searchTree_00_` mount parsing, descending numeric priority,
TREE 0005 lookup, and tombstones. A preliminary scan admits only local mount
filenames and checks their reparse status; it does not implement priority or asset
lookup. The overlay accepts at most 256 unique priorities. Only `searchTree_00_`
declarations are expanded. Config includes, other searchTree families, loose files,
and TOC mounts are unsupported. Include-only configuration fails for lack of mounts.
Other config directives are ignored rather than interpreted as a launch contract.

Archive parsing retains existing bounds: one million records, 64 MiB index blocks,
and 256 MiB per decoded member. The preflight reads requested payloads sequentially
to hash them, then retains only metadata. It does not extract files or prove a small
aggregate archive-index memory footprint.

## Receipt semantics

`OriginalInstallationReceipt` contains configuration SHA-256 (uppercase hex), byte
count, mount count, immutable ordered resource rows, and `AllResourcesPresent`.
Each row contains normalized logical path, `TreLookupKind`, winning priority,
decoded byte count, and decoded-byte SHA-256. Present rows have byte count/hash;
masked rows have winning priority and null byte count/hash; missing rows have null
priority, byte count, and hash. A tombstone prevents lower-priority fallback.
No payload bytes, physical paths, config text, or endpoints are in the receipt.
The borrowed overlay still exposes existing payload-reading APIs to later loaders.
Application code owns any optional receipt persistence and should keep installation
receipts private.

## Reproduction and evidence

The self-contained suite lives in [round83 Assets](../../SWG-Godot-Client/poc/round83-tests/Assets/Assets.csproj).
Its independent Python writer constructs synthetic TREE records and computes CRCs,
hashes, and expected metadata before native execution. It does not call the production
writer or copy original assets into fixtures.

```powershell
& ./poc/round83-tests/Assets/Invoke-Preflight.ps1 `
  -Python '<cached-python.exe>' -Dotnet '<cached-dotnet.exe>' `
  -CachedPackages '<existing-private-NuGet-cache>'
```

The runner requires a fresh ignored directory under `local-private/`, copies only
existing cached packages, clears NuGet sources, and runs the default Core smoke
suite. Add `-RunRoot '<absolute-fresh-local-private-directory>'` to select output.
The default is synthetic only. For the explicit read-only installation check add:

```powershell
  -AssetRoot 'C:/Stardust-DEV' -Config 'C:/Stardust-DEV/swgemu_live.cfg'
```

The optional original check requests only
`object/creature/player/shared_human_male.iff`, independently compares its known
hash, size, winning priority, config hash, and mount count, and writes metadata
only under the private output directory. No original executable is run.

For regression integration independent of the runner:

```text
python poc/round83-tests/Assets/peer.py generate <fresh-absolute-private-fixtures>
dotnet build poc/round83-tests/Assets/Assets.csproj <offline-build-options>
dotnet <built-Assets.dll> <fixtures>/fixtures.json <fixtures>/receipt.json
python poc/round83-tests/Assets/peer.py verify <fixtures>
```

Optional producer arguments are `--asset-root <root> --config <file>`; both must be
provided together. Native Program accepts exactly two arguments shown above.

Observed validation: synthetic priority override, masked fallback prevention,
missing status, normalization, immutable receipt, explicit admission, both exact
input-limit boundaries, 51 rejection checks, and four exclusive-file cleanup checks
(normal/idempotent disposal, failed compressed payload, failed later archive, and
successful admission). Independent metadata/hash comparison and read-only original
player-object preflight passed. Core build/default smoke passed without compiler
warnings or errors. These establish component behavior with **Confirmed** fixture
evidence, not new recovered executable behavior.

Dependency closure, terrain/appearance/player readiness, config includes, scene
creation, networking, and a full launcher remain **Unverified** here. The next
action is for launch composition to select its explicit required logical resources,
call `RequireAllPresent`, and keep the session alive for its loaders. Presence and
hash agreement do not establish semantic validity or transitive dependencies.
