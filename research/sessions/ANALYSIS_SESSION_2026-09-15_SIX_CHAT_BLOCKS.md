# Six-Block Chat Protocol Session — 2026-09-15

Completed consecutively:

1. Friend online/offline update.
2. Room message delivery.
3. Room-list reconstruction.
4. Moderator-removal result and mutation.
5. Outgoing instant-message acknowledgement.
6. Incoming instant-message delivery.

The blocks connect six active Stardust serializers to exact executable descriptors, parsers, listener branches, and client-state/UI handlers. They also establish reusable replacement primitives: `ChatAvatarId`, bounded room records, room registry/tree reconciliation, pending tell sequences, ignore filtering, last-reply identity, and model-before-UI publication.

No client implementation or asset modification was performed.

Post-publication matrix: **220 documented classes, 125 reachable-gap candidates, and 50 remaining literal-opcode gaps**, improving from 214/131/56. Weighted whole-client research coverage is now **78%**.
