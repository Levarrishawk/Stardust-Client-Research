# SOE optional transport transforms

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

All five numeric transform selectors accepted by
`transport_transform_install_VA_012C63C0` now have replacement-sufficient
behavioral definitions. Only the unpublished historical labels for selectors 2
and 3 remain unknown; that is not an interoperability blocker.

| Selector | Outbound / inbound | Effective behavior | Confidence |
|---:|---|---|---|
| 0 | `012C6120` / `012C6150` | identity copy | Confirmed |
| 1 | `012C6180` / `012C61B0` | provider zlib-or-copy pair with trailing compression flag | Confirmed |
| 2 | `012C61E0` / `012C6210` | provider secondary pair; concrete methods are `memcpy` | Confirmed |
| 3 | `012C6240` / `012C62B0` | generated-pad XOR plus ciphertext chaining on complete LE32 words | Confirmed |
| 4 | `012C6320` / `012C6370` | seed-only ciphertext chaining | Confirmed |

Current Stardust negotiates `1,4`; 2 and 3 are dormant compatibility modes.

## Selector 2

The wrappers call provider virtual slots `+0x0C/+0x10`. The concrete vtable at
`0x016006EC` maps them to `0x012BD590/0x012BD5C0`; both copy exactly the requested
bytes and return the requested length. They add no framing and mutate no codec
state. Adjacent slots `+4/+8` contain selector 1's compression methods.

## Selector 3

The connection owns a pad buffer at `+0x190`. Installation allocates and fills it
if absent; the normal configured size is `0x200` bytes. Generator
`transport_seed_pad_next_byte_VA_012C8CD0` is:

```text
q, r = divmod(state, 127773)
next = r * 16807 - q * 2836 + 123
if next <= 0: next += 2147483647
state = next
padByte = next & 0xff
```

For each complete little-endian 32-bit word:

```text
outCipher = plain  XOR previousCipher XOR padWord
inPlain   = cipher XOR previousCipher XOR padWord
```

The initial `previousCipher` is the connection seed at `connection+0x11C`.
Outbound updates it to `outCipher`; inbound updates it to the incoming `cipher`.
A final one-to-three-byte tail is XORed with pad bytes only—the word chain is not
applied to the tail.

## Golden vectors

The executable reference model is `soe_selector3_vectors.py`.

| Seed | Plaintext | Ciphertext |
|---|---|---|
| `00000001` | `00` | `22` |
| `00000001` | `000102` | `22a86e` |
| `00000001` | `00010203` | `23a86e5e` |
| `00000001` | `0001020304` | `23a86e5e18` |
| `12345678` | `000102030405060708090a0b0c0d0e` | `6136b8dffebf9351b2ec2bc9d5bd59` |
| `7ffffffe` | `000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f` | `2d3844743eb5e9205d59eeac36e8655ba015e644217b2fbd4dea3fb5d2789100` |

Every case round-trips through the independently directed decoder.

## Replacement rule

Dispatch by numeric value, preserve transform and inverse order, preserve LE32
interpretation and selector 3's tail exception. Selector 2 should remain an
explicit identity extension point. Historic names are unnecessary for compatible
implementation.
