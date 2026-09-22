# Analysis checkpoint: generic CuiTransfer mediator

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

MSVC RTTI confirms `CuiTransfer` as a generic two-denomination transfer/conversion dialog. It binds cancel, revert and OK buttons; a slider; starting-value labels; and editable from/to values. It is a local arithmetic and callback mediator, not a money packet or server-side transaction implementation.

The dialog retains two unit ratios, starting/current value pairs and a conserved weighted total. Validation is exact: `ratioTo * currentFrom + ratioFrom * currentTo == conservedTotal`. Text edits are rounded down to the applicable unit multiple, clamped to available total, and recompute the opposite side. Slider motion performs the same calculation. Revert restores both starting values; cancel closes locally; OK completes through the mediator callback path.

The only direct constructor callers recovered at `0x00BBF7A0` and `0x00BCAF60`
are mediator-factory allocation wrappers. No gameplay command or packet is
embedded in construction. Gameplay-specific interpretation is supplied through
the external completion/callback relationship.

| Address | Provisional name | Confidence |
|---|---|---|
| `0x013AAE00` | `cui_transfer_construct_VA_013AAE00` | Confirmed |
| `0x013AB0F0` | `cui_transfer_destroy_VA_013AB0F0` | Confirmed |
| `0x013AB140` | `cui_transfer_on_slider_changed_VA_013AB140` | Confirmed |
| `0x013AB160` | `cui_transfer_on_button_press_VA_013AB160` | Confirmed |
| `0x013AB190` | `cui_transfer_on_text_changed_VA_013AB190` | Confirmed |
| `0x013AB480` | `cui_transfer_recompute_from_slider_VA_013AB480` | Confirmed |
| `0x013AB540` | `cui_transfer_update_slider_VA_013AB540` | Confirmed |
| `0x013AB5A0` | `cui_transfer_set_current_values_VA_013AB5A0` | Confirmed |
| `0x013AB5E0` | `cui_transfer_revert_VA_013AB5E0` | Confirmed |
| `0x013AB600` | `cui_transfer_weighted_total_VA_013AB600` | Confirmed |
| `0x013AB620` | `cui_transfer_validate_conservation_VA_013AB620` | Confirmed |
| `0x013AB7D0` | `cui_transfer_initialize_values_VA_013AB7D0` | Confirmed |

## Replacement implication

Implement a deterministic conversion model plus asynchronous UI result. Caller-specific gameplay decides whether accepted values generate a command. Preserve integer arithmetic, floor-to-unit behavior, nonnegative clamps and exact total validation. Do not assign authority to the widget or invent a protocol from it.
