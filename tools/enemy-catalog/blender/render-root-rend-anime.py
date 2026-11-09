"""Render the owned Blender source scene; supports short frame batches via MCP."""
import bpy
import json

FIRST_FRAME = 1
LAST_FRAME = 24
scene = bpy.data.scenes["DiceLand_RootRend_Anime"]
if scene.get("diceland_asset_id") != "enemy_044_root_rend_blender_v1":
    raise RuntimeError("Unexpected scene ownership")
previous = (scene.frame_start, scene.frame_end, scene.render.filepath, scene.frame_current)
try:
    scene.frame_start = FIRST_FRAME
    scene.frame_end = LAST_FRAME
    scene.render.filepath = "C:/DiceLand/outputs/agent-battle/blender-root-rend-20261109/frames/rootrend_"
    paths = [scene.render.frame_path(frame=frame) for frame in range(FIRST_FRAME, LAST_FRAME+1)]
    bpy.ops.render.render(animation=True, scene=scene.name)
    print(json.dumps({"first":FIRST_FRAME,"last":LAST_FRAME,"files":paths}))
finally:
    scene.frame_start, scene.frame_end, scene.render.filepath = previous[:3]
    scene.frame_set(previous[3])
