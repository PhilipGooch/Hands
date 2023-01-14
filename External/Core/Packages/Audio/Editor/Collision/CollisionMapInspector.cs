using UnityEditor;
using UnityEngine;

namespace NBG.Audio.Editor
{
    [CustomEditor(typeof(CollisionMap))]
    public class CollisionMapInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CollisionMap myScript = (CollisionMap)target;

            if (GUILayout.Button("Rebuild Map"))
            {
                myScript.CollisionConfigs.Clear();

                string[] guids;

                guids = AssetDatabase.FindAssets("t:CollisionAudioSurfSurfConfig");
                foreach (string guid in guids)
                {
                    CollisionAudioSurfSurfConfig tmp = (CollisionAudioSurfSurfConfig)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(CollisionAudioSurfSurfConfig));
                    myScript.CollisionConfigs.Add(tmp);
                }
                EditorUtility.SetDirty(target);
            }

            DrawDefaultInspector();
        }
    }
}
