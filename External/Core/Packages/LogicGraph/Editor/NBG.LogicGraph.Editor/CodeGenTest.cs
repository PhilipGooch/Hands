using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NBG.LogicGraph
{
    static class CodeGenTest
    {
        [NodeAPI("CodeGenTest")]
        public static void CodeGenTestTarget()
        {
        }

        [InitializeOnLoadMethod]
        static void Check()
        {
            var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public;
            var mi = typeof(CodeGenTest).GetMethod($"{UserlandBindings.Prefix}CALL_{nameof(CodeGenTest.CodeGenTestTarget)}", flags);
            if (mi != null)
                return;

            Debug.LogError("CodeGenTest check failed: bindings were not generated!");
        }
    }
}
