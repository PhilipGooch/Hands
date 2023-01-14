using UnityEngine;
using Recoil;
using NBG.Core;

[RequireComponent(typeof(Attenuation))]
public class BlockedActivatorFeedback : HapticsBase, IOnFixedUpdate, IManagedBehaviour
{
    [HideInInspector]
    [SerializeField]
    private Attenuation attenuation;
    [SerializeField]
    private AudioClip rotationBlockedSound;
    [SerializeField]
    private float negativeFeedbackCooldown = 1f;
    private float negativeFeedbackTimer;

    private IBlockableInteractable target;

    public bool Enabled => true;

    private void OnValidate()
    {
        if (attenuation == null)
            attenuation = GetComponent<Attenuation>();
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
    }
    public void OnLevelLoaded()
    {
        OnFixedUpdateSystem.Register(this);
    }

    public void OnAfterLevelLoaded()
    {
    }

    public void OnLevelUnloaded()
    {
        OnFixedUpdateSystem.Unregister(this);

    }

    protected override void Start()
    {
        base.Start();

        target = GetComponent<IBlockableInteractable>();
        Debug.Assert(target != null, "No blockable activator found!");

        if (target != null)
            target.OnTryingToMoveBlockedActivator += OnBlockedActivatorFeedback;
    }

    public void OnFixedUpdate()
    {
        negativeFeedbackTimer += Time.fixedDeltaTime;
    }

    private void OnBlockedActivatorFeedback()
    {
        if (negativeFeedbackTimer >= negativeFeedbackCooldown)
        {
            AudioManager.instance.PlayOneShotSfx(transform.position, rotationBlockedSound, attenuation);
            TryVibrate();
            negativeFeedbackTimer = 0;
        }
    }


}
