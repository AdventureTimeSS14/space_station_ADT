import os
import sys
import yaml
import time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed

# Попытка использовать C-бэкэнд для ускорения, если доступен
BaseLoader = getattr(yaml, 'CSafeLoader', yaml.SafeLoader)


class IgnoreUnknownTagsConstructor(BaseLoader):
    pass


def _ignore_unknown(loader, node):
    return None


def _ignore_unknown_multi(loader, tag_suffix, node):
    return None


# Универсально игнорируем любые пользовательские теги, включая !type:*
yaml.add_constructor('!type:Dummy', _ignore_unknown, Loader=IgnoreUnknownTagsConstructor)
yaml.add_multi_constructor('!type:', _ignore_unknown_multi, Loader=IgnoreUnknownTagsConstructor)
yaml.add_multi_constructor('!', _ignore_unknown_multi, Loader=IgnoreUnknownTagsConstructor)


def find_repo_root(start_dir: str) -> str:
    marker_file = 'SpaceStation14.sln'
    current_dir = start_dir
    while True:
        if marker_file in os.listdir(current_dir):
            return current_dir
        parent_dir = os.path.dirname(current_dir)
        if parent_dir == current_dir:
            print(f"Не удалось найти {marker_file} начиная с {start_dir}")
            sys.exit(1)
        current_dir = parent_dir


def _read_text_with_fallbacks(file_path: str) -> str | None:
    encodings = ['utf-8', 'utf-8-sig', 'cp1251', 'latin-1']
    for enc in encodings:
        try:
            with open(file_path, 'r', encoding=enc) as f:
                return f.read()
        except UnicodeDecodeError:
            continue
        except Exception as e:
            print(f"Ошибка при чтении файла {file_path}: {e}")
            return None
    print(f"Не удалось декодировать файл {file_path} ни одной из кодировок: {', '.join(encodings)}")
    return None


def _load_yaml_any(file_path: str):
    text = _read_text_with_fallbacks(file_path)
    if text is None:
        return None
    if '\t' in text:
        print(f"Предупреждение: встречены табы, заменяю на пробелы: {file_path}")
        text = text.replace('\t', '    ')
    try:
        return yaml.load(text, Loader=IgnoreUnknownTagsConstructor)
    except yaml.YAMLError as e:
        print(f"Ошибка парсинга YAML {file_path}: {e}")
        return None
    except Exception as e:
        print(f"Неожиданная ошибка при обработке {file_path}: {e}")
        return None


def collect_entity_ids(prototypes_root: str) -> set[str]:
    ids: set[str] = set()
    yml_files = list(Path(prototypes_root).rglob('*.yml'))
    total_files = len(yml_files)
    print(f"Обрабатываю {total_files} файлов прототипов...")

    for i, file_path in enumerate(yml_files, 1):
        if i % 100 == 0 or i == total_files:
            print(f"Прогресс: {i}/{total_files} файлов обработано")

        data = _load_yaml_any(str(file_path))
        if data is None:
            # Ошибка уже залогирована
            continue

        if not data:
            continue

        if isinstance(data, dict):
            # Единичный документ типа entity
            if data.get('type') == 'entity' and 'id' in data:
                ids.add(str(data.get('id')))
        elif isinstance(data, list):
            for item in data:
                if isinstance(item, dict) and item.get('type') == 'entity' and 'id' in item:
                    ids.add(str(item.get('id')))

    print(f"Найдено {len(ids)} уникальных entity ID")
    return ids


def load_migration_map(repo_root: str) -> dict[str, str | None]:
    migration_path = os.path.join(repo_root, 'Resources', 'migration.yml')
    migration: dict[str, str | None] = {}
    if not os.path.isfile(migration_path):
        return migration

    data = _load_yaml_any(migration_path)
    if isinstance(data, dict):
        for k, v in data.items():
            key = str(k)
            val = None if v is None else str(v)
            migration[key] = val
    return migration


def resolve_migration(proto: str, migration: dict[str, str | None], max_depth: int = 10) -> str | None:
    current = proto
    visited: set[str] = set()
    depth = 0
    while current in migration and depth < max_depth:
        if current in visited:
            break
        visited.add(current)
        mapped = migration.get(current)
        if mapped is None:
            return None
        if mapped == current:
            break
        current = mapped
        depth += 1
    return current


def _find_proto_references_in_component(component_data: dict, known_ids: set[str], migration: dict[str, str | None]) -> list[str]:
    """Рекурсивно ищет ссылки на прототипы в компонентах сущности"""
    missing: list[str] = []

    def _check_value(value):
        if isinstance(value, str):
            # Пропускаем пустые строки и слишком короткие
            if not value or len(value) < 3:
                return
            # Пропускаем числовые строки (entity IDs)
            if value.isdigit():
                return
            # Пропускаем очевидно не прототипы
            if value.startswith('n') and value[1:].isdigit():
                return

            # Проверяем, является ли строка ссылкой на прототип
            if value in known_ids:
                return  # Существующий прототип
            resolved = resolve_migration(value, migration)
            if resolved is None:
                return  # Миграция удаляет объект
            if resolved not in known_ids:
                missing.append(value)
        elif isinstance(value, dict):
            for v in value.values():
                _check_value(v)
        elif isinstance(value, list):
            for v in value:
                _check_value(v)

    for key, value in component_data.items():
        # Проверяем известные поля, которые могут содержать ссылки на прототипы
        if key in ['prototype', 'prototypeId', 'spawn', 'spawnPrototype', 'entity', 'target', 'source']:
            _check_value(value)
        # Также проверяем все строковые значения на случай, если они являются прототипами
        elif isinstance(value, str) and len(value) > 3:  # Эвристика: прототипы обычно длиннее 3 символов
            _check_value(value)

    return missing


def collect_missing_map_protos(maps_root: str, known_ids: set[str], migration: dict[str, str | None]) -> dict[str, list[str]]:
    missing_by_file: dict[str, list[str]] = {}
    yml_files = list(Path(maps_root).rglob('*.yml'))
    total_files = len(yml_files)
    print(f"Обрабатываю {total_files} файлов карт...")

    # Параллельная обработка карт
    def parse_map_and_find_missing(file_path_str: str, known_ids_local: set[str], migration_local: dict[str, str | None]) -> tuple[str, list[str]] | None:
        try:
            data = _load_yaml_any(file_path_str)
            if not (data and isinstance(data, dict)):
                return None
            entities = data.get('entities')
            if not (entities and isinstance(entities, list)):
                return None

            # Собираем локальные ID сущностей на карте (uid/идентификаторы), чтобы валидировать меж-сущностные ссылки
            local_entity_ids: set[int] = set()
            for ent in entities:
                if not isinstance(ent, dict):
                    continue
                # На картах SS14 обычно используется поле uid (int)
                uid_val = ent.get('uid')
                if isinstance(uid_val, int):
                    local_entity_ids.add(uid_val)
                # Иногда встречаются альтернативные ключи
                for alt_key in ('eid', 'localId'):
                    v = ent.get(alt_key)
                    if isinstance(v, int):
                        local_entity_ids.add(v)

            missing: list[str] = []
            for entity in entities:
                if not isinstance(entity, dict):
                    continue

                # Проверяем proto
                if 'proto' in entity:
                    original_proto = str(entity['proto'])
                    # Учитываем миграции
                    resolved = resolve_migration(original_proto, migration_local)
                    if resolved is None:
                        # Миграция удаляет объект -> не считаем отсутствующим
                        continue
                    if resolved not in known_ids_local:
                        # Репортим исходный ID, чтобы было ясно что ломается на карте
                        missing.append(original_proto)

                # Проверяем компоненты на ссылки на прототипы
                for component_name, component_data in entity.items():
                    if component_name == 'proto' or not isinstance(component_data, dict):
                        continue

                    # Ищем ссылки на прототипы в компонентах
                    missing.extend(_find_proto_references_in_component(component_data, known_ids_local, migration_local))

                    # Проверяем меж-сущностные ссылки (device links и т.д.) на валидные local uid
                    def _check_entity_ref(val):
                        # Числовой uid
                        if isinstance(val, int):
                            if local_entity_ids and val not in local_entity_ids:
                                missing.append(f"invalid-entity-link:{val}")
                            return
                        # Строковый uid
                        if isinstance(val, str):
                            s = val.strip()
                            if s.isdigit():
                                iv = int(s)
                                if local_entity_ids and iv not in local_entity_ids:
                                    missing.append(f"invalid-entity-link:{iv}")
                            # Формат n12345 встречается в логах, но в картах обычно не хранится; пропускаем

                    # Наиболее типичные поля со ссылками на другие сущности
                    if local_entity_ids:
                        for key, value in component_data.items():
                            if key in (
                                'target', 'targets', 'source', 'sources',
                                'linked', 'links', 'devices', 'entities',
                                'receivers', 'transmitters', 'inputs', 'outputs'
                            ):
                                if isinstance(value, list):
                                    for v in value:
                                        _check_entity_ref(v)
                                else:
                                    _check_entity_ref(value)

            # Фильтруем пустые и некорректные значения
            def _valid_issue(m: str) -> bool:
                if not m:
                    return False
                ms = m.strip()
                if not ms:
                    return False
                if ms.startswith('invalid-entity-link:'):
                    return True
                return len(ms) > 2

            filtered_missing = [m for m in missing if _valid_issue(m)]

            if not filtered_missing:
                return None

            # Отладочная информация для проблемных файлов
            if file_path_str.endswith('plasma.yml'):
                print(f"DEBUG plasma.yml: найдено {len(filtered_missing)} проблем (отсутствующие прототипы/ссылки): {filtered_missing}")

            return (file_path_str, sorted(set(filtered_missing)))
        except Exception as e:
            print(f"Критическая ошибка при обработке карты {file_path_str}: {e}")
            return None

    # Кол-во воркеров: из env MAP_CHECK_WORKERS или по числу CPU (но не более 8 по умолчанию)
    try:
        workers_env = int(os.environ.get('MAP_CHECK_WORKERS', '0'))
    except ValueError:
        workers_env = 0
    max_workers = workers_env if workers_env > 0 else max(1, min(8, (os.cpu_count() or 2)))

    # Небольшой прогресс-лог на старте
    for i, fp in enumerate(yml_files[:10], 1):
        print(f"  Обрабатываю: {fp.name}")

    # Используем ThreadPoolExecutor, чтобы избежать pickle-проблем на Windows
    # и не передавать большой набор known_ids в каждый процесс.
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(parse_map_and_find_missing, str(fp), known_ids, migration): fp for fp in yml_files}
        processed = 0
        for future in as_completed(futures):
            processed += 1
            if processed % 50 == 0 or processed == total_files:
                print(f"Прогресс карт: {processed}/{total_files} файлов обработано")
            result = future.result()
            if result is None:
                continue
            file_path_str, miss = result
            missing_by_file[file_path_str] = miss

    return missing_by_file


def main() -> int:
    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = find_repo_root(script_dir)

    prototypes_root = os.path.join(repo_root, 'Resources', 'Prototypes')
    maps_root = os.path.join(repo_root, 'Resources', 'Maps')

    if not os.path.isdir(prototypes_root):
        print(f"Папка с прототипами не найдена: {prototypes_root}")
        return 2
    if not os.path.isdir(maps_root):
        print(f"Папка с картами не найдена: {maps_root}")
        return 2

    print("=== Сбор entity ID из прототипов ===")
    start_time = time.time()
    known_ids = collect_entity_ids(prototypes_root)
    proto_time = time.time() - start_time
    print(f"Сбор прототипов занял {proto_time:.1f} секунд")

    print("\n=== Проверка карт на отсутствующие прототипы ===")
    start_time = time.time()
    migration = load_migration_map(repo_root)
    if migration:
        print(f"Загружена миграция: {len(migration)} записей")
    else:
        print("Миграция не найдена или пуста")

    missing = collect_missing_map_protos(maps_root, known_ids, migration)
    maps_time = time.time() - start_time
    print(f"Проверка карт заняла {maps_time:.1f} секунд")

    if not missing:
        print("Все карты: все entity proto существуют в проекте.")
        return 0

    print("Обнаружены отсутствующие прототипы на картах:")
    total_missing = 0
    for file_path, protos in sorted(missing.items()):
        total_missing += len(protos)
        if protos:
            protos_str = ', '.join(protos)
            print(f"{file_path} [{len(protos)}]: {protos_str}")
        else:
            print(f"{file_path} [0]: <пустые ссылки>")

    print(f"\nИтого отсутствующих ссылок: {total_missing}")
    return 1


if __name__ == '__main__':
    sys.exit(main())


