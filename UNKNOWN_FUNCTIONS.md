# Unknown and Provisional Functions

## 2026-10-03 Round44 initialization prerequisites

The [creature template investigation](docs/CREATURE_VIEW4_INITIALIZATION_CONTRACT.md)
confirms constructor input identity through RTTI/vtables and concrete factory
006126D0. Getter011ADE40 resolves speed slots0/1;011AE700/011AEA90/011AEE20
resolve slope angle/percent and water percent. Local/parent loaded flags,
conditional zero fallback, parameter modes and relative operand order are
evidenced; effective values, globals/RNG and transitive later writers remain
Unknown. This supersedes the four getter-identity questions below without
assigning new canonical semantic names or changing the2626-address registry.

The [slope startup investigation](docs/TERRAIN_FILTER_SLOPE_INITIALIZATION_CONTRACT.md)
finds slot018560AC -> thunk00B77660 -> helper00B77670, storing exact3FC90FDB
to01947228 before ordinary application entry on the returning startup path.
Earlier-callback construction, later indirect writes and FSIN outcomes remain
Unknown. The [real height-order trace](docs/TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md)
identifies AHFR process00B768A0 as a shared state+18 cell writer preceding the
selected FHGT through root/earlier-sibling order; original fractal values and
derived-cache behavior remain separate open questions.

## 2026-10-03 CREO4 constructor input-getter follow-up

The SHA-gated [delivery investigation](docs/CREATURE_VIEW4_DELIVERY_CONTRACT.md)
adds58 direct constructor anchors. Five scalar fields0/1/4/5/10 receive F32 one
at construction; five others take results from the constructor's supplied object.
No new semantic names are assigned. The registry remains2626 distinct addresses
with zero duplicates. These observations do not establish a complete default
model or rule out subsequent template/subclass/other writes.

| Address | Observation / confidence | Exact unresolved question |
|---|---|---|
| `0x011ADE40` | Calls0042F0AA with argument1 and0042F0E0 with argument0; returned values spill F32 into registered fields11 and7. Confirmed caller/store chain. | Identify supplied object's type, getter branches/defaults/inheritance and effective values. |
| `0x011AE700` | Call0042F194 with argument0 feeds two multiplications then F32 field8 stores0042F1B8/BE. Confirmed graph. | Establish getter semantics, literal conversion role and active arithmetic controls. |
| `0x011AEA90` | Call0042F1D6 with argument0 spills into field9 stores0042F1EE/F4. Confirmed caller/store chain. | Establish getter semantics and overrides before first observer delta. |
| `0x011AEE20` | Call0042F20C with argument0 spills into field12 stores0042F226/229. Confirmed caller/store chain. | Establish getter semantics and effective constructor/template defaults. |

Next: follow these four callees and constructor0042ECF0 callers/input identity,
then reconcile all fourteen initial fields with observer deltas before
SceneEndBaselines. An all-zero network seed is not justified. No original
execution or live capture supports this follow-up.

## 2026-09-22 playback-script boundary

The `PBSC/0002` template grammar and scheduler are now bounded, but this is not a closure of all individual action semantics. The effective overlay has 32 scripts, 68 threads and 124 action nodes. One installed script contains `FIPL` and `PPRL`, which were not found among 25 direct factory registrations; its ordinary runtime reachability is unproven. See `research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PLAYBACK_SCRIPT_CORPUS_AND_FACTORY.md`.

Follow-up: Five installed `PSKA/0003` actions in four scripts exceed this executable's `PSKA/0002` ceiling. Their `INFO` extends seven to eleven bytes with a nonzero four-byte tag; its semantics and ordinary reachability remain unknown. The supported PSKA runtime does resolve a playback actor, require `SkeletalAppearance2` and apply an animation name/tag. See `research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PSKA_FORWARD_VERSION_AND_RUNTIME.md`.

| Address | Stable provisional name | Evidence / status |
|---:|---|---|
| `0x00402FA0` | `playback_template_cache_fetch_VA_00402FA0` | Confirmed cache/VFS fetch and null-on-missing path |
| `0x00404490` | `playback_template_pbsc_dispatch_VA_00404490` | Confirmed PBSC v0001/v0002 dispatch |
| `0x00404DF0` | `playback_template_load_v0002_VA_00404DF0` | Confirmed variables, thread list and action-factory dispatch; some INFO byte semantics open |
| `0x00404130` | `playback_action_factory_register_VA_00404130` | Confirmed tag/factory registration into lookup tree |
| `0x00407090` | `playback_action_factory_lookup_VA_00407090` | Confirmed key lookup and sentinel on miss |
| `0x00467370` | `playback_script_launch_from_action_VA_00467370` | Confirmed template fetch, instance creation and actor-variable setup |
| `0x00401AB0` | `playback_script_manager_alter_VA_00401AB0` | Confirmed active-script iteration, update and retirement |
| `0x004078C0` | `playback_script_alter_threads_VA_004078C0` | Confirmed per-thread action index, virtual update and timeout-like comparison |
| `0x005FA1E0` | `pska_template_version_dispatch_VA_005FA1E0` | Confirmed v0000–v0002 dispatch; installed v0003 unsupported by observed branch |
| `0x005FA4C0` | `pska_template_load_v0002_VA_005FA4C0` | Confirmed seven-byte INFO and optional PSTR/FTRK widths |
| `0x005F9AA0` | `pska_action_alter_VA_005F9AA0` | Confirmed one-time start then actor/animation state check |
| `0x005F9B50` | `pska_action_start_animation_VA_005F9B50` | Confirmed actor/SkeletalAppearance2 guard and animation-name/track application |
| `0x00407A20` | `playback_script_actor_by_index_VA_00407A20` | Confirmed bounds-checked actor table lookup at eight-byte stride |
| `0x00407D10` | `playback_script_string_variable_lookup_VA_00407D10` | Confirmed four-byte tag lookup in string-variable map, warning/null on missing tag |

## 2026-09-22 evening CLEF follow-up

`CPAP` is now behaviorally mapped through managed-particle lifetime,
expiration policy, randomized uniform geometry scale and instance time scale.
One literal `.prt ` trailing-space reference was previously misclassified as
live; the executable preserves the space and takes the default-appearance
fallback. All 87 CEF-reached empty-name PTEX shader slots are enabled and use
the installed `shader/3d_vertexcolorac.sht` fallback, rather than an unknown
missing-file path. See the CPAP and PTEX checkpoints dated 2026-09-22.

`CLGT`/CDF Light `+0x100..+0x108` are **closed to replacement depth** as the
ordered point-light attenuation coefficient vector. The typed visible-light
path reaches s207 shader-constant packing, which reads all three fields beside
point-light position and color. Constant/linear/quadratic member names are
strongly inferred from the order and `(1,0,0)` constructor default; only the
historic spellings and every legacy shader branch remain unknown. See
`research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_LIGHT_ATTENUATION_CONSUMER.md`.
The archive-backed
`.ffe` path passed to DirectInput is also an open runtime edge: SearchTree
formats `archive[member]`, and static analysis has not established whether a
separate materialization/interception layer makes it readable by
`EnumEffectsInFile`. See `ANALYSIS_CHECKPOINT_2026-09-22_FFBK_RESOLVED_PATH.md`.
The second CAMS scalar is stored at camera `+0x224`, but the traced per-frame
shake update does not read it. A direct-offset scan of the camera-family code
found no read; aliased or cross-module consumption is not excluded. Its units
and behavioral role remain unknown.

## 2026-09-22 secure-trade workflow closure

The secure-trade mediator family is no longer a material unknown. Stable names
now cover `SwgCuiTrade`, `SwgCuiTrade::Panel` and
`SwgCuiContainerProviderTrade` construction, state refresh, money validation,
acceptance emission, item-drop emission, verify gating, counterpart/range
validation, abort and teardown. Generic container/vector helpers remain unnamed
where their semantics are already supplied by the surrounding typed operation.
The client distance-limit constant's historical symbol is still unknown; this
does not change the server's authoritative `< 15.0f` rule or the replacement
contract. See `ANALYSIS_CHECKPOINT_2026-09-22_SECURE_TRADE_UI_WORKFLOW.md`.

## 2026-09-22 shader forward-version risk

`IMPL/0009` and `PASS/0010` are confirmed effective overlay versions beyond
this executable's `0007` and `0009` dispatcher ceilings. Direct reference scans
prove eight affected shaders feed 39 effective particle/environment assets.
The one-byte `PASS/0010` prefix extension remains a material Class C semantic
unknown; it must be retained in typed IR and resolved or visually validated
before claiming full original-asset compatibility. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHADER_FORWARD_VERSION_REACHABILITY.md`.

## 2026-09-22 crafting controller closure

The crafting ObjectController codec layer is no longer an unknown. Inner types
`0x102`, `0x106`, `0x107`, `0x108`, `0x15A`, `0x10C`, `0x1BE`, `0x113`, and
`0x1C2` have stable field widths, direction policy and server-side effects.
Remaining crafting work is mediator/session result semantics, not byte framing.
See `ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_CONTROLLER_CODECS.md`.

The `CuiCraftManager` response coordinator is now behaviorally closed through
counter matching, replicated-stage validation and typed mediator events. Exact
localized names for all numeric assembly/experiment outcomes and two dormant
legacy generic operations remain presentation-level work. See
`ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_RESPONSE_STATE_MACHINE.md`.

## 2026-09-21 final asset-runtime closures

These stable identifiers close the late-session glow, movable-wing and generic child-motion runtime paths. Their presence in this historical ledger does not mean they remain unknown.

| Address | Provisional name | Status |
|---:|---|---|
| `0x00700040` | `GlowData_createAppearance_VA_00700040` | Confirmed passes the two shader variants, RGBA and angular size profile into GlowAppearance |
| `0x007137C0` | `GlowAppearance_ctor_VA_007137C0` | Confirmed creates the local glow primitive and finalizes visibility state |
| `0x00713260` | `GlowQuadPrimitive_ctor_VA_00713260` | Confirmed retains shader pair, color and three-point size profile |
| `0x007134C0` | `GlowQuadPrimitive_render_VA_007134C0` | Confirmed camera-side shader selection, size interpolation and billboard generation |
| `0x00713C00` | `GlowAppearance_finalize_VA_00713C00` | Strong installs primitive bounds and DPVS visibility object |
| `0x006FFA10` | `WingTransitionDynamics_ctor_VA_006FFA10` | Confirmed retains transform, target angle, duration and optional sound |
| `0x006FFB30` | `WingTransitionDynamics_alter_VA_006FFB30` | Confirmed state-change sound and elapsed-over-duration interpolation |
| `0x00B353E0` | `RotationDynamics_ctor_VA_00B353E0` | Confirmed RTTI-backed continuous angular-velocity controller |
| `0x00B35470` | `RotationDynamics_alter_VA_00B35470` | Confirmed delta-time-scaled three-axis rotation update |
| `0x00B4AAB0` | `SeesawDynamics_ctor_VA_00B4AAB0` | Confirmed RTTI-backed axis/frequency/amplitude controller |
| `0x00B4AB70` | `SeesawDynamics_alter_VA_00B4AB70` | Confirmed sinusoidal symmetric angular update |
| `0x00B4A830` | `SpringDynamics_ctor_VA_00B4A830` | Confirmed RTTI-backed positional oscillation controller |
| `0x00B4A960` | `SpringDynamics_alter_VA_00B4A960` | Confirmed directional sine translation about retained equilibrium |
| `0x0061D700` | `TurretObject_ctor_VA_0061D700` | Confirmed RTTI-backed installation-turret runtime object |
| `0x0061D930` | `TurretObject_setBarrelConstraints_VA_0061D930` | Strong retains barrel object and angular parameters |
| `0x0061D970` | `TurretObject_setWeaponAndProjectileTravel_VA_0061D970` | Confirmed weapon template, muzzle transform, speed and lifetime retention |
| `0x0061DA90` | `TurretObject_alter_VA_0061DA90` | Confirmed tracking, constrained aim and projectile creation |
| `0x006F5D00` | `Projectile_setSpeed_VA_006F5D00` | Confirmed forward velocity setup |
| `0x006F5CF0` | `Projectile_setLifetime_VA_006F5CF0` | Confirmed elapsed-time expiry field |
| `0x00614AF0` | `ShipObjectDestruction_ctor_VA_00614AF0` | Confirmed RTTI-backed staged destruction constructor |
| `0x006155B0` | `ShipObjectDestruction_alter_VA_006155B0` | Confirmed split/debris/effects/final-cleanup update |
| `0x006149E0` | `ShipObjectDestruction_install_VA_006149E0` | Strong install/remove and configuration boundary |
| `0x006FD560` | `ShipDestructionSequenceData_getBreakpoints_VA_006FD560` | Confirmed `SPTL/BRKP` vector accessor |
| `0x006FD580` | `ShipDestructionSequenceData_getAmbientSounds_VA_006FD580` | Confirmed `ASNL/SDAS` vector accessor |
| `0x006FD590` | `ShipDestructionSequenceData_getTopScalar_VA_006FD590` | Confirmed accessor; scalar semantics remain open |
| `0x006FD5A0` | `ShipDestructionSequenceData_getPrimaryEffectPair_VA_006FD5A0` | Confirmed first top-level pair accessor |
| `0x006FD5B0` | `ShipDestructionSequenceData_getFinalEffectPair_VA_006FD5B0` | Strong terminal pair accessor |
| `0x006FD5C0` | `ShipDestructionSequenceData_applySpawnEvents_VA_006FD5C0` | Confirmed SDOE iteration/attachment path |
| `0x006EDF20` | `ShipObject_buildContrailsRecursive_VA_006EDF20` | Confirmed selection, hardpoint resolution and recursive trail construction |
| `0x00700F30` | `ContrailData_createSwooshAppearance_VA_00700F30` | Confirmed one- or two-ended SwooshAppearance creation |
| `0x007B19D0` | `SwooshAppearance_setWidth_VA_007B19D0` | Confirmed full ribbon-width setter |

## 2026-09-13 marathon closure: particles and server-reachable messages

The following replacement-relevant functions moved from anonymous or broad family status to stable behavioral names:

| Address | Provisional name | Status |
|---:|---|---|
| `0x007A2660` | `particle_quad_prepare_cpu_vertex_batch` | Confirmed six-vertex-per-particle dynamic triangle batch, PTCL/PTQD/PTEX sampling, local-space transform and environment-color blend |
| `0x008ABDA0` | `chat_system_message_listener_receive` | Confirmed type check and deserialization entry |
| `0x008ABEB0` | `chat_system_message_publish` | Confirmed presentation construction and registered-consumer fanout |
| `0x009CAA40` | `chat_system_message_build_presentation` | Confirmed display policy plus literal/localized prose payload construction |
| `0x00AEE970` | `chat_system_message_deserialize` | Confirmed display byte and two text/prose fields |
| `0x00522440` | `ground_scene_client_effect_listener` | Confirmed object/transform/location effect family dispatch |
| `0x00ADCB60` | `play_client_effect_object_message_deserialize` | Confirmed effect, auxiliary name and object ID codec |
| `0x00ADD2E0` | `play_client_effect_loc_message_deserialize` | Confirmed effect, zone, XYZ, cell and terrain-delta codec |
| `0x0051CDDB` | `scene_create_client_path_message_handler` | Confirmed replacement of retained debug path from float3 vector |
| `0x0051CECF` | `scene_destroy_client_path_message_handler` | Confirmed retained debug-path destruction/reset |
| `0x00620AA0` | `client_debug_path_object_ctor` | Confirmed construction from a decoded coordinate vector |

EMTR `+0x20C/+0x210` remain deliberately unnamed compatibility floats: both are serialized but zero in all 5,562 effective 0012-0014 records and have no live simulation/render consumers. This is a bounded dormant-data unknown, not an unresolved active function. PTMH resource identity is now general appearance-factory data: 399 records reference 275 APT, 79 MSH and 45 SAT resources.

The first server packet gap matrix contains 202 documentation hits and 143 source-referenced candidates. Only 66 candidates expose a top-level literal opcode; structural rows must be reclassified before being counted as missing handlers. ChatSystem, PlayClientEffectObject, PlayClientEffectLoc, CreateClientPath and DestroyClientPath were closed in this session.

## PTCL/PTQD visual-description closure (2026-09-13)

| Address | Provisional name | Status |
|---:|---|---|
| `0x007BF140` | `particle_description_common_load` | Confirmed PTCL 0000-0003 dispatch and alpha clamp |
| `0x007B7B70` | `particle_description_quad_load` | Confirmed modern and legacy PTQD dispatch |
| `0x007B8140` | `particle_description_quad_load_detail_v0000` | Confirmed three waveforms plus PTEX |
| `0x007B81A0` | `particle_description_quad_load_detail_v0001` | Confirmed dimension-lock addition |
| `0x007B7220` | `particle_texture_load` | Confirmed PTEX/0000-only dispatch |
| `0x007B7100` | `particle_texture_calculate_animated_uv` | Confirmed frame selection and UV calculation |
| `0x007C2080` | `particle_description_mesh_load` | Confirmed PTMH/common/detail dispatch |
| `0x007C2200` | `particle_description_mesh_load_detail_v0000` | Confirmed appearance-template path plus uniform-scale and three rotation waveforms |

Quad fields are closed: `+0xAC` rotation, `+0xC4` primary size, `+0xDC` secondary size, `+0xF4` dimension lock. PTMH fields are also closed: `+0xAC` uniform scale, `+0xC4/+0xDC/+0xF4` three rotations, and `+0x110` appearance-template name. Remaining items are field-label unknowns rather than unknown functions.

## PATT attachment closure (2026-09-13)

| Address | Provisional name | Status |
|---:|---|---|
| `0x007C23C0` | `particle_attachment_description_ctor` | Confirmed defaults and 0x24-byte legacy layout |
| `0x007C2880` | `particle_attachment_description_load` | Confirmed PATT 0000/0001 dispatcher |
| `0x007C2A00` | `particle_attachment_description_load_v0000` | Confirmed legacy fields; five words discarded; trigger defaults 2 |
| `0x007C2B90` | `particle_attachment_description_load_v0001` | Confirmed current path/range/kill/trigger schema |
| `0x007A72F0` | `particle_emitter_spawn_particle_attachment` | Confirmed appearance creation and transform initialization |
| `0x007C4B40` | `particle_attachment_runtime_dtor` | Confirmed conditional child termination from kill-with-parent flag |

PATT control flow is closed. Exact historical editor wording and the purpose of PATT/0000's discarded fields remain non-runtime metadata unknowns.

## PTEX sprite-animation closure (2026-09-13)

| Address | Provisional name | Status |
|---:|---|---|
| `0x007B7370` | `particle_texture_load_v0000` | Confirmed shader path, atlas, frame, FPS and enabled schema |
| `0x007B7100` | `particle_texture_calculate_animated_uv` | Confirmed normalized-life and explicit-FPS frame selection |
| `0x007B75F0` | `particle_texture_set_atlas_capacity` | Confirmed 1/4/16 to 1/2/4 row mapping and UV extent derivation |
| `0x007A15C0` | `particle_quad_local_shader_primitive_ctor` | Confirmed enabled-gated PTEX shader acquisition |

PTEX `+0x2C` is no longer unknown: it enables shader/resource binding. `+0x28` is frames per second with `-1` selecting normalized-lifetime animation.

Despite the historical filename, this is a **provisional function-name ledger**, not a list of wholly unknown functions. An entry belongs here whenever the original symbol is unavailable, even when behavior is confirmed. Confidence and remaining replacement relevance must be read from each row. `FUNCTION_UNCERTAINTY_AUDIT.md` separately tracks the much smaller set whose behavior or data contract is still materially unresolved.

## Naming policy

Use `subsystem_role_VA_XXXXXXXX`. Addresses are valid only for SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. If later evidence changes a role, retain the old identifier as an alias/history note.

## Authentication message candidates

| VA/region | Provisional name | Current evidence | Confidence | Next action |
|---|---|---|---|---|
| near `0x00ACAF20` | `stardust_protocol_version_init_VA_00ACAF20` | initializes global string `0x01946468` from `20260817-SD:01` | Confirmed | recover exact start and registration caller |
| `0x00ACA1B0` | `login_client_id_message_ctor_VA_00ACA1B0` | builds named `LoginClientId` message; registers username at `+0x38`, password at `+0x48`, version at `+0x58`; defaults version to `20260817-SD:01` unless override is configured | Confirmed construction and field order; credential labels confirmed by server parser | identify maximum accepted string lengths |
| `0x0069BC30` | `login_connection_opened_sender_VA_0069BC30` | on `LoginConnectionOpened`, obtains singleton strings at `+0x14/+0x20`, constructs `LoginClientId`, submits with framing flag 1 through `0x005A8D60` | Confirmed | exact legacy connection-event callback affinity remains incomplete; replacement queues it to session owner |
| `0x00AE4090` | `client_id_msg_ctor_VA_00AE4090` | registers gameBits, exact length-delimited credential bytes, and version; defaults version to `20260817-SD:01` | Confirmed | identify runtime gameBits initialization |
| `0x0069B430` | `connection_server_opened_sender_VA_0069B430` | handles `ConnectionServerConnectionOpened`; gets credential pointer/length through `0x004636D0`/`0x004636E0`, gameBits through `0x005035B0`, constructs and submits `ClientIdMsg` | Confirmed | exact legacy connection-event callback affinity remains incomplete; replacement queues it to session owner |
| `0x00AEC5B0` | `client_permissions_parse_VA_00AEC5B0` | parses exactly three one-byte permission fields despite current server's four-byte payload | Confirmed | test tolerance of truncated/extended payloads |
| `0x00899340` | `client_permissions_handler_VA_00899340` | publishes first permission and stores regular/Jedi creation permissions; ignores server's fourth tutorial byte | Confirmed mechanics; labels source-correlated | identify concrete observer/UI consumers |
| `0x00C7B2C9` region | `swg_cluster_selection_connect_flow_VA_00C7B2C9` | resolves selected galaxy; gates unavailable/loading/locked/missing endpoint/full capacity; drops mismatched existing connection; opens requested cluster | Confirmed control flow; individual localized labels Strong | map every UI token to its exact branch |
| `0x0089A400` | `cui_login_manager_open_cluster_VA_0089A400` | passes selected record's resolved address, zone port, and name to GameNetwork; records selected galaxy ID | Confirmed | identify exact cluster-record class RTTI |
| `0x00462060` | `game_network_open_connection_server_VA_00462060` | closes prior connection, constructs/stores fresh `ConnectionServerConnection`, retains cluster name | Confirmed | recover default connection-options fields |
| `0x00462330` | `game_network_close_connection_server_VA_00462330` | disconnects current connection-server object, clears singleton slot, broadcasts state change | Confirmed | identify close reason passed into UDP layer |
| `0x0069B2F0` | `connection_server_connection_ctor_VA_0069B2F0` | forwards address, 16-bit port, and options to base connection constructor; initializes lag state and vtables | Confirmed | decode every connection option and timeout |
| `0x00AE4B60` | `select_character_message_ctor_VA_00AE4B60` | constructs named `SelectCharacter` with exactly one registered 64-bit object ID field | Confirmed | none for minimum wire mapping |
| `0x00C62BB8` region | `avatar_selection_send_selected_character_VA_00C62BB8` | supplies selected avatar object ID to constructor and sends through `0x00462670` | Confirmed | name containing mediator method boundary |
| `0x00AC9E20` | `cmd_start_scene_parse_VA_00AC9E20` | parses byte, LE64 player ID, terrain string, X/Z/Y floats, template string, LE64 galactic time | Confirmed | characterize malformed-string behavior |
| `0x00462E60` | `game_network_cmd_start_scene_handler_VA_00462E60` | gates scene intent, stores player ID, begins scene transition; does not synchronously acknowledge | Confirmed | label scene-transition arguments/types |
| `0x00462AE0` | `scene_transition_begin_VA_00462AE0` | receives parsed scene identity/location/time and initiates scene/resource setup | Strong role; argument flow Confirmed | trace terrain/resource completion predicates |
| `0x00519C40` | `ground_scene_ctor_VA_00519C40` | constructs the new 0xF4-byte scene from terrain/player/location/time parameters supplied by CmdStartScene | Confirmed construction boundary | identify embedded manager fields |
| `0x00424220` | `game_set_scene_VA_00424220` | replaces global Scene, clears/publishes scene state and updates dependent global systems | Confirmed | name every subscriber call |
| `0x00423700` | `game_cleanup_scene_VA_00423700` | invokes current scene cleanup virtual and calls `game_set_scene(nullptr)` | Confirmed | name cleanup virtual slot |
| `0x008802F0` | `cui_manager_install_VA_008802F0` | constructs process-lifetime UI manager/canvas, service callbacks and input/message listeners; loads/registers UI resources | Confirmed broad behavior | label resource-loader and mediator registry calls |
| `0x00AC99F0` | `cmd_scene_ready_ctor_VA_00AC99F0` | constructs header-only named `CmdSceneReady` | Confirmed | none for minimum wire mapping |
| `0x00462790` | `cmd_scene_ready_sender_VA_00462790` | checks zone connection, constructs and sends `CmdSceneReady` | Confirmed | map failure behavior when connection disappears |
| `0x0051B428` | `scene_ready_ack_trigger_VA_0051B428` | sole direct sender caller after readiness predicate conjunction; sets one-shot flag | Confirmed mechanics | name all readiness predicates |
| `0x0051C206` | `server_scene_ready_dispatch_VA_0051C206` | handles later inbound `CmdSceneReady`, notifies scene subsystems, sets final-ready flag | Confirmed mechanics | name notified subsystem instances |
| `0x005A8D60` | `message_submit_VA_005A8D60` | invokes message virtual serializer at vtable `+8`, then submits buffer to `0x012B7F60` | Confirmed | identify metadata field and downstream queue |
| `0x012B7F60` | `serialized_message_transport_submit_VA_012B7F60` | false selects mode 0; true enforces pending-byte capacity and selects reliable mode 4; passes original buffer without a length prefix | Confirmed | identify any call sites intentionally using mode 0 |
| `0x012BE540` | `tcp_overlapped_send_VA_012BE540` | TCP subsystem; prepares OVERLAPPED state and calls `WSASend` | Confirmed | identify actual TCP consumers; do not label as game transport |
| `0x012BE870` | `tcp_overlapped_receive_VA_012BE870` | TCP subsystem; prepares OVERLAPPED state and calls `WSARecv` | Confirmed | identify actual TCP consumers |
| `0x012BE5D0` | `tcp_flush_send_buffer_VA_012BE5D0` | flushes TCP object buffer through `0x012BE540` | Confirmed | identify containing TCP class via RTTI |
| `0x012BA8D0` | `udp_socket_ctor_VA_012BA8D0` | RTTI `UdpSock`; calls `socket(AF_INET, SOCK_DGRAM, 0)` | Confirmed | map utility/ping versus game uses |
| `0x012C1230` | `udp_manager_socket_open_VA_012C1230` | opens/binds nonblocking datagram socket used by manager receive/send loops | Confirmed | recover containing manager type/config fields |
| `0x012C1D02` | `udp_manager_recvfrom_site_VA_012C1D02` | receives datagrams with source address | Confirmed | trace datagram decode branches |
| `0x012C20ED` | `udp_manager_sendto_site_VA_012C20ED` | sends constructed datagram to peer address | Confirmed | trace final CRC/transform producer |
| `0x012C2190` | `udp_manager_packet_dispatch_VA_012C2190` | handles session/mapping control before established-peer lookup; accepts valid 14-byte version-2 session requests | Confirmed behavior | label `1D`/`1E` management packets |
| `0x012C36B0` | `udp_connection_packet_receive_VA_012C36B0` | established receive accounting, seeded CRC trailer validation, inverse callbacks for the two negotiated selectors, SOE dispatch; 2026-09-29 direct instructions confirm no standalone opcode-08 CRC exception at positive width | Confirmed | characterize malformed-transform failure results; resolve operational server zero-trailer mismatch |
| `0x012C3B60` | `soe_opcode_dispatch_VA_012C3B60` | compact dispatch for leading-zero SOE opcodes and channel variants | Confirmed | finish `15`-`19` branch boundary |
| `0x012C3D0E` | `session_response_handler_VA_012C3D0E` | verifies echoed connection ID; parses seed, CRC width, two transform selectors, and maximum datagram; installs transforms and marks connected | Confirmed | recover callback failure/error paths |
| `0x012C3FB5` | `soe_multi_packet_handler_VA_012C3FB5` | walks one-byte length blocks and recursively dispatches embedded SOE packets | Confirmed | test extended-length behavior |
| `0x012C3FF7` | `soe_net_status_request_handler_VA_012C3FF7` | parses 40-byte request and builds `00 08`: tick, local timestamp, two echoed peer counters and two local counters; structure Confirmed, packet-counter labels Strong | Confirmed behavior | label five request words; resolve short server request policy |
| `0x012C446F` | `soe_net_status_response_handler_VA_012C446F` | parses large timing/statistics response and updates connection metrics | Strong | label every field |
| `0x012C484D` | `soe_reliable_channel_dispatch_VA_012C484D` | maps data `09`-`0C` and fragment `0D`-`10` forms to four channel indices, creates receiver state, calls `0x012C7CF0` | Confirmed mechanics; labels Strong | analyze data/fragment receiver state machine |
| `0x012C48D2` | `soe_out_of_order_selective_ack_dispatch_VA_012C48D2` | maps `11`-`14` to per-channel state, expands 16-bit sequence, selectively releases that exact retained entry via `0x012C82C0`; does not directly resend | Confirmed | characterize wraparound and malformed references |
| `0x012C4921` | `soe_acknowledgement_channel_dispatch_VA_012C4921` | maps `15`-`18` to per-channel state and calls `0x012C8210` | Strong label; Confirmed mechanics | analyze cumulative acknowledgement handling |
| `0x012C493E` | `soe_multipayload_handler_VA_012C493E` | walks variable-length blocks following opcode `19` and recursively dispatches embedded application material | Strong label; Confirmed mechanics | identify variable-length prefix decoder `0x012C8EC0` |
| `0x012C7CF0` | `reliable_channel_receive_VA_012C7CF0` | expands 16-bit sequence, delivers expected packet, buffers future packets, drains contiguous entries, emits ACK or out-of-order control | Confirmed | recover window capacity/configuration |
| `0x012C80E0` | `reliable_payload_or_fragment_VA_012C80E0` | direct payload dispatch for data; fragment form reads peer U32 total and allocates it without a cap; first-fragment four-byte minimum is not checked; overlong continuation throws | Confirmed | define a replacement-only bounded reassembly policy and fuzz fixtures |
| `0x012C8210` | `acknowledgement_receive_VA_012C8210` | expands received 16-bit ACK and cumulatively releases retained outgoing sequences | Confirmed | characterize wraparound limits |
| `0x012C82C0` | `release_acked_packet_VA_012C82C0` | locates retained send entry, updates timing/statistics, releases message, clears slot | Confirmed | map adaptive retransmit calculation |
| `0x012BD9D0` | `udp_adapter_send_VA_012BD9D0` | adapter lock/config branch; forwards active UDP connection to `0x012C3020` | Strong | resolve alternate instrumentation path |
| `0x012C3020` | `udp_connection_buffer_send_VA_012C3020` | requires connected state, accounts send request, forwards buffer spans to `0x012C3180` | Confirmed | label send arguments |
| `0x012C2810` | `outbound_udp_connection_ctor_VA_012C2810` | initializes outbound state 0, timeout, random connection ID, registration, and pump scheduling | Confirmed | name owning factory/class |
| `0x012C2FB0` | `disconnect_packet_send_VA_012C2FB0` | constructs and raw-sends eight-byte `00 05` disconnect control packet | Confirmed | resolve reason/field semantics |
| `0x012C3180` | `soe_send_dispatch_VA_012C3180` | modes 0/1 aggregate/immediate; 2/3 internal `00 1B` aggregate/immediate; 4-7 reliable channels 0-3; oversized non-reliable traffic promotes to mode 4 | Confirmed routing | recover original public enum labels for modes 0-3 |
| `0x012C4A80` | `connection_tick_VA_012C4A80` | state pump; in state 0 enforces timeout and periodically emits exact 14-byte session request | Confirmed | map all established-state maintenance branches |
| `0x012C5980` | `datagram_prepare_VA_012C5980` | preserves SOE opcode prefix, applies two configured callbacks, appends seeded 1-4 byte big-endian CRC-32 truncation, raw-sends | Confirmed | characterize callback failure/capacity behavior |
| `0x012C5B60` | `soe_multipacket_aggregate_VA_012C5B60` | accumulates bounded `00 03` length-prefixed blocks and flushes via final preparation | Strong | establish all size/flush thresholds |
| `0x012C54B0` | `udp_connection_raw_send_VA_012C54B0` | updates counters/timestamps and calls manager raw-send `0x012C1F20` | Confirmed | map scheduler interaction |
| `0x012C63C0` | `transport_transform_install_VA_012C63C0` | loops over two selectors: 0 identity, 1 provider zlib pair, 2 concrete provider copy pair, 3 generated-pad/cipher-chain pair, 4 seed-only chain; deployed response selects 1 then 4 | Confirmed | historic labels for 2/3 are non-behavioral unknowns |
| `0x012C6120` / `0x012C6150` | `transport_identity_pair_VA_012C6120_6150` | mode-0 copy/no-transform callback pair | Confirmed mechanics | verify overlap and capacity behavior |
| `0x012C6180` / `0x012C61B0` | `transport_zlib_pair_VA_012C6180_61B0` | delegates to configured shared `ZlibCompressor` provider virtual methods; selected by current server's first selector | Confirmed | recover exact provider wrapper return/capacity contract |
| `0x012C61E0` / `0x012C6210` | `transport_provider_secondary_copy_pair_VA_012C61E0_6210` | delegates to provider slots `+0x0C/+0x10`; concrete methods `0x012BD590/0x012BD5C0` copy exactly `length` bytes and return `length` | Confirmed | behavior closed; historic public label only |
| `0x012C6240` / `0x012C62B0` | `transport_seed_pad_chain_pair_VA_012C6240_62B0` | generated pad XOR plus previous-cipher chain per complete LE32 word; tail bytes use pad only; runnable vectors preserved | Confirmed | closed |
| `0x012C8CD0` | `transport_seed_pad_next_byte_VA_012C8CD0` | Schrage-form `16807` recurrence with `+123`, modulus `2147483647`, emits low byte | Confirmed | closed |
| `0x00A88B20` | `zlib_compress_wrapper_VA_00A88B20` | constructs a zlib stream using pooled allocation callbacks, calls embedded deflate initialization/run/finalization, returns produced size or `-1` | Confirmed | map four arguments and exact level/window defaults |
| `0x00A88BE0` | `zlib_decompress_wrapper_VA_00A88BE0` | constructs a zlib stream using pooled allocation callbacks, calls embedded inflate initialization/run/finalization, returns produced size or `-1` | Confirmed | characterize truncated/oversized input results |
| `0x012C6320` / `0x012C6370` | `transport_seed_chain_xor_pair_VA_012C6320_6370` | complementary seed-chained XOR word/tail transforms selected by current server's second selector | Confirmed mechanics; server encryption correlation Strong | create encode/decode vectors |
| `0x012C8D30` | `seeded_crc32_VA_012C8D30` | table-driven SOE CRC-32 using all seed bytes and final complement; matches server `BaseProtocol::generateCrc` | Confirmed | create byte-exact golden vectors |
| `0x012C6650` | `reliable_channel_ctor_VA_012C6650` | constructs per-channel queue/sequence tables and derives packet thresholds from negotiated size/overheads | Confirmed | label configuration fields |
| `0x012C6A40` | `reliable_channel_submit_VA_012C6A40` | direct/multipayload decision; oversized payload ownership and fragment-queue entry | Confirmed | verify two-span argument semantics |
| `0x012C7210` | `reliable_multipayload_append_VA_012C7210` | builds bounded `00 19` container with variable-length blocks and flushes on capacity | Confirmed | map aggregation flush timing |
| `0x012C7340` | `reliable_queue_append_VA_012C7340` | appends owned message to channel send queue and accounts queued bytes/items | Confirmed | identify ownership type RTTI |
| `0x012C73B0` | `reliable_sender_pump_VA_012C73B0` | assigns sequence, selects data/fragment opcode by channel, creates retained entries, sends within window | Confirmed | recover exact window policy fields |
| `0x012C7530` | `reliable_resend_check_VA_012C7530` | timer-scans retained sequences, rebuilds data/fragment prefix with the existing sequence, resubmits expired payloads through aggregation, adjusts schedule, disconnects with reason 8 on excessive delay | Confirmed | resolve delay units and exact retry thresholds |
| `0x012C8E60` | `variable_length_size_encode_VA_012C8E60` | encodes lengths in 1/3/7-byte forms | Confirmed | golden vectors at boundary values |
| `0x012C8EC0` | `variable_length_size_decode_VA_012C8EC0` | decodes matching 1/3/7-byte length forms | Confirmed | malformed/truncated-input behavior |
| `0x012B3930` | `connection_infrastructure_ctor_VA_012B3930` | allocates `UdpManagerHandlerMT` adapter through `0x012BCF50` | Confirmed | map manager callbacks to Connection methods |
| `0x012B7840` | `tcp_receive_frame_reassembler_VA_012B7840` | accumulates four-byte length-prefixed stream frames; sole caller `0x012BE80B` is adjacent to overlapped `WSARecv` path | Confirmed | identify higher-level TCP consumers |
| `0x012B4120` | `received_frame_dispatch_VA_012B4120` | creates/reuses pooled work item, references connection, copies complete frame, queues it | Confirmed | locate work-item consumer and decoder |
| `0x012B79B0` | `connection_received_message_VA_012B79B0` | receive accounting, optional overflow callback, then primary vtable `+0x28` message delivery | Confirmed | identify where its message argument is decoded/constructed |
| `0x005A8BF0` | `connection_overflow_event_VA_005A8BF0` | constructs literal `ConnectionOverflowing` event and publishes it | Confirmed | identify overflow argument type |
| `0x00AC2AB0` | `connection_event_dispatch_VA_00AC2AB0` | keyed listener lookup, virtual listener iteration, then global dispatch | Confirmed behavior | recover concrete observer/message class names |
| `0x0069B510` | `connection_server_named_message_VA_0069B510` | primary vtable `+0x28`; recognizes three lag-control message names, updates lag state, forwards all messages | Confirmed behavior and caller | map additional ConnectionServer observers |
| `0x005A8C50` | `connection_message_dispatch_VA_005A8C50` | common Login/ConnectionServer named-message forwarding and traffic accounting | Strong | identify destination dispatcher type and archive wrapper |
| `0x00ACB790` | `game_network_message_from_stream_VA_00ACB790` | constructs temporary message, reads 2-byte header plus registered 32-bit opcode, retains remaining archive | Confirmed structure; field semantics Strong | identify concrete MSVC class name through RTTI |
| `0x00ACB9E0` | `game_network_message_archive_VA_00ACB9E0` | returns message outer-object offset `+0x18`, used by typed parsers | Confirmed | determine exact archive iterator type |
| `0x00ACA8F0` | `login_client_token_parse_VA_00ACA8F0` | creates and archive-parses typed `LoginClientToken` object | Confirmed | label three fields |
| `0x00462DD0` | `login_client_token_handler_VA_00462DD0` | copies complete credential blob (token plus embedded account ID), stores station ID and username, clears pending flag | Confirmed | trace opaque blob into zone-login message |
| `0x00AC90A0` | `error_message_parse_VA_00AC90A0` | archive-parses two strings and one byte for named `ErrorMessage` | Confirmed | determine all non-login consumers |
| `0x00461B58` | `game_network_error_message_branch_VA_00461B58` | dispatches `ErrorMessage`, invokes typed parser, reports when fatal flag is nonzero | Confirmed | map nonfatal UI destination |
| `0x00461770` | `game_network_inbound_dispatch_VA_00461770` | exact dispatcher entry; handles connection lifecycle events and named application messages, including `ConnectionServerConnectionOpened` at `0x004619B3` | Confirmed | finish remaining early-zone branches |
| near `0x008986C0` | `login_ui_message_dispatch_VA_008986C0` | dispatches cluster enumeration/status, character enumeration, permissions via name hashes | Confirmed | exact entry byte and owner class |
| `0x00898AC0` | `login_enum_cluster_handler_VA_00898AC0` | clears and rebuilds cluster-name containers | Confirmed | name container classes and completion signal |
| `0x00898C70` | `login_cluster_status_handler_VA_00898C70` | joins status records to clusters, sets availability/status fields, constructs hostname+zone-port address keys, maps them to galaxy IDs | Confirmed | locate UI selection caller that opens zone connection |
| `0x00898F80` | `enumerate_character_id_handler_VA_00898F80` | rebuilds character selection records and associates each LE64 object ID with its galaxy | Confirmed | locate selection sender using chosen object ID |
| `0x00AEDA70` | `login_enum_cluster_message_ctor_VA_00AEDA70` | constructs/registers a vector of `0x14`-byte ID/name/metadata records and a separate LE32 footer; handler ignores footer | Confirmed | original footer name only; preserve as opaque LE32 value `8` for current-server compatibility |
| `0x00AEDE30` | `login_enum_cluster_record_vector_decode_VA_00AEDE30` | decodes count and repeated ID, ASCII16 name and LE32 metadata records into 20-byte elements | Confirmed | bounds/failure behavior belongs to shared archive layer |
| `0x00AEC9D0` | `login_cluster_status_message_ctor_VA_00AEC9D0` | constructs/registers vector of `0x2C`-byte endpoint/status records | Confirmed | original labels at record `+0x1C/+0x20` remain provisional |
| `0x00AECDB0` | `login_cluster_status_record_vector_decode_VA_00AECDB0` | decodes ID, ASCII16 host, two U16 ports, five U32 values and final U8 into each 44-byte record | Confirmed | shared archive bounds/failure behavior |
| `0x00AE4800` | `enumerate_character_id_parse_VA_00AE4800` | archive-parses vector of `0x20`-byte name/race/OID/galaxy/type records | Confirmed | malformed count/length behavior |
| `0x00AD51B0` | `scene_create_by_crc_parse_VA_00AD51B0` | archive-parses object ID, 48-byte transform, template CRC and flag | Confirmed | identify concrete archive transform type |
| `0x0051E1B3` | `scene_create_shared_handler_VA_0051E1B3` | common name/CRC create branch; resolves template, reconciles ID, instantiates and initializes object | Confirmed behavior | label every subordinate factory/notification call |
| `0x00B28AA0` | `client_template_lookup_by_crc_VA_00B28AA0` | consumes create-message CRC and returns a template-name/result wrapper; missing result is logged | Confirmed behavior | map owning template registry and load path |
| `0x00B2E760` | `object_instance_create_by_template_name_VA_00B2E760` | resolves name via `0x00B28700` and invokes the returned factory interface | Strong | identify returned interface RTTI and ownership |
| `0x00ADBC80` | `baseline_message_parse_VA_00ADBC80` | parses ID, tag, view and variable baseline payload | Confirmed | prove size/count bounds and malformed handling |
| `0x00554CF0` | `baseline_view_dispatch_VA_00554CF0` | routes baseline payload to per-object view handlers for view values 1–9 plus dynamic fallback | Confirmed | map tag/class-specific view field schemas |
| `0x00ADB2D0` | `delta_message_parse_VA_00ADB2D0` | parses the same ID/tag/view/payload wrapper used by baselines for `DeltasMessage` | Confirmed | prove payload size/count bounds |
| `0x00554E90` | `delta_view_dispatch_VA_00554E90` | routes views 1–9 to the same object view members as baseline dispatch, invokes delta apply and post-update hook | Confirmed | map AutoDelta container operations and notification effects |
| `0x0118A650` | `autodelta_byte_stream_ctor_VA_0118A650` | constructs RTTI `Archive::AutoDeltaByteStream`, vtable `0x015FE388`, including registered field-decoder container | Confirmed | enumerate class-specific registration call sites |
| `0x0118B740` | `autodelta_byte_stream_snapshot_decode_VA_0118B740` | consumes U16 declared count, then invokes every locally registered field snapshot decoder; declared count is not the loop bound | Confirmed | characterize failure propagation/trailing data |
| `0x0118AA00` | `autodelta_byte_stream_delta_decode_VA_0118AA00` | reads but ignores declared U16 update count, consumes indexed fields to bounded payload exhaustion, and dereferences `registry[index]` without a size check; earlier field mutations are nontransactional | Confirmed unsafe behavior and fatal outer policy | replacement uses validation plus transactional commit |
| `0x006F24D0` | `dynamic_replication_view_wrapper_ctor_VA_006F24D0` | stores dynamic view/package key and constructs embedded `AutoDeltaByteStream` at `+0x08` | Confirmed | identify factory and concrete view-8 packages |
| `0x0043B050` | `autodelta_vector_int_snapshot_decode_VA_0043B050` | clears vector, reads U32 count and counter, then count U32 elements | Confirmed | characterize archive failure propagation and count limits |
| `0x0043B1A0` | `autodelta_vector_int_delta_decode_VA_0043B1A0` | reconciles update counter; consumes stale prefix; applies command bytes 0–4 with index/value validation | Confirmed | test U32 counter wrap and malformed command behavior |
| `0x0043D820` | `autodelta_skill_set_snapshot_decode_VA_0043D820` | clears set; reads count/counter and skill keys | Confirmed mechanics | map skill-key resolver and invalid-name behavior |
| `0x0043D9A0` | `autodelta_skill_set_delta_decode_VA_0043D9A0` | stale-prefix reconciliation; commands 0 remove, 1 add, 2 clear | Confirmed | characterize duplicate/missing-key callbacks |
| `0x00540E30` | `autodelta_int_float_map_snapshot_decode_VA_00540E30` | clears map; reads count/counter and command/key/value entries | Confirmed | determine invalid baseline command behavior |
| `0x00540F60` | `autodelta_int_float_map_delta_decode_VA_00540F60` | counter reconciliation; map commands ADD 0, DROP 1, SET 2 | Confirmed | test duplicate and U32 counter-wrap cases |
| `0x0043A3B0` | `creature_visual_posture_decode_VA_0043A3B0` | reads one-byte posture, runs post-read hook, conditionally invokes typed callback | Confirmed | trace posture callback subscribers |
| `0x00440160` | `creature_performance_type_decode_VA_00440160` | reads U32 performance type and conditionally invokes typed callback | Confirmed | trace animation/UI consumers |
| `0x00664940` | `player_experience_value_decode_VA_00664940` | reads U32 value and conditionally dispatches `ExperienceChanged` | Confirmed | resolve callback target `0x0065E0B0` and UI consumers |
| `0x006652D0` | `player_spoken_language_decode_VA_006652D0` | reads U32 language and conditionally dispatches `SpokenLanguageChanged` | Confirmed | resolve callback target `0x0065ACC0` |
| `0x00665580` | `player_completed_quests_bitarray_decode_VA_00665580` | decodes/copies `BitArray`, compares old/new, conditionally notifies | Confirmed | recover bit indexing and subscriber effects |
| `0x00665820` | `player_active_quests_bitarray_decode_VA_00665820` | decodes/copies `BitArray`, compares old/new, conditionally notifies | Confirmed | recover active-quest subscriber effects |
| `0x00641430` | `tangible_max_hit_points_decode_VA_00641430` | reads U32; on change invokes owner virtual slot `+0xB0` with old/new | Confirmed | name owner virtual method and downstream state |
| `0x00643360` | `tangible_condition_flags_decode_VA_00643360` | reads U32; on change invokes owner virtual slot `+0xAC` with old/new | Confirmed | map condition bits and appearance/gameplay effects |
| `0x00441740` | `creature_wearable_vector_snapshot_decode_VA_00441740` | clears 0x20-stride runtime entries, reads vector baseline count/counter and wearable values; concrete callback slots are null | Confirmed | finish remap/customization use of cached metadata |
| `0x00441920` | `creature_wearable_vector_delta_decode_VA_00441920` | reconciles counters and applies generic vector command/index framing to `WearableEntry` values; does not directly attach appearances | Confirmed | finish remap/customization use of cached metadata |
| `0x0044CE30` | `wearable_vector_remove_callback_VA_0044CE30` | generic optional per-element removal callback dispatcher; CreatureObject's slot is null | Confirmed mechanics and concrete non-use | none for current field |
| `0x0044CE60` | `wearable_vector_insert_callback_VA_0044CE60` | generic optional per-element insertion callback dispatcher; CreatureObject's slot is null | Confirmed mechanics and concrete non-use | none for current field |
| `0x0044CE90` | `wearable_vector_replace_callback_VA_0044CE90` | invokes installed per-element replacement callback with index/old/new values | Confirmed mechanics | name installed callback target |
| `0x00440D50` | `wearable_vector_clear_apply_VA_00440D50` | invokes removal callback for every entry, clears storage, marks field dirty and invokes aggregate callback | Confirmed | identify all presentation side effects |
| `0x004412A0` | `wearable_vector_replace_all_apply_VA_004412A0` | replaces complete vector, records per-index replacements and marks field dirty | Confirmed | identify all presentation side effects |
| `0x00669F40` | `player_waypoint_map_snapshot_decode_VA_00669F40` | clears map, reads U32 count/counter and specialized `NetworkId`/`Waypoint` entries | Confirmed structure | name key/value codec helpers and notification boundary |
| `0x0066A1D0` | `player_waypoint_map_delta_decode_VA_0066A1D0` | reconciles map counter then parses specialized operations | Confirmed structure | verify client command meanings 0/1/2 and conversion to client waypoint objects |
| `0x00554240` | `client_object_replication_ctor_VA_00554240` | creates the six embedded view registries and registers common fields | Confirmed | finish non-replication member naming |
| `0x00554CF0` | `baseline_view_dispatch_VA_00554CF0` | selects the registered view and runs snapshot decode | Confirmed | characterize permitted trailing payload bytes by tag/view |
| `0x00554E90` | `delta_view_dispatch_VA_00554E90` | selects the registered view, applies indexed deltas, then commits callbacks only on normal return; an exception skips commit | Confirmed | replacement must not preserve partial mutation |
| `0x0118A840` | `autodelta_byte_stream_register_field_VA_0118A840` | assigns a U16 ordinal and appends a field object to the view registry | Confirmed | none |
| `0x0118AA00` | `autodelta_byte_stream_delta_decode_VA_0118AA00` | consumes U16 index/value pairs until payload end | Confirmed | document legacy exception path for invalid index |
| `0x0118A970` | `autodelta_byte_stream_commit_VA_0118A970` | invokes queued changed-field callbacks and clears pending state | Confirmed | determine callback ordering guarantees |
| `0x0118A7C0` | `autodelta_dirty_field_enqueue_VA_0118A7C0` | inserts field identity into an ordered set, suppressing duplicate callbacks within one delta | Confirmed | identify optional observer at stream `+0x1C` |
| `0x0065C3B0` | `player_replication_callback_install_VA_0065C3B0` | installs local-player waypoint-map add/drop/set callbacks; view 9 acts only for keys absent from view 8 | Confirmed | identify historical view-9 producer/use case |
| `0x00668920` | `player_waypoints_changed_publish_VA_00668920` | publishes `PlayerObject::Messages::WaypointsChanged` with mutation-safe receiver bookkeeping | Confirmed | determine coalescing behavior across a full delta |
| `0x006E58A0` | `cell_object_ctor_VA_006E58A0` | derives from ClientObject, registers the view-3 cell-number field and creates portal property | Confirmed | name portal-property builder helpers |
| `0x00709FE0` | `static_object_ctor_VA_00709FE0` | derives from ClientObject with no additional replicated fields | Confirmed | identify auxiliary property installed after construction |
| `0x0070DBB0` | `building_object_ctor_VA_0070DBB0` | derives from TangibleObject and adds no building-specific replicated fields | Confirmed | name secondary vtable/interface at object `+0x288` |
| `0x00542DA0` | `client_common_view6_u32_snapshot_decode_VA_00542DA0` | reads the common view-6 U32 and runs its post-read hook | Confirmed mechanics | identify field semantics |
| `0x00542DC0` | `client_common_view6_u32_delta_decode_VA_00542DC0` | reads the common view-6 U32 and queues callback work | Confirmed mechanics | identify field semantics |
| `0x01223E10` | `matchmaking_id_u32_vector_decode_VA_01223E10` | reads U32 count followed by that many U32 values for PLAY:3 bitmask/profile fields | Confirmed | recover historical `MatchMakingId` semantics and impose replacement cap |
| `0x0065E0B0` | `player_experience_changed_transceiver_emit_VA_0065E0B0` | walks registered receivers and deferred receiver sets, invoking their typed callbacks | Confirmed behavior | recover dispatch ordering/re-entrancy guarantees |
| `0x0065ACC0` | `player_spoken_language_changed_handler_VA_0065ACC0` | handles local-player language change and constructs downstream localized/UI state | Strong | finish function and identify emitted message/UI target |
| `0x00423780` | `game_loop_VA_00423780` | initializes loop state and repeatedly calls frame tick until quit flag | Confirmed | identify initialization call `0x00422E80` |
| `0x004237C0` | `frame_tick_VA_004237C0` | computes delta, pumps events, updates ordered subsystems, renders/presents, services resources and accounts frame | Confirmed structure | name each phase and recover timing policy |
| `0x011B9F70` | `wearable_appearance_map_install_VA_011B9F70` | installs map, calls loader `0x011B9B80`, registers removal with ExitChain | Confirmed | none for installation boundary |
| `0x011B9B80` | `wearable_appearance_map_load_VA_011B9B80` | loads the three named columns, converts empty mapped values to null, stores entries and sorts them | Confirmed | recover any case-folding inside CrcString construction |
| `0x011B9FF0` | `wearable_appearance_map_lookup_VA_011B9FF0` | binary-searches `(source appearance, wearer appearance)` and returns not-found, suppress, or mapped-object result states | Confirmed | none for principal behavior |
| `0x00437450` | `creature_underwear_wearable_refresh_VA_00437450` | manages default underwear/fallback wearable objects, `/shared_owner` customization mounts, and skeletal attach/detach | Confirmed | compare customization/remap ordering with containment-driven equipment path |
| `0x00436D90` | `creature_container_object_added_VA_00436D90` | CreatureObject virtual wrapper for child/container insertion; delegates to wear path and refreshes fallback underwear | Confirmed | label special mount/rider branch |
| `0x00437050` | `creature_container_object_removed_VA_00437050` | CreatureObject virtual wrapper for child/container removal; delegates to unwear path and refreshes fallback underwear | Confirmed | label special local-player collection branch |
| `0x0067A060` | `creature_wear_contained_object_VA_0067A060` | validates container/child appearance state, prepares relationship and calls skeletal wear | Confirmed | resolve `0x00679CC0/0x00679E20` remap/customization branches |
| `0x0067A140` | `creature_unwear_contained_object_VA_0067A140` | stops wearing valid child appearance, cleans relationship state and refreshes owner | Confirmed | resolve `0x00679F70` customization cleanup |
| `0x00555020` | `client_object_apply_containment_VA_00555020` | applies parsed parent NetworkId and arrangement through client object/container machinery | Strong principal role | name subordinate reference bookkeeping calls |
| `0x00661E20` / `0x00662030` | `player_ability_vector_snapshot/delta_decode` | length-prefixed ability-name vector with five indexed operations and counter reconciliation | Confirmed | map name resolution to command/ability definitions |
| `0x006633A0` / `0x00663530` | `player_draft_schematic_vector_snapshot/delta_decode` | U32-pair draft-schematic vector with five indexed operations | Confirmed | confirm pair member labels beyond server comments |
| `0x00665C40` / `0x00666430` | `player_quest_packed_map_snapshot/delta_decode` | U32 quest CRC to fixed PlayerQuestData record | Confirmed | name packed-map callback consumers |
| `0x01233560` | `player_quest_data_decode_VA_01233560` | decodes U64 owner, U16 active mask, U16 completed mask, U8 completion, U32 counter | Confirmed executable/server match | none |
| `0x0118B030` | `byte_stream_read_checked_VA_0118B030` | copies a requested fixed byte count when within the bounded stream; otherwise throws Archive exception | Confirmed | replacement parser must prevalidate and reject transactionally |
| `0x012B3B80` / `0x012B3D42` | `network_received_message_dispatch_loop_VA_012B3B80` / `network_archive_read_error_catch_rethrow_VA_012B3D42` | services queued inbound records; catch logs `Unhandled Archive read error: %s` and rethrows Archive exception | Confirmed | none for legacy policy; replacement disconnect policy remains a design choice |
| `0x00462BC0` | `game_network_dispatch_update_VA_00462BC0` | invokes network service/received-message loop and per-connection updates during a frame; no catch | Confirmed | name remaining maintenance calls |
| `0x00536670` | `ship_object_replication_ctor_VA_00536670` | RTTI ShipObject constructor; registration order confirms composed SHIP view sizes 3=22, 1=38, 6=23, 4=5; all four primary SHIP views are field-closed | Confirmed | retain as anchor; pursue transform/control consumers rather than constructor semantics |
| `0x00539B70` | `ship_component_attachment_refresh_VA_00539B70` | refreshes a changed component slot's chassis attachment/appearance; validates chassis and attachment CRC | Confirmed principal behavior | name attachment manager calls |
| `0x006189A0` | `update_ship_target_slot_payload_send_VA_006189A0` | constructs integer controller payload type `0x3FA` and submits with flags `0x83` | Confirmed | decode public flag labels |
| `0x00696480` | `ship_set_and_send_target_slot_VA_00696480` | for the current local ship, sends 0x3FA when a controller exists, then applies the target slot to the replicated field | Confirmed | characterize prediction/echo suppression |
| `0x01197250` | `shared_controller_payload_registry_install_VA_01197250` | registers 50 numeric controller IDs with typed outbound/inbound factories; includes `0x3FA -> int` | Confirmed | attach historical public names only where source or behavior proves them |
| `0x0067AFC0` | `group_object_replication_ctor_VA_0067AFC0` | RTTI GroupObject constructor; inherited field plus seven registrations match GRUP:6 | Confirmed | none for principal schema |
| `0x0067E500` / `0x0067E750` | `group_members_snapshot/delta_decode` | U32 count/counter and NetworkId+extended-length ASCII elements; emits aggregate and per-member events | Confirmed | impose replacement count/string caps |
| `0x0067FA90` / `0x0067FCA0` | `group_member_ships_snapshot/delta_decode` | U32 count/counter and NetworkId+U32 elements; emits aggregate and per-assignment events | Confirmed | verify the U32 is always list index in deployed traffic |
| `0x007C9A50` | `skeletal_wear_object_VA_007C9A50` | validates wearable skeletal appearance, retains a watcher/reference to the worn object and marks affected slots dirty | Confirmed | characterize later dirty-slot rebuild consumer |
| `0x007C9C30` | `skeletal_stop_wearing_object_VA_007C9C30` | finds/releases the worn-object reference and marks affected slots dirty | Confirmed | characterize missing-entry diagnostics |
| `0x007CB440` | `skeletal_mark_wearable_slots_dirty_VA_007CB440` | marks the affected skeletal slot interval dirty; it is not the complete appearance rebuild | Confirmed | locate the later dirty-slot consumer |
| `0x007C9680` / `0x007C97B0` | `skeletal_attach_alternate_wearable_record_VA_007C9680` / `skeletal_detach_alternate_wearable_record_VA_007C97B0` | add/remove a skeletal-appearance attachment record keyed by worn-object identity and rebuild affected appearance state | Confirmed lifecycle; exact internal record type provisional | identify public SDK type if useful |
| `0x0070B5F0` | `guild_object_replication_ctor_VA_0070B5F0` | constructs GuildObject and registers the single added GILD:3 string-set field | Confirmed | none for replication boundary |
| `0x0070C520` / `0x0070C720` | `guild_list_snapshot_decode_VA_0070C520` / `guild_list_delta_decode_VA_0070C720` | decode count/counter plus `guildID:guildAbbrev` strings; delta commands 0/1/2 remove/add/clear | Confirmed | none |
| `0x0070B8C0` | `guild_list_rebuild_lookup_VA_0070B8C0` | clears and rebuilds numeric guild-ID and abbreviation lookup structures | Confirmed | public container labels only |
| `0x00660A20` / `0x00660A30` | `player_view9_reserved_u32_14_get_VA_00660A20` / `player_view9_reserved_u32_15_get_VA_00660A30` | return PLAY:9 indices 14/15; exhaustive direct-call scan finds no callers and current server emits zero | Confirmed dormant/reserved behavior in this build | original historical names only; retain two U32 wire slots |
| `0x004630B0` | `game_network_parameters_store_VA_004630B0` | stores ParametersMessage U32 at GameNetwork `+0x60` | Confirmed | discover member readers/unit |
| `0x00463750` | `game_network_parameters_get_VA_00463750` | returns GameNetwork `+0x60`; no static callers exist in this executable | Confirmed dormant access path | historical unit only |
| `0x00A19580` | `customer_service_connect_test_response_handler_VA_00A19580` | parses ConnectPlayerResponseMessage, derives boolean and publishes UI/customer-service event | Confirmed | enumerate nonzero status meanings |
| `0x00FA0100` | `character_sheet_response_listener_VA_00FA0100` | dispatches character-sheet/faction/guild/badge/money responses; PlayerMoneyResponse branch parses then discards without consuming values | Confirmed for money branch | name remaining branches only when needed |
| `0x00BB4770` | `avatar_creation_response_listener_VA_00BB4770` | dispatches create success/failure, random-name response, and pending dialog events | Confirmed | outbound full character-create request remains separate |
| `0x00BB4D10` | `avatar_creation_request_build_send_VA_00BB4D10` | collects live customization/name/template/profession/biography/tutorial state, builds ClientCreateCharacter and sends reliably | Confirmed principal behavior | name every UI-state getter only as needed |
| `0x00AE4E30` | `client_create_character_message_ctor_VA_00AE4E30` | constructs the eleven-field outbound creation message in server parser order | Confirmed | none for wire boundary |
| `0x00BB6380` | `avatar_creation_result_apply_VA_00BB6380` | on success saves/associates new avatar ID and refreshes state; always closes pending dialogs and publishes result | Confirmed principal behavior | name subordinate persistence APIs only if needed |
| `0x00BB6880` | `avatar_random_name_response_apply_VA_00BB6880` | validates returned template and approval token, then applies or rejects generated Unicode name | Confirmed principal behavior | fully type the retained request state |
| `0x0046D7A0` | `client_command_queue_enqueue_VA_0046D7A0` | central command lookup/local queue path; network branch emits controller 0x116 | Confirmed principal behavior | name all local failure/result branches later |
| `0x0062CB50` | `command_queue_enqueue_send_VA_0062CB50` | constructs typed command payload and queues 0x116 with priority `0x23` | Confirmed | none for wire boundary |
| `0x00AE3B60` / `0x00AE3E80` | `command_queue_enqueue_payload_ctor_VA_00AE3B60` / `command_queue_enqueue_payload_serialize_VA_00AE3E80` | action U32, CRC U32, target U64, Unicode arguments | Confirmed | none |
| `0x0062CBD0` | `command_queue_cancel_send_VA_0062CBD0` | constructs 0x117 with action count and three zero fields, queues at priority `0x23` | Confirmed | none |
| `0x0046E720` | `client_command_queue_remove_apply_VA_0046E720` | publishes Removing, removes matching local command, and formats nonzero `resultCategory` through the seven-entry `cmd_err` table with category-specific `resultDetail` | Confirmed | fields formerly called tab1/tab2 are result category/detail |
| `0x0053CB70` | `ship_target_acquisition_apply_VA_0053CB70` | updates current target, sends 0x126, resets component slot, updates controller/acquisition state and publishes optional TargetAcquisition | Confirmed principal behavior | label acquisition timer fields only if needed |
| `0x00618930` | `target_update_send_VA_00618930` | constructs NetworkId payload and queues controller 0x126 with flags `0x83` | Confirmed | none |
| `0x00709D00` | `weapon_object_ctor_VA_00709D00` | derives from TangibleObject and adds no replicated fields in this build | Confirmed | identify five local DWORD members only if behavior requires them |
| `0x0070A230` | `resource_container_object_ctor_VA_0070A230` | registers quantity/resource ID in view 3 and max/type/name in view 6 | Confirmed | none for view order |
| `0x0070A570` | `resource_container_refresh_texture_VA_0070A570` | derives a texture from resource type/name and applies it to appearance tag `REP0` when dirty | Confirmed principal behavior | name lookup helper `0x0069D210` and fallback texture |
| `0x0070AD70` | `installation_object_ctor_VA_0070AD70` | registers active byte, surplus-power float and base-power-rate float in view 3 | Confirmed | trace derived installation view-7 classes separately |
| `0x0041CFD0` | `sound2d_template_load_VA_0041CFD0` | opens SD2D, dispatches versions 0000-0003, then applies category/mode and positive-radius normalization | Confirmed | trace scalar accessors into Miles playback consumers |
| `0x0041DB30` | `sound2d_template_load_0003_VA_0041DB30` | reads a 32-bit sample count, retained bounded sample strings and 28 ordered 32-bit scalar fields | Confirmed | assign semantic field names only from playback consumers |
| `0x0041DE60` | `sound2d_template_write_0003_VA_0041DE60` | canonical SD2D/0003 writer; independently confirms the v3 parser order | Confirmed | none for structural schema |
| `0x0041C540` | `sound3d_template_load_VA_0041C540` | loads common SD2D payload, dispatches SD3D 0000/0001 tail, forces runtime mode 2 | Confirmed | establish exact meaning of legacy v0 two-dword tail |
| `0x0082F940` | `track_animation_controller_alter_VA_0082F940` | forwards elapsed time to the distinguished physical track | Confirmed | prove outer appearance-update ordering |
| `0x0082F960` | `track_animation_controller_evaluate_transform_VA_0082F960` | resolves a logical channel and evaluates/copies rotation and translation | Confirmed | label channel-index source |
| `0x0083E630` | `physical_animation_track_alter_VA_0083E630` | advances active animation, accumulates root motion, completes transitions and processes queued changes | Confirmed | label transition modes and remaining-time outputs |
| `0x0083E9B0` | `physical_animation_track_change_animation_VA_0083E9B0` | applies or queues a reference-counted animation change and returns a stable sequence handle | Confirmed | correlate ASHT action parameters |
| `0x0041EA40` | `keyboard_device_translate_event_VA_0041EA40` | translates buffered scan-code records through the 256-byte table into down/up events 7/8 | Confirmed | label special callbacks and enum names |
| `0x0041EC90` | `mouse_device_translate_event_VA_0041EC90` | emits scaled relative axes and mouse button transitions | Confirmed | trace event consumption |
| `0x0041F700` | `joystick_device_translate_event_VA_0041F700` | normalizes axes/POV/buttons/sliders to engine event records | Confirmed | finish slider and extended-axis tail |
| `0x00AADA30` | `input_map_load_VA_00AADA30` | rejects/skips IMAP/0005 and dispatches effective IMAP/0006 | Confirmed | none for version envelope |
| `0x00AADB50` | `input_map_load_0006_VA_00AADB50` | loads CMDS, SHFT and per-shift-state MAP forms | Confirmed | decode command record fields |
| `0x00AB5670` | `normalized_input_event_enqueue_VA_00AB5670` | appends a five-dword event to the input FIFO and coalesces near-identical analog events | Confirmed | label all event enums |
| `0x00AB5310` | `iowin_dispatch_queued_events_VA_00AB5310` | detaches FIFO and sends each event down the IoWin stack until consumed/removed | Confirmed | label return bits and scene-specific stack composition |
| `0x0085CA90` | `procedural_terrain_template_registry_install_VA_0085CA90` | registers terrain template tags with factory `0x0085CAF0` and ExitChain removal | Confirmed | identify second registered tag semantics |
| `0x00860BD0` | `client_terrain_worker_create_VA_00860BD0` | allocates/configures a thread-like object associated with client terrain | Strong | trace start, queue input and completion handoff |
| `0x00ADC6D0` | `containment_message_parse_VA_00ADC6D0` | parses child ID, container ID and 32-bit arrangement/type | Confirmed | label archive scalar wrappers |
| `0x00AD58D0` | `scene_end_baselines_parse_VA_00AD58D0` | parses the single LE64 object ID for server class `SceneObjectCloseMessage`, wire identity `SceneEndBaselines` (`0x2C436037`) | Confirmed | none |
| `0x0051F9B0` | `scene_end_baselines_finalize_VA_0051F9B0` | commits per-object readiness/attachment after the initial baseline stream; this is explicitly not destruction | Confirmed lifecycle role and call path; individual predicates/virtual names Strong | classify individual readiness predicates and virtual methods when object-class vtables are named |
| `0x00AD55D0` | `scene_destroy_object_parse_VA_00AD55D0` | parses LE64 object ID and hyperspace byte for `SceneDestroyObject` | Confirmed | name scalar accessors |
| `0x0051BDFD` region | `scene_destroy_object_handler_VA_0051BDFD` | resolves object; selects specialized hyperspace disappearance or guarded detach/removal/destruction path | Confirmed principal flow | resolve virtual slots and local-player exclusions |
| `0x00ADAC80` | `update_transform_parse_VA_00ADAC80` | parses object ID, compressed X/Z/Y, movement counter, speed and direction | Confirmed | label exact parser member offsets |
| `0x0051C83B` region | `update_transform_dispatch_VA_0051C83B` | resolves object/controller and submits transform command type `0x71` | Confirmed | trace interpolation consumer |
| `0x00ADA510` | `update_transform_with_parent_parse_VA_00ADA510` | parses parent ID, object ID, compressed X/Z/Y, movement counter, speed and direction | Confirmed | label exact parser member offsets |
| `0x0051C9C4` region | `update_transform_with_parent_dispatch_VA_0051C9C4` | resolves object/controller and submits parent-relative command type `0xF1` | Confirmed | trace cell/ship interpolation consumer |
| `0x00AD4260` | `message_queue_data_transform_with_parent_construct_VA_00AD4260` | constructs the registered movement payload from counter, parent NetworkId, transform and speed | Confirmed | RTTI/vtable `0x015EAFB4` and pack/unpack call site |
| `0x00AD4360` | `message_queue_data_transform_with_parent_pack_VA_00AD4360` | writes nested counter, parent, full transform and speed; outer scalar supplies C→S timestamp | Confirmed | direct executable serialization and server callback cursor |
| `0x00AD43B0` | `message_queue_data_transform_with_parent_unpack_VA_00AD43B0` | reads S→C nested counter, parent, quaternion/translation and speed; replaces NaN quaternion with identity | Confirmed | direct executable decoder and factory registration |
| `0x0051CBC8` / `0x00AD9B70` | `client_open_container_dispatch_and_parse` | parses container NetworkId plus trailing byte/string payload, resolves object, forwards to container/UI call `0x008D9CA0` | Confirmed | label `0x008D9CA0` arguments and close-mode behavior |
| `0x0051CFED` / `0x00AD7830` | `create_projectile_dispatch_and_parse` | parses packed space projectile launch and creates local projectile state | Confirmed | map projectile manager ownership/lifetime |
| `0x0051D2AD` / `0x00AD72D0` | `create_missile_dispatch_and_parse` | parses source/target missile trajectory and constructs tracked missile state | Confirmed | map listener callbacks and impact timing |
| `0x0051D65C` / `0x00AD6EF0` | `update_missile_dispatch_and_parse` | parses projectile/ship/result fields and switches among four terminal outcomes | Confirmed | name four effect-manager calls |
| `0x0051D783` / `0x00AD6930` | `ship_transform_dispatch_and_parse` | resolves short ship ID and invokes controller with packed movement message | Confirmed | decode exact packed scales from client parser |
| `0x0051DAC8` / `0x00AD6190` | `ship_collision_correction_dispatch_and_parse` | accepts correction only for local player's root ship and forwards to `0x0061B120` | Confirmed | trace smoothing/collision reconciliation |
| `0x0051C287` / `0x00ADC3A0` | `update_cell_permission_dispatch_and_parse` | resolves cell ID and applies parsed allow-entry flag through `0x006E6850` | Confirmed | identify cell collision/portal invalidation effects |
| `0x0051C41E` / `0x00ADC0F0` | `server_time_dispatch_and_parse` | parses U64 galactic time, computes local offset, updates time service | Confirmed | determine precision/clamp behavior |
| `0x0051CB49` / `0x00AD9F40` | `play_music_dispatch_and_parse` | parses path/target/mode/flag and selects audio calls `0x006C9300` or `0x006C93B0` | Confirmed | label flag and target semantics |
| `0x0051DFF0` / `0x00AD5EE0` | `hyperspace_dispatch_and_parse` | gated named-message route, resolves addressed object, invokes removal/transition path | Confirmed behavior; phase labels Strong | enumerate operand-specific hyperspace phases |
| `0x0051E071` / `0x00520C20` | `flattened_theater_dispatch_and_parse` | resolves theater ID and applies boolean flatten request | Confirmed | trace terrain-modification lifetime |
| `0x0051E142` / `0x00520E80` | `launch_browser_dispatch_and_parse` | parses server-provided ASCII URL and forwards to platform launcher `0x00AA13D0` | Confirmed | identify exact scheme normalization and error handling |
| `0x0051CD19` / `0x00AD9270` | `debug_transform_dispatch_and_parse` | decodes compact transform to 3x4 matrix plus target NetworkId, writes matrix to current controller and updates retained target reference | Confirmed | recover compact transform wire codec details |
| `0x0051DBD6` inline | `dogfight_taunt_ui_dispatch` | consumes current server's 12-byte payload and constructs a localized client/UI notification | Strong | label the LE64 and LE32 fields from executable consumers |
| `0x005E2A20` | `trade_message_listener_VA_005E2A20` | literal dispatcher for 12 secure-trade messages; typed parsers feed client trade state/event functions | Confirmed | find remove-item outbound path |
| `0x005E17F0` | `trade_send_add_item_VA_005E17F0` | constructs `AddItemMessage` with U64 item ID and submits reliable | Confirmed | resolve pre-send duplicate/item-list guard fully |
| `0x005E1B20` | `trade_send_give_money_VA_005E1B20` | constructs U32 `GiveMoneyMessage`, sends reliable, updates local offer notification | Confirmed | characterize local clamp/UI validation |
| `0x005E1E00` | `trade_send_accept_VA_005E1E00` | sets local accepted flag, sends empty `AcceptTransactionMessage`, publishes acceptance event | Confirmed | none for wire behavior |
| `0x005E20F0` | `trade_send_unaccept_VA_005E20F0` | clears local accepted flag, sends empty `UnAcceptTransactionMessage`; if both accepted, first invokes deny/reset path | Confirmed | explain legacy both-accepted edge case |
| `0x005E23F0` | `trade_send_verify_VA_005E23F0` | constructs and reliably sends empty `VerifyTradeMessage` | Confirmed | identify exact UI trigger |
| `0x005E2450` | `trade_send_deny_and_reset_VA_005E2450` | clears both accept flags, constructs/reliably sends `DenyTradeMessage`, publishes false acceptance | Confirmed | distinguish request-denial UI cases |
| `0x005E2740` | `trade_send_abort_VA_005E2740` | constructs/reliably sends `AbortTradeMessage` and publishes abort state | Confirmed | correlate server callback implementation |
| `0x005E4120` | `trade_set_peer_acceptance_VA_005E4120` | stores inbound peer acceptance at `0x019161F0` and publishes typed `Accept` | Confirmed | none |
| `0x005E4500` | `trade_request_verify_event_VA_005E4500` | publishes `RequestVerify(true)` in response to `BeginVerificationMessage` | Confirmed | find current server emission path |
| `0x005E4790` | `trade_inbound_verify_noop_VA_005E4790` | single `ret`; named inbound Verify branch is intentionally inert in this build | Confirmed | none |
| `0x005E4A40` | `trade_complete_event_VA_005E4A40` | clears active flag and publishes `Complete(true)` | Confirmed | none |
| `0x005E4CE0` | `trade_abort_event_VA_005E4CE0` | clears active flag and publishes `AbortTrade(true)` | Confirmed | none |
| `0x00AC9660` | `obj_controller_message_parse_VA_00AC9660` | decodes priority U32, type S32, NetworkId U64, float scalar, then invokes the inner factory; absent type logs, yields null payload, records per-type telemetry, and is safely ignored when no controller can consume it | Confirmed | replacement should reject the message explicitly rather than queue nullable payload |
| `0x00B0D190` | `obj_controller_inner_factory_VA_00B0D190` | looks up inner type in a 139-entry factory registry and invokes its inbound constructor | Confirmed | assign names/schemas to active factories |
| `0x00462F60` | `obj_controller_dispatch_VA_00462F60` | resolves addressed object/controller and queues type, typed payload, scalar and normalized priority | Confirmed | name profiling calls and priority bits |
| `0x00B0CFC0` | `obj_controller_factory_register_VA_00B0CFC0` | registers type with outbound factory, inbound factory and locally-originatable capability annotation in three maps | Confirmed maps; capability label Strong | original public name only |
| `0x00B0D200` | `obj_controller_local_origin_capability_lookup_VA_00B0D200` | returns the per-type boolean stored by the fourth registration argument, zero if absent; no direct-call or absolute-pointer references exist | Confirmed dormant accessor; capability label Strong | original public name only |
| `0x00B0EDA6` | `quest_task_timer_register_as_0x441_VA_00B0EDA6` | registers factories `0x00B0F020`/`0x00B0F0D0`; RTTI/vtable trace identifies their product as `MessageQueueQuestTaskTimerMessage` | Confirmed | compare live packet behavior if quest journal is exercised |
| `0x00B0F3B6` | `quest_task_counter_register_as_0x440_VA_00B0F3B6` | registers factories `0x00B0F630`/`0x00B0F6F0`; RTTI/vtable trace identifies their product as `MessageQueueQuestTaskCounterMessage` | Confirmed | compare with server's mismatched `0x441` counter |
| `0x0062CAC0` | `secure_trade_queue_dispatch_VA_0062CAC0` | dispatches `MessageQueueSecureTrade` operation 1 to Requested(first ID), 2 to no-op, and 3/4/5 to denied/busy/unreachable(second ID); operation 0 is server-directed | Confirmed | verify forwarding order in integration test |
| `0x005E13F0` | `trade_request_target_VA_005E13F0` | validates/updates local pending target state and queues type `0x115`, operation 0, invalid first ID, selected target second ID | Confirmed principal request path | label every duplicate/pending-request UI branch |
| `0x00AFA390` | `append_comment_response_parse_VA_00AFA390` | inbound archive constructor for executable `AppendCommentResponseMessage`; registers two U32 fields at object `+0x38` and `+0x40` | Confirmed | label result enumeration |
| `0x005CBC10` | `append_comment_response_apply_VA_005CBC10` | receives zero-result-as-success plus ticket ID from listener and updates customer-service response state | Strong effect; argument path Confirmed | identify public transceiver name |
| `0x00AF8060` | `knowledge_base_search_response_parse_VA_00AF8060` | inbound archive constructor for `SearchKnowledgeBaseResponseMessage`; decodes status/count and variable 0x1C-byte article records | Confirmed parser family | map auxiliary U16 semantics |
| `0x00A1B9A0` | `knowledge_base_search_result_shape_VA_00A1B9A0` | converts parsed response fields into client result object before publication | Strong | label output type |
| `0x00A1B040` | `knowledge_base_search_result_publish_VA_00A1B040` | publishes shaped knowledge-base search results through retained manager/transceiver | Strong | identify subscribers |
| `0x00AD3000` | `update_pvp_status_parse_VA_00AD3000` | inbound `UpdatePvpStatusMessage` archive constructor; registers U32 status, U32 faction and NetworkId in that order | Confirmed | label archive helper types |
| `0x0051CF51` | `scene_update_pvp_status_branch_VA_0051CF51` | identifies the message, parses it, resolves object ID and stores status/faction at object `+0x3A0/+0x3A4` | Confirmed | identify affected observer notifications, if any |
| `0x013F63A0` | `player_money_response_parse_VA_013F63A0` | inbound archive constructor for executable `PlayerMoneyResponse`; registers two U32 fields matching server bank/cash order | Confirmed | locate indirect factory registration and final consumer |
| `0x013F3F60` | `resource_harvester_activate_page_parse_VA_013F3F60` | inbound archive constructor for `ResourceHarvesterActivatePageMessage`; registers one NetworkId field | Confirmed | none for schema |
| `0x00E40EDF` | `resource_harvester_activate_page_listener_VA_00E40EDF` | identifies message, parses installation ID, converts/resolves it and calls UI boundary `0x008CC460` | Confirmed route; UI method label Strong | identify concrete mediator/page class |

| VA/region | Provisional name | Current evidence | Confidence | Next action |
|---|---|---|---|---|
| `0x0131DC7A` | `crt_entry_VA_0131DC7A` | PE entry, SEH/CRT setup, calls wrapper | Confirmed | none |
| `0x00401810` | `winmain_wrapper_VA_00401810` | four args forwarded to `0x00401050`, `ret 0x10` | Strong | resolve preliminary checks/imports |
| `0x00401050` | `app_entry_VA_00401050` | config/platform install chain, run, cleanup, returns 0 | Strong | xref strings and label all direct callees |
| `0x00401000` | `startup_misc_override_load_VA_00401000` | opens `misc/override.cfg` through TreeFile, submits its bytes to override/config ingestion, then releases buffer and file | Confirmed broad role | inner override grammar belongs to shared config parser |
| `0x00401950` | `setup_client_animation_install_VA_00401950` | debug scope `SetupClientAnimation::install`; invokes the ordered animation/playback registration chain | Confirmed | individual sub-installers tracked separately |
| `0x004019D0` | `playback_script_manager_exit_registration_VA_004019D0` | scope `PlaybackScriptManager::install`; registers `0x004020A0` to clear the retained playback-script object vector at shutdown | Confirmed | closed |
| `0x00426300` | `game_feature_bits_get_VA_00426300` | returns mask set by `0x00426320`, whose diagnostic is `GameFeatureBits: %s`; bootstrap maps bits into subsystem feature flags | Confirmed | exact textual bit names remain data-level labels |
| `0x0075A220` | `client_graphics_application_install_VA_0075A220` | installs platform/client graphics components and registers ordered semantic render phases | Confirmed broad behavior | label subordinate install calls and phase abbreviations |
| `0x007548A0` | `graphics_install_VA_007548A0` | validates raster major, loads `.\\s2%02d_r.dll`, resolves `GetApi`, validates and installs returned graphics API table | Confirmed | recover complete API-table slot contract from `s205_r.dll` |
| `0x00421490` | `setup_client_direct_input_install_VA_00421490` | debug scope `SetupClientDirectInput::install`; reads `ClientDirectInput` mouse/joystick/Windows-key/translation/force-feedback options and installs platform input callbacks | Confirmed | closed |
| `0x00AA21E0` | `exit_chain_register_candidate_VA_00AA21E0` | repeated callback/name registration shape | Strong concept | match known `ExitChain` strings/exports |

## High-priority unknowns

This early queue is now fully reconciled. Later focused checkpoints closed the
GameNetwork/application dispatch and factories; SOE transform algorithms;
login, cluster, character and scene bootstrap; object creation/destruction and
replication; main-loop/frame ordering; raster DLL and D3D9 lifecycle; TRE/IFF
factories; and network/loader/terrain-worker thread ownership.

No current item in this historical list remains a Class-C architecture or
interoperability blocker. Remaining work is long-tail optional UI/gameplay
breadth and specialized presentation fidelity. See
`ANALYSIS_CHECKPOINT_2026-09-22_DOCUMENTATION_UNKNOWN_RECONCILIATION.md` and
`FUNCTION_UNCERTAINTY_AUDIT.md`.

## Suggested tooling checkpoint

## Core controller-effect names added 2026-09-11

| VA | Provisional name | Evidence | Confidence / remaining work |
|---:|---|---|---|
| `0x0062AA10` | `player_creature_controller_message_dispatch_VA_0062AA10` | player/UI typed switch, delegates unhandled cases | Confirmed; long tail remains |
| `0x006392E0` | `creature_controller_message_dispatch_VA_006392E0` | animation, posture, seating, flourish, buffs, then delegation | Confirmed |
| `0x006E5210` | `object_controller_common_dispatch_VA_006E5210` | combat, weapon data, crafting responses and fly text | Confirmed broad role |
| `0x006E4950` | `combat_action_apply_VA_006E4950` | resolves participants, rejects more than one defender, calls presentation | Confirmed |
| `0x007D78F0` | `animation_message_apply_VA_007D78F0` | fans named animation across appearance targets | Confirmed broad role |
| `0x0091BA10` | `spatial_chat_apply_VA_0091BA10` | filtering, language/text shaping, animation and chat publication | Confirmed broad role; flags incomplete |
| `0x00469410` | `posture_transition_prepare_VA_00469410` | updates dependent posture state before primary setter | Strong |
| `0x004328D0` | `creature_posture_apply_VA_004328D0` | validates transitions and stores/notifies posture | Confirmed core role |
| `0x008C94D0` | `combat_spam_format_and_publish_VA_008C94D0` | resolves participants, grammar/fallback and formatted output | Confirmed |
| `0x0063EA20` | `seat_target_resolve_VA_0063EA20` | spatial candidate-seat/parent resolution | Strong; ordering incomplete |
| `0x005DCA90` | `flourish_apply_VA_005DCA90` | finds matching performer and forwards flourish ID | Strong |

A future session should install or locate a reputable local decompiler only with user approval if installation is required. Import the exact hash at image base `0x00400000`, enable MSVC RTTI analysis, and export a symbols/address map into this directory. Decompiler output remains pseudocode and must be validated against disassembly and server source.
# Ghidra replication names added 2026-09-11

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x00554240` | `client_object_replication_ctor_VA_00554240` | Constructs the six embedded view registries and registers common fields. Confirmed. |
| `0x00555420` | `client_view1_register_field_VA_00555420` | Forwards to view object at `+0xA4`, vslot `+0x10`. Confirmed. |
| `0x005554A0` | `client_view3_register_field_VA_005554A0` | Forwards to `+0x124`. Confirmed. |
| `0x00555440` | `client_view4_register_field_VA_00555440` | Forwards to `+0xC4`. Confirmed. |
| `0x005554C0` | `client_view6_register_field_VA_005554C0` | Forwards to `+0x144`. Confirmed. |
| `0x00555460` | `client_view8_register_field_VA_00555460` | Forwards to `+0xE4`. Confirmed. |
| `0x00555480` | `client_view9_register_field_VA_00555480` | Forwards to `+0x104`. Confirmed. |
| `0x0118A840` | `autodelta_byte_stream_register_field_VA_0118A840` | Assigns U16 ordinal/back-pointer and appends field. Confirmed by vtable and body. |
| `0x0118B740` | `autodelta_byte_stream_snapshot_decode_VA_0118B740` | Reads declared count but decodes all registered fields. Confirmed. |
| `0x0118AA00` | `autodelta_byte_stream_delta_decode_VA_0118AA00` | Reads declared count but consumes indexed updates to payload exhaustion. Confirmed; malformed-index behavior remains to test. |
| `0x0118A970` | `autodelta_byte_stream_commit_VA_0118A970` | Invokes and clears deferred changed-field callbacks. Confirmed. |
| `0x0042ECF0` | `creature_object_replication_ctor_VA_0042ECF0` | Constructs CreatureObject fields and registers them by view. Confirmed role; individual field names incomplete. |
| `0x0063EE10` | `tangible_object_replication_ctor_VA_0063EE10` | TangibleObject replication constructor. Confirmed. |
| `0x0065B800` | `player_object_replication_ctor_VA_0065B800` | PlayerObject replication constructor. Confirmed. |
| `0x006E4800` | `intangible_object_replication_ctor_VA_006E4800` | IntangibleObject replication constructor. Confirmed. |
| `0x0065C3B0` | `player_replication_callback_install_VA_0065C3B0` | Installs local-player container callbacks, including both waypoint maps. Confirmed. |
| `0x00668920` | `player_waypoints_changed_publish_VA_00668920` | Publishes `PlayerObject::Messages::WaypointsChanged` with safe connect/disconnect bookkeeping. Confirmed. |
| `0x00701500` | `manufacture_schematic_object_ctor_VA_00701500` | Registers four MSCO:3 and five MSCO:6 additions after IntangibleObject. Confirmed. |
| `0x00701EA0` | `manufacture_schematic_crc_changed_VA_00701EA0` | Resolves draft-schematic template/appearance and publishes change; warnings identify invalid paths. Confirmed. |
| `0x00702180` | `manufacture_schematic_appearance_data_changed_VA_00702180` | Validates SharedTangibleObjectTemplate and builds custom appearance. Confirmed. |
| `0x006C34B0` | `client_mission_object_ctor_VA_006C34B0` | Registers the twelve mission fields composing exact MISO:3 indices 5-16. Confirmed. |
| `0x006C3B00` | `client_mission_entered_bag_publish_VA_006C3B00` | Publishes `ClientMissionObject::Messages::EnteredMissionBag` and refreshes mission presentation. Confirmed. |
| `0x006C3D90` | `client_mission_waypoint_changed_VA_006C3D90` | Resolves/creates and associates ClientWaypointObject presentation. Confirmed broad behavior. |
| `0x006C3F30` | `client_mission_type_changed_VA_006C3F30` | Maps mission type CRC to presentation selection and refreshes the object. Confirmed broad behavior. |
| `0x005252B0` | `client_waypoint_object_ctor_VA_005252B0` | Registers seven typed view-3 fields after ITNO and installs four presentation callbacks. Confirmed. |
| `0x00526E20` | `client_waypoint_location_changed_VA_00526E20` | Publishes `ClientWaypointObject::Messages::LocationChanged` and performs local refresh bookkeeping. Confirmed. |
| `0x005270D0` | `client_waypoint_color_changed_VA_005270D0` | Publishes `ClientWaypointObject::Messages::ColorChanged` and performs local refresh bookkeeping. Confirmed. |

Tooling note: Ghidra 12.1.3 missed valid functions at `0x0043F960`, `0x0118A840`, `0x0118AA00`, and `0x0118A970`; each was independently anchored by an unusual MSVC prologue or a concrete vtable entry and manually created in the database. Automated function totals must remain planning bounds, not ground truth.

# VFS/TRE names added 2026-09-11

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x00401000` | `startup_misc_override_load_VA_00401000` | Opens normalized `misc/override.cfg`, passes its buffer/size to the config override loader, frees buffer and file. Confirmed broad role. |
| `0x00A92800` | `tree_file_install_search_nodes_VA_00A92800` | Enumerates SharedFile searchPath/searchTree/searchTOC priorities, then adds absolute/cache nodes. Confirmed. |
| `0x00A92B00` | `tree_search_node_priority_compare_VA_00A92B00` | Compares node priority at `+4`; establishes descending order with insertion routine. Confirmed. |
| `0x00A92B20` | `tree_search_node_insert_sorted_VA_00A92B20` | Locked sorted insertion into global search-node vector. Confirmed. |
| `0x00A930D0` | `tree_normalize_logical_path_VA_00A930D0` | Strips leading separators/dot segments, canonicalizes separators, folds characters. Folding table/locale detail remains open. |
| `0x00A99550` | `search_tree_find_record_VA_00A99550` | CRC-plus-string binary search of 24-byte TREE records; zero-size entry signals mask/tombstone. Confirmed. |
| `0x00A99780` | `search_tree_open_record_VA_00A99780` | Returns archive slice for stored payload or eagerly decompressed memory file. Confirmed. |
| `0x00A99CF0` | `search_toc_find_record_VA_00A99CF0` | CRC-plus-string binary search with nonzero offset/size validity checks. Confirmed. |
| `0x00A99EC0` | `search_toc_open_record_VA_00A99EC0` | TOC stored/eagerly-decompressed open path. Confirmed. |

# Filename identity and appearance factory names added 2026-09-11

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x00AA4790` | `swg_filename_crc32_VA_00AA4790` | Exact non-reflected CRC-32; 2,285 installed TREE records verified with zero failures. Confirmed. |
| `0x0131D6E8` | `ascii_or_locale_lower_VA_0131D6E8` | ASCII A-Z lowercasing in default mode; locale-aware alternate path. Confirmed. |
| `0x00B25E80` | `appearance_factory_register_tag_VA_00B25E80` | Inserts/replaces outer FORM tag-to-callback mapping. Confirmed. |
| `0x00B26140` | `appearance_factory_create_from_iff_VA_00B26140` | Peeks outer tag, resolves callback, and invokes it. Confirmed. |
| `0x00B263C0` | `appearance_template_fetch_VA_00B263C0` | APT indirection, cache lookup, TreeFile/IFF open, and registry dispatch. Confirmed. |
| `0x00B2CB60` | `appearance_template_load_appr_VA_00B2CB60` | APPR base loader with versions 0001-0003. Confirmed. |
| `0x00796250` | `mesh_appearance_registry_install_VA_00796250` | Registers MESH factory and mesh settings. Confirmed. |
| `0x007962D0` | `mesh_appearance_factory_VA_007962D0` | Allocates MeshAppearanceTemplate and invokes constructor/loader. Confirmed. |

# Mesh boundary names added 2026-09-11

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x00796340` | `mesh_appearance_template_ctor_load_VA_00796340` | Initializes MeshAppearanceTemplate and optionally invokes loader. Confirmed. |
| `0x007968B0` | `mesh_appearance_load_VA_007968B0` | MESH version dispatcher 0002-0005 and runtime bounds/culling derivation. Confirmed. |
| `0x00796680` | `mesh_legacy_center_radius_load_VA_00796680` | Reads CNTR vector and RADI float. Confirmed. |
| `0x00770C30` | `shader_primitive_set_ctor_load_VA_00770C30` | Constructs primitive vector and invokes SPS loader. Confirmed broad role. |
| `0x00770C80` | `shader_primitive_set_require_form_VA_00770C80` | Requires outer `SPS ` form. Confirmed. |
| `0x00770CE0` | `shader_primitive_set_version_dispatch_VA_00770CE0` | Dispatches SPS versions 0000/0001. Confirmed. |
| `0x00B2CD40` | `appearance_hardpoints_load_VA_00B2CD40` | Optional HPTS physical child count; repeated HPNT 12-F32 transform plus remaining NUL-name bytes. No serialized count/name prefix. Sorts unsigned CRC then exact bytes. Confirmed; 2026-09-30 correction. |

# SPS/VTXA names added 2026-09-11

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x00770D60` | `shader_primitive_set_load_v0000_VA_00770D60` | CNT, shader groups, INFO primitive counts, legacy primitive records. Confirmed. |
| `0x00770F70` | `shader_primitive_set_load_v0001_VA_00770F70` | Same group envelope with versioned primitive records. Confirmed. |
| `0x0075B020` | `shader_template_fetch_or_inline_load_VA_0075B020` | NAME fetch or inline shader-template construction/cache. Confirmed. |
| `0x0076FA20` | `static_shader_primitive_load_legacy_VA_0076FA20` | Legacy topology/VTXA and conditional U32-on-disk to U16 INDX. Confirmed. |
| `0x00770220` | `static_shader_primitive_load_v0000_VA_00770220` | Topology/flags, VTXA, optional INDX via U32-entry reader 007700A0 with low-16 truncation; optional SIDX. Confirmed. |
| `0x00784750` | `vertex_array_version_dispatch_VA_00784750` | VTXA versions 0001-0003. Confirmed. |
| `0x007847D0` | `vertex_array_load_v0001_VA_007847D0` | Legacy declaration expansion and interleaved DATA decode. Confirmed broad layout. |
| `0x00784AA0` | `vertex_array_load_v0002_VA_00784AA0` | Packed declaration/count and DATA decode. Confirmed. |
| `0x00784D90` | `vertex_array_load_v0003_VA_00784D90` | V0002 family plus additional packed channel. Coordinate count bits 8–11 and two-bit dimensions from bit 12 now drive verified native 1105/55405/D205/D5405/5205/35305 slices. Final 4D tangent semantics remain inferred. Confirmed structure. |
| `0x007700A0` | `index_buffer_load_u32_legacy_to_u16_VA_007700A0` | U32 count/U32 entries truncated to U16 runtime buffer. Confirmed. |
| `0x00770160` | `index_buffer_load_u16_VA_00770160` | U32 count followed by U16 entries. Confirmed. |

2026-09-29 reconciliation: primitive/0000 calls 007700A0 at `0x007702FD`
(INDX) and `0x007703B5` (SIDX); primitive/0001 loader entry `0x007704E0`
calls 00770160 at `0x007705BD` and `0x00770675` respectively. SPS/0001
alone does not select U16 file entries. No names were repurposed or registry
rows added. See the [geometry checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATIC_MESH_SLICE.md).

# Topology and shader/effect names added 2026-09-12

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x0076F890` | `static_primitive_topology_table_install_VA_0076F890` | Installs twelve consecutive draw wrappers for topology IDs 0-11. Confirmed by raw disassembly and consumers. |
| `0x007707D0` | `static_shader_primitive_prepare_VA_007707D0` | Binds vertex/default index data or chooses SIDX by minimum local camera-direction dot product, retaining the first entry on strict-less ties. Confirmed. |
| `0x007709F0` | `static_shader_primitive_accumulate_metrics_VA_007709F0` | Accumulates vertex, primitive and fill metrics using topology-wrapper identity. Confirmed; supplied output field labels provisional. |
| `0x0075AA50` | `shader_template_fetch_cached_VA_0075AA50` | Cache lookup, TreeFile open, defaultshader fallback, parse. Confirmed. |
| `0x0075B240` | `shader_template_create_by_tag_VA_0075B240` | Dispatches outer shader-template tag through registered factory. Confirmed. |
| `0x0075C380` | `shader_effect_fetch_cached_VA_0075C380` | Cache lookup, TreeFile open, defaulteffect fallback, parse. Confirmed. |
| `0x0075C560` | `shader_effect_fetch_or_inline_load_VA_0075C560` | Accepts EFCT inline or NAME reference; qualifies relative effect names. Confirmed. |
| `0x007833E0` | `shader_effect_ctor_load_VA_007833E0` | Creates retained effect identity and dispatches EFCT loader. Confirmed. |
| `0x00783500` | `shader_effect_version_dispatch_VA_00783500` | EFCT versions 0000 and 0001. Confirmed. |
| `0x00783580` | `shader_effect_load_v0000_VA_00783580` | Reads U8 implementation count and selects first supported implementation. Confirmed. |
| `0x00783630` | `shader_effect_load_v0001_VA_00783630` | V0000 plus DATA byte 1, the `containsPrecalculatedVertexLighting` boolean, stored at effect `+0x18`. Confirmed. |
| `0x00783700` | `shader_effect_selected_implementation_get_VA_00783700` | Returns the first capability-supported implementation selected while loading EFCT. Confirmed. |
| `0x0077FFF0` | `static_shader_template_contains_precalculated_vertex_lighting_VA_0077FFF0` | `ShaderTemplate` virtual slot 7 for `StaticShaderTemplate`; returns the selected EFCT object's `+0x18` boolean. Confirmed. |
| `0x0076A4C0` | `static_shader_contains_precalculated_vertex_lighting_VA_0076A4C0` | Exported `StaticShader::containsPrecalculatedVertexLighting()`; dispatches template virtual slot 7. Confirmed. |
| `0x00782E50` | `shader_implementation_fetch_or_inline_load_VA_00782E50` | Accepts IMPL or NAME, caches named candidates, rejects unsupported implementation. Confirmed. |
| `0x0075CBA0` | `shader_implementation_ctor_load_VA_0075CBA0` | Initializes implementation state and dispatches IMPL versions. Confirmed. |
| `0x0075CD00` | `shader_implementation_version_dispatch_VA_0075CD00` | IMPL versions 0000-0007. Confirmed. |
| `0x0075E700` | `shader_pass_ctor_load_VA_0075E700` | Initializes pass defaults and dispatches PASS form. Confirmed. |
| `0x0075E840` | `shader_pass_version_dispatch_VA_0075E840` | PASS versions 0000-0009. Confirmed; field semantics pending. |

# PASS layout names added 2026-09-12

| Address | Stable provisional name | Evidence / remaining uncertainty |
|---:|---|---|
| `0x0075E940` | `shader_pass_load_v0000_VA_0075E940` | Oldest PASS subset. Confirmed version role; individual states pending. |
| `0x0075EDE0` | `shader_pass_load_v0001_VA_0075EDE0` | Additive PASS loader. Confirmed version role. |
| `0x0075F290` | `shader_pass_load_v0002_VA_0075F290` | Additive PASS loader. Confirmed version role. |
| `0x0075F770` | `shader_pass_load_v0003_VA_0075F770` | Additive PASS loader. Confirmed version role. |
| `0x0075FC50` | `shader_pass_load_v0004_VA_0075FC50` | Additive PASS loader. Confirmed version role. |
| `0x007601E0` | `shader_pass_load_v0005_VA_007601E0` | Additive PASS loader. Confirmed version role. |
| `0x007607D0` | `shader_pass_load_v0006_VA_007607D0` | Additive PASS loader. Confirmed version role. |
| `0x00760DC0` | `shader_pass_load_v0007_VA_00760DC0` | Additive PASS loader. Confirmed version role. |
| `0x007613C0` | `shader_pass_load_v0008_VA_007613C0` | Dominant installed compatibility version. Confirmed. |
| `0x007619D0` | `shader_pass_load_v0009_VA_007619D0` | Current executable-supported superset; exact serialized widths and child branches recovered. Confirmed. |
| `0x00762220` | `fixed_function_pipeline_version_dispatch_VA_00762220` | PFFP versions 0000/0001. Confirmed. |
| `0x00762360` | `fixed_function_vertex_lighting_load_v0000_VA_00762360` | three enable bytes, legacy U32 material tag, four U8 material-source selectors; sole effective record uses tag `MAIN` | Confirmed shape/tag; enable/source labels Strong |
| `0x00762440` | `fixed_function_vertex_lighting_load_v0001_VA_00762440` | three enable bytes and four U8 material-source selectors; 704 effective records, 11 distinct values | Confirmed shape/corpus; exact source-channel ordering Strong |
| `0x00762BA0` | `texture_stage_ctor_load_VA_00762BA0` | Establishes fixed-function color/alpha defaults and dispatches version loader. Confirmed broad role. |
| `0x00763160` | `pixel_shader_program_ref_ctor_load_VA_00763160` | Constructs PPSH-side program/constant reference state. Confirmed broad role. |

# Raster PASS compiler names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00755E00` | `graphics_select_shader_implementation_VA_00755E00` | Graphics API slot `+0xA0`; configured backend stores selected implementation. Confirmed. |
| `0x00755E20` | `graphics_apply_shader_pass_VA_00755E20` | Graphics API slot `+0xA4`; backend selects and applies compiled pass. Confirmed. |
| `0x62A046A0` | `d3d9_select_shader_implementation_VA_62A046A0` | Stores active implementation pointer. Confirmed. |
| `0x62A046B0` | `d3d9_apply_shader_pass_entry_VA_62A046B0` | Routes pass index to compiled state and updates derived state. Confirmed. |
| `0x62A0F3C0` | `d3d9_compile_static_shader_implementation_VA_62A0F3C0` | Sizes 0xA4 pass vector and compiles every engine PASS for one StaticShader. Confirmed. |
| `0x62A0E9D0` | `d3d9_compile_static_shader_pass_VA_62A0E9D0` | Resolves material/factors/scroll/alpha/stencil/texture data into one backend pass. Confirmed. |
| `0x62A0F4B0` | `d3d9_select_and_apply_compiled_pass_VA_62A0F4B0` | Caches implementation/pass pair, then applies implementation and pass records. Confirmed. |
| `0x62A0B9B0` | `d3d9_apply_implementation_render_states_VA_62A0B9B0` | Applies cached D3DRS/value vector and pixel shader. Confirmed. |
| `0x62A0EF30` | `d3d9_apply_compiled_pass_state_VA_62A0EF30` | Applies shader constants, alpha/stencil/fog data, textures and derived flags. Confirmed. |
| `0x62A0E910` | `d3d9_apply_texture_and_sampler_state_VA_62A0E910` | Binds texture and applies cached D3DSAMP/value pairs. Confirmed. |
| `0x00762C20` | `texture_stage_version_dispatch_VA_00762C20` | Texture-stage versions 0000/0001. Confirmed. |
| `0x00762CA0` | `texture_stage_load_v0000_VA_00762CA0` | STAG/0000: 18 U8 fields, texture/coordinate LE U32 tags at offsets 18/22, then seven U8 fields (33 bytes). Confirmed widths and corpus tag offsets; individual labels pending. |
| `0x00762F10` | `texture_stage_load_v0001_VA_00762F10` | Reduced/current stage payload. Confirmed widths; individual labels pending. |
| `0x62A0B340` | `d3d9_compile_pass_render_state_vector_VA_62A0B340` | Converts PASS offsets into cached D3DRS/value pairs; core state map confirmed. |
| `0x62A0E570` | `d3d9_compile_texture_sampler_state_VA_62A0E570` | Resolves texture data and compiles anisotropy/address/filter D3DSAMP pairs. Confirmed. |
| `0x62A0E0E0` | `d3d9_bind_texture_cached_VA_62A0E0E0` | Cached IDirect3DDevice9 texture binding. Confirmed. |
| `0x62A15070` | `d3d9_get_texture_coordinate_vertex_shader_variant_VA_62A15070` | Caches generated vertex shader by packed texture-coordinate-set mapping. Confirmed. |
| `0x62A14740` | `d3d9_compile_texture_coordinate_vertex_shader_VA_62A14740` | Builds texture-coordinate macros/declarations, compiles vs_1_1/vs_2_0 source through D3DX, and creates IDirect3DVertexShader9. Confirmed. |

`PASS 0010`'s additional early scalar remains deliberately unnamed. The corpus establishes its width and structural region but provides neither a client loader nor a same-name version pair. Fixed-function STAG color and alpha operation translation also remains open.

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x62A0B230` | `d3d9_apply_texture_stage_state_vector_VA_62A0B230` | Calls IDirect3DDevice9::SetTextureStageState for cached pairs. Confirmed behavior; no static code/data reference in this s207 build. |
| `0x62A0C2C0` | `d3d9_initialize_render_and_texture_stage_defaults_VA_62A0C2C0` | Initializes per-stage D3DTSS and sampler cache/device defaults. Confirmed. |
| `0x62A04D40` | `d3d9_create_programmable_implementation_graphics_data_VA_62A04D40` | Sole caller constructs the 0x62A0BAA0 implementation compiler. Confirmed. |

The exact STAG operation/argument translations remain provisional because the applicator is unreferenced and no active compiler was recovered. The 33-byte field grouping is durable; the trailing byte at runtime offset `+0x5C` remains unknown.

# StaticShader texture binding names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00780140` | `static_shader_template_load_v0000_VA_00780140` | Parses MATS, TXMS, TCSS and later parameter maps. Confirmed. |
| `0x00780A60` | `static_shader_template_load_v0001_VA_00780A60` | Same material/texture/coordinate map families for SSHT 0001. Confirmed. |
| `0x007813E0` | `shader_texture_map_version_dispatch_VA_007813E0` | Dispatches TXM versions 0000/0001/0002. Confirmed. |
| `0x00781470` | `shader_texture_map_load_v0000_VA_00781470` | Loads flag and tag, supplies sampler defaults and optional NAME texture. Confirmed. |
| `0x00781720` | `shader_texture_map_load_v0001_VA_00781720` | Loads tag, flag and six address/filter bytes. Confirmed. |
| `0x00781A20` | `shader_texture_map_load_v0002_VA_00781A20` | Adds maximum-anisotropy byte. Confirmed. |
| `0x00764F90` | `shader_texture_optional_name_load_VA_00764F90` | Reads NAME and returns central texture-cache result. Confirmed. |
| `0x00764F20` | `texture_cache_fetch_by_name_VA_00764F20` | Normalizes through TemporaryCrcString and fetches texture. Confirmed broad role. |
| `0x0076AEF0` | `static_shader_get_texture_data_VA_0076AEF0` | Tag lookup returning the 36-byte TextureData value. Confirmed. |
| `0x0076AF80` | `static_shader_get_texture_coordinate_set_VA_0076AF80` | Tag lookup returning one U8 coordinate-set index. Confirmed. |
| `0x62A12340` | `d3d9_dynamic_texture_slot_lookup_VA_62A12340` | Returns registry value slot for a dynamic texture tag. Confirmed. |
| `0x62A12380` | `d3d9_is_global_dynamic_texture_tag_VA_62A12380` | Accepts ENVM or any tag whose first character is underscore. Confirmed. |
| `0x62A123B0` | `d3d9_set_global_dynamic_texture_VA_62A123B0` | Inserts/replaces registry texture with fetch/release ownership. Confirmed. |

The sampler ordering is closed: STAG/TXM filter bytes are mip/min/mag because they emit D3DSAMP 7/6/5. The trailing byte at `+0x5C` is closed for the effective corpus as a coordinate-generation/profile selector: 0 ordinary, 3 reflection/environment, and 4/5 the first/second scrolling profiles. Values 1/2 are absent and retained as unsupported legacy values. Exact dormant combiner labels remain naming/breadth rather than an original-asset blocker. See `ANALYSIS_CHECKPOINT_2026-09-21_STAG_COORDINATE_GENERATION_ENUM.md`.

# DDS and Texture resource names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00764930` | `texture_list_install_VA_00764930` | Initializes fallback texture and CRC-keyed cache. Confirmed. |
| `0x00764CC0` | `texture_list_fetch_policy_wrapper_VA_00764CC0` | Applies fallback/debug policy before cache fetch. Confirmed broad role. |
| `0x00764DA0` | `texture_list_fetch_by_crc_VA_00764DA0` | Mutex-protected lookup/create and returned-reference fetch. Confirmed. |
| `0x00768BB0` | `texture_construct_from_crc_name_VA_00768BB0` | Initializes 0x30-byte Texture and calls DDS load. Confirmed. |
| `0x00769170` | `texture_load_dds_from_treefile_VA_00769170` | Parses DDS header, format/cube/mips, creates graphics resource and uploads levels. Confirmed. |
| `0x00768FE0` | `texture_upload_dds_faces_and_mips_VA_00768FE0` | Calculates level geometry/row pitch and copies or skips DDS payload data. Confirmed. |
| `0x00755F60` | `graphics_create_texture_data_VA_00755F60` | Graphics API slot +0x114 creating backend texture data. Confirmed. |
| `0x62A12510` | `d3d9_texture_data_construct_VA_62A12510` | Chooses supported format and creates managed 2D/cube D3D texture. Confirmed. |
| `0x62A127F0` | `d3d9_texture_lock_VA_62A127F0` | Locks selected mip/face or conversion surface. Confirmed. |
| `0x62A12A70` | `d3d9_texture_unlock_VA_62A12A70` | Unlocks and, when needed, converts staging surface into destination. Confirmed. |
| `0x00768FA0` / `0x00768FB0` | `texture_fetch_ref_VA_00768FA0` / `texture_release_ref_VA_00768FB0` | Plain non-atomic refcount; zero removes/destroys. Confirmed. |
| `0x00779A50` | `client_graphics_config_install_VA_00779A50` | Reads discardHighestMipMapLevels and discardHighestNormalMipMapLevels. Confirmed. |

Resolved 2026-09-22: all 14 client `TextureFormat` values, embedded names,
s207 D3DFORMAT mappings, capability checks and ordered fallback are mapped.
Only the historical source spelling of invalid BTRT sentinel 16 is unavailable.
See `ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md`.

# Procedural texture and render-target names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00765000` | `texture_create_solid_color_VA_00765000` | Allocates Texture, creates backend data and writes one clamped packed color. Confirmed broad behavior. |
| `0x00765070` | `texture_create_from_memory_VA_00765070` | Allocates Texture and copies supplied pixel rows according to format width/backend pitch. Confirmed. |
| `0x007650E0` | `texture_create_procedural_VA_007650E0` | Allocates empty procedural/renderable texture used by post-processing, UI/radar and environment systems. Confirmed broad behavior. |
| `0x62A0AD10` | `d3d9_render_target_copy_helper_create_VA_62A0AD10` | Saves original RT/depth and creates 512x512 GPU/system-memory scratch textures. Confirmed. |
| `0x62A0AE30` | `d3d9_render_target_copy_helper_release_VA_62A0AE30` | Releases saved/transient surfaces and both scratch textures. Confirmed. |
| `0x62A0AED0` | `d3d9_restore_saved_render_target_VA_62A0AED0` | Rebinds saved render target and depth-stencil surface. Confirmed. |
| `0x62A0AF40` | `d3d9_select_texture_render_target_VA_62A0AF40` | Selects texture face/mip surface, sizes scratch level and binds RT0. Confirmed broad behavior; public enum labels pending. |
| `0x62A0B120` | `d3d9_finish_render_target_copy_VA_62A0B120` | Readback/copy through system-memory scratch, releases surfaces and restores target. Confirmed broad behavior. |
| `0x62A03520` | `d3d9_pre_reset_release_resources_VA_62A03520` | Runs release callbacks/resource-family teardown before reset. Confirmed broad role. |
| `0x62A03560` | `d3d9_post_reset_recreate_resources_VA_62A03560` | Recreates resource families and runs callbacks after reset. Confirmed broad role. |

Closed in `ANALYSIS_CHECKPOINT_2026-09-20_PROCEDURAL_TEXTURE_CONTRACT.md`: exact seven-argument procedural texture ABI, width/height/mip fields, ordered candidate-format array/count, render-target/default-pool policy, 2D/cube selection, and all ten direct call families. Still open: exact public cube-face selector enum values and complete render-to-texture pass scheduling.

# Frame schedule names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00755700` / `0x62A03D70` | `graphics_begin_frame_state_VA_00755700` / `d3d9_advance_frame_constants_VA_62A03D70` | Advances backend time/count and uploads frame constant. Confirmed broad behavior. |
| `0x00755730` / `0x62A03DC0` | `graphics_begin_scene_VA_00755730` / `d3d9_begin_scene_VA_62A03DC0` | Calls IDirect3DDevice9::BeginScene and resets frame caches. Confirmed. |
| `0x00755740` / `0x62A03E30` | `graphics_end_scene_VA_00755740` / `d3d9_end_scene_VA_62A03E30` | Calls IDirect3DDevice9::EndScene. Confirmed. |
| `0x00755800` / `0x62A04010` | `graphics_present_default_VA_00755800` / `d3d9_present_default_VA_62A04010` | Default present wrapper. Confirmed. |
| `0x00755810` / `0x62A04040` | `graphics_present_to_destination_VA_00755810` / `d3d9_present_to_destination_VA_62A04040` | Explicit destination present wrapper. Confirmed broad behavior. |
| `0x62A03E60` | `d3d9_present_and_recover_device_VA_62A03E60` | Presents, tests cooperative level, resets and reconstructs resources. Confirmed. |
| `0x00AB54F0` | `iowin_manager_render_top_VA_00AB54F0` | Invokes virtual slot +8 on stack head. Confirmed. |
| `0x00AB5500` | `iowin_manager_push_VA_00AB5500` | Pushes window onto linked stack and routes initial event. Confirmed. |
| `0x0051B770` | `ground_scene_render_VA_0051B770` | GroundScene IoWin render virtual; camera/clear/world path. Confirmed broad behavior. |
| `0x00773B10` | `shader_primitive_sorter_begin_environment_VA_00773B10` | Pushes camera/environment state and initializes all phases. Confirmed. |
| `0x00773DE0` | `shader_primitive_sorter_end_environment_VA_00773DE0` | Closes phases and restores prior state. Confirmed. |

Closed in `ANALYSIS_CHECKPOINT_2026-09-20_RENDER_PHASE_PROFILES.md`: all twelve phase indices, numeric sort profiles, numeric execution modes, profile callback selection, comparator-key widths, and final drain order. Still open: original enum names and exact semantic identity of each comparator key; exact nested world/post-process/CUI ownership order remains separate.

Follow-up closure in `ANALYSIS_CHECKPOINT_2026-09-20_RENDER_SUBMISSION_RECORD.md`: phase inference/override routing, dual mask groups, exact 0x28-byte sortable-record field sources, comparator fields, and draw-loop sequence. Still open: RTTI names for the resolved shader/material-like object and exact historical labels for its virtual keys/methods.

# GroundScene, bloom, and CUI names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0093B2B0` | `cui_iowin_render_VA_0093B2B0` | Brackets next/lower IoWin with bloom, then GroundScene post pass and CUI. Strongly inferred class; confirmed behavior. |
| `0x00AB58E0` | `iowin_render_next_lower_VA_00AB58E0` | Calls render virtual +8 on `this+8`. Confirmed. |
| `0x0064B500` | `bloom_begin_scene_capture_VA_0064B500` | Selects bloom scene target and resets viewport. Confirmed. |
| `0x0064B560` | `bloom_resolve_and_composite_VA_0064B560` | Downsample, horizontal/vertical blur, and final composite. Confirmed. |
| `0x0064B3D0` | `bloom_build_gaussian_constants_VA_0064B3D0` | Computes seventeen weight/offset constants. Confirmed. |
| `0x0051BCE0` | `ground_scene_post_bloom_render_VA_0051BCE0` | GroundScene-only stage after bloom and before CUI; component identities partial. Confirmed position. |
| `0x00881210` | `cui_render_root_and_overlays_VA_00881210` | Full-screen UI viewport/state, manager/root-page drawing and cleanup. Confirmed broad role. |
| `0x00B5B710` | `terrain_object_get_instance_VA_00B5B710` | Returns singleton corroborated by TerrainObject RTTI/remove path. Confirmed. |
| `0x00B5CB50` | `terrain_object_get_environment_color_VA_00B5CB50` | Calls appearance virtual +0x128; concrete terrain method returns environment/clear color. Corrected and Confirmed. |
| `0x00561060` / `0x005610F0` | `procedural_terrain_camera_callbacks_begin/end_VA_...` | Paired callback registration/removal gated by ClientProceduralTerrainAppearance RTTI. Confirmed broad role. |

Closed in `ANALYSIS_CHECKPOINT_2026-09-20_GROUND_POST_BLOOM_COMPONENTS.md`: `0x0051BCE0` gets the active GameCamera and renders its screen effects, then conditionally renders OverheadMap (`GroundScene+0xC4`) and SpaceTargetBracketOverlay (`+0xD4`) in that order. `ANALYSIS_CHECKPOINT_2026-09-20_CUI_TIMED_PREVIEW_OVERLAY.md` further closes CuiIoWin's final rectangle as a two-second centered preference preview for reticle dead-zone size and HUD opacity. `ANALYSIS_CHECKPOINT_2026-09-20_COCKPIT_STAGE_RENDER_DRIVER.md` resolves `0x00573D90` to the shared `CockpitStage` / `CockpitLeaveStage` render slot. Still open: concrete terrain appearance sorter submissions.

# Procedural terrain submission names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00849350` | `client_procedural_terrain_construct_VA_00849350` | Writes confirmed RTTI-resolved vtable and creates worker/flora/water/LOD structures. Confirmed. |
| `0x0084B770` | `client_procedural_terrain_get_environment_color_VA_0084B770` | Returns terrain color or default; corrects prior draw interpretation. Confirmed. |
| `0x0084AAA0` | `client_procedural_terrain_visibility_render_VA_0084AAA0` | Visibility-tree traversal and selected renderable virtual dispatch. Confirmed broad role. |
| `0x0084E510` | `client_terrain_sorter_install_VA_0084E510` | Creates five buckets and temporary-node pools. Confirmed. |
| `0x0084EDB0` | `client_terrain_sorter_batch_primitive_VA_0084EDB0` | Groups primitives under capacity/count limits. Confirmed broad role. |
| `0x0084F110` | `client_terrain_sorter_submit_VA_0084F110` | Walks collected primitives and submits each to ShaderPrimitiveSorter. Confirmed. |
| `0x0084EBA0` | `client_terrain_sorter_finish_and_recycle_VA_0084EBA0` | Transfers/releases primitive references and clears accumulation state. Confirmed broad role. |
| `0x00561180` | `render_active_cameras_with_terrain_callbacks_VA_00561180` | Iterates active cameras and brackets camera virtuals with callback install/remove. Confirmed. |

Reconciled 2026-09-21: this open list is obsolete. The sorter categories, every concrete `0x0084CAF0` recipient, global/local water generation, flora runtime and chunk worker/main-thread commit lifecycle are closed to replacement-architecture depth. See `ANALYSIS_CHECKPOINT_2026-09-21_PROCEDURAL_TERRAIN_OPEN_ITEM_RECONCILIATION.md` for the evidence map. Only historical labels and bounded fidelity details remain.

# Water rendering names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x008569D0` | `client_global_water_manager_construct_VA_008569D0` | Confirmed RTTI/vtable and static/dynamic selection. |
| `0x00856B60` | `client_global_water_register_watr_phase_VA_00856B60` | Literal WATR lookup and sorter registration. Confirmed. |
| `0x00854940` | `global_water_static_prepare_VA_00854940` | Camera-side constants, shader and buffers. Confirmed broad role. |
| `0x00854B50` | `global_water_static_draw_VA_00854B50` | Up to four indexed quadrant draws. Confirmed. |
| `0x00854D60` | `global_water_dynamic_prepare_VA_00854D60` | Camera-relative generated geometry and bindings. Confirmed broad role. |
| `0x008632F0` | `client_local_water_manager_construct_VA_008632F0` | Confirmed RTTI/vtable. |
| `0x00863AD0` | `client_local_water_cull_and_register_watr_VA_00863AD0` | Per-primitive frustum test and literal WATR registration. Confirmed. |
| `0x00863210` | `local_water_primitive_prepare_VA_00863210` | Shader/vertex/index resource binding. Confirmed. |
| `0x00863240` | `local_water_primitive_draw_VA_00863240` | Paired vtable draw entry. Strongly inferred. |

Closed to replacement depth 2026-09-21: local polygon and polyline producers, winding correction, static buffers, fallback/capable vertex variants, exact `0x100/0x101` numeric branch, PTAT BPOL/BREC fields, ordinary `effect/water.eft` texture tags, lava material family, and environment/cube reflection inputs are mapped. No water-owned mirrored-scene pass exists in the observed client; planar reflection remains an optional enhancement, not legacy behavior. Only the historical capability-enum spelling is unknown. See `ANALYSIS_CHECKPOINT_2026-09-21_LOCAL_WATER_AND_SHADER_CONTRACT.md`.

# Terrain chunk pipeline names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0084BFF0` | `client_procedural_terrain_worker_entry_VA_0084BFF0` | Generic member-thread wrapper invoking callback at wrapper +0x18. Confirmed. |
| `0x0085EE60` | `terrain_chunk_worker_loop_VA_0085EE60` | Waits for ordered requests, publishes 20-byte completion records and builds ClientChunk outside lock. Confirmed. |
| `0x0084A530` | `terrain_build_client_chunk_VA_0084A530` | Allocates 0xEC pooled ClientChunk, runs generator and converts output to client structures. Confirmed broad role. |
| `0x0085F400` | `terrain_enqueue_chunk_requests_VA_0085F400` | Filters resident/duplicate work, inserts ordered records and signals worker. Confirmed broad role. |
| `0x0085EFD0` | `terrain_reconcile_completed_chunks_VA_0085EFD0` | Three-state normal/drain/reconcile consumer; installs or destroys completed chunks. Confirmed behavior; state names inferred. |
| `0x0085F950` | `terrain_begin_dirty_region_rebuild_VA_0085F950` | Pops dirty region, cancels pending work and gathers intersecting four-way-tree descriptors. Strongly inferred. |
| `0x0085F820` | `terrain_queue_dirty_region_VA_0085F820` | Appends one 16-byte dirty-region record through a terrain-appearance virtual. Structure confirmed; invalidation semantics strongly inferred. |
| `0x0085EDA0` | `terrain_lod_visit_and_request_VA_0085EDA0` | Updates LOD tree and emits missing chunk requests through appearance virtual +0x188. Strongly inferred. |
| `0x0085EBE0` | `terrain_subdivide_unloaded_lod_nodes_VA_0085EBE0` | Recursively creates four child nodes and requests unloaded leaves. Strongly inferred. |
| `0x00849FB0` | `client_procedural_terrain_destroy_VA_00849FB0` | Stops, signals and joins worker before clearing queues and terrain ownership. Confirmed. |

# Terrain LOD-policy names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0085E350` | `terrain_lod_compute_target_scale_VA_0085E350` | Observer-to-node-AABB distance, fine radius, threshold scaling and optional relief bias. Confirmed broad behavior. |
| `0x0085E450` | `terrain_lod_create_missing_children_VA_0085E450` | Creates/describes missing half-scale quadtree children and request records. Confirmed. |
| `0x0085E620` | `terrain_lod_update_node_VA_0085E620` | Chooses split/collapse, activation and recursive child state. Confirmed broad behavior. |
| `0x0085E710` | `terrain_lod_apply_neighbor_stitching_VA_0085E710` | Reconciles four neighbor edge bits and updates chunk primitives when mask changes. Confirmed. |
| `0x00862980` | `client_chunk_activate_render_world_VA_00862980` | Creates/adds the chunk render-world object when needed. Confirmed broad role. |
| `0x00862A10` | `client_chunk_deactivate_render_world_VA_00862A10` | Removes active render-world object without destroying ClientChunk. Confirmed. |

Closed by `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_WORK_SCHEDULING_AND_SEAMS.md` and `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_VERTEX_AND_BLEND_PAYLOAD.md`: the work priority, envelope, seam variants, and complete ClientChunk vertex/material payload are documented. Closed by `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_DIRTY_REGION_CALLERS.md`: the external producer is `ClientObjectTerrainModificationNotification`, with add/remove/move old/new rectangle polarity and server-side terrain-modification correlation established.

# ClientChunk mesh/material names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x008618D0` | `client_chunk_build_render_data_VA_008618D0` | Copies generated grids and constructs material-grouped per-cell primitives plus occlusion data. Confirmed broad role. |
| `0x008612B0` | `terrain_select_cell_material_VA_008612B0` | Samples 3x3 classification neighborhood and resolves material-layer combination. Confirmed broad role. |
| `0x00865DA0` | `terrain_create_cell_primitive_VA_00865DA0` | Allocates/initializes one pooled 0x148-byte nine-vertex primitive. Confirmed. |
| `0x00865FF0` | `terrain_primitive_select_seam_indices_VA_00865FF0` | Selects one of nine embedded topology lists and recomputes triangle planes. Confirmed. |
| `0x008654F0` | `terrain_fetch_blend_shader_VA_008654F0` | Finds/caches material tuple and constructs patterned terrain blend shader parameters. Confirmed broad role. |
| `0x008668E0` | `terrain_shader_set_append_visible_geometry_VA_008668E0` | Appends nine positions and selected 16-bit indices per visible cell to CPU batch. Confirmed. |
| `0x008661A0` | `terrain_shader_set_cull_and_submit_VA_008661A0` | Tests primitive bounds/planes and submits visible cells to terrain sorter. Strongly inferred. |
| `0x00865A80` | `terrain_seam_index_patterns_install_VA_00865A80` | Initializes global registry for nine embedded index lists. Confirmed. |

Closed by later terrain blocks: WMAP participation and most retained grid roles are documented; `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_VERTEX_AND_BLEND_PAYLOAD.md` proves the packed per-vertex DWORD is ARGB diffuse color, while `ALP1..3` are separately bound alpha-map resources sampled with auxiliary float2 UV sets. Terrain declarations are shader-driven but the full emitted semantics are position, normal, diffuse, base UV and up to three auxiliary UV pairs. Remaining bounded unknown: exact shader-program channel interpretation for every ALP texture and human-readable labels for a few retained generator grids.

# Terrain collision/query names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0084BDF0` | `terrain_appearance_collide_VA_0084BDF0` | Traverses terrain tree candidates and invokes collidable leaf object or extent intersection. Confirmed broad role. |
| `0x00860F60` | `client_chunk_collide_segment_VA_00860F60` | Chunk-extent broad phase followed by every ShaderSet's exact collision. Confirmed. |
| `0x008662E0` | `terrain_shader_set_collide_segment_VA_008662E0` | Tests selected indexed triangles/planes and updates nearest hit point, normal and distance. Confirmed. |
| `0x00860FF0` | `client_chunk_query_height_VA_00860FF0` | Vertical surface intersection at requested X/Z; returns intersection Y. Confirmed. |
| `0x00861140` | `client_chunk_query_height_normal_VA_00861140` | Vertical surface intersection returning Y and plane normal. Confirmed. |
| `0x00866550` | `terrain_shader_set_query_vertical_surface_VA_00866550` | X/Z bounds and triangle-containment test over selected topology. Confirmed broad role. |
| `0x00862720` | `client_chunk_sample_dynamic_flora_VA_00862720` | Shared world-coordinate sampler for the `FloraDynamicNear` (`+0xCC`) and `FloraDynamicFar` (`+0xD0`) packed family/selection grids; copies `+0xC4` color and resolves selected child. Confirmed. |
| `0x008628E0` | `client_chunk_sample_environment_family_VA_008628E0` | Maps coordinates into byte grid `+0xD4` and resolves the PTAT environment-family group. Confirmed. |

Reconciled 2026-09-21: terrain collision is fixed one-sided behavior (`dot(segment direction, triangle normal) < 0.0f`); the forwarded parameter object is ignored by ClientChunk/ShaderSet collision. Height queries extend chunk Y bounds by exact `0.1f` and use strict X/Z half-plane tests over the active stitched topology; no lateral LOD-edge epsilon exists in this path. The retained grids and interior floor/portal systems were closed in later dedicated blocks. Only unpublished public type/field labels remain. See `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_COLLISION_POLICY.md`.

# Auxiliary terrain/flora names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0084AD30` | `terrain_query_static_noncollidable_flora_VA_0084AD30` | Exact manager constructor label and callback into ClientChunk `+0xC8`. Confirmed. |
| `0x0084AD70` | `terrain_query_dynamic_near_flora_VA_0084AD70` | Exact `FloraDynamicNear` constructor callback into `+0xCC`. Confirmed. |
| `0x0084ADB0` | `terrain_query_dynamic_far_flora_VA_0084ADB0` | Exact `FloraDynamicFar` constructor callback into `+0xD0`. Confirmed. |
| `0x0084ADF0` | `terrain_query_environment_family_VA_0084ADF0` | ClientChunk `+0xD4` byte-family lookup; environment role strongly inferred. |
| `0x008626F0` | `client_chunk_query_static_noncollidable_flora_VA_008626F0` | Samples `+0xC8` through shared static-flora resolver. Confirmed. |
| `0x00862880` | `client_chunk_query_dynamic_near_flora_VA_00862880` | Samples `+0xCC` and shared color grid. Confirmed. |
| `0x008628B0` | `client_chunk_query_dynamic_far_flora_VA_008628B0` | Samples `+0xD0` and shared color grid. Confirmed. |
| `0x008628E0` | `client_chunk_query_environment_family_VA_008628E0` | Samples `+0xD4` and resolves a one-byte group key. Strongly inferred. |
| `0x008576A0` | `dynamic_radial_flora_advance_VA_008576A0` | Advances double time accumulator then shared radial update. Confirmed. |
| `0x008576C0` | `dynamic_radial_flora_build_render_batches_VA_008576C0` | Splits entries into at-most-500 groups and submits local primitives. Confirmed broad role. |

# Terrain lighting/static-collidable flora names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00B5D6E0` | `chunk_query_static_collidable_flora_VA_00B5D6E0` | Samples `ClientChunk +0xA4` through the packed static-flora resolver. Confirmed. |
| `0x00B5E740` | `terrain_query_static_collidable_flora_VA_00B5E740` | Resolves a containing chunk and forwards a world-coordinate query. Confirmed. |
| `0x00B5E780` | `terrain_update_static_collidable_flora_cell_VA_00B5E780` | Deterministic 16 m cell placement, height/flora query, full object/appearance creation and residency insertion. Confirmed broad role. |
| `0x00B5ED20` | `floor_cell_x_VA_00B5ED20` | Floor-aware division of first horizontal coordinate by supplied cell size. Confirmed. |
| `0x00B5ED70` | `floor_cell_z_VA_00B5ED70` | Floor-aware division of second horizontal coordinate by supplied cell size. Confirmed. |
| `0x00B5EDD0` | `terrain_linear_cell_key_VA_00B5EDD0` | Returns `x + z * rowWidth`. Confirmed. |

Correction: `ClientChunk +0xA8` is a binary terrain-lighting/shading mask, not a passability grid. Its exact original label remains unknown.

Follow-up 2026-09-21: dynamic flora residency is a fixed jittered radial grid whose out-of-radius nodes are wrapped, re-queried and enabled/disabled in place; it is not a time-based eviction cache. Dynamic node phase reseeds the legacy RNG with `x + z`. Static-collidable construction is fully mapped, but its exact placements consume the procedural generator's continuing global RNG state, so strict placement parity still requires generator call-order validation. See `ANALYSIS_CHECKPOINT_2026-09-21_FLORA_RESIDENCY_AND_RANDOMNESS.md`.

# Interior floor runtime names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x012647B0` | `test_floor_collision_VA_012647B0` | Walks from a starting FloorLocator; result 2/4/10 returns boolean zero; other owner-resolved collision outputs can return one. Missing start floor returns one. Boolean is not a universal legal-movement flag. Confirmed; see single-cell movement contract. |
| `0x01267380` | `collision_world_can_move_on_floor_VA_01267380` | Contact-height-aware floor movement solver with boundary recursion capped at five. Confirmed broad role. |
| `0x01273B80` | `floor_mesh_load_VA_01273B80` | FLOR version dispatcher for 0000 through 0006 and common post-load processing. Confirmed. |
| `0x012751E0` | `floor_mesh_load_0006_VA_012751E0` | Loads VERT, TRIS, optional BTRE, BEDG and PGRF. Confirmed. |
| `0x012732E0` | `floor_mesh_get_triangle_VA_012732E0` | Bounds checks and returns a 100-byte runtime triangle record. Confirmed. |
| `0x01292890` | `floor_bounding_tree_load_VA_01292890` | Loads optional BTRE/0000 for floor acceleration. Confirmed. |
| `0x006E6850` | `cell_object_set_allow_entry_VA_006E6850` | Stores replicated permission byte at CellObject +0x29C; no resource invalidation. Confirmed. |

# Portal layout and door runtime names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00B461B0` | `portal_layout_load_VA_00B461B0` | PRTO version dispatcher for 0000 through 0004. Confirmed. |
| `0x00B45BA0` | `portal_layout_read_crc_VA_00B45BA0` | Version-aware PRTO CRC probe/extractor. Confirmed. |
| `0x00B440B0` | `portal_cell_template_load_VA_00B440B0` | CELL version dispatcher for 0001 through 0005. Confirmed. |
| `0x00B43880` | `portal_reference_load_VA_00B43880` | PRTL version dispatcher for 0001 through 0005. Confirmed. |
| `0x00B2B240` | `cell_property_find_destination_cell_VA_00B2B240` | Tests transformed segment against cell portals and returns nearest connected destination. Confirmed broad role. |
| `0x00B53500` | `portal_runtime_create_door_VA_00B53500` | Resolves door style, constructs DoorObject and registers related objects. Confirmed broad role. |
| `0x0128FF40` | `door_object_ctor_VA_0128FF40` | Initializes runtime DoorObject. Confirmed. |
| `0x012902D0` | `door_object_create_appearances_VA_012902D0` | Creates frame/door appearances and force-field special path. Confirmed broad role. |
| `0x01290560` | `door_object_create_collision_property_VA_01290560` | Installs DoorCollisionProperty using configured motion vector. Confirmed broad role. |
| `0x012906F0` | `door_object_create_portal_barrier_VA_012906F0` | Creates portal blocking/barrier component. Confirmed broad role. |
| `0x01290760` | `door_object_alter_VA_01290760` | Advances door controllers, moves leaves, fires transition effects and updates portal blocking. Confirmed broad role. |
| `0x01290BB0` | `door_collision_notify_VA_01290BB0` | Validates candidate cell ownership, retains trigger object and starts door transition. Confirmed broad role. |
| `0x01290D50` | `door_object_add_trigger_object_VA_01290D50` | Deduplicates and retains a candidate in the bounded trigger list. Confirmed. |
| `0x01290EC0` | `door_object_prune_trigger_objects_VA_01290EC0` | Removes null or out-of-radius trigger objects. Confirmed. |
| `0x01290CB0` | `door_object_set_paired_door_VA_01290CB0` | Installs paired door and synchronizes barrier association. Confirmed broad role. |
| `0x00B53940` | `portal_record_set_enabled_VA_00B53940` | Stores changed portal-state byte and invokes registered callback. Confirmed. |

# Skeleton and animation format names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00829EB0` | `lod_skeleton_template_install_VA_00829EB0` | Registers SLOD factory and optional memory-block manager. Confirmed. |
| `0x0082A000` | `lod_skeleton_template_ctor_load_VA_0082A000` | SLOD/0000 dispatcher and object construction. Confirmed. |
| `0x0082A1D0` | `lod_skeleton_template_load_0000_VA_0082A1D0` | Reads signed-16 template count and nested skeleton templates. Confirmed. |
| `0x0082A620` | `basic_skeleton_template_install_VA_0082A620` | Registers SKTM factory. Confirmed. |
| `0x0082A6D0` | `basic_skeleton_template_ctor_load_VA_0082A6D0` | SKTM dispatcher supporting 0001 and 0002. Confirmed. |
| `0x007E0F20` | `logical_animation_table_load_VA_007E0F20` | LATT/0000 dispatcher. Confirmed. |
| `0x007E1250` | `logical_animation_table_load_0000_VA_007E1250` | Reads logical ANIM names, child templates, fallback and sorted index. Confirmed broad role. |
| `0x00824E30` | `compressed_keyframe_template_install_VA_00824E30` | Registers CKAT skeletal-animation factory. Confirmed. |
| `0x00825710` | `compressed_keyframe_template_ctor_load_VA_00825710` | CKAT dispatcher supporting 0000 and 0001. Confirmed. |
| `0x00825B30` | `compressed_keyframe_template_load_0001_VA_00825B30` | Effective CKAT loader for transform, rotation, translation, message and locomotion channels. Confirmed. |
| `0x0081C900` | `keyframe_animation_template_ctor_load_VA_0081C900` | KFAT dispatcher supporting 0002 and 0003. Confirmed. |
| `0x0081DF90` | `keyframe_animation_template_load_0003_VA_0081DF90` | Effective KFAT loader including LOCT and LOCR. Confirmed. |

# Animation state hierarchy names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x007EDFA0` | `animation_state_hierarchy_ctor_load_VA_007EDFA0` | ASHT dispatcher supporting 0000, 0001 and 0002. Confirmed. |
| `0x007EE390` | `animation_state_hierarchy_load_0000_VA_007EE390` | Legacy ASHT reader. Version confirmed; detailed fields partial. |
| `0x007EE7C0` | `animation_state_hierarchy_load_0001_VA_007EE7C0` | ASHT/0001 reader. Version confirmed; detailed fields partial. |
| `0x007EEBF0` | `animation_state_hierarchy_load_0002_VA_007EEBF0` | Loads optional AGPS action groups and root state tree. Confirmed broad role. |
| `0x007EDCC0` | `animation_state_hierarchy_write_binary_VA_007EDCC0` | Writes canonical ASHT/0002 with groups and root state. Confirmed. |

# Skeletal pose and yaw names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x0080E610` | `yaw_skeletal_animation_template_install_VA_0080E610` | Registers the typed yaw-animation template factory. Confirmed. |
| `0x0080EB30` | `yaw_skeletal_animation_template_ctor_load_VA_0080EB30` | Dispatches template version 0000. Confirmed. |
| `0x0080ED20` | `yaw_skeletal_animation_template_load_0000_VA_0080ED20` | Loads optional left, right and neutral child templates. Confirmed. |
| `0x00839C40` | `yaw_skeletal_animation_ctor_VA_00839C40` | Instantiates three children, blend delegate and `yaw_dir` environment handle. Confirmed. |
| `0x00839FA0` | `yaw_skeletal_animation_select_child_VA_00839FA0` | Selects left/right/neutral by sign and cross-fades on change. Confirmed. |
| `0x007D0700` | `look_at_transform_modifier_install_VA_007D0700` | Installs configuration and angular limits. Confirmed. |
| `0x007D07B0` | `look_at_transform_modifier_ctor_VA_007D07B0` | Initializes the look-at transform modifier. Confirmed. |
| `0x007D0820` | `look_at_transform_modifier_apply_VA_007D0820` | Computes, clamps and rate-limits local-space yaw. Confirmed broad role. |
| `0x007D8ED0` | `target_pitch_transform_modifier_install_VA_007D8ED0` | Installs target modifier configuration and pitch clamp. Confirmed. |
| `0x007D8F80` | `target_pitch_transform_modifier_ctor_VA_007D8F80` | Initializes target-pitch modifier. Confirmed. |
| `0x007D8FE0` | `target_pitch_transform_modifier_apply_VA_007D8FE0` | Applies clamped local-space pitch toward a valid target. Confirmed broad role. |
| `0x004355B0` | `skeletal_appearance_attach_pose_modifiers_VA_004355B0` | Attaches target pitch to spine3 and chained look-at modifiers to neck/head. Confirmed broad role. |
| `0x007EAC50` | `alignment_transform_modifier_apply_VA_007EAC50` | Tolerance-gated corrective rotation aligning a configured axis to a derived desired vector. Confirmed broad role. |
| `0x00679C30` | `rider_spine_transform_modifier_ctor_VA_00679C30` | Constructs AlignmentTransformModifier subclass used on spine1. Confirmed. |
| `0x00679C90` | `rider_spine_transform_modifier_get_desired_vector_VA_00679C90` | Returns the fixed desired alignment direction. Confirmed. |

# State-hierarchy runtime names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x007CF440` | `state_hierarchy_animation_controller_ctor_VA_007CF440` | Retains ASHT/LATT, current state path and TrackAnimationController. Confirmed. |
| `0x007CF760` | `state_hierarchy_change_state_path_VA_007CF760` | Traverses old/new hierarchical paths, submits link animations and starts destination loop. Confirmed broad role. |
| `0x007CFAB0` | `state_hierarchy_play_action_VA_007CFAB0` | Resolves an action and routes it to action/add track. Confirmed broad role. |
| `0x007CFCE0` | `state_hierarchy_resolve_action_VA_007CFCE0` | Searches current state and ancestors for action logical-animation and mode. Confirmed broad role. |
| `0x007CFD70` | `state_hierarchy_instantiate_logical_animation_VA_007CFD70` | Resolves LATT entry and constructs typed SkeletalAnimation. Confirmed. |
| `0x007CFC60` | `state_hierarchy_submit_animation_VA_007CFC60` | Applies default fallback and forwards animation/track/flags to TrackAnimationController. Confirmed. |
| `0x007CFDC0` | `state_hierarchy_set_pending_animation_timer_VA_007CFDC0` | Stores delayed animation identifier and timer. Confirmed storage; trigger semantics broad. |

# Audio playback names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00418F40` | `sound2d_runtime_ctor_VA_00418F40` | Common Sound2d/Sound3d dynamic-state and multi-sample initialization. Confirmed broad role. |
| `0x004196C0` | `sound_runtime_alter_and_apply_backend_parameters_VA_004196C0` | Per-frame voice state, gain/rate and spatial-occlusion update. Confirmed broad role. |
| `0x0041A2E0` | `sound_runtime_select_sample_VA_0041A2E0` | Random, sequential or shuffled/non-repeating sample selection. Confirmed broad role. |
| `0x0041A7D0` | `sound_runtime_initialize_volume_policy_VA_0041A7D0` | Initializes volume targets from template +0x70..+0x80 policy. Confirmed. |
| `0x00419B20` | `sound_runtime_update_volume_target_VA_00419B20` | Retargets/interpolates continuously varying template volume. Confirmed. |
| `0x0041AB50` | `sound_runtime_compute_effective_volume_VA_0041AB50` | Multiplies template volume, attenuation, envelope and external gain factors. Confirmed broad role. |
| `0x00419F00` | `sound_runtime_compute_playback_rate_multiplier_VA_00419F00` | Converts current pitch state exponentially to a rate multiplier. Confirmed broad role. |
| `0x0040FC40` | `audio_voice_set_volume_VA_0040FC40` | Type-aware 2D/3D/stream Miles volume adapter. Confirmed. |
| `0x0040FE70` | `audio_voice_set_playback_rate_VA_0040FE70` | Type-aware Miles playback-rate adapter with 5,000..60,000 clamp. Confirmed. |
| `0x0040FBD0` | `audio_3d_voice_set_obstruction_VA_0040FBD0` | Clamped Miles 3D obstruction adapter. Confirmed. |
| `0x0040FB60` | `audio_3d_voice_set_occlusion_VA_0040FB60` | Clamped Miles 3D occlusion adapter. Confirmed. |
| `0x0040F590` | `audio_voice_create_and_configure_backend_VA_0040F590` | Creates resident 2D, 3D or stream voice; applies gain/rate, loop count/markers, spatial data and callbacks. Confirmed broad role. |
| `0x0040C780` | `audio_get_effective_category_gain_VA_0040C780` | Reads one of 13 category gains and applies master scale except for category 8. Confirmed. |
| `0x0041B640` | `config_client_audio_install_cache_policy_VA_0041B640` | Reads `maxCached2dSampleSize`, clamps 0..512 KiB, and stores bytes. Confirmed. |
| `0x00412D80` | `audio_set_max_sample_count_VA_00412D80` | Clamps the configured sample-count limit to 16..64. Confirmed. |
| `0x00412DD0` | `audio_set_max_cached_2d_sample_bytes_VA_00412DD0` | Defensively clamps the resident 2D cutoff to 64..512 KiB. Confirmed. |
| `0x00412E10` | `audio_get_max_cached_2d_sample_bytes_VA_00412E10` | Returns the resident-versus-streamed 2D cutoff. Confirmed. |
| `0x0040C210` | `audio_acquire_sample_resource_VA_0040C210` | Routes eligible 2D assets above cutoff to `AIL_open_stream`, smaller assets to resident Miles samples, and spatial assets to 3D samples. Confirmed broad role. |
| `0x0041BAE0` | `sound_sample_resource_create_VA_0041BAE0` | Repeats the size/cutoff policy and constructs the corresponding sample resource. Confirmed broad role. |

# ASHT link/action policy names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00834C80` | `editable_animation_state_link_ctor_load_VA_00834C80` | Constructs a link and dispatches legacy/v2 readers. Confirmed. |
| `0x00834FC0` | `animation_state_link_get_target_path_VA_00834FC0` | Returns the owned AnimationStatePath. Confirmed. |
| `0x00834FD0` | `animation_state_link_has_transition_animation_VA_00834FD0` | Tests optional logical-animation name for non-empty content. Confirmed behavior; source name provisional. |
| `0x00835000` | `animation_state_link_get_transition_animation_name_VA_00835000` | Returns optional logical name or shared empty value. Confirmed behavior; source name provisional. |
| `0x00832C60` | `animation_state_find_first_matching_link_VA_00832C60` | Iterates stored links and returns the first whose path matches requested path/depth. Confirmed. |
| `0x00835340` | `animation_state_link_load_legacy_VA_00835340` | Reads LINK path, optional flag and legacy signed-16 transition reference for versions 0/1. Confirmed. |
| `0x008354E0` | `animation_state_link_load_v2_VA_008354E0` | Reads LINK path, optional flag and direct logical-animation string. Confirmed. |
| `0x007F7150` | `movement_animation_action_select_moving_VA_007F7150` | Returns true when any `loco_vel` component is nonzero. Confirmed. |
| `0x007F7B50` | `basic_animation_action_get_legacy_reserved_flag_VA_007F7B50` | Returns binary ACTN trailer byte 0 (`+0x28`). No runtime caller; XML omits it; all 16,617 ACTN records in configured archive copies store false. Confirmed dormant compatibility field. |
| `0x007F7B60` | `basic_animation_action_get_use_add_track_VA_007F7B60` | Returns XML-named `useAddTrack` byte `+0x29`. Confirmed. |
| `0x0083E9B0` | `physical_animation_track_submit_change_VA_0083E9B0` | Mode 1 replaces/clears pending work; mode 0 queues if occupied and applies immediately if empty. Confirmed. |

# Interior environment and weather names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00791E30` | `interior_environment_block_manager_install_VA_00791E30` | Executable-named manager install; creates default record/map and loads the interior table. Confirmed. |
| `0x007922A0` | `interior_environment_block_manager_load_table_VA_007922A0` | Reads all 13 DTII columns into 52-byte records. Confirmed. |
| `0x007920F0` | `interior_environment_block_lookup_VA_007920F0` | Searches exact POB/cell, POB default, then global default keys. Confirmed. |
| `0x00AD3F90` | `server_weather_message_parse_VA_00AD3F90` | Parses S32 weather and three-float wind vector. Confirmed with server constructor. |
| `0x0051C4E9` | `ground_scene_dispatch_server_weather_message_VA_0051C4E9` | Registered-message branch that parses and forwards wind/weather fields. Confirmed. |
| `0x00844AA0` | `global_wind_set_direction_VA_00844AA0` | Static wind service stores and normalizes direction, combines it with magnitude at `0x00844AD0`, and publishes effective wind to registered callbacks via `0x00844B10`. Confirmed. |
| `0x00845C90` | `ground_environment_set_weather_index_VA_00845C90` | Stores the server-provided weather index consumed by environment selection. Confirmed. |
| `0x0085D7F0` | `environment_descriptor_lookup_by_family_weather_VA_0085D7F0` | Searches `(familyId<<16)|weather`, retries same family/weather 0, then returns the manager's empty default descriptor. Confirmed. |

# Server time and environment-clock names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00ADC0F0` | `server_time_message_parse_VA_00ADC0F0` | Parses the server LE64 elapsed-Zone seconds field. Confirmed with server constructor. |
| `0x0051C41E` | `ground_scene_dispatch_server_time_message_VA_0051C41E` | Registered listener branch; performs clock-offset and normalized environment-cycle updates. Confirmed. |
| `0x0051F8F0` | `ground_scene_set_legacy_server_clock_offset_VA_0051F8F0` | Stores `low32(serverSeconds)-localWallClockSeconds` at scene `+0xC0`. Confirmed. |
| `0x0051F900` | `ground_scene_get_legacy_synchronized_seconds_VA_0051F900` | Returns local wall-clock seconds plus stored offset. Confirmed behavior; no direct static caller found. |
| `0x00B5CB40` | `terrain_get_environment_cycle_duration_VA_00B5CB40` | Virtual terrain-appearance query leading to PTAT template member `+0x40`. Confirmed. |
| `0x00B6E6C0` | `procedural_terrain_template_get_environment_cycle_duration_VA_00B6E6C0` | Returns PTAT member `+0x40`; concrete deployed Tatooine value is 11280 seconds. Confirmed. |
| `0x00846BE0` | `ground_environment_get_normalized_cycle_time_VA_00846BE0` | Returns cycle seconds divided by cycle duration. Confirmed. |
| `0x00846BF0` | `ground_environment_set_normalized_cycle_time_VA_00846BF0` | Sets cycle seconds from normalized input subject to disable/initialization policy and refreshes entry phases. Confirmed principal behavior. |
| `0x008479B0` | `ground_environment_update_VA_008479B0` | Adds frame delta, wraps at cycle duration, derives normalized phase, and updates environmental state. Confirmed. |

# Environment-family selector names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x008628E0` | `client_chunk_sample_environment_family_VA_008628E0` | Samples ClientChunk byte grid `+0xD4` by position and resolves it through the PTAT family group. Confirmed. |
| `0x0085D880` | `environment_descriptor_table_load_VA_0085D880` | Reads all 25 DTII columns, validates PTAT family name, constructs composite-key descriptors. Confirmed. |
| `0x008505D0` | `environment_descriptor_construct_VA_008505D0` | Initializes the pointer-rich runtime descriptor and all ramp arrays/resources. Confirmed. |
| `0x00850970` | `environment_descriptor_assign_row_VA_00850970` | Copies row scalars/strings, loads sky/cloud/ramp/appearance/environment-map/audio resources and wind scale. Confirmed. |
| `0x008510F0` | `environment_descriptor_load_lighting_ramp_VA_008510F0` | Decodes the 256×8 image into ordered RGB/scalar/RGBA arrays with deterministic fallback. Confirmed. |
| `0x00845460` | `ground_environment_load_envm_VA_00845460` | Validates ENVM and dispatches version 0000. Confirmed. |
| `0x008455E0` | `ground_environment_load_envm_v0000_VA_008455E0` | Loads NSKY/STAR/SUN/SSUN/MOON/SMOO/repeated CELS celestial definitions. Confirmed form/chunks; detailed CELS labels incomplete. |
| `0x00846D00` | `ground_environment_blend_descriptors_VA_00846D00` | Interpolates previous/current ramp-derived visual parameters by supplied transition fraction. Confirmed. |
| `0x00848940` | `ground_environment_rebuild_sky_cube_texture_VA_00848940` | Builds `_SKY` cube texture from a supplied sky resource, or six one-pixel faces using ramp row 5 fallback RGB. Confirmed. |
| `0x008488F0` | `ground_environment_publish_fog_to_world_cell_VA_008488F0` | Publishes fog-enable, row 6 fog color and derived fog density to the current world cell. Confirmed. |
| `0x0078D940` | `shadow_volume_set_global_color_VA_0078D940` | Stores the global ShadowVolume RGBA supplied by lighting-ramp row 7. Confirmed. |
| `0x00850DE0` | `environment_descriptor_get_ambient_color_ramp_VA_00850DE0` | Returns row 0 RGB array. Confirmed. |
| `0x00850DF0` | `environment_descriptor_get_generic_celestial_opacity_ramp_VA_00850DF0` | Returns row 0 scalar array consumed by ENVM CELS objects, gated by gradient-sky availability. Confirmed. |
| `0x008451B0` | `envm_cels_record_construct_VA_008451B0` | Constructs the 0x34-byte runtime CELS record with two strings, sizes, angular/animation fields and zero phase. Confirmed. |
| `0x008439F0` | `ground_environment_celestial_object_construct_VA_008439F0` | Creates primary and optional secondary shader billboards for SUN/MOON/CELS runtime objects. Confirmed principal behavior. |
| `0x00843CD0` | `ground_environment_celestial_set_opacity_VA_00843CD0` | Applies shared opacity to primary appearance and multiplier-adjusted opacity to optional secondary appearance. Confirmed. |
| `0x00850E00` | `environment_descriptor_get_main_primary_color_ramp_VA_00850E00` | Returns row 1 RGB array. Confirmed. |
| `0x00850E10` | `environment_descriptor_get_main_primary_intensity_ramp_VA_00850E10` | Returns row 1 scalar array. Confirmed. |
| `0x00850E20` | `environment_descriptor_get_main_secondary_color_ramp_VA_00850E20` | Returns row 2 RGB array; exact historical “specular” label remains unproven. Confirmed consumer. |
| `0x00850E30` | `environment_descriptor_get_main_secondary_intensity_ramp_VA_00850E30` | Returns row 2 scalar array. Confirmed consumer. |
| `0x00850E40` | `environment_descriptor_get_fill_color_ramp_VA_00850E40` | Returns row 3 RGB array. Confirmed. |
| `0x00850E50` | `environment_descriptor_get_fill_intensity_ramp_VA_00850E50` | Returns row 3 scalar array. Confirmed. |
| `0x00850E60` | `environment_descriptor_get_bounce_color_ramp_VA_00850E60` | Returns row 4 RGB array. Confirmed. |
| `0x00850E70` | `environment_descriptor_get_bounce_intensity_ramp_VA_00850E70` | Returns row 4 scalar array. Confirmed. |
| `0x00850E80` | `environment_descriptor_get_fallback_sky_color_ramp_VA_00850E80` | Returns row 5 RGB array used to synthesize a solid fallback `_SKY` cubemap. Confirmed. |
| `0x00850E90` | `environment_descriptor_get_celestial_intensity_ramp_VA_00850E90` | Returns row 5 scalar array propagated to ENVM celestial appearances. Confirmed behavior. |
| `0x00850EC0` | `environment_descriptor_get_fog_color_ramp_VA_00850EC0` | Returns row 6 RGB array published as world-cell fog color. Confirmed. |
| `0x00850ED0` | `environment_descriptor_get_sky_family_opacity_ramp_VA_00850ED0` | Returns row 6 scalar array controlling stars and sky/cloud transition opacity. Confirmed. |
| `0x00850F00` | `environment_descriptor_get_shadow_volume_color_ramp_VA_00850F00` | Returns row 7 RGBA array consumed by ShadowVolume global color. Confirmed. |

# PTAT terrain-map names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00B9A470` | `terrain_maps_load_VA_00B9A470` | Dispatches MAPS versions 0000/0001 and rejects unsupported versions. Confirmed. |
| `0x00B9A520` | `terrain_maps_load_v0001_VA_00B9A520` | Reads two F32 and two U32 fields, then WMAP and SMAP at `bytesPerRow * bitDimension` bytes each. Confirmed. |
| `0x00B9A620` | `terrain_maps_query_water_grid_point_VA_00B9A620` | Applies centered integer-grid offset and samples WMAP. Confirmed. |
| `0x00B9A660` | `terrain_maps_query_seed_grid_point_VA_00B9A660` | Applies centered integer-grid offset and samples SMAP. Confirmed mechanics; map name confirmed by server source. |
| `0x00B9A740` | `terrain_maps_query_water_world_rect_any_VA_00B9A740` | Converts rectangle and returns whether any WMAP bit is set inclusively. Confirmed. |
| `0x00B9A760` | `terrain_maps_query_seed_world_rect_any_VA_00B9A760` | Converts rectangle and returns whether any SMAP bit is set inclusively. Confirmed mechanics; no direct runtime caller found. |
| `0x00B9A780` | `terrain_maps_allocate_empty_VA_00B9A780` | Derives dimension and row stride from world size/resolution and allocates zeroed WMAP/SMAP grids. Confirmed. |
| `0x00B9A960` | `terrain_maps_world_to_centered_grid_VA_00B9A960` | Converts world X/Z to centered map indices with explicit negative-coordinate handling. Confirmed. |
| `0x00B9A9C0` | `terrain_maps_test_bit_VA_00B9A9C0` | Bounded LSB-first one-bit-grid lookup; out-of-range is false. Confirmed. |
| `0x00B9AA20` | `terrain_maps_rect_any_bit_VA_00B9AA20` | Normalizes converted corners and scans an inclusive rectangle for any set bit. Confirmed. |
| `0x00B9AAF0` | `terrain_maps_assign_bit_VA_00B9AAF0` | Sets or clears one LSB-first map bit. Confirmed. |
| `0x00B5E0E0` | `client_procedural_terrain_query_water_grid_point_VA_00B5E0E0` | PTAT TerrainMaps WMAP point-query virtual implementation. Confirmed. |
| `0x00B5E160` | `client_procedural_terrain_query_water_world_rect_any_VA_00B5E160` | PTAT TerrainMaps WMAP rectangle virtual implementation. Confirmed. |
| `0x00B5E120` | `client_procedural_terrain_query_seed_grid_point_VA_00B5E120` | PTAT TerrainMaps SMAP point-query virtual implementation. Confirmed; no direct caller found. |
| `0x00B5E1A0` | `client_procedural_terrain_query_seed_world_rect_any_VA_00B5E1A0` | PTAT TerrainMaps SMAP rectangle virtual implementation. Confirmed; no direct caller found. |
| `0x00B5C370` | `terrain_owner_query_water_grid_point_VA_00B5C370` | Dispatches common terrain vtable slot `+0xCC`; three direct callers established. Confirmed. |
| `0x006C9CE0` | `estimate_camera_local_water_coverage_VA_006C9CE0` | Samples a 5x5 grid around the active camera and returns a bounded WMAP occupancy fraction. Confirmed mechanics; historical name provisional. |
| `0x006CBA40` | `structure_placement_camera_update_render_VA_006CBA40` | RTTI-owned `StructurePlacementCamera` vtable method; uses WMAP while traversing/submitting terrain cells during placement view. Confirmed owner and principal behavior. |
| `0x00B4CA80` | `terrain_cell_process_skip_water_VA_00B4CA80` | Rejects water-marked cells before procedural classification/height processing. Confirmed principal behavior. |

# Structure-placement names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x013F3C00` | `enter_structure_placement_message_parse_VA_013F3C00` | Archive parser for NetworkId deed ID plus ASCII client-template path; RTTI/type string confirmed. |
| `0x00E40E40` | `game_message_receiver_dispatch_VA_00E40E40` | Central named-message receiver; structure-placement branch begins at `0x00E40FE3`. Confirmed. |
| `0x00CA9FB0` | `swg_cui_structure_placement_begin_VA_00CA9FB0` | Clears prior session, stores deed ID, fetches/validates SharedTangibleObjectTemplate, creates preview object and configures placement camera. Confirmed. |
| `0x00CAA260` | `swg_cui_structure_placement_clear_session_VA_00CAA260` | Frees copied path and releases template/derived data/temporary preview object. Confirmed. |
| `0x00CA9A30` | `swg_cui_structure_placement_submit_VA_00CA9A30` | Formats `deedId X Z quarterTurn` and queues `placeStructure`. Confirmed. |
| `0x006CB0F0` | `structure_placement_camera_construct_VA_006CB0F0` | Initializes camera state, three placement shaders and configuration flags. Confirmed. |
| `0x006CAD90` | `structure_placement_camera_build_footprint_cell_VA_006CAD90` | Samples four terrain-corner heights, classifies cell and emits color-coded footprint geometry. Confirmed principal behavior. |

# Ticket/travel names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x013F37A0` | `enter_ticket_purchase_mode_message_parse_VA_013F37A0` | Parses two registered ASCII fields for departure planet and point. Confirmed. |
| `0x00D85160` | `swg_cui_ticket_purchase_set_departure_VA_00D85160` | Copies those two strings to mediator members `+0xE4/+0xF0`. Confirmed. |
| `0x013F1BF0` | `planet_travel_point_list_request_construct_VA_013F1BF0` | Constructs registered NetworkId and ASCII zone fields; local correlation value is not serialized. Confirmed. |
| `0x00D84500` | `swg_cui_ticket_purchase_activate_and_request_planets_VA_00D84500` | Subscribes for responses, enumerates planets, increments outstanding count and sends requests. Confirmed principal behavior. |
| `0x013F1120` | `planet_travel_point_list_response_parse_VA_013F1120` | Parses planet plus name, coordinate, tax and interplanetary columns. Confirmed. |
| `0x00D850A0` | `swg_cui_ticket_purchase_message_listener_VA_00D850A0` | Recognizes PlanetTravelPointListResponse and forwards the parsed message. Confirmed. |
| `0x00D86380` | `swg_cui_ticket_purchase_apply_travel_points_VA_00D86380` | Decrements outstanding count, validates/joins response columns, and rebuilds destination/map UI state. Confirmed principal behavior. |
| `0x00D848D0` | `swg_cui_ticket_purchase_ui_event_VA_00D848D0` | Handles ticket UI events; purchase branch builds and queues `purchaseTicket` arguments. Confirmed principal behavior. |
| `0x00D84BC3` | `swg_cui_ticket_purchase_launch_into_space_branch_VA_00D84BC3` | Builds ShipControlDevice/group/destination arguments and submits `launchIntoSpace` against the retained terminal. Confirmed. |
| `0x00D86BE0` | `swg_cui_ticket_purchase_set_space_terminal_id_VA_00D86BE0` | Copies the U64 command-target NetworkId to mediator `+0x110`. Confirmed by selection-source and send-site use. |
| `0x00D86C10` | `swg_cui_ticket_purchase_set_ship_control_device_id_VA_00D86C10` | Copies the selected ship/device U64 ID to mediator `+0x118`. Confirmed by selection-source and first command argument. |
| `0x00DFFA90` | `launch_party_member_list_get_VA_00DFFA90` | Returns the shared NetworkId-list root at `0x0196A974`, traversed by both launch command builders. Confirmed. |
| `0x00DF6BD0` | `swg_cui_space_terminal_selection_event_VA_00DF6BD0` | Handles space-terminal/ship selection, populates launch mediator IDs, and can submit the same launch command. Confirmed principal behavior. |

# Object radial-menu names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00AF31D0` | `object_menu_controller_payload_install_VA_00AF31D0` | Registers shared payload factories for inner types `0x146` and `0x147`. Confirmed. |
| `0x00AF3220` | `message_queue_object_menu_construct_VA_00AF3220` | Builds target/player IDs, 44-byte internal item records and U8 counter. Confirmed. |
| `0x00AF3510` | `message_queue_object_menu_serialize_VA_00AF3510` | Writes two IDs, count, four bytes plus Unicode per item, then counter. Confirmed. |
| `0x00AF35F0` | `message_queue_object_menu_parse_factory_VA_00AF35F0` | Reads the same shared `0x146/0x147` payload and constructs the typed queue object. Confirmed. |
| `0x00968250` | `cui_radial_menu_update_cache_VA_00968250` | Validates counter, diagnoses missing/stale cache state and merges response items. Confirmed principal behavior. |
| `0x009685B0` | `cui_radial_menu_request_or_build_VA_009685B0` | Builds local/default menu state and sends inner `0x146` when server content is required. Confirmed principal behavior. |
| `0x009698C0` | `cui_radial_menu_update_and_retry_VA_009698C0` | Advances pending requests, retransmits expired `0x146` requests, and removes invalid/completed entries. Confirmed principal behavior. |
| `0x00AF3A30` | `object_menu_select_message_construct_VA_00AF3A30` | Registers U64 target and U8 radial ID for named selection message. Confirmed. |
| `0x0096B5D0` | `cui_radial_menu_select_cached_item_VA_0096B5D0` | Resolves cached item and sends server callback selections. Confirmed principal behavior. |
| `0x0096BDB0` | `cui_radial_menu_execute_selection_VA_0096BDB0` | Handles local/default actions and direct server-callback selections. Confirmed principal behavior; complete local-action catalog remains breadth work. |

# SUI protocol/runtime names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00A45D60` | `sui_network_message_listener_VA_00A45D60` | Dispatches create, update, force-close, and control notifications by registered message type. Confirmed. |
| `0x00B008F0` | `sui_create_page_message_parse_VA_00B008F0` | Constructs a typed create message from the archive and registers embedded page data. Confirmed. |
| `0x00B00320` | `sui_update_page_message_parse_VA_00B00320` | Constructs the update message from the same embedded page-data representation. Confirmed. |
| `0x00B01FA0` | `sui_force_close_page_parse_VA_00B01FA0` | Parses the force-close message containing the page ID. Confirmed. |
| `0x00A46400` | `sui_manager_create_page_VA_00A46400` | Resolves root page, creates runtime mediator, applies page data, retains it by page ID, activates it, and applies target/range. Confirmed principal behavior. |
| `0x00A46640` | `sui_manager_close_page_by_id_VA_00A46640` | Looks up a retained page and closes/removes it. Confirmed. |
| `0x00A46870` | `sui_manager_apply_update_by_id_VA_00A46870` | Finds a retained page by ID and applies the supplied page data. Confirmed. |
| `0x00A7C480` | `sui_runtime_apply_page_data_VA_00A7C480` | Executes command records, skips subscriptions on updates, and applies target/range fields. Confirmed. |
| `0x00A7C9D0` | `sui_runtime_execute_command_VA_00A7C9D0` | Seven-way command interpreter for clear/create/property/item/subscribe/container operations; rejects other types. Confirmed. |
| `0x00A7C0E0` | `sui_runtime_handle_ui_event_VA_00A7C0E0` | Matches a subscription, extracts widget values, builds notification and sends it reliably. Confirmed. |
| `0x00B0A700` | `sui_event_notification_construct_VA_00B0A700` | Constructs page ID, event index and Unicode-argument vector fields. Confirmed. |

# NPC conversation names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00B12F20` | `start_npc_conversation_payload_parse_VA_00B12F20` | Parses U64 target, U8 starter, ASCII text and U32 mobile CRC for inner `0xDD`. Confirmed. |
| `0x00B12BD0` | `stop_npc_conversation_payload_parse_VA_00B12BD0` | Parses U64 target for inner `0xDE`. Confirmed. |
| `0x00B14FF0` | `npc_conversation_text_payload_parse_VA_00B14FF0` | Parses the inner `0xDF` Unicode content payload. Confirmed. |
| `0x00B123A0` | `conversation_string_list_payload_parse_VA_00B123A0` | Reads U8 count and that many Unicode option strings for inner `0xE0`. Confirmed. |
| `0x008E7CA0` | `cui_conversation_start_VA_008E7CA0` | Sets target/mobile CRC, resets changed-target state and publishes opening state. Confirmed principal behavior. |
| `0x008E7E30` | `cui_conversation_stop_VA_008E7E30` | Terminates matching ground/space conversation and publishes teardown state. Confirmed principal behavior. |
| `0x008E7AA0` | `cui_conversation_set_text_VA_008E7AA0` | Normalizes/retains text and routes it to conversation/spatial presentation. Confirmed principal behavior. |
| `0x008E7550` | `cui_conversation_set_responses_VA_008E7550` | Replaces response strings and publishes response/menu changes. Confirmed principal behavior. |
| `0x008E7434` | `cui_conversation_select_response_branch_VA_008E7434` | Formats zero-based index and submits `npcConversationSelect` through the ordinary command queue. Confirmed. |

# Character-sheet response names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00FA0100` | `character_sheet_response_listener_VA_00FA0100` | Five-way listener for faction, guild, money, badges and character-sheet response messages. Confirmed. |
| `0x013F5E00` | `faction_response_message_parse_VA_013F5E00` | Registers rank, three principal standings and parallel faction-name/float arrays in server wire order. Confirmed. |
| `0x00FA16A0` | `character_sheet_refresh_faction_projection_VA_00FA16A0` | Refreshes the mediator after the listener rebuilds its faction-standing map. Confirmed principal behavior. |
| `0x013F4760` | `guild_response_message_parse_VA_013F4760` | Parses creature ID, guild name and trailing U16 field. Confirmed. |
| `0x00FA14B0` | `character_sheet_apply_guild_response_VA_00FA14B0` | Updates guild presentation for the local-player response. Confirmed principal behavior. |
| `0x013F64D0` | `player_money_response_parse_VA_013F64D0` | Parses bank and cash U32 fields; this listener immediately destroys the result. Confirmed. |
| `0x013F4230` | `badges_response_message_parse_VA_013F4230` | Parses player NetworkId and the badge U32 vector representation. Confirmed. |
| `0x00FA1890` | `character_sheet_refresh_badges_VA_00FA1890` | Refreshes badge presentation after the local bitmask vector is replaced. Confirmed principal behavior. |
| `0x013F5730` | `character_sheet_response_message_parse_VA_013F5730` | Registers the complete twelve-field character-sheet response in exact server order. Confirmed. |

# Group space-launch invitation names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00E00570` | `space_group_member_invite_request_VA_00E00570` | Builds generic NetworkId payload for selected member and queues inner `0x436` with flags `0x23`. Confirmed. |
| `0x01195850` | `network_id_controller_payload_serialize_VA_01195850` | Serializes the generic NetworkId payload shared by `0x436/0x437` and other types. Confirmed. |
| `0x01195890` | `network_id_controller_payload_parse_VA_01195890` | Parses one NetworkId and constructs the generic queue object; trailing bytes in `0x437` are not consumed. Confirmed. |
| `0x00652EE0` | `space_launch_invite_present_consent_VA_00652EE0` | Resolves/retains pilot ID, formats the invite prompt and creates a consent callback; replaces a prior prompt. Confirmed principal behavior. |
| `0x00652720` | `space_launch_invite_consent_callback_VA_00652720` | Validates the active prompt token and routes its boolean answer to the response sender. Confirmed. |
| `0x00652630` | `space_launch_invite_response_send_VA_00652630` | Builds ID+U8 payload and queues inner `0x438` with flags `0x23`. Confirmed. |
| `0x01196EB0` | `network_id_bool_controller_payload_serialize_VA_01196EB0` | Serializes NetworkId then one-byte decision for `0x438/0x439`. Confirmed. |
| `0x01196EE0` | `network_id_bool_controller_payload_parse_VA_01196EE0` | Parses NetworkId and one byte; trailing pilot ID in server `0x439` remains unread. Confirmed. |
| `0x0062F8D0` | `space_launch_invite_response_publish_VA_0062F8D0` | Publishes member NetworkId plus decision to registered launch consumers. Confirmed principal behavior. |

# Group loot-lottery names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00F121F0` | `swg_cui_group_loot_lottery_submit_action_VA_00F121F0` | Handles the three all/selection/none submission modes, builds the selected item-ID vector and sends inner `0x43F`. Confirmed principal behavior. |
| `0x00F12850` | `loot_lottery_payload_copy_construct_VA_00F12850` | Copies container NetworkId and vector of selected NetworkIds into the queue payload. Confirmed. |
| `0x01196FE0` | `loot_lottery_payload_serialize_VA_01196FE0` | Writes NetworkId, U32 count and each selected U64 NetworkId. Confirmed. |
| `0x01917314` | `group_loot_lottery_open_transceiver` | Receives container ID from player-controller `0x43C` branch and drives window opening. Confirmed role; data global rather than function. |
| `0x01917308` | `group_loot_lottery_close_transceiver` | Receives container ID from player-controller `0x43D` branch and drives matching window closure. Confirmed role; data global rather than function. |

# Image-design session names added 2026-09-12

| Address | Provisional name | Evidence/confidence |
|---|---|---|
| `0x00B11260` | `image_design_start_payload_parse_VA_00B11260` | Parses three NetworkIds and ASCII holo-emote for inner `0x23A`. Confirmed. |
| `0x00B0B620` | `image_design_session_payload_serialize_VA_00B0B620` | Serializes the identity/type prefix and complete shared image-design data record for `0x238/0x239`. Confirmed. |
| `0x00B0B990` | `image_design_session_payload_parse_VA_00B0B990` | Parses the matching variable-length record and builds its queue object. Confirmed. |
| `0x0121FE30` | `shared_image_designer_session_copy_VA_0121FE30` | Copies IDs, flags, strings, maps and scalar proposal state into the event payload. Confirmed. |
| `0x0062ECB0` | `image_designer_reject_publish_VA_0062ECB0` | Receives the copied shared session for inner `0x239` and publishes/tears down rejected state. Confirmed principal behavior. |
# Customization-manager table closure (2026-09-12)

The following names are address-stable and behaviorally closed. Detailed
evidence is in `ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md`.

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x00B54940` | `customization_id_manager_read_form_VA_00B54940` | Confirmed CIDM form/version gate |
| `0x00B54A00` | `customization_id_manager_parse_v1_VA_00B54A00` | Confirmed sequential ID/name loader and validation |
| `0x00B54CC0` | `customization_id_manager_install_VA_00B54CC0` | Confirmed resource install/cleanup registration |
| `0x01240CD0` | `asset_customization_manager_read_form_VA_01240CD0` | Confirmed ACST form/version gate |
| `0x01240D90` | `asset_customization_manager_parse_v0_tables_VA_01240D90` | Confirmed twelve-table loader and record widths |
| `0x012412B0` | `asset_customization_find_root_key_by_crc_VA_012412B0` | Confirmed CIDX binary lookup |
| `0x01241330` | `asset_customization_expand_key_recursive_VA_01241330` | Confirmed direct/inherited typed-variable expansion |
| `0x01241600` | `asset_customization_manager_install_VA_01241600` | Confirmed resource install/cleanup registration |

# Palette resource closure (2026-09-12)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x00AB6910` | `palette_cache_fetch_VA_00AB6910` | Confirmed name cache, TreeFile validation, and cyan 0xFF00FFFF fallback |
| `0x00AB7B00` | `palette_argb_list_get_count_VA_00AB7B00` | Confirmed |
| `0x00AB7B10` | `palette_argb_list_get_const_clamped_VA_00AB7B10` | Confirmed invalid-index clamp to zero |
| `0x00AB7B40` | `palette_argb_list_get_mutable_clamped_VA_00AB7B40` | Confirmed diagnostic and clamp |
| `0x00AB7C50` | `palette_argb_list_find_closest_argb_VA_00AB7C50` | Confirmed squared-distance search |
| `0x00AB7D30` | `palette_argb_list_construct_from_name_VA_00AB7D30` | Confirmed TreeFile-owned load path |
| `0x00AB7E60` | `palette_argb_list_parse_riff_VA_00AB7E60` | Confirmed RIFF validation and import |

# Palette-column policy closure (2026-09-12)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x012191A0` | `customization_palette_policy_copy_by_key_VA_012191A0` | Confirmed 15-field row copy |
| `0x012191D0` | `customization_palette_policy_get_or_default_VA_012191D0` | Strong; named lookup and default insertion/copy |
| `0x012216A0` | `palette_policy_select_tier_pair_VA_012216A0` | Confirmed tier-to-columns/exclusive-limit selection |

# Customization presentation-data closure (2026-09-12)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x01218290` | `customization_find_species_gender_VA_01218290` | Confirmed hierarchy lookup used by creation/image-design setup |

The large installer containing `0x01216600`–`0x01217848` has a missing Ghidra
function boundary. Its table behavior is nevertheless closed and address-
anchored; this is a disassembler inventory defect, not an unknown client rule.
## Customization group-instance and slot-rule closure (2026-09-12)

The following addresses are behavior-resolved provisional names, not open
unknowns. Full evidence is in
`ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_GROUPS_SLOT_RULES.md`.

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x012182D0` | `customization_find_group_instance` | Strong; two-key table lookup returning the six-field group payload |
| `0x00598C00` | `slot_rule_manager_read_form` | Confirmed |
| `0x00598CC0` | `slot_rule_manager_parse_v0_bindings` | Confirmed |
| `0x00598F30` | `slot_rule_manager_install` | Confirmed |
| `0x00598FC0` | `slot_rule_manager_get_binding_count` | Confirmed |
| `0x00598FD0` | `slot_rule_manager_get_binding` | Confirmed |
| `0x00598FF0` | `slot_rule_apply_binding_to_customization` | Strong; inserts the binding variable and evaluable rule wrapper |
| `0x00599410` | `slot_occupied_rule_evaluate` | Confirmed |
| `0x00599910` | `slot_rule_not_evaluate` | Confirmed |
| `0x00599D20` | `slot_rule_and_evaluate` | Confirmed |
| `0x0059A170` | `slot_rule_or_evaluate` | Confirmed |
| `0x0059A410` | `slot_rule_parse_polymorphic` | Confirmed tag-to-factory dispatcher |
| `0x0063F6C0` | `object_apply_slot_derived_customization_rules` | Strong; sole manager consumer and live object application loop |
## Appearance corpus/version closure and newly isolated optional paths (2026-09-12)

The 38,440-resource effective census confirms the deployed version families
and removes the earlier broad "expand structural coverage" unknown. These are
stable behavior-resolved addresses:

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x007DB3A0` | `skeletal_appearance_template_load_v3` | Confirmed mandatory and optional form reader |
| `0x007D9E70` | `skeletal_appearance_template_get_sfsk_policy` | Confirmed; policy skips intermediate detail index 1 beyond near range |
| `0x007D9E80` | `skeletal_appearance_template_get_apag_policy` | Confirmed; policy bypasses the periodic action-generator age gate |
| `0x0081BAB0` | `keyframe_uncompressed_quaternion_channel_load` | Confirmed U32-count, uncompressed quaternion channel reader |
| `0x00824870` | `compressed_keyframe_quaternion_channel_load` | Confirmed U16-count, three-byte quantizer selectors, U16-frame/U32-packed-key grammar; 11/11/10 signed component split and fourth-component reconstruction mapped |

The QCHN field layout and SFSK/APAG branch semantics are now explicitly
isolated Class-C items in `FUNCTION_UNCERTAINTY_AUDIT.md`; they do not imply
that the five functions above are wholly unknown.

`0x013A4910` is now `lod_distance_table_load_v0`: it reads U16 count plus
float32 min/max pairs and stores squared endpoints. `0x013A4780` is
`lod_distance_table_select_with_hysteresis`: it clamps the current index,
tests scaled squared distance against that interval, and scans down/up only
when leaving it. Both are Confirmed behavior-resolved functions.
## MLOD mesh-generator container closure (2026-09-12)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x007F8E10` | `lod_mesh_generator_template_install` | Confirmed MLOD registration and skipL0 configuration |
| `0x007F8EE0` | `lod_mesh_generator_template_get_exposed_level_count` | Confirmed `min(nameCount, 4)` |
| `0x007F8F10` | `lod_mesh_generator_template_fetch_level` | Confirmed lazy child fetch/cache and caller reference acquisition |
| `0x007F8F50` | `lod_mesh_generator_template_preload` | Confirmed exposed-level preload loop |
| `0x007F8FA0` | `lod_mesh_generator_template_release_cached_levels` | Confirmed cache release/null loop |
| `0x007F8FF0` | `lod_mesh_generator_template_factory` | Confirmed registered factory |
| `0x007F9050` | `lod_mesh_generator_template_ctor` | Confirmed constructor/version dispatch |
| `0x007F9290` | `lod_mesh_generator_template_load_v0` | Confirmed INFO signed-16 count plus repeated NAME reader |

The higher-level consumer combining skeletal detail selection with an MLOD
child is now closed by the 2026-09-22 composition trace. The format, lifetime,
selected-child bridge and availability fallback are no longer unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_LOD_COMPOSITION.md`.

| Address | Stable provisional name | Role | Confidence |
|---|---|---|---|
| `0x007C8B60` | `skeletal_appearance_update_and_select_detail_VA_007C8B60` | camera-distance detail selection and presentation update | Confirmed |
| `0x007CA9B0` | `skeletal_populate_detail_meshes_VA_007CA9B0` | SAT MSGN through common/MLOD level fetch into generated meshes | Confirmed |
| `0x007CAB40` | `skeletal_get_or_build_detail_meshes_VA_007CAB40` | lazy per-detail mesh-vector accessor | Confirmed |
| `0x007CAB70` | `skeletal_detail_dependencies_available_VA_007CAB70` | generated/dependent appearance availability check | Confirmed |
| `0x007CAC60` | `skeletal_detail_meshes_available_VA_007CAC60` | all-mesh availability test for one detail | Confirmed |
| `0x007CB240` | `skeletal_select_detail_index_VA_007CB240` | interval/hint/cap detail selector | Confirmed |
| `0x007CBBD0` | `skeletal_find_available_detail_VA_007CBBD0` | neighboring-index usable-detail fallback | Confirmed |
## SLOD skeleton container closure (2026-09-12)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x00829EB0` | `lod_skeleton_template_install` | Confirmed registration and skipL0 configuration |
| `0x00829F30` | `lod_skeleton_template_get_exposed_level_count` | Confirmed `min(childCount, 4)` |
| `0x00829F60` | `lod_skeleton_template_fetch_level` | Confirmed indexed return with acquired reference |
| `0x00829FA0` | `lod_skeleton_template_factory` | Confirmed factory |
| `0x0082A000` | `lod_skeleton_template_ctor` | Confirmed constructor/version dispatch |
| `0x0082A1D0` | `lod_skeleton_template_load_v0` | Confirmed count and recursive inline child loading |

SLOD serialization, ownership, exposed-level cap, and skip-L0 behavior are
resolved. The shared higher-level LDTB-to-detail consumer remains targeted.

## Auction detail request/response closure (2026-09-13)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x004CFB10` | `auction_request_details_VA_004CFB10` | Confirmed constructs/sends `GetAuctionDetails` for one U64 listing ID |
| `0x00ACFCC0` | `get_auction_details_request_ctor_VA_00ACFCC0` | Confirmed request archive constructor and sole NetworkId/U64 field |
| `0x006A8010` | `auction_message_listener_receive_VA_006A8010` | Confirmed `GetAuctionDetailsResponse` dispatch branch among registered auction responses |
| `0x00AE7B60` | `get_auction_details_response_parse_VA_00AE7B60` | Confirmed full response archive parser |
| `0x006A88E0` | `auction_details_archive_to_record_VA_006A88E0` | Confirmed converts archive wrappers to `AuctionManagerClientData` value fields |
| `0x004CD5B0` | `auction_details_cache_apply_publish_VA_004CD5B0` | Confirmed caches details, updates matching list records and publishes `DetailsReceived` |

The obsolete server wrapper names are no longer an uncertainty. Individual UI widget population after the typed `DetailsReceived` signal is outside this protocol-route closure and remains part of later detailed auction-UI reconstruction.

## Auction listing-creation response closure (2026-09-13)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x00AE8650` | `create_auction_response_parse` | Confirmed archive parser for U64 ID plus U32 status |
| `0x006A88B0` | `create_auction_response_extract_result` | Confirmed conversion of parsed response members into the handler value record |
| `0x004CD7D0` | `auction_create_response_apply_publish` | Confirmed pending-request removal, result dispatch/localization and typed `CreateAuctionResponse` publication |

The enclosing auction listener is already tracked as `auction_message_listener_receive_VA_006A8010`; `0x006A8291` is an internal branch, not a separately inventoried function. The exact localization resource selected for every legacy status remains presentation-level work. It does not leave the message identity, schema, request pairing, or principal state effect unknown.

### Auction action-response closure (2026-09-14)

The three high-use auction response gaps are now resolved through the same listener:

| Address | Provisional identity | Confidence |
|---:|---|---|
| `0x00AE8CA0` | `bid_auction_response_parse_VA_00AE8CA0` | Confirmed |
| `0x004CDED0` | `auction_manager_receive_bid_response_VA_004CDED0` | Confirmed |
| `0x00AE7320` | `retrieve_auction_item_response_parse_VA_00AE7320` | Confirmed |
| `0x004CEB20` | `auction_manager_receive_retrieve_response_VA_004CEB20` | Confirmed |
| `0x00AE8970` | `cancel_live_auction_response_parse_VA_00AE8970` | Confirmed |
| `0x004CED60` | `auction_manager_receive_cancel_response_VA_004CED60` | Confirmed |

The cancellation parser consumes a third U8 payload member after ID and result. Its semantic meaning remains unknown, but the active Stardust producer always writes zero and the model handler ignores it. This is a wire-compatibility unknown, not an unresolved handler.

### Auction query-header closure (2026-09-14)

`AuctionQueryHeadersResponseMessage` is no longer an unknown packet family. Parser `0x00AE9090`, raw 72-byte record decoder `0x00AE9D80`, indexed-string resolver `0x00AE9B40`, screen wrappers `0x004CCED0..0x004CCFB0`, and shared list applicator `0x004CCFD0` are assigned stable identities. The 72-byte compact and 112-byte resolved record sizes describe x86 runtime objects only. Unknown auction-option bits remain data-domain unknowns and must be preserved without invented meanings.

### Six chat-family closures (2026-09-15)

Six active gaps now have stable listener/parser/handler identities: friend-status update, room message, room list, remove-moderator result, instant-message acknowledgement, and incoming instant message. The trailing dwords in `ChatRoomMessage` and `ChatInstantMessageToClient` remain semantically unknown; Stardust sends zero and the client consumes them. They are compatibility fields, not unknown handlers.

## GameSceneChangedMessage negative closure (2026-09-13)

No provisional executable function is assigned for server opcode `0xBBADAEB9`. This is intentional, not an unresolved address: the server class has no constructor use, and Stardust.exe contains neither the opcode's little-endian bytes nor the class identity/parser evidence required for a normal message route. RTTI containing `Game::Messages::SceneChanged` describes internal `MessageDispatch` transceivers and must not be renamed as a network handler without a demonstrated bridge.

Reopen this item only if a future server revision emits the opcode, another authoritative client build contains it, or a controlled trace demonstrates an alternate numeric dispatch mechanism. For the current executable/server pair its stable disposition is `dormant_server_packet_no_client_route`.

## ConnectionServerTestMessage negative closure (2026-09-13)

No provisional executable function is assigned for `0x0E20D7E9`. Stardust.exe contains zero occurrences of the opcode bytes, `ConnectionServerTestMessage`, or `ConnectionServer:1348`. The current server has no constructor call. Local history proves the sole producer was a five-day-lived 2008 `GameCommandHandler::sendp` developer test branch, after which the header was orphaned.

Stable disposition: `obsolete_developer_test_packet_no_client_route`. Do not merge this artifact with `ConnectionServerConnectionOpened`, `ClientIdMsg`, `LagRequest`, `ConnectionServerLagResponse`, or `ConnectPlayerResponse`; each of those has separately recovered executable behavior.

## HeartBeat / LinkDeadMessage collision closure (2026-09-13)

No provisional executable function is assigned for `0xA16CF9AF`. Exact hashing establishes `HeartBeat` as the canonical identity. Stardust.exe has no opcode bytes, name string, parser or dispatcher route, and neither server class has a current producer. Historical source sends `HeartBeat` only in the old character-creation path; there is no historical construction evidence for `LinkDeadMessage`.

Stable dispositions:

- `HeartBeat`: `obsolete_character_creation_signal_no_current_client_route`;
- `LinkDeadMessage`: `invalid_duplicate_alias_incomplete_stub`.

The operation count 5 in `LinkDeadMessage` must not be interpreted as evidence of four missing fields. Link-dead player state belongs to connection/session lifecycle behavior, not an invented application-message handler.

## PEFT top-level loader and runtime bridge (2026-09-13)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x007A1100` | `setup_client_particle_install` | Confirmed particle subsystem installation sequence |
| `0x007B6DB0` | `particle_effect_appearance_template_install` | Confirmed `PEFT` appearance-factory registration |
| `0x007B6D50` | `particle_effect_appearance_template_factory` | Confirmed 0x24-byte template allocation and construction |
| `0x007B6C20` | `particle_effect_appearance_template_ctor_load` | Confirmed base-template initialization, description allocation and load delegation |
| `0x007C2DA0` | `particle_effect_description_ctor` | Confirmed defaults, timing member and owned emitter-group vector initialization |
| `0x007C3990` | `particle_effect_description_load` | Confirmed top-level version dispatcher for 0000-0002 |
| `0x007C3B10` | `particle_effect_description_load_v0000` | Confirmed group-count and ordered EMGP load |
| `0x007C3D10` | `particle_effect_description_load_v0001` | Confirmed PTIM plus group-count/EMGP load |
| `0x007C3F20` | `particle_effect_description_load_v0002` | Confirmed PTIM, group count, four F32 controls and EMGP load |
| `0x007C5460` | `particle_emitter_group_description_load` | Confirmed recursive EMGP version loader boundary |
| `0x007A8500` | `particle_effect_appearance_install` | Confirmed runtime appearance class/pool installation |
| `0x007A85A0` | `particle_effect_appearance_ctor_materialize_groups` | Confirmed one runtime group per decoded group description |

The four PEFT/0002 floats are closed by runtime consumers. `+0x28/+0x2C/+0x30` are initial time scale, transition time and sustaining time scale; `+0x34` is overall spatial effect scale. Nested effects recursively multiply parent and child scaling.

## EMGP/EMTR version and materialization boundary (2026-09-13)

| Address | Stable provisional name | Status |
|---:|---|---|
| `0x007C4BD0` | `particle_emitter_group_description_ctor` | Confirmed PTIM state plus owned emitter-description vector initialization |
| `0x007C5460` | `particle_emitter_group_description_load` | Confirmed EMGP 0000/0001 dispatcher |
| `0x007C5540` | `particle_emitter_group_description_load_v0000` | Confirmed emitter count and repeated EMTR descriptions |
| `0x007C5770` | `particle_emitter_group_description_load_v0001` | Confirmed PTIM load followed by count and repeated EMTR descriptions |
| `0x007B9B90` | `particle_emitter_description_load` | Confirmed EMTR 0000-0014 exhaustive dispatcher |
| `0x007B6030` | `particle_emitter_group_ctor_materialize_emitters` | Confirmed one runtime emitter per ordered description plus parent/timing retention |

Fifteen version-specific emitter loaders begin at `0x007B9D70`, `0x007BA010`, `0x007BA2B0`, `0x007BA560`, `0x007BA860`, `0x007BAB70`, `0x007BAEF0`, `0x007BB290`, `0x007BB6F0`, `0x007BBB70`, `0x007BC030`, `0x007BC5A0`, `0x007BCB20`, `0x007BD140`, and `0x007BD780`. They remain stable child targets for subsequent field-differencing; this block confirms their version mapping without prematurely assigning all serialized fields.

## EMTR modern fields and factories (2026-09-13)

| Address | Provisional name | Status |
|---|---|---|
| `0x007C5D30` | `particle_emitter_shape_factory` | Confirmed values 0-5: circle, sphere, rectangle, cube, line, X |
| `0x007BE8C0` | `particle_description_factory` | Confirmed 0 = quad, 1 = mesh; other values null |
| `0x007A6980` | `particle_emitter_advance_generation_accumulator` | Confirmed generation accumulation, capacity/burst gating, and 0013 `+0x228` policy consumption |
| `0x007A3990` | `particle_emitter_initialize_spawned_particle` | Strongly identified; initializes particle transforms and applies optional 0012 surface query/offset/alignment |
| `0x007A4840` | `particle_emitter_simulate_particles` | Strongly identified; consumes 0012 random-direction range/period fields among broader per-particle update work |

Modern versions are structurally closed. A corrected SWG-IFF walk establishes 6,373 effective emitters (rather than the earlier padded-walk undercount); their modern-version counts are 78/28/696/1,039/841/3,682 for 0009-0014. The `+0x229` byte added by 0014 is consumed at `0x007A4373`: it selects normal versus reserve/expanded particle-pool admission pressure through `0x007B5990`. It is false in 3,127 and true in 555 deployed 0014 emitters.

Common field `+0x114` enables random initialization of three per-particle scalar seeds. Field `+0x118` is a three-way orientation/reference-frame mode with distinct spawn and update math; serialized legacy value 3 normalizes to mode 2. Field `+0x11C` remains a compatibility Boolean: it is true in all 6,364 modern emitters and has no direct runtime read outside initialization/copy/load/save paths.

## EMTR waveform-channel consumers (2026-09-13)

| Address | Provisional name | Status |
|---|---|---|
| `0x007A2E10` | `particle_emitter_update_authored_transform` | Confirmed evaluation of translation channels 0-2 and ordered rotation channels 3-5 |
| `0x007A3990` | `particle_emitter_initialize_spawned_particle` | Confirmed channels 6-8/10/14/15 feed shape/spread, speed, lifetime and gravity state |
| `0x007A6980` | `particle_emitter_advance_generation_accumulator` | Confirmed channel 9 generation-rate integration and repeated spawn dispatch |
| `0x007A4840` | `particle_emitter_simulate_particles` | Confirmed particle age/lifetime normalization, gravity/motion integration and nested visual waveform evaluation |

Channels at `+0x150`, `+0x168` and `+0x180` are closed as dormant-zero fields for the deployed corpus. No executable-wide direct displacement read was found, their reset helpers initialize zero-valued random-key curves, and every key in all 6,373 records has zero non-time lanes under both WVFM/0001 and /0002. Their historical editor labels remain unknown, but their required runtime behavior does not.

| Address | Provisional name | Status |
|---:|---|---|
| `0x007B9860` | `particle_emitter_description_initialize_reserved_waveform_11` | Confirmed zero-bounded random-key initialization |
| `0x007B9880` | `particle_emitter_description_initialize_reserved_waveform_12` | Confirmed zero-bounded initialization plus zero lower clamp |
| `0x007B98B0` | `particle_emitter_description_initialize_reserved_waveform_13` | Confirmed zero-bounded initialization plus zero lower clamp |
| `0x00ABF420` | `waveform_initialize_random_keys_between_bounds` | Confirmed reset, randomized key count/time generation, bounded key values |
| `0x00ABFAA0` | `waveform_load` | Confirmed WVFM 0000/0001/0002 dispatcher |

## Particle timing, spawn admission and legacy conversion closure (2026-09-13)

| Address | Provisional name | Status |
|---:|---|---|
| `0x007A4350` | `particle_emitter_spawn_quad_particle` | Confirmed +0x229 admission check, allocation, initialization and simulation handoff |
| `0x007B5870` | `particle_pool_install` | Confirmed global 0xA8-record, 0x200-block pool setup |
| `0x007B5960` | `particle_pool_allocate` | Confirmed allocator wrapper |
| `0x007B5970` | `particle_pool_release` | Confirmed release wrapper |
| `0x007B5990` | `particle_pool_reject_spawn_at_selected_limit` | Confirmed normal/reserve threshold selection |
| `0x007A8AB0` | `particle_effect_appearance_alter` | Strong lifecycle/update/extent role |
| `0x007A9060` | `particle_effect_appearance_compute_effective_time_scale` | Confirmed initial/transition/sustaining and parent recursion |
| `0x007A90D0` | `particle_effect_appearance_update_effective_spatial_scale` | Confirmed PEFT +0x34 and parent-scale composition |
| `0x007B9D70` | `particle_emitter_description_load_v0000` | Confirmed oldest deployed emitter conversion |
| `0x007BAEF0` | `particle_emitter_description_load_v0006` | Confirmed transitional deployed emitter conversion |

## Chat room creation-result closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B045C0` | `chat_room_record_decode` | Confirmed shared room-record nested codec |
| `0x00B07FD0` | `chat_create_room_result_parse` | Confirmed result/room/request-ID parser |
| `0x00A2CF80` | `chat_room_manager_apply_create_room_result` | Confirmed request correlation, success insertion, failure presentation and silent result-24 policy |

## Chat room destruction closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B07490` | `chat_destroy_room_result_parse` | Confirmed ChatAvatarId/result/room-ID/request-ID parser |
| `0x00A2B100` | `chat_room_manager_apply_destroy_room` | Confirmed authoritative removal, tree pruning, diagnostics and correlated failure presentation |
| `0x00A283E0` | `chat_room_manager_find_room_by_id` | Confirmed room-ID lookup role in destroy/create-related lifecycle paths |
| `0x00A2AF90` | `chat_room_manager_remove_room` | Strong retained room-registry removal role |

## Chat room unban closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B04C90` | `chat_unban_avatar_from_room_parse` | Confirmed room-path/two-avatar/result/request parser |
| `0x00A2CC90` | `chat_room_manager_apply_unban_result` | Confirmed result mapping, success mutation, tree refresh and localized publication |
| `0x00A285E0` | `chat_room_manager_find_room_by_path` | Confirmed full-path room lookup in unban and related room operations |
| `0x00A6A4A0` | `chat_room_set_banned_membership` | Confirmed add/remove banned-set mutation: ban passes true, unban passes false |

## Add chat-room moderator closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B06FD0` | `chat_add_moderator_result_parse` | Confirmed two-avatar/result/path/request parser |
| `0x00A2C3A0` | `chat_room_manager_apply_add_moderator_result` | Confirmed request correlation, success mutation, tree refresh and failure presentation |
| `0x00A283A0` | `chat_room_manager_find_room_by_full_path` | Confirmed path lookup used before add-moderator mutation |
| `0x00A6A480` | `chat_room_set_moderator_membership` | Confirmed add=true moderator-set mutation; paired removal behavior remains represented by related paths |

## Chat room ban closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B05160` | `chat_ban_avatar_from_room_parse` | Confirmed path/two-avatar/result/request parser |
| `0x00A2C9A0` | `chat_room_manager_apply_ban_result` | Confirmed successful banned-set insertion, tree/event refresh and localized result publication |
| `0x00A6A4A0` | `chat_room_set_banned_membership` | Upgraded to confirmed bidirectional setter by paired ban/unban call sites |

## Entered chat-room closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B08D60` | `chat_entered_room_parse` | Confirmed avatar/result/room-ID/request-ID parser |
| `0x00A2BAF0` | `chat_room_manager_apply_enter_success` | Confirmed member insertion, self/other split, active-room/observer processing and tree refresh; special system-room subhelpers remain separately bounded |
| `0x00A2D1D0` | `chat_room_manager_apply_enter_failure` | Confirmed request correlation and join-result localization/publication |
| `0x00A6A440` | `chat_room_set_member_membership` | Confirmed bidirectional room member-set mutation by paired entered/leave paths |

## Leave chat-room closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B08950` | `chat_leave_room_parse` | Confirmed avatar/result/room-ID/request-ID parser |
| `0x00A2C040` | `chat_room_manager_apply_leave` | Confirmed room lookup, member removal, self/other split, active-channel teardown and tree refresh |
| `0x00A6A440` | `chat_room_set_member_membership` | Upgraded to confirmed bidirectional member setter: entered passes true, leave passes false |

## Chat room query-results closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00AFF190` | `chat_query_room_results_parse` | Confirmed four identity vectors, shared room record and request-ID parser |
| `0x00A2B520` | `chat_room_manager_apply_query_results` | Confirmed room merge, authoritative set replacement, registry/tree reconciliation and status publication |
| `0x00A6A660` | `chat_room_replace_members` | Confirmed top-level member snapshot destination |
| `0x00A6A680` | `chat_room_replace_invitees` | Confirmed top-level invitee snapshot destination |
| `0x00A6A6A0` | `chat_room_replace_moderators` | Confirmed top-level moderator snapshot destination |
| `0x00A6A6C0` | `chat_room_replace_banned` | Confirmed top-level banned snapshot destination |

## Chat room invite-result closure (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00B05EA0` | `chat_invite_to_room_result_parse` | Confirmed actor/target/path/result/request parser |
| `0x00A2DB50` | `chat_room_manager_apply_invite_result` | Confirmed pending correlation, success invitee insertion, tree/event refresh and localized result publication |
| `0x00A6FD90` | `chat_room_pending_invite_resolve` | Strong request-ID to retained invite context resolution role |
| `0x00A6A460` | `chat_room_set_invited_membership` | Confirmed bidirectional invited-set mutation: invite paths pass add=true; uninvite result passes add=false |
| `0x00B06600` | `chat_receive_room_invitation_parse` | Confirmed `ChatOnReceiveRoomInvitation` archive construction/decode; inviter ChatAvatarId and room-path fields |
| `0x00A2D3B0` | `chat_room_manager_receive_invitation` | Confirmed target-facing invitation handler; room precondition, local invitee insertion, typed event/tree refresh and prose publication |
| `0x009E8F90` | `cui_chat_get_local_avatar_id` | Confirmed semantic role in invitation handler: produces normalized local identity that is inserted as the invitee; internal identity-source mechanics remain below this block's boundary |
| `0x00A2E510` | `chat_room_manager_set_group_room_id` | Confirmed retained group-room ID setter and `GroupRoomIdChanged` publisher by RTTI |
| `0x00A2E7B0` | `chat_room_manager_set_guild_room_id` | Confirmed retained guild-room ID setter and `GuildRoomIdChanged` publisher by RTTI |
| `0x00B059D0` | `chat_uninvite_from_room_result_parse` | Confirmed five-field response parser: path, actor, target, result and request ID |
| `0x00A2DEC0` | `chat_room_manager_apply_uninvite_result` | Confirmed correlated result handler; success removes cached invitee, emits `InviteeRemoved`, refreshes tree and publishes prose |
| `0x00A6FF10` | `chat_uninvite_pending_resolve` | Confirmed request-ID lookup wrapper for dedicated pending table `0x0193B128` |
| `0x00AFC990` | `chat_uninvite_from_room_request_construct` | Confirmed outbound message constructor/field registration for target ChatAvatarId, room path and request ID |
| `0x00A289A0` | `chat_room_manager_send_to_room` | Confirmed send owner: validate room, allocate U32 sequence, retain room path, construct request and reliable-send |
| `0x00AFB960` | `chat_send_to_room_request_construct` | Confirmed four-field outbound constructor: message Unicode, reserved Unicode, room ID and sequence |
| `0x00B062B0` | `chat_send_room_result_parse` | Confirmed acknowledgement parser for U32 result and U32 sequence |
| `0x00A6FA60` | `chat_send_room_pending_insert` | Confirmed sequence-to-room-path insertion/update wrapper for table `0x0193B140` |
| `0x00A6FC10` | `chat_send_room_pending_resolve` | Confirmed sequence lookup/consumption wrapper for table `0x0193B140` |
| `0x00A2D9E0` | `chat_room_manager_apply_send_result` | Confirmed acknowledgement handler: silent success; localized result 9, result 16 and fallback failures |
| `0x005AA370` | `community_manager_build_friend_data_path` | Confirmed per-profile `*_friend_data.iff` path derivation; local presentation metadata, not the legacy network snapshot parser |
| `0x005AA7F0` | `community_manager_script_get_friend_list` | Confirmed script-facing `getFriendList` dispatch wrapper |
| `0x005AA850` | `community_manager_script_remove_friend` | Confirmed script-facing `removeFriend` dispatch wrapper |
| `0x005AA880` | `community_manager_script_add_friend` | Confirmed script-facing `addFriend` dispatch wrapper |
| `0x005AA820` | `community_manager_script_get_ignore_list` | Confirmed script-facing `getIgnoreList` dispatch wrapper |
| `0x005AA8B0` | `community_manager_script_remove_ignore` | Confirmed script-facing `removeIgnore` dispatch wrapper |
| `0x005AA8E0` | `community_manager_script_add_ignore` | Confirmed script-facing `addIgnore` dispatch wrapper |
| `0x00B0A600` | `chat_on_connect_avatar_parse` | Confirmed zero-payload archive constructor/parser for `ChatOnConnectAvatar` |
| `0x005AB170` | `community_manager_mark_chat_connected` | Confirmed listener destination; sets retained social-refresh-pending byte |
| `0x005AB180` | `community_manager_update` | Confirmed deferred update gate consuming chat-connect and other retained refresh flags |
| `0x00E40E40` | `game_ui_network_message_listener` | Confirmed broad UI message listener containing holocron open/close branches |
| `0x013F34A0` | `open_holocron_to_page_parse` | Confirmed archive constructor/parser for page string and reserved field envelope |
| `0x013F35C0` | `open_holocron_to_page_get_page` | Confirmed accessor returning page string member |
| `0x00AD3C60` | `newbie_tutorial_request_parse` | Confirmed request archive constructor/parser for ASCII action key |
| `0x00AD3E30` | `newbie_tutorial_request_get_action` | Confirmed action-key accessor |
| `0x00AD2390` | `newbie_tutorial_response_construct` | Confirmed client response constructor used by distributed action-event callbacks |
| `0x00F38C20` | `chat_ui_network_listener` | Confirmed listener containing `ConGenericMessage` decode/presentation branch |
| `0x013F4E70` | `con_generic_message_parse` | Confirmed parser for ASCII text and trailing U32 field |
| `0x00F3BFD0` | `chat_ui_present_generic_message` | Confirmed final Unicode chat-presentation wrapper used by `ConGenericMessage` |
| `0x0055E050` | `input_activity_manager_install` | Confirmed inactivity timer configuration and callback installation |
| `0x0055E0F0` | `input_activity_manager_remove` | Confirmed timer removal and installed-state clear |
| `0x0055E110` | `client_inactivity_message_send` | Confirmed guarded construction and reliable send of one-byte inactivity message |
| `0x00FA1280` | `player_resource_queries_send` | Confirmed sequential reliable send of stomach and money requests |
| `0x013F6A30` | `stomach_request_construct` | Confirmed payload-free named request constructor |
| `0x013F6840` | `player_money_request_construct` | Confirmed payload-free named request constructor |
| `0x013F64D0` | `player_money_response_parse` | Confirmed bank/cash two-U32 response parser; listener discards values in this build |
| `0x00A19580` | `customer_service_connect_test_response_handler` | Previously named function; now additionally confirmed as shared customer-service listener containing cancel/article/activity branches |
| `0x00AFA0D0` | `cancel_ticket_response_parse` | Confirmed result and ticket-ID parser |
| `0x00AF7BB0` | `get_article_response_parse` | Confirmed result and Unicode article parser |
| `0x00AF7880` | `new_ticket_activity_response_parse` | Confirmed flag and ticket-ID parser |
| `0x005CB9E0` | `cui_cs_apply_cancel_ticket_response` | Confirmed success/ticket-ID semantic destination |
| `0x005CA640` | `cui_cs_apply_new_ticket_activity` | Confirmed flag/ticket-ID semantic destination |
| `0x00AFA650` | `create_ticket_response_parse` | Confirmed two-U32 client response parser; exposes server constructor mismatch |
| `0x00AF9860` | `get_tickets_response_parse` | Confirmed two-U32 header plus ticket-record vector parser |
| `0x005CA660` | `cui_cs_apply_get_tickets_response` | Confirmed success/full-message semantic destination |
# Permission-list UI closure (2026-09-15)

- `0x0119B130` — `permission_list_create_parse_VA_0119B130` (confirmed archive parser for opcode `0x52F364B8`).
- `0x0119B470` — `permission_list_create_primary_names_VA_0119B470` (confirmed first Unicode-vector accessor).
- `0x0119B480` — `permission_list_create_secondary_names_VA_0119B480` (confirmed second Unicode-vector accessor).
- `0x0119B490` — `permission_list_create_list_name_VA_0119B490` (confirmed Unicode list-name accessor).
- `0x00E420D0` — `game_ui_route_permission_list_VA_00E420D0` (confirmed mediator lookup/create/activate route).
- `0x00FAC7A0` — `swg_cui_permission_list_create_VA_00FAC7A0` (confirmed `/PDA.PermissionList` mediator factory).
- `0x00FAC810` — `swg_cui_permission_list_apply_message_VA_00FAC810` (confirmed two-list and title population routine).

# Stat-migration target closure (2026-09-15)

- `0x00DB96D0` — `swg_cui_stat_migration_receive_targets_VA_00DB96D0` (confirmed descriptor check, parse, model replacement and activation).
- `0x00DB9940` — `swg_cui_stat_migration_refresh_VA_00DB9940` (confirmed widget refresh after target replacement).
- `0x013F22B0` — `stat_migration_targets_parse_VA_013F22B0` (confirmed ten-S32 archive parser).
- `0x013F24C0` — `stat_migration_targets_extract_values_VA_013F24C0` (confirmed decoded-value materialization used by the mediator).

# Server-directed game-command closure (2026-09-15)

- `0x00F38C20` — `game_ui_and_console_message_listener_VA_00F38C20` (confirmed owner of the `ExecuteConsoleCommand` branch among other UI messages).
- `0x013F51F0` — `execute_console_command_parse_VA_013F51F0` (confirmed single-ASCII-field archive parser).
- `0x009141D0` — `game_console_submit_command_VA_009141D0` (strongly inferred internal game-command submission boundary; explicitly not an OS shell).

# Tutorial HUD-element closure (2026-09-15)

- `0x013F29E0` — `newbie_tutorial_hud_element_parse_VA_013F29E0` (confirmed ASCII/U8/F32 archive parser).
- `0x013F2C10` — `newbie_tutorial_hud_element_key_VA_013F2C10` (confirmed key accessor).
- `0x013F2C20` — `newbie_tutorial_hud_element_enabled_VA_013F2C20` (confirmed U8 accessor).
- `0x013F2C30` — `newbie_tutorial_hud_element_blink_count_VA_013F2C30` (confirmed F32 accessor).
- `0x00E43730` — `game_ui_configure_tutorial_blink_VA_00E43730` (confirmed blink-count application helper).

## Ship transform control names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x0061C560` | `player_ship_controller_send_transform_VA_0061C560` | Confirmed role; exact cadence caller remains bounded |
| `0x00AD6370` | `ship_update_transform_construct_VA_00AD6370` | Confirmed serializer |
| `0x00AD6930` | `ship_update_transform_parse_VA_00AD6930` | Confirmed parser |
| `0x00AD6A90` | `ship_update_transform_get_short_id_VA_00AD6A90` | Confirmed |
| `0x00AD6AA0` | `ship_update_transform_decode_transform_VA_00AD6AA0` | Confirmed |
| `0x00AD6C40` | `ship_update_transform_decode_velocity_VA_00AD6C40` | Confirmed |
| `0x00AD6D40` | `ship_update_transform_get_stamp_VA_00AD6D40` | Confirmed |
| `0x0061B230` | `player_ship_controller_receive_transform_VA_0061B230` | Confirmed local reconciliation role |
| `0x00617F70` | `remote_ship_controller_receive_transform_VA_00617F70` | Confirmed remote interpolation role |
| `0x0061B120` | `player_ship_controller_apply_collision_correction_VA_0061B120` | Confirmed role; fine smoothing semantics bounded |

## Ship damage/destruction names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x005717F0` | `ship_hit_effects_apply_VA_005717F0` | Confirmed hit type/severity presentation handler |
| `0x00618850` | `ship_controller_handle_object_controller_message_VA_00618850` | Confirmed subtype dispatcher including `0x432` |
| `0x00AD2AE0` | `destroy_ship_component_message_parse_VA_00AD2AE0` | Confirmed parser |
| `0x0053E260` | `ship_object_handle_component_destruction_VA_0053E260` | Confirmed attachment/effect role |
| `0x00AD2840` | `destroy_ship_message_parse_VA_00AD2840` | Confirmed parser |
| `0x0053DE50` | `ship_object_handle_remote_destruction_VA_0053DE50` | Confirmed principal role |
| `0x005866A0` | `space_death_sequence_construct_VA_005866A0` | Confirmed local-player death sequence; internal phase fields bounded |

## Nebula lightning names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00AD2D80` | `create_nebula_lightning_message_parse_VA_00AD2D80` | Confirmed parser |
| `0x00AD2E80` | `create_nebula_lightning_message_get_data_VA_00AD2E80` | Confirmed aligned-record accessor |
| `0x0052F640` | `nebula_manager_enqueue_lightning_VA_0052F640` | Confirmed validation/queue boundary |
| `0x0052F820` | `nebula_manager_update_lightning_VA_0052F820` | Confirmed timed update; fine constants bounded |

## Ordinary `DataTransform` names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00AD4790` | `message_queue_data_transform_construct_VA_00AD4790` | Confirmed payload construction |
| `0x00AD4810` | `message_queue_data_transform_pack_VA_00AD4810` | Confirmed serializer |
| `0x00AD4860` | `message_queue_data_transform_unpack_VA_00AD4860` | Confirmed parser and NaN fallback |
| `0x00B4B000` | `creature_controller_dispatch_message_queue_VA_00B4B000` | Confirmed `0x71`/`0xF1` dispatch role |
| `0x0062C980` | `player_creature_controller_handle_data_transform_VA_0062C980` | Confirmed local correction/mount-redirection role |
| `0x00637D30` | `remote_creature_controller_handle_data_transform_VA_00637D30` | Confirmed freshness and target-install role |
| `0x00637270` | `remote_creature_controller_set_transform_target_VA_00637270` | Confirmed cell/transform/scalar storage |
| `0x006E4E50` | `creature_controller_apply_authoritative_transform_VA_006E4E50` | Confirmed world/cell validation and immediate mutation |

## Client MFD/group-status names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x013EEAE0` | `client_mfd_status_message_construct_VA_013EEAE0` | Confirmed generated message construction |
| `0x013EEC70` | `client_mfd_status_message_deserialize_VA_013EEC70` | Confirmed generated parser |
| `0x00BA6DCA` | `game_listener_dispatch_client_mfd_status_VA_00BA6DCA` | Confirmed message-type branch |
| `0x00BD0550` | `mfd_status_route_by_object_id_VA_00BD0550` | Confirmed subscriber router |
| `0x00BD0270` | `swg_cui_status_ground_handle_mfd_update_VA_00BD0270` | Confirmed zone/position presentation handler |

## Player-name existence lookup names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x005ACED0` | `community_request_verify_player_name_VA_005ACED0` | Confirmed local gate/request/send role |
| `0x00ADE5A0` | `verify_player_name_message_construct_VA_00ADE5A0` | Confirmed request serializer |
| `0x00ADE730` | `verify_player_name_message_deserialize_VA_00ADE730` | Confirmed request parser |
| `0x00B04870` | `verify_player_name_response_deserialize_VA_00B04870` | Confirmed expected Boolean/Unicode parser |
| `0x00A6F187` | `community_listener_handle_verify_name_response_VA_00A6F187` | Confirmed response dispatch branch |
| `0x005ACFA0` | `community_publish_verify_name_response_VA_005ACFA0` | Confirmed `(Boolean, Unicode)` publication boundary |

## Planetary map-location names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00ACEAC0` | `get_map_locations_message_construct_VA_00ACEAC0` | Confirmed request serializer |
| `0x00ACEC70` | `get_map_locations_message_deserialize_VA_00ACEC70` | Confirmed request parser |
| `0x00ACF5A0` | `get_map_locations_response_deserialize_VA_00ACF5A0` | Confirmed seven-field response parser |
| `0x00ACFA70` | `map_location_array_deserialize_VA_00ACFA70` | Confirmed counted-array parser |
| `0x00B1B640` | `map_location_record_deserialize_VA_00B1B640` | Confirmed record parser |
| `0x004B87E0` | `planet_map_listener_handle_response_VA_004B87E0` | Confirmed response branch |
| `0x004B9D40` | `planet_map_manager_install_snapshot_VA_004B9D40` | Confirmed planet validation/cache replacement role |

## Customer-service category names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x005C96F0` | `customer_service_request_categories_VA_005C96F0` | Confirmed language extraction, construction and send role |
| `0x00ADF140` | `request_categories_message_construct_VA_00ADF140` | Confirmed ASCII-language request constructor |
| `0x00AFAA40` | `request_categories_response_deserialize_VA_00AFAA40` | Confirmed response parser |
| `0x00AFAD60` | `customer_service_category_vector_deserialize_VA_00AFAD60` | Confirmed counted recursive-vector parser |
| `0x00B1D050` | `customer_service_category_record_serialize_VA_00B1D050` | Confirmed recursive record serializer |
| `0x00A19580` | `customer_service_listener_dispatch_VA_00A19580` | Confirmed listener including category-response branch |
| `0x005CAB60` | `customer_service_install_category_tree_VA_005CAB60` | Confirmed deep-copy/cache and notification boundary |

## Survey resource-list names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x01199900` | `resource_list_for_survey_deserialize_VA_01199900` | Confirmed generated message parser |
| `0x01199D00` | `survey_resource_record_deserialize_VA_01199D00` | Confirmed ASCII/U64/ASCII record parser |
| `0x00E35550` | `survey_listener_dispatch_messages_VA_00E35550` | Confirmed resource-list and survey-result dispatcher |
| `0x00F0F690` | `survey_ui_install_resource_list_VA_00F0F690` | Confirmed full-list UI replacement role |
| `0x01199160` | `survey_message_deserialize_VA_01199160` | Confirmed generated survey-grid parser |
| `0x01199480` | `survey_point_vector_deserialize_VA_01199480` | Confirmed counted four-F32 record parser |
| `0x00F0FD70` | `survey_ui_install_density_grid_VA_00F0FD70` | Confirmed density-plot replacement role |
| `0x00F0FF00` | `survey_ui_compute_plot_extent_VA_00F0FF00` | Confirmed X/Y extent computation |
| `0x00F0FFA0` | `survey_ui_add_density_sample_VA_00F0FFA0` | Confirmed per-sample visual construction |

## Vendor-ownership names (2026-09-15)

| Address | Provisional name | Status |
|---:|---|---|
| `0x00AF2DF0` | `is_vendor_owner_message_construct_VA_00AF2DF0` | Confirmed U64 request constructor |
| `0x00AF2F40` | `is_vendor_owner_message_deserialize_VA_00AF2F40` | Confirmed U64 request parser |
| `0x00AE76C0` | `is_vendor_owner_response_deserialize_VA_00AE76C0` | Confirmed five-field response parser |
| `0x006A8010` | `auction_response_dispatcher_VA_006A8010` | Confirmed multi-response dispatcher |
| `0x004CF440` | `auction_publish_vendor_ownership_VA_004CF440` | Confirmed event publication boundary |
## Accept-auction response closure (2026-09-20)

| Address | Stable provisional name | Status |
|---|---|---|
| `0x00AEBE10` | `accept_auction_response_parse_VA_00AEBE10` | Confirmed `U64 ID + U32 result` archive parser |
| `0x006A8820` | `accept_auction_response_extract_id_VA_006A8820` | Confirmed parsed-ID extractor |
| `0x004CF230` | `auction_manager_receive_accept_response_VA_004CF230` | Confirmed retained-auction update, result presentation and publication handler |

The corresponding server wrapper is not constructed by current Stardust source. This is retained legacy protocol support rather than an active server-to-client flow.

## Persistent-mail send acknowledgement closure (2026-09-20)

| Address | Stable provisional name | Status |
|---|---|---|
| `0x008EFE80` | `persistent_message_manager_listener_receive_VA_008EFE80` | Confirmed multi-message mail listener |
| `0x008F0089` | `persistent_mail_send_ack_branch_VA_008F0089` | Confirmed exact-name dispatch branch |
| `0x00AEFBC0` | `chat_on_send_persistent_parse_VA_00AEFBC0` | Confirmed `S32 result + U32 sequence` parser |

Result 23 is intentionally left unlabeled: the executable confirms special suppression behavior but not its semantic name.
## Client auction-request closure (2026-09-20)

| Address | Stable provisional name | Status |
|---|---|---|
| `0x00ACFFA0` | `auction_query_headers_message_construct_VA_00ACFFA0` | Confirmed query archive constructor |
| `0x004D0FC0` | `auction_bid_or_buy_request_send_VA_004D0FC0` | Confirmed request wrapper/send path |
| `0x00AD0710` | `bid_auction_message_construct_VA_00AD0710` | Confirmed U64 + U32 + U32 constructor |
| `0x004D1130` | `auction_cancel_request_send_VA_004D1130` | Confirmed request wrapper/send path |
| `0x00AD1490` | `cancel_live_auction_message_construct_VA_00AD1490` | Confirmed U64 constructor |
| `0x004D1430` | `auction_retrieve_request_send_VA_004D1430` | Confirmed request wrapper/send path |
| `0x00AD1780` | `retrieve_auction_item_message_construct_VA_00AD1780` | Confirmed two-U64 constructor |
## Random-name request closure (2026-09-20)

| Address | Stable provisional name | Status |
|---|---|---|
| `0x00BB6670` | `avatar_creation_request_random_name_VA_00BB6670` | Confirmed template selection, request construction and reliable send |
| `0x00AE5E60` | `client_random_name_request_construct_VA_00AE5E60` | Confirmed single-ASCII-field constructor |
## Chat request constructors (2026-09-20)

`0x00AFBE10`, `0x00AFC350`, `0x00AFDB50`, `0x00AF2900`, `0x00AF0B10`, `0x00AF1C10`, `0x00AF1EB0`, and `0x00AEF750` are now assigned to the exact chat request constructors listed in `ANALYSIS_CHECKPOINT_2026-09-20_CHAT_REQUESTS.md`. Their callers at `0x00A28C10`, `0x00A28E80`, `0x00A2A1F0`, `0x00900A60`, `0x008F0790`, `0x008F3400`, and `0x008EFB40` are stable subsystem request/send boundaries.
## Final packet-inventory bounded unknown (2026-09-20)

`HarvesterObjectMessage7` is no longer an unknown packet route. Its complete top-level grammar and active synchronized-UI role are recorded in `ANALYSIS_CHECKPOINT_2026-09-20_FINAL_PACKET_ROWS.md`. The nested `HopperList::insertToMessage` codec is now also closed: U32 count, U32 update counter, then repeated U64 resource-spawn ID and F32 quantity. The outer marker is numerically confirmed as zero when empty and `count + 1` otherwise; only its historical source-level name is unknown. `HarvesterObjectDeltaMessage3` is confirmed unused and incompatible (wrong `ITNO` tag/index for active harvesters), so it is a defect artifact rather than missing client behavior. See `ANALYSIS_CHECKPOINT_2026-09-20_HARVESTER_HOPPER_CODEC.md`.

`CreateTicketMessage` constructor `create_ticket_message_construct_VA_00ADFA90` is byte-complete. It registers ASCII, U32, U32, Unicode, Unicode, Unicode, ASCII, constant U32 zero and U8/Boolean fields in that order. Current Stardust consumes only the first four. The three legacy text-field business labels and final flag name are unpublished but do not affect current-server interoperability. See `ANALYSIS_CHECKPOINT_2026-09-20_CREATE_TICKET_TAIL.md`.

`CommoditiesItemTypeListResponse` nonempty records are no longer a codec unknown. `commodity_item_type_hierarchy_decode_VA_006A9030` reads an outer U32-keyed map of inner U32-keyed maps; each leaf is decoded by `commodity_item_type_leaf_decode_VA_012B31A0` as two Archive ASCII strings plus one U32. Only the historical business label of the final U32 remains unknown; it must be preserved verbatim. See `ANALYSIS_CHECKPOINT_2026-09-20_COMMODITY_ITEM_TYPES.md`.

The file-streamer queue topology is closed. `file_streamer_install_VA_00A9BC70` owns a single worker, mutex, wake primitive and three strict-priority FIFO queues. `file_streamer_worker_loop_VA_00A9C090` services class 2 then 1 then 0; `file_streamer_process_read_VA_00A9BF30` reads/requeues in 128 KiB slices; `file_streamer_shutdown_join_VA_00A9BDC0` uses a terminal request and join. Only human-readable priority names remain unknown. See `ANALYSIS_CHECKPOINT_2026-09-20_FILE_STREAMER_THREAD.md`.

Network payload dispatch affinity is closed for the ordinary configuration. `shared_network_config_install_VA_012BBA30` defaults `useNetworkThread` false and `useTcp` true; `shared_network_enforce_thread_policy_VA_012BB870` disables a requested network thread under TCP. Complete frames are copied into the single producer/consumer queue and drained by the frame update before named-message state mutation. Exact legacy affinity of every connection-open/close callback remains a bounded lifecycle detail; the replacement will queue those events to the session owner. See `ANALYSIS_CHECKPOINT_2026-09-20_NETWORK_THREAD_AFFINITY.md`.

The spatial-audio coordinate boundary is closed. `audio_update_listener_and_voices_VA_0040E030` extracts world position as X/Y/Z, forward from transform K and up from J; `audio_listener_commit_VA_0041B780` passes those values unchanged to Miles. Spatial source positions are also passed as X/Y/Z. SWG wire X/Z/Y ordering is converted before this boundary. See `ANALYSIS_CHECKPOINT_2026-09-20_AUDIO_COORDINATES.md`.

The generic ExitChain lifecycle is no longer an architectural unknown. `exit_chain_register_VA_00AA21E0` inserts 20-byte callback records by ascending priority while retaining equal-priority registration order; `exit_chain_drain_VA_00AA22C0` unlinks, invokes and frees them. `app_entry_VA_00401050` explicitly drains the chain after the registered game loop returns. `game_install_VA_00422E80` supplies named removal callbacks for Game, scene cleanup, CUI and eight client managers. Individual unlabeled foundation calls remain bounded implementation details. See `ANALYSIS_CHECKPOINT_2026-09-20_EXIT_CHAIN_LIFECYCLE.md`.

The effective declarative UI corpus vocabulary is now inventoried: 251 effective records, 65 case-normalized element spellings, 130/130 include targets resolved, 20 event-like property spellings and zero decode failures. The remaining UI unknown is runtime semantics/fidelity, not corpus discovery. See `ANALYSIS_CHECKPOINT_2026-09-20_UI_CORPUS_VOCABULARY.md` and `ui_corpus_inventory.md`.

The IMAP/0006 command and binding codecs are byte-complete. External CMDS/0006 files contain CATE names and CMD chunks; each command is name, U32 device-family mask, two bytes and four `(U32,U32,cstring)` action records. All binding families use `(U32 physical index, cstring command name)`, including JOYX, JOYS and the correctly spelled `POV ` form. Later closures identify MAP inheritance, duplicate precedence and all four action transitions; only historical names for the two command flags remain unavailable. See `ANALYSIS_CHECKPOINT_2026-09-20_INPUTMAP_0006_CODECS.md` and the 2026-09-21 input checkpoints.

The procedural-terrain sorter's five category entries are no longer confused with the global renderer phases. `terrain_build_cell_material_blend_descriptor_VA_008612B0` writes a relevant material count, the chunk builder adds one, the primitive constructor stores it at `+0x108`, and `terrain_sorter_submit_primitive_VA_0084EDB0` uses it directly as the five-entry outer-batch index. Generated primitives therefore occupy material-complexity categories 1 through 4; category zero is reserved/default. Original category labels remain unknown but are not an architectural blocker. See `ANALYSIS_CHECKPOINT_2026-09-20_TERRAIN_SORTER_CATEGORIES.md`.

The virtual-`+8` dispatch thunk at `0x0084CAF0` no longer has unidentified concrete recipients. Terrain vector `+0x1B8` contains only the labeled far-dynamic, static-noncollidable and near-dynamic flora managers; vector `+0x1C8` contains only the optional global and local water managers. Both concrete flora vtables map slot `+8` to no-op `0x0085B3C0`; their visible geometry is maintained through radial/DPVS and shader-primitive machinery. Only the original common-interface method name is unavailable. See `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_RENDERABLE_DISPATCH.md`.

The UI/gameplay long tail is now bounded by a reproducible class inventory rather than an unenumerated unknown. There are 225 filtered concrete `Cui`/`SwgCui` RTTI classes. The inventory mediator and the seven-screen crafting family have confirmed vtables, widget bindings and typed event topology. This closes their architectural ownership model, but not every screen's internal semantics; those remain explicit per-class behavioral work. See `ANALYSIS_CHECKPOINT_2026-09-21_UI_MEDIATOR_CLASS_INVENTORY.md` and `ui_concrete_class_inventory.tsv`.

The common `CuiMediator` lifecycle is no longer an architectural unknown. Base construction binds named and runtime-typed `CodeData` dependencies; activation restores settings; deactivation persists them; update optionally enforces target validity/range; derived mediators attach and detach domain callbacks around those base operations. Exact historical names for all twelve virtual slots and individual flag bits remain unavailable. See `ANALYSIS_CHECKPOINT_2026-09-21_CUI_MEDIATOR_LIFECYCLE.md`.

The camera family is structurally inventoried and the principal gameplay `FreeChaseCamera` is mapped through profile load and per-frame update. Its `FCHS/0000` asset grammar, preference keys, discrete/smoothed zoom behavior, watched-object/controller dependency and transform output are confirmed. Later analysis closes `FRST` as first-person threshold, `DEFH` as default vertical target height, `PSHM` as posture-plus-state height overrides and `PSTM` as posture-only height multipliers. Only unpublished tag expansions remain. See `ANALYSIS_CHECKPOINT_2026-09-21_CAMERA_FAMILY_AND_FREE_CHASE.md` and `ANALYSIS_CHECKPOINT_2026-09-21_FREE_CHASE_PROFILE_SEMANTICS.md`.

The diagnostics/crash stack is closed at architectural level. Report, Warning, strict-fatal, Fatal, unhandled-exception, companion-text/minidump and obsolete external CrashReporter/SMTP layers are separated, including their configuration defaults. A replacement should retain structured severity and dumps while omitting historical SOE upload endpoints and converting untrusted-input failures into bounded rejection. See `ANALYSIS_CHECKPOINT_2026-09-21_DIAGNOSTICS_AND_CRASH_POLICY.md`.

The custom allocator/pool architecture is now closed for replacement decisions. The global heap is a synchronized, 16-byte-aligned boundary-tag allocator with coalescing and accounting; `MemoryBlockManager` supplies shared-by-element-size or private fixed-size pools with live-allocation guards. Its internal tree/free-list identity is intentionally redesignable and is not a compatibility contract. See `ANALYSIS_CHECKPOINT_2026-09-21_MEMORY_MANAGER_AND_POOLS.md`.

The remaining concurrency topology is closed at architectural level. There is no general job pool: a frame-owning main thread drains immutable network/asset results, while separate optional workers handle asynchronous asset opens, file streaming and terrain generation. `GameScheduler` is a timed callback queue rather than a worker. Exact OS-thread labels and lock primitives remain implementation details. See `ANALYSIS_CHECKPOINT_2026-09-21_THREAD_AND_SCHEDULER_TOPOLOGY.md`.

Runtime input layering is now closed architecturally: DirectInput devices feed pooled `IoWinManager` events, stacked scene/UI consumers route them into context-specific IMAP action queues, and camera/controller/UI systems consume logical IDs and values. Ground and space profile sets plus the dedicated UI map are enumerated, and later corpus work exhaustively records every configured command/action and physical binding. Exact calibration labels and selected long-tail numeric interpreter internals remain bounded refinement, not an engine blocker. See `ANALYSIS_CHECKPOINT_2026-09-21_INPUT_RUNTIME_DISPATCH.md` and the subsequent input-corpus checkpoints.

The manager-to-presentation long tail is now machine-enumerated from templated RTTI: 26 publishers, 107 unique manager/event contracts and 121 concrete owned-subscriber edges. Together with the 225-class UI inventory this supplies a finite closure checklist. It does not claim every event's payload semantics are complete; generic pair/vector payloads remain targeted work. See `ANALYSIS_CHECKPOINT_2026-09-21_GAMEPLAY_MANAGER_EVENT_GRAPH.md` and `manager_event_graph.tsv`.

The client macro subsystem is now closed at an architectural and persistence-codec level. `macros.txt` version `0000` is primary, `macros.iff` (`ORCM/MRTN/0000`) is the validated fallback, the 0x40-byte logical record and serialized field order are mapped, and execution enters the internal console-command parser rather than an OS shell. Only original member/helper labels remain unknown. See `ANALYSIS_CHECKPOINT_2026-09-21_CLIENT_MACRO_MANAGER.md`.

The original localization runtime is now architecturally closed and separated from DataLint. Locale-dependent `.stf` tables are opened through TreeFile/VFS, parsed only after `0xABCD` magic and variant 0/1 validation, retained in a cache, and indexed by both narrow key and numeric identity to owned UTF-16 values. Primary/fallback path semantics and bad-table versus bad-name diagnostics are retained. Exact historical header-member labels remain bounded unknowns. See `ANALYSIS_CHECKPOINT_2026-09-21_LOCALIZATION_STF_RUNTIME.md`.

The normal object-attribute path is now closed. Client requests are batched as decimal IDs through the queued `getAttributesBatch` command; Stardust answers with `AttributeListMessage` opcode `0xF3F12F2A` carrying U64 ID plus repeated narrow-key/Unicode-value pairs. The manager atomically caches the list, tracks freshness/invalidation and publishes `AttributesChanged`. The distinct auction-details alias collision is explicitly separated. See `ANALYSIS_CHECKPOINT_2026-09-21_OBJECT_ATTRIBUTE_MANAGER.md`.

The draft-schematic catalog bridge is now closed. `PLAY:9[4]` supplies only `(objectCRC, schematicID)` U32 pairs; DraftSchematicManager derives `DraftSchematicInfo` records by resolving `SharedDraftSchematicObjectTemplate`, its crafted shared template, localization, appearance/customization and weight metadata from original assets, then publishes `Changed`. This confirms that original draft and crafted templates are mandatory, not optional presentation extras. See `ANALYSIS_CHECKPOINT_2026-09-21_DRAFT_SCHEMATIC_ASSET_BRIDGE.md`.

QuestJournalManager is now closed at the architectural/content boundary. It builds a seven-column hierarchy from original `datatables/player/quests.iff`, composes that catalog with replicated active/completed/task state, and owns transient counter/timer overlays with typed events. A material blocker remains explicitly confirmed: the executable expects counter `0x440` and timer `0x441`, while Stardust emits counter `0x441` with a longer body and timer `0x444` with no client factory. See `ANALYSIS_CHECKPOINT_2026-09-21_QUEST_JOURNAL_MANAGER.md`.

The timed attribute-modifier path is now closed end to end. Creature-controller `0x229` calls `modifier_manager_add_or_refresh_VA_00943670` with U32 CRC/F32 seconds, `0x22A` calls `modifier_manager_remove_VA_00943700`, and `modifier_manager_tick_VA_00943300` expires entries. Definitions come from the four-column, 148-row original `datatables/modifiers/modifiers.iff`; CRC, two StringIds, icon style and original name are mapped. See `ANALYSIS_CHECKPOINT_2026-09-21_ATTRIBUTE_MODIFIER_MANAGER.md`.

Skill/progression ownership is now closed architecturally. The effective Stardust `skills.iff` has 1,089 rows and 27 confirmed columns and `xp_limits.iff` has 48 rows; `CREO:1` skill names and `PLAY:8` XP values are projections onto those definitions. `CuiSkillManager` validates selected skill/profession names, persists ten XP-monitor choices and sends confirmed surrender through `surrenderSkill`, while the server remains authoritative. See `ANALYSIS_CHECKPOINT_2026-09-21_SKILL_PROGRESSION_MANAGER.md`.

The social/emote loop is now closed. Ordered `social_types.iff` rows define numeric IDs, `social.stf` supplies at least 44 indexed presentation lines per entry, `cui_social_send_VA_00970F80` formats the `socialInternal` command, and controller `0x12E` reaches `cui_social_process_message_VA_009706E0` for localized chat and optional animation. Individual 44-line labels remain a content-decoder refinement. See `ANALYSIS_CHECKPOINT_2026-09-21_SOCIALS_AND_EMOTES.md`.

The game-language catalog and garbling algorithm are now closed. Original `game_language.iff` has 11 row-indexed languages, speaking/comprehension skill-mod names, 26 variable-length letter substitutions and an audible flag; `game_language_transform_text_VA_011C6530` performs deterministic case-preserving substitution. Spatial chat carries U8 language while `PLAY:9[9]` currently replicates it as U32. See `ANALYSIS_CHECKPOINT_2026-09-21_GAME_LANGUAGE_MANAGER.md`.

The structured localized-prose boundary is now closed architecturally. The exact active `StringIdChatParameter` wire order is recorded; `cui_string_variables_expand_VA_008AD1E0` parses three-character `%XY` placeholders with U/T/O participants and eleven stable grammar codes, using original `grammar.stf`, object identities, nested StringIds/literals and viewpoint fallbacks. Six historical selector labels remain explicitly unknown but their stable behavior boundary is preserved. See `ANALYSIS_CHECKPOINT_2026-09-21_STRING_VARIABLES_AND_GRAMMAR.md`.

The mood/spatial-chat catalog layer is now closed. `spatial_chat_types.iff` defines 102 positional U16 type IDs plus volume, private-type and skill policies; `mood_types.iff` defines 188 positional U16 mood IDs and `mood_types.stf` supplies display, animation and prose variants. Both server and client loaders preserve row order, and controller `0xF4` consumes those IDs. High spatial-chat flag labels remain bounded work. See `ANALYSIS_CHECKPOINT_2026-09-21_MOOD_AND_SPATIAL_CHAT_CATALOGS.md`.

The command-definition layer is now closed. Four ordered DTI overlays supply 833 enabled ground or 886 enabled space commands, including local/native hooks, script hooks, locomotion/state eligibility, targeting, range, visibility and queue policy. Disabled rows delete prior definitions, while authoritative commands enter the already-confirmed controller `0x116` path. Private record-member labels remain redesignable. See `ANALYSIS_CHECKPOINT_2026-09-21_COMMAND_TABLE_AND_QUEUE_POLICY.md`.

The toolbar action model is now closed architecturally. Pane/slot settings persist semantic drag type, command/string and U64 object-ID references; player setup, command-list, scene, macro and keybinding events re-resolve them. `CMD_uiToolbarSlot%02d` input actions reach a generic semantic execution interface and then local or queued command behavior. Exact drag enum numbers and UI effect timings remain presentation refinements. See `ANALYSIS_CHECKPOINT_2026-09-21_TOOLBAR_ACTION_MODEL.md`.

The DSEF material-field audit supersedes earlier tentative names. `0x006FD590` is `ShipDestructionSequenceData_getInitialSplitDelay_VA_006FD590`; its value seeds split countdowns at successive factors of 0.5, clamped to 0.1–20 seconds. `0x006FD5A0` and `0x006FD5B0` expose break/final effect resource pairs whose floats are nominal reference lengths used for uniform scaling. `0x006FC8C0` resolves an SDOE object template or appearance fallback, while `0x006FC950` constructs the selected object and attaches it at the third-string parent hardpoint. See `ANALYSIS_CHECKPOINT_2026-09-21_DSEF_FIELD_SEMANTICS.md`.

`0x006EDF20` is further closed as `ShipObject_buildContrailsRecursive_VA_006EDF20`: it processes children first, inserts `CONT` string D into a rebuild-local override set, and skips the current record when string A is already present. This corrects the former tentative “hardpoint remap” interpretation. See `ANALYSIS_CHECKPOINT_2026-09-21_CONTRAIL_OVERRIDE_KEYS.md`.

`0x00B59EF0` is `legacy_shuffled_park_miller_next_VA_00B59EF0`. Raw `ECX` flow proves `0x00B5E780` gives each static-collidable flora cell its own zeroed state seeded with the negative linear cell key; it consumes X/Z jitter, yaw and optional scale in order. `0x00AB8E10` is transform yaw, correcting the former seed interpretation at `0x00858BC0`. See `ANALYSIS_CHECKPOINT_2026-09-21_STATIC_FLORA_RNG_CLOSURE.md`.

The specialized CDF residual audit confirms no unidentified active functions are hidden behind PALV, OBJT, or VLEF. `0x006FF150` handles only MESH/WCSI and generically skips the first two; the top-level CDF dispatcher skips VLEF. Their remaining field labels belong to a dormant/newer extension domain, not this executable's behavior. See `ANALYSIS_CHECKPOINT_2026-09-21_CDF_TOLERATED_EXTENSIONS.md`.

`0x00A9ADC0` is `file_streamer_read_wait_VA_00A9ADC0`, the sole normal request producer. It passes a stack-local result address, enqueues operation 2 and waits before returning; therefore requests cannot outlive caller storage and no cancellation handle exists. See `ANALYSIS_CHECKPOINT_2026-09-21_FILE_STREAMER_COMPLETION_OWNERSHIP.md`.

`0x00A965B0` is `async_loader_cancel_callback_VA_00A965B0`; `0x00A96BA0` is the sole main-thread completion publisher; and callback `0x00A962A0` performs sentinel/drain/join shutdown. Cancellation suppresses publication while retaining unconditional result cleanup. See `ANALYSIS_CHECKPOINT_2026-09-21_ASYNC_LOADER_CANCELLATION_SHUTDOWN.md`.

The procedural-terrain stale-result and teardown boundary is closed. `0x0085F820` queues dirty envelopes, `0x0085F950` starts one exclusive generation cycle, `0x0085EE60` builds chunks off-thread and publishes pointers under lock, and `0x0085EFD0` installs/retire results on the main thread. `0x00849FB0` stops, signals and joins before releasing those structures. There is no generation-tag rejection path because new observer updates/cycles are gated by the active byte. See `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_GENERATION_LIFECYCLE.md`.

The effective input action content-enumeration item is closed. All 436 command
definitions and 525 populated action tuples are recorded in
`input_command_actions.csv`; the inverse map covers all 120 numeric action IDs.
Unknown historical enum labels remain nomenclature only, not missing bytes or
configured behavior. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_COMMAND_ACTION_MAP.md`.

The original physical-binding corpus is closed at the asset boundary. All 14
effective maps and 1,939 rows are recorded in `input_physical_bindings.csv`.
Twenty-four stale command references and two duplicate physical tuples are now
explicit compatibility fixtures. Only MAP INFO/flag selection semantics and
duplicate precedence remain bounded runtime-policy questions. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_PHYSICAL_BINDING_CORPUS.md`.

The command-mask policy is fully closed. `0x00AB12F0` supplies family constants
1/2/4/8 to the discrete, POV, axis and slider loaders; their test against
command `+0x18` proves device eligibility rather than shift-state filtering.
MAP `INFO` remains separate. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_DEVICE_MASK_POLICY.md`.

`0x00AADEB0` is `input_map_rebuild_active_bindings_VA_00AADEB0` and
`0x00AADDC0` is `input_map_apply_sparse_bindings_VA_00AADDC0`. The former
selects the exact active shift state and applies state zero first when the MAP
flag is set; the latter writes sparse records into dense tables in serialized
order, proving last-wins duplicate precedence. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_MAP_INHERITANCE_PRECEDENCE.md`.

`0x00AADFD0`, `0x00AAE150`, `0x00AAE5A0`, and `0x00AAE470` close command
action slots 0..3 as activation/press, deactivation/release, held-repeat update,
and forced reset/cancel. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_ACTION_TRANSITIONS.md`.

The mission UI no longer carries an undifferentiated list/details uncertainty.
The live current-server path is `MISO` replication plus mission-bag events;
explicit list/details response code is historical/dormant. The remaining
bounded mission unknown is the legacy `ResponseRemove` request/opcode and the
unused `0xF8` payload tail. See
`ANALYSIS_CHECKPOINT_2026-09-22_MISSION_UI_BOUNDARY.md`.

The persistent-mail manager is closed at its active protocol, state and UI
event boundary. Remaining mail unknowns are only friendly labels for rare
legacy send results and the complete shared OOB tag vocabulary; the parser's
length-delimited skip requirement is known. See
`ANALYSIS_CHECKPOINT_2026-09-22_PERSISTENT_MAIL_MANAGER.md`.

The inventory/container manager is closed at the active command, response,
projection and UI-event boundary. The only bounded protocol unknown is the
friendly meaning of the U32 field that current Stardust always emits as zero;
the bytes and current behavior are known. See
`ANALYSIS_CHECKPOINT_2026-09-22_INVENTORY_CONTAINER_MANAGER.md`.

The character-search result path is no longer unknown: controller `0x1E7`
decodes a typed vector and publishes `CharacterMatchRetrieved`. Remaining
matchmaking uncertainty is limited to friendly meanings for four legacy query
integers and the historical behavior of two set-pref commands that are no-ops
in current Stardust. See
`ANALYSIS_CHECKPOINT_2026-09-22_MATCHMAKING_AND_WHO.md`.

The combat/damage subsystem boundary is no longer unknown. Remaining gaps are
bounded visual-fidelity coefficients for damage fly-outs; protocol authority,
event ownership, self/other preference gates and subscriber layers are known.
See `ANALYSIS_CHECKPOINT_2026-09-22_COMBAT_DAMAGE_PRESENTATION.md`.

`CuiObjectTextManager` is closed at its architecture/lifetime boundary. The
remaining unknown labels are two cached integer pairs and fine-grained legacy
nameplate permutations; authoritative inputs, style dependencies, smoothing,
scene invalidation and safe replacement ownership are known. See
`ANALYSIS_CHECKPOINT_2026-09-22_OBJECT_TEXT_NAMEPLATES.md`.

`CuiMessageQueueManager` is closed at its internal event/translation boundary.
Only the friendly names of two registered UI action keys remain unknown; the
typed request payload, macro/input-map behavior and non-network ownership are
confirmed. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_MESSAGE_QUEUE_COMMAND_BRIDGE.md`.

`CuiIconManager` is closed at its semantic resolver boundary. Unpublished
friendly labels for a few returned numeric descriptor categories remain, but
the action types, catalog lookups, diagnostics and renderer-independent
replacement ownership are known. See
`ANALYSIS_CHECKPOINT_2026-09-22_ICON_MANAGER_RESOLUTION.md`.
## Cached avatar manager reconciliation (2026-09-22)

The subsystem boundary is now closed. Stable names cover callback installation/removal, deferred dirty processing, `CADF` load/write, current-player refresh, character planet/last-played mutation, and character-list persistence. Remaining unlabeled helpers are container/string machinery or exact legacy cache-path/text-format details; they are not server-protocol or live-replication blockers. See `ANALYSIS_CHECKPOINT_2026-09-22_CACHED_AVATAR_MANAGER.md`.
## Knowledge-base manager reconciliation (2026-09-22)

The offline/online split is closed. The original-asset file list, five-column node schema, seven concrete node families and overlay requirement are mapped. Network search/article codecs were already known; current Stardust's corresponding command handlers are confirmed no-ops. Only the historical semantic name of the per-search-result trailing U16 remains unknown; current server code writes zero. See `ANALYSIS_CHECKPOINT_2026-09-22_KNOWLEDGE_BASE_MANAGER.md`.
## Instant-message manager reconciliation (2026-09-22)

The manager state machine is closed across sequence allocation, pending timeout, acknowledgement result handling, ignore filtering, reply identity, typed UI publication, and automatic replies. The incoming trailing U32 remains an opaque zero-valued compatibility field; its exact width/order are known and it is not a handler gap. See `ANALYSIS_CHECKPOINT_2026-09-22_INSTANT_MESSAGE_MANAGER.md`.
## Login manager reconciliation (2026-09-22)

The client-side state coordinator is now closed beyond the previously mapped wire codecs: subscriptions, collection rebuild/join rules, typed UI events, permissions correlation, connection-server opening, independent UDP ping lifecycle, and original-template avatar preview creation are mapped. Residual historical labels on unused cluster metadata do not block the replacement state model. See `ANALYSIS_CHECKPOINT_2026-09-22_LOGIN_MANAGER_STATE.md`.
## Resource manager / harvester reconciliation (2026-09-22)

The previously under-documented `CuiResourceManager` is closed at the transaction boundary. Stable names cover installation, resource snapshot application, empty/discard initiation, and correlated completion. Inner controller types `0xEA/0xED`, all active fields, mode meanings and U8 stale-response token are confirmed against current Stardust. Residual container helpers are implementation detail. See `ANALYSIS_CHECKPOINT_2026-09-22_RESOURCE_MANAGER_HARVESTER.md`.

## System-message manager reconciliation (2026-09-22)

The active `ChatSystemMessage` path is closed from both server constructors
through the client decoder, prose formatter, and typed UI fanout. Remaining
uncertainty is limited to friendly names for rare structured-prose tag
variants; the archive is length-delimited and its safe compatibility behavior
is known. See `ANALYSIS_CHECKPOINT_2026-09-22_SYSTEM_MESSAGE_MANAGER.md`.

## Auction manager joined-state reconciliation (2026-09-22)

The active market coordinator is closed at the replacement boundary. Query
lists, detail merging, pagination, action responses, vendor capability and
commodity taxonomy are joined by stable IDs and typed events. Remaining gaps
are friendly meanings for preserved compatibility fields/option bits and a
dormant accept-bid producer, not missing active dispatch paths. See
`ANALYSIS_CHECKPOINT_2026-09-22_AUCTION_MANAGER_JOINED_STATE.md`.

## Avatar-creation workflow reconciliation (2026-09-22)

Character creation is closed across catalog-driven client state, exact request
wire order, random-name correlation, result handling, and current server-side
creation authority. The reserved U8 remains opaque but preserved. Original
template/customization/hair data is an explicit required asset dependency, not
an unknown code path. See
`ANALYSIS_CHECKPOINT_2026-09-22_AVATAR_CREATION_WORKFLOW.md`.

## Waypoint runtime reconciliation (2026-09-22)

Waypoint replication and presentation lifetime are closed. The only bounded
semantic unknowns are the preserved auxiliary U64 and historical purpose of
the currently empty secondary map; its fallback precedence is confirmed. See
`ANALYSIS_CHECKPOINT_2026-09-22_WAYPOINT_REPLICATION_RUNTIME.md`.

## Common view-6 reconciliation (2026-09-22)

The common U32 is no longer an unknown behavior path: it is a passive replicated
scalar with no installed semantic callback in this executable. Its historical
friendly name remains intentionally unknown; retain it by exact width and
tag/view. See `ANALYSIS_CHECKPOINT_2026-09-22_COMMON_VIEW6_OPAQUE_FIELD.md`.

## Replication exception-boundary reconciliation (2026-09-22)

The open catch-boundary question is closed through the network queue: the
original path has destructor unwinding but no local decode recovery. The
replacement must use bounded transactional decoding rather than emulate this
failure behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_REPLICATION_DECODE_FAILURE_BOUNDARY.md`.

## UI event-expression reconciliation (2026-09-22)

The effective event language is no longer an unbounded scripting unknown. Its
complete deployed values require ordered property assignments, tree paths,
copies, Boolean comparison and basic arithmetic. Exact malformed-input coercion
remains optional fidelity work. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_EVENT_EXPRESSION_CORPUS.md`.

## UI style/font/atlas reconciliation (2026-09-22)

Original UI presentation is no longer an unspecified asset dependency:
effective glyph metrics, atlas identities, image slices, frames and palettes
are captured. Rectangle edge convention and rare source qualifiers remain
fixture-level fidelity work. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_STYLE_FONT_ATLAS_PIPELINE.md`.

## Custom UI render-widget reconciliation (2026-09-22)

Embedded object previews are no longer an architectural unknown. They retain
real client objects/appearances, compute transformed scene bounds and use
camera/light properties supplied by original UI assets. Paperdoll slot semantics
and ground-radar projection details remain bounded follow-up targets. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUSTOM_UI_RENDER_WIDGETS.md`.

## Ground radar reconciliation (2026-09-22)

The radar projection boundary is closed: it consumes player/world transforms,
procedural terrain, scene objects and waypoint locations, with persisted range
and mode. Per-category blip priority remains detailed fidelity work. See
`ANALYSIS_CHECKPOINT_2026-09-22_GROUND_RADAR_RUNTIME.md`.

## Paperdoll reconciliation (2026-09-22)

The paperdoll's equipment interaction is no longer an architectural unknown.
It derives slot volumes from skeletal hardpoints and routes successful drops to
the container transfer service. Numeric event labels remain provisional. See
`ANALYSIS_CHECKPOINT_2026-09-22_PAPERDOLL_EQUIPMENT_RUNTIME.md`.

## CUI preference reconciliation (2026-09-22)

Preference ownership, typing, change notification, scene scoping and palette
application are no longer architectural unknowns. The exact machine-options IFF
envelope remains bounded persistence-format work. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_PREFERENCES_RUNTIME.md`.

## Machine-options IFF reconciliation (2026-09-22)

The previously bounded persistence-format task is closed for deployed version
`0002`, including all five value types and lookup precedence. Older version
`0000` migration semantics remain intentionally unsupported by the replacement.
See `ANALYSIS_CHECKPOINT_2026-09-22_MACHINE_OPTIONS_IFF.md`.

## Character UI settings reconciliation (2026-09-22)

The `.uis` path, `UIST/0003` grammar, resolution policy, owner records and three
typed map domains are closed and corpus-validated. Only individual mediator
opaque-blob payloads remain local fidelity work. See
`ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_UI_SETTINGS_UIS.md`.

## Incapacitation UI reconciliation (2026-09-22)

The incap mediator is closed through replicated timer, local interpolation,
display update and authoritative-state close behavior. No client recovery
request is involved. See `ANALYSIS_CHECKPOINT_2026-09-22_INCAPACITATION_UI.md`.

## Death and cloning reconciliation (2026-09-22)

No dedicated cloning-message handler remains unresolved for the current server
path: Stardust constructs a generic `SuiListBox`, retains each facility's U64 ID
as server-side row metadata, and accepts a client row index through the normal
SUI event reply. The predesignated-cloner policy, eligibility filtering and
transport are server responsibilities. See
`ANALYSIS_CHECKPOINT_2026-09-22_DEATH_CLONING_WORKFLOW.md`.

## Vendor UI reconciliation (2026-09-22)

The vendor and set-price mediator families are now bounded through their common
auction-list base, six concrete panes, ownership/session-driven view choice,
typed set-price event, item-name/price projection and established create-listing
request/response path. The previously recorded duration-unit question remains a
wire-semantics test item, not an unidentified client function. See
`ANALYSIS_CHECKPOINT_2026-09-22_VENDOR_UI_RUNTIME.md`.

## Space navigation UI reconciliation (2026-09-22)

The hyperspace/zone-map family is closed at the behavioral boundary: destination
selection, exact command argument, server task handoff, map-scene lifecycle,
input/picking, filter categories, waypoint/context actions and original asset
dependencies are identified. Remaining anonymous camera helpers are local math
implementation details, not protocol or architecture gaps. See
`ANALYSIS_CHECKPOINT_2026-09-22_SPACE_NAVIGATION_UI.md`.

## Survey UI reconciliation (2026-09-22)

The remaining survey UI family is bounded through construction, resource tree,
selection, exact command submissions, result point/density projection and
server authority. Anonymous collection helpers are implementation mechanics,
not unresolved client behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_SURVEY_UI_RUNTIME.md`.

## Resource splitter reconciliation (2026-09-22)

The split-container mediator and both resource/factory-crate command paths are
closed. Remaining slider/text framework helpers are shared UI mechanics, not
unidentified inventory behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_RESOURCE_SPLITTER.md`.

## Space radar reconciliation (2026-09-22)

The radar family is closed at the architectural boundary: base controls,
range/preference handling, planar composition, spherical custom-pane render
passes, object classification, special overlays and ship instrumentation are
identified. Remaining anonymous helpers implement projection, collection and
UI drawing mechanics. Exact visual constants remain fidelity tests rather than
unknown client/server behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_SPACE_RADAR_RUNTIME.md`.

## HUD/target/status reconciliation (2026-09-22)

The principal ground/space HUD composition, target list, all-target overlay and
space target-status behavior are now bounded. Remaining anonymous routines are
mostly widget layout, projection and visual interpolation. Exact visual tuning
and rare capability visibility branches remain validation work rather than an
unknown networking or object-model subsystem. See
`ANALYSIS_CHECKPOINT_2026-09-22_HUD_TARGET_STATUS_ARCHITECTURE.md`.

## Ship selection/component UI reconciliation (2026-09-22)

The ship-choice, ship-view, component-management and component-detail families
are bounded through their event inputs, controls, view-model fields and exact
command names. Current server parsing and validation are correlated. The empty
server implementation of `repairShipComponentInSlot` is explicitly tracked as
a server feature gap, not unresolved client behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHIP_SELECTION_COMPONENT_UI.md`.

## Space HUD widget reconciliation (2026-09-22)

The player-formation, missile-count, component-fly-out and ship-reticle families
are behaviorally bounded. Formation's missing current server consumers are a
known implementation gap. Remaining anonymous predicates select component
types and visual states; they are fidelity details rather than unidentified
transport. See
`ANALYSIS_CHECKPOINT_2026-09-22_SPACE_HUD_WIDGETS_FORMATION.md`.

## Toolbar reconciliation (2026-09-22)

Toolbar construction, action vocabulary, slot resolution/execution, drag and
context operations, multi-pane persistence and keybinding labels are bounded.
Settings-subkey historical names remain importer-fixture work, not a new
gameplay or network unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_TOOLBAR_ACTION_MODEL.md`.

## Quest counter/timer discrepancy reconciliation (2026-09-22)

This is no longer an analysis unknown. The exact original-client mapping is
counter `0x440` and timer `0x441`; server payload fields already align with
those respective codecs but current IDs `0x441`/`0x444` do not. The remaining
work is a deliberate future server compatibility change and integration test.
See
`ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_COMPATIBILITY_DECISION.md`.

## Combat queue UI reconciliation (2026-09-22)

The combat queue mediator, event inputs, sequence lookup, list projection and
cancel behavior are bounded. Remaining anonymous helpers are row layout and
animation mechanics, not an independent combat or network queue. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMBAT_QUEUE_UI.md`.

## Network status/MFD reconciliation (2026-09-22)

The network-status and reusable MFD status-bar families are bounded. Exact
display units and decay timing remain visual-fidelity checks, not unknown
network behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_NETWORK_STATUS_MFD_WIDGETS.md`.

## Ship weapon-group reconciliation (2026-09-22)

Assignment UI, local manager events and `SWGI` persistence are bounded. The two
per-member boolean labels and exact profile pathname remain naming/fidelity
items, not protocol or architectural unknowns. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHIP_WEAPON_GROUPS.md`.

## Biography workflow reconciliation (2026-09-22)

The request command, response schema, typed controller event, editor mediator
and save command are behaviorally closed. The exact address of the small
controller `0x1DB` decode branch remains an address-label gap only; it does not
leave wire fields or resulting state unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_BIOGRAPHY_WORKFLOW.md`.

## Droid command programming reconciliation (2026-09-22)

The mediator, staged collections, storage presentation, confirmation send,
inner `0x435` byte schema, authoritative server mutation and replication-based
reconciliation are bounded. Remaining anonymous row helpers are naming/UI
fidelity details, not a protocol or ownership unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_DROID_COMMAND_PROGRAMMING.md`.

## Skills UI family reconciliation (2026-09-22)

The concrete skills/profession mediator composition, event inputs, five graph
pages, stats/XP/certification tables, original-asset dependencies and surrender
authority boundary are closed. Pixel layout and graph-line routing are
presentation fidelity, not a missing progression contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKILLS_UI_FAMILY.md`.

## Data storage reconciliation (2026-09-22)

The waypoint, generic data, POI and draft pane sources, containment event
boundary, capacity displays, waypoint controls and local persisted view state
are bounded. Historical sort/filter labels are UI fidelity rather than an
unknown ownership or wire contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_DATA_STORAGE_UI.md`.

## Command browser reconciliation (2026-09-22)

Replicated command subscriptions, macro-manager subscriptions, catalog
construction, macro create/edit/delete and typed drag handoff are bounded.
Historical drag property names are presentation nomenclature, not a missing
command or protocol boundary. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMMAND_BROWSER.md`.

## Experience monitor reconciliation (2026-09-22)

Skill selection, XP type/cap lookup, replicated current value, progress and
completion visual state are closed. Remaining formatting is presentation
fidelity, not a progression or protocol unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_EXPERIENCE_MONITOR.md`.

## Ground group UI reconciliation (2026-09-22)

Member-row lifecycle, group/loot/target events, far-member status augmentation
and dismiss/disband/leave command boundaries are closed. Row layout and context
visibility nuance are presentation fidelity, not a group protocol unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_GROUND_GROUP_UI.md`.

## Session shell reconciliation (2026-09-22)

Loading, scene-selection, news, disconnect and splash ownership is bounded;
the five ground readiness categories explicitly separate session, asset/world,
terrain and player gates. Progress weights and image rotation remain visual
fidelity, not a hidden session protocol. See
`ANALYSIS_CHECKPOINT_2026-09-22_SESSION_SHELL_UI.md`.

## Locations mediator reconciliation (2026-09-22)

The mediator is classified as an optional developer/debug location selector;
its command and terrain-probe boundaries are known. Named-list provenance is a
low-priority tool detail, not a gameplay or protocol unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_LOCATIONS_DEBUG_MEDIATOR.md`.

## Harassment-report reconciliation (2026-09-22)

The form controls, asynchronous verify-name gate, Boolean result branch,
canonical-name copy and customer-service handoff are bounded. The malformed
current server response is a known implementation defect, not an unknown
client behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_HARASSMENT_REPORT_UI.md`.

## Game-menu reconciliation (2026-09-22)

Resume, disconnect/selection, exit, credits and hidden developer-tool routes
are bounded. Confirmation wording is UX fidelity rather than session
architecture uncertainty. See `ANALYSIS_CHECKPOINT_2026-09-22_GAME_MENU.md`.

## Stat-migration workflow reconciliation (2026-09-22)

Construction, activation, fixed ten-S32 response, row refresh/reset, target
submission, start/stop commands and authoritative server validation/application
are behaviorally closed. Slider timing and historical start/stop intent are
fidelity/history questions, not wire, state-ownership or gameplay unknowns. See
`ANALYSIS_CHECKPOINT_2026-09-22_STAT_MIGRATION_WORKFLOW.md`.

## Image-designer workflow reconciliation (2026-09-22)

The shared session model, distinct role mediators, complete controller envelopes,
preview/proposal/accept/reject flow and authoritative timer/payment/customization/
stat/XP commit are closed. Exact `type` enum labels beyond the server's hair value
and presentation mechanics remain fidelity/naming work, not a missing transaction
or ownership boundary. See
`ANALYSIS_CHECKPOINT_2026-09-22_IMAGE_DESIGNER_WORKFLOW.md`.

## Character-sheet runtime reconciliation (2026-09-22)

Construction, activation/deactivation, replicated-state projection, five-message
detail listener and stat-migration action boundary are closed. Formatting and
obsolete conditional visibility are visual/content fidelity, not state or protocol
unknowns. See `ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_SHEET_RUNTIME.md`.

## Permission-list workflow reconciliation (2026-09-22)

Mediator construction, replace-on-response member/suggestion state, add/remove
commands and authoritative structure/ship mutation policy are closed. The absent
server suggestion population is an optional UX detail, not an access-control or
wire unknown. See `ANALYSIS_CHECKPOINT_2026-09-22_PERMISSION_LIST_WORKFLOW.md`.

## Mission-details response reconciliation (2026-09-22)

The former `0xF8` payload-schema unknown is closed: the named response object is
fixed at `0x0C` bytes and contributes only its U8 sequence at `+0x08`. Mission
detail content remains in replicated `MISO` state. The current server's absence of
an emitter is a known feature gap, not an unknown executable message layout. See
`ANALYSIS_CHECKPOINT_2026-09-22_MISSION_DETAILS_RESPONSE_CLOSURE.md`.

## CuiConsent reconciliation (2026-09-22)

The reusable yes/no mediator is bounded through construction, listener lifetime,
completion and close. It carries a caller-owned callback and is not a fixed wire
message. The server's similarly named `cuiConsentResponse` command is a dormant
stub, while persistent consent lists use ordinary commands. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_CONSENT_BOUNDARY.md`.

## Examine/inventory-info reconciliation (2026-09-22)

The examine shell and reusable inventory-info target path are bounded from target
retention through preview/text refresh and attribute-manager requests. Creature
inspection's separate replicated/guild path is explicit. Remaining row styling
is presentation fidelity, not a protocol unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_EXAMINE_INVENTORY_INFO.md`.

## Community UI composition reconciliation (2026-09-22)

The six-page root, child ownership, original matchmaking table, tab lifecycle and
manager/event boundaries are closed. Historical labels for all matchmaking bits
remain asset semantics and current Stardust does not persist their setters; that
is explicit dormant feature behavior rather than an unknown hidden packet. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMMUNITY_UI_COMPOSITION.md`.

## Planet-map presentation reconciliation (2026-09-22)

Planet asset selection/fallback, coordinate projection, category tree, marker
pool, filters, zoom/pan, live player marker and typed location/waypoint refreshes
are behaviorally bounded. Exact internal UI property-token labels remain importer
fidelity, not a network or state-ownership unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_PLANET_MAP_PRESENTATION.md`.

## Customer-service ticket UI reconciliation (2026-09-22)

The root/child lifecycle, category and ticket-list dependencies, form validation,
numeric category selection, submit cooldown and create-message handoff are
bounded. Ignored legacy tail labels remain documented compatibility residue, not
a current-server blocker. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUSTOMER_SERVICE_TICKET_UI.md`.

## Bug-submission UI reconciliation (2026-09-22)

The form, report-body composition, category response dependency, ticket bridge
and obsolete beta-report route are bounded. The two Unicode ticket-tail fields
are empty here; the narrow field is auxiliary metadata and the final flag is
true. Historical member labels are naming-only and obsolete external routing
must not be restored. See `ANALYSIS_CHECKPOINT_2026-09-22_BUG_SUBMISSION_UI.md`.

## Vehicle-prototype reconciliation (2026-09-22)

The class is closed as a development-only local hover-dynamics tuner with twelve
named parameters and no network boundary. Its original hidden invocation route
is optional tooling breadth, not a gameplay/protocol unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_VEHICLE_HOVER_TUNING_PROTO.md`.

## CuiTransition reconciliation (2026-09-22)

Door geometry, close/covered-wait/open states, requested-versus-ready token
matching, sound phases and local ownership are closed. Exhaustive production
caller naming is later breadth, not an unknown transition contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_TRANSITION_OVERLAY.md`.

## Ship diagnostic widget reconciliation (2026-09-22)

Self/target resolution and every shared diagnostic row family are behaviorally
closed. Original accessibility is optional tooling breadth, not a ship-state or
network unknown. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHIP_DIAGNOSTIC_INFO_WIDGETS.md`.

## CuiInputBox reconciliation (2026-09-22)

Required widgets, affirmative/negative/cancel convergence, callback signature,
one-shot completion and eight-byte caller context are closed. The adjacent
`CuiSetName` specialization is bounded through RTTI validation and the existing
waypoint update route. Asset-authored geometry is presentation breadth, not an
unknown protocol contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_INPUT_BOX_CALLBACK_BOUNDARY.md`.

## CuiNotepad reconciliation (2026-09-22)

Profile path, filename, activation/deactivation, `0xFF` marker and byte limits
are behaviorally closed. The historical marker label is naming-only. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_NOTEPAD_PERSISTENCE.md`.

## CuiKeypad/CuiMoney reconciliation (2026-09-22)

Required widget bindings, one-shot keypad tokens, money subscriptions,
bound-object filtering and balance refresh behavior are closed. Caller-specific
keypad interpretation correctly remains outside the generic mediator. See
`ANALYSIS_CHECKPOINT_2026-09-22_KEYPAD_AND_MONEY_WIDGETS.md`.

## CuiTransfer reconciliation (2026-09-22)

Ratios, starting/current values, total invariant, slider/text edits, rounding,
clamps, revert and button lifecycle are closed. Caller-specific accepted-result
handling is outside this generic mediator. See
`ANALYSIS_CHECKPOINT_2026-09-22_GENERIC_TRANSFER_MEDIATOR.md`.

## Data-driven SUI reconciliation (2026-09-22)

The mandatory original-asset/server-script SUI bridge is behaviorally closed
through page construction, command interpretation, updates and outbound event
arguments. Textbox event 7 is not specialized and is unused by current scripts;
server-only command 8 has no producer and is rejected by this executable. These
are explicit compatibility boundaries, not unexplained functions. See
`ANALYSIS_CHECKPOINT_2026-09-22_DATA_DRIVEN_SUI_RUNTIME.md`.

## SUI page-manager reconciliation (2026-09-22)

The network listener, page-ID registry, create/update/force-close bridges,
scene-change drain and user-close distinction are behaviorally closed. Only the
narrow automatic target/range-close policy remains unproven; it does not block
the wire grammar, page ownership model or initial replacement implementation.
See `ANALYSIS_CHECKPOINT_2026-09-22_SUI_PAGE_MANAGER_LIFECYCLE.md`.

## Server SUI asset-footprint reconciliation (2026-09-22)

No currently referenced Stardust SUI root page is missing from the effective
original overlay. Remaining UI work is parser/widget implementation breadth,
not an unknown source for the server's concrete page layouts. See
`ANALYSIS_CHECKPOINT_2026-09-22_SERVER_SUI_ASSET_FOOTPRINT.md`.

## Stardust SUI behavioral-surface reconciliation (2026-09-22)

The current server's concrete event/property/widget requirements are bounded.
Textbox event 7 remains unused; text is returned on terminal events. Legacy UI
expression execution and the object-viewer bridge are required asset-runtime
features, not unknown network handlers. See
`ANALYSIS_CHECKPOINT_2026-09-22_STARDUST_SUI_BEHAVIORAL_SURFACE.md`.

## PASS/0010 reconciliation (2026-09-22)

The forward-version layout is no longer opaque: it inserts one Boolean at DATA
offset `0x04` and preserves the complete shifted PASS/0009 suffix. Only that
Boolean's historical render-state label remains unknown. It is false in all
currently literal-referenced affected families and true in four passes across
three indirectly/unreferenced shader paths. See
`ANALYSIS_CHECKPOINT_2026-09-22_PASS_0010_FIELD_ALIGNMENT.md`.

Full child-program correlation further constrains true to programmable bloom
passes in `ui_bar_glow` and the dedicated `3d_lava_heat_render` pass, while
same-shader fallback/ordinary passes are false. Treat it as a strongly inferred
auxiliary post-process write flag; only its historical name and exact target
channel remain unknown.

## IMPL/0009 reconciliation (2026-09-22)

The effective forward implementation envelope is structurally closed as the
0007 SCAP/[OPTN]/DATA/PASS grammar. The executable's rejection is a dispatcher
ceiling, not evidence of a different installed schema. Nested PASS/0010's one
new Boolean remains the only forward shader semantic field without a label.
See `ANALYSIS_CHECKPOINT_2026-09-22_IMPL_0009_FORWARD_SCHEMA.md`.

## Quest counter/timer reachability reconciliation (2026-09-22)

The `0x440/0x441/0x444` mismatch remains an explicit future compatibility
decision, but current shipped scripts do not emit either mismatched message.
This reclassifies it from a current-server runtime blocker to a dormant
profile-selection risk. See
`ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_REACHABILITY.md`.

## Session/process teardown integration (2026-09-22)

The architectural teardown scopes and worker-publication ordering are closed
for replacement design. Exact priority order of every legacy ExitChain callback
is unnecessary to copy; explicit dependency-owned scene, session and process
phases supersede it. See
`ANALYSIS_CHECKPOINT_2026-09-22_SESSION_TEARDOWN_INTEGRATION.md`.

## Deployed transport-selector reconciliation (2026-09-22)

The selector and installed launch path are closed: `useTcp=true` reaches the
length-prefixed TCP constructor, no loose include overrides it, and the shipped
launcher passes no arguments to `Stardust.exe`. The checked-in login and zone
services remain direct UDP/SOE. Only the external deployment edge that reconciles
those facts is unknown because it is absent from the supplied source; it is not
an unknown executable branch. See
`ANALYSIS_CHECKPOINT_2026-09-22_DEPLOYED_TRANSPORT_SELECTOR.md`.

## GPU TRE overlay selection reconciliation (2026-09-22)

The launch-time archive mutation is closed: only priority 54 changes, NVIDIA is
the default, and AMD selects a strict 26-record subset archive rather than
alternate payloads. The 179 names lost without fallback form a coherent
Mustafar ribbon/affector package. Only the historic vendor-specific defect or
capability reason remains unknown; loader behavior and replacement requirements
are explicit. See `ANALYSIS_CHECKPOINT_2026-09-22_GPU_TRE_OVERLAY_SELECTION.md`.

## MGN TRTS reconciliation (2026-09-22)

TRTS is no longer an unknown MGN extension. Loaders `0x007FE6B0`, `0x007FF9D0`
and `0x00800EF0` cover SKMG versions 0002/0003/0004 and decode named blueprint
texture-renderer records with flattened primitive/tag bindings. The sixteen
effective users all bind a face TRT output to `MAIN`. Remaining work belongs to
the internal BTRT command graph, not the skeletal mesh grammar. See
`ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md`.

The downstream BTRT resource family is also bounded: all eight effective
templates are version 0001, while the executable accepts 0001/0002 and parses
destination, resources, static geometry, camera and two command lists. The
remaining unknown is the friendly semantic name and modern operation for each
polymorphic command/DEST attribute, not the outer file contract. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_ENVELOPE.md`.

The command-family unknown is now reduced further. Dispatch plus RTTI identifies
CFBC/SRSC, RTFC/RTLI and all seven prepare-operation tags. The eight effective
face graphs use only SRSC/RTFC and unconditional SSTC/SST1/STFP operations.
Only friendly labels for their individual numeric operands remain open. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_COMMANDS.md`.

Runtime vtable correlation subsequently closes the operands for every live
type: SSTC selects a constant texture by table index, SST1 selects a clamped
customization-indexed texture range, STFP selects a clamped palette color as
texture factor, SRSC iterates shader passes, and RTFC binds its indexed vertex
buffer and draws. Subsequent blocks close the dormant operations and bound the
STFP Boolean as factory identity rather than render state.

The dormant BTRT variants are subsequently closed at the behavior boundary.
CFBC is confirmed color/depth/stencil clear state and STFP's Boolean is a
factory-domain discriminator. Only historical intent for the CVTP always-true
alias and the human label of the STFP discriminator remain unknown; no
unmapped BTRT command family or operand remains. See the dormant-path,
STFP-factory and CFBC-clear checkpoints.

## Skeletal, coordinate and nameplate reconciliation (2026-09-22)

The appearance-local update-order unknown is closed: pose-cache invalidation
precedes ordered controller alters; physical tracks extract root motion and
emit messages synchronously before installing replacement animation. The
Godot adapter is tested as a Z reflection with quaternion mapping
`[w,x,y,z] -> [-x,-y,z,w]`. Object-text cache pairs `+0x30/+0x34` and
`+0x38/+0x3C` are current/previous layout displacement and projected screen-Y.
Only cross-object scene order and historical source member names remain.

The SUNT angular-field gap is also bounded. `SunTrackingObject` uses scalar 1
as local-Y yaw slew rate and scalar 4 as child local-X pitch slew rate. The
middle authored `-10/+10` pair are strongly identified as lower/upper pitch
limits but are dormant in the executable. See
`ANALYSIS_CHECKPOINT_2026-09-22_SUN_TRACKER_RUNTIME.md`.

The STFP Boolean is no longer a behavioral unknown. It distinguishes
customization `VariableFactory` identity together with the variable name; it
does not alter palette lookup, color, alpha or shader state. Only the original
false/true domain label is unavailable. See
`ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md`.

CFBC's formerly friendly-only fields are confirmed as color/depth/stencil
enable-value pairs by the D3D9 Clear backend boundary. No anonymous BTRT
command operand remains.

## Skeletal appearance alter reconciliation (2026-09-22)

The former animation-update ordering unknown is closed at the appearance-local
boundary. `0x007C8AD0` invalidates pose evaluation state and advances the ordered
controller aggregate; `0x0083E630` extracts root motion and synchronously emits
messages before installing an animation returned by the transition step.
Readiness/fade progression and inactive-detail eviction occur afterward. Exact
relative order among separate scene objects remains outside what static indirect
dispatch proves. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md`.

## Coordinate-adapter reconciliation (2026-09-22)

The former host-engine pose-adapter test is closed for Godot. A single Z
reflection maps SWG Y-up/+Z-forward transforms into Godot Y-up/-Z-forward;
quaternion `[w,x,y,z]` becomes Godot `[-x,-y,z,w]`. Matrix-conjugation fixtures
cover identity, all principal 90-degree rotations and a mixed pose. Wire x/z/y
remains a codec field-order issue and is not conflated with this scene-space
conversion. See `ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md`.

## BTRT DEST reconciliation (2026-09-22)

`0x0086EEB0` and `0x0086F9E0` parse DEST dimensions and format lists;
`0x00869CD0` sends dimensions, first mapped format and list count to texture
creation. All 14 client enum labels, s207 D3DFORMAT mappings, capability probes,
and first-success fallback semantics are closed. Only the invalid sentinel 16's
historic source spelling is unavailable. See
`ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md`.

## Options/preferences binding reconciliation (2026-09-22)

The former generic options-page gap is closed at the behavioral boundary.
`SwgCuiOptBase` (`0x00FEFA20`) owns typed widget bindings carrying current,
setter and default callbacks. Integer slider (`0x00FF00E0`), float slider
(`0x00FF09A0`), Boolean (`0x00FF0B80`) and selection (`0x00FF0F60`) families
are distinguished. Widget changes preview through setters live. Parent
`SwgCuiOpt` (`0x00D58A40`) snapshots every child on entry; Cancel restores the
bindings, OK retains them, and dialog deactivation calls the machine-options
writer. Defaults is confirmation-gated and invokes each binding's default path.
Chat Colors, Alarm and AFK remain explicitly specialized
collection/edit controllers, not scalar bindings. Persistence is already
bounded through typed preference notifications and the `OPTN/0002`
machine-options writer. The unknown is historical class/member naming, not
page behavior or persistence. See
`ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md`.
# Interior-layout manager names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x0059B500` | `client_interior_layout_manager_install_VA_0059B500` | Registers manager/config/removal callback; confirmed |
| `0x0059B5A0` | `client_interior_layout_manager_apply_pending_world_layouts_VA_0059B5A0` | Frame-called eligible-building scan and local furnishing materialization; confirmed |
| `0x0059B7B0` | `client_interior_layout_manager_apply_building_layout_VA_0059B7B0` | Per-building/cell ILF node creation, transform, insertion and release; confirmed |

# Component-appearance names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x00798680` | `component_appearance_template_install_VA_00798680` | Registers/unregisters CMPA factory; confirmed |
| `0x007986D0` | `component_appearance_template_factory_VA_007986D0` | Allocates and constructs 0x2c-byte template; confirmed |
| `0x00798E40` | `component_appearance_template_load_VA_00798E40` | CMPA 0001-0005 dispatcher; confirmed |
| `0x007989D0` | `component_appearance_load_matrix_parts_VA_007989D0` | Repeated path plus 3x4 transform PART loader; confirmed |
| `0x007991D0` | `component_appearance_load_radar_policy_VA_007991D0` | v0005 RADR/INFO U32 and conditional auxiliary object; field label unknown |
| `0x007A0320` | `component_appearance_construct_VA_007A0320` | Creates/owns child appearances and parent visibility hierarchy; confirmed |

# Sprite/quad appearance names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x0079B8B0` | `sprite_quad_template_install_VA_0079B8B0` | Registers shared QUAD/SPRT factory; confirmed |
| `0x0079B910` | `sprite_quad_template_factory_VA_0079B910` | Allocates/constructs 0x44-byte template; confirmed |
| `0x0079BAE0` | `sprite_quad_template_load_VA_0079BAE0` | Outer QUAD/SPRT and v0000 dispatch; confirmed |
| `0x0079BC50` | `sprite_quad_template_load_v0000_VA_0079BC50` | Center, size, RGBA and shader decoder; confirmed |
| `0x00789AF0` | `sprite_quad_appearance_construct_VA_00789AF0` | Quad primitive, color/shader, bounds and visibility construction; confirmed |

# BEAM appearance names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x00798240` | `beam_appearance_template_install_VA_00798240` | Registers/unregisters BEAM factory; confirmed |
| `0x00798280` | `beam_appearance_template_factory_VA_00798280` | Allocates/constructs 0x3c-byte template; confirmed |
| `0x00798460` | `beam_appearance_template_load_VA_00798460` | BEAM outer form and 0000 dispatcher; confirmed |
| `0x00798540` | `beam_appearance_template_load_v0000_VA_00798540` | Two F32 dimensions, Vector4 and shader path; confirmed by loader/runtime |
| `0x007983F0` | `beam_appearance_factory_VA_007983F0` | Creates runtime instance from template; confirmed |
| `0x00792EC0` | `beam_appearance_construct_VA_00792EC0` | Owns shader-backed four-vertex primitive; confirmed |

# LEFX lightning-appearance names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x007B3570` | `lightning_appearance_template_install_VA_007B3570` | Registers/unregisters LEFX factory; confirmed |
| `0x007B3500` | `lightning_appearance_template_factory_VA_007B3500` | Allocates/constructs 0x100-byte template; confirmed |
| `0x007B3630` | `lightning_appearance_template_load_VA_007B3630` | Versions 0000-0002 dispatcher; confirmed |
| `0x007B37C0` | `lightning_appearance_template_load_v0000_VA_007B37C0` | PTEX and legacy scalar/attachment layout; confirmed |
| `0x007B3B80` | `lightning_appearance_template_load_v0001_VA_007B3B80` | Adds two waveform forms and Boolean; confirmed |
| `0x007B3E90` | `lightning_appearance_template_load_v0002_VA_007B3E90` | Current scalar layout; confirmed |
| `0x007A9BE0` | `lightning_appearance_construct_VA_007A9BE0` | Procedural five-branch shader-primitive runtime; confirmed broad contract |

# DTLA detail-appearance names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x007992E0` | `detail_appearance_template_install_VA_007992E0` | DTLA registration and skipL0 configuration; confirmed |
| `0x00799390` | `detail_appearance_template_factory_VA_00799390` | Allocates typed template; confirmed |
| `0x007993F0` | `detail_appearance_template_ctor_VA_007993F0` | Constructs loaded template; confirmed |
| `0x00799770` | `detail_appearance_template_level_count_VA_00799770` | Counts 20-byte level records; confirmed |
| `0x007997D0` | `detail_appearance_template_level_lower_bound_VA_007997D0` | Reads first F32 bound; confirmed |
| `0x007997F0` | `detail_appearance_template_level_upper_bound_VA_007997F0` | Reads second F32 bound; confirmed |
| `0x00799810` | `detail_appearance_template_lazy_fetch_child_VA_00799810` | Fetches child template; confirmed |
| `0x00799880` | `detail_appearance_template_create_child_VA_00799880` | Creates runtime child from fetched template; confirmed |
| `0x00799950` | `detail_appearance_template_find_level_by_id_VA_00799950` | Matches authored U32 ID; confirmed |
| `0x007999F0` | `detail_appearance_template_load_levels_VA_007999F0` | INFO/DATA/CHLD parser; confirmed |
| `0x0079A100` | `detail_appearance_template_load_radr_policy_VA_0079A100` | Reads RADR policy flag; precise semantic label unknown |
| `0x0079A1C0` | `detail_appearance_template_load_test_policy_VA_0079A1C0` | Reads TEST policy flag; precise semantic label unknown |
| `0x0079A280` | `detail_appearance_template_load_writ_policy_VA_0079A280` | Reads WRIT policy flag; precise semantic label unknown |
| `0x0079A340` | `detail_appearance_template_compare_upper_bound_VA_0079A340` | Sort comparator on second F32 bound; confirmed |
| `0x0079A370` | `detail_appearance_template_load_VA_0079A370` | Versions 0001–0007 dispatch; confirmed |
| `0x00787400` | `detail_appearance_construct_VA_00787400` | RTTI-backed runtime constructor; confirmed |
| `0x00787650` | `detail_appearance_ensure_child_VA_00787650` | Lazily creates/attaches a child; confirmed |
| `0x00787710` | `detail_appearance_select_distance_detail_VA_00787710` | Distance selection plus adjacent availability fallback; confirmed broad behavior |
| `0x00766700` | `indexed_detail_mesh_to_dpvs_model_VA_00766700` | Converts IDTL geometry through imported DPVS::MeshModel::create; confirmed |
| `0x0079AA50` | `detail_appearance_template_build_test_model_VA_0079AA50` | Builds TEST model or bound fallback; confirmed |

# Appearance-fetch fallback names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x00B260F0` | `appearance_template_fetch_or_default_VA_00B260F0` | Replaces null/empty/nonexistent path with defaultappearance.apt before fetch; confirmed by disassembly/decompilation |
| `0x00B262D0` | `appearance_template_default_path_VA_00B262D0` | Returns the default-appearance APT string; confirmed |
| `0x00A93030` | `tree_file_exists_normalized_VA_00A93030` | Normalizes logical path, then tests mounted search nodes; confirmed |
| `0x00A92FD0` | `tree_file_find_existing_node_VA_00A92FD0` | Priority-ordered search-node existence walk; confirmed |
| `0x00A94210` | `iff_open_path_allow_missing_VA_00A94210` | Calls TreeFile::open with caller's allow-missing flag and initializes IFF reader; confirmed |
| `0x00A996A0` | `search_tree_exists_VA_00A996A0` | TREE-node existence virtual delegates to CRC/path record lookup; confirmed |

# CLEF/MKAT loader names added 2026-09-22

| Address | Stable provisional name | Evidence |
|---|---|---|
| `0x006984E0` | `client_effect_read_CLEF_dispatch_VA_006984E0` | Dispatches CLEF/0001–0003; confirmed |
| `0x00698560` | `client_effect_read_CLEF_v0001_VA_00698560` | Reads CPAP, PSND, CAMS, CLGT, FFBK leaves; confirmed |
| `0x00698B70` | `client_effect_read_CLEF_v0002_VA_00698B70` | Version 0002 reader with extra CPAP byte; confirmed |
| `0x00699100` | `client_effect_read_CLEF_v0003_VA_00699100` | Version 0003 reader with extra CPAP fields; confirmed |
| `0x005F40D0` | `client_effect_template_fetch_cached_VA_005F40D0` | Cache/open path, warning/null on absent top-level CEF; confirmed |
| `0x00699860` | `client_effect_template_add_ref_VA_00699860` | Increments template reference at +0x04; confirmed |
| `0x00417320` | `sound_template_fetch_cached_VA_00417320` | Cache/open allow-missing and null return for absent sound template; confirmed |
| `0x007975D0` | `marker_appearance_factory_VA_007975D0` | Allocates MKAT appearance and calls constructor; confirmed |
| `0x00797630` | `marker_appearance_construct_and_dispatch_VA_00797630` | Initializes MKAT and dispatches v0000 loader; confirmed |
| `0x00797740` | `marker_appearance_read_v0000_INFO_VA_00797740` | Reads shader path and numeric marker fields; confirmed |

Open field-level semantics from this cluster: CLEF `CAMS` four scalars, `CLGT` byte/scalar record, `FFBK` two scalars, and downstream behavior of a null `PSND` template handle. The observed file/path contracts are in the 2026-09-22 CLEF and MKAT checkpoints; no semantics are assigned merely from the tag names.

## 2026-09-23 XFMS transform-mask closure

| Address | Stable provisional name | Evidence / status |
|---:|---|---|
| `0x008291B0` | `transform_mask_fetch_cached_VA_008291B0` | Path-existence check, VFS load and cache insertion; confirmed |
| `0x0083CDD0` | `transform_mask_construct_and_dispatch_VA_0083CDD0` | XFMS tag/version dispatch, only 0000 accepted; confirmed |
| `0x0083D000` | `transform_mask_read_v0000_INGR_VA_0083D000` | U16 count and counted transform-name set; confirmed |
| `0x0083C950` | `transform_mask_contains_name_VA_0083C950` | CRC-string ordered-set membership lookup; confirmed |
| `0x00817F80` | `masked_priority_template_read_VA_00817F80` | Mask and in/out/locomotion group references; confirmed |
| `0x00818110` | `masked_priority_template_create_runtime_VA_00818110` | Passes loaded mask and group identities to runtime constructor; confirmed |
| `0x0083D350` | `masked_priority_animation_construct_VA_0083D350` | Allocates per-skeleton-transform group array; confirmed |
| `0x0083CA00` | `masked_priority_animation_build_group_map_VA_0083CA00` | Membership chooses transform-in versus transform-out group for each skeleton transform; confirmed |

## 2026-09-23 deployed skeleton reader and pose route

| Address | Stable provisional name | Evidence / status |
|---:|---|---|
| `0x0082B320` | `basic_skeleton_read_SKTM_v0002_VA_0082B320` | Exact INFO/NAME/PRNT/RPRE/RPST/BPTR/BPRO/JROR order and count-derived widths; confirmed |
| `0x0082AC30` | `basic_skeleton_read_SKTM_v0001_VA_0082AC30` | Older variant includes BPMJ; confirmed route, no effective v0001 sample |
| `0x0082BAE0` | `basic_skeleton_build_inverse_bind_chain_VA_0082BAE0` | Three quaternion inputs, BPTR translation, inverse and parent accumulation; output label strongly inferred |
| `0x00ABA9F0` | `quaternion_multiply_scalar_first_VA_00ABA9F0` | Component formula and scalar-first ordering confirmed |
| `0x00ABA530` | `quaternion_to_rotation_matrix_VA_00ABA530` | Direct 3×3 coefficient formula confirmed |
| `0x00AB93B0` | `rigid_transform_inverse_VA_00AB93B0` | Transposed rotation and negative rotated translation confirmed |
| `0x00AB9390` | `transform_multiply_dispatch_VA_00AB9390` | Indirect optimized/scalar backend; scalar contract `destination = left * right` confirmed via `0x00AB8FC0` |

## 2026-09-23 compressed keyframe animation (CKAT) route

| Address | Stable provisional name | Evidence / status |
|---:|---|---|
| `0x00825710` | `compressed_keyframe_template_ctor_load_VA_00825710` | CKAT version dispatch and root-motion optional-mode selection; confirmed |
| `0x00825B30` | `compressed_keyframe_template_load_0001_VA_00825B30` | INFO, XFRM, AROT, SROT, ATRN, STRN, MSGS, LOCT and optional QCHN reader; confirmed |
| `0x00824870` | `compressed_keyframe_quaternion_channel_load_VA_00824870` | U16 key count, three quantizer selectors, packed U32 keys, runtime interval setup; confirmed |
| `0x00824B80` | `compressed_translation_channel_read_CHNL_VA_00824B80` | U16 key count and U16/F32 records; confirmed |
| `0x00822FC0` | `compressed_animation_runtime_construct_VA_00822FC0` | Skeleton-name-to-XFIN match and per-transform evaluator creation; confirmed |
| `0x00825310` | `compressed_animation_find_transform_XFIN_VA_00825310` | Binary search by transform name; confirmed |
| `0x00825430` | `compressed_animation_decode_static_SROT_VA_00825430` | XFIN index to SROT table; instruction-level receiver verified |
| `0x00825410` | `compressed_animation_select_animated_AROT_VA_00825410` | XFIN index to AROT channel; confirmed |
| `0x00822710` | `compressed_animation_register_translation_dispatch_VA_00822710` | Installs eight axis-mask variants; confirmed |
| `0x00822330` | `compressed_translation_channel_sample_linear_VA_00822330` | Cached interval and linear F32 interpolation; confirmed |
| `0x00824F00` | `compressed_quaternion_channel_sample_VA_00824F00` | Key interval, packed decode and quaternion interpolation; confirmed |
| `0x00AC0D90` | `packed_quaternion_decode_11_11_10_VA_00AC0D90` | Quantizer selectors, signed magnitudes, fourth-component reconstruction; confirmed |
| `0x00AC0E20` | `packed_quaternion_quantizer_table_install_VA_00AC0E20` | SharedMath-init table build; exact numerical formula now resolved |
| `0x00825070` | `compressed_LOCT_position_sample_linear_VA_00825070` | Cached 3D linear interpolation; confirmed |
| `0x00826660` | `compressed_root_motion_identity_VA_00826660` | No optional channels; confirmed |
| `0x008266B0` | `compressed_root_motion_rotation_only_VA_008266B0` | Optional top-level QCHN; confirmed |
| `0x00826930` | `compressed_root_motion_translation_only_VA_00826930` | Optional LOCT; confirmed |
| `0x008267A0` | `compressed_root_motion_rotation_and_translation_VA_008267A0` | Both optional channels; combined coordinate frame tentative |
| `0x00824340` | `compressed_animation_read_MESG_VA_00824340` | Named message group with U16 occurrence frames; confirmed |
| `0x00822F30` | `compressed_animation_collect_timed_messages_VA_00822F30` | Virtual message query and loop-time conversion; confirmed route, exact boundary rules pending |

## Ground HUD window-manager composition (2026-09-24)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00E34BE0` | `ground_hud_window_manager_construct_VA_00E34BE0` | Binds named RadarPage/AllTargets/AttribMod/MfdStatusPage/group children; confirmed |
| `0x00E35090` | `ground_hud_window_manager_destroy_VA_00E35090` | Releases radar/attribute child references and calls base destruction; confirmed |
| `0x00E357D0` | `ground_hud_window_manager_update_VA_00E357D0` | Procedural-terrain/survey visibility gate then base update; confirmed route, refresh internals open |

Existing `0x00E35550` retains its stable name `survey_listener_dispatch_messages_VA_00E35550`; the new checkpoint broadens its documented role to tutorial-HUD and scene-change dispatch without minting a duplicate alias.

## Data Storage POI provider (2026-09-24)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00E9F870` | `poi_container_provider_construct_VA_00E9F870` | Initializes provider and object-reference collections; confirmed |
| `0x00E9F930` | `poi_container_provider_destroy_VA_00E9F930` | Releases reference collections and base; confirmed |
| `0x00E9FF80` | `poi_container_provider_apply_index_permutation_VA_00E9FF80` | Size/range-validated local reordering and change notice; confirmed; persistence unknown |

Existing `0x00D414C0` retains its stable name `swg_cui_data_storage_construct_VA_00D414C0` from the 2026-09-22 Data Storage checkpoint.

## Space HUD window-manager composition (2026-09-24)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00E21B40` | `space_hud_window_manager_construct_VA_00E21B40` | Binds six space-specific HUD child pages; confirmed |
| `0x00E22030` | `space_hud_window_manager_destroy_VA_00E22030` | Releases four retained child references and calls base; confirmed |
| `0x00E220E0` | `space_hud_window_manager_dispatch_VA_00E220E0` | Two-key tutorial HUD branch then base dispatch; confirmed route, literals open |

## IME input mediator (2026-09-24)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00CA8D00` | `swg_cui_ime_input_construct_VA_00CA8D00` | Binds `listCandidate` and registers `IMEInput`; confirmed |
| `0x00CA8EB0` | `swg_cui_ime_input_activate_VA_00CA8EB0` | Candidate-overlay placement and UI registration; confirmed route, geometry semantics partial |
| `0x00CA91B0` | `swg_cui_ime_input_deactivate_VA_00CA91B0` | De-registers overlay and restores active input behavior; confirmed route |

## Dynamic IMM32 text-composition bridge (2026-09-24)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x009B4FF0` | `ime_platform_initialize_once_VA_009B4FF0` | Calls dynamic loader, captures keyboard layout and registers callbacks; confirmed |
| `0x009B5540` | `ime_platform_resolve_system_dll_exports_VA_009B5540` | Loads IMM32 and VERSION from system directory, resolves named APIs; confirmed |
| `0x009B51F0` | `ime_platform_dispatch_win32_messages_VA_009B51F0` | Routes IME composition/candidate Win32 message codes to local readers/events; confirmed |
| `0x009B5FF0` | `ime_platform_read_unicode_composition_VA_009B5FF0` | Calls `ImmGetCompositionStringW` and copies UTF-16 state; confirmed; full flag behavior open |
| `0x009B6310` | `ime_platform_enumerate_unicode_candidates_VA_009B6310` | Calls `ImmGetCandidateListW` and converts offset-addressed UTF-16 entries; confirmed; UI sink edge open |

## Controls menu and cluster selection (2026-09-25)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00CEC180` | `swg_controls_menu_activate_VA_00CEC180` | Clones working input scheme and populates controls/presets/categories; confirmed |
| `0x00CECE90` | `swg_controls_menu_commit_and_save_VA_00CECE90` | Copies working scheme, saves, reports failure without closing; confirmed |
| `0x00CED470` | `swg_controls_menu_modal_chat_confirmation_VA_00CED470` | Applies scene-scoped toggle only on accepted message-box results; confirmed |
| `0x013DC0B0` | `controls_bind_entry_validate_and_commit_VA_013DC0B0` | Enforces input mask, conflict confirmation/removal and emits change; confirmed |
| `0x013DC500` | `controls_bind_entry_message_box_continue_VA_013DC500` | Clears retained box and retries accepted confirmation; confirmed |
| `0x00C7A180` | `swg_cluster_selection_activate_VA_00C7A180` | Connects status/result transceivers, resets connection and rebuilds table; confirmed |
| `0x00C7A3C0` | `swg_cluster_selection_deactivate_VA_00C7A3C0` | Disconnects transceivers/callbacks and clears pending/table state; confirmed |
| `0x00C7A730` | `swg_cluster_selection_rebuild_table_VA_00C7A730` | Projects login-manager cluster records into status/population/distance rows; confirmed route |
| `0x00C7B220` | `swg_cluster_selection_request_selected_VA_00C7B220` | Validates selected record, opens attempt UI and hands endpoint to login manager; confirmed |
| `0x00C7B920` | `cluster_connection_result_apply_VA_00C7B920` | Existing stable name; rejects false/unsolicited/wrong-cluster results and enters avatar selection on matched success; confirmed |

## GameScheduler lifecycle (2026-09-25)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x013E5D80` | `game_scheduler_queue_construct_VA_013E5D80` | Creates deferred vector, deadline heap, clock and drain flag; confirmed |
| `0x013E60A0` | `game_scheduler_enqueue_callback_VA_013E60A0` | Allocates callback/context event at current time plus delay; confirmed |
| `0x013E6110` | `game_scheduler_advance_and_invoke_due_VA_013E6110` | Invokes/destroys due callbacks then merges reentrant insertions; confirmed |
| `0x013E5DF0` | `game_scheduler_queue_destroy_VA_013E5DF0` | Frees queued events without invoking callbacks and destroys both containers; confirmed |
| `0x00D83720` | `ticket_purchase_delayed_location_callback_VA_00D83720` | Frees 12-byte context then performs delayed point/location work using copied generation/index/raw-owner values; exact owner-retention guarantee unknown |

## Playback-template fetch reachability (2026-09-25)

| Address | Stable provisional name | Evidence and limits |
|---:|---|---|
| `0x00402FA0` | `playback_template_cache_fetch_VA_00402FA0` | Existing stable name; complete static xrefs now enumerate fixed install, combat preload/runtime and generic instantiate routes; confirmed |
| `0x005F4D60` | `game_playback_script_install_fixed_templates_VA_005F4D60` | Fetches only posture-align and restore-scale fixed literals; confirmed |
| `0x00469E80` | `combat_playback_prefetch_templates_VA_00469E80` | Fetches every path retained from the combat manager table; confirmed |
| `0x004030E0` | `playback_template_fetch_and_instantiate_VA_004030E0` | Generic caller-supplied path remains a future/debug reachability route; confirmed API shape |


## APPR/PIVT distance-origin closure — 2026-09-30

See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md). Primary extent ownership and center offset are confirmed statically; original malformed extent failure policy remains bounded uncertainty.

| Address | Canonical name | Evidence/confidence |
|---|---|---|
| `0x00B2C2F0` | `appearance_extent_center_or_zero_VA_00B2C2F0` | Confirmed: appearance virtual returns primary extent center at +8 or initialized zero vector; 2026-09-30 APPR/PIVT checkpoint |
| `0x00788300` | `detail_appearance_template_extent_VA_00788300` | Confirmed: DTLA extent virtual fetches template primary extent at +0x0C; 2026-09-30 APPR/PIVT checkpoint |
| `0x01271450` | `sphere_extent_load_sphr_v0001_VA_01271450` | Confirmed: SPHR center vector and F32 radius stored at +8/+0x14; 2026-09-30 APPR/PIVT checkpoint |
| `0x012703F0` | `extent_factory_null_VA_012703F0` | Confirmed: registered NULL extent factory consumes form and returns zero; 2026-09-30 APPR/PIVT checkpoint |
| `0x0125F690` | `box_extent_load_v0001_VA_0125F690` | Confirmed: EXBX/0001 dispatches EXSP then maximum/minimum BOX reader; 2026-09-30 APPR/PIVT checkpoint |


## Portal implementation correction — 2026-09-30

The [portal reader checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LAYOUT_READER.md) corroborates deployed readers against all 291 layouts. Helper 00B43E80 toggles the second reference byte; its older attribution as neighbor resolution is superseded. CELL v5 uses optional extent factory 01270770. Complete post-load flag/link semantics remain open. Canonical names/counts are unchanged.


## CSHD observations — 2026-09-30

Static/corpus observations at0077C7D0 (version dispatch),0077CE70 (0001 loader),0077BD80 (TX1D),0077C020 (PAL),0077D640 (exact-name/namespace variable lookup),0077BEE0 (clamped texture choice) and007859B0 (private/shared-owner runtime lookup) are consolidated in the [owned CSHD contract](docs/CUSTOMIZABLE_SHADER_DECODER_CONTRACT.md). No provisional name was added or repurposed; complete registration/lifecycle remains open.


Palette fallback label correction (2026-09-30): original00AB69FC writesFF00FFFF and import00AB8135–00AB8182 confirmsFFRRGGBB packing. [Owned palette contract](docs/PALETTE_DECODER_CONTRACT.md). Stable names/counts unchanged.


### Floor movement caller refinement — 2026-09-30

The [single-cell contract](docs/SINGLE_CELL_FLOOR_MOVEMENT_CONTRACT.md) records the existing 0x012647B0 / 0x01267380 result branches and the subordinate wrapper/classifier/predicate chain. No new semantic name was added: special collision modes and exact enum names remain unknown. The constant at 0x015FFCA4 is FLT_MAX, not normalized progress one. Stable-address count remains 2,590.

## 2026-09-30 — Cell transfer and entry permission refinement

| VA | Canonical name | Evidence / confidence |
|---|---|---|
| `00422CB0` | `cell_entry_permission_callback_VA_00422CB0` | Confirmed registered callback; state-bit 1B rejection, world/null-owner allow, CellObject+29C read. Actor-state semantics Unknown. |
| `00B2B3F0` | `cell_property_resolve_floor_portal_destination_VA_00B2B3F0` | Confirmed owner portal-vector indexing by local floor portal ordinal and linked CellProperty return. |
| `00B2BDB0` | `cell_property_query_entry_permission_VA_00B2BDB0` | Confirmed global callback dispatch; default true without callback. |

0126AA80 preserves the same wrapper. 01267380 boundary recursion is same-floor candidate continuation; its five-step bound does not establish a cross-cell bound. Actual owner continuation is 012670E0 / 00B2B3F0. Full actor/barrier ordering remains Unknown. See [contract](docs/PORTAL_FLOOR_TRANSFER_CONTRACT.md).

## 2026-09-30 — Portal barrier/controller refinement

| VA | Canonical name | Evidence / confidence |
|---|---|---|
| `012A4C40` | `portal_barrier_object_ctor_VA_012A4C40` | Confirmed BarrierObject RTTI, linked door +A8, dedicated collision property and padded extent construction. |
| `012A4E60` | `portal_barrier_create_padded_extent_VA_012A4E60` | Confirmed portal-frame bounds through B52DD0 expanded by F32 0.05 and assigned collision extent. |
| `012A50A0` | `portal_barrier_test_permission_block_VA_012A50A0` | Confirmed BarrierObject +64 predicate returns 1 when linked DoorObject +5C eligibility is false. |
| `012A4A30` | `door_controller_advance_fraction_VA_012A4A30` | Confirmed open/close-duration update, first transition-index selection, fraction clamp and active cleanup. |

The supported-version template fields are LeadingByte at +0, SolidByte at +1, WindingByte at +2. Only the PRTO/0002 loader calls B43E80 inversion. Permission blocking and active-controller candidate filtering are separate; collision mode 1 semantics remain Unknown. See [contract](docs/PORTAL_BARRIER_COLLISION_CONTRACT.md).


## 2026-09-30 — Single-leaf motion refinement

| VA | Canonical name | Evidence / confidence |
|---|---|---|
| `012901E0` | `door_object_compute_leaf_displacement_VA_012901E0` | Confirmed: calls primary fraction curve then multiplies authored movement at D0/D4/D8. |
| `01290B60` | `door_object_shape_displacement_fraction_VA_01290B60` | Confirmed: reciprocal springiness power and F32-pi cosine blend with smoothness. |

Constructor 0128FF40 stores squared 1.5-times-radius retention distance; trigger 01290560 uses authored radius at local (0,1,0). These refine earlier generic proximity/motion summaries. Original physical leaf collision extent choice and global actor ordering remain Unknown. See [contract](docs/SINGLE_LEAF_DOOR_CONTRACT.md).


## 2026-09-30 — Interior door integration boundary

The [integration contract](docs/INTERIOR_DOOR_MOVEMENT_CONTRACT.md) reuses existing door/permission evidence and adds original POB/floor instance validation. No new executable semantic names: canonical registry remains 2,599. Original general actor collision ordering, physical narrow-phase selection and legacy paired-controller coordination remain Unknown. Modern source-contact ordering and directional instance tests do not close those executable questions.


## 2026-09-30 — Persistent actor implementation seam

The [persistent actor contract](docs/PERSISTENT_DOOR_ACTOR_CONTRACT.md) implements modern shared-leaf and outward-touch retreat policies. These do not resolve legacy paired-controller synchronization, original solver tolerances, moving-leaf overlap or global collision ordering. No address naming change; registry remains 2,599.

## 2026-09-30 — Modern fixed-step input boundary

The [timing contract](docs/FIXED_STEP_ACTOR_CONTRACT.md) adds replacement timing/input policies. Original timer behavior, paired-controller synchronization, narrow-phase ordering and moving-leaf overlap remain unresolved; no naming change (2,599 rows).

## 2026-09-30 — Input timeline policy

The [timeline](docs/ACTOR_INPUT_TIMELINE_CONTRACT.md) defines modern tick-start quantization; original input timestamp/quantization semantics remain Unknown. Canonical names remain 2,599.

## 2026-09-30 — Detail-leaf envelope boundary

The [capability matrix](docs/DOOR_STYLE_CAPABILITY_MATRIX.md) adds modern DTLA root-box support. Original physical extent selection/narrow phase and secondary flip/controller coordination remain Unknown. Canonical names remain 2,599.

## 2026-10-01 — Static mesh collision dispatch

[Selection specification](docs/STATIC_MESH_COLLISION_SELECTION.md) records confirmed observations at runtime dispatch00793640, primitive query00770BA0, selector01269040, shader getters0077FF60/0077FF90 and first-pass predicate0075E4E0. No canonical semantic names added (2,599 rows). Original actor selector assignment, bypass enabling caller policy and receiver intersection tolerances remain Unknown.


## 2026-10-01 — Actor extent participation

[Durable boundary](docs/GROUNDED_ACTOR_COLLISION_PARTICIPATION.md) records confirmed transformed extent accessor012A09C0, object-pair dispatcher012A0AB0/012A12D0, RTTI SimpleExtent constructor01293CB0 and refresh01260E40. Inspected body/floor paths01267A90/01266960 remain distinct from MeshAppearance segment queries. No canonical names added; shape-source ownership, controller reachability and global ordering remain Unknown.


## 2026-10-01 — Creature fallback fields

[Fallback evidence](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md) maps stepHeight011AF1B0, radius011AF8D0, X/Z offsets011B05B0/011B0940 and length011B0CD0 through loader011B13F0 and property configuration012604D0. No canonical names added. Controller reachability, flag transitions and full template parameter semantics remain open.


## 2026-10-01 — Original template parameter codecs

[Object profile contract](docs/OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) records float loader013FA200 at vtable01609AB8 and string loader013FADA0 at01609B48, with inheritance versus literal modes. No canonical names added; complete random/relative/default semantics remain outside the deterministic slice.


## 2026-10-01 skeleton name normalization observation

Verified baseline0082B320 constructs transform names with00AA4050(name,1). Direct instructions trace through00AA4320/00AA4250/00AA2D10 to0131D6E8, whose default ASCII branch folds A-Z by+0x20. Locale branch is separate. This narrows original human head/Head attachment matching; no new canonical names or arbitrary CRC-map equivalence. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKELETON_DESCRIPTORS.md).


## 2026-10-01 deployed MGN prefix observation

Existing canonical skeletal_mesh_generator_load_v0004_VA_00800EF0 now has bounded INFO/SKTM/XFNM/POSN/TWHD/TWDT/NORM width/count evidence and original body/head agreement. Influence-group/index-weight interpretation is Strongly inferred; primitive/generation consumers remain next. No canonical rename. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKIN_SOURCES.md).


## 2026-10-01 skeletal shader primitive observations

007FCB50 reads PSDT maps/coordinates and dispatches ITL007FB380 or OITL007FBA00. Both map shader-local triangle indices through PIDX to POSN and omit exact zero-area triangles; OITL retains leading I16 field. 007FD900 only stores two words and does not close weighted skinning semantics. No new canonical names. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_MGN_GEOMETRY.md).


## 2026-10-01 skeletal influence consumers

[Skeletal influence consumers](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_SKELETAL_INFLUENCE_CONSUMERS.md) confirms TWHD/TWDT position groups, complete transfer without four-weight truncation, and PIDX-selected influence ownership for NIDX normals. Weighted position/direction evaluator and direct normal output traced; animated matrix producer remains open. Metrics unchanged.


## 2026-10-01 logical animation tables

[Logical animation tables](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LOGICAL_ANIMATION_TABLES.md) independently matches14 original sources/710,463 bytes and1,046 serialized entries. Four duplicated body names retained; authored/synthetic default and opaque typed subtrees validated. Selected clip/proxy execution remains next; metrics unchanged.


## 2026-10-01 animated local composition observation

[Pose contract](docs/SKELETON_ANIMATION_POSE_CONTRACT.md) ties0082B8D0/E0/F0/900 to RPRE/RPST/BPTR/BPRO and confirms007E6917..007E6995 order RPST*(sample*BPRO*RPRE), with BPTR plus sample translation.00822FC0 iterates requested skeleton names, so extra clip names need not acquire body joints. No canonical names added; original inverse-palette cache/segment ownership remains separate.


2026-10-01 inline face effect observations:0075D490 IMPL0003 DATA-before-OPTN;00763D10 PTXM dispatcher/00763D90 v0000 consumes16 bytes;62A0B340 PASS+20 cached blend,+28/+2C blend-table indices;62A04EC0/62A04EE0 cache blend/write bytes,62A05620 draw branch applies states27/168. Existing semantic names preserved; no new provisional labels or coverage count. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md). Effective blueprint target-alpha global mode remains unknown.


2026-10-01 preparation observations:0086D490 SST1 loader passes Boolean1 to0086E370, which forwards name/Boolean to00870510 and factory construction0086C680.0086DF70 negative/high palette index selects last entry;0086D5E0 texture index clamps to endpoints. Existing names preserved; semantic meaning of Boolean remains unknown. [Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_EXPLICIT_FACE_PREPARATION.md).


## 2026-10-01 Face camera and target observations

[Face execution boundary](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_EXECUTION_BOUNDARY.md) traces PCAM0086C380/00768620/00767700, sampler inheritance62A0E570, managed destination00869CD0/ARGB8 scratch62A0AF40 and original shared pixel-program output. Corrected selected DEST remap;51-source/2,283,555-byte original dependency/instruction audit and negative/ownership gates pass. Godot build passes. Global opacity and exact raster parity remain open; coverage metrics/canonical names unchanged.


2026-10-01 namespace relationship:00872C90 registration and00873190 value update map true to01924C80 `/private/`, false01924C70 `/shared_owner/`;00872A00/00872B10 initialize the strings from0188F570/0186B880. This matches CSHD007859B0 and closes the BTRT domain relationship without repurposing existing factory names. [Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SHARED_CUSTOMIZATION.md).


## 2026-10-01 skeletal blend execution observations

Baseline identity unchanged.007FAA00 constructs shared-owner BLT names;008031A0 applies positions before weighted transfer and normalizes all normals when any target activates.008036C0 applies/normalizes tangent XYZ;007F9F20 preserves W.00AB5C40 leaves directions below the documented tiny-length threshold unchanged. [Canonical blend contract](docs/SKELETAL_BLEND_SHAPE_CONTRACT.md) records exact sites/constants, confidence and prototype limits. No semantic name repurposed or canonical registry change; factory range/defaults and HPTS semantics remain unresolved.

## Material component reconciliation —2026-10-01

Direct007843B0/00A95860 and backend62A0E9D0/62A0EF30 now establish ambient/diffuse/emissive/specular ARGB serialized order, power and RGBA shader constant mapping. [Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SPECULAR_EQUATIONS.md). No provisional semantic names added or repurposed; complete light selection remains open.

## Camera zoom scale observation —2026-10-01

0x00503400 reads F32 global0x01911180; configuration instructions0x0050407E..0x00504092 load ClientGame/freeChaseCameraMaximumZoom with default1. Free-chase constructor multiplies authored ZOOM values by this getter. [Camera profile contract](docs/FREE_CHASE_CAMERA_PROFILE_CONTRACT.md). No canonical names added or repurposed; live override state remains unclaimed.

## Original CELL lights —2026-10-01

| Address | Canonical name | Observation/confidence |
|---|---|---|
| `0x00B451E0` | `portal_cell_template_load_v0005_VA_00B451E0` | Confirmed version dispatcher target and signed-count/93-byte LGHT parser; two ARGB colors, transform, ordered attenuation and pi local yaw. |
| `0x006E5AA0` | `portal_cell_light_record_create_VA_006E5AA0` | Confirmed record factory maps0/1/2 to runtime ambient0/directional1/point3; preserves point attenuation and directional/point poses/colors. |

Consumer0x006E5D60 obtains the record vector through0x00B45A40 (template+0x14), creates primary lights and adds a separate participation-light when second RGB is nonzero. Selected three-room source19 records create24 such objects. [Contract](docs/PORTAL_CELL_LIGHT_CONTRACT.md). Two canonical names added; no function-coverage percentage follows.

## Original bounded light selection —2026-10-01

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_LIGHT_SELECTION.md):25 hash-gated instruction anchors close baked/unbaked masks,1+2 directional/1+4 point slots, absolute-RGB ranking and channel roles. Twelve original/64 synthetic independent selection cases pass. Live visibility/order remains Unknown; baked room over-lighting correction next. Metrics unchanged.

## Point constant packing closure —2026-10-01

Existing backend0x62A0A3C0 is now tied to [original source attenuation](docs/LEGACY_POINT_ATTENUATION_CONTRACT.md):20 hash-gated anchors confirm coefficient offsets+0x100/104/108, fourth-zero writes0x62A0A78D/845, empty-point constant1 and28-vector c16 upload. No new names or address meanings are inferred from external LIT documentation; compiled high-power behavior remains Unknown.

## Light-scaling configuration snapshot —2026-10-01

`0x00779E50` is canonically `client_graphics_light_scaling_enabled_VA_00779E50`: getter for global0x01923544, written from [ClientGraphics] enableLightScaling(default true) by existing0x00779A50. Both StaticShader constructors capture engine shader+0x12; copies preserve it and0x62A046B0 selects scaled/raw RGB/scores. Prior generic graphics-data wording is superseded by this engine-object owner evidence. [Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIGHT_SCALING_SELECTOR.md). Current canonical count2602; no function-coverage claim.

## 2026-10-02 — integer-vector kernel correction

Existing canonical autodelta_vector_int_delta_decode_VA_0043B1A0 is unchanged. Direct pinned decoder/helper instructions now establish unsigned min((N-C+L) mod2^32,N), assignment k+1 cursor units and mutation-dependent local counters without forced incoming publication. Confirmed kernel mechanics; downstream callback effects and unsafe recovery remain unresolved. [Evidence and bounded replacement policy](docs/CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md). No new addresses/names or denominator change.

## 2026-10-02 — integer-set kernel and normalization

Existing canonical autodelta_set_int_snapshot_parse_VA_00642F00 and autodelta_set_int_delta_parse_VA_00643050 retain their names. Pinned direct instructions establish raw-counter copy, signed unique tree/in-order serialization, conditional unsigned sum gate, membership-dependent increments and final unsigned maximum. [Exact components contract](docs/CREATURE_VIEW3_COMPONENTS_DELTA_CONTRACT.md) separates raw snapshot preservation and modern atomic bounds from original callbacks/unsafe recovery. No new names, packet boundaries or denominator changes.

## Round19 appearance field4 callback observations — 2026-10-02

Pinned executable SHA256 was rechecked as the documented E782BF1C...6150.
[Ingress evidence](docs/CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) connects
existing canonical00641030/00641180 decoders to nonempty callback0063F880,
unescape/version dispatch00B33020/00B33E30 and record reader00B33F70.
Records resolve ID&7F through00B54D50, named variable00B32890 and setter
vtable+10; setter conversion/clamping/getter semantics remain Unknown. These
are Confirmed static address/call observations, not newly assigned canonical
names. Full input hash, instruction ranges and confidence are in the checkpoint.
Empty callback bypass, ordered mutation and absent success-tail check are retained
as original observations. Atomic strict inner rejection would be replacement
policy. Next exact probe: concrete ranged/palette setter targets and widths.

## Round20 concrete customization observations — 2026-10-03

Pinned executable instructions/RTTI identify BasicRangedIntCustomizationVariable vtable015EC8C4 and PaletteColorCustomizationVariable015EC920 sharing packed reader00B49FD0 (+10). It converts U8/LE S16 and calls integer setter+20. Basic00B4ADF0 and palette00B4B8E0 retain out-of-range local integers; getters00B4ADE0/00B4B8D0 return stored fields. Both still forward attempted values through00B4A270;00B4A300 invokes owner notification. Palette color lookup00B4B920 separately clamps lookup. [Confirmed observations and remaining context](docs/PACKED_CUSTOMIZATION_CONTRACT.md); no new canonical semantic names or registry count change.

## Round27 SWTS/DTST reconciliation — 2026-10-03

Pinned original static instruction/RTTI/asset audit establishes SWTS loader/runtime and DTST local selector roles.18 canonical addresses are reconciled,16 newly registered:2618 distinct names/zero duplicates. 00785C30/50 formerly carried SwitchShader_forwardSelectedOperationA/B names; the indexed forwarding structure remains correct, but RTTI places these functions in SwitchTextureShader. Canonical names now shader_swts_forward_selected_operation_a/b_VA_00785C30/50. SWSH corresponding forwarders are007860A0/C0; they remain address observations, not extra registered aliases. Historical interpretation and exact evidence are preserved in the [R27 checkpoint](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R27_SWITCH_TEXTURE_RUNTIME.md) and [runtime contract](docs/SWITCH_TEXTURE_RUNTIME_CONTRACT.md). Outer clock units, pause/culling/thread and first visible frame remain Unknown. No function-coverage denominator is inferred.

## Round28 specialized vector and clock reconciliation — 2026-10-03

Three new canonical roles: CachedNetworkId vector snapshot serializer00641690, delta serializer00641700 and eight-byte archive helper0043CEB0. Existing006421A0/006423E0 parser names remain. [Pinned specialization evidence](docs/TANGIBLE_VIEW6_CODEC_CONTRACT.md) establishes consume grammar and assignment overshoot; counter/state application remains the next probe. Registry2621 distinct addresses/zero duplicates, no function-coverage denominator change.

[Clock chain](docs/SWITCH_TEXTURE_CLOCK_CONTRACT.md) establishes ordinary GroundScene scheduler-accumulated seconds and update-before-render ordering. Reachable installer/frame-pump configures PC00/RC00; actual callback control word, first visible frame, alternate owners and full pause/culling behavior remain unobserved.

## Round29 specialized identity state reconciliation

Five roles adopted after the pinned 109-anchor/two-RTTI audit: default construction00B2FCC0, raw-bit equality00B2FFF0, resize00643CD0, assignment00641D20 and clear00641850. [State policy](docs/TANGIBLE_VIEW6_STATE_POLICY.md) distinguishes counter mutation, default-zero growth, observer ownership and callbacks from modern atomic publication. Registry2626 distinct addresses, zero duplicates; function coverage remains uncalibrated.

## Round45 accepted compatibility increment

The [Round45 report](research/sessions/PARALLEL_DEVELOPMENT_ROUND_45_2026-10-04.md) records independently reviewed/replayed supplied
AHFR/FFRA consumers, an incarnation-bound received-only partial CREO4 store,
one human movement-template input chain and the actual current-height FHGT/AHCN
Godot preview. The store accepts pending/finalized objects without default seeds
but has no world/router admission yet. Original fractal generation/cache lifetime,
floating controls, complete terrain and later creature writers remain unknown.
Research scores and canonical name/packet denominators remain unchanged.

## Round46 accepted compatibility increment

Round46 adds bounded static descriptions without adding or renaming curated rows. Family signed lower-bound00B76420, exact lookup00B75DE0, unchecked inline sampler getter00B757A0, clear00B75A50 and MGRP loader00B75920 are described in [family lookup](docs/TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md). Sampler013E7C00, cache clear013E6B50, resize013E6AD0, normalization013E6DD0, noise013E66F0, seed initialization013E64D0, RNG00B59EF0 and integer helper0131E27C are correlated in [sampler evidence](docs/TERRAIN_FRACTAL_SAMPLER_CONTRACT.md). Last three bodies/table semantics remain incomplete. Supplied F32 does not reproduce wide miss return; same-key pointer reload/quiescence remains Unknown.2626 distinct names/zero duplicate rows unchanged.

## Round47 compatibility increment

The [conditional seed/noise record](docs/TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md) reuses canonical legacy_shuffled_park_miller_next_VA_00B59EF0 and closes bounded semantics at013E64D0/013E66F0/0131E27C:322 shuffle entries, rounded floating-index division,514-entry tables, signed64 truncation/restoration and asymmetric x/z storage.289 instruction anchors and520 fixed-width recurrence checks support the interpretations. No provisional names are added or renamed; original active controls/exception recovery/optional postprocessing remain Unknown.

## Round48 compatibility increment

[Movement writer evidence](docs/CREATURE_MOVEMENT_WRITER_CONTRACT.md) pins runtime readers004388A0/00438950, current slots+674/+660, generic local assignment00439B00 and caller00629F7E/8A. Indirect targeting/notification/global lifetime and units remain unresolved; no canonical semantic rename or registry row is added. Conditional noise code does not establish original worker controls.

## Round49 bounded normalization verification and implementation (2026-10-04)

SHA-gated013E6DD0 confirms retained weight sum/multiply at013E6DF8/FE and final F32 sum/reciprocal stores013E6E06/12. This reinforces the existing Round46 observation and introduces no new provisional address/name. Conditional software sampler and derived current-input implementation do not recover original controls, optional helpers, related-object arbitration or final controller units. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R49_SAMPLER_AND_SOURCED_HOST.md). Canonical2626-name registry unchanged.

## Round50: owned families and current movement inputs (2026-10-04)

Four Low milestones are integrated: owned MGRP/0000 sets, explicit sampler modes0..5, current field7/11 admission and the original controller receiver/consumption chain. Lead independently replays the focused suites and2592 actual family/sampler/consumer compositions; current-field11 original-resource headless/GPU and nonfinite rejection pass. The current host remains an explicit modern planar policy; optional fractal curves/sine, original controls, controller flag/helper semantics and full terrain/movement parity remain open. [Lead checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R50_CURRENT_INPUTS_AND_FRACTAL_FAMILIES.md) and [round record](research/sessions/PARALLEL_DEVELOPMENT_ROUND_50_2026-10-04.md). Research89/readiness94 heuristics,2626 names and345 packet rows are unchanged; no implementation-completion percentage is inferred.

## Round51: generator routing and controller gates (2026-10-04)

The owned generator now publishes exact fractal families into a fresh family-routed height-affector slice;486 complete native joins pass. Controller evidence identifies the run-default preference/local inversion and closes the direct acceleration helper, while getter sampling, input-message timing and units remain open. [Lead checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R51_GENERATOR_ROUTING_AND_CONTROLLER_GATES.md) and [round record](research/sessions/PARALLEL_DEVELOPMENT_ROUND_51_2026-10-04.md). Conservative generator-copy reservation and fresh-call cache policy are explicit modern limits. Research89/readiness94 weighted heuristics,2626 names and345 packet rows remain unchanged and separate from client implementation completion.

## 2026-10-04 Round52 controller prerequisites

[Indexed acceleration getter011ADD30](docs/CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md) and endpoint013FA1E0 have complete bounded static bodies, indexed/inherited/relative storage evidence and one original serialized record. [Run producer](docs/PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md) establishes one deactivation-slot0x99 enqueue boundary. No canonical names change. Remaining exact questions: one controller update-argument caller/units and selected InputMap queue/end-binding caller; transitive global/lifetime and arithmetic controls remain separate.

## 2026-10-04 Round53 bounded carriers

[Controller-unit evidence](docs/PLAYER_CONTROLLER_UPDATE_UNITS_CONTRACT.md)
closes common Object alter00B23100 and wrapper00B3D1C0 forwarding to player
slot+14/00629750. Actual carrier across CreatureObject slot+10/00430260 remains
Unknown. [Ground normalized routing](docs/PLAYER_CONTROLLER_INPUT_QUEUE_BINDING_CONTRACT.md)
closes0051AF10's selected prefix through0051AA40/00AAE2E0 to00AAE150 family1;
non-null scene+4C acquisition and queue+0C/controller+4 equality remain Unknown.
No canonical address name is introduced or changed.


## Round54 accepted increment

[CreatureObject carrier](docs/CREATURE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md):00430260/0063F3B0 forward word/receiver to00554C60. [Input map assignment](docs/GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md):0051FAD0 uses getter00B3D340 identical to0062A9B0 and assigns current controller+4 into selected map+0C. No canonical names changed. Next00554C60/006385C0; full units/queue lifetime unknown.


## 2026-10-04 Round55 carrier and base lifecycle

[ClientObject carrier](docs/TANGIBLE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md):00554C60 forwards the unchanged word/receiver into00B23100 before branching. Player seconds are supported conditionally under the explicit scheduler/attachment/type/flag/normal-return premises. [Base queue lifecycle](docs/PLAYER_CONTROLLER_QUEUE_LIFECYCLE_CONTRACT.md):006385C0/006E4A20 forward construction into00B4AE50; queue layout, allocation, ownership and destruction timing remain Unknown. No canonical semantic names are introduced or changed.


## 2026-10-04 Round56 selected queue and curve boundaries

[Queue storage](docs/OBJECT_CONTROLLER_QUEUE_STORAGE_CONTRACT.md): NetworkController00B4AE50/00B4AEA0 forwards to Controller00B3D0F0/00B3D190; exact returned queue +4 writer/cleanup/null/order observed. Initializer00AA6050 and cleanup00AA6310 internals remain unclosed; no queue-class name inferred. [Curve finish](docs/TERRAIN_FRACTAL_CURVE_FINISH_CONTRACT.md):013E7C00 first/second finish stores/operands closed, wrapper0131E2B0 and denominator initializer bounded, power core0131E2D2 only partial. No canonical semantic names change.


## 2026-10-04 Round57 queue initializer boundary

[Initializer/cleanup](docs/OBJECT_CONTROLLER_QUEUE_INITIALIZATION_CONTRACT.md) closes00AA6050 normal-return receiver identity and00AA6310 selected record cleanup order. Five fields and16-byte arithmetic are observed; concrete queue class/payload ownership remain Unknown. Next00AA67C0/00AA6370. No canonical address names change. [First-curve prototype](docs/TERRAIN_FIRST_CURVE_FRACTAL_BATCH_CONTRACT.md) is modern binary64 approximation only; exact original power/control parity remains Unknown.


## 2026-10-04 Round58 queue record boundary

[Record initialization/cleanup](docs/OBJECT_CONTROLLER_QUEUE_RECORD_CLEANUP_CONTRACT.md) closes00AA67C0 direct store/return and00AA6370 captured bounds/16-byte stride/entry+8 virtual slot-zero argument1/call-then-zero order. Caller flag and queue receiver are unused locally. Concrete payload deletion/ownership and00AA6710 storage release remain Unknown. No canonical names change; exact original curve/control and full locomotion parity remain unclosed.


## 2026-10-04 Round59 record storage boundary

[Storage cleanup](docs/OBJECT_CONTROLLER_QUEUE_RECORD_STORAGE_CLEANUP_CONTRACT.md) closes00AA6710 with tiny00AA6040: exact entry+8 zeroing,16-byte traversal and unsigned128-byte route to00AC1640/012EA920; outer descriptor release remains separate. Concrete payload vtable/deletion, allocator internals and exception handler014BA44B remain Unknown. No canonical names change.

## 2026-10-04 Round60 entry producer boundary

[Queue entry producer](docs/OBJECT_CONTROLLER_QUEUE_ENTRY_PRODUCER_CONTRACT.md) closes00AA6480 normal-return entry construction/spare/growth/publication ordering and tiny initializer00AA6030. Entry+8 receives stack argument three; the optional queue+10 callback receiver is separately loaded and does not prove payload RTTI. Payload class/vtable/deletion/ownership, caller argument construction and exceptional recovery remain Unknown. No canonical semantic names or address counts change. Next exact caller probe is deferred until Levarris resumes.
