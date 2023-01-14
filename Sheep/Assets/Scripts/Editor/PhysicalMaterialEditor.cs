using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PhysicalMaterial))]
public class PhysicalMaterialEditor : Editor
{
    SerializedProperty flammableProperty;
    SerializedProperty timeToIgniteProperty;
    SerializedProperty burnableProperty;
    SerializedProperty timeToBurnProperty;
    SerializedProperty spawnFireParticlesProperty;
    SerializedProperty createFireSourceWhenOnFireProperty;
    SerializedProperty floatingMeshDataProperty;
    SerializedProperty magneticProperty;
    SerializedProperty canBeNailedByHandProperty;
    SerializedProperty canBeAxedProperty;
    SerializedProperty granularProperty;
    SerializedProperty canExtinguishWithMovementProperty;
    SerializedProperty canExtinguishWithWindProperty;
    SerializedProperty velocityToExtinguishFireProperty;
    SerializedProperty canBurnout;
    SerializedProperty timeUntilBurnot;
    SerializedProperty despawnAfterBurnout;
    SerializedProperty darkenColorWhenOnFireProperty;
    SerializedProperty maxColorTintFromFireProperty;
    SerializedProperty timeForFullTintFromFireProperty;
    SerializedProperty transfersElectricCurrentProperty;

    void OnEnable()
    {
        flammableProperty = FindProperty("flammable");
        timeToIgniteProperty = FindProperty("timeToIgnite");
        burnableProperty = FindProperty("burnable");
        timeToBurnProperty = FindProperty("burnDuration");
        spawnFireParticlesProperty = FindProperty("spawnFireParticles");
        createFireSourceWhenOnFireProperty = FindProperty("createFireSourceWhenOnFire");
        floatingMeshDataProperty = FindProperty("floatingMeshData");
        magneticProperty = FindProperty("magnetic");
        canBeNailedByHandProperty = FindProperty("canBeNailedByHand");
        canBeAxedProperty = FindProperty("canBeAxed");
        granularProperty = FindProperty("granular");
        canExtinguishWithMovementProperty = FindProperty("canExtinguishWithMovement");
        canExtinguishWithWindProperty = FindProperty("canExtinguishWithWind");
        velocityToExtinguishFireProperty = FindProperty("velocityToExtinguishFire");
        canBurnout = FindProperty("canBurnout");
        timeUntilBurnot = FindProperty("timeUntilBurnout");
        despawnAfterBurnout = FindProperty("despawnAfterBurnout");
        darkenColorWhenOnFireProperty = FindProperty("darkenColorWhenOnFire");
        maxColorTintFromFireProperty = FindProperty("maxColorTintFromFire");
        timeForFullTintFromFireProperty = FindProperty("timeForFullTintFromFire");
        transfersElectricCurrentProperty = FindProperty("transfersElectricCurrent");
    }

    SerializedProperty FindProperty(string name)
    {
        return serializedObject.FindProperty(name);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawFireValues();
        DrawWaterValues();
        DrawNailValues();
        DrawElectricalProperties();
        DrawMiscProperties();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFireValues()
    {
        using (new NamedIndentation("Fire"))
        {
            flammableProperty.boolValue = EditorGUILayout.Toggle("Flammable", flammableProperty.boolValue);
            if (flammableProperty.boolValue)
            {
                timeToIgniteProperty.floatValue = EditorGUILayout.FloatField("Time to ignite", timeToIgniteProperty.floatValue);
                burnableProperty.boolValue = EditorGUILayout.Toggle("Burnable", burnableProperty.boolValue);
                if (burnableProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    timeToBurnProperty.floatValue = EditorGUILayout.FloatField("Time to burn", timeToBurnProperty.floatValue);
                    EditorGUI.indentLevel--;
                }

                spawnFireParticlesProperty.boolValue = EditorGUILayout.Toggle("Spawn Fire Particles", spawnFireParticlesProperty.boolValue);
                createFireSourceWhenOnFireProperty.boolValue = EditorGUILayout.Toggle("Create Fire Source When On Fire", createFireSourceWhenOnFireProperty.boolValue);

                darkenColorWhenOnFireProperty.boolValue = EditorGUILayout.Toggle("Add dark tint from burn", darkenColorWhenOnFireProperty.boolValue);
                if (darkenColorWhenOnFireProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    maxColorTintFromFireProperty.colorValue = EditorGUILayout.ColorField("Burn tint color", maxColorTintFromFireProperty.colorValue);
                    timeForFullTintFromFireProperty.floatValue = EditorGUILayout.FloatField("Time to get full tint", timeForFullTintFromFireProperty.floatValue);
                    EditorGUI.indentLevel--;
                }

                canExtinguishWithMovementProperty.boolValue = EditorGUILayout.Toggle("Can Extinguish With Movement", canExtinguishWithMovementProperty.boolValue);
                if (canExtinguishWithMovementProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    velocityToExtinguishFireProperty.floatValue = EditorGUILayout.FloatField("Velocity to extinguish fire", velocityToExtinguishFireProperty.floatValue);
                    EditorGUI.indentLevel--;
                }
                canExtinguishWithWindProperty.boolValue = EditorGUILayout.Toggle("Can Extinguish With Wind", canExtinguishWithWindProperty.boolValue);

                canBurnout.boolValue = EditorGUILayout.Toggle("Can Burnout", canBurnout.boolValue);
                if (canBurnout.boolValue)
                {
                    EditorGUI.indentLevel++;
                    timeUntilBurnot.floatValue = EditorGUILayout.FloatField("Time To Self Extinguish", timeUntilBurnot.floatValue);
                    despawnAfterBurnout.boolValue = EditorGUILayout.Toggle("Despawn Object After Burnout", despawnAfterBurnout.boolValue);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    void DrawWaterValues()
    {
        using (new NamedIndentation("Water"))
        {
            EditorGUILayout.PropertyField(floatingMeshDataProperty);
        }
    }

    void DrawNailValues()
    {
        using (new NamedIndentation("Nailing"))
        {
            canBeNailedByHandProperty.boolValue = EditorGUILayout.Toggle("Can be nailed by hand", canBeNailedByHandProperty.boolValue);
            canBeAxedProperty.boolValue = EditorGUILayout.Toggle("Can be axed", canBeAxedProperty.boolValue);
            if (canBeNailedByHandProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                granularProperty.boolValue = EditorGUILayout.Toggle("Is granular (sand/gravel)", granularProperty.boolValue);
                EditorGUI.indentLevel--;
            }
        }
    }

    void DrawElectricalProperties()
    {
        using (new NamedIndentation("Eletricity"))
        {
            transfersElectricCurrentProperty.boolValue = EditorGUILayout.Toggle("Can transfer eletrical current", transfersElectricCurrentProperty.boolValue);

        }
    }

    void DrawMiscProperties()
    {
        using (new NamedIndentation("Misc"))
        {
            magneticProperty.boolValue = EditorGUILayout.Toggle("Magnetic", magneticProperty.boolValue);
        }
    }

    class NamedIndentation : System.IDisposable
    {
        public NamedIndentation(string name)
        {
            EditorGUILayout.LabelField(name);
            EditorGUI.indentLevel++;
        }

        public void Dispose()
        {
            EditorGUI.indentLevel--;
        }
    }
}
