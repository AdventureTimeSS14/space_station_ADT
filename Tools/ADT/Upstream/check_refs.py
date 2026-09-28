import json
import os
import re
import sys

import yaml

root = sys.argv[1]
res = os.path.join(root, "Resources")
tex = os.path.join(res, "Textures")


class Loader(yaml.CSafeLoader):
    pass


def any_tag(loader, suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)


Loader.add_multi_constructor("!", any_tag)
Loader.add_multi_constructor("tag:", any_tag)


def walk(d, exts):
    for dp, _, fs in os.walk(d):
        for f in fs:
            if f.endswith(exts):
                yield os.path.join(dp, f)


protos = []
for p in walk(os.path.join(res, "Prototypes"), (".yml", ".yaml")):
    try:
        data = yaml.load(open(p, encoding="utf-8-sig"), Loader=Loader)
    except Exception as e:
        print(f"YAML-ERROR {os.path.relpath(p, root)}: {str(e).splitlines()[0]}")
        continue
    for e in data or []:
        if isinstance(e, dict) and "type" in e:
            e["__file"] = os.path.relpath(p, root).replace("\\", "/")
            protos.append(e)

by_kind = {}
for e in protos:
    by_kind.setdefault(e["type"], {})
    if "id" in e:
        by_kind[e["type"]][e["id"]] = e

ents = by_kind.get("entity", {})
tiles = set(by_kind.get("tile", {}))
tile_alias = set(by_kind.get("tileAlias", {}))

problems = set()


def parents(e):
    p = e.get("parent")
    if p is None:
        return []
    return p if isinstance(p, list) else [p]


for kind, items in by_kind.items():
    for i, e in items.items():
        for p in parents(e):
            if p not in items:
                problems.add(f"missing-parent {kind} {i} -> {p}")

meta_cache = {}


def rsi_states(path):
    path = path.strip()
    if path.startswith("/Textures/"):
        path = path[len("/Textures/"):]
    full = os.path.join(tex, path)
    if full not in meta_cache:
        mj = os.path.join(full, "meta.json")
        if not os.path.isfile(mj):
            meta_cache[full] = None
        else:
            try:
                meta_cache[full] = {s["name"] for s in json.load(open(mj, encoding="utf-8-sig"))["states"]}
            except Exception:
                meta_cache[full] = set()
    return meta_cache[full]


def comp(e, name):
    for c in e.get("components") or []:
        if isinstance(c, dict) and c.get("type") == name:
            return c
    return None


def inherited_sprite(e, name, seen=None):
    seen = seen or set()
    if e.get("id") in seen:
        return None
    seen.add(e.get("id"))
    c = comp(e, name)
    if c and isinstance(c.get("sprite"), str):
        return c["sprite"]
    for p in parents(e):
        if p in ents:
            r = inherited_sprite(ents[p], name, seen)
            if r:
                return r
    return None


def check(owner, sprite, state):
    if not isinstance(sprite, str) or not sprite.endswith(".rsi"):
        return
    st = rsi_states(sprite)
    if st is None:
        problems.add(f"missing-rsi {owner} -> {sprite}")
    elif isinstance(state, str) and st and state not in st:
        problems.add(f"missing-state {owner} -> {sprite}:{state}")


for i, e in ents.items():
    for cname in ("Sprite", "Icon"):
        c = comp(e, cname)
        if not c:
            continue
        base = c.get("sprite") if isinstance(c.get("sprite"), str) else inherited_sprite(e, "Sprite")
        if cname == "Icon":
            check(i, c.get("sprite") or base, c.get("state"))
            continue
        if isinstance(c.get("state"), str):
            check(i, base, c.get("state"))
        for l in c.get("layers") or []:
            if isinstance(l, dict) and isinstance(l.get("state"), str):
                check(i, l.get("sprite") or base, l.get("state"))

for kind, items in by_kind.items():
    for i, e in items.items():
        if kind == "entity":
            continue
        for key in ("sprite", "icon"):
            v = e.get(key)
            if isinstance(v, dict) and isinstance(v.get("sprite"), str):
                check(f"{kind}:{i}", v["sprite"], v.get("state"))
            elif isinstance(v, str) and v.endswith(".png"):
                if not os.path.isfile(os.path.join(res, v.lstrip("/"))):
                    problems.add(f"missing-png {kind}:{i} -> {v}")

migr = {}
mf = os.path.join(res, "migration.yml")
for line in open(mf, encoding="utf-8-sig"):
    m = re.match(r"^\s*([A-Za-z0-9_]+)\s*:\s*([A-Za-z0-9_]+)\s*(#.*)?$", line)
    if m:
        migr[m.group(1)] = m.group(2)

for k, v in migr.items():
    if v != "null" and v not in ents:
        problems.add(f"migration-target-missing {k} -> {v}")
    if k in ents and v != "null":
        problems.add(f"migration-shadows-existing {k} -> {v}")

proto_re = re.compile(r"^- proto: ([A-Za-z0-9_]+)\s*$")
tile_re = re.compile(r"^\s+-?\d+: ([A-Za-z0-9_]+)\s*$")
for p in walk(os.path.join(res, "Maps"), (".yml",)):
    rel = os.path.relpath(p, root).replace("\\", "/")
    in_tilemap = False
    with open(p, encoding="utf-8-sig", errors="replace") as fh:
        for line in fh:
            if line.startswith("tilemap:"):
                in_tilemap = True
                continue
            if in_tilemap:
                m = tile_re.match(line)
                if m:
                    t = m.group(1)
                    if t not in tiles and t not in tile_alias:
                        problems.add(f"map-missing-tile {rel} -> {t}")
                    continue
                in_tilemap = False
            m = proto_re.match(line)
            if m:
                pid = m.group(1)
                if pid and pid not in ents and pid not in migr:
                    problems.add(f"map-missing-proto {rel} -> {pid}")

for x in sorted(problems):
    print(x)
