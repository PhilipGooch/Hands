using UnityEngine;

namespace NBG.Audio
{
    public class CollisionAudioSourceGizmo : MonoBehaviour
    {
        public string text = "---";
        private AudioSource audioSource;
        private Color gizmoColor;

        private void OnEnable()
        {
            audioSource = GetComponent<AudioSource>();
        }

        void OnDrawGizmos()
        {
            if (audioSource == null) return;
            if (audioSource.outputAudioMixerGroup == AudioRouting.GetChannel(AudioChannel.Footsteps))
            {
                Gizmos.color = new Color(1, 0, 0, (1 + audioSource.volume) * 0.5f);
            }
            else
            {
                Gizmos.color = new Color(0, 0, 1, (1 + audioSource.volume) * 0.5f);
            }
            //DrawString(text, transform.position, Gizmos.color);
            Gizmos.DrawSphere(transform.position, 0.1f);
        }

        //static public void DrawString(string text, Vector3 worldPos, Color? colour = null)
        //{
        //    UnityEditor.Handles.BeginGUI();

        //    var restoreColor = GUI.color;

        //    if (colour.HasValue) GUI.color = colour.Value;
        //    var view = UnityEditor.SceneView.currentDrawingSceneView;
        //    Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);

        //    if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
        //    {
        //        GUI.color = restoreColor;
        //        UnityEditor.Handles.EndGUI();
        //        return;
        //    }

        //    Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
        //    GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
        //    GUI.color = restoreColor;
        //    UnityEditor.Handles.EndGUI();
        //}

        string vol;
        private void Update()
        {
            if (audioSource.clip != null)
            {
                vol = audioSource.volume.ToString();
                if (vol.Length > 4) vol = vol.Substring(0, 4);
                text = audioSource.clip.name + "  " + vol;
            }
        }
    }
}
