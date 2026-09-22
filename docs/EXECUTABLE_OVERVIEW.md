# Executable Overview

## Executive findings

`Stardust.exe` is a Windows GUI PE32 executable for x86, not a 64-bit binary. The PE timestamp is 2005-04-14 16:36:40 (timestamp authenticity not independently verified). Its version resource identifies Sony Online Entertainment, `SwgClient_r`, version `0.0.119.798`, and original filename `SwgClient_r.exe`. **Confirmed.**

Despite stripped COFF symbols, the image retains extensive MSVC RTTI/type strings, diagnostic strings, a PDB path, and 78 decorated C++ exports. This makes class and subsystem recovery materially more feasible than raw stripped-code analysis alone. **Confirmed.**

## PE metadata

| Property | Value | Confidence/evidence |
|---|---|---|
| Format | PE32 (`0x10b`) | Confirmed: PE optional header |
| Machine | x86 / `IMAGE_FILE_MACHINE_I386` (`0x14c`) | Confirmed: COFF header |
| Subsystem | Windows GUI | Confirmed: optional header |
| Image base | `0x00400000` | Confirmed |
| Entry point | RVA `0x00F1DC7A`, VA `0x0131DC7A` | Confirmed |
| Image size | `0x015C0000` | Confirmed |
| Linker | version 6.00 | Confirmed; consistent with Visual C++ 6-era toolchain, but compiler identity remains strongly inferred |
| Relocations | stripped | Confirmed |
| Large-address-aware | yes | Confirmed: COFF characteristic; still a 32-bit address space |
| ASLR/DEP flags | DLL characteristics `0` | Confirmed; no modern opt-in flags in header |
| Checksum/signature | checksum `0`; no certificate directory | Confirmed |
| Debug record | NB10 PDB reference | Confirmed |
| PDB path | `c:\work\swg\stage\src\compile\win32\SwgClient\Release\SwgClient_r.pdb` | Confirmed |

Sections: `.text` (executable code), `.rdata`, `.data`, `.rsrc`, and a small executable `.apas` section. The purpose and provenance of `.apas` are **unknown**. Do not interpret it as a protection mechanism without more evidence.

## Entry flow

The PE entry point at `0x0131DC7A` is CRT startup code. It gathers process/runtime state and calls `0x00401810` with four WinMain-shaped arguments. `0x00401810` performs two preliminary environment/OS checks then forwards the same four arguments to `0x00401050`. **Confirmed by disassembly.**

Provisional labels:

| VA | Provisional label | Observation | Confidence |
|---|---|---|---|
| `0x0131DC7A` | `crt_entry_VA_0131DC7A` | MSVC-style exception frame and CRT initialization | Confirmed role as PE entry; CRT semantics strongly inferred |
| `0x00401810` | `winmain_wrapper_VA_00401810` | Four parameters, forwards to `0x00401050`, `ret 0x10` | Strongly inferred |
| `0x00401050` | `app_entry_VA_00401050` | ordered initialization, run, teardown; returns zero | Strongly inferred |
| `0x004014ED`–`0x004015DE` | `client_run_install_chain_region` | callbacks/install calls and central run call | Tentative until callees are labeled |

The app-entry disassembly visibly contains a long install chain followed by a run phase and reverse cleanup. Function identities require string-xref/decompiler work; calling them by subsystem now would overstate the evidence.

## Imports and platform clues

- Networking: `WS2_32.dll`, `MSWSOCK.dll`; `WSASocketA`, `WSAConnect`, `WSASend`, `WSARecv`, `WSAEventSelect`, `WSAEnumNetworkEvents`, `WSACreateEvent`, and `AcceptEx`, plus ordinal imports. **Confirmed.**
- Input: `DINPUT8.dll` / `DirectInput8Create`, plus USER32 keyboard, cursor, and message APIs. **Confirmed.**
- Audio: `mss32.dll` with extensive Miles 2D/3D, streaming, listener, occlusion, obstruction, reverb, and provider APIs. **Confirmed.**
- Visibility: `dpvs.dll`, with cameras, cells, portals, ROI objects, mesh/OBB/sphere models, scissor and visibility resolution. **Confirmed.**
- Threading/synchronization: threads, IO completion ports, events, mutexes, semaphores, critical sections, TLS, interlocked operations, wait/suspend/resume/priority APIs. **Confirmed.**
- Memory: process heaps, private heaps, virtual allocation, file mappings, and exported `MemoryManager`/`MemoryBlockManager` APIs. **Confirmed.**
- Diagnostics: `OutputDebugStringA`, unhandled-exception APIs, `dbghelp.dll` beside the executable, exported `Fatal`, `Warning`, `Report::printf`, profiler and crash-report functions. **Confirmed.**
- Registry/shell: ADVAPI32 registry APIs and `ShellExecuteA`. **Confirmed.**
- Rendering: no static Direct3D import appears in the main image. Runtime strings and configuration name `Direct3d9`; a local `d3d9.dll` wrapper and versioned rasterizer DLLs are present. A dynamically loaded renderer is **strongly inferred**; the exact device-creation function is not yet located.

## Exported architectural clues

The 78 exports expose names including `Material`, `StaticIndexBuffer`, dynamic/static vertex-buffer graphics data, `ShaderImplementation*GraphicsData`, `StaticShaderGraphicsData`, `TextureGraphicsData`, `Texture`, `StaticShader`, `Transform`, `Object`, `TreeFile`, `ConfigFile`, `MemoryManager`, `MemoryBlockManager`, `Profiler`, `Report`, `Graphics`, `Clock`, and `Os`. This confirms a native C++ engine with explicit graphics-resource wrappers, CRC strings, custom allocation, tree-file resource abstraction, and internal diagnostics.

## Nearby-file interpretation

`Stardust.exe` contains `20260817-SD:01` at VA `0x018AFBAC`. Static initialization near `0x00ACAF20` installs it into a global string at `0x01946468`. Message-construction paths associated with `LoginClientId` and `ClientIdMsg` consume that global near `0x00ACA30A` and `0x00AE41D7`. Wire use is therefore **strongly inferred directly from Stardust.exe**; final send/serialization tracing remains.

The directory also contains a `d3d9.dll`, `d3d9-postfx.ini`, and log. The DLL is a separate x86 PE built with linker 14.00 and exports only `Direct3DCreate9`, `D3DPERF_BeginEvent`, `D3DPERF_EndEvent`, and `D3DPERF_SetMarker`. It is therefore a deployment-time D3D9 proxy/post-effect layer rather than statically linked original engine code. **Strongly inferred** (proxy role); its export surface and separate-binary status are confirmed. Analyze it separately before treating its behavior as client architecture.

## Limits of this pass

No Ghidra/rizin-style decompiler was located in the initial tool inventory. Analysis used PE headers/imports/exports, printable strings, source correlation, and targeted x86 disassembly. Resources, vtable reconstruction, call-graph recovery, indirect renderer loading, and complete executable diffing remain future work.
