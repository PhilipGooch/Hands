using System.Collections;
using UnityEngine;
using NBG.Core;

public class ManagedCoroutinesSample : MonoBehaviour
{
    ICoroutine coInfiniteToBreak;

    private void Awake()
    {
        Coroutines.LingerSeconds = 5.0f;

        this.StartManagedCoroutine(CoInfinite());
        coInfiniteToBreak = this.StartManagedCoroutine(CoInfinite());
    }

    private void Start()
    {
        this.StartManagedCoroutine(CoWillComplete());
        this.StartManagedCoroutine(CoWillThrow());
        this.StartManagedCoroutine(CoTimer1s());
        this.StartManagedCoroutine(CoTimerRealtime1s());
        this.StartManagedCoroutine(CoUnityWaits());
        this.StartManagedCoroutine(CoNestedSimpleWillThrow());
        this.StartManagedCoroutine(CoNestedComplex());
        Coroutines.StartManagedCoroutine(CoTimer1s());
    }

    int frame = 0;
    private void Update()
    {
        if (++frame == 60 * 5)
        {
            coInfiniteToBreak.Stop();
            coInfiniteToBreak = null;
        }
    }

    IEnumerator CoInfinite()
    {
        while (true)
            yield return null;
    }

    IEnumerator CoWillComplete()
    {
        int frames = 0;
        while (++frames < 100)
            yield return null;
    }

    IEnumerator CoWillThrow()
    {
        int frames = 0;
        while (++frames < 50)
            yield return null;
        throw new System.Exception();
    }

    IEnumerator CoTimer1s()
    {
        yield return new WaitForSeconds(1.0f);
    }

    IEnumerator CoTimerRealtime1s()
    {
        yield return new WaitForSecondsRealtime(1.0f);
    }

    IEnumerator CoUnityWaits()
    {
        if (Time.inFixedTimeStep != false)
            throw new System.InvalidOperationException("CoUnityWaits (step 1)");

        yield return new WaitForFixedUpdate();
        if (Time.inFixedTimeStep != true)
            throw new System.InvalidOperationException("CoUnityWaits (step 2)");

        yield return new WaitForEndOfFrame();
        if (Time.inFixedTimeStep != false)
            throw new System.InvalidOperationException("CoUnityWaits (step 3)");

        yield return new WaitForFixedUpdate();
        if (Time.inFixedTimeStep != true)
            throw new System.InvalidOperationException("CoUnityWaits (step 4)");

        yield return new WaitForSeconds(0.1f);
        if (Time.inFixedTimeStep != false)
            throw new System.InvalidOperationException("CoUnityWaits (step 5)");

        var waitForTime = Time.realtimeSinceStartup + 0.5f;
        yield return new WaitWhile(() => { return waitForTime > Time.realtimeSinceStartup; });
        if (waitForTime > Time.realtimeSinceStartup)
            throw new System.InvalidOperationException("CoUnityWaits (step 6)");
    }

    IEnumerator CoNestedSimpleWillThrow()
    {
        var waitForTime = Time.realtimeSinceStartup + 1.0f;

        yield return new WaitForSecondsRealtime(0.5f);
        yield return CoTimer1s();

        if (waitForTime > Time.realtimeSinceStartup)
            throw new System.InvalidOperationException("CoNestedSimple (step 1)");
    }

    IEnumerator CoNestedComplex()
    {
        var waitForTime = Time.realtimeSinceStartup + 1.0f;

        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(CoTimer1s());

        if (waitForTime > Time.realtimeSinceStartup)
            throw new System.InvalidOperationException("CoNestedComplex (step 1)");
    }
}
