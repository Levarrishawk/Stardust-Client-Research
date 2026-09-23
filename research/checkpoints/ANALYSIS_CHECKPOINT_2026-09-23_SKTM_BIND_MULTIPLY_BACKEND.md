# Analysis checkpoint — SKTM final bind-transform multiplication

The prior SKTM bind-pose checkpoint left the final `Transform::multiply` convention open because `0x00AB9390` calls an indirect backend. That specific uncertainty is now closed for this executable.

`0x00AB8DD0` initializes the dispatch pointer `0x01945B04` to an optimized backend at `0x00AB9280`, or to scalar `0x00AB8FC0` when its CPU-capability predicate is false. The scalar backend has an explicit alias-safe branch and computes a 3×4 row-major affine product `destination = left * right`: for example, its first result row is `left[0]*right[0] + left[1]*right[4] + left[2]*right[8]`, and its first translation is `left[0]*right[3] + left[1]*right[7] + left[2]*right[11] + left[3]`. The optimized path is a performance alternative selected by the same dispatcher, not a different caller contract.

At `0x0082BAE0`, x86 pushes the parent/external transform as the third argument, the inverted local bind transform as the second, and the destination joint slot as the first before calling `0x00AB9390`. Thus the stored result is precisely:

```text
jointInverseBind[i] = inverse(localBind[i]) *
                      (parent[i] < 0 ? externalRoot : jointInverseBind[parent[i]])
```

The local bind rotation is built by the previously confirmed scalar-first quaternion route `RPST * BPRO * RPRE`, then combined with `BPTR` translation before inversion. This formula describes the observed client matrix representation; it should be validated against fixture poses when converting to a renderer with a different row/column-vector convention. The multiplication order and field path are **confirmed by code**. Calling the resulting chain “inverse bind” remains a behavioral label rather than a recovered original class-member name.

Evidence: read-only Ghidra `0x00AB8DD0`, `0x00AB8FC0`, `0x00AB9390`, `0x0082BAE0`; direct disassembly in ignored `sktm_pose_disasm.tsv` and `transform_multiply_disasm.tsv`.
