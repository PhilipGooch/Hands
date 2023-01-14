using UnityEngine;


public class Attenuation : MonoBehaviour
{
    public float volumeDb = 0;
    public float maxDistance = 30;
    public float falloffStart = 1;
    public float falloffPower = .5f;
    public float lpStart = 2;
    public float lpPower = .5f;
    public float spreadNear = .25f;
    public float spreadFar = 0;
    public float spatialNear = 1f;
    public float spatialFar = 1;

    //public void CopyFrom(Attenuation source)
    //{
    //    volumeDb = source.volumeDb;
    //    maxDistance = source.maxDistance;
    //    falloffStart = source.falloffStart;
    //    falloffPower = source.falloffPower;
    //    lpStart = source.lpStart;
    //    lpPower = source.lpPower;
    //    spreadNear = source.spreadNear;
    //    spreadFar = source.spreadFar;
    //    spatialNear = source.spatialNear;
    //    spatialFar = source.spatialFar;
    //}

    //float appliedHash;

    //public float GetHash() // simple hash using prime multipliers
    //{
    //    return volumeDb + maxDistance * 233 + falloffStart * 619 + falloffPower * 1223 + lpStart * 1741 + lpPower * 1741 + spreadNear * 3407 + spreadFar * 3407 + spatialNear * 4391 + spatialFar * 4391;
    //}

    public void Apply(AudioSource source, bool applyVolume=true)
    {
        if (source == null) return;
        //var attenuation = source.GetComponent<Attenuation>();
        //if (attenuation == null)
        //    attenuation = source.gameObject.AddComponent<Attenuation>();
        //var hash = GetHash();
        //if (attenuation.appliedHash == hash) return;

        //attenuation.CopyFrom(this);
        //attenuation.appliedHash = hash;

        source.volume = applyVolume ? DBToValue(volumeDb) : 1;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Custom;
        //source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CalculateAttenuation.VolumeFalloff(falloffStart / source.maxDistance, falloffPower));
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Attenuation.VolumeFalloffFromTo(1, 0, falloffStart, source.maxDistance, falloffPower));
        //source.SetCustomCurve(AudioSourceCurveType.Spread, Attenuation.VolumeFalloffFromTo(spreadNear / 2, spreadFar / 2, falloffStart, source.maxDistance, falloffPower));
        //source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, Attenuation.VolumeFalloffFromTo(spatialNear, spatialNear == 0 ? 0 : 1, falloffStart, source.maxDistance, falloffPower));
        source.spatialBlend = 1;
        // spread 0->directional
        // spread 0.5->nondirectional
        //source.SetCustomCurve(AudioSourceCurveType.Spread, CalculateAttenuation.Spread((1-spreadNear)/2, (1-spreadFar)/2, falloffStart / source.maxDistance, falloffPower));
        ////source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, CalculateAttenuation.Spread(spatialNear, spatialFar, falloffStart / source.maxDistance, falloffPower));
        //source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, CalculateAttenuation.Spread(spatialNear, spatialNear == 0 ? 0 : 1, falloffStart / source.maxDistance, falloffPower));

        var lowPass = source.GetComponent<AudioLowPassFilter>();
        if (lowPass != null)
        {
            lowPass.customCutoffCurve = Attenuation.LowPassFalloff(lpStart / maxDistance, lpPower);
        }
    }
    public static float DBToValue(float decibel, float zeroVolumeDB = -120)
    {
        if (decibel <= zeroVolumeDB)
            return 0;
        var logvalue = decibel / 20;
        var value = Mathf.Pow(10, logvalue);
        return value;
    }


    public static AnimationCurve VolumeFalloffFromTo(float near, float far, float falloffStart, float falloff48db, float falloffFocus)
    {


        var curve = new AnimationCurve();
        curve.AddKey(KeyframeUtil.GetNew(0, near, TangentMode.Linear));
        //if (falloffStart > 29.9f && falloffFocus > 0.99f)
        //{
        //    curve.UpdateAllLinearTangents();
        //    return curve;

        //}
        if (falloff48db <= falloffStart)
            curve.AddKey(KeyframeUtil.GetNew(1, near, TangentMode.Linear));
        else if (falloffFocus == 1)
        {
            var pFalloff = falloffStart / (falloffStart + falloff48db);

            var vol = 1f;
            for (int i = 0; i < 16; i++)
            {
                var pCur = pFalloff + (1 - pFalloff) * i / 16;
                curve.AddKey(KeyframeUtil.GetNew(pCur, Mathf.Lerp(far, near, vol) /* Mathf.InverseLerp(1, pFalloff,pCur + pEmit)*/, TangentMode.Linear));
                //pCur *= Mathf.Sqrt(B);
                //pFalloff *= distMultiplierToHalve;
                vol /= Mathf.Sqrt(2);
            }
            curve.AddKey(KeyframeUtil.GetNew(1, far, TangentMode.Linear));
        }
        else
        {


            //var B = 1.05f; //(1->2)
            var B = Mathf.Pow(2, 1 - falloffFocus);

            var pFalloff = falloffStart / (falloffStart + falloff48db);
            var pEmit = (1 - Mathf.Pow(B, 8) * pFalloff) / (1 - Mathf.Pow(B, 8));
            var pCur = pFalloff - pEmit;
            var vol = 1f;
            for (int i = 0; i < 16; i++)
            {
                curve.AddKey(KeyframeUtil.GetNew(pEmit + pCur, Mathf.Lerp(far, near, vol) /* Mathf.InverseLerp(1, pFalloff,pCur + pEmit)*/, TangentMode.Linear));
                pCur *= Mathf.Sqrt(B);
                //pFalloff *= distMultiplierToHalve;
                vol /= Mathf.Sqrt(2);
            }


            /*


            var distMultiplierToHalve = Mathf.Pow(1 / pFalloff, 1f/8);
            var vol = 1f;
            for (int i = 0; i < 8; i++)
            {
                curve.AddKey(KeyframeUtil.GetNew(pFalloff, vol, TangentMode.Linear));
                pFalloff *= distMultiplierToHalve;
                vol /= 2;
            }*/
            curve.AddKey(KeyframeUtil.GetNew(1, far, TangentMode.Linear));
        }

        /*
        if (falloffPoint >= 1 || falloffPower == 1)
            curve.AddKey(KeyframeUtil.GetNew(1, 1, TangentMode.Linear));
        else
        {

            for (float i = 0; i < 10; i += 0.5f)
            {
                var dist = falloffPoint * Mathf.Pow(2, i);
                if (dist > 1) break;
                var volume = Mathf.Pow(falloffPower, i) // inverse square
                    * Mathf.InverseLerp(1, falloffPoint, dist); // linear
                curve.AddKey(KeyframeUtil.GetNew(dist, volume, TangentMode.Linear));
            }

            curve.AddKey(KeyframeUtil.GetNew(1, 0, TangentMode.Linear));
        }*/
        curve.UpdateAllLinearTangents();
        return curve;
    }
    public static AnimationCurve VolumeFalloff(float falloffPoint, float falloffPower)
    {

        var curve = new AnimationCurve();
        curve.AddKey(KeyframeUtil.GetNew(0, 1, TangentMode.Linear));
        if (falloffPoint >= 1 || falloffPower == 1)
            curve.AddKey(KeyframeUtil.GetNew(1, 1, TangentMode.Linear));
        else
        {

            for (float i = 0; i < 10; i += 0.5f)
            {
                var dist = falloffPoint * Mathf.Pow(2, i);
                if (dist > 1) break;
                var volume = Mathf.Pow(falloffPower, i) // inverse square
                    * Mathf.InverseLerp(1, falloffPoint, dist); // linear
                curve.AddKey(KeyframeUtil.GetNew(dist, volume, TangentMode.Linear));
            }

            curve.AddKey(KeyframeUtil.GetNew(1, 0, TangentMode.Linear));
        }
        curve.UpdateAllLinearTangents();
        return curve;
    }
    public static AnimationCurve LowPassFalloff(float falloffPoint, float falloffPower)
    {
        //return null;
        var curve = new AnimationCurve();
        curve.AddKey(KeyframeUtil.GetNew(0, 1, TangentMode.Linear));
        if (falloffPoint > 29.9f && falloffPower > 0.99f)
        {
            curve.UpdateAllLinearTangents();
            return curve;

        }

        if (falloffPoint >= 1 || falloffPower == 1)
            curve.AddKey(KeyframeUtil.GetNew(1, 1, TangentMode.Linear));
        else
        {
            var max = 1 / falloffPoint;
            for (float i = 0; i < 10; i += 0.5f)
            {
                var dist = falloffPoint * Mathf.Pow(2, i);
                if (dist > 1) break;
                var volume = Mathf.Pow(falloffPower, i);
                curve.AddKey(KeyframeUtil.GetNew(dist, volume, TangentMode.Linear));
            }
        }
        curve.UpdateAllLinearTangents();
        return curve;
    }
    public static AnimationCurve Spread(float near, float far, float falloffPoint, float falloffPower)
    {

        var curve = new AnimationCurve();
        curve.AddKey(KeyframeUtil.GetNew(0, near, TangentMode.Linear));
        if (falloffPoint >= 1 || falloffPower == 1)
            curve.AddKey(KeyframeUtil.GetNew(1, near, TangentMode.Linear));
        else
        {
            var max = 1 / falloffPoint;
            for (float i = 0; i < 10; i += 0.5f)
            {
                var dist = falloffPoint * Mathf.Pow(2, i);
                if (dist >= 1) break;
                var volume = Mathf.Lerp(far, near, Mathf.Pow(falloffPower, i));
                curve.AddKey(KeyframeUtil.GetNew(dist, volume, TangentMode.Linear));
            }
            curve.AddKey(KeyframeUtil.GetNew(1, far, TangentMode.Linear));
        }
        curve.UpdateAllLinearTangents();
        return curve;
    }



}
