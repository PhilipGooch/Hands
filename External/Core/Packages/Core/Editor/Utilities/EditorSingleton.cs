using UnityEngine;

namespace NBG.Core.Editor
{
    // Editor singleton which survives domain reloads.
    // Note that callbacks need to be reregistered.
    public abstract class EditorSingleton<T> : ScriptableObject where T : EditorSingleton<T>
    {
        static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var objs = Resources.FindObjectsOfTypeAll<T>();
                    Debug.Assert(objs.Length <= 1);
                    if (objs.Length == 0)
                        _instance = ScriptableObject.CreateInstance<T>();
                    else
                        _instance = objs[0];
                }
                return _instance;
            }
        }

        public virtual void Awake()
        {
            Debug.Assert(_instance == null);
            _instance = (T)this;
            _instance.hideFlags = HideFlags.DontSave;
        }
    }
}
