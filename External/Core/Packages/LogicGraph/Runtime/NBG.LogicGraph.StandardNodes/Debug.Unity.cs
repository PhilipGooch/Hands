using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Debug")]
    public static class DebugUnity
    {
        [NodeAPI("Log")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [NodeAPI("LogWarning")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        [NodeAPI("LogError")]
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        [NodeAPI("Break")]
        public static void Break()
        {
            Debug.Break();
        }
    }
}
