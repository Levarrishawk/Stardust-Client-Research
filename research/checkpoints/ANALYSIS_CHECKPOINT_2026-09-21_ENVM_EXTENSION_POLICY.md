# ENVM Extension Policy Reconciliation

Target client: `C:\Stardust-DEV\Stardust.exe`

This block combines the executable's negative evidence, the effective TRE corpus, and Stardust server parsers to turn `TLOK`, `SKYB`, and `DIST` from a pre-build unknown into an explicitly versioned optional-extension policy.

## Compatibility boundary

`Stardust.exe` does not recognize the `TLOK`, `SKYB`, or `DIST` tags in its ground `ENVM/0000` loader. There are no tag immediates anywhere in its executable code, and no extension data reaches its `GroundEnvironment` state. Exact emulation of this executable therefore means these chunks have **no active ground-render behavior**.

The future client's non-negotiable original-asset support calls for a better policy than destructive skipping: parse and retain the extensions in renderer-neutral IR, keep legacy behavior as the default compatibility profile, and enable evidence-backed enhanced behavior only behind an explicit capability/profile.

## TLOK

The deployed record is exactly:

```text
U8 enabled
F32 normalizedCyclePosition
```

All seven effective records have `enabled=1`; values are 0.15, 0.25, 0.30, or 0.90. Tag spelling, normalized domain, and content placement make “time lock” a Strong identification, but neither this client nor current Stardust server source implements precedence against server-supplied time.

Policy:

- always decode and retain the two fields;
- legacy profile: ignore for active time calculation, matching `Stardust.exe`;
- enhanced profile: allow it to pin environment-cycle sampling only after a test defines precedence with authoritative server time, transitions, and admin time changes;
- never silently alter network time or simulation time because of an asset-local visual lock.

## SKYB

The sole ground ENVM extension is:

```text
U8 mode = 0
cstring logicalName = "nebula_kash"
```

The effective corpus also contains `terrain/space_kashyyyk.trn`, whose normal `FORM/SKYB/0000` record contains the **identical 13-byte body** (`0`, `"nebula_kash"`). Stardust's `SpaceTerrainAppearance::parseSKYB` independently confirms the byte-plus-string grammar. This is direct evidence that the ground extension reuses the space-terrain skybox logical identifier contract; it is not a filename to expand by guessing a path.

Policy:

- preserve `{mode, logicalName}` exactly;
- resolve the logical name through a future skybox-definition registry shared with the space-terrain asset path;
- do not invent `nebula_kash.sht`, `.dds`, or another filename;
- legacy profile ignores it because this executable does;
- enhanced rendering remains gated until the space skybox registry/renderer is reconstructed.

The byte's historical enum label remains unknown. Corpus value zero must be preserved, not renamed “disabled”: a zero-valued record is actively present in the working space-terrain definition.

## DIST

The four Mustafar ground records are byte-compatible with the server's ordinary space-terrain `DIST/0000` parser:

```text
cstring appearancePath
F32 x
F32 z
F32 y
F32 angle1Degrees
F32 angle2Degrees
F32 angle3Degrees
```

The referenced `appearance/pt_mustafar_distant_volcano_01.prt` (and `_02`/`_03`) names are real archive member references, not guessed names. Space terrain uses the same schema for distant particle/starfield appearances. The intended role “place distant appearance at transform” is therefore Strong and sufficient for an optional implementation, even though `Stardust.exe` ground terrain never instantiates it.

Policy:

- decode the source-order X/Z/Y values and degree angles exactly;
- retain the appearance path through the ordinary asset resolver;
- convert to engine coordinate and quaternion/matrix representation only at the typed asset boundary;
- legacy profile does not instantiate it;
- enhanced profile may instantiate it as an environment-owned distant appearance after lifecycle, culling, and scale behavior are validated against the space-terrain implementation.

## Recommended IR

```text
EnvironmentExtensions {
  optional TimeLock { u8 enabled; f32 normalizedCyclePosition; }
  optional Skybox { u8 mode; string logicalName; }
  vector<DistantAppearance> distant;
  vector<UnknownIffNode> preservedUnknown;
}
```

These fields should survive decode/re-encode and diagnostics even when the active compatibility profile ignores them.

## Confidence and residual unknowns

- Schemas, occurrence counts, identical ground/space SKYB body, real DIST asset paths, absent executable consumers, and legacy no-op behavior: **Confirmed**.
- TLOK time-lock role, shared logical skybox registry requirement, and DIST distant-appearance intent: **Strongly inferred/correlated**.
- TLOK precedence and the SKYB byte's original enum name: **Unknown**, but no longer blockers because they are not behaviors of the target executable. They are enhancement validation tasks.

