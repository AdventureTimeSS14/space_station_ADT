#!/usr/bin/env python3
"""Сводка по шардам интеграционных тестов.
Читает NUnit XML всех шардов и проверяет, что ни один тест не выполнился дважды.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

PREFIX = "nunit3-results-"


def shard_name(path):
    for part in path.replace(os.sep, "/").split("/"):
        if part.startswith(PREFIX):
            return part[len(PREFIX):]
    return path


def main(root):
    paths = sorted(glob.glob(os.path.join(root, "**", "*.xml"), recursive=True))
    if not paths:
        print(f"No NUnit XML found under {root}")
        return 1

    seen = defaultdict(list)
    rows = []

    for path in paths:
        shard = shard_name(path)
        run = ET.parse(path).getroot()
        cases = [tc.get("fullname") for tc in run.iter("test-case")]
        rows.append((shard, len(cases), float(run.get("duration") or 0)))
        for name in cases:
            seen[name].append(shard)

    print(f"{'shard':<16}{'tests':>8}{'duration':>12}")
    for shard, count, duration in sorted(rows, key=lambda row: -row[2]):
        print(f"{shard:<16}{count:>8}{duration:>10.0f} s")
    print(f"{'TOTAL':<16}{sum(row[1] for row in rows):>8}")

    dupes = {name: shards for name, shards in seen.items() if len(shards) > 1}
    if dupes:
        print(f"\nERROR: {len(dupes)} test(s) ran in more than one shard.")
        print("A shard filter is leaking. Check the NUnit.Where expressions in")
        print(".github/workflows/adt-build-test-debug.yml")
        for name, shards in list(dupes.items())[:20]:
            print(f"  {name}  ->  {', '.join(shards)}")
        return 1

    print("\nNo test ran in more than one shard.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "shard-results"))
