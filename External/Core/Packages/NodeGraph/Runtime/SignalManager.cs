using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.NodeGraph
{
    public class SignalManager : MonoBehaviour
    {
        public static bool skipTransitions = true;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }

        public static void LevelLoaded()
        {
            BeginReset();
            EndReset();
        }

        static void BeginReset()
        {
            skipTransitions = true;
        }

        static void EndReset()
        {
            for (int i = 0; i < Node.all.Count; i++)
                Node.all[i].ResetOutputs();
            for (int i = 0; i < Node.all.Count; i++)
                Node.all[i].ResetInputs();
            for (int i = 0; i < Node.all.Count; i++)
                Node.all[i].SetDirty();
            Instance.Update();
            skipTransitions = false;
        }

        ///seems like a warning bug
#pragma warning disable CS0649
        static SignalManager instance;
#pragma warning restore CS0649

        static SignalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SignalManager", typeof(SignalManager));
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        static Queue<Node> dirtyNodes = new Queue<Node>();

        public static void AddDirtyNode(Node node)
        {
            dirtyNodes.Enqueue(node);
            ProcessQueues();
        }

        private void Update()
        {
            ProcessQueues();
        }

        static bool inProcess = false;
        static void ProcessQueues()
        {
            if (inProcess) return;
            inProcess = true;

            int sanityCheck = 1000;
            while (dirtyNodes.Count > 0)
            {
                if (sanityCheck-- <= 0)
                    throw new System.Exception("Infinite loop in Signal chain");

                var s = dirtyNodes.Dequeue();
                if (s != null) // check for destroy
                {
                    if (s.isDirty)
                    {
                        s.isDirty = false;
                        try
                        {
                            s.Process();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e, s.gameObject);
                            continue;
                        }

                    }
                }
            }
            inProcess = false;
        }
    }
}
