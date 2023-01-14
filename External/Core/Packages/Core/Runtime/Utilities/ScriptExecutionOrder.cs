using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Core
{
    public class ScriptExecutionOrder : Attribute
    {
        public int order;
        public ScriptExecutionOrder(int order)
        {
            this.order = order;
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class ScriptExecutionOrderManager
    {
        static ScriptExecutionOrderManager()
        {
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() != null)
                {
                    foreach (var attr in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(ScriptExecutionOrder)))
                    {
                        var newOrder = ((ScriptExecutionOrder)attr).order;
                        if (MonoImporter.GetExecutionOrder(monoScript) != newOrder)
                            MonoImporter.SetExecutionOrder(monoScript, newOrder);
                    }
                }
            }
        }
    }
#endif
}
