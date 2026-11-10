# Auction query: second Unicode field and Core3 empty-filter subset

Question: are the two previously unlabelled S32 fields in the auction request
both actual original scalar fields? Stop condition: identify their actual
archive types and distinguish original-client grammar from server parsing.

Input is the hash-verified original baseline recorded in the
[durable contract](../../docs/PRECU_AUCTION_QUERY_CONTRACT.md). Tools: pefile,
Capstone, existing bounded instruction exporter, immediate-reference/string
readers and the new deterministic verifier. No dynamic original client, capture,
network query or server mutation.

The constructor registers two Unicode members. Its vtable thunks reach the same
U32-count/two-byte-unit primitive. The original UI's second getter returns empty;
Core3 instead parses the empty string's length as one S32 and would misalign a
nonempty string. The first scalar comes from the selected node's
ItemTemplateCrc property. These observations correct the prior claim of two
unknown scalar meanings; exact evidence, confidence and schema are consolidated
in the durable contract. The separate commodity leaf's U32 remains Unknown.

Reproducible bounded exporter ranges (inclusive):

- 00ACFFA0:00AD02EF, constructor and thirteen member registrations.
- 00ACEA50:00ACEA84, string write/read thunks.
- 011948B0:01194A09, counted UTF-16 primitive.
- 010BD350:010BD3B0, ItemGot/ItemTemplateCrc initializers.
- 010BF2D0:010BF35F, selected-node scalar getter.
- 010BF090:010BF0B5, primary and empty second-string getters.
- 004CFD00:004CFDEE, ordinary query wrapper tail.

The verifier passes two synthetic callback-alignment cases and pinned static
checks. This is a structural correction, not new whole-protocol or function
coverage. Dated September reports retain their original text with explicit
supersession notes. Packet-map terminology and the constructor observation are
reconciled; the 345-row server-source inventory and 2,626-name registry are
unchanged because message identity and canonical names did not change.

Next probe: implement the supported original query subset and original compact
response decoder in the source repository, with actual registered serialization,
truncated/invalid-index fixtures, NGE branch isolation and a bounded read-only
terminal browse. No purchase/listing action is required to accept browsing.
