using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public enum CameraRotationMode
{
    DEGREES_45,
    DEGREES_25,
    SMOOTH
}

public enum CameraMovementMode
{
    SmallJump,
    BigJump,
    Smooth,
}

public class Move : MonoBehaviour
{
    [SerializeField]
    Player player;
    [SerializeField]
    Transform cam;
    [SerializeField]
    float speed = 2;
    [SerializeField]
    float vSpeed = 2;

    [SerializeField]
    float smoothRotationEaseOutDuration = 1f;
    [SerializeField]
    float smoothRotationEaseOutMulti = 3f;
    [SerializeField]
    float tinyJumpDistance = 0.33f;
    [SerializeField]
    float bigJumpDistance = 1f;

    float rotateSpeed = 10;
    float firstRotCooldown = .5f;
    float multiRotCooldown = 0.25f;
    float autoRotateDuration = 0.3f;
    float rotCooldown = 0f;
    float rotationTime;
    float easeOutTimer;
    bool firstRotate = true;
    bool autoRotate;
    float previousAngle = 0;
    float dir;
    bool readyToJump = true;

    CameraRotationMode rotationMode;
    CameraMovementMode movementMode;
    public bool LimitedMovement { private get; set; }
    public bool LimitedRotation { private get; set; }
    public bool InstantAnimations { private get; set; }


    void Update()
    {
        if (LimitedMovement && LimitedRotation)
            return;

        var leftInput = player.leftHand.MoveDir;
        var rightInput = player.rightHand.MoveDir;

        if (!LimitedMovement)
        {
            HandleMovement(leftInput);
            HandleElevation(rightInput.y);
        }
        if (!LimitedRotation)
        {
            // If teleportation mode.
            if (LimitedMovement) 
            {
                // Handling two handed rotation.
                if (Mathf.Abs(leftInput.x) > 0.9f)
                {
                    HandleRotation(leftInput.x);
                }
                else if (Mathf.Abs(rightInput.x) > 0.9f)
                {
                    HandleRotation(rightInput.x);
                }
                else
                {
                    HandleRotation(0);
                }
            }
            // If joystick mode.
            else
            {
                HandleRotation(rightInput.x);
            }
        }
    }

    void HandleMovement(Vector2 input)
    {
        switch(movementMode)
        {
            case CameraMovementMode.Smooth:
                transform.position += input.RotateDeg(cam.transform.rotation.eulerAngles.y).To3D() * speed * Time.deltaTime;
                break;
            case CameraMovementMode.SmallJump:
            case CameraMovementMode.BigJump:
                var inputMag = input.magnitude;
                if (inputMag < 0.1f)
                {
                    readyToJump = true;
                }
                if (readyToJump && inputMag > 0.9f)
                {
                    transform.position += input.RotateDeg(cam.transform.rotation.eulerAngles.y).To3D() * GetJumpDistance(movementMode);
                    readyToJump = false;
                }
                break;

        }
    }

    void HandleElevation(float input)
    {
        if (Mathf.Abs(input) > 0.5f)
        {
            transform.position += new Vector3(0, input, 0) * vSpeed * Time.deltaTime;
        }
    }

    public void HandleRotation(float input)
    {
        if (autoRotate)
        {
            AutoRotation();
        }
        else
        {
            var readyToRotate = Mathf.Abs(input) > 0.9f && rotCooldown <= 0f;
            if (firstRotate && readyToRotate) // first rotation change
            {
                dir = Mathf.Sign(input);
                easeOutTimer = 0;
                firstRotate = false;

                CalcualteRotation(firstRotCooldown);
            }
            else if (!firstRotate && readyToRotate) //holding rotation
            {
                CalcualteRotation(multiRotCooldown);
            }
            else if (!firstRotate && Mathf.Abs(input) < 0.9f) //reset rotation if not holding
            {
                firstRotate = true;
                rotCooldown = 0f;
                easeOutTimer = 0;
                dir = 0;
            }
        }

        if (rotCooldown > 0)
        {
            rotCooldown -= Time.deltaTime;
        }
    }

    void CalcualteRotation(float cooldown)
    {
        if (rotationMode == CameraRotationMode.SMOOTH)
        {
            ConstantRotation();
        }
        else
        {
            if (InstantAnimations)
            {
                Rotate(rotateSpeed);
                rotCooldown = cooldown;
            }
            else
            {
                StartAutoRotation(cooldown);
            }
        }
    }

    void Rotate(float deltaAngle)
    {
        transform.RotateAround(cam.position, Vector3.up, dir * deltaAngle);
    }

    void ConstantRotation()
    {
        var x = Mathf.Clamp01(easeOutTimer / smoothRotationEaseOutDuration) - 1;
        var easeOut = smoothRotationEaseOutMulti * x * x + 1;
        var speed = rotateSpeed * Time.deltaTime * easeOut;

        Rotate(speed);

        easeOutTimer += Time.deltaTime;
    }

    void StartAutoRotation(float cooldown)
    {
        autoRotate = true;
        rotCooldown = cooldown + autoRotateDuration;
        rotationTime = 0;
    }

    void AutoRotation()
    {
        float lerp = Mathf.Sqrt(rotationTime / autoRotateDuration);

        float totalAngle = Mathf.Lerp(0, rotateSpeed, lerp);
        var delta = totalAngle - previousAngle;
        previousAngle = totalAngle;

        Rotate(delta);

        if (rotationTime >= autoRotateDuration)
        {
            autoRotate = false;
            previousAngle = 0;
        }

        rotationTime += Time.deltaTime;
    }

    public void SetRotationMode(CameraRotationMode rotationMode)
    {
        this.rotationMode = rotationMode;
        switch (rotationMode)
        {
            case CameraRotationMode.DEGREES_45:
                rotateSpeed = 45;
                autoRotateDuration = 0.2f;
                firstRotCooldown = 0.5f;
                multiRotCooldown = 0.25f;
                break;
            case CameraRotationMode.DEGREES_25:
                rotateSpeed = 25;
                autoRotateDuration = 0.2f;
                firstRotCooldown = 0.5f;
                multiRotCooldown = 0.1f;
                break;
            case CameraRotationMode.SMOOTH:
                rotateSpeed = 50;
                firstRotCooldown = multiRotCooldown = 0;
                break;
        }
    }

    public void SetMovementMode(CameraMovementMode movementMode)
    {
        this.movementMode = movementMode;
    }

    public float GetJumpDistance(CameraMovementMode movementMode)
    {
        switch(movementMode)
        {
            case CameraMovementMode.SmallJump:
                return tinyJumpDistance;
            case CameraMovementMode.BigJump:
                return bigJumpDistance;
        }
        return 0f;
    }
}
