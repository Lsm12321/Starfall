import bpy, math, random
from pathlib import Path
from mathutils import Vector

random.seed(2026)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name,color,emission=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1);m.use_nodes=True
    s=m.node_tree.nodes.get('Principled BSDF')
    s.inputs['Base Color'].default_value=(*color,1);s.inputs['Roughness'].default_value=.86
    s.inputs['Emission Color'].default_value=(*color,1);s.inputs['Emission Strength'].default_value=emission
    return m
stone=[mat('Slate '+str(i),(.054+i*.006,.084+i*.007,.1+i*.008)) for i in range(5)]
dark=mat('Obsidian border',(.025,.043,.065))
cyan=mat('Ancient cyan runes',(.035,.38,.37),.5)
bright=mat('Crystal facets',(.035,.54,.57),1)
rose=mat('Crimson vegetation',(.27,.065,.14))

def block(name,p,scale,m,bevel=.1):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
    b=o.modifiers.new('Chipped edges','BEVEL');b.width=bevel;b.segments=1;o.modifiers.new('Normals','WEIGHTED_NORMAL')
    return o
block('Foundation',(0,0,-.3),(30,18,.5),dark)
for x in range(-7,7):
    for y in range(-4,4):
        o=block('Ancient paving',(x*2+1,y*2+1,random.uniform(-.025,.025)),(1.96,1.96,.18),random.choice(stone),random.uniform(.035,.1))
        if random.random()<.12:
            for i in range(random.randint(1,3)):
                angle=random.uniform(-1,1)
                r=block('Broken etched line',(x*2+random.uniform(.3,1.5),y*2+random.uniform(.3,1.5),.105),(.025,random.uniform(.2,.8),.015),cyan,.003);r.rotation_euler.z=angle
for radius in (2.8,3.0,6.1):
    bpy.ops.mesh.primitive_torus_add(major_segments=128,minor_segments=6,location=(0,0,.105),major_radius=radius,minor_radius=.014 if radius!=3 else .035)
    bpy.context.object.data.materials.append(cyan if radius!=3 else dark)
for index in range(24):
    a=index*math.tau/24
    r=block('Circular runes',(math.cos(a)*3.2,math.sin(a)*3.2,.11),(.04,.17,.008),cyan,.003);r.rotation_euler.z=a
for side in (-1,1):
    for index in range(8):
        x=-12.2+index*3.5
        block('Low ruin wall',(x,side*7.9,.18),(3.35,.32,.55),random.choice(stone))
    for index in range(5):
        y=-6.5+index*3.25
        block('Low ruin wall',(side*13.8,y,.18),(.32,3.1,.55),random.choice(stone))
for x,y in [(-12,-6),(12,-6),(-12,6),(12,6),(-7,7),(7,-7)]:
    block('Ruined plinth',(x,y,.18),(1.25,1.05,.5),dark,.12)
    for index in range(5):
        dx,dy=random.uniform(-.45,.45),random.uniform(-.36,.36)
        bpy.ops.mesh.primitive_cone_add(vertices=5,radius1=random.uniform(.15,.3),radius2=0,depth=random.uniform(.45,1.3),location=(x+dx,y+dy,.6))
        o=bpy.context.object;o.data.materials.append(bright);o.rotation_euler=(random.uniform(-.3,.3),random.uniform(-.3,.3),random.random())
for index in range(110):
    x=random.uniform(-13.3,13.3);y=random.uniform(-7.5,7.5)
    if abs(x)<11.8 and abs(y)<6.4:continue
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=random.uniform(.06,.23),location=(x,y,.2))
    o=bpy.context.object;o.scale.z=.4;o.data.materials.append(rose if random.random()<.55 else dark)
scene=bpy.context.scene
bpy.ops.object.camera_add(location=(0,0,30));camera=bpy.context.object;camera.rotation_euler=(0,0,0);camera.data.type='ORTHO';camera.data.ortho_scale=28
scene.camera=camera
bpy.ops.object.light_add(type='AREA',location=(-3,-4,14));bpy.context.object.data.energy=1800;bpy.context.object.data.size=15
scene.world.color=(.12,.15,.2)
scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1792;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.film_transparent=False;scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
scene.render.image_settings.file_format='PNG'
assets=Path(__file__).resolve().parents[1]
scene.render.filepath=str(assets/'Resources'/'Art'/'arena-floor.png')
bpy.ops.render.render(write_still=True)
source=assets/'ArtSource'/'Blender';source.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'starfall_arena.blend'))
