# Durable research documents

Current scope — October 14, 2026: this is research documentation. Any implementation choices or acceptance results below are historical evidence. Use [current status](../ANALYSIS_STATUS.md) for coverage and open questions; no engine choice or client development is prescribed here.


These documents consolidate conclusions that remain useful across analysis sessions. Dated evidence and investigative history live under `research/`.

## Foundation

- [Executable overview](EXECUTABLE_OVERVIEW.md)
- [Client architecture](CLIENT_ARCHITECTURE.md)
- [Client subsystems](CLIENT_SUBSYSTEMS.md)
- [Game loop](GAME_LOOP_ARCHITECTURE.md)
- [Scene lifecycle](SCENE_LIFECYCLE_ARCHITECTURE.md)

## Network and replicated state

- [Network architecture](NETWORK_ARCHITECTURE.md)
- [SOE transport protocol](SOE_TRANSPORT_PROTOCOL.md)
- [SOE transport vectors](SOE_TRANSPORT_TEST_VECTORS.md)
- [Login and zone state machine](LOGIN_AND_ZONE_STATE_MACHINE.md)
- [Packet-handler map](PACKET_HANDLER_MAP.md)
- [Protocol coverage and boundaries](PROTOCOL_COVERAGE_AND_BOUNDARIES.md)
- [Replication schema map](REPLICATION_SCHEMA_MAP.md)
- [Object-controller protocol](OBJECT_CONTROLLER_PROTOCOL.md)
- [Server/client compatibility risks](SERVER_CLIENT_COMPATIBILITY_RISKS.md)

## Assets and presentation

- [Asset pipeline](ASSET_PIPELINE.md)
- [Asset format catalog](ASSET_FORMAT_CATALOG.md)
- [Rendering architecture](RENDERING_ARCHITECTURE.md)
- [Appearance and animation](APPEARANCE_ANIMATION_ARCHITECTURE.md)
- [World and terrain](WORLD_TERRAIN_ARCHITECTURE.md)
- [Interior and portal runtime](INTERIOR_PORTAL_ARCHITECTURE.md)
- [UI architecture](UI_ARCHITECTURE.md)
- [Platform, input, and audio](PLATFORM_INPUT_AUDIO_ARCHITECTURE.md)

## Replacement planning and audit

- [64-bit port plan](64BIT_PORT_PLAN.md)
- [Engine-decision requirements](ENGINE_DECISION_REQUIREMENTS.md)
- [Function closure summary](FUNCTION_CLOSURE_SUMMARY.md)
- [Function uncertainty audit](FUNCTION_UNCERTAINTY_AUDIT.md)
- [Material analysis backlog](MATERIAL_ANALYSIS_BACKLOG.md)
- [Ghidra workflow](GHIDRA_WORKFLOW.md)
