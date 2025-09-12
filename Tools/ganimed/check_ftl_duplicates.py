import os
from collections import defaultdict

LOCALE_DIR = "Resources/Locale"

def find_duplicates():
    has_duplicates = False

    for lang in os.listdir(LOCALE_DIR):
        lang_path = os.path.join(LOCALE_DIR, lang)
        if not os.path.isdir(lang_path):
            continue

        keys_seen = defaultdict(list)

        for root, _, files in os.walk(lang_path):
            for file in files:
                if not file.endswith(".ftl"):
                    continue

                path = os.path.join(root, file)
                with open(path, encoding="utf-8-sig", errors="replace") as f:
                    for lineno, line in enumerate(f, start=1):
                        line = line.strip()
                        if not line or line.startswith("#") or "=" not in line:
                            continue

                        key = line.split("=", 1)[0].strip()
                        if key.startswith("."):
                            continue

                        keys_seen[key].append(f"{path}:{lineno}")

        duplicates = {k: v for k, v in keys_seen.items() if len(v) > 1}
        if duplicates:
            has_duplicates = True
            print(f"\n❌ Duplicates in {lang}:")
            for key, occurrences in duplicates.items():
                print(f"  Key: {key}")
                for occ in occurrences:
                    print(f"    {occ}")

    return has_duplicates

if __name__ == "__main__":
    import sys
    if find_duplicates():
        sys.exit(1)  # Ошибка для CI
    else:
        print("✅ No duplicates found.")
        sys.exit(0)