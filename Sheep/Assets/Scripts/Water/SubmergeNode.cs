using NBG.LogicGraph;
using System;
using UnityEngine;

public class SubmergeNode : MonoBehaviour
{
    [NodeAPI("OnSubmerged")]
    public event Action<bool> onSubmergedStateChanged;
    [NodeAPI("IsDry")]
    public event Action<bool> onDryStateChanged;

    InteractableEntity targetEntity;
    bool gotSubmergeEvent = false;
    bool submerged = false;

    void Awake()
    {
        targetEntity = GetComponentInParent<InteractableEntity>();
        ResetState();
        if (targetEntity != null)
        {
            targetEntity.onSubmerged += ReactToSubmerge;
            targetEntity.onResetState += ResetState;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (submerged)
        {
            // Allow for one frame to pass before becoming wet
            // This way it becomes possible to distinquish between the first submerge event and the subsequent events
            // by doing isDry && wasSubmergedThisFrame
            onDryStateChanged?.Invoke(false);
        }

        if (gotSubmergeEvent)
        {
            if (!submerged)
            {
                onSubmergedStateChanged?.Invoke(true);
            }

            submerged = true;
            gotSubmergeEvent = false;
        }
        else
        {
            if (submerged)
                onSubmergedStateChanged.Invoke(false);

            submerged = false;
        }
    }

    void ReactToSubmerge()
    {
        gotSubmergeEvent = true;
    }

    void ResetState()
    {
        gotSubmergeEvent = false;
        submerged = false;
    }
}
