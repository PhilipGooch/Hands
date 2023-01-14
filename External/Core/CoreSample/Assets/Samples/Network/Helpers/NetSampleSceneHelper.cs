using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NBG.Net.Sample
{
    public class NetSampleSceneHelper : MonoBehaviour
    {
        static NetSampleSceneHelper _instance;
        public static NetSampleSceneHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetSampleSceneHelper>();
                }
                return _instance;
            }
        }

        public Text textLog;
        public NetStreamerSample netStreamerSample;
        public Text floatSyncExampleText;

        public Text customEvent1Text;
        public CustomEventsSample customEventSample1;

        public Text customEvent2Text;
        public CustomEventsSample customEventSample2;

        private void OnDestroy()
        {
            _instance = null;
        }

        private void Update()
        {
            floatSyncExampleText.text = netStreamerSample.floatValueToSync.ToString("0.000");
            customEvent1Text.text = customEventSample1.someStateWeWantToSync.ToString();
            customEvent2Text.text = customEventSample2.someStateWeWantToSync.ToString();
        }

        public void ApplyLog(string text)
        {
            textLog.text += text + "\n";
            Debug.Log(text);
        }
    }
}
