using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Opens animation window when opening an animation asset
    /// </summary>
    public static class OnOpenAnimationAsset
    {
        [OnOpenAsset(100)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            PhysicalAnimation anim = Selection.activeObject as PhysicalAnimation;

            if (anim != null)
            {
                NoodleAnimatorWindow.OpenWindowWithAsset();
                NoodleAnimatorWindow.LoadAnim(anim);
                return true; //catch open file
            }

            return false; // let unity open the file
        }
    }
}