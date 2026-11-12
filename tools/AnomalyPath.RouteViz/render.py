"""Отрисовка маршрутов, сгенерированных AnomalyPath.Core.

Читает JSON из tools/AnomalyPath.RouteViz (настоящий WorldGenerator на данных
Assets/Data) и собирает картинки: по одной на маршрут и общую схему сравнения.
Подписи берутся из Assets/Data/Localization/strings.ru.json.

Разметка: линия маршрута (хаб → звенья пути) идёт слева направо. Ветки
распределяются по сторонам — нечётные номера уходят вверх, чётные вниз, —
поэтому ветки не сбиваются в одну полосу под путём и используют всё поле листа.
"""

import json
import math
import os
import re
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image, ImageDraw, ImageFont

from route_layout import (
    BRANCH_LEVEL_BASE,
    BRANCH_LEVEL_STEP,
    EDGE_GAP,
    MARGIN,
    build_layout,
)

FONT_REG = r"C:\Windows\Fonts\segoeui.ttf"
FONT_BOLD = r"C:\Windows\Fonts\segoeuib.ttf"

BG = (15, 19, 26)
NODE_FILL = (32, 41, 54)
BUS = (58, 72, 94)
LINE = (78, 94, 118)
EXIT = (96, 132, 178)
TEXT = (232, 236, 245)
MUTED = (140, 152, 171)
FAINT = (98, 111, 131)

SUBTYPE_COLOR = {
    "hub": (232, 176, 75),
    "path": (143, 163, 191),
    "building": (91, 143, 214),
    "terrain": (95, 174, 114),
    "interior": (167, 128, 207),
}

SUBTYPE_LABEL = {
    "hub": "хаб",
    "path": "путь",
    "building": "здание",
    "terrain": "местность",
    "interior": "помещение",
}

VIA_LABEL = {
    "road": "дорога",
    "path": "тропа",
    "door": "дверь",
    "gate": "ворота",
    "stairs": "лестница",
    "hatch": "люк",
    "corridor": "коридор",
    "tunnel": "тоннель",
}

ROUTE_TITLE = {"small": "Малый", "medium": "Средний", "far": "Дальний"}

SPINE_RE = re.compile(r"^loc_e(\d+)_path_(\d+)$")
BRANCH_RE = re.compile(r"^loc_e(\d+)_b(\d+)_(\d+)$")
INTERIOR_RE = re.compile(r"^loc_e(\d+)_b(\d+)i_(\d+)$")


class Route:
    def __init__(self, data, strings):
        self.data = data
        self.strings = strings
        self.nodes = {node["id"]: node for node in data["nodes"]}
        self.spine = {}
        self.branches = {}
        self.interiors = {}
        self.hub = None
        self.vias = {}
        self.neighbours = {}
        for edge in data["edges"]:
            self.vias[tuple(sorted((edge["from"], edge["to"])))] = edge["via"]
            self.neighbours.setdefault(edge["from"], []).append(edge["to"])
            self.neighbours.setdefault(edge["to"], []).append(edge["from"])
        for node in data["nodes"]:
            if node["subtype"] == "hub":
                self.hub = node["id"]
                continue
            match = SPINE_RE.match(node["id"])
            if match:
                self.spine.setdefault(int(match.group(1)), []).append((int(match.group(2)), node["id"]))
                continue
            match = BRANCH_RE.match(node["id"])
            if match:
                self.branches.setdefault((int(match.group(1)), int(match.group(2))), []).append(
                    (int(match.group(3)), node["id"]))
                continue
            match = INTERIOR_RE.match(node["id"])
            if match:
                self.interiors.setdefault((int(match.group(1)), int(match.group(2))), []).append(
                    (int(match.group(3)), node["id"]))
        for items in self.spine.values():
            items.sort()
        for items in self.branches.values():
            items.sort()
        for items in self.interiors.values():
            items.sort()
        self.edges_count = data["routeEdges"]
        self.attach = {}
        self.attached = {}
        self.exit_to = {}
        for (edge, number), items in self.branches.items():
            chain = [node_id for _, node_id in items]
            head = chain[0]
            parents = [node for node in self.neighbours.get(head, []) if self._spine_edge(node) == edge]
            if parents:
                self.attach[(edge, number)] = sorted(parents)[0]
                parent = sorted(parents)[0]
                self.attached[parent] = self.attached.get(parent, 0) + 1
            exits = [node for node in self.neighbours.get(chain[-1], []) if self._spine_edge(node) == edge + 1]
            if exits:
                self.exit_to[(edge, number)] = sorted(exits)[0]

    @staticmethod
    def _spine_edge(node_id):
        match = SPINE_RE.match(node_id)
        return int(match.group(1)) if match else None

    def title(self, node):
        return self.strings.get(node["titleKey"], node["titleKey"])

    def short_title(self, node, limit=16):
        text = self.title(node)
        if len(text) <= limit:
            return text
        words = text.split(" ")
        while len(words) > 1 and len(" ".join(words)) > limit:
            words.pop()
        return " ".join(words) + "…"

    def via_between(self, left, right):
        return self.vias.get(tuple(sorted((left, right))))

    def owner_of_interior(self, key, index):
        chain = self.branches.get(key, [])
        position = index // 10
        return chain[position][1] if 0 <= position < len(chain) else None


def load(path):
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def font(size, bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT_REG, size)


def fit(draw, text, width, size, bold=False):
    current = size
    while current > 7:
        probe = font(current, bold)
        if draw.textlength(text, font=probe) <= width:
            return probe
        current -= 1
    return font(7, bold)


def dashed(draw, start, end, color, dash=5, gap=5, width=1):
    x1, y1 = start
    x2, y2 = end
    length = math.hypot(x2 - x1, y2 - y1)
    if length < 1:
        return
    offset = 0.0
    while offset < length:
        head = offset / length
        tail = min(1.0, (offset + dash) / length)
        draw.line([(x1 + (x2 - x1) * head, y1 + (y2 - y1) * head),
                   (x1 + (x2 - x1) * tail, y1 + (y2 - y1) * tail)], fill=color, width=width)
        offset += dash + gap


def draw_chain(draw, boxes, chain):
    for left_id, right_id in zip(chain, chain[1:]):
        a = boxes[left_id]
        b = boxes[right_id]
        acx = (a[0] + a[2]) // 2
        bcx = (b[0] + b[2]) // 2
        if b[1] >= a[3]:
            draw.line([(acx, a[3]), (bcx, b[1])], fill=LINE, width=2)
        elif b[3] <= a[1]:
            draw.line([(acx, a[1]), (bcx, b[3])], fill=LINE, width=2)
        else:
            draw.line([(a[2], (a[1] + a[3]) // 2), (b[0], (b[1] + b[3]) // 2)], fill=LINE, width=2)


def draw_node(draw, box, node, route):
    kind = node["subtype"]
    color = SUBTYPE_COLOR[kind]
    left, top, right, bottom = box
    draw.rounded_rectangle(box, radius=8, fill=NODE_FILL, outline=color, width=2 if kind == "hub" else 1)
    draw.rounded_rectangle((left + 2, top + 4, left + 6, bottom - 4), radius=2, fill=color)
    title = route.short_title(node)
    draw.text((left + 12, top + 6), title, font=fit(draw, title, right - left - 20, 11, kind == "hub"), fill=TEXT)
    meta = "хаб · старт" if kind == "hub" else "шаг {0} · {1}".format(node["step"], SUBTYPE_LABEL[kind])
    branches = route.attached.get(node["id"], 0)
    if branches > 0:
        meta += " · веток " + str(branches)
    if branches >= 2:
        draw.rounded_rectangle((left - 3, top - 3, right + 3, bottom + 3),
                               radius=11, outline=SUBTYPE_COLOR["hub"], width=2)
    draw.text((left + 12, top + 25), meta, font=font(9), fill=MUTED)


def panel(route):
    geom = build_layout(route)
    boxes = geom["boxes"]
    edges = geom["edges"]
    width = geom["width"]
    height = geom["height"]
    spine_top = geom["spine_top"]
    spine_bottom = geom["spine_bottom"]

    image = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(image)

    draw.text((MARGIN, 26), ROUTE_TITLE[route.data["name"]] + " маршрут", font=font(26, True), fill=TEXT)
    draw.text((MARGIN, 64), "seed {0} · звеньев {1} · локаций {2} · шагов в звене до {3} · окно {4}".format(
        route.data["seed"], route.data["routeEdges"], len(route.nodes),
        route.data["maxStep"], route.data["lookahead"]), font=font(13), fill=MUTED)
    above_total = sum(len(info["above"]) for info in edges.values())
    below_total = sum(len(info["below"]) for info in edges.values())
    draw.text((MARGIN, 86), "веток по звеньям: " + " / ".join(str(v) for v in route.data["branchCounts"]) +
              " · сверху {0} · снизу {1}".format(above_total, below_total),
              font=font(13), fill=MUTED)
    limit = route.data.get("maxTransitionsPerNode", 3)
    draw.text((MARGIN, 108),
              "предел переходов с локации: {0} (веток до {1}) · цена возврата в хаб: {2} вын. × шаг, "
              "на новом звене снова {2}".format(limit, max(0, limit - 1), route.data["hubReturnStamina"]),
              font=font(13), fill=MUTED)

    counts = {}
    for node in route.nodes.values():
        counts[node["subtype"]] = counts.get(node["subtype"], 0) + 1
    summary = " · ".join("{0} {1}".format(SUBTYPE_LABEL[kind], counts[kind])
                         for kind in ("path", "building", "terrain") if kind in counts)
    draw.text((width - MARGIN - draw.textlength(summary, font=font(13)), 86), summary,
              font=font(13), fill=MUTED)

    # звенья: скобка над группой путей; если сверху есть ветки — выше них
    for edge, info in edges.items():
        left = info["start"]
        right = info["spine_end"]
        if right <= left:
            continue
        y = info["bracket_y"]
        draw.line([(left, y + 10), (right, y + 10)], fill=LINE, width=1)
        draw.line([(left, y + 4), (left, y + 16)], fill=LINE, width=1)
        draw.line([(right, y + 4), (right, y + 16)], fill=LINE, width=1)
        edge_spine = route.spine.get(edge, [])
        step = max(route.nodes[node_id]["step"] for _, node_id in edge_spine) if edge_spine else 0
        label = "звено {0} · {1} · в хаб {2}".format(
            edge, len(edge_spine), step * route.data["hubReturnStamina"])
        draw.text(((left + right) // 2 - draw.textlength(label, font=font(11, True)) / 2, y - 16),
                  label, font=font(11, True), fill=SUBTYPE_COLOR["path"])

    # ветки: шина идёт к своей стороне; несколько веток с одной локации получают разные точки отхода
    for edge, info in edges.items():
        for side in ("above", "below"):
            slots = info[side]
            if not slots:
                continue
            levels = {slot["key"]: index for index, slot in enumerate(slots)}
            groups = {}
            for slot in slots:
                attach = route.attach.get(slot["key"])
                if attach is None:
                    attach = info["spine"][-1] if info["spine"] else None
                if attach is None:
                    continue
                groups.setdefault(attach, []).append(slot)

            for attach, group in groups.items():
                attach_box = boxes[attach]
                group.sort(key=lambda slot: slot["key"][1])
                for index, slot in enumerate(group):
                    head = boxes[slot["chain"][0]]
                    ax = attach_box[0] + (attach_box[2] - attach_box[0]) * (index + 1) // (len(group) + 1)
                    bx = (head[0] + head[2]) // 2
                    level = levels[slot["key"]]
                    if side == "below":
                        bus_y = spine_bottom + BRANCH_LEVEL_BASE + level * BRANCH_LEVEL_STEP
                        from_y = attach_box[3]
                        head_y = head[1]
                        label_y = head[1] - 13
                    else:
                        bus_y = spine_top - BRANCH_LEVEL_BASE - level * BRANCH_LEVEL_STEP
                        from_y = attach_box[1]
                        head_y = head[3]
                        label_y = head[3] + 3
                    draw.line([(ax, from_y), (ax, bus_y)], fill=BUS, width=2)
                    draw.line([(ax, bus_y), (bx, bus_y)], fill=BUS, width=2)
                    draw.line([(bx, bus_y), (bx, head_y)], fill=BUS, width=2)
                    draw.ellipse((ax - 3, from_y - 3, ax + 3, from_y + 3), fill=SUBTYPE_COLOR["path"])
                    draw.text((bx + 8, label_y),
                              "от шага {0}".format(route.nodes[attach]["step"]),
                              font=font(8), fill=FAINT)

    # выходы веток: концы сверху уходят на верхнюю шину, снизу — на нижнюю
    for edge, info in edges.items():
        for side in ("above", "below"):
            slots = info[side]
            if not slots:
                continue
            rail_y = info["rail_above_y"] if side == "above" else info["rail_below_y"]
            if rail_y is None:
                continue
            ends = [boxes[slot["chain"][-1]] for slot in slots]
            xs = []
            for box in ends:
                x = (box[0] + box[2]) // 2
                xs.append(x)
                if side == "above":
                    dashed(draw, (x, box[1]), (x, rail_y), EXIT, width=1)
                    draw.ellipse((x - 2, box[1] - 2, x + 2, box[1] + 2), fill=EXIT)
                else:
                    dashed(draw, (x, box[3]), (x, rail_y), EXIT, width=1)
                    draw.ellipse((x - 2, box[3] - 2, x + 2, box[3] + 2), fill=EXIT)
            left = min(xs)
            prefix = "сверху" if side == "above" else "снизу"
            if edge < route.edges_count and route.spine.get(edge + 1):
                target = boxes[route.spine[edge + 1][0][1]]
                gutter = edges[edge + 1]["start"] - EDGE_GAP // 2
                gutter += -8 if side == "above" else 8
                y = (target[1] + target[3]) // 2
                draw.line([(left, rail_y), (gutter, rail_y)], fill=EXIT, width=2)
                draw.line([(gutter, rail_y), (gutter, y)], fill=EXIT, width=2)
                draw.line([(gutter, y), (target[0] - 2, y)], fill=EXIT, width=2)
                draw.polygon([(target[0], y), (target[0] - 9, y - 5), (target[0] - 9, y + 5)], fill=EXIT)
                label = "{0} → звено {1} · шаг 1".format(prefix, edge + 1)
                middle = (left + gutter) // 2
            else:
                right = max(xs) + 84
                draw.line([(left, rail_y), (right, rail_y)], fill=EXIT, width=2)
                draw.polygon([(right + 10, rail_y), (right, rail_y - 5), (right, rail_y + 5)], fill=EXIT)
                label = "{0} → следующее звено (достроится)".format(prefix)
                middle = (left + right) // 2
            draw.text((middle - draw.textlength(label, font=font(9)) / 2, rail_y - 15),
                      label, font=font(9), fill=FAINT)

    # связи внутри пути
    spine_chain = [route.hub]
    for edge in range(1, route.edges_count + 1):
        spine_chain.extend(node_id for _, node_id in route.spine.get(edge, []))
    for left_id, right_id in zip(spine_chain, spine_chain[1:]):
        a = boxes[left_id]
        b = boxes[right_id]
        start = (a[2], (a[1] + a[3]) // 2)
        end = (b[0], (b[1] + b[3]) // 2)
        draw.line([start, end], fill=LINE, width=3)
        via = route.via_between(left_id, right_id)
        if via:
            label = VIA_LABEL.get(via, via)
            draw.text(((start[0] + end[0]) / 2 - draw.textlength(label, font=font(8)) / 2,
                       start[1] - 16), label, font=font(8), fill=FAINT)

    for edge, info in edges.items():
        for slot in info["slots"]:
            draw_chain(draw, boxes, slot["chain"])
            for index, node_id in slot["rooms"]:
                owner = route.owner_of_interior(slot["key"], index)
                if owner is None:
                    continue
                a = boxes[owner]
                b = boxes[node_id]
                draw.line([(a[2], (a[1] + a[3]) // 2), (b[0], (b[1] + b[3]) // 2)], fill=LINE, width=1)

    for node_id, box in boxes.items():
        draw_node(draw, box, route.nodes[node_id], route)

    legend_y = height - 84
    draw.text((MARGIN, legend_y), "подтипы:", font=font(11, True), fill=MUTED)
    x = MARGIN + 74
    for kind in ("hub", "path", "building", "terrain", "interior"):
        draw.rounded_rectangle((x, legend_y + 1, x + 12, legend_y + 13), radius=3, fill=SUBTYPE_COLOR[kind])
        draw.text((x + 18, legend_y - 1), SUBTYPE_LABEL[kind], font=font(11), fill=MUTED)
        x += 104
    draw.text((MARGIN, legend_y + 26),
              "линия — переход (подпись: способ), шина — ветка от локации звена "
              "(нечётные сверху, чётные снизу), пунктир — выход ветки в первую локацию следующего "
              "звена (шаг 1), золотая рамка — с локации две ветки",
              font=font(11), fill=FAINT)
    return image


def overview(routes, path):
    scale = 26
    dot = 10
    panel_h = 500
    width = 760
    height = 76 + panel_h * len(routes)
    image = Image.new("RGB", (width, max(height, 380)), BG)
    draw = ImageDraw.Draw(image)
    for index, route in enumerate(routes):
        top = 50 + index * panel_h
        draw.text((MARGIN, top), ROUTE_TITLE[route.data["name"]] + " маршрут", font=font(22, True), fill=TEXT)
        draw.text((MARGIN + draw.textlength(ROUTE_TITLE[route.data["name"]] + " маршрут", font=font(22, True)) + 18,
                   top + 8),
                  "звеньев {0} · локаций {1}".format(route.data["routeEdges"], len(route.nodes)),
                  font=font(14), fill=MUTED)
        spine_ids = [route.hub]
        for edge in range(1, route.edges_count + 1):
            spine_ids.extend(node_id for _, node_id in route.spine.get(edge, []))
        y = top + 210
        points = {}
        for order, node_id in enumerate(spine_ids):
            points[node_id] = (MARGIN + order * scale, y)
        for left_id, right_id in zip(spine_ids, spine_ids[1:]):
            draw.line([points[left_id], points[right_id]], fill=LINE, width=2)
        group = {}
        for key in sorted(route.branches):
            attach = route.attach.get(key)
            if attach is None or attach not in points:
                spine = route.spine.get(key[0], [])
                if not spine:
                    continue
                attach = spine[-1][1]
            group.setdefault(attach, []).append(key)

        for attach, keys in group.items():
            point = points[attach]
            for order, key in enumerate(sorted(keys)):
                chain = [node_id for _, node_id in route.branches[key]]
                shift = int(round((order - (len(keys) - 1) / 2.0) * 10))
                x = point[0] + shift
                direction = -1 if order % 2 == 0 else 1
                first = (x, point[1] + direction * 12)
                draw.line([point, first], fill=BUS, width=2)
                for depth, node_id in enumerate(chain):
                    spot = (x, point[1] + direction * (16 + depth * 15))
                    previous = first if depth == 0 else (x, point[1] + direction * (16 + (depth - 1) * 15))
                    draw.line([previous, spot], fill=BUS, width=2)
                    draw.ellipse((spot[0] - 3, spot[1] - 3, spot[0] + 3, spot[1] + 3),
                                 fill=SUBTYPE_COLOR[route.nodes[node_id]["subtype"]])
                if key in route.exit_to:
                    tail = (x, point[1] + direction * (16 + (len(chain) - 1) * 15))
                    draw.rectangle((tail[0] - 6, tail[1] - 6, tail[0] + 6, tail[1] + 6), outline=EXIT)

        for node_id in spine_ids:
            x, y2 = points[node_id]
            color = SUBTYPE_COLOR[route.nodes[node_id]["subtype"]]
            draw.ellipse((x - dot // 2, y2 - dot // 2, x + dot // 2, y2 + dot // 2), fill=color)
    draw.text((MARGIN, height - 48),
              "точка — локация, цвет — подтип; синяя рамка — конец ветки: из неё переход в начало следующего звена",
              font=font(12), fill=FAINT)
    draw.text((MARGIN, height - 28),
              "шаг по горизонтали одинаков для всех маршрутов; ветки идут сверху и снизу от своих локаций звена",
              font=font(12), fill=FAINT)
    image.save(path)


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else ".build/route-viz"
    out = sys.argv[2] if len(sys.argv) > 2 else "docs/visualizations"
    strings = load(os.path.join("Assets", "Data", "Localization", "strings.ru.json"))
    os.makedirs(out, exist_ok=True)
    routes = []
    for name in ("small", "medium", "far"):
        route = Route(load(os.path.join(root, "route-" + name + ".json")), strings)
        routes.append(route)
        image = panel(route)
        target = os.path.join(out, "route-" + name + ".png")
        image.save(target, optimize=True)
        print("{0}: {1}x{2}".format(target, image.width, image.height))
    overview(routes, os.path.join(out, "routes-overview.png"))
    print("overview: routes-overview.png")


if __name__ == "__main__":
    main()
