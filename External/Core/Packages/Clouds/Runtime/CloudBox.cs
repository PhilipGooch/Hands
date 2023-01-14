using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NBG.Clouds
{
    public class CloudBox : MonoBehaviour
    {
        public float fadeInDuration = 0;
        public float fadeInTime = 0;
        public float fade = 1;
        public Vector3 innerSize = new Vector3(100, 50, 100);
        public Vector3 outerSize = new Vector3(120, 70, 120);

        public static List<CloudBox> all = new List<CloudBox>();
        public static System.Object cloudLock = new System.Object();

        //public static CloudBox main;

        void OnEnable()
        {
            lock (cloudLock)
            {
                all.Add(this);
            }
        }

        void OnDisable()
        {
            lock (cloudLock)
            {
                all.Remove(this);
            }
        }

        // saves 0.5ms per 4K
        Vector3 transformPosition;
        public void ReadPos()
        {
            transformPosition = transform.position;
        }

        public float GetAlpha(Vector3 pos)
        {
            //var relative = pos-transform.position;
            //var x = 1 - Mathf.Clamp01((Mathf.Abs(relative.x * 2) - innerSize.x) / (outerSize.x - innerSize.x));
            //var y = 1 - Mathf.Clamp01((Mathf.Abs(relative.y * 2) - innerSize.y) / (outerSize.y - innerSize.y));
            //var z = 1 - Mathf.Clamp01((Mathf.Abs(relative.z * 2) - innerSize.z) / (outerSize.z - innerSize.z));
            //var alpha = x * y * z;
            //if (cleanInside) alpha = 1 - alpha;
            //return Mathf.Lerp(1, alpha,fade);

            // improves by .45ms on 4K particles
            var alpha = 0f;
            var relative = pos - transformPosition;
            var x = 1 - (Mathf.Abs(relative.x * 2) - innerSize.x) / (outerSize.x - innerSize.x);
            if (x > 0)
            {
                var y = 1 - (Mathf.Abs(relative.y * 2) - innerSize.y) / (outerSize.y - innerSize.y);
                if (y > 0)
                {
                    var z = 1 - (Mathf.Abs(relative.z * 2) - innerSize.z) / (outerSize.z - innerSize.z);
                    if (z > 0)
                    {
                        alpha = Mathf.Clamp01(x) * Mathf.Clamp01(y) * Mathf.Clamp01(z);
                    }
                }
            }
            alpha = 1 - alpha;
            return fade * alpha + (1 - fade);
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, innerSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, outerSize);
        }

        void Update()
        {
            if (fadeInDuration == 0)
            {
                fade = 1;
                return;
            }
            else
            {
                fadeInTime += Time.deltaTime;
                fade = Mathf.Clamp01(fadeInTime / fadeInDuration);
            }
        }

        public void FadeIn(float duration)
        {
            fadeInTime = 0;
            fadeInDuration = duration;
        }

    }
}