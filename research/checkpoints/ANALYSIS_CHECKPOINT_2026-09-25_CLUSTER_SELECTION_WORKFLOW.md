# Cluster Selection workflow closure

Date: 2026-09-25  
Target: `C:\Stardust-DEV\Stardust.exe`

## Reachability and asset

`SwgCuiClusterSelection` is a factory-registered live mediator. Factory create
slot `0x00BCBB60` allocates 0xCC bytes and calls constructor `0x00C79D70`.
Activation `0x00C7A180` subscribes to typed
`CuiLoginManager::Messages::ClusterConnection` and `ClusterStatusChanged`
transceivers, closes any previous connection-server session, binds UI input and
rebuilds the galaxy table.

The configured `ui/ui_cluster_selection.inc` winner is the 16,198-byte member
from priority-53 `stardust_3_01.tre`. Its CodeData binds Previous, Next, help,
sample population bar and a single-selection table. The table columns are name,
online/status, population and distance.

## Data flow and user action

`LoginClusterStatus` and the login cluster list populate the records owned by
`CuiLoginManager`. Table rebuild `0x00C7A730` projects those records into the
four-column model, derives localized status/population/distance presentation,
preserves a selected cluster where possible and updates button availability.
The status-change transceiver rebuilds this table rather than retaining stale
row state.

Next/double-select reaches `0x00C7B220`. It resolves the selected row to a
galaxy ID and current record, then rejects:

- no selection or missing ID/record;
- unavailable/loading/locked status;
- a missing resolved endpoint;
- a full cluster unless the account/record policy permits entry; and
- a request that conflicts with an already pending/different connection.

For a valid record it closes a mismatched prior connection, opens a connecting
message box, retains the requested galaxy ID and calls
`cui_login_manager_open_cluster_VA_0089A400`. The login manager supplies the
record's resolved hostname/address and zone port to
`game_network_open_connection_server_VA_00462060`. The ping port remains part
of latency/distance measurement and is not the connection-server port.

Previous returns to the login page. When an appropriate session already exists,
the mediator transitions to Avatar Selection rather than opening a duplicate
connection.

## Asynchronous result and policy

Client permission packet handler `0x00899340` publishes the typed connection
result. Consumer `0x00C7B920`:

1. closes the pending connection message box;
2. reports false as connection failure;
3. rejects unsolicited success and success for a cluster other than the
   retained request;
4. on a valid success, transitions to Avatar Selection; and
5. selects Jedi-only creation only when Jedi creation is allowed and the
   effective regular-creation getter denies regular creation.

The original build's regular-creation getter is contaminated by an installed
state global and therefore effectively bypasses normal server denial. This is a
confirmed legacy defect; a replacement should use the received permission bits
as policy and explicitly decide whether compatibility requires emulating it.
The server's fourth skip-tutorial byte is not consumed by this executable.

Connection-close messages return from Avatar Selection to Cluster Selection
when appropriate. Update `0x00C7B6F0` detects a dropped selected cluster,
reports it, and repairs the visible/session state rather than allowing a stale
transition.

## Cleanup and replacement contract

Deactivation `0x00C7A3C0` closes a retained message box, removes both typed
transceivers, clears table-owned row objects, unregisters three UI callbacks and
releases login-manager selection state. Late false, unsolicited, or wrong-ID
results are explicitly non-transitioning.

The replacement contract is a generation/attempt-scoped state machine:

`cluster rows -> selected record snapshot -> connection attempt ID -> permission result -> avatar selection`

Never accept an asynchronous result solely because a socket is open; match it
to the selected galaxy/attempt. Preserve zone and ping ports as distinct U16
wire fields and keep galaxy IDs at their legacy width.

## Confidence and remaining fixture

| Finding | Confidence | Evidence |
|---|---|---|
| factory and activation/deactivation lifecycle | Confirmed | vtables and `0x00C7A180` / `0x00C7A3C0` |
| table projection and status refresh | Confirmed route; exact localized wording not material | `0x00C7A730` and effective UI asset |
| validation and endpoint handoff | Confirmed | `0x00C7B220`, `0x0089A400`, `0x00462060` |
| permission continuation and wrong-attempt rejection | Confirmed | `0x00899340`, `0x00C7B920` |
| fourth server permission byte behavior | Confirmed ignored by this executable | parser/handler correlation |

A controlled-server fixture should vary status, capacity and permissions while
changing the selected row between request and reply. This is the next transport
fixture; it validates timing/state transitions rather than discovering the
static contract again.
