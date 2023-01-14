using UnityEngine;
using System.Collections.Generic;

public class Teleportation : SingletonBehaviour<Teleportation>
{
    [SerializeField]
    Player player;
    [SerializeField]
    float vibrationDuration = 0.1f;
    [SerializeField]
    [Range(0, 1)]
    float vibrationAmplitude = 0.5f;
    [Tooltip("The number of line segments a full length arc is broken into. (No effect when changing while running.)")]
    public int arcSegmentCount = 30;
    [Tooltip("The amount of time in seconds to predict the motion of the projectile. Affects the maximum distance of the arc.")]
    public float arcDuration = 3.0f;
    [SerializeField]
    [Tooltip("The speed at which the projectile is launched.")]
    static float arcSpeed = 30.0f;
    [SerializeField]
    [Tooltip("How far the controller needs to move on the Y axis to trigger aiming the arc.")]
    [Range(0, 1)]
    float controllerMinY = 0.8f;
    [SerializeField]
    [Tooltip("How far the controller needs to move back on the Y axis to trigger teleportation when aiming the arc.")]
    [Range(0, 1)]
    float controllerMaxY = 0.8f;

    public Vector3 ArcStartPosition { get; private set; }
    public Vector3 ArcVelocity { get; private set; }
    public bool ShouldTeleport { get; set; }

    bool readyToTeleport;
    bool teleportationTriggered;
    Hand teleportingHand;
    TeleportationVisuals teleportationVisuals;
    TeleportationGrid teleportationGrid;

    protected override void Awake()
    {
        base.Awake();
        teleportationVisuals = GetComponent<TeleportationVisuals>();
    }

    private void Start()
    {
        teleportingHand = player.rightHand;
    }

    private void HandleInput()
    {
        Vector2 leftInput = player.leftHand.MoveDir;
        Vector2 rightInput = player.rightHand.MoveDir;

        if (!readyToTeleport && leftInput.y > controllerMaxY)
        {
            teleportingHand = player.leftHand;
            readyToTeleport = true;
        }
        if (!readyToTeleport && rightInput.y > controllerMaxY)
        {
            teleportingHand = player.rightHand;
            readyToTeleport = true;
        }
        if (readyToTeleport)
        {
            if (teleportingHand.MoveDir.y < controllerMinY)
            {
                teleportationTriggered = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!ShouldTeleport)
            return;

        if (!teleportationGrid)
            return;

        HandleInput();

        if (readyToTeleport)
        {

            teleportationGrid.SetVisible(true);

            ArcStartPosition = teleportingHand.HandPositions.PointingPosition;
            ArcVelocity = teleportingHand.transform.rotation * Vector3.forward * arcSpeed;

            RaycastHit hitInfo;
            float hitTime;
            bool arcCollision = CheckForArcCollision(ArcStartPosition, ArcVelocity, out hitInfo, out hitTime);

            if (arcCollision)
            {
                teleportationVisuals.DrawInvalidRing(hitInfo.point, hitInfo.normal);
                teleportationVisuals.SetArcColor(false);
                teleportationVisuals.DrawArc(hitTime);
            }
            else
            {
                float arcTimeAtGridHeight = Arc.GetArcTimeAtHeight(teleportationGrid.GetGridHeight(), ArcStartPosition, ArcVelocity);
                Vector3 arcPositionAtGridHeight = Arc.GetArcPositionAtTime(arcTimeAtGridHeight, ArcStartPosition, ArcVelocity);
                Vector2Int arcGridPosition = new Vector2Int(Mathf.FloorToInt(arcPositionAtGridHeight.x + 0.5f), Mathf.FloorToInt(arcPositionAtGridHeight.z + 0.5f));
                bool validGridPosition = teleportationGrid.IsPositionValid(arcGridPosition);

                if (validGridPosition)
                {
                    teleportationVisuals.DrawValidRing(arcPositionAtGridHeight);
                    teleportationVisuals.SetArcColor(true);
                    teleportationVisuals.DrawArc(arcTimeAtGridHeight);

                    if (teleportationTriggered)
                    {
                        Teleport(arcPositionAtGridHeight);
                    }
                }
                else
                {
                    teleportationVisuals.HideRing();
                    teleportationVisuals.SetArcColor(false);
                    teleportationVisuals.DrawArc(arcTimeAtGridHeight);
                }
            }

            if (teleportationTriggered)
            {
                teleportationGrid.SetVisible(false);
                teleportationVisuals.HideArc();
                teleportationVisuals.HideRing();
                readyToTeleport = false;
                teleportationTriggered = false;
            }
        }
    }

    private async void Teleport(Vector3 position)
    {
        await UICamera.Instance.FadeToBlack();
        transform.position += new Vector3(position.x - player.mainCamera.transform.position.x, 0.0f, position.z - player.mainCamera.transform.position.z);
        teleportingHand.Vibrate(0, vibrationDuration, 1, vibrationAmplitude);
        await UICamera.Instance.FadeFromBlack();
    }

    public void RegisterGrid(TeleportationGrid grid)
    {
        teleportationGrid = grid;
        grid.SetVisible(false);
    }

    public void UnregisterGrid(TeleportationGrid grid)
    {
        if (teleportationGrid == grid)
        {
            teleportationGrid = null;
        }
    }

    private bool CheckForArcCollision(Vector3 arcStartPosition, Vector3 arcVelocity, out RaycastHit hitInfo, out float hitTime)
    {
        float arcTimeAtGridHeight = Arc.GetArcTimeAtHeight(teleportationGrid.GetGridHeight(), arcStartPosition, arcVelocity);
        float timeStep = arcDuration / arcSegmentCount;
        float segmentStartTime = 0.0f;
        hitInfo = new RaycastHit();  // hitInfo must be assigned to. arcSegmentCount may be 0 so not guaranteed to be set by line cast...
        Vector3 segmentStartPos = Arc.GetArcPositionAtTime(segmentStartTime, arcStartPosition, arcVelocity);
        for (int i = 0; i < arcSegmentCount; ++i)
        {
            float segmentEndTime = segmentStartTime + timeStep;
            Vector3 segmentEndPos = Arc.GetArcPositionAtTime(segmentEndTime, arcStartPosition, arcVelocity);
            bool hit = Physics.Linecast(segmentStartPos, segmentEndPos, out hitInfo);
            if (hit)
            {
                float segmentDistance = Vector3.Distance(segmentStartPos, segmentEndPos);
                hitTime = segmentStartTime + (timeStep * (hitInfo.distance / segmentDistance));
                if(hitTime < arcTimeAtGridHeight)
                {
                    return true;
                }
            }
            else if (segmentEndTime >= arcTimeAtGridHeight)
            {
                hitTime = arcTimeAtGridHeight;
                return false;
            }
            segmentStartTime = segmentEndTime;
            segmentStartPos = segmentEndPos;
        }
        hitTime = float.MaxValue;
        return false;
    }
}
