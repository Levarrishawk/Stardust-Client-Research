# Selected animation proxy boundary

LogicalAnimationProxyDecoder supports PXAT/0000 INFO NUL clip path with optional single retained priority metadata node, plus up to eight nested TSCL/0000 INFO F32 / typed child envelopes. Raw scale factors and priority metadata are owned; their runtime priority/state policies are not executed. Modern scales must be finite, positive, at most1,000 individually and combined product at most1e6. Selected path must be under appearance/animation. Unsupported versions/families/order/width/depth/paths reject.

Static proxy constructor00818ED0 reaches00819100, which reads INFO and optional priority factory00817A60. Time-scale constructor00810B70 reaches00810CF0, which reads F32 and generic child template. Envelope widths/order are Confirmed; modern scale limits and descriptor-product convenience are explicit policies.

Creature bundle optional animation selection requires owned tables, explicit table/name and clip SHA pin before lookup. Missing/null logical selection rejects rather than silently playing a fallback; duplicate logical lookup remains ambiguous. Only the selected clip is loaded once under the same cache and retained as owned IFF envelope. This stage accepts CKAT/0001 or KFAT/0003 selected envelopes; other historical versions are outside this bounded implementation. It does not decode or play channels.

Original selected body entry emt_standing_idle_ambient_1_ag has PXAT clip appearance/animation/all_b_idl_ambient_01.ans, retained PUNF priority names spice/spice and no scale wrapper. Effective clip priority30,15,671 bytes, SHA798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B. Object-root union15 unchanged sources726,134 bytes, one external lookup each. Independent original-byte table/proxy/path/envelope and all source pins agree; masked/wrong-pin and native malformed proxy/scale gates pass. Selected CKAT0001 INFO is16 bytes; channels remain next.

Native --skeletal-selected-animation uses the existing pinned human object/SAT/configuration inputs; current bounded fixture supplies the selected original logical name/clip pin. verify_skeletal_descriptors.py audits selected rows automatically. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SELECTED_ANIMATION_PROXY.md). Next: owned compressed channel/index structures, then quaternion decoding and evaluation before any animation claim.


Follow-up: [owned compressed channel structures](COMPRESSED_KEYFRAME_STRUCTURE_CONTRACT.md) now decode the selected CKAT0001 sample. The selected-envelope stage itself remains generic; playback is still unsupported.
