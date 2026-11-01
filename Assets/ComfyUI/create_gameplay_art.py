"""Create deterministic gameplay art from the Blender hero render.

The game uses tiny sprites, so these assets are deliberately designed with a strong
silhouette, large color blocks and transparent edges.  The skill icons are vector-like
PNG art so they stay crisp at the 72 px upgrade-card size.
"""
from pathlib import Path
import math

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Resources" / "Art"


def rgba(hex_color, alpha=255):
    value = hex_color.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4)) + (alpha,)


PILOT_PALETTES = {
    "rei": ("#0B3742", "#18E2E8", "#F4C84D", "#061B2B"),
    "rikka": ("#762631", "#FF7651", "#FFC14D", "#211627"),
    "rin": ("#173B69", "#7EEBFF", "#EAF7FF", "#0C1B3C"),
    "mei": ("#482B78", "#D683FF", "#F5A8C6", "#24153E"),
    "yuna": ("#20265E", "#28D9FF", "#FF5CCB", "#10162F"),
}


def luminance(r, g, b):
    return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0


def blend_color(target, value, minimum=0.18):
    strength = minimum + value * (1.0 - minimum)
    return tuple(max(0, min(255, int(channel * strength))) for channel in target)


def make_pilot_models():
    # Original, transparent 2D anime-style minis, rather than recolors of the old
    # helmet placeholder.  Supersampling keeps their face and gear clean in battle.
    scale = 4
    w, h = 256, 320

    def pt(v):
        if isinstance(v, (int, float)):
            return int(round(v * scale))
        return tuple(int(round(n * scale)) for n in v)

    def draw_pilot(key):
        primary_hex, accent_hex, secondary_hex, dark_hex = PILOT_PALETTES[key]
        primary, accent, secondary, dark = [rgba(x)[:3] for x in (primary_hex, accent_hex, secondary_hex, dark_hex)]
        skin, skin_shadow = (255, 220, 198), (233, 166, 154)
        hair = {"rei": (31, 53, 99), "rikka": (240, 94, 72), "rin": (211, 232, 247), "mei": (153, 105, 205), "yuna": (55, 43, 101)}[key]
        hair_light = {"rei": (73, 126, 203), "rikka": (255, 157, 91), "rin": (255, 255, 255), "mei": (216, 164, 255), "yuna": (151, 83, 205)}[key]
        iris = {"rei": (45, 221, 255), "rikka": (255, 177, 61), "rin": (82, 194, 255), "mei": (190, 126, 255), "yuna": (53, 223, 255)}[key]
        outline = (20, 22, 42)
        image = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
        halo = Image.new("RGBA", image.size, (0, 0, 0, 0))
        hd = ImageDraw.Draw(halo)
        hd.ellipse(pt((48, 34, 210, 225)), fill=(*accent, 38))
        image = Image.alpha_composite(image, halo.filter(ImageFilter.GaussianBlur(20 * scale)))
        d = ImageDraw.Draw(image)

        def poly(points, fill, stroke=outline, sw=3):
            points = [pt(p) for p in points]
            d.polygon(points, fill=fill)
            if stroke:
                d.line(points + [points[0]], fill=stroke, width=max(1, int(sw * scale)), joint="curve")

        def line(points, fill, sw=3, joint="curve"):
            d.line([pt(p) for p in points], fill=fill, width=max(1, int(sw * scale)), joint=joint)

        def oval(box, fill, stroke=None, sw=2):
            d.ellipse(pt(box), fill=fill, outline=stroke, width=max(1, int(sw * scale)) if stroke else 1)

        def roundrect(box, radius, fill, stroke=None, sw=2):
            d.rounded_rectangle(pt(box), radius=int(radius * scale), fill=fill, outline=stroke,
                                width=max(1, int(sw * scale)) if stroke else 1)

        # Ground shadow and booted legs.
        oval((77, 287, 181, 305), (5, 9, 22, 88))
        for x in (98, 139):
            poly(((x, 234), (x + 30, 234), (x + 27, 280), (x + 3, 280)), dark, outline)
            poly(((x - 2, 270), (x + 29, 270), (x + 34, 296), (x - 6, 296)), primary, outline)
            line(((x + 1, 281), (x + 27, 281)), (*accent, 235), 3)
            oval((x + 4, 286, x + 9, 291), secondary)

        # Weapon held to one side, drawn behind the arm.
        if key == "rei":
            roundrect((190, 176, 224, 199), 6, dark, accent, 2)
            roundrect((197, 168, 222, 184), 4, primary, outline, 2)
            line(((217, 174), (234, 164)), (*accent, 255), 5)
            oval((228, 156, 238, 166), (*accent, 235))
        elif key == "rikka":
            roundrect((185, 182, 228, 209), 6, dark, secondary, 2)
            for y in (183, 194):
                roundrect((209, y - 4, 240, y + 2), 2, secondary, outline, 1)
            line(((200, 204), (198, 220)), dark, 7)
            oval((236, 176, 242, 182), (*accent, 250))
        elif key == "rin":
            line(((191, 237), (225, 139)), dark, 9)
            line(((193, 237), (227, 139)), (*accent, 245), 4)
            poly(((225, 143), (223, 112), (241, 139), (230, 154)), secondary, (*accent, 255), 2)
            line(((197, 220), (210, 227)), (244, 253, 255, 245), 3)
        elif key == "mei":
            # Orbiting crescents flank the character rather than obscure her face.
            d.arc(pt((31, 127, 80, 190)), 210, 340, fill=(*accent, 250), width=5 * scale)
            poly(((45, 154), (35, 143), (52, 145)), secondary, (*accent, 245), 2)
            d.arc(pt((179, 122, 225, 187)), 18, 148, fill=(*accent, 250), width=5 * scale)
            poly(((211, 151), (222, 139), (216, 161)), secondary, (*accent, 245), 2)
        else:
            line(((202, 234), (225, 151)), dark, 8)
            line(((204, 231), (227, 149)), (*accent, 245), 3)
            poly(((224, 153), (218, 130), (236, 146), (230, 164)), secondary, (*accent, 255), 2)
            line(((225, 147), (237, 141), (232, 133), (245, 128)), (*accent, 245), 3)

        # Jacket torso, sleeves and small luminous seams.
        poly(((91, 162), (112, 154), (129, 170), (145, 154), (169, 162), (189, 187), (175, 224), (166, 251), (91, 251), (81, 222), (67, 188)), primary, outline, 4)
        poly(((111, 160), (127, 175), (143, 160), (158, 169), (151, 203), (127, 219), (103, 203), (98, 170)), dark, outline, 2)
        poly(((111, 165), (127, 178), (143, 165), (139, 191), (127, 199), (115, 191)), secondary, outline, 2)
        poly(((82, 178), (103, 170), (112, 203), (97, 223), (80, 215)), primary, outline, 2)
        poly(((165, 173), (183, 183), (179, 216), (163, 224), (151, 205)), primary, outline, 2)
        # Hands and glove cuffs.
        oval((78, 210, 97, 232), skin, outline, 2)
        oval((163, 211, 182, 233), skin, outline, 2)
        line(((86, 213), (90, 225)), (*accent, 250), 3)
        line(((171, 214), (168, 225)), (*accent, 250), 3)
        # Collar, central fastener, belt and tech piping.
        poly(((105, 160), (126, 177), (115, 188), (99, 169)), secondary, outline, 2)
        poly(((149, 160), (128, 177), (139, 188), (157, 169)), secondary, outline, 2)
        line(((127, 194), (127, 235)), (*accent, 235), 3)
        roundrect((94, 228, 161, 238), 4, dark, secondary, 2)
        oval((120, 226, 135, 241), (*accent, 255), (235, 255, 255), 2)
        for y in (205, 216):
            oval((96, y, 102, y + 6), (*accent, 240))
        poly(((109, 186), (119, 191), (115, 202), (105, 197)), (*accent, 200), None)

        # Character-specific long hair and signature silhouette.
        if key == "rikka":
            poly(((77, 68), (60, 83), (66, 150), (83, 171), (91, 129)), hair, outline, 3)
            poly(((177, 70), (194, 85), (186, 160), (171, 174), (168, 126)), hair, outline, 3)
            line(((184, 77), (205, 69), (217, 78), (207, 94), (190, 88)), (*accent, 255), 6)
        elif key == "rin":
            poly(((72, 79), (57, 95), (65, 225), (83, 248), (88, 153)), hair, outline, 3)
            poly(((181, 74), (201, 95), (195, 224), (177, 249), (169, 155)), hair, outline, 3)
            line(((70, 153), (70, 206)), (*accent, 230), 3)
            line(((187, 147), (185, 210)), (*accent, 230), 3)
            poly(((182, 81), (197, 66), (201, 91), (184, 100)), secondary, outline, 2)
        elif key == "mei":
            oval((64, 43, 98, 79), hair, outline, 3)
            oval((157, 41, 193, 79), hair, outline, 3)
            oval((75, 50, 88, 66), hair_light)
            oval((169, 48, 183, 65), hair_light)
            poly(((72, 75), (62, 97), (72, 147), (89, 159), (91, 111)), hair, outline, 3)
            poly(((181, 74), (196, 100), (184, 149), (166, 161), (165, 105)), hair, outline, 3)
        elif key == "yuna":
            poly(((161, 46), (196, 34), (218, 52), (206, 112), (186, 138), (171, 100)), hair, outline, 3)
            line(((188, 61), (207, 55), (208, 82), (194, 102)), (*secondary, 255), 5)
            roundrect((160, 56, 176, 74), 5, secondary, outline, 1)
        else:
            poly(((78, 83), (67, 100), (74, 133), (88, 120)), hair, outline, 2)

        # Neck, face and anime eyes.
        poly(((112, 139), (145, 139), (149, 169), (128, 181), (106, 169)), skin, outline, 2)
        poly(((73, 89), (80, 63), (98, 43), (128, 35), (159, 43), (179, 67), (184, 106), (172, 143), (155, 160), (127, 169), (99, 157), (81, 137)), skin, outline, 4)
        poly(((128, 71), (165, 63), (177, 96), (166, 137), (144, 157), (130, 153)), (*skin_shadow, 125), None)
        for ex in (103, 151):
            oval((ex - 17, 99, ex + 18, 137), (253, 255, 255), outline, 2)
            oval((ex - 10, 105, ex + 11, 134), iris, (*outline, 235), 1)
            oval((ex - 6, 112, ex + 7, 132), (21, 30, 59))
            oval((ex - 5, 108, ex + 2, 115), (255, 255, 255))
            oval((ex + 4, 121, ex + 8, 126), (190, 238, 255))
        line(((85, 101), (101, 94), (117, 98)), hair, 4)
        line(((138, 97), (154, 93), (169, 100)), hair, 4)
        oval((125, 143, 130, 147), skin_shadow)
        if key in ("rikka", "mei"):
            d.arc(pt((113, 144, 139, 160)), 12, 165, fill=(152, 61, 83), width=2 * scale)
        else:
            line(((120, 152), (128, 155), (136, 152)), (147, 69, 80), 2)

        # Front fringe, with hairstyle and highlight colors unique to each pilot.
        if key == "rei":
            poly(((76, 92), (78, 59), (99, 36), (128, 31), (158, 40), (178, 64), (174, 91), (158, 76), (145, 98), (136, 73), (118, 101), (108, 75), (92, 104)), hair, outline, 3)
            line(((91, 69), (111, 46), (124, 42)), (*hair_light, 235), 3)
            line(((136, 54), (157, 64)), (*hair_light, 205), 2)
        elif key == "rikka":
            poly(((74, 94), (78, 59), (99, 37), (128, 31), (159, 39), (178, 64), (175, 92), (160, 80), (151, 102), (134, 79), (121, 101), (105, 79), (91, 101)), hair, outline, 3)
            line(((90, 64), (107, 46), (122, 42)), (*hair_light, 235), 3)
            poly(((79, 48), (66, 26), (87, 31), (101, 43)), (*accent, 255), outline, 2)
        elif key == "rin":
            poly(((72, 96), (76, 62), (99, 38), (128, 32), (158, 39), (179, 67), (178, 93), (159, 81), (148, 101), (135, 77), (119, 101), (105, 77), (90, 101)), hair, outline, 3)
            line(((86, 63), (107, 44), (124, 42)), (255, 255, 255, 245), 3)
            line(((140, 46), (161, 63)), (*accent, 230), 2)
        elif key == "mei":
            poly(((75, 96), (75, 65), (99, 42), (128, 36), (157, 42), (179, 66), (178, 96), (160, 80), (149, 102), (136, 78), (120, 102), (104, 79), (90, 103)), hair, outline, 3)
            line(((90, 66), (108, 48), (124, 45)), (*hair_light, 245), 3)
            for x in (87, 170):
                oval((x, 52, x + 7, 59), (238, 250, 255))
        else:
            poly(((74, 94), (77, 61), (100, 37), (128, 31), (157, 39), (179, 66), (178, 94), (161, 78), (149, 99), (136, 76), (121, 101), (105, 78), (90, 103)), hair, outline, 3)
            line(((90, 65), (108, 46), (125, 43)), (*hair_light, 240), 3)
            line(((142, 52), (156, 68)), (*secondary, 250), 4)

        # A small signature chest mark differentiates silhouettes at a glance.
        if key == "rei":
            poly(((105, 204), (112, 219), (129, 221), (116, 232), (120, 247), (105, 238), (91, 247), (95, 231), (82, 221), (99, 219)), (*secondary, 255), (255, 245, 194), 2)
        elif key == "rikka":
            for x in (106, 120):
                oval((x, 210, x + 9, 224), (*accent, 255), (255, 229, 180), 1)
        elif key == "rin":
            line(((104, 214), (126, 192), (148, 214), (126, 235), (104, 214)), (220, 250, 255, 235), 2)
        elif key == "mei":
            d.arc(pt((101, 199, 153, 243)), 205, 335, fill=(*accent, 245), width=4 * scale)
            oval((120, 215, 133, 228), (255, 229, 242, 245))
        else:
            line(((105, 214), (119, 221), (111, 232), (127, 239), (143, 224)), (*accent, 245), 4)

        final = image.resize((512, 640), Image.Resampling.LANCZOS)
        final.save(ART / "Characters" / "Sprites" / f"pilot_sprite_{key}.png", optimize=True)

    for key in PILOT_PALETTES:
        draw_pilot(key)


ICON_COLORS = {
    "pulse": "#28E6E8", "scatter": "#FFB84D", "orbit": "#C27BFF", "arc": "#73B8FF", "cryo": "#96F2FF",
    "damage": "#FF687A", "haste": "#49E0FF", "speed": "#6DFFBA", "health": "#FF6FA8", "magnet": "#75E8FF",
    "crit": "#FFD166", "crit_damage": "#FF5A9E", "pierce": "#B892FF", "regen": "#69F7C4", "armor": "#8EA8D8", "medical": "#FF7DB5", "repair": "#56F4D5",
    "boss_overload": "#FFC14D", "boss_phoenix": "#FF6D9B", "boss_resonance": "#B58CFF",
}


def point(cx, cy, radius, angle):
    return (cx + math.cos(angle) * radius, cy + math.sin(angle) * radius)


def make_icon_base(color):
    size = 512
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((95, 95, 417, 417), fill=(*rgba(color)[:3], 100))
    glow = glow.filter(ImageFilter.GaussianBlur(34))
    layer = Image.alpha_composite(layer, glow)
    draw = ImageDraw.Draw(layer)
    cx = cy = 256
    outer = [point(cx, cy, 194, -math.pi / 2 + i * math.pi / 3) for i in range(6)]
    inner = [point(cx, cy, 174, -math.pi / 2 + i * math.pi / 3) for i in range(6)]
    draw.polygon(outer, fill=(8, 20, 42, 245), outline=(*rgba(color)[:3], 255))
    draw.line(outer + [outer[0]], fill=(*rgba(color)[:3], 180), width=10, joint="curve")
    draw.polygon(inner, fill=(9, 27, 50, 255))
    draw.arc((112, 112, 400, 400), 210, 325, fill=(*rgba(color)[:3], 175), width=8)
    draw.arc((132, 132, 380, 380), 25, 145, fill=(255, 255, 255, 100), width=5)
    return layer, draw


def draw_icon(name, color):
    image, draw = make_icon_base(color)
    c = rgba(color)[:3]
    white = (235, 255, 255, 235)
    cx = cy = 256
    if name == "pulse":
        draw.polygon(((132, 274), (224, 248), (255, 170), (282, 245), (380, 222), (291, 287), (256, 350), (230, 286)), fill=(*c, 240), outline=white)
        draw.ellipse((230, 230, 282, 282), fill=white)
    elif name == "scatter":
        for dx, dy, length in ((-28, -36, 128), (0, -8, 165), (28, 18, 128), (0, 46, 103), (-25, 62, 74)):
            draw.line((cx - 32, cy + 5, cx + length - 32 + dx, cy + dy), fill=(*c, 245), width=18)
            ex, ey = cx + length - 38 + dx, cy + dy
            draw.ellipse((ex - 12, ey - 12, ex + 12, ey + 12), fill=white)
        draw.rectangle((172, 236, 224, 280), fill=white)
    elif name == "orbit":
        draw.ellipse((151, 151, 361, 361), outline=(*c, 245), width=18)
        for angle in (-.3, 1.8, 3.9):
            px, py = point(cx, cy, 142, angle)
            draw.polygon((point(px, py, 45, angle), point(px, py, 20, angle + 2.2), point(px, py, 20, angle - 2.2)), fill=white, outline=(*c, 255))
        draw.ellipse((222, 222, 290, 290), fill=(*c, 220), outline=white, width=6)
    elif name == "arc":
        draw.line((143, 322, 202, 230, 245, 288, 302, 177, 370, 252), fill=white, width=19, joint="curve")
        draw.line((143, 322, 202, 230, 245, 288, 302, 177, 370, 252), fill=(*c, 250), width=10, joint="curve")
        draw.ellipse((177, 205, 218, 246), fill=(*c, 255), outline=white, width=5)
        draw.ellipse((283, 156, 324, 197), fill=(*c, 255), outline=white, width=5)
    elif name == "cryo":
        crystal = ((256, 126), (309, 214), (365, 256), (309, 300), (256, 387), (203, 300), (147, 256), (203, 214))
        draw.polygon(crystal, fill=(*c, 245), outline=white)
        draw.line((256, 126, 256, 387), fill=white, width=7)
        draw.line((147, 256, 365, 256), fill=(225, 255, 255, 200), width=6)
        draw.polygon(((256, 151), (290, 240), (256, 256)), fill=(255, 255, 255, 160))
    elif name == "damage":
        draw.polygon(((256, 125), (311, 214), (290, 214), (343, 214), (280, 264), (297, 264), (222, 385), (237, 280), (213, 280)), fill=(*c, 250), outline=white)
        draw.ellipse((227, 227, 285, 285), outline=white, width=7)
    elif name == "haste":
        for i, length in enumerate((150, 190, 120)):
            y = 188 + i * 60
            draw.line((145, y, 145 + length, y - 28), fill=white if i == 1 else (*c, 245), width=17)
        draw.polygon(((308, 168), (379, 256), (308, 344), (319, 274), (270, 256), (319, 238)), fill=(*c, 250), outline=white)
    elif name == "speed":
        draw.polygon(((131, 256), (284, 143), (270, 220), (382, 220), (232, 369), (247, 282), (131, 282)), fill=(*c, 245), outline=white)
        draw.line((165, 322, 218, 322), fill=white, width=11)
    elif name == "health":
        draw.polygon(((256, 364), (148, 252), (142, 214), (169, 171), (215, 160), (256, 197), (297, 160), (343, 171), (370, 214), (364, 252)), fill=(*c, 245), outline=white)
        draw.line((256, 208, 256, 318), fill=white, width=14)
        draw.line((205, 263, 307, 263), fill=white, width=14)
    elif name == "magnet":
        draw.arc((151, 148, 361, 370), 15, 165, fill=white, width=38)
        draw.line((151, 196, 151, 277), fill=(*c, 250), width=36)
        draw.line((361, 196, 361, 277), fill=(*c, 250), width=36)
        draw.rectangle((132, 176, 169, 224), fill=white)
        draw.rectangle((343, 176, 380, 224), fill=white)
        draw.ellipse((214, 232, 298, 316), outline=(*c, 230), width=11)
    elif name == "crit":
        draw.ellipse((160, 160, 352, 352), outline=(*c, 245), width=13)
        draw.ellipse((222, 222, 290, 290), outline=white, width=9)
        for a in (0, math.pi / 2, math.pi, math.pi * 1.5):
            p1 = point(cx, cy, 95, a); p2 = point(cx, cy, 174, a)
            draw.line((p1, p2), fill=white, width=10)
        draw.line((238, 265, 258, 286, 306, 225), fill=white, width=11, joint="curve")
    elif name == "crit_damage":
        draw.polygon(((256, 130), (295, 213), (377, 187), (323, 255), (376, 321), (290, 302), (256, 384), (222, 302), (136, 321), (189, 255), (135, 187), (217, 213)), fill=(*c, 245), outline=white)
        draw.ellipse((220, 219, 292, 291), fill=(11, 26, 48, 255), outline=white, width=8)
        draw.line((242, 257, 254, 270, 276, 238), fill=(*c, 255), width=12, joint="curve")
    elif name == "pierce":
        draw.line((135, 335, 374, 165), fill=white, width=19)
        draw.polygon(((374, 165), (320, 180), (350, 220)), fill=(*c, 250), outline=white)
        for r in (62, 104, 146):
            draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(*c, 170), width=7)
    elif name == "regen":
        draw.ellipse((178, 188, 329, 358), outline=(*c, 240), width=12)
        draw.line((253, 196, 253, 333), fill=white, width=15)
        draw.line((196, 265, 314, 265), fill=white, width=15)
        draw.arc((137, 143, 238, 268), 215, 35, fill=white, width=12)
    elif name == "armor":
        shield = ((256, 137), (356, 181), (342, 304), (256, 375), (170, 304), (156, 181))
        draw.polygon(shield, fill=(*c, 245), outline=white)
        draw.line((256, 168, 256, 336), fill=white, width=8)
        draw.line((184, 214, 328, 214), fill=(255, 255, 255, 180), width=7)
    elif name == "medical":
        draw.rounded_rectangle((158, 186, 354, 326), radius=25, fill=(*c, 235), outline=white, width=8)
        draw.line((256, 218, 256, 293), fill=white, width=15)
        draw.line((218, 256, 294, 256), fill=white, width=15)
        draw.line((175, 205, 337, 307), fill=(255, 255, 255, 90), width=5)
    elif name == "repair":
        draw.ellipse((167, 167, 345, 345), fill=(*c, 210), outline=white, width=9)
        draw.polygon(((219, 173), (292, 173), (279, 225), (327, 225), (256, 348), (267, 277), (216, 277)), fill=white, outline=(*c, 255))
    elif name == "boss_overload":
        draw.polygon(((256, 120), (296, 214), (391, 256), (296, 298), (256, 392), (216, 298), (121, 256), (216, 214)), fill=(*c, 245), outline=white)
        draw.ellipse((218, 218, 294, 294), fill=(8, 20, 42, 255), outline=white, width=8)
        draw.polygon(((256, 227), (277, 253), (259, 253), (274, 282), (237, 251), (254, 251)), fill=white)
    elif name == "boss_phoenix":
        draw.polygon(((256, 134), (286, 204), (363, 171), (322, 241), (390, 261), (315, 282), (337, 365), (256, 314), (175, 365), (197, 282), (122, 261), (190, 241), (149, 171), (226, 204)), fill=(*c, 240), outline=white)
        draw.ellipse((220, 217, 292, 289), fill=(8, 20, 42, 255), outline=white, width=7)
        draw.ellipse((243, 240, 269, 266), fill=white)
    elif name == "boss_resonance":
        for radius, alpha, width in ((130, 240, 13), (86, 205, 9), (42, 255, 7)):
            draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), outline=(*c, alpha), width=width)
        for angle in (-math.pi / 2, math.pi / 6, 5 * math.pi / 6):
            px, py = point(cx, cy, 145, angle)
            draw.ellipse((px - 20, py - 20, px + 20, py + 20), fill=white, outline=(*c, 255), width=5)
        draw.ellipse((230, 230, 282, 282), fill=(*c, 255), outline=white, width=7)
    return image.resize((128, 128), Image.Resampling.LANCZOS)


def make_icons():
    mapping = {
        "weapon_pulse": "pulse", "weapon_scatter": "scatter", "weapon_orbit": "orbit", "weapon_arc": "arc", "weapon_cryo": "cryo",
        "stat_damage": "damage", "stat_haste": "haste", "stat_speed": "speed", "stat_health": "health", "stat_magnet": "magnet",
        "stat_crit": "crit", "stat_crit_damage": "crit_damage", "stat_pierce": "pierce", "stat_regen": "regen", "stat_armor": "armor", "emergency_repair": "repair",
        "boss_overload": "boss_overload", "boss_phoenix": "boss_phoenix", "boss_resonance": "boss_resonance",
    }
    icon_dir = ART / "UI" / "Icons"
    icon_dir.mkdir(parents=True, exist_ok=True)
    for filename, shape in mapping.items():
        draw_icon(shape, ICON_COLORS[shape]).save(icon_dir / f"{filename}.png", optimize=True)


if __name__ == "__main__":
    ART.mkdir(parents=True, exist_ok=True)
    make_icons()
    print("Created", len(list((ART / "UI" / "Icons").glob("*.png"))), "skill icons. Rebuild 3D pilot and enemy models with Assets/Blender/create_gameplay_models.py.")
