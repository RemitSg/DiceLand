"""Build an editable Blender root-rend VFX scene. Run through Blender MCP.

Blender 5.2.3, EEVEE, orthographic transparent render. No external assets.
The existing scene is retained; this creates a separate named scene.
"""
import bpy
import math
import random
import json
from mathutils import Vector

SCENE_NAME = "DiceLand_RootRend_Anime"
SOURCE_PATH = "C:/DiceLand/source-art/enemies/044-root-rend/RootRendAnime.blend"
FRAME_PATH = "C:/DiceLand/outputs/agent-battle/blender-root-rend-20261109/frames/"

def enum_value(owner, property_name, wanted):
    return next(item.identifier for item in owner.bl_rna.properties[property_name].enum_items
                if item.identifier == wanted)

if bpy.data.scenes.get(SCENE_NAME) is not None:
    raise RuntimeError("The named source scene already exists; inspect it before rebuilding.")
original_scene = bpy.context.scene
scene = bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene = scene
scene["diceland_asset_id"] = "enemy_044_root_rend_blender_v1"
scene["design"] = "Three bark-cored poison-green slashes, faceted roots, leaves and fungal flecks"
scene["source_builder"] = "tools/enemy-catalog/blender/create-root-rend-anime.py"
scene.frame_start, scene.frame_end = 1, 24
scene.render.fps = 48
scene.render.fps_base = 1
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = enum_value(scene.render.image_settings, "file_format", "PNG")
scene.render.image_settings.color_mode = enum_value(scene.render.image_settings, "color_mode", "RGBA")
scene.render.image_settings.color_depth = enum_value(scene.render.image_settings, "color_depth", "8")
scene.eevee.taa_render_samples = 64
scene.view_settings.exposure = 0.15

world = bpy.data.worlds.new("RR_World")
world.use_nodes = True
next(node for node in world.node_tree.nodes if node.type == "BACKGROUND").inputs[0].default_value = (0.02, 0.035, 0.025, 1)
scene.world = world
root = bpy.data.objects.new("RootRend_Editable", None)
scene.collection.objects.link(root)

camera_data = bpy.data.cameras.new("RR_OrthoCamera")
camera_data.type = enum_value(camera_data, "type", "ORTHO")
camera_data.ortho_scale = 3.65
camera = bpy.data.objects.new("RR_OrthoCamera", camera_data)
scene.collection.objects.link(camera)
camera.location = (0, -8, 0)
camera.rotation_euler = (math.pi / 2, 0, 0)
scene.camera = camera
area_type = next(item.identifier for item in bpy.types.Light.bl_rna.properties["type"].enum_items if item.identifier == "AREA")
light_data = bpy.data.lights.new("RR_KeyLight", area_type)
light_data.energy, light_data.shape, light_data.size = 120, enum_value(light_data, "shape", "DISK"), 4
light = bpy.data.objects.new("RR_KeyLight", light_data)
scene.collection.objects.link(light)
light.location = (0, -4, 4)
light.rotation_euler = (Vector((0, 0, 0)) - light.location).to_track_quat('-Z', 'Y').to_euler()

def linear_color(hex_value):
    rgb = [int(hex_value[index:index+2], 16) / 255 for index in (0, 2, 4)]
    return tuple(value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4 for value in rgb)

def material(name, color, strength=1, alpha=1):
    result = bpy.data.materials.new("RR_" + name)
    result.use_nodes = True
    result.diffuse_color = (*linear_color(color), alpha)
    nodes = result.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs[0].default_value = (*linear_color(color), 1)
    emission.inputs[1].default_value = strength
    if name in ("Bark", "BarkFacet", "BarkShadow"):
        coordinate = nodes.new("ShaderNodeTexCoord")
        stretch = nodes.new("ShaderNodeVectorMath")
        stretch.operation = enum_value(stretch, "operation", "MULTIPLY")
        stretch.inputs[1].default_value = (9, 1, 1.6)
        grain = nodes.new("ShaderNodeTexNoise")
        grain.inputs["Scale"].default_value = 5
        grain.inputs["Detail"].default_value = 1.8
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.29
        ramp.color_ramp.elements[0].color = (*linear_color('454533'),1)
        ramp.color_ramp.elements[1].position = 0.73
        ramp.color_ramp.elements[1].color = (*linear_color('B19A70'),1)
        middle = ramp.color_ramp.elements.new(0.51)
        middle.color = (*linear_color(color),1)
        result.node_tree.links.new(coordinate.outputs[0], stretch.inputs[0])
        result.node_tree.links.new(stretch.outputs[0], grain.inputs[0])
        result.node_tree.links.new(grain.outputs[0], ramp.inputs[0])
        result.node_tree.links.new(ramp.outputs[0], emission.inputs[0])
    output = nodes.new("ShaderNodeOutputMaterial")
    if alpha < 1:
        transparent = nodes.new("ShaderNodeBsdfTransparent")
        mix = nodes.new("ShaderNodeMixShader")
        mix.inputs[0].default_value = alpha
        result.node_tree.links.new(transparent.outputs[0], mix.inputs[1])
        result.node_tree.links.new(emission.outputs[0], mix.inputs[2])
        result.node_tree.links.new(mix.outputs[0], output.inputs[0])
        modes = [item.identifier for item in result.bl_rna.properties["surface_render_method"].enum_items]
        if "BLENDED" in modes:
            result.surface_render_method = next(item for item in modes if item == "BLENDED")
    else:
        result.node_tree.links.new(emission.outputs[0], output.inputs[0])
    return result

mats = {
    'outline': material('Ink', '18271D'),
    'halo': material('SoftSapHalo', 'B9D94E', 1.1, 0.045),
    'rim': material('OuterSap', 'A8C44B', 1.25),
    'light': material('LitSap', 'C5DC73', 1.3),
    'glint': material('IvoryGlint', 'FAF0BE', 1.35),
    'bark': material('Bark', '695A42'),
    'bark_light': material('BarkFacet', '9A8763'),
    'bark_dark': material('BarkShadow', '3A3D2D'),
    'leaf': material('Leaf', '839A3B'),
    'leaf_light': material('LeafFacet', 'B8C65B'),
    'violet': material('FungalDust', '87758B'),
}

def mesh_object(name, vertices, faces, material_list, material_indices=None):
    mesh = bpy.data.meshes.new("RR_" + name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("RR_" + name, mesh)
    scene.collection.objects.link(obj)
    obj.parent = root
    for mat in material_list:
        mesh.materials.append(mat)
    if material_indices:
        for polygon, index in zip(mesh.polygons, material_indices):
            polygon.material_index = index
    return obj

def smooth(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)

def path(t, offset):
    # A hook-ended diagonal claw arc, with a tiny physical depth curve.
    u = 1-t
    x = -0.95*u*u*u - 0.55*3*u*u*t + 1.35*3*u*t*t + 0.45*t*t*t
    z = -1.15*u*u*u + 0.05*3*u*u*t + 1.0*3*u*t*t + 1.02*t*t*t
    return Vector((x + offset[0], 0.025*math.sin(math.pi*t), z + offset[1]))

def ribbon(name, offset, width, depth, mat, center_bias=0, texture=False, delay=0):
    segments = 36
    vertices, faces = [], []
    for index in range(segments+1):
        t = index/segments
        point = path(t, offset)
        tangent = path(min(1, t+0.001), offset) - path(max(0, t-0.001), offset)
        normal = Vector((-tangent.z, 0, tangent.x)).normalized()
        profile = max(0, math.sin(math.pi*t)) ** 0.67 * (1 - 0.32*t)
        if texture:
            profile *= 1 + 0.16*math.sin(21*t) + 0.08*math.sin(49*t)
        center = point + normal * center_bias * profile
        for sign in (-1, 1):
            p = center + normal * sign * width * profile
            p.y += depth
            vertices.append(tuple(p))
        if index:
            faces.append((2*index-2, 2*index-1, 2*index+1, 2*index))
    obj = mesh_object(name, vertices, faces, [mat])
    basis = obj.shape_key_add(name="Basis")
    start = path(0, offset)
    for vertex in basis.data:
        vertex.co = start
    # Baked per-frame shape keys make the slash draw and erase along its curve.
    # The scene remains editable and uses no drivers or handlers.
    for frame in range(1,25):
        front = smooth((frame-1.5-delay)/5)
        tail = min(1, smooth((frame-13-delay)/9)*1.15)
        key = obj.shape_key_add(name="Sweep_%02d"%frame)
        for index in range(segments+1):
            t = index/segments
            visible = tail <= t <= front and tail < front and frame < 24
            clipped = max(tail, min(t, front))
            point = path(clipped, offset)
            tangent = path(min(1,clipped+0.001),offset)-path(max(0,clipped-0.001),offset)
            normal = Vector((-tangent.z,0,tangent.x)).normalized()
            profile = max(0,math.sin(math.pi*clipped))**0.67*(1-0.32*clipped) if visible else 0
            if texture:
                profile *= 1+0.16*math.sin(21*clipped)+0.08*math.sin(49*clipped)
            center = point+normal*center_bias*profile
            for side,sign in enumerate((-1,1)):
                p=center+normal*sign*width*profile
                p.y+=depth
                key.data[index*2+side].co=p
        for key_frame,value in ((frame-1,0),(frame,1),(frame+1,0)):
            key.value=value
            key.keyframe_insert(data_path="value",frame=key_frame)
        key.value=0
    return obj

def animate_slash(obj, delay):
    for frame in range(1,25):
        grow = smooth((frame - 1.5 - delay)/5)
        vanish = 1 - smooth((frame - 12 - delay)/9)
        if frame == 24:
            vanish = 0
        scale = vanish
        obj.scale = (scale, scale, scale)
        obj.location = (-0.22*(1-grow) + 0.16*(1-vanish), 0, -0.22*(1-grow) + 0.21*(1-vanish))
        obj.keyframe_insert(data_path="scale", frame=frame)
        obj.keyframe_insert(data_path="location", frame=frame)

for index, offset in enumerate([(-0.42,-0.12),(0,0.05),(0.43,0.17)]):
    layers = [
        ('Halo',0.245,0.055,mats['halo'],0,False),
        ('Outline',0.174,0.035,mats['outline'],0,False),
        ('Sap',0.151,0.018,mats['rim'],0,False),
        ('LitEdge',0.033,-0.003,mats['light'],-0.091,False),
        ('RootCore',0.062,-0.015,mats['bark'],0.032,True),
        ('RootFacet',0.018,-0.024,mats['bark_light'],0.055,True),
        ('Glint',0.007,-0.033,mats['glint'],-0.119,False),
    ]
    for label,width,depth,mat,bias,texture in layers:
        obj = ribbon('Slash%d_%s'%(index+1,label),offset,width,depth,mat,bias,texture,index*0.65)
        animate_slash(obj,index*0.65)

rng = random.Random(4401)
for index in range(15):
    leaf = index < 10
    length = rng.uniform(0.085,0.16) if leaf else rng.uniform(0.025,0.065)
    width = length*(0.38 if leaf else 0.75)
    vertices=[(0,0,-length),(-width,0,0.02),(0,-0.018,length),(width,0,0.02),(0,-0.024,0)]
    obj=mesh_object('Leaf_%02d'%index,vertices,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],
        [mats['outline'],mats['leaf'] if leaf else mats['violet'],mats['leaf_light']], [1,2,1,1])
    angle=rng.uniform(0,math.tau)
    origin=Vector((rng.uniform(-0.65,0.65),-0.09,rng.uniform(-0.5,0.65)))
    velocity=Vector((math.cos(angle)*rng.uniform(0.5,1.1),0,math.sin(angle)*rng.uniform(0.5,1.1)))
    born=rng.uniform(6.2,9.5)
    spin=rng.uniform(-2.8,2.8)
    for frame in range(1,25):
        age=max(0,(frame-born)/13)
        alive=smooth((frame-born)/2)*(1-smooth((frame-15)/9)) if frame>=born and frame<24 else 0
        obj.scale=(alive,alive,alive)
        obj.location=origin+velocity*age+Vector((0,0,-0.38*age*age))
        obj.rotation_euler=(0.6*math.sin(age*3),angle+spin*age,0.4*age)
        for channel in ("scale","location","rotation_euler"):
            obj.keyframe_insert(data_path=channel,frame=frame)

for index,(x,z,size) in enumerate([(-0.12,0.22,0.13),(0.55,0.85,0.09),(-0.7,-0.5,0.07)]):
    vertices=[(0,-0.14,-size),(-size*0.22,-0.14,0),(0,-0.14,size),(size*0.22,-0.14,0)]
    obj=mesh_object('ImpactGlint%d'%index,vertices,[(0,1,2,3)],[mats['glint']])
    obj.location=(x,0,z)
    for frame in range(1,25):
        value=max(0,1-abs(frame-(7+index*0.6))/3) if frame<24 else 0
        obj.scale=(value,value,value)
        obj.keyframe_insert(data_path="scale",frame=frame)

scene.frame_set(8)
scene.render.filepath=FRAME_PATH+"peak.png"
bpy.ops.wm.save_as_mainfile(filepath=SOURCE_PATH,copy=True,check_existing=False,compress=True)
print(json.dumps({'scene':scene.name,'source':SOURCE_PATH,'objects':len(scene.objects),'meshes':len([o for o in scene.objects if o.type=='MESH']), 'frames':24,'fps':48,'engine':scene.render.engine,'camera_scale':camera_data.ortho_scale,'view_transform':scene.view_settings.view_transform}))
