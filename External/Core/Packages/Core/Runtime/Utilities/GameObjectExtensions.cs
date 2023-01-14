using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core
{
    public static class GameObjectExtension
    {
        public static string GetFullPath(this GameObject obj)
        {
            if (obj == null)
                return "null";

            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }

            return obj.scene.name + "/" + path;
        }
        public static GameObject FindChildRecursive(this GameObject go, string name)
        {
            foreach (Transform child in go.transform)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
                else
                {
                    var found = FindChildRecursive(child.gameObject, name);
                    if (found != null) return found;
                }
            }
            return null;
        }
        public static bool FindDisablers(this GameObject go, ref List<GameObject> disablingParents)
        {
            if (go.activeInHierarchy)
                return false;

            bool ret = false;
            if (!go.activeSelf)
            {
                ret = true;
                disablingParents.Add(go);
            }
            
            var current = go.transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    disablingParents.Add(current.gameObject);
                    ret = true;
                }
                current = current.parent;
            }

            return ret;
        }
    }
}
