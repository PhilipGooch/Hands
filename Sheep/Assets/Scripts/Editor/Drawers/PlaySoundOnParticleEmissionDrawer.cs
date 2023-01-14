using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PlaySoundOnParticleEmissionData))]
public class PlaySoundOnParticleEmissionDrawer : PropertyDrawer
{

    bool foldoutSounds = false;
    bool foldoutIntervals = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        foldoutSounds = EditorGUILayout.Foldout(foldoutSounds, "Sounds");
        if (foldoutSounds)
        {
            EditorGUI.indentLevel++;

            AddProperty(property, "onParticlesStartSounds");
            AddProperty(property, "onParticlesEndSounds");
            AddProperty(property, "onParticlesIncreaseSounds");
            AddProperty(property, "onParticlesDecreaseSounds");

            EditorGUI.indentLevel--;
        }

        foldoutIntervals = EditorGUILayout.Foldout(foldoutIntervals, "Min sound play intervals");
        if (foldoutIntervals)
        {
            EditorGUI.indentLevel++;

            AddProperty(property, "minParticlesStartSoundInterval");
            AddProperty(property, "minParticlesEndSoundInterval");
            AddProperty(property, "minParticlesIncreaseSoundInterval");
            AddProperty(property, "minParticlesDecreaseSoundInterval");

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    void AddProperty(SerializedProperty baseProperty, string propertyName)
    {
        EditorGUILayout.PropertyField(baseProperty.FindPropertyRelative(propertyName), true);
    }

}
