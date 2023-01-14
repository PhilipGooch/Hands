using System;
using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor
{
    public static class TouchAllMaterials
    {
        [MenuItem("No Brakes Games/Utilities/Touch all Materials")]
        static void DoTouchAllMaterials()
        {
            var count = 0;
            var errors = 0;

            try
            {
                AssetDatabase.DisallowAutoRefresh();

                var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mainAsset = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mainAsset == null)
                        continue;
                    ++count;

                    try
                    {
                        // Querying for a property forces the material to be correctly reserialized ¯\_(ツ)_/¯
                        mainAsset.HasProperty("_NBG_TOUCH_ALL_MATERIALS_DUMMY_");
                    }
                    catch (Exception)
                    {
                        ++errors;
                    }
                    finally
                    {
                        EditorUtility.SetDirty(mainAsset);
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.AllowAutoRefresh();
            }

            GC.Collect();
            Debug.Log($"Touched {count} materials. Got {errors} errors.");
        }
    }
}
