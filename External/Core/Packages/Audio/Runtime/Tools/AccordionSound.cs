using UnityEngine;

namespace NBG.Audio
{
    public class AccordionSound : MonoBehaviour
    {
        [Tooltip("Maps the volume of the object depending on its velocity")]
        [SerializeField] AnimationCurve volumeMap = default;
        [SerializeField] Transform relativeTo;
        [Tooltip("Bigger number faster sound transition")]
        [SerializeField] float transitionStep = 1;
        [SerializeField] AudioSource audioSource;
        private Vector3 previousPosition;
        private float previousDistance;
        private float velocityEstimated = 0;
        private float targetVolume;

        void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            previousPosition = transform.position;
        }

        void Update()
        {
            if (audioSource == null || previousPosition == null)
                return;

            if (relativeTo != null)
            {
                velocityEstimated = (Vector3.Distance(transform.position, relativeTo.position) - previousDistance) / Time.fixedDeltaTime;
                previousDistance = Vector3.Distance(transform.position, relativeTo.position);
            }
            else
            {
                velocityEstimated = Vector3.Distance(transform.position, previousPosition) / Time.fixedDeltaTime;
                previousPosition = transform.position;
            }

            targetVolume = volumeMap.Evaluate(Mathf.Abs(velocityEstimated));

            if (audioSource.volume < 0.01 && targetVolume < 0.01f)
                audioSource.volume = 0;

            if (audioSource.volume > (targetVolume - 0.01f) && audioSource.volume < (targetVolume + 0.01f))
                return;
            else
            {
                audioSource.volume = audioSource.volume + Mathf.Sign(targetVolume - audioSource.volume) * transitionStep * Time.deltaTime;
                audioSource.pitch = Mathf.Lerp(1.2f, 0.9f, volumeMap.Evaluate(velocityEstimated));
            }
        }
    }
}
