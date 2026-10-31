"""Build stylized, editable 3D pilot/enemy models and render transparent game sprites."""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Resources" / "Art"
SOURCE = ROOT / "Assets" / "ArtSource" / "Blender"


def material(name, color, metallic=0.0, roughness=.28, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*color, 1)
        shader.inputs["Emission Strength"].default_value = emission
    return mat


def ball(name, location, scale, mat, segments=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=16, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def ico(name, location, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return obj


def box(name, location, scale, mat, bevel=.08):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Soft armor chamfers", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new("Weighted highlights", "WEIGHTED_NORMAL")
    return obj


def rod(name, start, end, radius, mat, vertices=12, taper=.82):
    start, end = Vector(start), Vector(end)
    delta = end - start
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius * taper,
                                   depth=delta.length, location=(start + end) * .5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = delta.to_track_quat("Z", "Y")
    obj.data.materials.append(mat)
    return obj


def curve(name, points, mat, width=.035):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 16
    data.bevel_depth = width
    data.bevel_resolution = 3
    spline = data.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, xyz in zip(spline.bezier_points, points):
        point.co = xyz
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def torus(name, location, major, minor, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor,
                                     major_segments=48, minor_segments=12, location=location,
                                     rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def move_to_collection(obj, collection):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)


def make_pilot(key, palette):
    base, trim, glow, hair, hairlight = palette
    suit = material(key.title() + " | ceramic suit", base, .24)
    dark = material(key.title() + " | midnight undersuit", (.028, .045, .095), .34)
    armor = material(key.title() + " | pearl armor", (.66, .78, .87), .32)
    trim_mat = material(key.title() + " | signature trim", trim, .35, .24, .2)
    neon = material(key.title() + " | reactor light", glow, .18, .18, 2.2)
    skin = material(key.title() + " | porcelain skin", (.96, .70, .62), .02, .38)
    hair_mat = material(key.title() + " | sculpted hair", hair, .12, .3)
    hair_hi = material(key.title() + " | hair sheen", hairlight, .15, .25, .12)
    eye_white = material(key.title() + " | eye whites", (.92, .98, 1), .05, .2)
    iris = material(key.title() + " | irises", glow, .14, .18, .55)
    ink = material(key.title() + " | pupils", (.018, .028, .055), .06, .22)
    pearl = material(key.title() + " | bright metal", (.83, .91, 1), .75, .2)

    # Compact full-body anime proportions with recognizable faces and tailored suits.
    for side in (-1, 1):
        x = side * .17
        rod("Leg | articulated thigh", (x, 0, .69), (x * 1.05, -.015, .38), .105, dark)
        rod("Leg | armored shin", (x * 1.05, -.015, .39), (x * 1.12, -.04, .18), .095, suit)
        ball("Knee | luminous joint", (x * 1.05, -.035, .39), (.095, .095, .09), trim_mat, 16)
        boot = box("Boot | sculpted heel", (x * 1.12, -.095, .13), (.22, .34, .22), dark, .06)
        box("Boot | alloy toe", (x * 1.12, -.25, .12), (.22, .13, .12), armor, .04)
        box("Boot | light strip", (x * 1.12, -.18, .235), (.15, .025, .035), neon, .01)

    box("Waist | flight harness", (0, 0, .77), (.51, .32, .21), dark, .08)
    box("Belt | brushed alloy", (0, -.18, .79), (.48, .08, .08), pearl, .025)
    torso = ico("Torso | fitted pilot jacket", (0, 0, 1.05), (.39, .30, .43), suit, 2)
    # Offset shoulder armor and articulated gauntlets keep each silhouette readable.
    for side in (-1, 1):
        ball("Shoulder | pauldrons", (side * .36, -.005, 1.31), (.20, .25, .18), armor, 20)
        box("Shoulder | color mark", (side * .35, -.22, 1.31), (.16, .035, .07), trim_mat, .025)
        rod("Arm | upper sleeve", (side * .33, 0, 1.28), (side * .48, -.08, 1.02), .105, suit)
        ball("Elbow | flex seal", (side * .48, -.08, 1.02), (.11, .11, .11), dark, 16)
        rod("Arm | wrist gauntlet", (side * .48, -.08, 1.02), (side * .53, -.2, .85), .09, armor)
        ball("Glove | palm", (side * .54, -.22, .82), (.12, .10, .13), dark, 16)

    # Layered collar, reactor, and panel seams.
    ball("Neck | skin", (0, -.02, 1.42), (.15, .15, .16), skin, 18)
    box("Collar | left", (-.10, -.24, 1.40), (.18, .05, .12), armor, .04)
    box("Collar | right", (.10, -.24, 1.40), (.18, .05, .12), armor, .04)
    box("Chest plate | central", (0, -.29, 1.12), (.25, .07, .31), dark, .07)
    ball("Reactor | signature gem", (0, -.355, 1.19), (.105, .04, .105), neon, 24)
    torus("Reactor | silver bezel", (0, -.37, 1.19), .125, .016, pearl, (math.pi / 2, 0, 0))
    for side in (-1, 1):
        box("Chest | telemetry", (side * .24, -.27, 1.06), (.07, .035, .16), trim_mat, .025)
        ball("Chest | status diode", (side * .24, -.295, 1.30), (.035, .025, .035), neon, 12)

    # Face sits forward of the hair mass; large iris highlights remain clear at gameplay scale.
    ball("Face | anime sculpt", (0, -.11, 1.78), (.385, .32, .40), skin, 32)
    ball("Eye | left white", (-.145, -.405, 1.82), (.105, .045, .135), eye_white, 24)
    ball("Eye | right white", (.145, -.405, 1.82), (.105, .045, .135), eye_white, 24)
    for side in (-1, 1):
        ball("Eye | iris", (side * .145, -.448, 1.815), (.061, .027, .092), iris, 24)
        ball("Eye | pupil", (side * .145, -.472, 1.812), (.028, .016, .061), ink, 18)
        ball("Eye | catchlight", (side * .162, -.488, 1.852), (.018, .012, .025), eye_white, 12)
        rod("Brow | expressive", (side * .09, -.415, 2.005), (side * .20, -.407, 2.015), .018, hair_mat, 8, .6)
    ball("Nose | soft highlight", (0, -.432, 1.73), (.022, .018, .034), material(key + " | nose", (.82, .43, .4)), 12)
    curve("Mouth | confident smile", [(-.045, -.421, 1.665), (0, -.433, 1.651), (.045, -.421, 1.665)],
          material(key + " | lip", (.48, .13, .18)), .009)

    # Sculpted cap, swept fringe, and long locks echo each portrait rather than using a helmet.
    ball("Hair | crown", (0, -.005, 2.05), (.44, .39, .25), hair_mat, 28)
    if key == "mei":
        for side in (-1, 1):
            ball("Hair | tech bun", (side * .42, .03, 2.10), (.16, .17, .16), hair_mat, 20)
            torus("Hair | bun ring", (side * .42, -.03, 2.10), .115, .025, trim_mat, (math.pi / 2, 0, 0))
    if key in ("rikka", "yuna"):
        for side in (-1, 1):
            tail_x = side * (.46 if key == "rikka" else .39)
            end_z = .97 if key == "rikka" else 1.05
            curve("Hair | flowing tail", [(side * .34, .06, 2.08), (tail_x, .05, 1.83), (tail_x * 1.18, -.02, 1.43), (tail_x * .94, -.08, end_z)], hair_mat, .115 if key == "rikka" else .095)
            curve("Hair | tail glint", [(side * .35, -.035, 1.9), (tail_x * 1.08, -.13, 1.54), (tail_x, -.16, end_z + .12)], hair_hi, .022)
    elif key == "rin":
        for side in (-1, 1):
            curve("Hair | long frost lock", [(side * .32, .08, 1.98), (side * .43, .03, 1.58), (side * .46, -.015, 1.15), (side * .39, -.09, .65)], hair_mat, .105)
            curve("Hair | ice sheen", [(side * .36, -.06, 1.88), (side * .46, -.115, 1.37), (side * .40, -.17, .78)], hair_hi, .022)
    elif key == "rei":
        for side in (-1, 1):
            curve("Hair | tapered side lock", [(side * .32, -.10, 2.0), (side * .40, -.13, 1.78), (side * .33, -.18, 1.56)], hair_mat, .09)
    else:
        for side in (-1, 1):
            curve("Hair | bob lock", [(side * .32, -.08, 2.0), (side * .40, -.13, 1.80), (side * .35, -.18, 1.61)], hair_mat, .095)
    for x in (-.30, -.15, 0, .15, .30):
        tip_z = 1.90 + (0.05 if x == 0 else 0)
        rod("Hair | layered fringe", (x, -.27, 2.13), (x * .76, -.424, tip_z), .09 if x == 0 else .075, hair_mat, 12, .12)
        curve("Hair | fringe shine", [(x * .85, -.352, 2.06), (x * .74, -.435, 1.99)], hair_hi, .014)
    if key == "rikka":
        box("Hair | ember clasp", (-.32, -.20, 2.20), (.18, .06, .075), trim_mat, .025)
        for side in (-1, 1):
            ball("Hair | gold pin", (side * .34, -.18, 2.16), (.04, .025, .04), pearl, 12)
    elif key == "rin":
        rod("Hair | crystal pin", (.27, -.19, 2.10), (.38, -.18, 2.23), .045, neon, 6, .05)
    elif key == "mei":
        for side in (-1, 1):
            ball("Hair | shrine pin", (side * .33, -.19, 2.14), (.055, .03, .045), pearl, 12)
    elif key == "yuna":
        box("Hair | signal clip", (-.31, -.20, 2.16), (.13, .045, .06), neon, .02)

    # Personal flight pack, skirt/coattails, and a portrait-specific weapon.
    box("Pack | reactor housing", (0, .24, 1.19), (.42, .25, .42), dark, .09)
    for side in (-1, 1):
        box("Pack | thruster", (side * .22, .29, 1.13), (.10, .18, .30), armor, .035)
        ball("Pack | exhaust", (side * .22, .18, 1.0), (.045, .04, .055), neon, 12)
    for side in (-1, 1):
        panel = box("Coat | split tail", (side * .20, .12, .69), (.22, .11, .33), suit, .045)
        panel.rotation_euler[1] = side * -.10
        box("Coat | tail piping", (side * .20, .055, .70), (.035, .025, .28), trim_mat, .01)

    if key == "rei":
        box("Pulse carbine | receiver", (.69, -.22, 1.03), (.28, .16, .19), dark, .035)
        rod("Pulse carbine | barrel", (.76, -.25, 1.06), (1.00, -.25, 1.10), .055, pearl, 10)
        ball("Pulse carbine | emitter", (1.0, -.25, 1.10), (.07, .06, .07), neon, 16)
        ball("Chest | navigation star", (0, -.398, 1.08), (.07, .027, .07), trim_mat, 16)
    elif key == "rikka":
        box("Scatter blaster | body", (.69, -.23, 1.00), (.34, .20, .22), dark, .045)
        for dz in (-.06, .06):
            rod("Scatter blaster | twin barrel", (.80, -.31, 1.00 + dz), (1.08, -.31, 1.00 + dz), .045, trim_mat, 10)
        ball("Scatter blaster | muzzle glow", (1.08, -.31, 1.00), (.08, .05, .11), neon, 16)
        for x in (-.10, .10): ball("Chest | shell mark", (x, -.39, 1.07), (.042, .025, .055), trim_mat, 12)
    elif key == "rin":
        rod("Cryo lance | shaft", (.55, -.28, .62), (.78, -.28, 2.25), .045, dark, 12)
        rod("Cryo lance | luminous spine", (.57, -.33, .70), (.80, -.33, 2.19), .018, neon, 8)
        ico("Cryo lance | spear crystal", (.80, -.29, 2.28), (.12, .09, .26), armor, 1)
        box("Chest | researcher tag", (0, -.39, .99), (.16, .035, .12), pearl, .02)
    elif key == "mei":
        torus("Orbit | halo controller", (0, .02, 1.58), .57, .018, neon, (math.radians(18), 0, 0))
        for side in (-1, 1):
            ico("Orbit | floating blade", (side * .72, -.03, 1.48), (.09, .035, .23), pearl, 1)
            ball("Orbit | blade jewel", (side * .72, -.07, 1.48), (.045, .028, .05), neon, 12)
        ball("Chest | moon seal", (0, -.40, 1.12), (.12, .025, .12), neon, 16)
    else:
        rod("Arc staff | grip", (.61, -.26, .48), (.78, -.26, 1.97), .045, dark, 12)
        torus("Arc staff | coil", (.76, -.27, 1.77), .18, .028, trim_mat, (math.pi / 2, 0, 0))
        ball("Arc staff | capacitor", (.76, -.32, 1.77), (.08, .05, .08), neon, 16)
        curve("Arc staff | live arc", [(.73, -.34, 1.76), (.91, -.35, 1.89), (.82, -.36, 2.03)], neon, .018)
        for side in (-1, 1): ball("Chest | circuit rivet", (side * .11, -.397, 1.09), (.035, .025, .035), trim_mat, 12)


def make_phase_lancer():
    shell = material("Phase Lancer | deep teal shell", (.025, .28, .39), .38)
    bright = material("Phase Lancer | cyan plasma", (.05, .88, 1), .2, .18, 2.0)
    dark = material("Phase Lancer | graphite", (.035, .06, .12), .3)
    ico("Lancer | swept body", (0, 0, .58), (.40, .48, .48), shell, 2)
    ball("Lancer | eye core", (0, -.39, .67), (.18, .07, .16), bright, 20)
    for side in (-1, 1):
        rod("Lancer | forward horn", (side * .19, -.12, .82), (side * .31, -.57, 1.21), .14, bright, 7, .03)
        rod("Lancer | blade leg", (side * .22, .02, .38), (side * .46, -.23, .10), .10, dark, 9)


def make_echo_wisp():
    shell = material("Echo Wisp | amethyst glass", (.25, .22, .59), .36, .2)
    light = material("Echo Wisp | spectral blue", (.39, .72, 1), .18, .16, 2.1)
    ink = material("Echo Wisp | shadow", (.035, .045, .13), .3)
    ico("Wisp | levitating core", (0, 0, .61), (.34, .36, .48), shell, 2)
    ball("Wisp | mask", (0, -.31, .68), (.24, .09, .24), ink, 20)
    for side in (-1, 1):
        ball("Wisp | twin eye", (side * .09, -.40, .72), (.045, .025, .07), light, 16)
        curve("Wisp | spectral streamer", [(side * .18, 0, .38), (side * .44, .05, .18), (side * .52, .10, .03)], light, .045)
        rod("Wisp | crown shard", (side * .17, -.06, .90), (side * .26, -.10, 1.34), .09, light, 6, .01)
    torus("Wisp | orbit ring", (0, 0, .35), .45, .025, light, (math.radians(20), 0, 0))


def make_rift_brute():
    shell = material("Rift Brute | obsidian violet", (.18, .10, .34), .53)
    plate = material("Rift Brute | magenta crystal", (.75, .12, .50), .28, .2, .8)
    core = material("Rift Brute | rift core", (.28, .78, 1), .16, .16, 2.4)
    ico("Brute | massive torso", (0, 0, .69), (.62, .55, .66), shell, 2)
    ball("Brute | chest aperture", (0, -.49, .72), (.26, .10, .29), plate, 20)
    ball("Brute | heart", (0, -.59, .72), (.12, .05, .15), core, 18)
    for side in (-1, 1):
        ico("Brute | shoulder carapace", (side * .57, -.04, .91), (.42, .39, .42), plate, 1)
        rod("Brute | shoulder spike", (side * .55, 0, 1.15), (side * .82, -.11, 1.65), .17, shell, 7, .02)
        rod("Brute | arm", (side * .52, -.02, .64), (side * .74, -.24, .20), .22, shell, 10)
    torus("Brute | gravity shackle", (0, .02, .46), .72, .03, core, (math.radians(16), 0, 0))


def make_nova_reaver():
    shell = material("Nova Reaver | hot coral", (.68, .11, .29), .35)
    dark = material("Nova Reaver | black alloy", (.08, .045, .14), .46)
    light = material("Nova Reaver | nova edge", (1, .42, .24), .2, .18, 2)
    ico("Reaver | aerodynamic chassis", (0, 0, .55), (.38, .53, .38), shell, 2)
    ball("Reaver | visor", (0, -.41, .67), (.23, .075, .12), dark, 20)
    ball("Reaver | eye", (0, -.48, .68), (.13, .04, .05), light, 16)
    for side in (-1, 1):
        rod("Reaver | scythe arm", (side * .25, -.07, .70), (side * .55, -.20, .28), .075, dark, 10)
        rod("Reaver | scythe blade", (side * .55, -.20, .28), (side * .88, -.30, .77), .11, light, 7, .015)
        rod("Reaver | dorsal fin", (side * .12, .24, .68), (side * .32, .44, 1.25), .13, dark, 7, .02)


def make_siege_orb():
    shell = material("Siege Orb | bronze alloy", (.40, .24, .14), .75)
    armor = material("Siege Orb | gold plating", (.98, .55, .16), .55, .22)
    core = material("Siege Orb | furnace", (1, .23, .055), .15, .16, 2.3)
    ico("Siege Orb | armored sphere", (0, 0, .72), (.66, .63, .66), shell, 2)
    ball("Siege Orb | furnace eye", (0, -.58, .73), (.28, .12, .26), core, 24)
    torus("Siege Orb | equatorial rail", (0, 0, .72), .72, .04, armor, (math.radians(72), 0, 0))
    for i in range(8):
        a = math.pi * 2 * i / 8
        start = (math.cos(a) * .48, math.sin(a) * .47, .72)
        end = (math.cos(a) * .93, math.sin(a) * .89, .88 + (.17 if i % 2 else -.14))
        rod("Siege Orb | outer spike", start, end, .11, armor, 8, .015)
    for z in (.47, .96):
        box("Siege Orb | vent", (0, -.61, z), (.30, .04, .045), armor, .015)


def make_null_maw():
    shell = material("Null Maw | void hide", (.13, .075, .23), .42)
    purple = material("Null Maw | nebula crystal", (.45, .19, .82), .23, .2, .55)
    black = material("Null Maw | event horizon", (.008, .01, .028), .06, .16)
    light = material("Null Maw | singularity rim", (.56, .78, 1), .24, .16, 2.0)
    ico("Maw | swollen body", (0, 0, .76), (.72, .66, .76), shell, 2)
    ball("Maw | open event horizon", (0, -.57, .81), (.40, .12, .43), black, 28)
    torus("Maw | luminous jaw", (0, -.63, .81), .39, .045, light, (math.pi / 2, 0, 0))
    for side in (-1, 1):
        ball("Maw | orbit eye", (side * .38, -.42, 1.04), (.10, .06, .11), purple, 16)
        rod("Maw | crown barb", (side * .33, -.02, 1.23), (side * .52, .02, 1.72), .15, purple, 7, .01)
    for i in range(6):
        a = i * math.pi / 3
        rod("Maw | rift tendril", (math.cos(a) * .45, math.sin(a) * .4, .56),
            (math.cos(a) * .91, math.sin(a) * .76, .16), .085, purple, 9, .02)


def make_echo_herald():
    armor = material("Echo Herald | ivory shell", (.60, .68, .83), .66, .22)
    dark = material("Echo Herald | deep indigo", (.07, .055, .19), .46)
    gold = material("Echo Herald | antique gold", (1, .59, .18), .66, .2, .35)
    pulse = material("Echo Herald | echo energy", (.45, .30, 1), .18, .15, 2.8)
    body = ico("Herald | floating mantle", (0, 0, .92), (.86, .67, .98), dark, 2)
    ball("Herald | porcelain face", (0, -.57, 1.10), (.47, .16, .46), armor, 28)
    ball("Herald | eclipse visor", (0, -.70, 1.10), (.35, .09, .20), dark, 24)
    for side in (-1, 1):
        ball("Herald | violet eye", (side * .14, -.78, 1.10), (.06, .025, .08), pulse, 16)
        ico("Herald | orbiting shard", (side * 1.0, .04, .92), (.24, .20, .61), gold, 1)
        curve("Herald | ribbon", [(side * .54, .16, .74), (side * .95, .24, .40), (side * 1.30, .12, .72)], pulse, .045)
        rod("Herald | crown blade", (side * .34, -.04, 1.50), (side * .49, -.08, 2.17), .16, armor, 7, .02)
        ball("Herald | shoulder core", (side * .57, -.38, .85), (.22, .09, .21), pulse, 20)
    torus("Herald | resonance orbit A", (0, 0, .96), 1.17, .035, pulse, (math.radians(28), 0, 0))
    torus("Herald | resonance orbit B", (0, -.03, .98), 1.32, .024, gold, (math.radians(-32), math.radians(8), 0))
    ball("Herald | heartstone", (0, -.72, .63), (.23, .10, .26), pulse, 22)
    for side in (-1, 1):
        ico("Herald | lower mantle plate", (side * .36, -.20, .25), (.39, .34, .42), armor, 1)


def make_star_eater():
    shell = material("Star Eater | ancient basalt", (.09, .065, .22), .56)
    plate = material("Star Eater | violet crystal armor", (.30, .18, .58), .42, .21, .35)
    fissure = material("Star Eater | stellar fissure", (.53, .24, 1), .12, .16, 3.3)
    black = material("Star Eater | core void", (.009, .008, .025), .05, .15)
    # Wide, anchored silhouette: a colossal monolith with a broken halo and layered shoulders.
    ico("Star Eater | cathedral body", (0, .02, 1.16), (1.45, .79, 1.48), shell, 2)
    ico("Star Eater | mantle plate", (0, -.30, 1.60), (1.22, .47, .95), plate, 2)
    ball("Star Eater | event horizon", (0, -.72, 1.31), (.70, .18, .69), black, 28)
    torus("Star Eater | core halo", (0, -.81, 1.31), .74, .075, fissure, (math.pi / 2, 0, 0))
    ico("Star Eater | singularity", (0, -.92, 1.31), (.40, .075, .40), fissure, 2)
    ico("Star Eater | inner shadow", (0, -.995, 1.31), (.21, .045, .21), black, 2)
    for side in (-1, 1):
        ico("Star Eater | immense shoulder", (side * 1.22, -.12, 1.40), (.67, .63, .77), shell, 2)
        ico("Star Eater | shoulder crown", (side * 1.27, -.24, 1.76), (.52, .43, .45), plate, 1)
        rod("Star Eater | skyward horn", (side * 1.18, .02, 1.84), (side * 1.63, .06, 2.73), .28, plate, 8, .015)
        rod("Star Eater | arm pillar", (side * 1.23, -.10, 1.05), (side * 1.47, -.35, .20), .34, shell, 10, .55)
        ico("Star Eater | knuckle", (side * 1.48, -.38, .21), (.40, .32, .32), plate, 1)
        curve("Star Eater | glowing fracture", [(side * .36, -.71, 1.98), (side * .25, -.79, 1.55), (side * .46, -.85, 1.30)], fissure, .035)
    for i in range(7):
        x = (i - 3) * .27
        rod("Star Eater | crown needle", (x, .0, 2.25), (x * 1.22, .04, 2.94 + (.2 if i % 2 else 0)), .17, plate, 7, .015)
    # Grounded, broad feet and a stone dais make its immobility obvious in-game.
    for side in (-1, 1):
        box("Star Eater | anchored foot", (side * .62, -.05, .12), (.75, .75, .28), shell, .12)
        box("Star Eater | foot rune", (side * .62, -.45, .20), (.36, .035, .06), fissure, .02)
    torus("Star Eater | gravity seal", (0, 0, .02), 1.9, .055, fissure)


def build_collection(name, builder):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    before = set(bpy.context.scene.objects)
    builder()
    created = set(bpy.context.scene.objects) - before
    for obj in created:
        move_to_collection(obj, collection)
    return collection


def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    ART.mkdir(parents=True, exist_ok=True)
    SOURCE.mkdir(parents=True, exist_ok=True)

    palettes = {
        "rei": ((.055, .13, .26), (.98, .67, .20), (.08, .88, 1), (.08, .14, .35), (.21, .62, .98)),
        "rikka": ((.31, .075, .12), (1, .40, .13), (1, .69, .20), (.83, .15, .12), (1, .49, .22)),
        "rin": ((.70, .84, .94), (.20, .56, .88), (.31, .86, 1), (.64, .83, .98), (.91, .98, 1)),
        "mei": ((.25, .12, .42), (.88, .44, 1), (.83, .55, 1), (.39, .20, .61), (.75, .52, .98)),
        "yuna": ((.10, .13, .31), (.97, .28, .64), (.17, .82, 1), (.15, .09, .32), (.56, .30, .80)),
    }
    builders = {"pilot_rei": lambda: make_pilot("rei", palettes["rei"]),
                "pilot_rikka": lambda: make_pilot("rikka", palettes["rikka"]),
                "pilot_rin": lambda: make_pilot("rin", palettes["rin"]),
                "pilot_mei": lambda: make_pilot("mei", palettes["mei"]),
                "pilot_yuna": lambda: make_pilot("yuna", palettes["yuna"]),
                "phase_lancer": make_phase_lancer, "echo_wisp": make_echo_wisp,
                "rift_brute": make_rift_brute, "nova_reaver": make_nova_reaver,
                "siege_orb": make_siege_orb, "null_maw": make_null_maw,
                "boss_echo_herald": make_echo_herald, "boss_star_eater": make_star_eater}
    collections = {name: build_collection(name, builder) for name, builder in builders.items()}

    bpy.ops.object.camera_add(location=(3.7, -7.3, 8.0))
    camera = bpy.context.object
    camera.name = "Three-quarter game-sprite camera"
    camera.data.type = "ORTHO"
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.2, 7.5))
    key = bpy.context.object
    key.name = "Soft key | cool anime highlights"
    key.data.energy = 950
    key.data.shape = "DISK"
    key.data.size = 5
    bpy.ops.object.light_add(type="AREA", location=(4, 2.0, 5.8))
    rim = bpy.context.object
    rim.name = "Rim light | reactor edge"
    rim.data.energy = 680
    rim.data.color = (.28, .52, 1.0)
    rim.data.size = 3.5
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    scene.render.image_settings.compression = 18
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.camera.data.ortho_scale = 3.1

    for name, collection in collections.items():
        for other in collections.values():
            other.hide_render = other != collection
        is_boss = name.startswith("boss_")
        if name.startswith("pilot_"):
            aim(camera, (0, 0, 1.2))
            camera.data.ortho_scale = 3.1
            scene.render.resolution_x, scene.render.resolution_y = 512, 640
        elif is_boss:
            aim(camera, (0, 0, 1.35 if name == "boss_star_eater" else 1.05))
            camera.data.ortho_scale = 4.8 if name == "boss_star_eater" else 3.45
            scene.render.resolution_x, scene.render.resolution_y = 768, 768
        else:
            aim(camera, (0, 0, .72))
            camera.data.ortho_scale = 2.8
            scene.render.resolution_x, scene.render.resolution_y = 512, 512
        if name.startswith("pilot_"):
            asset = name.replace("pilot_", "pilot_sprite_")
        else:
            asset = "starfall_" + name
        scene.render.filepath = str(ART / (asset + ".png"))
        bpy.ops.render.render(write_still=True)
        print("Rendered", asset)

    for collection in collections.values():
        collection.hide_render = False
    # Arrange the editable source models into a readable lineup in the saved scene.
    pilot_names = ["pilot_rei", "pilot_rikka", "pilot_rin", "pilot_mei", "pilot_yuna"]
    for i, name in enumerate(pilot_names):
        for obj in collections[name].objects:
            obj.location.x += (i - 2) * 2.55
    enemy_names = ["phase_lancer", "echo_wisp", "rift_brute", "nova_reaver", "siege_orb", "null_maw"]
    for i, name in enumerate(enemy_names):
        for obj in collections[name].objects:
            obj.location.x += (i - 2.5) * 2.35
            obj.location.y += 4.0
    for i, name in enumerate(["boss_echo_herald", "boss_star_eater"]):
        for obj in collections[name].objects:
            obj.location.x += (i - .5) * 4.0
            obj.location.y -= 4.0
    camera.location = (0, -19, 15)
    camera.data.ortho_scale = 18.0
    aim(camera, (0, 0, 1.1))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "starfall_gameplay_models.blend"))
    print("Saved editable 3D source and all gameplay sprite renders.")


main()
