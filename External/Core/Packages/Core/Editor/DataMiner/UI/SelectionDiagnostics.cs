using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class SelectionDiagnostics : DataMinerWindowComponent
    {
        const string uxmlGUID = "98d532789f2bcdf45b4a926f223c4898";

        Dictionary<IFrameSelectHandler, string> selectedDatasets;

        VisualElement datasetSelection;
        public SelectionDiagnostics(VisualElement root, Action<DataMinerWindowComponent> onRemove)
        {
            this.root = root;

            visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGUID));
            component = visualTreeAsset.Instantiate();

            datasetSelection = component.Q<VisualElement>("toggleDatasets");

            selectedDatasets = new Dictionary<IFrameSelectHandler, string>();

            AddToWindow();
            SetupDeleteButton(onRemove);
            Repaint();

        }
        protected override void AfterLayoutInitUpdate() { }

        protected override bool IsSliceViable()
        {
            return true;
        }

        protected override void Reset()
        {
            datasetSelection.Clear();

        }

        protected override void SetupDatasetSelection()
        {
            datasetSelection.Clear();
            foreach (var item in currentSlice.allSelectionHandlers)
            {
                CheckboxReverse checkboxReverse = new CheckboxReverse(item.Value, datasetSelection);

                if (selectedDatasets.ContainsKey(item.Key))
                    checkboxReverse.toggle.SetValueWithoutNotify(true);
               
                checkboxReverse.toggle.RegisterValueChangedCallback<bool>((evt) =>
                {
                    if (evt.newValue == true)
                    {
                        selectedDatasets.Add(item.Key, item.Value);
                        currentSlice.selectedSelectionHandlers.Add(item.Key, item.Value);
                    }
                    else
                    {
                        selectedDatasets.Remove(item.Key);
                        currentSlice.selectedSelectionHandlers.Remove(item.Key);

                        item.Key.OnReset();
                    }
                });

            }

        }

        protected override void UpdateSelectedDataset()
        {
            foreach (var item in currentSlice.allSelectionHandlers)
            {
                if (!selectedDatasets.ContainsKey(item.Key))
                    currentSlice.selectedSelectionHandlers.Remove(item.Key);

            }
        }


    }
}
