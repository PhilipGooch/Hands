using System.Collections.Generic;
using NBG.Entities;
using UnityEngine;

namespace Noodles
{
    public interface ICamera
    {
        Entity trackedEntity { get; set; }
        Camera unityCamera { get; }
    }

    public abstract class CameraManagerBase : MonoBehaviour
    {
        public static CameraManagerBase instance;
        public static CameraManagerBase GetOrCreateInstance()
        {
            if (instance == null)
                instance = FindObjectOfType<CameraManagerBase>();
            return instance;
        }
        public abstract void AddCamera(Entity target);
        public abstract void RemoveCamera(Entity target);

    }
    public class CameraManagerBase<TCamera> : CameraManagerBase
        where TCamera : MonoBehaviour, ICamera
    {
        public TCamera cameraPrefab;
        public List<TCamera> cameras = new List<TCamera>();

        public static new CameraManagerBase<TCamera> instance => CameraManagerBase.instance as CameraManagerBase<TCamera>;
        public override void AddCamera(Entity target)
        {
            var camera = cameraPrefab;
            if (cameras.Count > 0 || !camera.gameObject.scene.IsValid())
            {
                camera = Instantiate(cameraPrefab, Vector3.zero, Quaternion.identity);
                camera.transform.SetParent(transform);
            }
            else
            {
                // make a copy if in scene
                cameraPrefab = Instantiate(cameraPrefab);
                cameraPrefab.transform.SetParent(transform);
                cameraPrefab.gameObject.SetActive(false);
            }
            if (!camera.gameObject.activeSelf)
                camera.gameObject.SetActive(true);
            camera.trackedEntity = target;
            //camera.OnCreate();
            cameras.Add(camera);

            SetViewports();
        }

        public override void RemoveCamera(Entity target)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i].trackedEntity == target)
                {
                    //cameras[i].Dispose();
                    Destroy(cameras[i].gameObject);
                    cameras.RemoveAt(i);
                }
            }

            SetViewports();
        }

        bool forceSingleView = false;
        private void SetViewports()
        {
            if (forceSingleView) return;
            switch (cameras.Count)
            {
                case 1:
                    cameras[0].unityCamera.rect = new Rect(0, 0, 1, 1);
                    break;
                case 2:
                    cameras[0].unityCamera.rect = new Rect(0, 0, .5f, 1);
                    cameras[1].unityCamera.rect = new Rect(0.5f, 0, .5f, 1);
                    break;
                case 3:
                    cameras[0].unityCamera.rect = new Rect(0, 0, .5f, 1);
                    cameras[1].unityCamera.rect = new Rect(0.5f, 0, .5f, .5f);
                    cameras[2].unityCamera.rect = new Rect(0.5f, 0.5f, .5f, .5f);
                    break;
                case 4:
                    cameras[0].unityCamera.rect = new Rect(0, 0, .5f, .5f);
                    cameras[1].unityCamera.rect = new Rect(0, .5f, .5f, .5f);
                    cameras[2].unityCamera.rect = new Rect(.5f, 0, .5f, .5f);
                    cameras[3].unityCamera.rect = new Rect(.5f, .5f, .5f, .5f);
                    break;
            }
        }


        //[ExecuteIn(typeof(PhysicsAfterSolve))]
        //public class CameraManagerPostFixedUpdate : SystemBase
        //{
        //    public override void Execute()
        //    {
        //        foreach (var cam in CameraManager.instance.cameras)
        //        {
        //            ref var target = ref EntityStore.GetComponentData<CameraTarget>(cam.trackedEntity);
        //            //cam.CameraTarget.UpdateCurrentPosition(target.position, target.velocity, !target.grounded);
        //        }
        //    }
        //}

   

    }
}

