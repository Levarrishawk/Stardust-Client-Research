# Memory Manager and Fixed-Size Pools

## Global allocator

Exported `MemoryManager::allocate` at `0x00AC1EB0` is a custom 32-bit heap allocator. Confirmed behavior:

- a zero-byte request is promoted to one byte;
- requested storage plus allocator metadata is rounded to a 16-byte boundary;
- returned user storage begins 16 bytes after the internal block header;
- allocation searches a free structure, attempts heap growth/recovery twice, and optionally dumps outstanding allocations before a fatal out-of-memory report;
- allocation and free maintain current/peak byte and allocation counters;
- `MemoryManager::free` at `0x00AC20C0` marks the block free, coalesces adjacent free blocks in both directions, and reinserts the result;
- both paths use the foundation synchronization wrapper;
- exported `MemoryManager::own` is a no-op in this build.

These details are implementation machinery, not an ABI to reproduce. They do explain pervasive assumptions about 16-byte alignment and 32-bit header/pointer arithmetic.

## `MemoryBlockManager`

`MemoryBlockManager` constructor `0x00AA2700` takes:

```text
name, sharedPoolAllowed, elementSize, initialCount, growCount, limit/policy
```

The last labels are partly provisional, but the mechanics are clear:

- element size is clamped to at least four bytes;
- shared managers are keyed by element size and reuse one underlying fixed-size pool;
- configuration `memoryBlockManagerForceAllNonShared` disables that sharing and gives a default initial count of 100 when no positive count was supplied;
- `allocate` and `free` are synchronized and update a live-allocation count at manager `+0x08`;
- destruction skips teardown while the process is already fataling;
- normal destruction refuses to destroy a pool with outstanding allocations;
- a shared pool is removed from the size registry only when its final manager owner is removed.

The executable contains 226 strings matching `memoryBlockManager`; after the handful of framework/configuration labels, the great majority identify class-specific pools. They cover network messages, scene objects, particles, animation objects, terrain chunks, UI widgets, filesystem request objects, collision extents and data-table cells. The exact list is in `memory_pool_names.txt`.

## Why the old design existed

This design reduced fragmentation and allocation overhead for many small, repeatedly created objects on a constrained 32-bit address space. Sharing by equal element size also reduced reserved-pool overhead. It did **not** encode gameplay behavior or wire layout.

## 64-bit replacement policy

- Do not port the allocator or depend on object byte sizes matching 32-bit sizes.
- Use the platform/runtime allocator initially; measure before adding pools.
- Add bounded arenas or modern fixed-size pools only for proven hot paths such as packet decode staging, particles or transient render submissions.
- Keep network/asset decoded structures independent of allocation layout.
- Use `size_t` for internal allocation sizes, but explicit fixed-width integers for file/wire fields.
- Preserve required alignment for SIMD/GPU upload data explicitly, not as an accidental allocator property.
- Track allocation budgets per untrusted input domain to prevent count/size amplification.
- Replace “fatal on OOM after dumping everything” with best-effort diagnostics and controlled shutdown; no recovery is promised after true process-wide exhaustion.

## Confidence

- **Confirmed:** alignment, header offset, coalescing, accounting, lock boundary, shared-by-size pool registry, force-nonshared behavior, live-count guard and breadth of pool use.
- **Tentative:** exact historical names for constructor integer policy arguments beyond element size and initial-count behavior.
- **Not required:** red-black/free-list implementation identity and object-size parity.

## Evidence

- `decompiled-memory-allocators/`
- `memory_pool_names.txt`
- exports `MemoryManager::{allocate,free,own}` and `MemoryBlockManager::{ctor,dtor,allocate,free,getElementSize}`

