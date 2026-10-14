# Bounded height terrace and inherited rule activation

The target executable establishes the AHTR height writer independently of the
later NGE source. The implementation admits exact AHTR/0004 with known
IHDR/0000 or0001 metadata, supplied finite height/influence, and an explicit
modern binary64 arithmetic policy with the target's F32 stores. Original x87
numeric parity and earlier AHTR formats remain unimplemented.

## Target evidence and original occurrence

Executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The static hash was rechecked October13. RTTI descriptor018B8A54 names
AffectorHeightTerrace; vtable015ED6BC slot+10 points to00B770B0.
Constructor00B77040 supplies tagAHTR, height20 at+14 and fraction.25 at+18.
Factory00B7A240 calls this constructor at00B7A7E8.
Loader00B771B0 dispatches versions0000..0004;0004 loader00B77530 calls
common IHDR loader00B62FA0 and reads two four-byte DATA values, fraction
to+18 then height to+14. These identities, order and raw widths are **Confirmed**.

Original Tatooine SHA-256:
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
TGEN SHA-256:
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
Both AHTR0004 sections have98 encoded bytes and IHDR0001:

| Layer | TGEN offset | Enabled | Fraction bits | Height bits |
|---|---:|---:|---|---|
|8|17446|0|3E5D2F1B|41F00000 (30)|
|49|39266|1|3E70A3D7|41200000 (10)|

Section hashes respectively:
`6D5529EA86873B8F2687D3A0CCD79E203FC2BB3530F702B082E10E6A5CDAEE52`
and `E42A53096C8E387D26C5FBE1C223677CEA4177FC60E669CC8C23215C5251CA9B`.
The installed occurrence and disabled flag are **Confirmed** by original bytes.
Generic writer/filter admission now reads the
[existing common IHDR contract](TERRAIN_GENERATOR_INPUT_CONTRACT.md), skips
disabled records before semantic dispatch, and excludes disabled writers from
the height-subtree mask. Enabled unknown rules and malformed/unknown headers
still reject. A disabled-only subtree cannot trigger an irrelevant filter.
The target's [fresh layer preparation](TERRAIN_LAYER_PREPARATION_CONTRACT.md)
walks filters and affectors through enabled getter00B62F20 (byte+08), with
filter preparation00B63C8E..AC and affector preparation00B63CBE..DC. This
independently confirms per-entry activation gates; the executable's general
evaluation entry00B63D20 also skips layers without prepared local/child work.

## Recovered graph and implementation consequence

At00B770B6..00B770F2 the writer requires positive supplied amount and terrace
height. It reads the current height grid through generator state+18 at
00B77100..00B77114. The remainder call0131F4FA reaches the CRT FPREM path
at0131F50F (including a processor workaround branch); signed truncating
remainder semantics are **Strongly inferred** from that path and independent
NGE corroboration. For finite original height `h`, positive step `s`, fraction
`f` and supplied amount `a`, the target graph is:

```text
r = signed_remainder(h,s)
low = h - (h < 0 ? s+r : r)
lowStore = F32(low)              low remains retained as well
mid = F32(low + s*f)
high = F32(low + s)
if h > mid:
    t = (h-mid)/(high-mid)
    next = lowStore + (high-lowStore)*t
else:
    next = retained low
result = F32(h + (next-h)*a)
```

Stores and branch/order are **Confirmed** at00B7712A..00B77193. A negative
exact multiple selects the preceding interval: h=-20,s=10 yields low=-30,
high=-20. At full influence the ramp still returns-20. Do not replace this
branch with mathematical floor. Amounts above1 extrapolate; no clamp was
observed. Caller layer activation/ordering follows the
[height-order contract](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md).

The helper declares modern binary64 arithmetic and checks each recovered F32
store. Active fraction must be inthe separate implementation record include675 arithmetic
samples, every truncation of a synthetic AHTR, malformed versions/extents,
nonfinite operands, disabled unsupported writers/filters/subtrees, enabled
unsupported rejection and ordered current-height dispatch. The
[independent rational oracle](../tools/python/verify_terrain_terrace.py) uses
exact dyadic remainder and explicit rounding instead of C# arithmetic or `%`.

```powershell
dotnet run --project poc/terrain-terrace-tests/TerrainTerraceTests.csproj -- $OriginalTerrain $PrivateReceipt
python tools/python/verify_terrain_terrace.py $PrivateReceipt
```

Without original input, pass only the receipt path for synthetic tests. These
checks do not execute Stardust.exe. Next: replay the sparse geographic survey
and determine whether layer49 becomes applicable; retain enabled unsupported
filters/roads as failures, and keep geographic coverage separate from numeric
parity. No canonical address labels or research scores change.
