"""生成塔防占位美术素材（纯几何图形，风格统一，可直接替换）。"""
import os
import math
from PIL import Image, ImageDraw

OUT = r"C:\Users\lbw13\Documents\deepseek-harness\default-workspace\TowerDefenseKit\Assets\Art\Placeholder"
os.makedirs(OUT, exist_ok=True)

S = 4  # 4 倍超采样，缩小时抗锯齿

C = {
    "bg":      (27, 34, 51, 255),
    "stone":   (44, 52, 72, 255),
    "stone_l": (58, 69, 96, 255),
    "stone_d": (24, 30, 44, 255),
    "line":    (14, 18, 28, 255),
    "gold":    (242, 193, 78, 255),
    "wood":    (138, 90, 59, 255),
    "wood_l":  (166, 112, 76, 255),
    "iron":    (74, 85, 104, 255),
    "iron_l":  (104, 118, 142, 255),
    "frost":   (127, 211, 242, 255),
    "frost_l": (196, 238, 252, 255),
    "e_grunt": (143, 163, 200, 255),
    "e_run":   (242, 162, 92, 255),
    "e_arm":   (110, 110, 120, 255),
    "e_heal":  (126, 208, 138, 255),
    "white":   (255, 255, 255, 255),
    "hp_bg":   (12, 16, 24, 220),
    "hp":      (89, 227, 107, 255),
    "panel":   (30, 38, 56, 235),
    "panel_b": (86, 104, 140, 255),
}


def shade(color, k):
    r, g, b, a = color
    return (max(0, min(255, int(r * k))), max(0, min(255, int(g * k))), max(0, min(255, int(b * k))), a)


def new(w, h):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))


def save(img, w, h, name):
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name))


def box(d, xy, r, fill=None, outline=None, width=0):
    d.rounded_rectangle([v * S for v in xy], radius=r * S, fill=fill, outline=outline,
                        width=max(1, int(width * S)) if width else 0)


def ell(d, xy, fill=None, outline=None, width=0):
    d.ellipse([v * S for v in xy], fill=fill, outline=outline,
              width=max(1, int(width * S)) if width else 0)


def poly(d, pts, fill=None, outline=None, width=0):
    d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline,
              width=max(1, int(width * S)) if width else 0)


def ln(d, pts, fill, width):
    d.line([(x * S, y * S) for x, y in pts], fill=fill, width=max(1, int(width * S)), joint="curve")


def eyes(d, cx, cy, gap, r, look=0):
    for sx in (-1, 1):
        ex = cx + sx * gap
        ell(d, (ex - r, cy - r, ex + r, cy + r), C["white"])
        ell(d, (ex - r * 0.45 + look, cy - r * 0.45, ex + r * 0.45 + look, cy + r * 0.45), C["line"])


# ---------------- 地块 / 范围圈 / UI ----------------

def tile():
    w = h = 128
    img = new(w, h)
    d = ImageDraw.Draw(img)
    box(d, (4, 4, 124, 124), 14, C["stone"], C["line"], 3)
    box(d, (14, 14, 114, 114), 10, C["stone_l"], None)
    ell(d, (58, 58, 70, 70), C["stone"])
    for x, y in ((20, 20), (108, 20), (20, 108), (108, 108)):
        ell(d, (x - 4, y - 4, x + 4, y + 4), C["stone_d"])
    save(img, w, h, "tile_ground.png")


def ring():
    w = h = 256
    img = new(w, h)
    d = ImageDraw.Draw(img)
    for i, (rr, a) in enumerate(((118, 90), (112, 160), (106, 230))):
        ell(d, (128 - rr, 128 - rr, 128 + rr, 128 + rr), None, (255, 255, 255, a), 4)
    save(img, w, h, "vfx_range_circle.png")


def health():
    for name, color in (("ui_health_bg.png", C["hp_bg"]), ("ui_health_fill.png", C["hp"])):
        w, h = 64, 12
        img = new(w, h)
        d = ImageDraw.Draw(img)
        box(d, (0, 0, w, h), 6, color)
        save(img, w, h, name)


def panel():
    w = h = 128
    img = new(w, h)
    d = ImageDraw.Draw(img)
    box(d, (2, 2, 126, 126), 20, C["panel"], C["panel_b"], 3)
    box(d, (12, 12, 116, 116), 14, None, (255, 255, 255, 18), 2)
    save(img, w, h, "ui_panel.png")


# ---------------- 敌人 ----------------

def enemy_grunt():
    w = h = 96
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ell(d, (10, 46, 86, 92), C["line"])          # 影子
    ell(d, (14, 10, 82, 78), C["e_grunt"], C["line"], 3)
    ell(d, (22, 18, 74, 58), shade(C["e_grunt"], 1.18))
    eyes(d, 48, 44, 13, 7)
    save(img, w, h, "enemy_grunt.png")


def enemy_runner():
    w = h = 96
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ell(d, (14, 52, 82, 90), C["line"])
    poly(d, [(48, 6), (86, 50), (48, 84), (10, 50)], C["e_run"], C["line"], 3)
    poly(d, [(48, 18), (72, 48), (48, 70), (24, 48)], shade(C["e_run"], 1.15))
    eyes(d, 48, 46, 10, 6)
    save(img, w, h, "enemy_runner.png")


def enemy_armored():
    w = h = 112
    img = new(w, h)
    d = ImageDraw.Draw(img)
    cx, cy, r = 56, 54, 46
    pts = [(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a)))
           for a in range(-90, 270, 60)]
    ell(d, (18, 62, 94, 102), C["line"])
    poly(d, pts, C["e_arm"], C["line"], 3)
    inner = [(cx + r * 0.62 * math.cos(math.radians(a)), cy + r * 0.62 * math.sin(math.radians(a)))
             for a in range(-90, 270, 60)]
    poly(d, inner, shade(C["e_arm"], 1.2))
    for a in range(0, 360, 60):
        x1 = cx + r * 0.6 * math.cos(math.radians(a - 90))
        y1 = cy + r * 0.6 * math.sin(math.radians(a - 90))
        ln(d, [(cx, cy), (x1, y1)], shade(C["e_arm"], 0.75), 2)
    eyes(d, cx, cy - 6, 12, 6)
    save(img, w, h, "enemy_armored.png")


def enemy_healer():
    w = h = 96
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ell(d, (12, 50, 84, 92), C["line"])
    ell(d, (14, 10, 82, 78), C["e_heal"], C["line"], 3)
    ell(d, (24, 18, 72, 56), shade(C["e_heal"], 1.2))
    box(d, (42, 26, 54, 62), 3, C["white"])
    box(d, (30, 38, 66, 50), 3, C["white"])
    ell(d, (40, 36, 56, 52), C["white"])
    save(img, w, h, "enemy_healer.png")


# ---------------- 塔 ----------------

def tower_base(accent, name):
    w = h = 128
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ell(d, (18, 74, 110, 118), C["line"])
    ell(d, (22, 30, 106, 110), C["stone"], C["line"], 3)
    ell(d, (30, 38, 98, 102), shade(C["stone"], 1.15))
    ell(d, (44, 52, 84, 90), accent, C["line"], 2)
    for x, y in ((30, 96), (98, 96), (34, 66), (94, 66)):
        ell(d, (x - 4, y - 4, x + 4, y + 4), C["stone_d"])
    save(img, w, h, name)


def tower_head(body, name, kind="barrel"):
    w = h = 64
    img = new(w, h)
    d = ImageDraw.Draw(img)
    if kind == "barrel":
        box(d, (24, 4, 40, 58), 7, body, C["line"], 3)
        box(d, (27, 8, 37, 26), 4, shade(body, 1.2))
        ell(d, (24, 2, 40, 12), shade(body, 0.8), C["line"], 2)
    elif kind == "cannon":
        box(d, (18, 8, 46, 58), 10, body, C["line"], 3)
        ell(d, (18, 2, 46, 22), shade(body, 0.8), C["line"], 3)
        box(d, (24, 16, 40, 40), 6, shade(body, 1.2))
    else:  # crystal
        poly(d, [(32, 2), (50, 30), (32, 58), (14, 30)], body, C["line"], 3)
        poly(d, [(32, 10), (43, 30), (32, 48), (21, 30)], shade(body, 1.25))
    save(img, w, h, name)


# ---------------- 子弹 ----------------

def proj_arrow():
    w, h = 24, 48
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ln(d, [(12, 44), (12, 14)], C["wood"], 4)
    poly(d, [(12, 2), (20, 18), (12, 14), (4, 18)], C["iron_l"], C["line"], 2)
    poly(d, [(12, 44), (18, 34), (6, 34)], C["wood_l"])
    save(img, w, h, "proj_arrow.png")


def proj_cannon():
    w = h = 32
    img = new(w, h)
    d = ImageDraw.Draw(img)
    ell(d, (3, 3, 29, 29), C["iron"], C["line"], 3)
    ell(d, (8, 7, 20, 19), C["iron_l"])
    save(img, w, h, "proj_cannon.png")


def proj_frost():
    w, h = 24, 44
    img = new(w, h)
    d = ImageDraw.Draw(img)
    poly(d, [(12, 2), (21, 22), (12, 42), (3, 22)], C["frost"], C["line"], 2)
    poly(d, [(12, 10), (17, 22), (12, 34), (7, 22)], C["frost_l"])
    save(img, w, h, "proj_frost.png")


tile()
ring()
health()
panel()
enemy_grunt()
enemy_runner()
enemy_armored()
enemy_healer()
tower_base(C["wood_l"], "tower_arrow_base.png")
tower_head(C["wood"], "tower_arrow_head.png", "barrel")
tower_base(C["iron_l"], "tower_cannon_base.png")
tower_head(C["iron"], "tower_cannon_head.png", "cannon")
tower_base(C["frost"], "tower_frost_base.png")
tower_head(C["frost"], "tower_frost_head.png", "crystal")
proj_arrow()
proj_cannon()
proj_frost()

print("done:", len(os.listdir(OUT)), "files")
for f in sorted(os.listdir(OUT)):
    print("  ", f, Image.open(os.path.join(OUT, f)).size)
