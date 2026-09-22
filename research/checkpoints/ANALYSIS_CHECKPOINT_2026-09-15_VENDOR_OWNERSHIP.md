# Analysis checkpoint: vendor ownership query

Date: 2026-09-15

## Result

The `IsVendorOwnerMessage` / `IsVendorOwnerResponseMessage` workflow is closed from the executable's reliable U64-object request through Stardust ownership policy to the client's auction-manager event publication.

## Request (`0x21B55A3B`)

Generated constructor `0x00AF2DF0` and parser `0x00AF2F40` establish a single U64 vendor/terminal object ID. Two UI call sites (`0x0092AAEC` and `0x0092AFAB`) construct it and send reliably through `0x00462670`. Stardust resolves the object, zone and player before responding.

## Response (`0xCE04173E`)

Generated parser `0x00AE76C0` establishes:

| Field | Type | Meaning |
|---|---|---|
| rights | U32 | 0 own vendor, 1 another owner, 2 galaxy/bazaar |
| error | U32 | 0 normal; 1 auctioneer/disabled/on-strike; 2 invalid object |
| object ID | U64 | queried vendor/terminal |
| vendor UID | ASCII | auction-manager identity; empty for invalid object |
| trailing value | U16 | current server literal `0x64`; unknown meaning |

Stardust verifies vendor/bazaar type, market availability, vendor data, ownership rights and strike state. The invalid-object path still produces all five fields. A malformed vendor-data path can return before completing the packet and is a server-side robustness risk.

## Client action

Auction dispatcher `0x006A8010` matches the response at branch `0x006A853E`, parses it, and passes rights, error, object ID and vendor UID to `0x004CF440`. That function publishes auction-manager event data through the transceiver at `0x0190ECC8`. The trailing U16 is parsed but not forwarded by this path.

## Mapping

| Message/opcode | Codec | Handler | Result | Confidence |
|---|---|---|---|---|
| `IsVendorOwnerMessage` / `0x21B55A3B` C->Z | `0x00AF2DF0` / `0x00AF2F40` | server callback | query access/ownership for a vendor or bazaar | Confirmed |
| `IsVendorOwnerResponseMessage` / `0xCE04173E` Z->C | `0x00AE76C0` | `0x006A853E` -> `0x004CF440` | publish ownership/error/identity to auction subscribers | Confirmed; trailing U16 Unknown |

## Replacement requirements

- Preserve U64 IDs and all five response fields.
- Model rights and error separately.
- Accept empty vendor UID on invalid-object responses.
- Parse and retain the trailing U16.
- Treat incomplete packets as failures; do not emulate the server early-return hazard.

## Remaining bounded unknowns

- Meaning of trailing `0x64`.
- Exact widget decisions after the confirmed publication boundary.
