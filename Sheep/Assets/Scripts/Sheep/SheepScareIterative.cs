using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Recoil;

public static class SheepScareIterative 
{
    static IterativeScareJob job = new IterativeScareJob();
    static NativeArray<SheepScareInfo> sheepInfo;
    static NativeArray<float3> proximityScare;
    static NativeArray<float3> proximityScarePrev;
    static NativeArray<float> proximityThreatLevel;

    public static void ScareAll(IList<Sheep> all)
    {
        EnsureInitialized();
        job.sheepCount = all.Count;
        job.all = sheepInfo;
        job.proximityScare = proximityScare;
        job.proximityScarePrev = proximityScarePrev;
        job.proximityThreatLevel = proximityThreatLevel;
        job.fixedDeltaTime = Time.fixedDeltaTime;
        job.regularThreats = Threat.GetInstance().all;
        job.pokeThreats = Threat.GetInstance().pokeThreats;

        for (int i = 0; i < all.Count; i++)
        {
            sheepInfo[i] = new SheepScareInfo(all[i]);
        }

        job.Schedule().Complete();

        for (int i = 0; i < all.Count; i++)
        {
            sheepInfo[i].WriteData(all[i]);
        }
    }

    static void EnsureInitialized()
    {
        if (!sheepInfo.IsCreated)
        {
            sheepInfo = new NativeArray<SheepScareInfo>(SheepManager.MAX, Allocator.Persistent);
            proximityScare = new NativeArray<float3>(SheepManager.MAX, Allocator.Persistent);
            proximityScarePrev = new NativeArray<float3>(SheepManager.MAX, Allocator.Persistent);
            proximityThreatLevel = new NativeArray<float>(SheepManager.MAX, Allocator.Persistent);
        }
    }

    public static void Dispose()
    {
        if (sheepInfo.IsCreated)
        {
            sheepInfo.Dispose();
            proximityScare.Dispose();
            proximityScarePrev.Dispose();
            proximityThreatLevel.Dispose();
        }
    }

    struct SheepScareInfo
    {
        public float3 scare;
        public float threatLevel;
        public float bravery;
        public float3 relaxHead;
        public float3 relaxTail;
        public float braveryRnd;
        public float stretchRnd;
        public float focusRnd;
        public Sheep.SheepScareTypes sheepScareTypes;

        public SheepScareInfo(Sheep sheep)
        {
            scare = sheep.scare;
            threatLevel = sheep.threatLevel;
            bravery = sheep.bravery;
            relaxHead = sheep.relaxHead;
            relaxTail = sheep.relaxTail;
            braveryRnd = sheep.braveryRnd.value;
            stretchRnd = sheep.stretchRnd.value;
            focusRnd = sheep.focusRnd.value;
            sheepScareTypes = sheep.sheepScareTypes;
        }

        public void WriteData(Sheep sheep)
        {
            var isnan = math.isnan(sheep.scare);
            if (isnan.x || isnan.y || isnan.z)
            {
                sheep.scare = float3.zero;
            }
            else
            {
                sheep.scare = scare;
            }
            sheep.threatLevel = threatLevel;
            sheep.bravery = bravery;
        }

        public bool IsScaredBy(Sheep.SheepScareTypes targetType)
        {
            return (targetType & sheepScareTypes) > 0;
        }
    }

    [BurstCompile]
    struct IterativeScareJob : IJob
    {
        public NativeArray<SheepScareInfo> all;
        public NativeArray<float3> proximityScare;
        public NativeArray<float3> proximityScarePrev;
        public NativeArray<float> proximityThreatLevel;
        public float fixedDeltaTime;
        public int sheepCount;
        public NativeList<BoxBoundThreat> regularThreats;
        public NativeList<BoxBoundThreat> pokeThreats;

        public void Execute()
        {
            for (int i = 0; i < sheepCount; i++)
            {
                var current = all[i];
                current.scare = Threat.CalculateScare(current.relaxHead, current.relaxTail, 0, current.sheepScareTypes, regularThreats, pokeThreats);//, all[i].bravery);
                current.threatLevel = math.length(current.scare);
                all[i] = current;
                proximityScarePrev[i] = float3.zero;
            }

            // change scare to break the line


            for (int i = 0; i < sheepCount; i++)
            {
                var current = all[i];
                current.bravery = re.MoveTowards(current.bravery, .5f * (i + 1) / sheepCount + .5f * (current.braveryRnd - .5f), fixedDeltaTime);
                current.scare = Threat.CalculateScare(current.relaxHead, current.relaxTail, current.bravery, current.sheepScareTypes, regularThreats, pokeThreats);//, all[i].bravery);
                current.threatLevel = math.length(current.scare);
                all[i] = current;
                proximityScarePrev[i] = float3.zero;
            }

            // see if any sheep is pushing other, add scare
            //for (int i = 0; i < all.Count; i++)
            //    PushScare(all[i]);
            //AccumulateProximityScare(all);

            var totalIterations = 5;
            for (int iteration = 0; iteration < totalIterations; iteration++)
            {
                //for (int i = 0; i < all.Count; i++)
                //    all[i].scare += all[i].proximityScare;
                for (int i = 0; i < sheepCount; i++)
                {
                    var current = all[i];
                    if (current.IsScaredBy(Sheep.SheepScareTypes.Herding))
                    {
                        ProximityScare(i, 1f * iteration / (totalIterations - 1));
                    }
                }
                AccumulateProximityScare();
            }
        }

        void AccumulateProximityScare()
        {
            for (int i = 0; i < sheepCount; i++)
            {
                var current = all[i];
                if (current.IsScaredBy(Sheep.SheepScareTypes.Herding))
                {
                    //all[i].scare = Threat.Max(all[i].scare, proximityScare[i]);
                    current.scare = proximityScare[i];
                    current.threatLevel = math.max(current.threatLevel, math.length(current.scare));
                    current.threatLevel = math.max(current.threatLevel, proximityThreatLevel[i]);
                }
                all[i] = current;
            }
        }

        void ProximityScare(int index, float iteration)
        {
            VectorSet stretch = new VectorSet();
            VectorSet focus = new VectorSet();
            var s = all[index];

            proximityThreatLevel[index] = 0;
            var myScare = s.scare + proximityScarePrev[index];
            //for (int i = 0; i < s.neighbors.Count; i++)
            //{
            //    var n = s.neighbors[i];
            for (int i = 0; i < sheepCount; i++)
            {
                if (index == i) continue;
                var n = all[i];

                if (!n.IsScaredBy(Sheep.SheepScareTypes.Herding)) continue;

                var nscare = n.scare;
                var nscaremag = math.length(nscare);
                if (nscaremag < 0.0001f) continue;
                var nscaredir = nscare / nscaremag;
                var offset = (s.relaxHead - n.relaxHead);//.ZeroY();
                var scarestrength = math.max(math.length(myScare), nscaremag);
                //var strength = Mathf.InverseLerp(1.5f*scarestrength, .75f*scarestrength, offset.magnitude);
                var strength = re.InverseLerp(1f * scarestrength, .25f * scarestrength, math.length(offset));

                // max own, neighbor

                var dotfwd = math.dot(offset, nscaredir);

                //// pushed from behind
                if (dotfwd > -.25f)
                {
                    var fwd = re.InverseLerp(-.25f, .25f, dotfwd);
                    proximityThreatLevel[index] = math.max(proximityThreatLevel[index], strength * fwd * n.threatLevel * .9f);
                    stretch.AddSquare(.1f * strength * fwd * math.max(0, nscaremag + n.threatLevel * .5f - .6f) * nscaredir);
                }

                // get scared by the sheep behind (not run, just panic level)
                if (Vector3.Angle(nscaredir, offset) < 30)
                    proximityThreatLevel[index] = math.max(proximityThreatLevel[index], strength * n.threatLevel);


                // focus a bit wider range
                strength = re.InverseLerp(1.5f * scarestrength, .75f * scarestrength, math.length(offset));

                // align with scare direction
                var maxScare = Threat.Max(myScare, nscare);
                var side = re.ProjectOnPlane(offset, math.normalize(maxScare));
                var scareside = re.ProjectOnPlane(myScare, math.normalize(nscare));
                focus.AddSquare(
                    -.05f * strength * side * math.length(maxScare) // group
                    - .25f * strength * scareside * math.length(maxScare) // align
                    );


            }
            //proximityScare[sid] = stretch.max + focus.value;

            // less stretch
            //proximityScare[sid] = 0*.8f* stretch.max *Mathf.Lerp(1f,1.2f,s.stretchRnd.value)+ focus.value*1.25f * Mathf.Lerp(1, 1.2f, s.focusRnd.value);
            proximityScare[index] = s.scare + stretch.max * math.lerp(.5f, 1.5f, s.stretchRnd) + focus.value * math.lerp(.5f, 1.5f, s.focusRnd);
        }
    }
}
