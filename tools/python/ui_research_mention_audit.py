"""Find UI RTTI classes absent from existing research prose.

This is a discovery aid, not a behavioral-coverage classifier: a mention is
not proof that the class has been analyzed, and no mention is not proof of
runtime reachability. Input is the existing filtered RTTI inventory.
"""

import csv
import pathlib
import re
import sys
from collections import Counter


def main() -> None:
    root = pathlib.Path(sys.argv[1])
    source = root / "local-private/generated-evidence/ui_concrete_class_inventory.tsv"
    with source.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle, delimiter="\t"))

    paths = list((root / "docs").rglob("*.md"))
    paths += list((root / "research/checkpoints").rglob("*.md"))
    paths += [root / "UNKNOWN_FUNCTIONS.md", root / "ANALYSIS_STATUS.md"]
    corpus = [(path, path.read_text(encoding="utf-8", errors="replace")) for path in paths]

    totals = Counter()
    print("category\tclass_name\tmention_files\tfirst_example")
    for row in rows:
        name = row["class_name"]
        # Nested callback/owner RTTI is not itself a standalone screen.
        if "@" in name:
            continue
        pattern = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])")
        hits = [path.relative_to(root).as_posix() for path, content in corpus if pattern.search(content)]
        totals[(row["category"], bool(hits))] += 1
        print(f"{row['category']}\t{name}\t{len(hits)}\t{hits[0] if hits else ''}")
    print("\nSUMMARY", file=sys.stderr)
    for category in sorted({key[0] for key in totals}):
        print(f"{category}\tmentioned={totals[(category, True)]}\tno_mention={totals[(category, False)]}", file=sys.stderr)


if __name__ == "__main__":
    main()
