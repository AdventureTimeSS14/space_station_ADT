import re
import sys
import logging
from pathlib import Path
import yaml

FILE_PATH = Path(__file__).resolve()
CHANGELOG_PATH = FILE_PATH.parents[2] / "Resources" / "Changelog" / "1ChangelogADT.yml"

CHANGE_TYPES = {
    "add": "Add",
    "remove": "Remove",
    "tweak": "Tweak",
    "fix": "Fix",
}
CHANGE_LINE_RE = re.compile(
    r"^-\s*(" + "|".join(CHANGE_TYPES.keys()) + r")\s*:\s*(.*)$",
    re.IGNORECASE,
)

class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if "yaml_implicit_resolvers" not in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()
        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [
                (tag, regexp) for tag, regexp in mappings if tag != tag_to_remove
            ]


NoDatesSafeLoader.remove_implicit_resolver("tag:yaml.org,2002:timestamp")


class MyDumper(yaml.SafeDumper):
    def increase_indent(self, flow=False, indentless=False):
        return super(MyDumper, self).increase_indent(flow, False)

def represent_dict_compact(dumper, data):
    if len(data) == 2 and "message" in data and "type" in data:
        return dumper.represent_mapping("tag:yaml.org,2002:map", data.items(), flow_style=True)
    return dumper.represent_mapping("tag:yaml.org,2002:map", data.items())

def represent_multiline_string(dumper, data):
    return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|" if "\n" in data else None)

MyDumper.add_representer(dict, represent_dict_compact)
MyDumper.add_representer(str, represent_multiline_string)

def strip_newlines(value):
    if isinstance(value, dict):
        return {k: strip_newlines(v) for k, v in value.items()}
    if isinstance(value, list):
        return [strip_newlines(v) for v in value]
    if isinstance(value, str):
        return value.replace("\n", " ").replace("\r", " ")
    return value

def load_yaml(file_path: Path):
    if file_path.exists():
        with file_path.open("r", encoding="utf-8") as f:
            return yaml.load(f, Loader=NoDatesSafeLoader) or {"Entries": []}
    return {"Entries": []}

def save_yaml(data, file_path: Path):
    file_path.parent.mkdir(parents=True, exist_ok=True)
    with file_path.open("w", encoding="utf-8") as f:
        yaml.dump(data, f, default_flow_style=False, allow_unicode=True, Dumper=MyDumper)

def parse_pr_body(body: str, default_author: str):
    author = default_author
    changes = []

    lines = [line.strip() for line in body.splitlines()]
    for line in lines:
        lowered = line.lower()

        if lowered.startswith("no cl") or lowered.startswith(":no_cl:"):
            return None, []

        if line.startswith(":cl:"):
            potential_author = line[len(":cl:"):].strip()
            if potential_author:
                author = potential_author
            continue

        match = CHANGE_LINE_RE.match(line)
        if match:
            change_type = CHANGE_TYPES[match.group(1).lower()]
            message = match.group(2).strip()
            if message:
                changes.append({"message": message, "type": change_type})
            else:
                logging.warning(f"Пустое сообщение в строке: {line!r}.")
            continue

        if line.startswith("-") and ":" in line:
            logging.warning(f"Не распознана строка: {line!r}")

    return author, changes


def append_entry(author: str, changes: list, merged_at: str, pr_number: int):
    data = load_yaml(CHANGELOG_PATH)
    entries = data.get("Entries") or []

    if any(entry.get("pr_number") == pr_number for entry in entries):
        logging.info(f"PR #{pr_number} уже есть в чейнджлоге.")
        return

    next_id = max((entry.get("id", 0) for entry in entries), default=0) + 1

    new_entry = {
        "author": author,
        "changes": strip_newlines(changes),
        "id": next_id,
        "pr_number": pr_number,
        "time": merged_at,
    }
    entries.append(new_entry)

    save_yaml({"Entries": entries}, CHANGELOG_PATH)

def main():
    if len(sys.argv) < 5:
        logging.error("Usage: auto_cl_on_merge.py <PR_NUMBER> <PR_BODY_FILE> <PR_AUTHOR> <MERGED_AT>")
        sys.exit(1)

    pr_number_arg = sys.argv[1]
    body_file = Path(sys.argv[2])
    default_author = sys.argv[3]
    merged_at = sys.argv[4]

    try:
        pr_number = int(pr_number_arg)
    except ValueError:
        logging.error(f"PR_NUMBER должен быть числом, получено: {pr_number_arg!r}")
        sys.exit(1)

    if not merged_at:
        logging.error("MERGED_AT не может быть пустым.")
        sys.exit(1)

    if not body_file.exists():
        logging.error(f"Файл с телом PR не найден: {body_file}")
        sys.exit(1)

    body = body_file.read_text(encoding="utf-8")

    author, changes = parse_pr_body(body, default_author)

    if author is None or not changes:
        return

    append_entry(author, changes, merged_at, pr_number)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    main()
