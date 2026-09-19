# нейрослоп
"""Режет карту SS13 (.dmm в формате TGM) в комнату формата adtDungeonRoom.

Соответствия путей SS13 и прототипов SS14 задаются в mapping.json рядом со скриптом:
пути, которых там нет, скрипт перечислит и остановится, чтобы ничего не потерялось молча.

Виды правил: "tile" кладёт тайл, "entity" ставит сущность, "skip" выбрасывает путь,
"area" переносит область SS13 в AreaGrid комнаты, например
{"kind": "area", "proto": "ADTAreaNecropolis"}.

    python Tools/ADT/ConvertDungeonRooms/convert_dmm.py \
        SS13/.../lavaland_surface_worldanvil.dmm \
        Resources/Prototypes/ADT/Procedural/Rooms/Lavaland/ADTLavaDungeonWorldAnvil.yml \
        --id ADTLavaDungeonWorldAnvil --tags LavaDungeonWorldAnvil
"""

import argparse
import io
import json
import os
import re
import sys

LEGEND_CHARS = (
    "abcdefghijklmnopqrstuvwxyz"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    "0123456789"
    "+=/()<>;:~^"
)

SPACE_CHAR = "."

# Направления BYOND и соответствующий поворот в SS14. У нас 0 это юг, поворот против часовой.
DIR_TO_ROTATION = {
    1: "3.141592653589793",   # север
    2: None,                  # юг, поворот не нужен
    4: "1.5707963267948966",  # восток
    8: "-1.5707963267948966", # запад
}


def load_mapping(path):
    with io.open(path, encoding="utf-8") as file:
        return json.load(file)


def parse_dmm(path):
    """Возвращает (словарь ключ -> список (путь, свойства), сетка[x][y] -> ключ, размер)."""
    text = io.open(path, encoding="utf-8").read()
    split = re.search(r'^\(\d+,\d+,\d+\) = \{"$', text, re.M)

    if split is None:
        sys.exit("это не TGM-карта, тело не найдено")

    head, body = text[:split.start()], text[split.start():]

    definitions = {}
    for key, content in re.findall(r'"(\w+)" = \((.*?)\)\n', head, re.S):
        atoms = []
        for path_text, properties in re.findall(r'^(/[\w/]+)(\{.*?\})?', content, re.M | re.S):
            direction = None
            if properties:
                match = re.search(r'dir\s*=\s*(\d+)', properties)
                if match:
                    direction = int(match.group(1))
            atoms.append((path_text, direction))
        definitions[key] = atoms

    columns = {}
    for x_text, y_text, _z, block in re.findall(r'\((\d+),(\d+),(\d+)\) = \{"\n(.*?)\n"\}', body, re.S):
        columns[int(x_text)] = block.split("\n")

    width = max(columns)
    height = max(len(rows) for rows in columns.values())

    grid = {}
    for x, rows in columns.items():
        for index, key in enumerate(rows):
            # В TGM первая строка блока это верх карты, а у нас y растёт вверх.
            y = height - index
            grid[(x, y)] = key

    return definitions, grid, (width, height)


def build(definitions, grid, size, mapping):
    width, height = size
    unknown = set()

    legend = {}
    legend_order = []
    rows = []
    entities = {}

    area_legend = {}
    area_order = []
    area_rows = []

    for y in range(height, 0, -1):
        row = []
        area_row = []
        for x in range(1, width + 1):
            key = grid.get((x, y))
            atoms = definitions.get(key, [])
            symbol = SPACE_CHAR
            area_symbol = SPACE_CHAR

            # В SS13 каменная плитка делает лаву под собой безопасной. У нас такой механики нет,
            # поэтому под плитками лаву просто не ставим: дорожки остаются проходимыми.
            covered = any(
                mapping.get(path_text, {}).get("covers")
                for path_text, _direction in atoms)

            for path_text, direction in atoms:
                rule = mapping.get(path_text)

                if rule is None:
                    unknown.add(path_text)
                    continue

                kind = rule.get("kind")

                if kind == "skip":
                    continue

                if covered and rule.get("skipIfCovered"):
                    kind = "tile"

                # Правило может задавать и пол, и сущность сразу: например стена боссов
                # в SS13 это турф, а у нас под ней всё равно должен быть пол.
                tile = rule.get("tile")
                if tile is not None:
                    if tile not in legend:
                        legend[tile] = LEGEND_CHARS[len(legend_order)]
                        legend_order.append(tile)
                    symbol = legend[tile]

                if kind == "tile":
                    continue

                # Область SS13 это такой же атом тайла, но в SS14 она живёт не на тайле,
                # а в AreaGrid грида, поэтому копим её отдельной картой символов.
                if kind == "area":
                    proto = rule["proto"]

                    if proto not in area_legend:
                        if len(area_order) >= len(LEGEND_CHARS):
                            sys.exit("слишком много разных областей")
                        area_legend[proto] = LEGEND_CHARS[len(area_order)]
                        area_order.append(proto)

                    area_symbol = area_legend[proto]
                    continue

                if kind == "entity":
                    rotation = DIR_TO_ROTATION.get(direction)
                    entities.setdefault((rule["proto"], rotation), []).append((x - 1, y - 1))
                    continue

                sys.exit(f"непонятный вид правила у {path_text}: {kind}")

            row.append(symbol)
            area_row.append(area_symbol)
        rows.append("".join(row))
        area_rows.append("".join(area_row))

    if unknown:
        print("нет соответствия для путей:")
        for path_text in sorted(unknown):
            print("  " + path_text)
        sys.exit("допиши mapping.json")

    if not area_order:
        area_rows = []

    return legend, legend_order, rows, entities, area_legend, area_order, area_rows


def write(path, room_id, tags, size, legend, legend_order, rows, entities, area_legend, area_order, area_rows):
    width, height = size
    lines = []
    lines.append(f"# Комната данжа {room_id}, вырезана из карты SS13 скриптом convert_dmm.py.")
    lines.append("- type: adtDungeonRoom")
    lines.append(f"  id: {room_id}")
    lines.append(f"  size: {width}, {height}")

    if tags:
        lines.append("  tags:")
        for tag in tags:
            lines.append(f"  - {tag}")

    lines.append("  legend:")
    for tile in legend_order:
        lines.append(f'    "{legend[tile]}":')
        lines.append(f"      tile: {tile}")

    lines.append("  tiles:")
    for row in rows:
        lines.append(f'  - "{row}"')

    if area_rows:
        lines.append("  areaLegend:")
        for proto in area_order:
            lines.append(f'    "{area_legend[proto]}": {proto}')

        lines.append("  areas:")
        for row in area_rows:
            lines.append(f'  - "{row}"')

    if entities:
        lines.append("  entities:")
        for (proto, rotation), positions in sorted(entities.items(), key=lambda item: (item[0][0], str(item[0][1]))):
            lines.append(f"  - proto: {proto}")
            if rotation is not None:
                lines.append(f"    rot: {rotation} rad")
            lines.append("    positions:")
            for x, y in positions:
                lines.append(f"    - {x + 0.5},{y + 0.5}")

    with io.open(path, "w", encoding="utf-8", newline="\n") as file:
        file.write("\n".join(lines) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("dmm", help="исходная карта SS13")
    parser.add_argument("out", help="куда записать комнату")
    parser.add_argument("--id", required=True, help="id комнаты")
    parser.add_argument("--tags", nargs="*", default=[], help="теги комнаты")
    parser.add_argument(
        "--mapping",
        default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "mapping.json"),
        help="файл соответствий путей SS13 и прототипов SS14")
    args = parser.parse_args()

    mapping = load_mapping(args.mapping)
    definitions, grid, size = parse_dmm(args.dmm)
    legend, legend_order, rows, entities, area_legend, area_order, area_rows = build(
        definitions, grid, size, mapping)

    write(
        args.out, args.id, args.tags, size, legend, legend_order, rows, entities,
        area_legend, area_order, area_rows)

    total = sum(len(positions) for positions in entities.values())
    print(
        f"{args.id}: {size[0]}x{size[1]}, тайлов в легенде {len(legend_order)},"
        f" сущностей {total}, областей {len(area_order)}")


if __name__ == "__main__":
    main()
