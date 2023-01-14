using NBG.LogicGraph;
using UnityEngine;

namespace Sample.LogicGraph
{
    public static class TransformLGExtensions
    {
        [NodeAPI("Get Position")]
        public static Vector3 GetPosition(this Transform self)
        {
            return self.position;
        }
    }
}
