import re
import sys
from collections import Counter


def main():
    src, out = sys.argv[1:3]
    rows = set()
    with open(src, encoding="utf-8", errors="replace") as f:
        for line in f:
            # All RTTI transceiver forms retain Event@Messages@Publisher.
            m = re.search(r"U([^@?]+)@Messages@([^@?]+)@@@MessageDispatch", line)
            if not m:
                continue
            event, publisher = m.groups()
            subscriber = ""
            if "OwnedTransceiver" in line:
                prefix = line[:m.start()]
                # The final non-template V...@@ preceding the event is normally the owner.
                owners = re.findall(r"V([A-Za-z_][A-Za-z0-9_]*(?:@[A-Za-z_][A-Za-z0-9_]*)*)@@", prefix)
                if owners:
                    subscriber = owners[-1].replace("@", "::")
            rows.add((publisher, event, subscriber))
    rows = sorted(rows)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("publisher\tevent\tconfirmed_subscriber\n")
        for row in rows:
            f.write("\t".join(row) + "\n")
    counts = Counter(p for p, _, _ in rows)
    print(f"edges={len(rows)} publishers={len(counts)} subscribed={sum(bool(s) for _,_,s in rows)}")
    for p, n in sorted(counts.items(), key=lambda x: (-x[1], x[0])):
        print(f"{p}\t{n}")


if __name__ == "__main__":
    main()
