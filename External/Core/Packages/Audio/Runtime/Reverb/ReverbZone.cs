using UnityEngine;

namespace NBG.Audio
{
    [RequireComponent(typeof(BoxCollider))]
    public class ReverbZone : MonoBehaviour
    {
        public float weight = 1;
        public float level = 0;
        public float delay = .5f;
        public float diffusion = .5f;
        public float innerZoneOffset = 2;
        public float lowPass = 22000;
        public float highPass = 10;
#pragma warning disable
        BoxCollider collider;
#pragma warning enable

        void OnEnable()
        {
            collider = GetComponent<BoxCollider>();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.name == "Ball") // TODO: change this to probably other.layer == "Player" when player prefab is updated.
                Reverb.instance.ZoneEntered(this);
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.name == "Ball") // TODO: change this to probably other.layer == "Player" when player prefab is updated.
                Reverb.instance.ZoneLeft(this);
        }

        public float GetWeight(Vector3 pos)
        {
            var localPos = transform.InverseTransformPoint(pos) - collider.center;
            var x = Mathf.InverseLerp(0, innerZoneOffset, collider.size.x / 2 - Mathf.Abs(localPos.x));
            var y = Mathf.InverseLerp(0, innerZoneOffset, collider.size.y / 2 - Mathf.Abs(localPos.y));
            var z = Mathf.InverseLerp(0, innerZoneOffset, collider.size.z / 2 - Mathf.Abs(localPos.z));
            var fweight = Mathf.Min(Mathf.Min(x, y), z) * this.weight;
            return fweight;
        }

        public void OnDrawGizmosSelected()
        {
            collider = GetComponent<BoxCollider>();
            var backup = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 0, 0, 0.8f);
            Gizmos.DrawCube(collider.center, collider.size - Vector3.one * innerZoneOffset * 2);
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawCube(collider.center, collider.size);
            Gizmos.matrix = backup;
        }
    }
}
