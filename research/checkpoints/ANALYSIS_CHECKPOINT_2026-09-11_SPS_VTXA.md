# Analysis Checkpoint — SPS and VTXA

Date: 2026-09-11  
Primary binary: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The static-mesh path is now traced from MESH through shader groups into primitive topology, VTXA declarations, vertex DATA, INDX and optional SIDX. Strict whole-client progress advances from **45% to 46%**; VFS/templates/assets reaches **58%**.

## Confirmed hierarchy

```text
SPS  0000/0001
  CNT
  repeated shader group
    NAME external SHT | inline shader template
    INFO primitive count
    repeated primitive
      INFO topology + flags (new form)
      VTXA 0001/0002/0003
        INFO declaration + vertex count
        DATA interleaved vertices
      optional INDX: U32 count + U16 indices
      optional SIDX: sorted range descriptors + index buffers
```

VTXA declarations support optional position, normal, packed color/scalar channels, and up to 15 texture-coordinate sets with independently encoded dimensions of one through four floats. Exact labels for auxiliary channels and the complete topology-ID enum remain intentionally open.

## Corpus evidence

Of 41,016 raw installed MSH occurrences:

- 29,025 are MESH 0005 / SPS 0001 / VTXA 0003;
- 11,814 are MESH 0004 / SPS 0001 / VTXA 0003;
- six are MESH 0003 / SPS 0000 / VTXA 0002;
- 171 require anomaly, tombstone or alternate-structure review.

`appearance/defaultappearance.msh` independently validates MESH 0003, SPS 0000, one external shader NAME, primitive type ID 9, VTXA 0002, and indexed geometry.

## Replacement consequences

Decode declarations into semantic vertex attributes rather than copying legacy packed structs. Validate vertex stride times count, index bounds, texture-coordinate set count/dimensions and all chunk limits before allocation. Preserve 16-bit file indices but allow modern internal meshes to use 32-bit indices when generated or combined; file compatibility must not impose a global engine limit.

## Exact resume point

Recover the topology-ID table from draw call sites, finish SIDX meaning, then follow external shader NAME into SHT/EFT pass/state decoding. Audit the 171 unclassified MSH records separately so dominant-version success does not conceal valid alternates or corrupt data.
