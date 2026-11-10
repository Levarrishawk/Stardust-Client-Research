# Reproducible static PE dependency audit

October 17, 2026. **Current product scope:** the user explicitly excludes Bink video playback. The retained Bink/Miles mismatch is historical optional-path evidence, not a required-release blocker; no further Bink candidate or playback work is planned. Original Pre-CU splash screens and the native rendered intro scene are required and are separate from Bink.

This tool answers a bounded packaging question: do explicitly pinned x86 application roots and named dynamic exports have providers under an application-directory/explicit-system-directory search model? It reads PE files as data. It never executes an audited executable or DLL, reads client configuration, activates a feature or contacts a server.

## Tools and confidence

- [Generic auditor](../tools/python/audit_pe_runtime_dependencies.py): imports, delay imports, named/ordinal exports and recursive forwarders; machine 014C enforcement; API-set namespace v6 exact names/importer aliases; bounded module/edge/forwarder work.
- [Source adapter](../tools/python/audit_source_runtime_dependencies.py): captures three source files once and extracts literal Bink/DebugHelp bind names plus renderer GetApi. This adapter understands the documented source layout and those literal forms, not arbitrary C++ preprocessing or all dynamic loading.
- [Synthetic fixtures](../tools/python/test_audit_pe_runtime_dependencies.py): **27 test methods pass** with Python 3.12.14. Fixtures use authored namespace bytes, synthetic module graphs and harmless temporary files; no proprietary PE input is required. The installed runtime audit additionally uses pefile 2024.8.26.

Each explicit root requires a runtime-relative path and expected SHA-256; the schema has its own expected hash. PE/schema parsing and hashing use the same captured bytes. Dynamic exports can discover additional forwarder hosts; their imports are drained before reporting. Final checks re-read every audited module, schema, pinned root and source bind file before writing. They detect changed final backing files, not arbitrary transient rewrites or process-memory changes. Output must be a fresh directory under this repository's ignored local-private directory. Reports contain paths and derived proprietary metadata and remain private.

The 27 methods include lookup/case/ordinal handling, missing providers/exports, wrong bitness, named/ordinal/API-set forwarders, cycles and budgets, delayed-edge classification, API-set aliases, every truncated namespace prefix, invalid fields/declared sizes 0-27, parse/hash generation races, late dynamic host imports, pinned-root/output boundaries and source extraction. Run from the research root:

```powershell
python tools/python/test_audit_pe_runtime_dependencies.py
```

## Exact current-pair invocation

The following canonical CLI run completed on the normal probe-off ABE9/396 Experimental pair. Use a fresh output name on repetition. The optional package directory is resolved against this research repository, independently of the invocation directory; omit it when pefile is installed in the selected Python environment. All provider roots are explicit. No binaries, reports, private configuration or third-party packages are included in the canonical tools.

```powershell
python tools/python/audit_source_runtime_dependencies.py `
  --runtime C:/Stardust-Experimental --system C:/Windows/SysWOW64 `
  --schema C:/Windows/System32/apisetschema.dll `
  --expected-schema-sha256 7FA8AA6F1DCA1D87EC9627C8A1BF486AD91B21FB3EB16E59545C8124A04E8ADF `
  --source C:/Users/ancie/Documents/GitHub/SD-Src-Client `
  --renderer-name gl05_r.dll --bink-name binkw32.dll --debug-name dbghelp_6.3.17.0.dll `
  --pefile-path local-private/toolchain/python-static-analysis-packages `
  --root SwgClient_r.exe=ABE9F6017663BF116B72C4913D098F7603D29F6D81CAE4AE3439E9541CA558D9 `
  --root gl05_r.dll=3962789ED50EA5B864EB87517F1121841716F155664B878082EEE06CA13B37A5 `
  --root miles/mssmp3.asi=BCBF4A4B285458AE93843C604AE62D05ECF5E851187574958E64E14931F6A347 `
  --root miles/mssogg.asi=CB559C3E3E81BD121210B8B49383BCC9D32DFF541059C5BB06AAE5C400D6F922 `
  --root miles/mssvoice.asi=B2E0400689199B0E41008DF6756910847FCA844264788FEC85434BDEF5D429A5 `
  --root binkw32.dll=E67E0319F9929C024A6D0757DE50C65257F686F6711CB0EFB7AD53AFC3405DD4 `
  --root dbghelp_6.3.17.0.dll=412F6C0ED936316275E68E21DF7255E4E35FAD14E114E479E8C5C9D75ECAC88C `
  --out local-private/experimental-startup/runtime-dependency-audit/abe9-canonical-20261017
```

The actual interpreter is the bundled 64-bit Python 3.12.14. The generic CLI accepts the same common arguments without --source/provider-name arguments; optional repeatable `--dynamic 'ROOT_NAME!EXPORT'` requests named exports, or `ROOT_NAME!#N` requests ordinals. A dynamic provider must itself be an explicitly pinned root. The source adapter adds no roots automatically. It captures BinkDLL.cpp, BinkVideo.h and sharedDebug/win32/DebugHelp.cpp, records hashes of the captured inputs and rejects an empty extraction. Verify the extraction against source when syntax or layout changes.

## Result and limits

**Confirmed static observation:** the canonical run has **11 application modules / 1,418 imported symbols / 75 delayed symbols**. All 1,343 ordinary application imports and 74 delayed imports resolve under this model. One delayed requirement remains: installed Bink requests `_AIL_init_sample@12`, while installed Miles exports `_AIL_init_sample@8`. All **52 enumerated dynamic exports** are present (GetApi, 38 Bink and 13 DebugHelp names). Input identities match after collection. Private report SHA-256: **A7A3726013716D2CF534B7FFB83EE95BAC678F64F62A50D46CF35541E0865787**. The earlier private-tool C2B and ABE9 receipts retain their separate identities; promotion does not relabel them.

**Interpretation:** this is a reproducible static package boundary result, not proof that the optional delayed Bink audio path executes. The current source profile disables video before Bink loading. The companion source runtime-dependency document records the source reachability analysis and separately authorized loaded-module observation. Dynamic export presence establishes lookup compatibility, not ABI or behavior.

The exploratory recursive graph has 322 modules and 80,008 potential symbol edges. Its 24,891 recorded unresolved/error entries include unmodeled API-set revision aliases, optional Windows branches and loader-policy differences; these are not 24,891 client failures. Work limits are 512 modules, 100,000 symbol edges and 32 forwarder hops. Exceeding a limit aborts rather than publishing a complete-looking result.

The search inference does not emulate KnownDLLs, SxS/activation contexts, package policy, PATH, already-loaded modules or every altered LoadLibrary search flag. System32 redirection is not inferred: the explicit SysWOW64 directory provides x86 DLL bytes, while the named System32 API-set schema is data only. API-set version compatibility aliases are not modeled. Arbitrary dynamic loads, provider selection and feature activation remain outside the enumerated requests. The tool does not establish licensing/redistribution provenance, a clean-install package, process-memory identity, GPU behavior or release readiness. No research score, function registry, protocol denominator or implementation gate changes.

Next: restore and validate the original Pre-CU splash sequence and native scene/intro.iff workflow, preserving disabled Bink playback. Further required-feature loaded-module/execution evidence needs its own bounded authorization. For a new package, pin its roots and schema explicitly and retain its own private receipt. Bink candidate searches and playback activation are outside the user-approved product scope.
