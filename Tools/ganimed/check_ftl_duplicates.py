import os
from collections import defaultdict

LOCALE_PATH = "Resources/Locale"

def find_duplicates():
    duplicates = defaultdict(lambda: defaultdict(list))

    for root, dirs, files in os.walk(LOCALE_PATH):
        for file in files:
            if not file.endswith(".ftl"):
                continue

            path = os.path.join(root, file)
            try:
                locale_name = path.split(os.sep)[2]
            except IndexError:
                locale_name = "unknown"

            with open(path, encoding="utf-8-sig", errors="replace") as f:
                for lineno, line in enumerate(f, start=1):
                    line = line.strip()
                    if not line or line.startswith("#") or "=" not in line:
                        continue

                    key = line.split("=", 1)[0].strip()
                    duplicates[locale_name][key].append(f"{path}:{lineno}")

    has_duplicates = False
    for locale, keys in duplicates.items():
        for key, locations in keys.items():
            if len(locations) > 1:
                has_duplicates = True
                print(f"Duplicates found for key '{key}' in locale '{locale}':")
                for loc in locations:
                    print(f"  {loc}")
                print()

    return has_duplicates

if __name__ == "__main__":
    if find_duplicates():
        print("Duplicates exist!")
    else:
        print("No duplicates found.")