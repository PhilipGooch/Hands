using UnityEngine;
using System.Collections.Generic;
using NBG.Core;

namespace NBG.Audio
{
    public class Listener : MonoBehaviour
    {
        public static Listener instance;
        Transform originalParent;
        public List<Transform> earList = new List<Transform>();
        bool transformOverride;
        private void Awake()
        {
            if (instance != null)
            {
                DestroyImmediate(this.gameObject);
            }
            else
            {
                if (originalParent == null)
                    originalParent = transform.parent;
            }
        }
        void OnEnable()
        {
            if (instance == null) instance = this;
        }

        private void OnDisable()
        {
            Update();
        }
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void OverrideTransform(Transform t)
        {
            //Debug.Log("Added transform override: " + t);
            transformOverride = true;
            transform.SetParent(t, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        public void EndTransfromOverride()
        {
            //Debug.Log("Removed Transform Override");
            transformOverride = false;
            UpdateHierarchy();
        }

        public void AddListenTransform(Transform ears)
        {
            earList.Add(ears);
            UpdateHierarchy();
        }

        public void RemoveListenTransform(Transform ears)
        {
            if (earList.Contains(ears))
                earList.Remove(ears);
            UpdateHierarchy();
        }

        void UpdateHierarchy()
        {
            if (transformOverride) return;

            // we mustn't ever set the parent of this to null or we could end up destroying the Listener (which would be baaaad!).
            // quick prune of earList to make sure destroyed objects are eliminated
            earList.Remove(null);

            if (earList.Count == 1)
            {
                if (transform.parent != earList[0])
                {
                    transform.SetParent(earList[0], false);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
            }
            else if ((transform.parent != originalParent) && (originalParent != null))
            {
                transform.SetParent(originalParent);
                //Debug.Log("Set original Transform: " + originalParent);
            }
        }

        void Update()
        {
            //PrintDebug();
            if (transformOverride) return;

            for (var i = earList.Count - 1; i > -1; i--)
            {
                if (earList[i] == null)
                {
                    earList.RemoveAt(i);
                }
            }

            if (earList.Count > 0)
            {
                transform.position = earList[0].position;
                transform.rotation = earList[0].rotation;
            }
            // if (earList.Count>0)
            // {
            //     Vector3 pos = Vector3.zero;
            //     Quaternion rot = Quaternion.identity;
            //     for(int i=0; i<earList.Count;i++)
            //     {
            //         pos = Vector3.Lerp(pos, earList[i].position, 1f / (i + 1));
            //         rot = Quaternion.Slerp(rot, earList[i].rotation, 1f / (i + 1));
            //     }
            //     transform.position = pos;
            //     transform.rotation = rot;
            // }
        }
        private void PrintDebug()
        {
            var sb = new System.Text.StringBuilder();
            if (transformOverride)
            {
                sb.Append("TransformOverride: " + transform?.gameObject.GetFullPath());
            }
            else
            {
                sb.Append("Ears: ");
                foreach (var ear in earList)
                {
                    if (ear == null) sb.Append("\n\tnull");
                    sb.Append("\n\t" + ear.gameObject.GetFullPath());
                }
                //DebugUI.QuickPrint = sb.ToString(); //TODO@AUDIO
            }
        }
    }
}
