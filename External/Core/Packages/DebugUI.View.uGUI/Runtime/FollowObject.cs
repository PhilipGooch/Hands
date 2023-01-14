using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.DebugUI.View.uGUI
{
    public class FollowObject : MonoBehaviour
    {
        [SerializeField]
        float distanceFromObject = 20f;
        [SerializeField]
        float moveDuration = 1f;

        float timer = 0;

        Vector3 goalPosition;
        Quaternion goalRotation;

        bool moving = false;
        bool movedOnce = false;

        const float maxAngleChangeToMove = 0.01f;
        const float sqrMinPositionChangeToMove = 15f;
        const int framesStored = 5;

        List<Quaternion> lastTargetRotations = new List<Quaternion>();

        [SerializeField]
        Transform objectToFollow;

        public void Setup(Transform toFollow, float followDistance)
        {
            objectToFollow = toFollow;
            distanceFromObject = followDistance;
        }

        private void OnEnable()
        {
            StopAllCoroutines();

            moving = false;
            movedOnce = false;
        }

        private void LateUpdate()
        {

            if (!movedOnce)
            {
                SetNewPosAndRot();
                moving = true;
                movedOnce = true;
                timer = 0;
            }

            SaveTargetRotationAndPositions();

            if (!moving)
            {
                if (ShouldMove())
                {
                    SetNewPosAndRot();
                }
                if (Vector3.SqrMagnitude(goalPosition - transform.position) > sqrMinPositionChangeToMove)
                {
                    moving = true;
                    timer = 0;
                    Move();
                }
            }
            else
            {
                Move();
            }
        }

        void Move()
        {
            if (timer <= moveDuration)
            {

                float lerp = Mathf.SmoothStep(0, 1, timer / moveDuration);

                transform.position = Vector3.Lerp(transform.position, goalPosition, lerp);
                transform.rotation = Quaternion.Lerp(transform.rotation, goalRotation, lerp);

                timer += Time.deltaTime;
            }
            else
            {
                transform.position = goalPosition;
                transform.rotation = goalRotation;
                moving = false;

            }
        }

        void SetNewPosAndRot()
        {
            Vector3 lookRotation = objectToFollow.forward;
            goalPosition = objectToFollow.position + objectToFollow.forward * distanceFromObject - objectToFollow.up * 0.5f;
            goalRotation = Quaternion.LookRotation(lookRotation, Vector3.up);
        }

        void SaveTargetRotationAndPositions()
        {
            if (lastTargetRotations.Count + 1 == framesStored)
            {
                lastTargetRotations.RemoveAt(0);
            }

            lastTargetRotations.Add(objectToFollow.rotation);

        }

        bool ShouldMove()
        {
            var firstRot = lastTargetRotations[0];
            var totalAngleDifs = 0f;

            for (int i = 1; i < lastTargetRotations.Count; i++)
            {
                totalAngleDifs += Quaternion.Angle(firstRot, lastTargetRotations[i]);
            }

            totalAngleDifs /= lastTargetRotations.Count;
            return totalAngleDifs < maxAngleChangeToMove;

        }
    }
}
