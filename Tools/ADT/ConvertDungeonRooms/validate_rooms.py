"""Проверяет файлы комнат: размеры карты тайлов, легенду и позиции сущностей."""

import glob
import io
import sys

import yaml


def main():
    pattern = sys.argv[1] if len(sys.argv) > 1 else "Resources/Prototypes/ADT/Procedural/Rooms/**/*.yml"
    problems = 0
    total = 0

    for path in sorted(glob.glob(pattern, recursive=True)):
        total += 1
        data = yaml.safe_load(io.open(path, encoding="utf-8"))
        room = data[0]

        size_x, size_y = [int(value) for value in str(room["size"]).split(",")]
        tiles = room.get("tiles", [])
        legend = room.get("legend", {})

        if len(tiles) != size_y:
            print(f"строк не столько: {room['id']} {len(tiles)} вместо {size_y}")
            problems += 1

        for row in tiles:
            if len(row) != size_x:
                print(f"ширина строки не та: {room['id']} {len(row)} вместо {size_x}")
                problems += 1
                break

        unknown = {char for row in tiles for char in row if char != "." and char not in legend}
        if unknown:
            print(f"символы без легенды: {room['id']} {sorted(unknown)}")
            problems += 1

        areas = room.get("areas", [])
        area_legend = room.get("areaLegend", {})

        if areas and len(areas) != size_y:
            print(f"строк областей не столько: {room['id']} {len(areas)} вместо {size_y}")
            problems += 1

        for row in areas:
            if len(row) != size_x:
                print(f"ширина строки областей не та: {room['id']} {len(row)} вместо {size_x}")
                problems += 1
                break

        unknown_areas = {char for row in areas for char in row if char != "." and char not in area_legend}
        if unknown_areas:
            print(f"области без легенды: {room['id']} {sorted(unknown_areas)}")
            problems += 1

        for group in room.get("entities", []):
            for position in group["positions"]:
                x, y = [float(value) for value in str(position).split(",")]
                if not (0 <= x <= size_x and 0 <= y <= size_y):
                    print(f"сущность вне комнаты: {room['id']} {group['proto']} {position}")
                    problems += 1

    print(f"файлов: {total}, проблем: {problems}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
