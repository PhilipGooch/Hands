using NBG.LogicGraph;
using Recoil;
using System;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    [NodeAPI("OnPressedStateChanged")]
    public event Action<bool> onPressedStateChanged;

    bool pressed;
    public bool Pressed
    {
        get
        {
            return pressed;
        }
        set
        {
            pressed = value;
            onPressedStateChanged?.Invoke(pressed);
        }
    }

    [Tooltip("Rigidbody of actual button")]
    public Rigidbody button;
    [Tooltip("Travel distance to fully compress button from initial position")]
    public float travelReleased;
    [Tooltip("Travel distance for pressed state")]
    public float travelPressed;
    [Tooltip("Spring tension distance for released button")]
    public float tensionDist = .4f;
    public bool initialState;
    public bool isToggle;
    ConfigurableJoint joint;
    Vector3 fullyPressedAnchor;

    //Vector3 connectedAnchor;

    bool isInteracted = false;
    //bool isPressed = false;

    private void Start()
    {
        joint = button.GetComponent<ConfigurableJoint>();
        fullyPressedAnchor = joint.connectedAnchor - joint.axis * travelReleased;
        if (!isToggle)
            initialState = false;

        SetState(initialState);

        SetPosition(initialState ? travelPressed : travelReleased);
    }

    void SetState(bool pressed)
    {
        // don't keep pushed
        //isPressed = pressed;
        this.Pressed = pressed;

        if (!isToggle)
            pressed = false;

        var halfTravel = pressed ? travelPressed / 2 : travelReleased / 2;
        joint.autoConfigureConnectedAnchor = false;
        joint.linearLimit = new SoftJointLimit() { limit = halfTravel };
        joint.connectedAnchor = fullyPressedAnchor + joint.axis * halfTravel;
        //joint.connectedAnchor = connectedAnchor;
        joint.targetPosition = new Vector3(-halfTravel - tensionDist, 0, 0);

        // move to target pose is starting

    }

    public void SetPosition(float position)
    {
        var anchorPressed = joint.connectedBody.TransformPoint(fullyPressedAnchor) + button.TransformDirection(joint.axis) * position;
        button.transform.position = anchorPressed - button.TransformDirection(joint.anchor);
    }

    public void FixedUpdate()
    {
        var dist = (joint.connectedBody.TransformPoint(fullyPressedAnchor) - button.TransformPoint(joint.anchor)).magnitude;

        if (!isInteracted)
        {
            if (dist < travelPressed * .25f)
            {
                //Debug.Log($"{dist} {isInteracted} {isPressed}");
                isInteracted = true;
                if (isToggle)
                    SetState(!Pressed);
                else
                    SetState(true);
            }
        }
        else
        {
            var releaseTreshold = isToggle && Pressed ? travelPressed * .75f : travelReleased * .75f;
            if (dist > releaseTreshold)
            {
                //Debug.Log($"{dist} {isInteracted} {isPressed}");
                isInteracted = false;
                if (!isToggle)
                    SetState(false);
            }
        }

    }
}
