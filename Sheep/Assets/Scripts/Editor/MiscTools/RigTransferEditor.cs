using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RigTransferEditor : EditorWindow
{
    SkinnedMeshRenderer transferFrom;
    Transform rootBone;

    [MenuItem("Tools/Sheep/Rig Transfer...")]
    static void Init()
    {
        var window = (RigTransferEditor)GetWindow(typeof(RigTransferEditor));
        window.Show();
    }

    void OnGUI()
    {
        transferFrom = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Transfer From", transferFrom, typeof(SkinnedMeshRenderer), true);
        rootBone = (Transform)EditorGUILayout.ObjectField("Root Bone", rootBone, typeof(Transform), true);

        if (transferFrom != null && rootBone != null)
        {
            if (GUILayout.Button("Transfer"))
            {
                var targets = new List<Transform>();
                var currentRoot = rootBone;
                foreach(var bone in transferFrom.bones)
                {
                    Debug.Log(bone.name);
                    if (currentRoot != null)
                    {
                        targets.Add(currentRoot);
                        currentRoot = currentRoot.GetChild(0);
                    }
                }

                if (targets.Count == transferFrom.bones.Length)
                {
                    transferFrom.bones = targets.ToArray();
                    transferFrom.rootBone = rootBone;
                    Debug.Log($"{targets.Count} bones transfered successfully to {rootBone.name}.");
                }
            }
        }
    }
}
