using UnityEngine;
using VR.System;

public class PlaceInfrontOfPlayer : MonoBehaviour
{
    [SerializeField]
    private bool alwaysAtPlayersHeight = false;
    [SerializeField]
    private bool constantFollow = true;

    private float distanceFromCamera = 20f;
    private float positionChangeToMove = 12f;
    private float followSpeed = 3;
    private float firstMoveSpeed = 10;

    private float sqrMinPositionChangeToMove;
    private float minSqrDistToCam;
    private float maxSqrDistToCam;

    private Vector3 targetPosition;

    private bool isEditor => (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor);
    private bool initialized = false;
    private bool moving = false;
    private bool movedOnce = false;
    private bool stopped = true;

    private Transform cameraTransform;

    private Transform GetCameraTransform()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        return cameraTransform;
    }

    public void PlaceInFrontOfPlayer()
    {
        StopAllCoroutines();

        var settings = GameParameters.Instance;
        distanceFromCamera = settings.distanceFromCamera;
        positionChangeToMove = settings.positionChangeToMove;
        followSpeed = settings.followSpeed;
        firstMoveSpeed = settings.firstMoveSpeed;

        minSqrDistToCam = distanceFromCamera * distanceFromCamera * 0.50f;
        maxSqrDistToCam = distanceFromCamera * distanceFromCamera * 1.20f;

        sqrMinPositionChangeToMove = positionChangeToMove * positionChangeToMove;

        stopped = false;
        moving = false;
        movedOnce = false;
        initialized = false;
    }

    private void OnDisable()
    {
        stopped = true;
    }

    private void LateUpdate()
    {
        if (stopped)
            return;

        if (!initialized)
        {
            initialized = isEditor || (VRSystem.Instance.Initialized && !VRSystem.Instance.Calibrating && Player.Initialized);
        }
        else
        {
            if (!constantFollow && movedOnce)
                return;

            SetNewTargetPos();

            if (!movedOnce && !moving)
            {
                moving = true;
            }

            var distDeltaSqr = Vector3.SqrMagnitude(targetPosition - transform.position);
            var distToCamSqr = Vector3.SqrMagnitude(cameraTransform.position - transform.position);

            bool distBeyondThresh = distDeltaSqr > sqrMinPositionChangeToMove || distToCamSqr < minSqrDistToCam || distToCamSqr > maxSqrDistToCam;

            if (!moving)
            {
                if (distBeyondThresh)
                {
                    ConstantMove(followSpeed);
                    moving = true;
                }
            }
            else
            {
                if (distDeltaSqr > 4)
                {
                    ConstantMove(movedOnce ? followSpeed : firstMoveSpeed);
                }
                else
                {
                    moving = false;
                    movedOnce = true;
                }
            }
        }
    }

    private void ConstantMove(float speed)
    {
        var distClamped = Mathf.Clamp(Vector3.SqrMagnitude(targetPosition - transform.position), 0.1f, 1);

        var lerp = distClamped * speed * Time.deltaTime;

        var camPos = GetCameraTransform().transform.position;

        //to keep this object at a constant distance despite lerp speed or distance to the goal
        transform.position = (Vector3.Lerp(transform.position, targetPosition, lerp) - camPos).normalized * distanceFromCamera + camPos;
        transform.rotation = Quaternion.LookRotation((transform.position - camPos).normalized);
    }

    private void SetNewTargetPos()
    {
        var cameraTransform = GetCameraTransform();

        float height = cameraTransform.position.y;

        if (alwaysAtPlayersHeight)
        {
            targetPosition = cameraTransform.position + cameraTransform.forward;
            targetPosition.y = height;
            Vector3 dir = (targetPosition - cameraTransform.position).normalized;
            targetPosition = cameraTransform.position + (dir * distanceFromCamera);
        }
        else
        {
            targetPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera - cameraTransform.up * 0.5f;
        }
    }
}
