#!/usr/bin/env python3
"""Summarize effective STAG 0000 scalar/tag fields from the payload inventory."""
from __future__ import annotations

import csv
import struct
import sys
from collections import Counter


FIELDS = [
    "color_op", "color_arg1", "color_arg1_complement", "color_arg1_alpha_replicate",
    "color_arg2", "color_arg2_complement", "color_arg2_alpha_replicate",
    "color_arg0", "color_arg0_complement", "color_arg0_alpha_replicate",
    "alpha_op", "alpha_arg1", "alpha_arg1_complement", "alpha_arg2",
    "alpha_arg2_complement", "alpha_arg0", "alpha_arg0_complement", "result_arg",
]


def main(path):
    counters = [Counter() for _ in range(27)]
    count = 0
    with open(path, encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row["record"] != "STAG" or row["version"] != "0000":
                continue
            payload = bytes.fromhex(row["value"])
            if len(payload) != 33:
                raise ValueError(len(payload))
            values = list(payload[:18]) + [struct.unpack_from("<I", payload, 18)[0], struct.unpack_from("<I", payload, 22)[0]] + list(payload[26:])
            for counter, value in zip(counters, values):
                counter[value] += 1
            count += 1
    names = FIELDS + ["texture_tag", "texcoord_tag", "address_u", "address_v", "address_w", "filter_0", "filter_1", "filter_2", "trailing"]
    print("records", count)
    for name, counter in zip(names, counters):
        values = ", ".join(f"{value:#x}:{uses}" for value, uses in counter.most_common())
        print(f"{name}\t{len(counter)}\t{values}")


if __name__ == "__main__":
    main(sys.argv[1])
