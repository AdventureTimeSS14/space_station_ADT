import yaml
import sys
from collections import Counter

file = sys.argv[1]

with open(file, encoding="utf-8") as f:
    data = yaml.safe_load(f)

keys = []

def extract_keys(d, prefix=""):
    if isinstance(d, dict):
        for k, v in d.items():
            extract_keys(v, prefix + k + ".")
    else:
        keys.append(prefix[:-1])

extract_keys(data)

counter = Counter(keys)
dups = [k for k, v in counter.items() if v > 1]

if dups:
    for k in dups:
        print(f"{file}: {k}")
    sys.exit(1)