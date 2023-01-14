using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class SettingsPopup
    {
        const int width = 400;

        const string uxmlGUID = "eb8eebbeb1588764c8ab63af4727dd08";
        VisualElement component;
        public SettingsPopup(VisualElement root, Action<int> graphHeightSet, Action<int> msLineHeightSet, Action<int> columnWidthSet, Action<int> msCapSet)
        {
            VisualTreeAsset visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGUID));
            visualTreeAsset.CloneTree(root);
            component = root.Q<Box>("settingsPopup");
            root.Add(component);
            component.BringToFront();
            component.visible = false;

            if (EditorGUIUtility.isProSkin) //dark mode
            {
                component.AddToClassList("dark-background");
            }
            else //light mode
            {
                component.AddToClassList("light-background");
            }

            component.style.position = Position.Absolute;
            component.style.width = width;

            Button closeBtn = component.Q<Button>("closeBtn");
            closeBtn.clickable.clicked += Hide;
            closeBtn.style.width = 20;
            closeBtn.style.left = width - 30;

            SetupGraphHeightSelection(graphHeightSet);
            SetupMsLineHeightSelection(msLineHeightSet);
            SetupColumnWidthSelection(columnWidthSet);
            SetupMsCapSelection(msCapSet);
        }
        
        void SetupGraphHeightSelection(Action<int> graphHeightSet)
        {
            
            SliderInt  graphHeight = component.Q<SliderInt>("graphsHeighSelection");
            graphHeight.SetValueWithoutNotify(DataMinerEditorPrefs.GetGraphsHeight());

            graphHeight.RegisterValueChangedCallback<int>((evt) =>
            {
                DataMinerEditorPrefs.SaveGraphsHeight(evt.newValue);
                graphHeightSet(evt.newValue);
            });
        }
        void SetupColumnWidthSelection(Action<int> columnWidthSet)
        {
            SliderInt columnWidth = component.Q<SliderInt>("columnWidthSelection");

            columnWidth.SetValueWithoutNotify(DataMinerEditorPrefs.GetColumnWidth());
            columnWidth.RegisterValueChangedCallback<int>((evt) =>
            {

                DataMinerEditorPrefs.SaveColumnWidth(evt.newValue);
                columnWidthSet(evt.newValue);
            });
        }
        void SetupMsLineHeightSelection(Action<int> msLineHeightSet)
        {
            IntegerField msLineHeight = component.Q<IntegerField>("msLineHeight");
            msLineHeight.SetValueWithoutNotify(DataMinerEditorPrefs.GetMsLineHeight());

            msLineHeight.RegisterValueChangedCallback<int>((evt) =>
            {
                int value = evt.newValue;
                value = Mathf.Clamp(value, 1, 1000);

                msLineHeight.SetValueWithoutNotify(value);
                DataMinerEditorPrefs.SaveMsLineHeight(value);

                msLineHeightSet(value);
            });
        }
        void SetupMsCapSelection(Action<int> msCapSet)
        {
            IntegerField msCap = component.Q<IntegerField>("msCap");

            msCap.SetValueWithoutNotify(DataMinerEditorPrefs.GetMsCap());

            msCap.RegisterValueChangedCallback<int>((evt) =>
            {
                int value = evt.newValue;
                value = Mathf.Clamp(value, 1, 1000);

                msCap.SetValueWithoutNotify(value);
                DataMinerEditorPrefs.SaveMsCap(value);

                msCapSet(value);
            });

        }
        public void Hide()
        {
            component.visible = false;
        }

        public void Show()
        {
            component.visible = true;
        }
    }
}
