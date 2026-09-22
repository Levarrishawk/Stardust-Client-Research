from __future__ import annotations

import csv
import re
import sys
from collections import Counter
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else Path(__file__).resolve().parents[2]).resolve()
PRIVATE = ROOT / "local-private"
LINK_RE = re.compile(r"!?(?:\[[^\]]*\])\(([^)\s]+)(?:\s+[^)]*)?\)")

markdown_files = [p for p in ROOT.rglob("*.md") if PRIVATE not in p.parents]
broken: list[tuple[str, str]] = []
private_links: list[tuple[str, str]] = []
for document in markdown_files:
    text = document.read_text(encoding="utf-8-sig", errors="replace")
    for raw_target in LINK_RE.findall(text):
        if re.match(r"^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#|/|\\)", raw_target):
            continue
        target = unquote(raw_target.split("#", 1)[0])
        if not target:
            continue
        resolved = (document.parent / target).resolve()
        if not resolved.exists():
            broken.append((document.relative_to(ROOT).as_posix(), raw_target))
        elif PRIVATE == resolved or PRIVATE in resolved.parents:
            private_links.append((document.relative_to(ROOT).as_posix(), raw_target))

names_path = ROOT / "data" / "curated" / "provisional_names.csv"
with names_path.open(encoding="utf-8-sig", newline="") as handle:
    names_rows = list(csv.DictReader(handle))
address_key = next((key for key in names_rows[0] if key.lower() in {"address", "va", "virtual_address"}), None)
addresses = [row[address_key].strip().lower() for row in names_rows] if address_key else []
duplicate_addresses = sorted(key for key, count in Counter(addresses).items() if count > 1)

packet_path = ROOT / "data" / "curated" / "server_packet_gap_matrix.csv"
with packet_path.open(encoding="utf-8-sig", newline="") as handle:
    packet_rows = list(csv.DictReader(handle))

public_files = [p for p in ROOT.rglob("*") if p.is_file() and PRIVATE not in p.parents]
public_bytes = sum(p.stat().st_size for p in public_files)
large_public = sorted(
    ((p.stat().st_size, p.relative_to(ROOT).as_posix()) for p in public_files if p.stat().st_size >= 25 * 1024 * 1024),
    reverse=True,
)

print(f"MARKDOWN_FILES={len(markdown_files)}")
print(f"BROKEN_LINKS={len(broken)}")
for source, target in broken[:100]:
    print(f"BROKEN\t{source}\t{target}")
print(f"LINKS_INTO_LOCAL_PRIVATE={len(private_links)}")
for source, target in private_links[:100]:
    print(f"PRIVATE_LINK\t{source}\t{target}")
print(f"PROVISIONAL_ROWS={len(names_rows)}")
print(f"PROVISIONAL_ADDRESS_COLUMN={address_key or 'NOT_FOUND'}")
print(f"PROVISIONAL_DUPLICATES={len(duplicate_addresses)}")
print(f"PACKET_ROWS={len(packet_rows)}")
print(f"CURATED_FILES={len(public_files)}")
print(f"CURATED_BYTES={public_bytes}")
print(f"CURATED_MIB={public_bytes / 1024 / 1024:.2f}")
print(f"PUBLIC_FILES_GE_25MIB={len(large_public)}")
for size, name in large_public:
    print(f"LARGE\t{size}\t{name}")

if broken or duplicate_addresses or not address_key:
    raise SystemExit(1)
