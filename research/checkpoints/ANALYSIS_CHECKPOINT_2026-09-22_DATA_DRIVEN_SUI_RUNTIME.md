# Analysis checkpoint: data-driven SUI runtime

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust `SuiPageData`, `SuiCommand`, create/update messages and event callback

## Result

`CuiDataDrivenPage` is the client runtime for server-authored SUI. It is a critical bridge between original UI assets and Stardust gameplay scripts, not merely a cosmetic mediator.

Creation resolves the server-supplied root page from the original UI namespace, reads its `MediatorData` object, creates any named child mediators, installs a `CuiDataDrivenPageListener`, executes the initial command batch, records target/range metadata, and activates the page. Missing page/widget/data-source names are warned and skipped rather than silently synthesized.

## Command grammar

The executable switch at `0x00A7C9D0` exactly matches current server command values 1–7:

| Type | Server name | Client action | Confidence |
|---:|---|---|---|
| 1 | `SCT_clearDataSource` | resolve named data source and clear it | Confirmed |
| 2 | `SCT_addChildWidget` | resolve parent, create named UI type, attach child | Confirmed |
| 3 | `SCT_setProperty` | resolve widget and set named Unicode property | Confirmed |
| 4 | `SCT_addDataItem` | create data item, set property, append to data source | Confirmed |
| 5 | `SCT_subscribeToEvent` | register event callback plus widget/property subscriptions | Confirmed |
| 6 | `SCT_addDataSourceContainer` | create and append named data-source container | Confirmed |
| 7 | `SCT_clearDataSourceContainer` | resolve and clear container | Confirmed |

The server enum also declares type 8, `SCT_addDataSource`, but has no current producer/helper for it. This executable's switch rejects values outside 1–7 as invalid. Type 8 must therefore remain an explicitly unsupported dormant extension, not be treated as currently compatible.

Each command is encoded as `U8 type`, `U32 Unicode count`, repeated Unicode strings, `U32 ASCII count`, repeated ASCII strings. Create/update wrappers and footer fields remain as documented in the packet map.

## Event route

The listener normalizes original UI callbacks to SUI event IDs:

| Event | ID | Recovered adapter |
|---|---:|---|
| button | 1 | `0x00A837D0` generic button; special OK=9 and cancel=10 paths |
| checkbox | 2 | `0x00A83860` / `0x00A83880` |
| enabled changed | 3 | `0x00A83790` / `0x00A837B0` |
| generic selection | 4 | `0x00A838E0` / `0x00A83900` |
| slider | 5 | `0x00A838A0` |
| tabbed pane | 6 | `0x00A838C0` |
| textbox | 7 | no specialized listener override recovered; unused by current Stardust scripts |
| visibility changed | 8 | `0x00A83750` / `0x00A83770` |
| closed OK | 9 | `0x00A837D0`, one-shot close latch |
| closed cancel | 10 | `0x00A837D0`, one-shot close latch |

`0x00A7C0E0` finds the matching subscription, reads every subscribed widget property as Unicode, constructs `SuiEventNotification`, sends it reliably, and prevents duplicate terminal close notifications. The current server parses page ID, event index and Unicode arguments, then removes/resolves the retained page and invokes its native/Lua callback.

Updates at `0x00A7C480` execute the same command stream against an existing page but deliberately skip type-5 subscription commands after the initial batch. Property commands trigger page/layout refresh where necessary.

## Stable provisional functions

| Address | Provisional name |
|---|---|
| `0x00A7B420` | `cui_data_driven_page_construct_VA_00A7B420` |
| `0x00A7B8F0` | `cui_data_driven_page_destroy_VA_00A7B8F0` |
| `0x00A7BDD0` | `cui_data_driven_page_collect_subscribed_properties_VA_00A7BDD0` |
| `0x00A7C0E0` | `cui_data_driven_page_emit_event_VA_00A7C0E0` |
| `0x00A7C480` | `cui_data_driven_page_apply_command_batch_VA_00A7C480` |
| `0x00A7C9D0` | `cui_data_driven_page_execute_command_VA_00A7C9D0` |
| `0x00A837D0` | `cui_data_driven_listener_button_or_close_VA_00A837D0` |
| `0x00A83750` | `cui_data_driven_listener_visibility_changed_VA_00A83750` |
| `0x00A83790` | `cui_data_driven_listener_enabled_changed_VA_00A83790` |
| `0x00A83860` | `cui_data_driven_listener_checkbox_VA_00A83860` |
| `0x00A838A0` | `cui_data_driven_listener_slider_VA_00A838A0` |
| `0x00A838C0` | `cui_data_driven_listener_tabbed_pane_VA_00A838C0` |
| `0x00A838E0` | `cui_data_driven_listener_selection_VA_00A838E0` |

## Replacement requirement

This runtime is mandatory for compatibility with existing Stardust screenplays and original UI assets. A replacement needs:

1. original page-name and widget-path resolution;
2. command types 1–7 with identical parameter ordering and tolerant failure policy;
3. event subscription/property capture and the exact notification codec;
4. initial-versus-update subscription behavior;
5. target/range forced-close handling;
6. explicit rejection/diagnostics for unsupported command 8.

