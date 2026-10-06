# Compressed locomotion and messages contract

Engine-neutral `CompressedLocomotion` owns validated CKAT/0001 MSGS and translation-only LOCT suffix records. Joint sampling remains independent. It rejects top-level QCHN. The separate [absolute root-rotation importer](COMPRESSED_ROOT_ROTATION_CONTRACT.md) now samples that track without actor authority or relative/combined evaluation; accepting its raw tree is not execution support.

## Evidence and grammar

Confirmed prior executable evidence: loader 0x00825B30; message loader 0x008242A0/0x00824340; half-open collector 0x008245D0 and template 0x00825610; position interpolation 0x00825070; translation-only difference 0x00826930. See [disk messages](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_MSGS_DISK_CORPUS.md), [message interval](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_EVENT_WINDOW_BOUNDS.md), [root modes](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_LOCT_CORPUS_AND_ROOT_MODES.md) and [authored speed](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_FRAME_RATE_AND_LOCT_SPEED.md).

MSGS is a FORM with U16LE INFO group count then counted MESG chunks: U16LE occurrence count, NUL ASCII name, counted U16LE frames. Preserve groups and duplicate occurrences; reject descending or clip-outside frames. Integer collection uses [start,end), retaining end-frame events when explicitly queried through end+1. This supplies neither callbacks nor higher-level loop scheduling.

LOCT is a chunk: F32LE authored average speed, S16LE key count, counted 14-byte records U16LE frame + three F32LE positions. Require finite bounded positions/speed, at least two keys, strictly increasing clip-bounded times and exact width. These are replacement parser limits, not a claim about every original rejection branch. Sample explicit finite frames from zero through the clip end, clamp to first/last key outside their span, interpolate linearly between keys; translation delta is sample(second)-sample(first). Do not apply the delta to actor state here.

## Original selected human occurrence

Effective configured all_m.lat, SHA256 8AFC6F77B4D769EFA1EA6764D7D4E80FF236F4B917DD50DDAFD45253DC29F494, loop_standing contains SSAT gender. VAL maps m/o to child0 and f to child1; both contain SPAT/0000 with U8 INFO count3 and ordered children: mood SSAT, male walk PXAT, run PXAT. This establishes authored references/order; it does not execute gender/mood/speed-selection state or infer thresholds. Both movement proxies carry PUNF locomotion/locomotion.

| Clip | Effective priority | Bytes | FPS/end | LOCT keys | Authored speed | event_footstep frames |
|---|---:|---:|---|---:|---:|---|
| all_b_loc_walk_male.ans | 30 | 7509 | 30/33 | 34 | 1.5492503643035889 | 7,23 |
| all_b_loc_run.ans | 30 | 5843 | 30/22 | 23 | 5.132026672363281 | 6,17 |

Walk SHA256 FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6; run SHA256 72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4. Both have39 ordinary joint transforms and MSGS/LOCT suffixes, no top-level root rotation.

## Verification and limits

Native `--compressed-locomotion <owned-clip-file> <exact-SHA256>` validates input identity and emits owned keys/messages and every quarter-frame sample. `tools/python/verify_compressed_locomotion.py <archive-root> <configuration> <member> <native-rows> --configuration-sha256 <pin>` independently resolves effective archive bytes, checks fields/F32 bits and interpolation plus the speed invariant. Original222 samples agree with maximum1.6398468005718314e-7 at tolerance2e-6. Synthetic endpoint, ownership, translation difference, malformed width/count/time/float, truncation and unsupported-root checks pass.

Unknown: complete animation graph execution, original speed-node blend/transition policy, message scheduling at loop boundaries and effect delivery. Next: retain explicitly selected movement clips alongside idle, validate suffixes, and drive diagnostic playback from accepted movement while collision owns translation.

## Modern event cursor — 2026-10-01

[AnimationEventCursor](ANIMATION_EVENT_CURSOR_CONTRACT.md) consumes these owned message records with an explicit modern (previous, target] playback policy, loop boundary ordering, pause/reset and per-advance budgets. Its independent oracle and original walk/run checks pass. This does not replace the recovered integer [start, end) collector or establish original scheduler semantics. Effect/audio dispatch is a future integration boundary.

## Round75 root evidence reconciliation

[Scale-source and prior-application record](COMPRESSED_ROOT_SCALE_SOURCE_CONTRACT.md). The September25 ROOT_MOTION_OBJECT_APPLICATION report already establishes eligible CreatureController object-local rotation post-composition and local translation converted into parent space. Recent local-body Unknown statements did not replace that prior code conclusion. Round75 reconciles/audits existing evidence without fresh actor arithmetic replay or additional coverage. Constructor00822FC0 confirms runtime+4C lookup initialization; scalar storage ownership remains Unknown.

## Round76 root storage boundary

[Storage contract](COMPRESSED_ROOT_SCALE_STORAGE_CONTRACT.md). Lookup007D3790 confirms lazy object+10 container and returns node+20 interior storage on hit/miss. Miss prepares zero payload bits; effective float default0 is Strongly inferred until insertion007D40C0 is expanded. Original key/type/units/node stability/lifetime remain Unknown. Prior September25 actor closure is not repeated or recounted.

## Round77 root insertion boundary

[Insertion contract](COMPRESSED_ROOT_SCALE_INSERTION_CONTRACT.md). Insertion007D40C0 publishes a0x24 node and delegates payload construction to77-byte007D64B0; conditional source+10/destination+10 maps to node+20. Actual zero/default remains Strongly inferred; key/type/units/lifetime Unknown. No actor-chain repetition or function-name increment.

## Round78 scalar copy and key type

[Value construction](COMPRESSED_ROOT_SCALE_VALUE_CONSTRUCTION_CONTRACT.md). Value construction007D64B0 copies32 then-current bits source+10 to destination+10, composing to node+20. Constructed key CrcLowerString directly identified by installed-vtable RTTI. Source preservation across107-byte00AA3FE0/all alternate defaults remains unproved; effective default Strongly inferred and key contents/units/lifetime Unknown.

## Round79 key-base boundary

[Key-base contract](COMPRESSED_ROOT_KEY_BASE_COPY_CONTRACT.md). Key-base00AA3FE0 explicitly writes destination0/8/C, installs PersistentCrcString and has no local scalar+10 write. Virtual getter/initializer/setter effects remain Unknown; complete source-zero/reference/lifetime proof is not claimed. Conditional13-byte00AA41C0 getter is the next bounded probe.
