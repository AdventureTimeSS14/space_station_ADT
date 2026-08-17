import os
import sys
import soundfile as sf

root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Resources"))

broken_files = []
checked = 0

for dirpath, _, filenames in os.walk(root_dir):
    for f in filenames:
        if not f.lower().endswith(".ogg"):
            continue

        path = os.path.join(dirpath, f)
        checked += 1

        try:
            with sf.SoundFile(path) as audio:
                audio.frames
            print(f"[OK]   {path}")
        except Exception as e:
            print(f"[FAIL] {path} -> {e}")
            broken_files.append((path, str(e)))

print(f"\nПроверено файлов: {checked}")

if broken_files:
    print("\n⚠️ Найдены битые OGG файлы:")
    for path, err in broken_files:
        print(f"[FAIL] {path} -> {err}")
    print(f"\n💔🙄 Общее количество битых файлов: {len(broken_files)}")
    sys.exit(1)
else:
    print("✅ Все OGG файлы валидные")

"""
    ╔════════════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾              ║
    ║   Автор: Шрёдька (Discord: schrodinger71)   ║
    ║   Лицензия: AGPL v3.0                       ║
    ║   /\_/\\                                    ║
    ║  ( o.o )  Meow!                             ║
    ║   > ^ <                                     ║
    ╚════════════════════════════════════════════╝
"""

