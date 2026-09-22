import re
import sys
from collections import Counter


def demangle_leaf(raw: str) -> str:
    raw = raw.strip().rstrip("\x00")
    m = re.match(r"^\.\?A[UV](.+?)@@$", raw)
    return m.group(1) if m else ""


def category(name: str) -> str:
    n = name.lower()
    rules = [
        ("login_session", ("login", "avatar", "scene", "loading")),
        ("inventory_economy", ("inventory", "auction", "vendor", "trade", "bank", "loot", "container", "harvester", "resourceextract")),
        ("crafting", ("craft", "draft", "experiment", "manufactur", "schematic")),
        ("social_communications", ("chat", "mail", "conversation", "community", "group", "guild", "friend", "who")),
        ("missions_quests_maps", ("mission", "quest", "planetmap", "map", "waypoint", "journal")),
        ("character_progression", ("skill", "profession", "character", "statmigration", "image", "bio", "incapac", "combat")),
        ("space", ("space", "ship", "hyperspace", "cockpit")),
        ("options_support", ("option", "control", "ticket", "debug", "bug", "customer", "help")),
        ("hud_gameplay", ("hud", "target", "radar", "toolbar", "reticle", "ground", "damage", "buff", "status")),
        ("framework", ("mediator", "workspace", "widget", "messagebox", "action", "console", "io", "layer", "canvas", "ime", "manager")),
    ]
    for cat, words in rules:
        if any(w in n for w in words):
            return cat
    return "other"


def main():
    src, out = sys.argv[1:3]
    names = set()
    with open(src, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            # find_pe_strings output may prefix offsets; retain the decorated token.
            m = re.search(r"(\.\?A[UV][^\s\x00]+@@)", line)
            if not m:
                continue
            leaf = demangle_leaf(m.group(1))
            if not leaf or "?$" in leaf or "@Messages@" in leaf or "@Message@" in leaf:
                continue
            # Concrete UI/application types, excluding anonymous-source-path RTTI.
            if not (leaf.startswith("Cui") or leaf.startswith("SwgCui")):
                continue
            if "?%" in leaf:
                continue
            names.add(leaf)
    rows = sorted((category(n), n) for n in names)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("category\tclass_name\n")
        for cat, name in rows:
            f.write(f"{cat}\t{name}\n")
    counts = Counter(cat for cat, _ in rows)
    print(f"concrete classes: {len(rows)}")
    for cat, count in sorted(counts.items()):
        print(f"{cat}\t{count}")


if __name__ == "__main__":
    main()
