# Replacement launch profile and preparation

The engine-neutral profile codec (external historical reference)
and preparation owner (external historical reference)
assemble explicit installation inputs and the accepted
[installed creature preparation](INSTALLED_CREATURE_PREPARATION_CONTRACT.md).
They require no historical argument vector or manually supplied hashes. This is
asset launch preparation, not a network/session launcher or proof of live interoperability.

## Frozen API

```csharp
public sealed record ReplacementLaunchProfile(int Version, string AssetRoot,
    string ConfigurationPath, string ObjectPath, InstalledAnimationChoice Idle,
    InstalledAnimationChoice Walk, InstalledAnimationChoice Run);
ReplacementLaunchProfileCodec.Decode(ReadOnlySpan<byte> json);
ReplacementLaunchPreparation.Open(ReplacementLaunchProfile profile);
```

`ReplacementLaunchPreparation` owns the opened `OriginalInstallationInputs` and
implements idempotent Dispose. Its `Inputs` property is a guarded borrowed service:
callers must not dispose it or retain it beyond the owner lifetime. `Creature` is
historical immutable catalog/options/source metadata. `Profile` is a defensive
immutable copy with read-only route snapshots. Those two metadata properties remain
readable after disposal; access to `Inputs` rejects after owner disposal. Previously
borrowed installation services also reject lookups after owner disposal.

Open validates manually constructed records as well as decoded profiles, copies
all three route collections before archive I/O, opens the installation, requires
the explicit root object to be present, and derives creature pins/CRC/source
identities through the existing preparer. If preparation or root admission fails,
the newly opened installation is disposed. Callers must await all borrowing
world/presentation jobs before disposing this owner. Concurrent mutation/disposal
and loading are outside this contract.

## Exact JSON schema

All names are PascalCase and case-sensitive. Root fields are exactly `Version`,
`AssetRoot`, `ConfigurationPath`, `ObjectPath`, `Idle`, `Walk`, `Run`; all are
required. Each choice is an object with required `TablePath`, `LogicalName`, and
optional `ExplicitChildRoute`. Unknown and duplicate properties reject at either
level, including escaped aliases for the same property name. Credential, endpoint,
token, and other extra fields are rejected rather than retained.

Version is integer 1. Path/name values are JSON strings. Route values are omitted,
null, or integer arrays with at most 32 elements, each 0 through 4,095. Numeric
fields use Int32 JSON integer parsing; quoted numbers, fractions, booleans, null
elements and out-of-range values reject. JSON input is 1 through 65,536 UTF-8 bytes,
with maximum parser depth eight; invalid UTF-8, comments, trailing commas, malformed
JSON and non-object roots reject. The codec does not perform filesystem I/O.

Physical installation/config paths use the existing explicit local-path lexical
policy: fully qualified, at most 4,096 characters, without UNC/device/URI,
drive-relative, traversal/dot segments, control/NUL, extra colon or surrounding
whitespace. Open applies the existing filesystem existence/reparse/config/archive
admission through [original preflight](ORIGINAL_INSTALLATION_PREFLIGHT_CONTRACT.md).
Resource paths are bounded 1,024-character printable ASCII relative paths without
empty, dot/traversal segments, colon, control/NUL or surrounding whitespace. Object
paths use `object/*.iff`, table paths `appearance/lat/*.lat`; legacy ASCII case and
internal-backslash normalization apply. Logical names use the existing bounded
ASCII policy, with empty/space-only and `default` selections rejected.

The codec's route budget of 32 is the profile-envelope limit. Open retains the
existing installed preparer's tighter eight-level route limit and actual counted
branch checks. Codec admission therefore does not imply installed-resource success.
No parser redesign or relaxed branch selection is introduced here.

This independently authored example uses placeholders; replace both physical
paths with explicit local installation paths before decoding:

```json
{
  "Version": 1,
  "AssetRoot": "<absolute-local-installation-root>",
  "ConfigurationPath": "<absolute-local-configuration-file>",
  "ObjectPath": "object/creature/player/shared_human_male.iff",
  "Idle": {
    "TablePath": "appearance/lat/all_m.lat",
    "LogicalName": "emt_standing_idle_ambient_1_ag"
  },
  "Walk": {
    "TablePath": "appearance/lat/all_m.lat",
    "LogicalName": "loop_standing",
    "ExplicitChildRoute": [0, 1]
  },
  "Run": {
    "TablePath": "appearance/lat/all_m.lat",
    "LogicalName": "loop_standing",
    "ExplicitChildRoute": [0, 2]
  }
}
```

Those explicit modern animation choices are verified accepted human policies.
The preparer discovers hashes from the original configured overlay, including
all declared LATs and selected clip bytes. Discovery is not an immutable archive
snapshot or a claim about arbitrary avatars/customization. Sources record the
preparation slice; full geometry/material/animation loading remains the existing
router's separate step. No proprietary bytes are serialized by this component.
Profiles contain physical paths and should be persisted privately by the caller.

## Reproduction and verification

The self-contained test project (external historical reference) uses the
sealed Round84 authored fixture producer and original hash oracle. Default tests
need no original inputs. The required CLI is:

```text
python poc/round88-tests/Assets/peer.py generate FRESH_PRIVATE_DIR
dotnet build poc/round88-tests/Assets/Assets.csproj <offline-build-options>
dotnet <built-Assets.dll> FRESH_PRIVATE_DIR/fixtures.json FRESH_PRIVATE_DIR/receipt.json
python poc/round88-tests/Assets/peer.py verify FRESH_PRIVATE_DIR
```

The producer optionally accepts `--asset-root ABS --config ABS` together. It writes
the generated profile/expected identities only under a fresh ignored `local-private/`
directory, before native execution. Original input admission and expected identity
derivation use the accepted84 oracle; no original bytes are exported.

The cache-only runner executes this sequence and default Core smoke:

```powershell
& ./poc/round88-tests/Assets/Invoke-Launch.ps1 `
  -Python '<cached-python.exe>' -Dotnet '<cached-dotnet.exe>' `
  -CachedPackages '<existing-private-package-cache>'
```

Optional original mode supplies `-AssetRoot 'C:/Stardust-DEV'` and
`-Config 'C:/Stardust-DEV/swgemu_live.cfg'`; optional `-RunRoot` selects a fresh
absolute private output directory. Package sources are cleared; existing cached
packages and build output are isolated privately.

Observed final results: 109 malformed-profile rejections, 140 total rejections,
17 exclusive-file cleanup checks, exact 65,536-byte JSON admission and over-limit
rejection, codec route-32 admission and Open's inherited route-nine rejection,
manual-record validation, path aliases, caller-route mutation isolation, immutable
route access, historical metadata after disposal and borrowed-service rejection.
Missing/masked root/parent/SAT/LAT/clip, malformed family/table, cycle, ambiguity,
undeclared table and inherited byte-budget failures all release the opened archive.
Synthetic-only and explicit original-human preparation independently match source
identities and derived catalog/clip pins. Builds and default Core smoke pass with
zero warnings/errors. These are **Confirmed fixture/component observations**.

Round88 adds no new proprietary corpus or SHA fixture and claims no GPU, session,
world-job, live-server or original-executable execution. Full launch/session/world
assembly is the lead's next integration action: open this owner, pass its borrowed
installation and immutable creature catalog/options into existing loaders, drain
all jobs, then dispose. Preserve the distinct original-preparation, scene and
presentation validation gates.
