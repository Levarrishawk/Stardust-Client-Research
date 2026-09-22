# Analysis checkpoint: CuiKeypad and CuiMoney widgets

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## CuiKeypad

MSVC RTTI confirms `CuiKeypad`. Required asset widgets are digits zero through nine, `buttonKeycard`, `buttonSlice`, `buttonEnter`, and `textboxNumbers`. Numeric buttons append their digit locally. The three action buttons publish a local `ButtonPressed` callback carrying `keycard`, `slice`, or `enter`, then latch completion so it is emitted at most once. No network serialization or command send occurs here.

| Address | Provisional name | Confidence |
|---|---|---|
| `0x013A8E80` | `cui_keypad_construct_VA_013A8E80` | Confirmed |
| `0x013A9260` | `cui_keypad_destroy_VA_013A9260` | Confirmed |
| `0x013A9310` | `cui_keypad_on_button_press_VA_013A9310` | Confirmed |

## CuiMoney

MSVC RTTI confirms `CuiMoney`. It requires `textCash` and `textBank`. Activation subscribes to `ClientObject::Messages::BankBalance` and `CashBalance`, then refreshes. Deactivation removes both subscriptions. The callback refreshes only when the changed object matches the bound object. Refresh formats its two replicated balance fields; absence clears both displays.

| Address | Provisional name | Confidence |
|---|---|---|
| `0x013DC5F0` | `cui_money_construct_VA_013DC5F0` | Confirmed |
| `0x013DC7D0` | `cui_money_destroy_VA_013DC7D0` | Confirmed |
| `0x013DC860` | `cui_money_activate_VA_013DC860` | Confirmed |
| `0x013DC8A0` | `cui_money_deactivate_VA_013DC8A0` | Confirmed |
| `0x013DC990` | `cui_money_on_balance_changed_VA_013DC990` | Confirmed |
| `0x013DC9C0` | `cui_money_refresh_VA_013DC9C0` | Confirmed |

## Replacement implications

- Preserve original widget identifiers for original-asset compatibility.
- Keep both widgets asynchronous and presentation-only.
- Do not invent a keypad packet or money-query packet.
- Preserve balance widths in replicated state; formatting belongs after decode.

