"""Build stage-specific arena plates and the first raid guardian in Blender.

The arena renders are deliberately low-contrast in the center so combat sprites,
projectiles and ground telegraphs remain readable. Each scene is saved as an
editable .blend next to the renders.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Resources" / "Art"
SOURCE = ROOT / "Assets" / "ArtSource" / "Blender"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, color, metallic=0.0, roughness=.65, emission=0.0):
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


def cube(name, location, dimensions, mat, bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Soft chipped edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("Weighted highlights", "WEIGHTED_NORMAL")
    return obj


def ico(name, location, scale, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return obj


def rod(name, start, end, radius, mat, vertices=8, taper=.72):
    a, b = Vector(start), Vector(end)
    delta = b - a
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius * taper,
                                   depth=delta.length, location=(a + b) * .5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = delta.to_track_quat("Z", "Y")
    obj.data.materials.append(mat)
    return obj


def torus(name, location, major, minor, mat, rotation=(0, 0, 0), segments=112):
    bpy.ops.mesh.primitive_torus_add(major_segments=segments, minor_segments=8,
                                     major_radius=major, minor_radius=minor,
                                     location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return obj


def setup_camera(location, target, ortho):
    bpy.ops.object.camera_add(location=location)
    camera = bpy.context.object
    camera.name = "Raid art camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    return camera


def setup_lights():
    bpy.ops.object.light_add(type="AREA", location=(-5, -5, 10))
    key = bpy.context.object
    key.name = "Cool softbox"
    key.data.energy = 1850
    key.data.size = 8
    bpy.ops.object.light_add(type="AREA", location=(5, 3, 8))
    rim = bpy.context.object
    rim.name = "Colored rim light"
    rim.data.energy = 1050
    rim.data.size = 6
    rim.data.color = (.35, .65, 1)


def build_arena(stage, title, base, tile_a, tile_b, trim, glow, debris, seed):
    clear_scene()
    random.seed(seed)
    scene = bpy.context.scene
    base_mat = material(title + " | foundation", base, .18, .86)
    tile_a_mat = material(title + " | ceramic slate", tile_a, .24, .74)
    tile_b_mat = material(title + " | dark slate", tile_b, .3, .72)
    trim_mat = material(title + " | rune metal", trim, .58, .36, .22)
    glow_mat = material(title + " | powered glyphs", glow, .25, .28, 1.35)
    debris_mat = material(title + " | shattered crystal", debris, .12, .34, .08)

    cube("Arena foundation", (0, 0, -.33), (28.3, 16.55, .55), base_mat, .16)
    for ix in range(16):
        for iy in range(9):
            x = -13.125 + ix * 1.75
            y = -7.0 + iy * 1.75
            edge = min(ix, 15 - ix, iy, 8 - iy)
            mat = tile_a_mat if (ix + iy) % 3 else tile_b_mat
            height = random.uniform(.105, .16)
            obj = cube("Paved floor tile", (x, y, -.015 + random.uniform(-.012, .012)),
                       (1.71, 1.71, height), mat, .045)
            obj.rotation_euler.z = random.uniform(-.006, .006)
            if edge == 0 and (ix + iy) % 2 == 0:
                cube("Perimeter alloy rail", (x, y, .105), (1.5, .045, .025), trim_mat, .012)

    # Large, quiet rings create a clear arena focal point without obscuring action.
    for radius, width, mat in ((2.75, .034, trim_mat), (2.95, .055, glow_mat),
                               (5.8, .022, glow_mat), (10.9, .018, trim_mat)):
        torus("Concentric arena glyph", (0, 0, .115), radius, width, mat)
    for i in range(40):
        angle = i * math.tau / 40
        radius = 3.35 if i % 4 else 3.5
        loc = (math.cos(angle) * radius, math.sin(angle) * radius, .13)
        tick = cube("Segmented navigation rune", loc,
                    (.045 if i % 4 else .075, .19 if i % 4 else .32, .024),
                    glow_mat if i % 4 else trim_mat, .009)
        tick.rotation_euler.z = angle

    # Stage motifs live around the perimeter; keep the central 9-metre radius open.
    if stage == 1:
        for x, y, sign in ((-12.0, -6.6, -1), (12.0, 6.6, 1), (-11.8, 6.4, 1), (11.8, -6.5, -1)):
            for j in range(4):
                shard = ico("Cyan ruin crystal", (x + sign * j * .17, y + j * .16, .36 + j * .07),
                            (.16, .13, .48 + j * .05), glow_mat, 1)
                shard.rotation_euler = (random.uniform(-.2, .2), random.uniform(-.22, .22), j * .35)
    elif stage == 2:
        # Broken phase-rift lines and paired amber anchors on the outer lanes.
        for side in (-1, 1):
            for j in range(5):
                x = side * (10.8 + (j % 2) * .72)
                y = -5.6 + j * 2.75
                slash = cube("Rift fissure plate", (x, y, .13), (.11, 1.55, .045), glow_mat, .02)
                slash.rotation_euler.z = side * (.32 + .08 * (j % 3))
                if j % 2 == 0:
                    torus("Amber phase anchor", (side * 12.3, y + .4, .22), .38, .06, trim_mat,
                          (0, 0, 0), 48)
    else:
        # Pale gold locks and violet shard clusters foreshadow the anchored final boss.
        for i in range(12):
            angle = i * math.tau / 12
            radius = 6.6 if i % 2 == 0 else 7.15
            x, y = math.cos(angle) * radius, math.sin(angle) * radius * .74
            lock = cube("Star-eater seal marker", (x, y, .17), (.20, .70, .055), trim_mat, .025)
            lock.rotation_euler.z = angle + math.pi / 2
            for j in range(3):
                shard = ico("Violet void shard", (x * 1.43 + random.uniform(-.3, .3),
                           y * 1.43 + random.uniform(-.3, .3), .31),
                           (.19, .17, .26 + .08 * j), glow_mat, 1)
                shard.rotation_euler.z = random.uniform(0, math.tau)

    # Sparse debris stays near the walls so the play area remains legible.
    for _ in range(74):
        side = random.randrange(4)
        if side < 2:
            x = random.uniform(-13.3, 13.3)
            y = (-1 if side == 0 else 1) * random.uniform(6.7, 7.75)
        else:
            x = (-1 if side == 2 else 1) * random.uniform(11.4, 13.3)
            y = random.uniform(-7.5, 7.5)
        item = ico("Outer-lane debris", (x, y, .12),
                   (random.uniform(.06, .21), random.uniform(.06, .19), random.uniform(.04, .13)),
                   debris_mat, 1)
        item.rotation_euler.z = random.uniform(0, math.tau)

    setup_camera((0, 0, 30), (0, 0, 0), 28.4)
    setup_lights()
    scene.world.color = (.035, .045, .075)
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1792
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.compression = 20
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.filepath = str(ART / f"arena-floor-stage{stage}.png")
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"starfall_arena_stage{stage}.blend"))
    print("Rendered and saved", title, flush=True)


def build_warden():
    clear_scene()
    scene = bpy.context.scene
    dark = material("Warden | midnight alloy", (.025, .044, .095), .62, .28)
    armor = material("Warden | blue-black ceramic", (.075, .13, .22), .46, .33)
    pearl = material("Warden | pale steel", (.54, .68, .76), .75, .24)
    gold = material("Warden | antique gold", (.92, .48, .14), .67, .23, .22)
    cyan = material("Warden | containment light", (.12, .83, 1.0), .18, .16, 3.0)
    rose = material("Warden | warning light", (.95, .12, .40), .14, .18, 1.5)

    # Broad, readable knight silhouette with a faceplate and a contained star core.
    ico("Warden | armored torso", (0, .02, 1.33), (.78, .55, .92), armor, 2)
    cube("Warden | chest plate", (0, -.48, 1.35), (.92, .18, .86), dark, .13)
    ico("Warden | chest reactor bezel", (0, -.61, 1.39), (.37, .13, .39), gold, 2)
    ico("Warden | exposed star core", (0, -.72, 1.40), (.25, .10, .27), cyan, 2)
    torus("Warden | core restraint", (0, -.76, 1.40), .39, .045, pearl, (math.pi / 2, 0, 0), 64)

    ico("Warden | helm", (0, -.08, 2.23), (.49, .40, .43), dark, 2)
    ico("Warden | visor", (0, -.43, 2.24), (.36, .09, .20), pearl, 2)
    cube("Warden | dark visor glass", (0, -.515, 2.24), (.48, .055, .16), dark, .055)
    for side in (-1, 1):
        ico("Warden | cyan eye", (side * .15, -.56, 2.25), (.083, .035, .06), cyan, 2)
        rod("Warden | crown antenna", (side * .22, .0, 2.51), (side * .40, .06, 2.98), .105, gold, 6, .02)

        shoulder = ico("Warden | angular pauldron", (side * .88, -.015, 1.79), (.55, .53, .54), pearl, 1)
        shoulder.rotation_euler.y = side * -.12
        ico("Warden | pauldron inset", (side * .91, -.39, 1.80), (.34, .17, .34), gold, 1)
        rod("Warden | upper arm", (side * .86, .02, 1.55), (side * 1.13, -.08, 1.05), .23, dark, 8)
        ico("Warden | elbow", (side * 1.13, -.09, 1.04), (.25, .24, .23), gold, 1)
        rod("Warden | lower arm", (side * 1.13, -.08, 1.02), (side * 1.20, -.33, .57), .19, armor, 8)
        ico("Warden | fist", (side * 1.20, -.37, .51), (.25, .22, .24), pearl, 1)
        rod("Warden | shin", (side * .43, .06, .72), (side * .51, -.02, .22), .19, dark, 8)
        cube("Warden | armored foot", (side * .52, -.18, .14), (.48, .58, .24), armor, .08)
        cube("Warden | shin rune", (side * .47, -.22, .52), (.14, .035, .28), cyan, .018)
        rod("Warden | shoulder blade", (side * .95, .17, 2.03), (side * 1.26, .16, 2.60), .16, gold, 6, .02)

    torus("Warden | fragmented halo", (0, .05, 1.88), 1.52, .055, gold, (math.radians(68), 0, math.radians(16)))
    torus("Warden | inner ward ring", (0, .05, 1.89), 1.69, .026, cyan, (math.radians(68), 0, math.radians(-21)))
    for side in (-1, 1):
        for y in (-.22, .06):
            ico("Warden | floating ward node", (side * 1.48, y, 1.58), (.12, .12, .17),
                cyan if y < 0 else rose, 1)

    camera = setup_camera((4.1, -7.7, 5.8), (0, 0, 1.43), 4.55)
    setup_lights()
    scene.world.color = (.025, .03, .06)
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.compression = 18
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.filepath = str(ART / "starfall_boss_warden.png")
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "starfall_boss_warden.blend"))
    print("Rendered and saved Warden model", flush=True)


def main():
    ART.mkdir(parents=True, exist_ok=True)
    SOURCE.mkdir(parents=True, exist_ok=True)
    palettes = [
        ("ASHEN GATE", (.018, .035, .055), (.055, .09, .112), (.042, .068, .087),
         (.12, .40, .43), (.055, .55, .58), (.22, .075, .13), 1001),
        ("ECHO RIFT", (.025, .018, .065), (.068, .047, .135), (.046, .034, .105),
         (.42, .23, .70), (.42, .18, .82), (.22, .055, .19), 2002),
        ("STAR-EATER SANCTUM", (.012, .012, .032), (.040, .033, .078), (.028, .024, .059),
         (.38, .24, .58), (.48, .16, .92), (.18, .045, .14), 3003),
    ]
    for i, (name, base, tile_a, tile_b, trim, glow, debris, seed) in enumerate(palettes, 1):
        build_arena(i, name, base, tile_a, tile_b, trim, glow, debris, seed)
    build_warden()


if __name__ == "__main__":
    main()
