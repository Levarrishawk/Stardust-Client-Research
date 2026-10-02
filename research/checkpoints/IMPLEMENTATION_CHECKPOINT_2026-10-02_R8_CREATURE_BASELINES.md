# 2026-10-02 R8-NET-01: complete CREO:3 baseline

Question: can a bounded engine-neutral codec retain and re-encode the complete eighteen-field CREO:3 snapshot? Stop at codec/fixture verification; world state binding and live interoperability are outside this milestone.

Input executable SHA-256 was verified: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static evidence and server correlation are consolidated in the [contract](../../docs/CREATURE_VIEW3_BASELINE_CONTRACT.md). No executable execution or network contact occurred. Round8 baseline manifest SHA-256: `5c6d703a5f6a6ccdb90548b89070356a6ba6b26e8dbbe49376f3cf195131cbdb`; archive SHA-256: `c878c4e17f1f6d8b2dbcc1778efbe77b1ec8eb310c5ad89e704be8694d3d79c2`.

Observation: existing client evidence establishes eighteen registered fields, four-byte component keys, extended byte strings and raw Unicode units. Interpretation: full snapshots can be represented without original memory layout. Confidence: Confirmed structural evidence; source set uniqueness is server correlation; original duplicate handling remains Unknown. Consequence: preserve raw count/counters/order/prefixes and expose six scalar values, while deferring runtime normalization.

Added codec, focused scenarios, independent Python oracle and isolated offline runner. Synthetic fixtures include extended short strings/high bytes, embedded zeroes, unpaired surrogates, signed zero, nonunit height, high IDs/counters, signed component keys and duplicate wounds. Negative tests cover every prefix truncation, corrected-envelope partial fields, tail, schema, nonfinite floats, impossible lengths/counts, each budget and snapshot ownership. Duplicate component retention is explicitly modern raw policy.

Reproduce: Python oracle `--generate local-private/round8`; run isolated Networking project with input path `local-private/round8/input.bin` and export directory `local-private/round8/export`; oracle `--verify local-private/round8/export`. Use offline package sources and existing .NET 8 toolchain. Generated fixtures remain ignored.

Next probe: bind accepted full snapshots to engine-neutral object state with an explicit component-set policy and revision/lifecycle rules. No Round9 work is authorized here. Shared runners, registries and ledgers are reserved for lead integration; research metrics remain unchanged.

Validation: focused native scenarios passed after correcting synthetic mutation offsets; Core build passed with zero warnings/errors; independent Python wire/all-field comparison passed; workspace validator reported zero broken links/private links and unchanged 2,602 names/345 packet rows. Diff whitespace check passed. Scope review found exactly seven allowed additions, all 1,924 accepted baseline files byte-identical, and an empty index.
