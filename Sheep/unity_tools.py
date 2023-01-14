import bpy
import os
import re
from collections import defaultdict

bl_info = {
 "name": "Unity Tools",
 "description": "Tools to batch export fbx files",
 "author": "Patrick Jezek",
 "blender": (2, 7, 9),
 "version": (1, 0, 0),
 "category": "Unity",
 "location": "",
 "warning": "",
 "wiki_url": "",
 "tracker_url": "",
}


class UnityBatchExportPanel(bpy.types.Panel):

    bl_idname = "PEA_unity_tools"
    bl_label = "Unity Tools"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'TOOLS'
    bl_context = "objectmode"
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):

        layout = self.layout

        # batch export
        col = layout.column(align=True)
        col.label(text="Batch export:")
        col.prop(context.scene, 'pea_batch_export_path')
        row = col.row(align=True)
        row.operator("pea.batch_export_single_aligned", text="Export One", icon='EXPORT')
        row = col.row(align=True)
        row.operator("pea.batch_export_scene_aligned", text="Export Scene", icon='EXPORT')

        col = layout.column(align=True)
        col.label(text="Legacy:")
        row = col.row(align=True)
        row.operator("pea.batch_export_single", text="Export (legacy)", icon='EXPORT')

class PeaBatchExportSingle(bpy.types.Operator):
    bl_idname = "pea.batch_export_single"
    bl_label = "Choose Directory"

    def execute(self, context):
        print ("execute Pea_batch_export_single")

        basedir = os.path.dirname(bpy.data.filepath)
        if not basedir:
            raise Exception("Blend file is not saved")

        if context.scene.pea_batch_export_path == "":
            raise Exception("Export path not set")
            
        # convert path to windows friendly notation
        dir = os.path.dirname(bpy.path.abspath(context.scene.pea_batch_export_path))

        name = bpy.context.active_object.name  
        print(name)
        fn = os.path.join(dir, name)
        print("exporting: " + fn)
        
        obj = bpy.context.active_object
        bpy.ops.object.select_grouped(extend=True, type='CHILDREN_RECURSIVE')
        
        # export fbx
        bpy.ops.export_scene.fbx(filepath=fn + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')
        return {'FINISHED'}
class PeaBatchExportSingleAligned(bpy.types.Operator):
    bl_idname = "pea.batch_export_single_aligned"
    bl_label = "Choose Directory"

    def execute(self, context):
        print ("execute Pea_batch_export_single_aligned")

        basedir = os.path.dirname(bpy.data.filepath)
        if not basedir:
            raise Exception("Blend file is not saved")

        if context.scene.pea_batch_export_path == "":
            raise Exception("Export path not set")
            
        # convert path to windows friendly notation
        dir = os.path.dirname(bpy.path.abspath(context.scene.pea_batch_export_path))

        name = bpy.context.active_object.name  
        print(name)
        fn = os.path.join(dir, name)
        print("exporting: " + fn)
        
        obj = bpy.context.active_object
        bpy.ops.object.select_grouped(extend=True, type='CHILDREN_RECURSIVE')
        
        # export fbx
        bpy.ops.export_scene.fbx(filepath=fn + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y')
        return {'FINISHED'}

def file_base_name(file_name):
    if '.' in file_name:
        separator_index = file_name.index('.')
        base_name = file_name[:separator_index]
        return base_name
    else:
        return file_name

class PeaBatchExportSceneAligned(bpy.types.Operator):
    bl_idname = "pea.batch_export_scene_aligned"
    bl_label = "Choose Directory"

    def execute(self, context):
        print ("execute Pea_batch_export_scene_aligned")

        basedir = os.path.dirname(bpy.data.filepath)
        if not basedir:
            raise Exception("Blend file is not saved")

        if context.scene.pea_batch_export_path == "":
            raise Exception("Export path not set")
            
        # convert path to windows friendly notation
        dir = os.path.dirname(bpy.path.abspath(context.scene.pea_batch_export_path))

        name = file_base_name(os.path.basename(bpy.data.filepath))
        print(name)
        fn = os.path.join(dir, name)
        print("exporting: " + fn)
        
        #obj = bpy.context.active_object
        #bpy.ops.object.select_grouped(extend=True, type='CHILDREN_RECURSIVE')
        
        # export fbx
        bpy.ops.export_scene.fbx(filepath=fn + ".fbx", use_selection=False, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y')
        return {'FINISHED'}
# registers
def register():
    bpy.types.Scene.pea_batch_export_path = bpy.props.StringProperty (
        name="Export Path",
        default="",
        description="Define the path where to export",
        subtype='DIR_PATH'
    )
    bpy.utils.register_class(UnityBatchExportPanel)
    bpy.utils.register_class(PeaBatchExportSingle)
    bpy.utils.register_class(PeaBatchExportSingleAligned)
    bpy.utils.register_class(PeaBatchExportSceneAligned)

def unregister():
    del bpy.types.Scene.pea_batch_export_path
    bpy.utils.unregister_class(UnityBatchExportPanel)
    bpy.utils.unregister_class(PeaBatchExportSingle)
    bpy.utils.unregister_class(PeaBatchExportSingleAligned)
    bpy.utils.unregister_class(PeaBatchExportSceneAligned)

if __name__ == "__main__":
    register()
