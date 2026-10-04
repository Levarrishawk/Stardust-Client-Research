"""Independent literal-wire and R43 policy-gap oracle; no production imports."""
import json
import struct
import sys
from pathlib import Path


def verify(path):
    result = json.loads(Path(path).read_text(encoding="utf-8"))
    wire = bytes.fromhex(result["Literal"])
    header = struct.unpack_from("<HIQIBIH", wire)
    assert header == (5, 0x12862153, 0xDABC001122334455, 0x4352454F, 4, 20, 3)
    assert len(wire) == 43
    assert [struct.unpack_from("<HI", wire, 25 + 6 * i) for i in range(3)] == [
        (1, 0x3F800000), (5, 0x40000000), (10, 0x40400000)
    ]
    assert result["CounterexampleChecks"] == 23 and result["Checks"] == 43
    assert result["ActiveError"] == "Finalized CREO4 target required."
    assert result["StagedError"] == "Finalized CREO4 target required."
    assert result["PartialStateRequired"] is True
    assert bytes.fromhex(result["SignedZeroLiteral"]) == struct.pack("<HIQIBIH", 5, 0x12862153, 0xDABC001122334455, 0x4352454F, 4, 8, 1) + struct.pack("<HI", 5, 0x80000000)
    partial = result["Partial"]
    known = {}
    for name, revision, updates, declared in [
        ("Empty", 0, [], 0),
        ("First", 1, [(1, 0x3F800000), (5, 0x40000000), (10, 0x40400000)], 3),
        ("Repeat", 2, [(1, 0x3F800000), (5, 0x40000000), (10, 0x40400000)], 3),
        ("Third", 3, [(5, 0x80000000)], 1),
    ]:
        events = []
        for ordinal, (field, bits) in enumerate(updates):
            old = known.get(field)
            source = dict(Revision=revision, DeclaredCount=declared, Ordinal=ordinal, Origin=1)
            events.append(dict(Index=field, Ordinal=ordinal, OldWasKnown=old is not None, OldBits=None if old is None else old[0], NewBits=bits, OldProvenance=None if old is None else old[1], Provenance=source))
            known[field] = (bits, source)
        expected = dict(ObjectId=0xDABC001122334455, Revision=revision, KnownMask=sum(1 << f for f in known), KnownCount=len(known), DecodedBytes=16+32*len(known), Fields=[dict(Index=f, Bits=b, StateRevision=revision, Provenance=p) for f, (b, p) in sorted(known.items())])
        assert partial[name] == expected, name
        if updates:
            assert partial[name+"Events"] == events, name+"Events"
    print("R44_OBSERVER_LITERAL_ORACLE_PASS bytes=43 fields=3 lifecycle_checks=23 partial_checks=20")


if __name__ == "__main__":
    verify(sys.argv[1])
