import bpy
import math
from pathlib import Path
from mathutils import Vector


def material(name, color, metallic=0.0, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get('Principled BSDF')
    principled.inputs['Base Color'].default_value = (*color, 1)
    principled.inputs['Metallic'].default_value = metallic
    principled.inputs['Roughness'].default_value = 0.28
    if emission:
        principled.inputs['Emission Color'].default_value = (*color, 1)
        principled.inputs['Emission Strength'].default_value = emission
    return mat


def sphere(name, location, scale, mat, segments=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=8, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = False
    return obj


def ico(name, location, scale, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    return obj


def beveled_cube(name, location, scale, mat, bevel=0.12):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    modifier = obj.modifiers.new('Readable silhouette bevel', 'BEVEL')
    modifier.width = bevel
    modifier.segments = 1
    obj.modifiers.new('Weighted corners', 'WEIGHTED_NORMAL')
    return obj


def cone_between(name, start, end, radius, mat, vertices=8):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=0.0, depth=direction.length, location=(start + end) * 0.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    obj.data.materials.append(mat)
    return obj


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def create_hero(m):
    # The avatar faces -Y, with a cyan visor clearly visible from the render camera.
    sphere('Hero suit', (0, 0, .58), (.39, .34, .53), m['suit'])
    sphere('Helmet', (0, .08, 1.04), (.36, .34, .32), m['armor'])
    beveled_cube('Visor', (0, -.235, 1.07), (.49, .08, .16), m['cyan'], .07)
    beveled_cube('Chest core', (0, -.30, .59), (.19, .07, .22), m['gold'], .04)
    beveled_cube('Backpack', (0, .35, .67), (.46, .28, .5), m['armor'], .08)
    for side in (-1, 1):
        sphere('Shoulder', (side * .43, -.01, .68), (.18, .2, .2), m['gold'])
        sphere('Glove', (side * .54, -.1, .3), (.16, .15, .13), m['suit'])
        cone_between('Boot', (side * .21, -.38, .22), (side * .25, -.5, .13), .12, m['armor'])
    cone_between('Energy blade', (.61, -.35, .22), (.75, -.68, .96), .09, m['cyan'])


def create_grunt(m):
    ico('Spore body', (0, 0, .43), (.48, .42, .43), m['red'], 2)
    sphere('Spore core', (0, -.36, .56), (.18, .09, .16), m['cyan'])
    for index, angle in enumerate((20, 110, 200, 290)):
        a = math.radians(angle)
        x, y = math.cos(a) * .3, math.sin(a) * .29
        cone_between('Leg', (x, y, .35), (x * 1.55, y * 1.55, .1), .11, m['dark'])
    for angle in (45, 135, 225, 315):
        a = math.radians(angle)
        ico('Spore', (math.cos(a) * .38, math.sin(a) * .35, .68), (.13, .13, .14), m['orange'], 1)


def create_runner(m):
    ico('Runner body', (0, 0, .38), (.31, .58, .33), m['orange'], 2)
    sphere('Runner core', (0, -.39, .49), (.18, .1, .16), m['cyan'])
    for side in (-1, 1):
        for y in (-.34, .05, .38):
            cone_between('Runner leg', (side * .2, y, .33), (side * .52, y - .17, .08), .07, m['dark'])
    cone_between('Tail', (0, .47, .35), (0, .82, .55), .12, m['orange'])


def create_brute(m):
    beveled_cube('Brute torso', (0, 0, .62), (1.12, .9, .88), m['violet'], .18)
    beveled_cube('Brute chest', (0, -.48, .63), (.35, .08, .3), m['pink'], .07)
    for side in (-1, 1):
        beveled_cube('Shoulder', (side * .68, 0, .83), (.5, .54, .54), m['dark'], .13)
        sphere('Fist', (side * .71, -.28, .29), (.29, .28, .25), m['violet'])
        cone_between('Crystal horn', (side * .61, .12, 1.03), (side * .8, .1, 1.52), .22, m['pink'], 6)
        cone_between('Boot', (side * .33, -.14, .23), (side * .36, -.36, .1), .17, m['dark'])


def create_boss(m):
    ico('Warden body', (0, 0, .91), (.93, .85, 1.04), m['dark'], 2)
    sphere('Warden crown core', (0, -.6, 1.02), (.53, .17, .42), m['pink'])
    sphere('Core highlight', (0, -.76, 1.12), (.22, .08, .2), m['cyan'])
    for angle in range(0, 360, 45):
        a = math.radians(angle)
        start = (math.cos(a) * .68, math.sin(a) * .64, 1.05)
        end = (math.cos(a) * 1.28, math.sin(a) * 1.16, 1.75 if angle % 90 else .55)
        cone_between('Warden crown spike', start, end, .2, m['violet'], 7)
    for side in (-1, 1):
        ico('Warden shoulder', (side * .84, -.02, .72), (.4, .36, .42), m['violet'], 1)
        cone_between('Warden arm spike', (side * .83, -.15, .52), (side * 1.18, -.42, .93), .16, m['pink'])


def main():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    mats = {
        'suit': material('Hero | deep teal suit', (.025, .23, .24), .25),
        'armor': material('Hero | graphite armor', (.075, .11, .16), .45),
        'cyan': material('Energy | cyan', (.02, .83, 1.0), .15, 2.8),
        'gold': material('Energy | warm gold', (1.0, .58, .12), .25, .7),
        'red': material('Spore | coral shell', (.82, .11, .19), .12),
        'orange': material('Runner | amber hide', (1.0, .36, .08), .08),
        'dark': material('Monster | dark carapace', (.075, .055, .14), .25),
        'violet': material('Monster | violet plates', (.38, .16, .67), .2, .18),
        'pink': material('Core | hot magenta', (1.0, .055, .39), .1, 2.0),
    }

    collections = {}
    builders = {'hero': create_hero, 'grunt': create_grunt, 'runner': create_runner, 'brute': create_brute, 'boss': create_boss}
    for name, builder in builders.items():
        collection = bpy.data.collections.new(name.title())
        bpy.context.scene.collection.children.link(collection)
        before = set(bpy.context.scene.objects)
        builder(mats)
        created = set(bpy.context.scene.objects) - before
        for obj in created:
            for owner in list(obj.users_collection):
                owner.objects.unlink(obj)
            collection.objects.link(obj)
        collections[name] = collection

    bpy.ops.object.camera_add(location=(4.2, -7.5, 9.5))
    camera = bpy.context.object
    camera.name = 'Sprite orthographic camera'
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 3.65
    bpy.ops.object.light_add(type='AREA', location=(-3, -4, 8))
    key = bpy.context.object
    key.data.energy = 750
    key.data.shape = 'DISK'
    key.data.size = 5
    bpy.ops.object.light_add(type='AREA', location=(4, 2, 5))
    rim = bpy.context.object
    rim.data.energy = 450
    rim.data.color = (.25, .75, 1.0)
    rim.data.size = 4

    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = True
    scene.render.image_settings.compression = 18
    scene.render.filepath = '//sprite.png'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'Medium High Contrast'

    project = Path(__file__).resolve().parents[1]
    output = project / 'Resources' / 'Art'
    output.mkdir(parents=True, exist_ok=True)
    for name, collection in collections.items():
        for item in collections.values():
            item.hide_render = item != collection
        camera.location = (4.2, -7.5, 9.5)
        look_at(camera, (0, 0, .65 if name != 'boss' else .9))
        scene.render.filepath = str(output / f'starfall_{name}.png')
        bpy.ops.render.render(write_still=True)
    for item in collections.values():
        item.hide_render = False
    source = project / 'ArtSource' / 'Blender'
    source.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source / 'starfall_characters.blend'))


main()
