# Analysis Checkpoint: PLAY:9 Reserved Fields

## Question

Do PLAY:9 indices 14 and 15 represent unresolved active behavior that must be reconstructed before a compatible client can be designed?

## Evidence

- PlayerObject constructor registration identifies both fields as ordinary 32-bit AutoVariables.
- Accessors `0x00660A20` and `0x00660A30` are trivial getters for the two slots.
- An executable-wide raw scan of all possible direct near-call (`E8 rel32`) encodings found no caller of either accessor.
- No previously recovered callback is installed on either field.
- Current server `PlayerObjectMessage9` emits LE32 zero for both indices.
- Current server delta implementation has no updater for either index.

## Conclusion

The two slots are **Confirmed dormant in Stardust.exe and current Stardust server behavior**. Their wire positions and widths remain part of the protocol and must be retained, but no client-side action is triggered by them in this build. They are therefore reserved compatibility fields, not material architectural unknowns.

The historical source names remain unknown. Recovering those names would improve archaeology but cannot change the behavioral implementation required for this client/server pair.

## Replacement rule

Decode indices 14 and 15 as opaque LE32 values, initialize them to zero, retain them in diagnostic snapshots if desired, and perform no gameplay/UI action. A tolerant decoder should accept nonzero values without corrupting adjacent fields.
