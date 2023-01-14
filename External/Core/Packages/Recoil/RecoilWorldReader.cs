//#define ENABLE_RECOIL_EVENT_DEBUGGING
using NBG.Core;
using NBG.Core.GameSystems;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recoil
{
    [ScriptExecutionOrder(-32768)]
    public class RecoilWorldReader : MonoBehaviour
    {
        private new Collider collider;
        private ICoroutine syncCoroutine;
        private Scene defaultScene;
        
        public static Vector3 DefaultWorldPos = new Vector3(3333, 3333, 3333);

        public static RecoilWorldReader Create(string name)
        {
            var go = new GameObject($"NBG_RECOIL_WORLDREADER - {name}");
            go.transform.position = DefaultWorldPos;
            DontDestroyOnLoad(go);
            go.hideFlags |= HideFlags.NotEditable;

            var rigid = new GameObject("rigid");
            rigid.transform.SetParent(go.transform, false);
            rigid.hideFlags |= HideFlags.NotEditable;
            rigid.AddComponent<Rigidbody>().isKinematic = true;
            rigid.AddComponent<SphereCollider>();

            var reader = go.AddComponent<RecoilWorldReader>();
            reader.Init();
            return reader;
        }

        private void Init()
        {
            defaultScene = gameObject.scene;

            collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            
            StartSync();
        }

        public static void Destroy(RecoilWorldReader reader)
        {
            if (reader == null)
                return;

            reader.StopSync();
            DestroyImmediate(reader.gameObject);
        }

        public void StartSync()
        {
            StopSync();
            syncCoroutine = this.StartManagedCoroutine(SyncCoroutine());
        }

        public void StopSync()
        {
            if (syncCoroutine != null && syncCoroutine.Status == CoroutineStatus.Running)
            {
                syncCoroutine.Stop();
            }
        }

        public void MoveToScene(Scene newScene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, newScene);
            StartSync();
        }

        public void MoveToDefaultScene()
        {
            SceneManager.MoveGameObjectToScene(gameObject, defaultScene);
            StartSync();
        }

        private IEnumerator SyncCoroutine()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();
#if ENABLE_RECOIL_EVENT_DEBUGGING
            Debug.Log($"RecoilWorldReader WaitForFixedUpdate {Time.frameCount}");
#endif
                collider.enabled = false;
                collider.enabled = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
#if ENABLE_RECOIL_EVENT_DEBUGGING
            Debug.Log($"RecoilWorldReader Trigger {Time.frameCount}");
#endif
            if (GameSystemWorldDefault.Instance != null)
            {
                var rs = GameSystemWorldDefault.Instance.GetExistingSystem<ReadState>();
                if (rs != null)
                {
                    rs.Update();
                    rs.LastJobHandle.Complete();
                }
            }
        }
    }
}
