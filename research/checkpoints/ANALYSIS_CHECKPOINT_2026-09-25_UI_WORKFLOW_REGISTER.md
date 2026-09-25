# UI workflow reachability register

Date: 2026-09-25  
Target: `C:\Stardust-DEV\Stardust.exe`

## Purpose and denominator

`data/curated/ui_workflow_register.csv` is now the canonical discovery and
closure register for the client UI workstream. It starts from the executable
RTTI inventory, removes nested `@` action/helper types, and therefore contains
**217 standalone classes**. Of those, **93** have a direct
`CuiMediatorFactory` constructor registration.

The generator deliberately does not equate a class-name mention with a closed
workflow. Initial exact-name evidence is only a routing aid. A manual override
may mark a row closed only after recording:

1. activation/owner evidence;
2. effective configured asset evidence;
3. event-to-state or event-to-packet behavior; and
4. failure, cancellation and cleanup behavior.

The first generated pass classified 163 rows as having some exact-name
checkpoint evidence, four as architecture mentions only, and 50 as untriaged.
Among factory-registered mediators, 14 were initially untriaged and two had
only architecture mentions. These figures measure audit routing, not behavioral
completion.

## Repeatability

Run:

```text
python tools/python/build_ui_workflow_register.py <workspace> data/curated/ui_workflow_register.csv
```

`data/curated/ui_workflow_overrides.csv` holds reviewed evidence and prevents a
regeneration from discarding manual closure decisions. The generated register
retains the source category, factory status, exact-name evidence files, current
reachability/coverage status and the next missing observation.

## First resolved live gap

`SwgCuiControlsMenu` and its owned `CuiControlsMenuBindEntry` are the first
rows promoted to `closed_to_replacement_depth`. Their complete local workflow
is documented in the companion Controls Menu checkpoint. They are not a
network/server workflow.

## Interpretation

- `factory_registered_activation_unverified` is an executable construction
  route, not proof that current actions or assets reach it.
- `evidence_present_workflow_exit_test_required` means prior prose mentions the
  exact class, not that all four closure dimensions are satisfied.
- `closed_to_replacement_depth` records a sufficient behavioral contract for a
  replacement; it does not claim every instruction or visual pixel is named.
- support components inherit reachability from a proven owner and still require
  material behavior and cleanup evidence.

This register converts A09 from an open-ended name list into a bounded queue.
It does not by itself change the whole-client coverage percentage.
