import os
import sys
import yaml


class IgnoreUnknownTagsConstructor(yaml.SafeLoader):
    pass


def _ignore_unknown(loader, node):
    return None


def _ignore_unknown_multi(loader, tag_suffix, node):
    return None


# Универсально игнорируем любые пользовательские теги, включая !type:*
yaml.add_constructor('!type:Dummy', _ignore_unknown, Loader=IgnoreUnknownTagsConstructor)  # якорь не обязателен, но оставим
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


def collect_entity_ids(prototypes_root: str) -> set[str]:
    ids: set[str] = set()
    for subdir, _, files in os.walk(prototypes_root):
        for file in files:
            if not file.endswith('.yml'):
                continue
            file_path = os.path.join(subdir, file)
            data = _load_yaml_any(file_path)
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
    return ids


def collect_missing_map_protos(maps_root: str, known_ids: set[str]) -> dict[str, list[str]]:
    missing_by_file: dict[str, list[str]] = {}
    for subdir, _, files in os.walk(maps_root):
        for file in files:
            if not file.endswith('.yml'):
                continue
            file_path = os.path.join(subdir, file)
            data = _load_yaml_any(file_path)
            if data is None:
                # Ошибка уже залогирована
                continue

            if not (data and isinstance(data, dict)):
                continue
            entities = data.get('entities')
            if not (entities and isinstance(entities, list)):
                continue

            missing: list[str] = []
            for entity in entities:
                if not (isinstance(entity, dict) and 'proto' in entity):
                    continue
                proto = str(entity['proto'])
                if proto not in known_ids:
                    missing.append(proto)

            if missing:
                # Уникализируем и сортируем для стабильности вывода
                uniq_sorted = sorted(set(missing))
                missing_by_file[file_path] = uniq_sorted

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

    known_ids = collect_entity_ids(prototypes_root)
    missing = collect_missing_map_protos(maps_root, known_ids)

    if not missing:
        print("Все карты: все entity proto существуют в проекте.")
        return 0

    print("Обнаружены отсутствующие прототипы на картах:")
    total_missing = 0
    for file_path, protos in sorted(missing.items()):
        total_missing += len(protos)
        print(f"{file_path} [{len(protos)}]: {', '.join(protos)}")

    print(f"Итого отсутствующих ссылок: {total_missing}")
    return 1


if __name__ == '__main__':
    sys.exit(main())


