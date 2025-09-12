import os
import sys
from collections import Counter
import yaml

LOCALES = ["ru-RU", "en-US"]
BASE_PATH = os.path.join("Content.Shared", "Localizations")

duplicates_found = False

for locale in LOCALES:
    path = os.path.join(BASE_PATH, f"{locale}.yml")
    if not os.path.exists(path):
        continue

    with open(path, encoding="utf-8") as f:
        data = yaml.safe_load(f)

    if not isinstance(data, dict):
        continue

    counter = Counter(data.keys())
    duplicates = [k for k, v in counter.items() if v > 1]

    if duplicates:
        duplicates_found = True
        for d in duplicates:
            print(f"Duplicate localization id '{d}' found in culture {locale}")

if duplicates_found:
    sys.exit(1)