# Selected controller queue allocation, storage, and cleanup boundary

The selected chain now establishes the queue allocation request, null branch, exact controller `+4` writer, and matching cleanup/release call boundary. NetworkController delegates to **Controller**, whose constructor stores the return of `00AA6050` at `+4` and whose destruction body later reads that field.

The queue's concrete class/RTTI and internal layout/counts remain **Unknown**. The initializer's raw argument `0x10` is Confirmed; its meaning is Unknown. Owned returned storage is **Strongly inferred** from paired initialization and cleanup/release. Equality between the initializer's return and its allocation input is not proved.

## Input, scope refinement, and reproduction

All VAs use executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. [Verifier](../tools/python/verify_object_controller_queue_storage.py): explicit `--exe`, `--packages`, fresh ignored `--receipt`. [Independent runner](../poc/round56-tests/Networking/research_checks.py): explicit `--exe`, `--packages`, fresh ignored `--output-dir`. SHA rejection precedes package imports and VA use. These tools do not execute original instructions or contact a server.

The initial two-primary-body allowance covered NetworkController's 28-byte constructor and 11-byte destruction wrapper. An explicit lead refinement authorized `00B3D0F0` and `00B3D190` as two additional substantive bodies: **four primary bodies total**, including the thin wrappers. Two directly necessary 33-byte deleting thunks and one known four-byte accessor are counted separately. No further initializer/cleanup body is expanded; the fresh candidate limit remains 20.

The assigned artifact filename says ObjectController. Actual RTTI identifies NetworkController and Controller here; neither controller RTTI establishes queue RTTI, and no canonical function name/registry is changed.

## Exact storage and lifecycle observations

| Observation | Interpretation/confidence | Compatibility consequence |
| --- | --- | --- |
| Accepted `006E4A43` calls `00B4AE50`, whose `00B4AE5A` call forwards the same receiver/object argument to `00B3D0F0`. It later installs `015EC8F0`. | Confirmed NetworkController layer through installed vtable/RTTI. | Reuse the accepted player/creature/client carrier and ground map identity proofs. |
| Controller constructor saves receiver in ESI at `00B3D10C`, pushes `0x14` at `00B3D10A`, and calls allocation helper `00AC15C0` at `00B3D10E`. Non-null allocation is passed as ECX to `00AA6050` at `00B3D128`, with raw stack argument `0x10`. | Confirmed call/data flow and 20-byte request; allocation role is supported by the common allocation pattern. Queue internal allocation/class/count semantics remain Unknown. | Preserve construction of the queue field separately from profile/map acquisition. Do not mistake raw argument 16 for a recovered capacity. |
| `00B3D119/00B3D122` test the allocation and branch to `00B3D12F`, which sets EAX zero. The non-null path leaves the initializer's return in EAX; both converge to `00B3D134`, storing EAX into `[ESI+4]`. | Confirmed exact writer and allocation-null policy. Constructor return identity versus allocation input remains Unknown. | Store the returned queue reference; allocation returning null is represented by a null field at this boundary. Deeper initializer failures remain separately handled. |
| `00B3D13A` stores the original object argument at controller `+8`; `00B3D13D` installs `015EC5B4`, whose RTTI is Controller. | Confirmed common controller header observations: vtable at `+0`, returned queue field at `+4`, passed object reference at `+8`, all four-byte stores in this 32-bit executable. | Preserve logical relationships; a native 64-bit implementation need not reproduce these offsets or pointer widths. No full controller/queue layout is claimed. |
| NetworkController's actual destruction body `00B4AEA0` restores its vtable and tail-jumps at `00B4AEA6` to `00B3D190`, without receiver adjustment. | Confirmed selected base teardown entry. | Follow the exact base member-cleanup path. |
| `00B3D194` loads the stored field `[ESI+4]` into EDI; the zero test/branch skips both queue calls when null. Otherwise it passes that value as ECX to `00AA6310` at `00B3D1A3`, then as an argument to `00AC1640` at `00B3D1A9`. | Confirmed matching field identity and call order under normal return/ABI preservation. Cleanup/release roles and owned returned-object interpretation are Strongly inferred; helper internals are unexpanded. | The selected controller routine attempts cleanup/release of its stored queue before completion; input maps must not keep using that reference afterward. Complete scene detachment timing remains Unknown. |
| After the queue calls, `00B3D1B2` clears controller `+8`. This body never clears controller `+4`. Accessor `0062A9B0` reads four bytes at `+4`. | Confirmed local state writes and accessor width. | Do not infer reusable/idempotent destruction, a post-destruction null queue, or a live non-null guarantee from this routine. |

The [accepted carrier contract](PLAYER_CONTROLLER_QUEUE_LIFECYCLE_CONTRACT.md) and [ground map contract](GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md) remain the upstream evidence. The latter already establishes selected map `+0C = current controller+4`; its full audit is not repeated.

## RTTI, deleting thunks, and exact bodies

NetworkController: vtable `015EC8F0`, COL `01625554`, type descriptor `0186D08C`, slot-zero deleting thunk `00B4AE70`, body `00B4AEA0`. Controller: vtable `015EC5B4`, COL `016250F8`, type descriptor `0186AE78`, slot-zero deleting thunk `00B3D160`, body `00B3D190`.

Both 33-byte/14-instruction thunks retain the incoming receiver, call their body, then conditionally call `00AC1640` on that receiver when flags bit zero is set. Shape/order are Confirmed; deleting-destructor/allocation-release roles are Strongly inferred. Thus, for the selected Controller thunk on normal return, the stored queue's helper/release sequence precedes the thunk's conditional self-release. This is not a proof of the entire derived player destructor chain.

| Primary body | Bytes / instructions | Terminal instruction | SHA-256 |
| --- | --- | --- | --- |
| Network constructor `00B4AE50` | 28 / 12 | `ret 4` | `9d6d772d48527848b2226a72f470023a18363daa574fdda9502adc415614d5fd` |
| Network destruction `00B4AEA0` | 11 / 2 | base tail jump | `d04b89a92f0e594ac81ace05f87bfa83fc72a72454d9a44946927d2c250aee1d` |
| Controller constructor `00B3D0F0` | 99 / 33 | `ret 4` | `31441fd0e2da7aa47586f8c001c581e9ae32ecc3fefa0f5faca3fcf5dbc3171a` |
| Controller destruction `00B3D190` | 43 / 16 | `ret` | `1634c945005d87a200c3210b918288e9e1ec16080c9a38c93b646f69b89baf18` |

## Failure and ownership limits

The allocation-null path stores zero and returns the controller; the destructor skips queue cleanup/release for zero. This establishes what happens **if** the allocator returns null, not whether it returns null versus throws. The initializer return is stored without another local null check. Its failure behavior, allocation-return identity, internal buffers/counts, exception unwind cleanup, callback behavior, and the internals of `00AA6310`/`00AC1640` remain unclosed. If the cleanup helper does not return normally, the subsequent release and object-reference clear are not reached on this observed straight-line path.

The 20-byte request does not recover fields inside that requested allocation. Argument `0x10` does not establish a maximum queue count or ring size. Controller RTTI is not queue RTTI. Paired cleanup/release supports ownership as a strong inference, not complete allocator or lifecycle correctness.

The destructor re-reads the current `+4` member. It is not proved to remain the constructor's initial return after other methods/callbacks; queue-pointer replacement and intervening mutations remain Unknown. Replacement/current map detachment before the full player/controller destruction chain, current dynamic type/attachment at runtime, physical binding/profile, and actual event/live occurrence remain Unknown. No native production behavior is added.

## Validation and next probe

The final verifier pins 44 selected lifecycle anchors, four primary signatures/extents/exits, two deleting thunks, one accessor, two controller RTTI chains, and 13 selected call/jump/vtable candidate edges within the fresh cap of 20. This coincidental 44-anchor count is not the frozen producer audit; closed55 carriers and54ground-map full audits are not replayed.

The independent runner checks 17 byte witnesses, ten relative calls, two RTTI names, four data pointers, preserved wrapper carrier/tail, both deleting-thunk shapes, and separately parsed allocation/null/store and load/null/cleanup/release patterns. Twenty-two rejection cases cover identity, missing/truncated/unmapped inputs, altered instructions/RTTI/spans/edges/field offsets/null branches/masks/tail targets, invalid outputs, lexical link/reparse ancestry before resolution, and cap overflow. Fresh nested output and rejected output-state preservation pass. All are static or synthetic helper checks, not runtime lifetime verification.

Next bounded question: inspect initializer `00AA6050` and cleanup helper `00AA6310`. Establish queue concrete type if available, return identity, field layout and the meaning of raw argument 16, internal allocations/failure/cleanup, and update ownership confidence only when those exact bodies support it. Keep scene detachment timing and physical profile separate.


## Round57 initializer refinement

[Initializer and cleanup](OBJECT_CONTROLLER_QUEUE_INITIALIZATION_CONTRACT.md) now close the selected normal-return allocation identity, five outer fields/two records and reserve arithmetic. Payload ownership and concrete queue class remain Unknown;00AA67C0/00AA6370 are the next exact delegated pair. Earlier unclosed statements above describe the Round56 stopping point.
