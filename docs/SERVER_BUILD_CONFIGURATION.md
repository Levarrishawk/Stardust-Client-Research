# Server Build and Runtime Configuration

Status: repository-default audit, 2026-09-08. These are checked-in defaults, not proof of the deployed VPS configuration.

## Confirmed repository defaults

| Setting | Default | Evidence |
|---|---:|---|
| Login port | `44453` | `MMOCoreORB/src/conf/ConfigManager.h`; `MMOCoreORB/bin/conf/config.lua` |
| Required login version | `20050408-18:00` | same files |
| Zone port | `0` (runtime/dynamic configuration) | `ConfigManager.h` |
| BaseProtocol send compression | disabled (`1`) | `BaseProtocol.cpp` property default |
| BaseProtocol send encryption | enabled (`disableSendEncryption=0`) | `BaseProtocol.cpp` property default |
| `ENABLE_LOCKFREE_BCLIENT_BUFFERS` CMake default | OFF | `MMOCoreORB/CMakeLists.txt` |
| `ENABLE_SWGREALMS` CMake default | OFF | `MMOCoreORB/CMakeLists.txt` |
| `ENABLE_BUILD_CLIENT` CMake default | ON | `MMOCoreORB/CMakeLists.txt` |

The engine3 utility Makefile explicitly supplies `-DENABLE_LOCKFREE_BCLIENT_BUFFERS=ON`, so the effective value depends on the actual build path. No checked-in property override for transport compression/encryption was found in the first pass.

## Version discrepancy

The checked-in server and Core3 client default to `20050408-18:00`. `Stardust.exe` directly contains the version-like string `20260817-SD:01`. This does not prove it is the wire version sent by the executable.

Before live compatibility testing, record from the authoritative deployed environment:

- effective `Core3.LoginRequiredVersion` value;
- login and zone bind/advertised endpoints;
- whether SWGRealms authentication is compiled/enabled;
- effective transport compression/encryption properties;
- build route and lock-free client-buffer setting;
- server binary/source revision.

Until captured, deployed configuration remains **Unknown** and must not be replaced by repository-default assumptions.

The installed client configuration identifies `live.stardust-swg.com:44453` as its login endpoint. This confirms the intended local-client destination, but no live connection was made during static analysis.

## POC configuration rule

Make the compatibility version an explicit configuration value shared by login and zone message encoders. Defaulting to `20050408-18:00` is reasonable for local repository-based tests, but a run must log the selected value and its source. Never infer a wider wire field because the POC process is 64-bit.
