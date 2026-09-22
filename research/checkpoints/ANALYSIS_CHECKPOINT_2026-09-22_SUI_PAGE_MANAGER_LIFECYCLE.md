# Analysis checkpoint: SUI page-manager lifecycle

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: `SuiCreatePageMessage`, `SuiUpdatePageMessage`, `SuiForceClosePage`, `SuiEventNotification`

## Result

The manager surrounding `CuiDataDrivenPage` is now mapped from network dispatch through registry ownership and closure. Active pages are held in a process-global ordered map keyed by the server's 32-bit page ID. The scene-change notification drains that registry, so a data-driven page never survives a scene boundary.

## End-to-end routes

| Input / event | Parser or bridge | Manager action | Result | Confidence |
|---|---|---|---|---|
| `SuiCreatePageMessage` | message archive -> `0x00B008F0` -> `0x00A46830` | `0x00A46400` | resolve original root `UIPage`; construct `CuiDataDrivenPage`; apply initial commands; insert by page ID; attach/activate in workspace | Confirmed |
| `SuiUpdatePageMessage` | archive -> `0x00B00320` -> `0x00A46870` | registry lookup then `0x00A7C480` | apply command batch to existing page; missing page is ignored | Confirmed |
| `SuiForceClosePage` | archive -> `0x00B01FA0` -> `0x00A46850` | `0x00A46640` -> `0x00A46690` | remove/deactivate registered page without synthesizing a client response | Confirmed |
| `Game::SCENE_CHANGED` | local dispatch in `0x00A45D60` | `0x00A46800` | repeatedly closes every registered SUI page | Confirmed |
| user/window close | mediator virtual `0x00A7BB70` | emit event 10 once, then `0x00A46690(...,1)` | reliable cancel notification, registry removal, base close | Confirmed |

`0x00A46400` refuses an unresolved or non-`UIPage` root and logs `Bad DataDriven page name <%s>`. This is further evidence that server SUI depends on the original UI asset namespace: the replacement must load compatible page definitions and cannot invent a generic layout after receiving the packet.

## Ownership and cleanup

`0x00A46690` obtains the mediator's page ID from `0x00A7BAD0`, finds the equal-key registry range, erases the matching node(s), and performs mediator deactivation/cleanup. Its Boolean distinguishes an externally requested close from a close already initiated inside the mediator, preventing duplicate lifecycle work. Server force-close does not enter `0x00A7BB70`, so it does not return event 10.

`0x00A463B0` is manager teardown: it destroys the message receiver/listener and releases every registered mediator. `0x00A46800` is the normal scene-boundary drain. These are distinct shutdown and runtime-reset paths.

## Replacement contract

1. Keep the wire page ID exactly 32-bit; use it only as a key, never as a native pointer.
2. Resolve the server-supplied page name against original SWG UI resources before construction.
3. Apply create commands before exposing/activating the page.
4. Route updates by page ID and safely ignore a stale/missing page.
5. Distinguish server force-close from user close; only the latter returns the terminal cancel event.
6. Clear active SUI pages on scene transition and session teardown.
7. Preserve the one-shot terminal-event latch already identified in the data-driven page runtime.

## Stable provisional functions

| Address | Provisional name |
|---|---|
| `0x00A45D60` | `sui_page_manager_receive_message_VA_00A45D60` |
| `0x00A463B0` | `sui_page_manager_teardown_VA_00A463B0` |
| `0x00A46400` | `sui_page_manager_create_page_VA_00A46400` |
| `0x00A46640` | `sui_page_manager_force_close_by_id_VA_00A46640` |
| `0x00A46690` | `sui_page_manager_remove_page_VA_00A46690` |
| `0x00A46800` | `sui_page_manager_close_all_on_scene_change_VA_00A46800` |
| `0x00A46830` | `sui_page_manager_handle_create_VA_00A46830` |
| `0x00A46850` | `sui_page_manager_handle_force_close_VA_00A46850` |
| `0x00A46870` | `sui_page_manager_handle_update_VA_00A46870` |
| `0x00A7BAD0` | `cui_data_driven_page_get_page_id_VA_00A7BAD0` |
| `0x00A7BB70` | `cui_data_driven_page_close_cancel_VA_00A7BB70` |

## Boundary

`0x00A7C480` reads the U64 target and F32 distance through `0x0124D020` and
`0x0124D030`, then installs them on the generic mediator through `0x009C26D0`
and `0x009C2720`. A nonzero target plus positive distance enables the mediator's
periodic-close flag. The shared `CuiMediator` update at `0x009C1040` then
resolves player and target on a throttled interval, compares radius-adjusted
world distance, and invokes close if the target is missing or out of range.
This completes the footer-to-close policy; it is generic mediator behavior, not
an additional SUI wire rule.
