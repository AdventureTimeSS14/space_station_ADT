#!/usr/bin/env python3
"""Проверяем OGG-файлы в Resources/.

Для JukeBox (прототипы type: jukebox) - строго MONO.
Всё остальное проверяем только на целостность.

Ищем type: jukebox в любых .yml прототипах.

Запуск:
    python3 Tools/ADT/validate_ogg.py
"""

from __future__ import annotations

import os
import re
import sys

try:
    import yaml
except ImportError:
    sys.exit("PyYAML is required: pip install pyyaml")

import soundfile as sf

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RESOURCES_DIR = os.path.join(PROJECT_ROOT, "Resources")
PROTOTYPES_DIR = os.path.join(RESOURCES_DIR, "Prototypes")

JUKEBOX_TYPE_RE = re.compile(r"^\s*-\s*type:\s*jukebox\s*$", re.MULTILINE)


def find_jukebox_catalogs() -> list[str]:
    """Ищем все .yml, где есть type: jukebox.

    Возвращаем список путей к ним, чтобы потом вытащить треки.
    Если файл не читается - молча пропускаем (вдруг права или битый файл).
    """
    catalogs: list[str] = []
    for dirpath, _, filenames in os.walk(PROTOTYPES_DIR):
        for name in filenames:
            if not name.endswith(".yml"):
                continue
            path = os.path.join(dirpath, name)
            try:
                with open(path, encoding="utf-8") as fh:
                    if JUKEBOX_TYPE_RE.search(fh.read()):
                        catalogs.append(path)
            except OSError:
                continue
    return sorted(catalogs)


def collect_jukebox_paths(catalogs: list[str]) -> set[str]:
    """Собираем абсолютные пути всех .ogg, упомянутых в jukebox-прототипах.

    Формат в прототипах бывает разный:
      - path: /Audio/...ogg
      - path: { path: /Audio/...ogg }

    Обрабатываем оба варианта, чтобы не пропустить.
    """
    paths: set[str] = set()
    for catalog in catalogs:
        with open(catalog, encoding="utf-8") as fh:
            data = yaml.safe_load(fh)
        if not isinstance(data, list):
            continue
        for entry in data:
            if not isinstance(entry, dict) or entry.get("type") != "jukebox":
                continue
            path_spec = entry.get("path")
            rel = path_spec.get("path") if isinstance(path_spec, dict) else path_spec
            if not isinstance(rel, str) or not rel.lower().endswith(".ogg"):
                continue
            rel = rel.lstrip("/")
            abs_path = os.path.normpath(os.path.join(RESOURCES_DIR, rel))
            paths.add(abs_path)
    return paths


def main() -> int:
    catalogs = find_jukebox_catalogs()
    jukebox_paths = collect_jukebox_paths(catalogs)

    print(f"JukeBox catalog prototypes ({len(catalogs)}):")
    for catalog in catalogs:
        print(f"  - {os.path.relpath(catalog, PROJECT_ROOT)}")
    print(f"JukeBox tracks referenced: {len(jukebox_paths)}\n")

    # бежим по всем .ogg в Resources/ и проверяем
    broken_files: list[tuple[str, str]] = []            # битые файлы
    stereo_jukebox_files: list[tuple[str, int]] = []    # стерео-файлы в джубоксе
    missing_jukebox_files: list[str] = []               # файлы, которые есть в прототипах, но отсутствуют на диске
    checked = 0
    seen_jukebox_paths = set()                          # чтобы потом найти пропущенные

    for dirpath, _, filenames in os.walk(RESOURCES_DIR):
        for f in filenames:
            if not f.lower().endswith(".ogg"):
                continue

            path = os.path.normpath(os.path.join(dirpath, f))
            is_jukebox = path in jukebox_paths
            if is_jukebox:
                seen_jukebox_paths.add(path)
            checked += 1

            # Проверяем целостность файла через soundfile
            try:
                with sf.SoundFile(path) as audio:
                    audio.frames
                    channels = audio.channels
            except Exception as e:
                print(f"[FAIL]  {path} -> {e}")
                broken_files.append((path, str(e)))
                continue

            # Теперь - если это джубокс, проверяем на моно.
            if is_jukebox:
                if channels != 1:
                    stereo_jukebox_files.append((path, channels))
                    print(f"[STEREO] {path} -> {channels} channels (JukeBox track must be mono)")
                # else:
                    # print(f"[OK]    {path} -> mono (JukeBox)")
            # else:
                # print(f"[OK]    {path} -> {channels} channel(s)")

    missing_jukebox_files = sorted(jukebox_paths - seen_jukebox_paths)

    print(f"\nПроверено файлов: {checked}")
    print(f"Из них JukeBox треков: {len(seen_jukebox_paths)}")

    has_errors = False

    if missing_jukebox_files:
        print("\n⚠️ JukeBox треки, указанные в прототипах, но не найденные на диске:")
        for path in missing_jukebox_files:
            print(f"[MISS] {path}")
        has_errors = True

    if broken_files:
        print("\n⚠️ Найдены битые OGG файлы:")
        for path, err in broken_files:
            print(f"[FAIL] {path} -> {err}")
        has_errors = True

    if stereo_jukebox_files:
        print("\n⚠️ Найдены стерео JukeBox файлы (должны быть моно):")
        for path, channels in stereo_jukebox_files:
            print(f"[STEREO] {path} -> {channels} channels")
        has_errors = True

    if has_errors:
        total = len(missing_jukebox_files) + len(broken_files) + len(stereo_jukebox_files)
        print(f"\n💔🙄 Общее количество проблемных файлов: {total}")
        return 1

    print("\n✅ Все OGG файлы валидные, все JukeBox треки в моно-формате")
    return 0


if __name__ == "__main__":
    sys.exit(main())

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

