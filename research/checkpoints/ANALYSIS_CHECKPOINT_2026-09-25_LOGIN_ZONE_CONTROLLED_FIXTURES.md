# Login/zone controlled fixture set

Date: 2026-09-25

## Result

`tools/python/verify_login_zone_fixtures.py` adds deterministic, credential-free
application-message vectors for:

- one-record `LoginEnumCluster`, including the separately registered footer;
- one-record `LoginClusterStatus`, keeping zone and ping ports distinct;
- four-byte denied and allowed `ClientPermissionsMessage` variants; and
- the tolerated historical three-byte permission variant.

The verifier independently generates the Engine3/SWG string hash and checks it
against the two fixed login opcodes before deriving the permissions opcode. It
then serializes and reparses the exact server-source layouts. It performs no
network access and is not replacement-client implementation.

All eight assertions passed on 2026-09-25. The vectors pin the following
synthetic record:

| Field | Value |
|---|---:|
| galaxy ID | 7 |
| name | `Stardust` |
| host | `127.0.0.1` |
| zone port | 44453 |
| ping port | 44462 |
| population/capacity | 100 / 3250 |
| reserved/current-server scalar | 8 |
| metadata | `0xFFFF8F80` |
| status/recommended | 2 / false |

## Controlled-server scenario matrix

The later live fixture should emit these same layouts under a test-only server
configuration and observe state, not production credentials:

| Scenario | Server sequence | Required client outcome |
|---|---|---|
| matched success | list/status ID 7; user selects 7; permission true | one connection attempt, then Avatar Selection |
| denied | selected ID 7; permission false | progress closes, failure shown, selected attempt cleared |
| stale success | select 7, change/cancel attempt, then success | no transition |
| wrong ID | select 7, active selection becomes another cluster before result | no transition; warning/diagnostic |
| status loss | selected ID 7 becomes unavailable/omitted | row/session repaired; no stale Next transition |
| capacity gate | population reaches capacity | reject unless explicit account/record policy bypass applies |
| legacy permissions | only three flag bytes | parse first three; represent skip-tutorial as absent |

These scenarios connect the byte grammar to the confirmed
`SwgCuiClusterSelection` state machine. Actual live execution remains a separate
controlled test because no test server process or disposable account was placed
in scope in this block.

## Next fixture boundary

Extend the same verifier with zone-open `ClientIdMsg`, `SelectCharacter`,
`CmdStartScene` and `CmdSceneReady` vectors, then specify loss/reconnect timing
at the transport boundary. Do not fold host pointer sizes into any serialized
field.
