using NBG.LogicGraph;
using System.Collections;
using UnityEngine;

public class PlayParticleEffect : MonoBehaviour
{
    public ParticleSystem Target;

    public bool PlayOnlyOnce;
    public bool DontPlayWhilePlaying;
    private bool playing = false;

    [NodeAPI("TryPlay")]
    public void TryPlay()
    {
        if (!playing)
        {
            Target.Play();
            playing = true;
            if (!PlayOnlyOnce)
            {
                if (DontPlayWhilePlaying)
                {
                    StartCoroutine(WaitTillFinished());
                }
                else
                {
                    playing = false;
                }
            }
        }
    }

    IEnumerator WaitTillFinished()
    {
        yield return new WaitForSeconds(Target.main.duration);
        playing = false;
    }
}

