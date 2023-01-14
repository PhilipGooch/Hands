using System.Collections.Generic;
using UnityEngine;

namespace NBG.Audio
{
    [CreateAssetMenu(fileName = "CollisionMap", menuName = "[NBG] Audio/Collision Map")]
    public class CollisionMap : ScriptableObject
    {
        public List<CollisionAudioSurfSurfConfig> CollisionConfigs;

        // Search for a collision config that matches the one we are looking for
        public CollisionAudioSurfSurfConfig GetCollisionConfig(SurfaceType surf1, SurfaceType surf2)
        {
            foreach (var item in CollisionConfigs)
            {
                if (item.SurfaceType1 == surf1)
                    if (item.SurfaceType2 == surf2)
                        return item;
            }

            foreach (var item in CollisionConfigs)
            {
                if (item.SurfaceType1 == surf2)
                    if (item.SurfaceType2 == surf1)
                        return item;
            }

            return null;
        }
    }
}
