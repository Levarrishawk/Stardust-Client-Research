# Round92 explicit bootstrap replay

`replay.py` privately stages the production bootstrap/host and three exactly
pinned sealed Runtime92 sources. It never copies those dependencies into the
public checkout. Explicit absolute paths are required for every tool/input and
a fresh output directory. Existing bundled SDK/NuGet packages are used offline.

Required flags: `--dotnet`, `--godot`, `--packages`, `--asset-root`, `--config`,
`--runtime-root`, `--runtime-pins`, `--output`. The pins file is a JSON mapping
from these three repository-relative paths to lowercase SHA-256:

- `poc/Stardust.Core/Configuration/ReplacementControlledLaunch.cs`
- `poc/Stardust.Core/Session/ReplacementControlledAttempt.cs`
- `poc/Stardust.Core/Session/ReplacementEndpointApproval.cs`

The runner checks that mapping against Runtime92's final handoff and verifies
all input source identities before and after execution. Borrowed dependencies
and the handoff identity are recorded separately from frozen public sources.
The launch JSON is independently authored from the explicit original human
profile and reserved offline peers. Credentials are synthetic native-field
inputs; no credentials are stored in the launch JSON.

Fifteen native bootstrap cases cover success, cancellation, provider failure,
disconnect send error/timeout, movement stop, window close, retained-node removal,
credentials/list/readiness phase stalls, overall timeout, delayed successful and
faulted startup rollback, and faulted acquired-port cleanup. The second character
uses the same supported human CRC but an unapproved object ID. Exact rejection
must preserve the one login acquisition and zero selection generation.
Caller-owned unacquired fake ports are disposed by the harness separately.

Actual protected emissions are compared to independently authored bytes; only
their digests are persisted by the new native harness. Float32 actor positions,
400 ms minimum send cadence, strict launch identity, original preparation and
loaded asset/terrain identities are independently checked. Eight unchanged
Round90 native host cases are replayed with the same modified production host.
Direct scene startup is checked for valid-input refusal and missing/relative/
extra-argument rejection. None constructs a real socket or contacts a server.

`--gpu` additionally runs the success case with a GPU screenshot. Worker
headless receipts explicitly leave GPU and live compatibility unverified.
Keep the scene and Godot context alive until the exact bootstrap completion;
the test quits only after it observes finalization or rollback completion.
The authored minimum flag is not complete terrain/floor/server-state proof.

The [bootstrap contract](../../../docs/REPLACEMENT_CLIENT_BOOTSTRAP_CONTRACT.md)
describes the production boundary. No original executable is run, no toolchain
is installed, and no live traffic is captured by this harness.
