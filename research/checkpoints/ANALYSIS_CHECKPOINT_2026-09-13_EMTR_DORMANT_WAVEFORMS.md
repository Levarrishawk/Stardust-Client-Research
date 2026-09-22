# EMTR dormant waveform closure

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

Modern EMTR channels 11-13 at legacy description offsets `+0x150`, `+0x168`, and `+0x180` are **confirmed dormant zero-valued waveforms for the complete effective Stardust asset corpus**. Their historical editor labels remain unknown, but their deployed runtime behavior no longer blocks a compatible importer or particle simulation.

## Executable evidence

- The emitter description constructor/reset path initializes the channels through helpers `0x007B9860`, `0x007B9880`, and `0x007B98B0`.
- All three call `waveform_initialize_random_keys_between_bounds_VA_00ABF420` with both value bounds equal to zero. The helper resets the waveform, chooses randomized key count/time positions, and fills bounded values.
- Helpers for channels 12 and 13 additionally call `0x00ABEEF0(0)`, enabling a zero lower-bound/clamp property; channel 11 does not.
- The only direct calls to those three helpers are the reset path at `0x007B93B7`, `0x007B93C3`, and `0x007B93CF`.
- An executable-wide direct-displacement scan found no consumer of `+0x150`, `+0x168`, or `+0x180`. This cannot exclude an indirect computed-index access by itself, so corpus evidence is also required.

`waveform_load_VA_00ABFAA0` accepts WVFM versions 0000-0002. It dispatches version 0001 to `0x00ABFD00` and 0002 to `0x00ABFE00`. Both read 16-byte key records. WVFM/0001 has a 12-byte header with key count at byte 8; WVFM/0002 has a 20-byte header with key count at byte 16. Save path `0x00ABF960` emits version 0002.

## Corpus evidence

The configuration-aware scan follows the 58 archives named by `swgemu_live.cfg`, honors highest-priority virtual-path winners, and uses the SWG IFF rule that the next sibling starts at the declared chunk end without an external odd-byte pad.

| Measure | Result |
|---|---:|
| Effective EMTR descriptions | 6,373 |
| WVFM records | 101,968 (= 6,373 × 16) |
| WVFM/0001 per channel | 1,882 |
| WVFM/0002 per channel | 4,491 |
| Structurally decoded channel-11 records | 6,373 |
| Structurally decoded channel-12 records | 6,373 |
| Structurally decoded channel-13 records | 6,373 |
| Records with any nonzero non-time key lane | 0 |
| Scan errors | 0 |

The channels have 44, 65, and 61 distinct raw hashes respectively and a range of record sizes. Those differences are explained by randomized default key counts and key times; every actual value lane remains zero.

## Scanner correction and revised inventory

The earlier EMTR scalar census advanced by an extra byte after odd-sized chunks, following classic EA-IFF padding behavior that this SWG corpus does not use. It consequently stopped before later sibling emitters in many groups. Direct declared-end traversal produces 6,373 emitters with no errors, replacing the previous 3,334 total:

| EMTR version | Correct count |
|---|---:|
| 0000 | 1 |
| 0006 | 8 |
| 0009 | 78 |
| 0010 | 28 |
| 0011 | 696 |
| 0012 | 1,039 |
| 0013 | 841 |
| 0014 | 3,682 |

## Replacement-client rule

For execution of the installed Stardust corpus, normalize channels 11-13 to constant zero and do not expose them as required simulation inputs. Retain their raw WVFM forms as optional compatibility metadata when exact round-trip serialization or forensic comparison is desired. Do not assign invented semantic names. A different external asset corpus should still be validated before assuming its records are also zero.

## Confidence

- **Confirmed by code:** initialization behavior, WVFM version dispatch and layout, absence of direct member-displacement reads.
- **Confirmed by deployed assets:** complete structural coverage and zero values for all three channels.
- **Unknown:** original tool/editor labels and whether unreleased/noninstalled assets ever used these fields.
