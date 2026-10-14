# Authored offline scenario admission

## Scope and public API

OfflineScenarioManifest (external historical reference) is an engine-neutral immutable compiler for a modern synthetic configuration. It does not recover legacy configuration or protocol behavior and does not implement transport, factories, asset loading, or a launcher.

```csharp
using Stardust.Core.Offline;
OfflineScenarioManifest manifest = OfflineScenarioManifest.Parse(utf8, manifestDirectory);
int runs = manifest.RunCount;
string[] arguments = manifest.ArgumentsForRun(oneBased);
```

`Parse(byte[] utf8, string manifestDirectory)` accepts at most 262144 UTF8 bytes, with JSON depth at most 4. Strict UTF8 validation precedes JSON parsing; comments and trailing commas reject. Duplicate keys are rejected recursively, including escaped spellings that decode to the same name. Unknown/missing keys and nonexact types reject. Null bytes throw ArgumentNullException; other invalid admission throws InvalidDataException. Diagnostics do not print input paths or contents. The supplied directory must be fully qualified, local and existing; it never silently derives from cwd.

The input byte array and parsed JSON objects are not retained. All stored paths and booleans are copied into private arrays. `ArgumentsForRun(int oneBased)` accepts 1 through RunCount, throws ArgumentOutOfRangeException otherwise, and returns a newly allocated array. Mutating returned arguments or original bytes cannot alter future arguments. Filesystem existence is checked at admission, not held open or rechecked by this accessor; later removal/replacement remains a launch-time concern.

## Schema1 exact shape

The root has exactly `Schema`, `RunCount`, `Inputs`, `Options`, `PointTargetProfiles`. Schema is integer 1; RunCount is integer 1..32. Numeric fractions, exponent spelling, strings and booleans are not integer coercions. PointTargetProfiles is an array of strings whose length equals RunCount.

Inputs has exactly these required keys, in compilation order:

1. asset-root
2. asset-config
3. mixed-peer
4. catalog-profile
5. motion-peer
6. face-profile
7. customization-peer
8. customization-dictionary
9. customization-asset-context
10. customization-name-context
11. tangible-peer
12. tangible6-peer
13. creature6-full-peer
14. creature4-partial-peer
15. terrain-peer
16. terrain-material-profile

Options has exactly `local-input`, `customization-wire`, `customization-scalars`, `customization-full`, `customization-extended`, `customization-named`, `terminal-animation`, `terminal-clock`, in that order. Every option is a strict JSON boolean. True emits its flag; false omits it.

Each run compiles all sixteen `--key`, resolved-path pairs, then `--terrain-point-target-profile`, that run's resolved profile path, then true option flags in the declared order. No arbitrary arguments or override field exists. The lead's entry integration must keep raw override arguments separate from manifest admission.

## Paths and effects

Path strings are nonempty, at most 4096 UTF16 code units, and contain no NUL. Resource/network URI schemes, extra colons, drive-relative forms, UNC/network/device prefixes reject before existence I/O. Fully qualified local Windows drive paths are allowed. Relative paths, including parent segments, resolve through `Path.GetFullPath(value, manifestDirectory)`; no sandbox containment inside that directory is promised. The supplied asset-root must exist as a directory. Every other input and each profile must exist as a file. These are metadata checks, not asset decoding. Spaces and shell punctuation remain ordinary argument-array characters; no command interpretation occurs.

Actual Version13 profile semantics remain with the existing child guard. The compiler does not inspect profile bytes, reseed identities, validate protected packet frames, create a socket, or retain world/session/node references. Symlink targets and launch-time file changes are not pinned by this configuration contract.

Resolved full paths must also remain at most 4096 characters, preserving the launcher's argument limit even when the manifest directory enlarges a relative path. This limit is checked before the file/directory existence lookup.

## Validation and reproduction

Confirmed on 2026-10-06: the standalone test project (external historical reference) passes 28 positive checks and 90 rejection cases; full Core and standalone test builds pass with zero warnings/errors. Tests use authored empty files and a directory, not proprietary assets. They cover order, mixed/all flags, run32, defensive copies, absolute and relative paths, cwd independence, inclusive byte cap, missing keys/files, unknown/case/duplicate keys, recursive duplicates, malformed UTF8/JSON/depth, invalid scalar types, paths and indices.

Example explicit offline commands, using isolated private outputs and existing exact cached packages:

```powershell
$env:APPDATA = '<private-appdata-with-NuGet.Config>'
$env:DOTNET_CLI_HOME = '<private-dotnet-home>'
$env:NUGET_PACKAGES = '<existing-offline-package-cache>'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
dotnet build poc/round82-tests/Runtime/Runtime.csproj --artifacts-path <private-artifacts> --configfile poc/round82-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet <private-artifacts>/bin/Runtime/debug/Runtime.dll <fresh-absolute-private-test-directory>
dotnet build poc/Stardust.Core/Stardust.Core.csproj --artifacts-path <separate-private-core-artifacts> --configfile poc/round82-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
```

The first build attempted the inaccessible user NuGet configuration; redirecting APPDATA to the existing private offline configuration resolved it. No network restore was used. The test runner refuses a preexisting directory and creates only authored files; callers should use a fresh path under the checkout's ignored private directory.

Next action: lead integrates this API before any preview factory, supplies producer-authored scenarios, and independently replays actual full resources. No native launcher integration or resource replay is claimed by this parser-only task.
