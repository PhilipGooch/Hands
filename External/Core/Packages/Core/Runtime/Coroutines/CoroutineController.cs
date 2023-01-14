using UnityEngine;

namespace NBG.Core
{
    /// <summary>
    /// Updates the coroutine system and hosts global coroutines.
    /// This should not become a system because then coroutine hosting wouldn't be possible.
    /// </summary>
    internal class CoroutineController : MonoBehaviour
    {
        private void Awake()
        {
            GameObject.DontDestroyOnLoad(this.gameObject);
        }

        private void Update()
        {
            Coroutines.Update();
        }
    }
}
