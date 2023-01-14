using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    public interface IGrabbable 
    {
        void OnGrab(Entity noodle, NoodleHand hand);
        void OnRelease(Entity noodle, NoodleHand hand);
    }
}
