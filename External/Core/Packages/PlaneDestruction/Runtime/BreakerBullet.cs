using NBG.MeshGeneration;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    public class BreakerBullet : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float minSpeedToBreak = 1.0f;
        [SerializeField] private float pieceVelocityMultiplier = 0.7f;
        [SerializeField] private float minRadius, maxRadius;

        [SerializeField] private float cutWidth = 0.05f;
        [SerializeField] private float piecesScale = 0.6f;

        [SerializeField] private Polygon2DAsset polygonAsset;
        [SerializeField] private float assetResize = 1.0f;

        [SerializeField] private bool useDetectionSphereRadius = false;
        [SerializeField] private float detectionSphereRadius;
        [SerializeField] private string nameFilter;

        [HeaderAttribute("Debug")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private bool debugSpeed = false;

        public delegate void OnBreak();
        public OnBreak onBreak;

        private RaycastHit[] hits;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (rb == null)
                rb = transform.GetComponentInParent<Rigidbody>();

            hits = new RaycastHit[10];
        }

        private void OnDrawGizmos()
        {
            if (showGizmo)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, minRadius);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, maxRadius);

                if (useDetectionSphereRadius)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, detectionSphereRadius);
                }

                if (maxRadius < minRadius)
                {
                    maxRadius = minRadius + 0.1f;
                }
            }
        }

        private void FixedUpdate()
        {
            Vector3 velocity = rb.velocity;
            Ray ray = new Ray
            {
                direction = velocity.normalized,
                origin = transform.position
            };

            if (velocity.magnitude > minSpeedToBreak)
            {
                float detectionRadious = useDetectionSphereRadius ? detectionSphereRadius : minRadius;
                int max = Physics.SphereCastNonAlloc(ray.origin, detectionRadious, ray.direction, hits, velocity.magnitude * Time.fixedDeltaTime * 4.0f, ~(1 << gameObject.layer));

                for (int i = 0; i < max; i++)
                {
                    RaycastHit hit = hits[i];

                    if (debugSpeed)
                    {
                        Debug.Log("Breaker bullet speed on hit " + velocity.magnitude);
                    }

                    if (nameFilter?.Equals("") != false || hit.collider.name.Equals(nameFilter))
                    {
                        BreakableWall breakableWall = hit.collider.GetComponent<BreakableWall>();

                        if (breakableWall != null)
                        {
                            bool assetAvailable = polygonAsset != null;
                            Vector3 pos = breakableWall.transform.InverseTransformPoint(transform.position);

                            Polygon2D shape;
                            if (assetAvailable)
                                shape = polygonAsset.polygon;
                            else
                                shape = BreakableWall.CreateRandomBreakShape(20, minRadius, maxRadius);

                            breakableWall.BreakAndUpdate(pos, velocity * pieceVelocityMultiplier, shape, assetAvailable ? assetResize : 1.0f, cutWidth, false, piecesScale);
                            breakableWall.PlayParticles(transform.position, -velocity, true);

                            onBreak?.Invoke();
                        }
                    }
                }
            }
        }
    }
}
