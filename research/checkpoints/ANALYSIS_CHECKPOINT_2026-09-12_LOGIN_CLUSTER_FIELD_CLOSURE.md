# Analysis Checkpoint: Login Cluster Field Closure

## Scope

This block resolved the material uncertainty around `LoginEnumCluster` (`0xC11C63B9`) and `LoginClusterStatus` (`0x3436AEB6`) by correlating Stardust.exe's archive constructors, vector decoders and UI handlers with the current Stardust server constructors.

## Confirmed findings

- `LoginEnumCluster` registers two independent archive fields: a vector and a trailing LE32.
- Vector decoder `0x00AEDE30` reads count followed by 20-byte logical records: LE32 galaxy ID, ASCII16 name and LE32 metadata.
- Handler `0x00898AC0` consumes only the vector. It never reads the trailing LE32. The server's footer value `8` is therefore required for wire shape/alignment but behaviorally inert in this executable.
- `LoginClusterStatus` vector decoder `0x00AECDB0` produces 44-byte records with exact offsets documented in `PACKET_HANDLER_MAP.md`.
- Handler `0x00898C70` treats record `+0x24` as the status enum. Zero records are skipped; status values 1 and 3 set distinct internal flags.
- The final byte at `+0x28` is copied to the cluster model as a recommendation-like flag.
- Record `+0x20` is retained as cluster metadata. Current Stardust emits `0xFFFF8F80`.
- Record `+0x1C` is discarded: the handler writes zero into the corresponding internal field instead of copying it.
- Hostname, zone port and ping port are retained; hostname plus zone port forms the endpoint used for zone connection, while ping port remains separate.

## Server-source correction

The server packet constructor's inline comments are not reliable labels for the last fields. In particular, the server labels `+0x24` as status correctly in code order, while the executable proves the adjacent fields have different behavioral roles than the older generic names suggested by the client stub. Wire order, widths and current values are authoritative; historical semantic labels remain provisional.

## Confidence

- Exact wire widths/order: **Confirmed by code**.
- `+0x24` status behavior and `+0x28` recommendation storage: **Confirmed by code**.
- `+0x1C` ignored by this build: **Confirmed by code**.
- `+0x20` as signed time-zone offset: **Strongly inferred** from retained placement and current value; retain as opaque signed 32-bit metadata until another consumer names it.
- Footer's historical name: **Unknown**, but its lack of client-side behavioral effect is confirmed.

## Replacement-client consequence

The proof-of-concept may model both uncertain historical labels as opaque fixed-width fields. It must parse/emit them in the original order, but it does not need to reproduce nonexistent behavior. This closes the login-cluster row as a material blocker without inventing names.
