using UnityEngine;

namespace NBG.Automation.RuntimeTests.Controller
{
    public class FrameworkCatchesExceptionsHelper : MonoBehaviour
    {
        public static string ExceptionMessage = "[FrameworkCatchesExceptionsHelper] Helper exception";

        public bool Done { get; private set; }
        int counter = 0;

        void Update()
        {
            ++counter;

            if (counter == 10)
            {
                Done = true;
                enabled = false;
                throw new System.Exception(ExceptionMessage);
            }
        }
    }
}
