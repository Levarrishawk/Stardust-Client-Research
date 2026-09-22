# Deployed transport-selector reconciliation

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The transport selector itself and the installed launch path are now confirmed.
`shared_network_config_install_VA_012BBA30` reads
`[SharedNetwork] useTcp` with a default of `true` into the byte returned by the
accessor used to construct the 136-byte options record. At
`game_connection_base_ctor_VA_012B6150`, options byte `+0x86` selects:

- zero: UDP manager construction and `udp_manager_open_outbound_wrapper_VA_012BCDA0`;
- nonzero: `tcp_stream_connection_ctor_VA_012BE1C0`.

The TCP constructor creates `AF_INET/SOCK_STREAM`, enables nonblocking mode and
`TCP_NODELAY`, calls `WSAConnect`, registers close notification, and creates an
I/O completion port. Its already recovered framing is a native little-endian
U32 length followed by the named-message bytes.

The installed root `swgemu.cfg` includes login, live, preload, options and user
files in order. None defines `SharedNetwork.useTcp`. Static IL inspection of
`C:\Stardust-DEV\StardustLauncher.exe` closes the former command-line caveat:
`PlayButton_Click` combines the launcher directory with `Stardust.exe` and
passes only that filename to `ProcessStartInfo(string)`. `StartProcess` sets
only `UseShellExecute=true`; it never sets `Arguments`. Thus the ordinary
shipped launcher does not append a hidden config override, and the effective
installed selection is TCP.

## Server correlation and boundary

The inspected authoritative Stardust source is different at its direct service
boundary:

- `LoginServerImplementation` constructs `DatagramServiceThread`;
- `ZoneServerImplementation` constructs `DatagramServiceThread`;
- current configuration assigns login port `44453`.

No source-side stream listener fronts either game service. Therefore the
following are distinct facts, not a contradiction to paper over:

1. the distributed client package ordinarily selects its TCP framing path;
2. the checked-in login and zone service implementations directly speak UDP/SOE.

An external TCP gateway, reverse proxy, deployment-only service, or untracked
server component is strongly suggested by the combined evidence but is not
present in the supplied server tree. Its existence and translation behavior
remain **Unknown**. No live endpoint was contacted.

## Replacement consequence

Transport must be a session-level compatibility profile rather than hard-coded:

- `DirectCore3UdpSoe` for direct interoperability with the checked-in server;
- `LengthPrefixedTcp` for parity with the installed public-client selection;
- endpoint configuration must select the profile explicitly.

The first local x64 networking proof of concept should use UDP/SOE against the
developer-controlled Stardust server. A later TCP fixture requires either the
missing deployment edge implementation or a captured/controlled test endpoint;
it must not be invented from the UDP server source.

## Confidence

- Executable selector, defaults and both constructor branches: **Confirmed by code**.
- Launcher supplies no arguments: **Confirmed by managed IL**.
- Current server login/zone services are datagram services: **Confirmed by source**.
- External deployment gateway/listener: **Strongly suggested, implementation Unknown**.

