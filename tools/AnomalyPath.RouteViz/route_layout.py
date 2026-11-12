"""Разметка картинки маршрута: путь идёт слева направо, ветки — сверху и снизу.

Модуль только считает рамки: где стоит хаб, локации пути, ветки и выходные линии.
Рисование — в render.py. Ветки распределяются по сторонам по чётности номера:
чётные (0, 2, …) уходят вниз, нечётные (1, 3, …) — вверх, поэтому информация
не сбивается в одну широкую полосу под путём.
"""

MARGIN = 46
HEADER_H = 152
SPINE_W = 158
SPINE_H = 54
SPINE_GAP = 26
EDGE_GAP = 70
NODE_W = 146
NODE_H = 44
ROW_STEP = 52
ROOM_W = 120
ROOM_H = 36
SLOT_GAP = 16
BRANCH_GAP_BASE = 34
BRANCH_LEVEL_BASE = 16
BRANCH_LEVEL_STEP = 12
MIN_SPINE_Y = HEADER_H + 74
TOP_SPACE = 210


def _slots(route, edge):
    """Ветки звена с готовыми цепочками и стороной."""
    slots = []
    for key in sorted(key for key in route.branches if key[0] == edge):
        chain = [node_id for _, node_id in route.branches[key]]
        if not chain:
            continue
        rooms = route.interiors.get(key, [])
        width = NODE_W + (ROOM_W + 24 if rooms else 0)
        slots.append({
            "key": key,
            "chain": chain,
            "rooms": rooms,
            "width": width,
            "side": "above" if key[1] % 2 == 1 else "below",
            "x": 0,
        })

    return slots


def build_layout(route):
    """Считает рамки всех локаций, рёбра и итоговый размер листа."""
    slots_by_edge = {}
    max_side_slots = 0
    max_above_rows = 0
    for edge in range(1, route.edges_count + 1):
        slots = _slots(route, edge)
        slots_by_edge[edge] = slots
        above = [slot for slot in slots if slot["side"] == "above"]
        below = [slot for slot in slots if slot["side"] == "below"]
        max_side_slots = max(max_side_slots, len(above), len(below))
        if above:
            max_above_rows = max(max_above_rows, max(len(slot["chain"]) for slot in above))

    branch_gap = BRANCH_GAP_BASE + max(0, max_side_slots - 1) * BRANCH_LEVEL_STEP
    above_height = NODE_H + max(0, max_above_rows - 1) * ROW_STEP if max_above_rows else 0
    spine_y = max(MIN_SPINE_Y, TOP_SPACE + SPINE_H // 2 + branch_gap + above_height)
    spine_top = spine_y - SPINE_H // 2
    spine_bottom = spine_y + SPINE_H // 2

    boxes = {}
    if route.hub:
        boxes[route.hub] = (MARGIN, spine_top, MARGIN + 176, spine_bottom)

    edges = {}
    x = MARGIN + 176 + EDGE_GAP
    for edge in range(1, route.edges_count + 1):
        start = x
        spine_ids = [node_id for _, node_id in route.spine.get(edge, [])]
        for node_id in spine_ids:
            boxes[node_id] = (x, spine_top, x + SPINE_W, spine_bottom)
            x += SPINE_W + SPINE_GAP
        spine_end = x - SPINE_GAP if spine_ids else start
        spine_width = max(SPINE_W, spine_end - start)

        slots = slots_by_edge[edge]
        above = [slot for slot in slots if slot["side"] == "above"]
        below = [slot for slot in slots if slot["side"] == "below"]

        above_x = start
        for slot in above:
            slot["x"] = above_x
            above_x += slot["width"] + SLOT_GAP
        above_width = max(0, above_x - start - SLOT_GAP)

        below_x = start
        for slot in below:
            slot["x"] = below_x
            below_x += slot["width"] + SLOT_GAP
        below_width = max(0, below_x - start - SLOT_GAP)

        column = max(spine_width, above_width, below_width)
        end = start + column
        edges[edge] = {
            "start": start,
            "spine_end": spine_end,
            "end": end,
            "column": column,
            "spine": spine_ids,
            "slots": slots,
            "above": above,
            "below": below,
        }
        x = end + EDGE_GAP

    below_top = spine_bottom + branch_gap
    above_bottom = spine_top - branch_gap
    for info in edges.values():
        for slot in info["slots"]:
            x = slot["x"]
            for index, node_id in enumerate(slot["chain"]):
                if slot["side"] == "below":
                    top = below_top + index * ROW_STEP
                else:
                    top = above_bottom - NODE_H - index * ROW_STEP
                boxes[node_id] = (x, top, x + NODE_W, top + NODE_H)

            for index, node_id in slot["rooms"]:
                owner = route.owner_of_interior(slot["key"], index)
                if owner is None:
                    continue
                owner_box = boxes[owner]
                left = x + NODE_W + 12
                boxes[node_id] = (left, owner_box[1] + 4, left + ROOM_W, owner_box[1] + 4 + ROOM_H)

    top_boxes = min(box[1] for box in boxes.values()) if boxes else spine_top
    bottom_boxes = max(box[3] for box in boxes.values()) if boxes else spine_bottom
    for info in edges.values():
        above_boxes = [boxes[node_id] for slot in info["above"] for node_id in slot["chain"]]
        below_boxes = [boxes[node_id] for slot in info["below"] for node_id in slot["chain"]]
        edge_top = min(box[1] for box in above_boxes) if above_boxes else spine_top
        edge_bottom = max(box[3] for box in below_boxes) if below_boxes else spine_bottom
        info["above_top"] = edge_top
        info["below_bottom"] = edge_bottom
        info["rail_above_y"] = edge_top - 22 if above_boxes else None
        info["rail_below_y"] = edge_bottom + 22 if below_boxes else None
        info["bracket_y"] = edge_top - 50 if above_boxes else spine_top - 30

    width = max(
        [box[2] for box in boxes.values()] + [info["end"] for info in edges.values()] + [MARGIN]
    ) + MARGIN
    height = bottom_boxes + 160
    return {
        "boxes": boxes,
        "edges": edges,
        "width": width,
        "height": height,
        "spine_y": spine_y,
        "spine_top": spine_top,
        "spine_bottom": spine_bottom,
        "branch_gap": branch_gap,
        "top_boxes": top_boxes,
        "bottom_boxes": bottom_boxes,
    }
