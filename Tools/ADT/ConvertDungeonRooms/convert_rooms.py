"""Разрезает атлас-карту данжей на отдельные файлы комнат в формате.

Запуск из корня репозитория:

    python Tools/ADT/ConvertDungeonRooms/convert_rooms.py \
        --atlas /Maps/ADTMaps/Ruins/lavadunges.yml \
        --rooms Resources/Prototypes/ADT/Procedural/Themes/lavadunges.yml \
        --out Resources/Prototypes/ADT/Procedural/Rooms/Lavaland

Если старых прототипов dungeonRoom уже нет, комнату можно вырезать по координатам

    python Tools/ADT/ConvertDungeonRooms/convert_rooms.py \
        --atlas /Maps/ADTMaps/Ruins/hierophant_arena.yml \
        --room ADTHierophantArena --size 23,23 --offset 0,0 --tags ADTHierophantArena \
        --out Resources/Prototypes/ADT/Procedural/Rooms/Hierophant
"""

import argparse
import base64
import os
import struct
import sys

import yaml

CHUNK_SIZE = 16
TILE_STRUCT_SIZE = 7
LEGEND_CHARS = (
    "abcdefghijklmnopqrstuvwxyz"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    "0123456789"
    "+=/()<>;:~^"
)

SPACE_CHAR = "."
SKIPPED_COMPONENTS = {
    "Transform",
    "MetaData",
    "ContainerContainer",
    "Storage",
    "EntityStorage",
    "DeviceLinkSource",
    "DeviceLinkSink",
    "DeviceList",
    "DeviceNetwork",
    "Physics",
    "Fixtures",
}


class Tagged:
    def __init__(self, tag, value):
        self.tag = tag
        self.value = value


class MapLoader(yaml.SafeLoader):
    pass


class MapDumper(yaml.SafeDumper):
    pass


def construct_tagged(loader, tag_suffix, node):
    if isinstance(node, yaml.MappingNode):
        value = loader.construct_mapping(node, deep=True)
    elif isinstance(node, yaml.SequenceNode):
        value = loader.construct_sequence(node, deep=True)
    else:
        value = loader.construct_scalar(node)

    return Tagged(node.tag, value)


def represent_tagged(dumper, data):
    if isinstance(data.value, dict):
        return dumper.represent_mapping(data.tag, data.value)
    if isinstance(data.value, list):
        return dumper.represent_sequence(data.tag, data.value)
    return dumper.represent_scalar(data.tag, str(data.value))


def represent_str(dumper, data):
    if "\n" in data:
        return dumper.represent_scalar("tag:yaml.org,2002:str", data, style='"')

    return dumper.represent_scalar("tag:yaml.org,2002:str", data)


MapLoader.add_multi_constructor("", construct_tagged)
MapDumper.add_representer(Tagged, represent_tagged)
MapDumper.add_representer(str, represent_str)


def load_yaml(path):
    with open(path, encoding="utf-8-sig") as file:
        return yaml.load(file, Loader=MapLoader)


class Atlas:
    def __init__(self, path):
        data = load_yaml(path)

        self.tilemap = {int(key): value for key, value in data["tilemap"].items()}
        self.grid_uid = data["grids"][0]

        self.chunks = {}
        self.decals = []
        self.entities = []
        self.areas = {}

        for group in data["entities"]:
            proto = group.get("proto") or ""

            for entity in group["entities"]:
                components = {}
                for component in entity.get("components", []):
                    components.setdefault(component["type"], []).append(component)

                if entity["uid"] == self.grid_uid:
                    self._read_grid(components)
                    continue

                if not proto:
                    continue

                transforms = components.get("Transform")
                if not transforms:
                    continue

                transform = transforms[0]
                if "pos" not in transform:
                    continue

                if transform.get("parent") != self.grid_uid:
                    continue

                position = parse_vector(transform["pos"])

                overrides = []
                for name, entries in components.items():
                    if name in SKIPPED_COMPONENTS:
                        continue
                    overrides.extend(entries)

                self.entities.append({
                    "proto": proto,
                    "pos": position,
                    "rot": transform.get("rot"),
                    "anchored": transform.get("anchored"),
                    "components": dump_components(overrides),
                })

    def _read_grid(self, components):
        for grid in components.get("MapGrid", []):
            for chunk in (grid.get("chunks") or {}).values():
                index = parse_vector_int(chunk["ind"])
                version = int(chunk.get("version", 1))
                if version < 7:
                    raise RuntimeError(f"чанк версии {version} не поддерживается")
                self.chunks[index] = decode_chunk(chunk["tiles"])

        # Области мапер сводит в грид командой "areas save", по одному прототипу на тайл.
        for area_grid in components.get("AreaGrid", []):
            for key, proto in (area_grid.get("areas") or {}).items():
                self.areas[parse_vector_int(key)] = proto

        for decal_grid in components.get("DecalGrid", []):
            # Компонент может стоять на гриде пустышкой, если декалей на карте нет.
            collection = decal_grid.get("chunkCollection") or {}

            for node in collection.get("nodes") or []:
                info = node.get("node", {})
                for position in node.get("decals", {}).values():
                    self.decals.append({
                        "id": info.get("id"),
                        "color": info.get("color"),
                        "angle": info.get("angle"),
                        "zIndex": info.get("zIndex"),
                        "cleanable": info.get("cleanable"),
                        "pos": parse_vector(position),
                    })

    def tile_at(self, x, y):
        chunk = self.chunks.get((x // CHUNK_SIZE, y // CHUNK_SIZE))
        if chunk is None:
            return None

        tile_id, _flags, variant, rotation = chunk[(y % CHUNK_SIZE) * CHUNK_SIZE + (x % CHUNK_SIZE)]
        name = self.tilemap.get(tile_id)

        if name is None or name == "Space":
            return None

        return name, variant, rotation


def decode_chunk(encoded):
    raw = base64.b64decode(encoded)
    tiles = []

    for i in range(CHUNK_SIZE * CHUNK_SIZE):
        offset = i * TILE_STRUCT_SIZE
        tile_id, flags, variant, rotation = struct.unpack_from("<iBBB", raw, offset)
        tiles.append((tile_id, flags, variant, rotation))

    return tiles


def dump_components(components):
    if not components:
        return None

    text = yaml.dump(
        components,
        Dumper=MapDumper,
        default_flow_style=False,
        allow_unicode=True,
        sort_keys=False,
        width=10000)

    return [line for line in text.rstrip("\n").split("\n")]


def parse_vector(value):
    x, y = str(value).split(",")
    return float(x), float(y)


def parse_vector_int(value):
    x, y = str(value).split(",")
    return int(x), int(y)


def format_float(value):
    text = f"{value:.4f}".rstrip("0").rstrip(".")
    return text if text not in ("", "-0") else "0"


def format_vector(vector):
    return f"{format_float(vector[0])},{format_float(vector[1])}"


def collect_rooms(path, atlas_path):
    rooms = []

    for entry in load_yaml(path):
        if entry.get("type") != "dungeonRoom":
            continue
        if entry.get("atlas") != atlas_path:
            continue

        rooms.append({
            "id": entry["id"],
            "size": parse_vector_int(entry["size"]),
            "offset": parse_vector_int(entry["offset"]),
            "tags": list(entry.get("tags", [])),
        })

    return rooms


def build_room(atlas, room):
    offset_x, offset_y = room["offset"]
    size_x, size_y = room["size"]

    legend = {}
    order = []
    rows = []

    for y in reversed(range(size_y)):
        row = []
        for x in range(size_x):
            tile = atlas.tile_at(offset_x + x, offset_y + y)

            if tile is None:
                row.append(SPACE_CHAR)
                continue

            if tile not in legend:
                if len(order) >= len(LEGEND_CHARS):
                    raise RuntimeError(f"в комнате {room['id']} слишком много разных тайлов")
                legend[tile] = LEGEND_CHARS[len(order)]
                order.append(tile)

            row.append(legend[tile])
        rows.append("".join(row))

    area_legend = {}
    area_order = []
    area_rows = []

    for y in reversed(range(size_y)):
        row = []
        for x in range(size_x):
            proto = atlas.areas.get((offset_x + x, offset_y + y))

            if proto is None:
                row.append(SPACE_CHAR)
                continue

            if proto not in area_legend:
                if len(area_order) >= len(LEGEND_CHARS):
                    raise RuntimeError(f"в комнате {room['id']} слишком много разных областей")
                area_legend[proto] = LEGEND_CHARS[len(area_order)]
                area_order.append(proto)

            row.append(area_legend[proto])
        area_rows.append("".join(row))

    if not area_order:
        area_rows = []

    groups = {}
    for entity in atlas.entities:
        x, y = entity["pos"]
        if not (offset_x <= x < offset_x + size_x and offset_y <= y < offset_y + size_y):
            continue

        components = entity["components"]
        key = (entity["proto"], entity["rot"], entity["anchored"], tuple(components) if components else None)
        groups.setdefault(key, []).append((x - offset_x, y - offset_y))

    decals = {}
    for decal in atlas.decals:
        x, y = decal["pos"]
        if not (offset_x <= x < offset_x + size_x and offset_y <= y < offset_y + size_y):
            continue

        key = (decal["id"], decal["color"], decal["angle"], decal["zIndex"], decal["cleanable"])
        decals.setdefault(key, []).append((x - offset_x, y - offset_y))

    return {
        "legend": [(char, tile) for tile, char in legend.items()],
        "rows": rows,
        "areaLegend": [(area_legend[proto], proto) for proto in area_order],
        "areaRows": area_rows,
        "entities": groups,
        "decals": decals,
    }


def write_room(path, room, built):
    lines = []
    lines.append(f"# Комната данжа {room['id']}, вырезана из атласа автоматически.")
    lines.append("# Правится руками: легенда сверху, ниже карта тайлов, потом сущности и декали.")
    lines.append("- type: adtDungeonRoom")
    lines.append(f"  id: {room['id']}")
    lines.append(f"  size: {room['size'][0]}, {room['size'][1]}")

    if room["tags"]:
        lines.append("  tags:")
        for tag in room["tags"]:
            lines.append(f"  - {tag}")

    if built["legend"]:
        lines.append("  legend:")
        for char, (name, variant, rotation) in sorted(built["legend"]):
            lines.append(f'    "{char}":')
            lines.append(f"      tile: {name}")
            if variant:
                lines.append(f"      variant: {variant}")
            if rotation:
                lines.append(f"      rotation: {rotation}")

    lines.append("  tiles:")
    for row in built["rows"]:
        lines.append(f'  - "{row}"')

    if built["areaRows"]:
        lines.append("  areaLegend:")
        for char, proto in sorted(built["areaLegend"]):
            lines.append(f'    "{char}": {proto}')

        lines.append("  areas:")
        for row in built["areaRows"]:
            lines.append(f'  - "{row}"')

    if built["entities"]:
        lines.append("  entities:")
        for (proto, rotation, anchored, components), positions in sorted(built["entities"].items(), key=lambda item: str(item[0])):
            lines.append(f"  - proto: {proto}")
            if rotation is not None:
                lines.append(f"    rot: {rotation}")
            if anchored is not None:
                lines.append(f"    anchored: {str(anchored).lower()}")
            if components:
                lines.append("    components:")
                for line in components:
                    lines.append(f"    {line}")
            lines.append("    positions:")
            for position in positions:
                lines.append(f"    - {format_vector(position)}")

    if built["decals"]:
        lines.append("  decals:")
        for (decal_id, color, angle, z_index, cleanable), positions in sorted(built["decals"].items(), key=lambda item: str(item[0])):
            lines.append(f"  - id: {decal_id}")
            if color is not None:
                lines.append(f"    color: '{color}'")
            if angle is not None:
                lines.append(f"    angle: {angle}")
            if z_index is not None:
                lines.append(f"    zIndex: {z_index}")
            if cleanable is not None:
                lines.append(f"    cleanable: {str(cleanable).lower()}")
            lines.append("    positions:")
            for position in positions:
                lines.append(f"    - {format_vector(position)}")

    with open(path, "w", encoding="utf-8", newline="\n") as file:
        file.write("\n".join(lines) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--atlas", required=True, help="путь атласа так, как он записан в прототипах комнат")
    parser.add_argument("--rooms", help="файл с прототипами dungeonRoom, откуда брать размеры и смещения")
    parser.add_argument("--room", help="вырезать одну комнату с этим id, без файла прототипов")
    parser.add_argument("--size", help="размер одиночной комнаты, например 23,23")
    parser.add_argument("--offset", help="смещение одиночной комнаты в атласе, например 0,0")
    parser.add_argument("--tags", nargs="*", default=[], help="теги одиночной комнаты")
    parser.add_argument("--out", required=True, help="куда складывать файлы комнат")
    parser.add_argument("--resources", default="Resources", help="папка Resources")
    args = parser.parse_args()

    if bool(args.rooms) == bool(args.room):
        sys.exit("нужен либо --rooms, либо --room, но не оба сразу")

    atlas_path = os.path.join(args.resources, args.atlas.lstrip("/"))
    if not os.path.exists(atlas_path):
        sys.exit(f"атлас не найден: {atlas_path}")

    if args.room:
        if not args.size or not args.offset:
            sys.exit("для --room нужны ещё --size и --offset")

        rooms = [{
            "id": args.room,
            "size": parse_vector_int(args.size),
            "offset": parse_vector_int(args.offset),
            "tags": list(args.tags),
        }]
    else:
        rooms = collect_rooms(args.rooms, args.atlas)
        if not rooms:
            sys.exit(f"в {args.rooms} нет комнат с атласом {args.atlas}")

    print(f"читаю атлас {atlas_path}")
    atlas = Atlas(atlas_path)
    print(f"тайловых чанков: {len(atlas.chunks)}, сущностей на гриде: {len(atlas.entities)}, декалей: {len(atlas.decals)}")

    os.makedirs(args.out, exist_ok=True)

    for room in rooms:
        built = build_room(atlas, room)
        path = os.path.join(args.out, f"{room['id']}.yml")
        write_room(path, room, built)

        entity_count = sum(len(positions) for positions in built["entities"].values())
        decal_count = sum(len(positions) for positions in built["decals"].values())
        area_count = len(built["areaLegend"])
        print(
            f"  {room['id']}: {room['size'][0]}x{room['size'][1]},"
            f" сущностей {entity_count}, декалей {decal_count}, областей {area_count}")

    print(f"готово, комнат записано: {len(rooms)}")


if __name__ == "__main__":
    main()
