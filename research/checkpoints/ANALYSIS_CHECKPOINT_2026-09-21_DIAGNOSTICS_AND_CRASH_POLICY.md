# Diagnostics and Crash Policy

## Diagnostic layers

The executable exposes and uses distinct channels rather than a single logger:

1. `Report::printf` (`0x00A890E0`) formats ordinary report text.
2. `Warning` (`0x00AA20D0`) formats a warning, increments the warning count, and reports with warning flags.
3. `WarningStrictFatal` (`0x00AA2170`) becomes `Fatal` when strict-fatal mode is enabled, otherwise behaves as a warning.
4. `Fatal` (`0x00AA1D80`, internal `0x00AA1DA0`) builds the global fatal text, records/report it, captures crash information, then either throws `FatalException` when configured or executes a breakpoint/trap path.
5. the unhandled-exception filter (`0x00A9F640`) emits exception code/address text to the debugger, writes a companion text record and minidump, then invokes `Fatal("ExceptionHandler invoked")` under a recursion guard.

`Report` routing at `0x00A88F90` supports registered callbacks, `OutputDebugStringA`, and a main-thread `MessageBoxA`. Fatal reports use the `Fatal Report` caption. The client therefore mixes structured-ish callbacks, debugger output, files and modal UI.

## Configuration contract

`SharedFoundation` diagnostics configuration parsed at `0x00AA7740` includes:

- `noExceptionHandling`;
- individual floating-point traps for invalid, denormal, divide-by-zero, overflow, underflow and precision;
- `fatalCallStackDepth` (default 32);
- `warningCallStackDepth` (default -1);
- `lookUpCallStackNames` (default true);
- verbose warning/hardware flags;
- memory allocation/out-of-memory reporting;
- remote-debug and profiler controls.

`SharedLog` setup at `0x012CCDC0` includes:

- `logNetQueueSize` default 2000;
- `logNetReconnectTimeMs` default 10000;
- `logReportLogs` default false;
- `logReportWarnings` default true;
- `logReportFatals` default true;
- `logStderr` default false;
- repeated `logTarget` entries.

This is evidence for an optional remote/network log sink with bounded queue and reconnect interval, in addition to local report routing.

## Legacy crash-report subsystem

`SetupClientBugReporting::install` at `0x012CE580` installs tool/client crash-report components and registers reverse-order removals in ExitChain. It carries obsolete SOE infrastructure:

- `CrashReporter_r.exe`;
- SMTP destination/default sender settings;
- `swgtoolbugreports@soe.sony.com`;
- `minidump.log` and `SwgClient_*.mdmp` discovery;
- external crash-reporter process startup.

These endpoints are historical and must **not** be reused. Their presence explains the original architecture, not a compatibility requirement.

## Replacement policy

Preserve diagnostic intent while redesigning mechanics:

- structured local logs with severity, subsystem, thread and session fields;
- rotating text/JSON log files plus debugger output in development builds;
- platform minidumps only with explicit user privacy settings;
- no SMTP client and no legacy SOE endpoint;
- assertions/fatals reserved for impossible internal invariants;
- malformed network packets and corrupt optional assets become bounded errors, transactional rejection and usually session disconnect—not process termination;
- required root assets/configuration may stop startup cleanly with a useful error report;
- crash metadata must avoid authentication tokens, passwords and personally identifying chat content.

The old executable's exact modal/report routing is not a behavioral interoperability contract.

## Confidence

- **Confirmed:** exported diagnostic functions, report routing, strict-fatal branch, unhandled-exception/minidump path, configuration keys/defaults, and obsolete crash-reporter defaults.
- **Strongly inferred:** remote `SharedLog` target topology from queue/reconnect/target configuration; transport details were not followed in this block.
- **Unknown/non-required:** exact minidump implementation internals and server protocol of the retired logging target.

## Evidence

- `decompiled-diagnostics/`
- exports `Fatal`, `Warning`, `WarningStrictFatal`, `Report::printf`, `CrashReportInformation::addStaticText`
- imports `SetUnhandledExceptionFilter`, `UnhandledExceptionFilter`, `OutputDebugStringA`, `MessageBoxA`, `TerminateProcess`

