using NBG.Core;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recoil
{
    public static class RigidbodyRegistration
    {
        public static void RegisterAll()
        {
            var count = SceneManager.sceneCount;
            for (int i = 0; i < count; ++i)
                Register(SceneManager.GetSceneAt(i));
        }

        public static void UnregisterAll()
        {
            var count = SceneManager.sceneCount;
            for (int i = 0; i < count; ++i)
                Unregister(SceneManager.GetSceneAt(i));
        }



        public static void Register(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                RegisterHierarchy(root);
            }
        }

        public static void Unregister(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                UnregisterHierarchy(root);
            }
        }

        // We use recursive crawling instead of GetComponentsInChildren so that we could opt out of handling rigidbodies that are owned by IHandlesRigidbodies.
        // This also guarantees the correct order of objects.
        public static void RegisterHierarchy(GameObject go) => RecursiveAction(go, RegisterBody);
        public static void UnregisterHierarchy(GameObject go) => RecursiveAction(go, UnregisterBody);

        // Called when IHandlesRigidbodies is not taking over
        static void RegisterBody(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out IHandlesRigidbodies hr))
            {
                // The whole hierarchy is owned by IHandlesRigidbodies, skip.
                hr.OnRegisterRigidbodies();
                return;
            }

            if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                if (ManagedWorld.main.FindBody(rigidbody, true) == World.environmentId)
                {
                    var rb = gameObject.AddComponent<RecoilBodyAssistant>();
                    rb.hideFlags = HideFlags.HideAndDontSave;
                    rb.Register(rigidbody);
                }
            }
        }

        // Called when IHandlesRigidbodies is not taking over
        static void UnregisterBody(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out IHandlesRigidbodies hr))
            {
                // The whole hierarchy is owned by IHandlesRigidbodies, skip.
                AssertHierarchyDoesNotContainRecoilBody(gameObject);
                hr.OnUnregisterRigidbodies();
                return;
            }

            if (gameObject.TryGetComponent<RecoilBodyAssistant>(out RecoilBodyAssistant rb))
            {
                rb.Unregister();
                GameObject.Destroy(rb);
            }
        }

        static void RecursiveAction(GameObject go, Action<GameObject> action)
        {
            action(go);

            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                RecursiveAction(t.gameObject, action);
            }
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        static void AssertHierarchyDoesNotContainRecoilBody(GameObject go)
        {
            Debug.Assert(go.GetComponent<RecoilBodyAssistant>() == null, $"GameObject with {nameof(IHandlesRigidbodies)} is not expected to have a {nameof(RecoilBodyAssistant)}: {go.GetFullPath()}");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var t = go.transform.GetChild(i);
                AssertHierarchyDoesNotContainRecoilBody(t.gameObject);
            }
        }
    }
}
