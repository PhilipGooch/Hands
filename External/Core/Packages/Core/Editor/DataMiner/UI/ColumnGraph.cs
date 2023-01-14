using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class ColumnGraph : GraphComponentBase
    {
        VisualElement msLine;
        TextElement msLineText;
        VisualElement selectionIndicator;

        DataID selectedDataID;

        int msLineHeight;
        int msCap;
        const string uxmlGUID = "4170ec9c530c05c468c5cccb0a10d637";

        DatasetTimer activeDataset;

        public ColumnGraph(VisualElement root, int columnCount, Action<DataMinerWindowComponent> onRemove)
        {
            msLineHeight = DataMinerEditorPrefs.GetMsLineHeight();
            msCap = DataMinerEditorPrefs.GetMsCap();

            Setup(root, columnCount, onRemove, uxmlGUID);
            Repaint();

            msLine = component.Q<VisualElement>("msLine");
            msLine.PlaceInFront(selectionColumns);
            msLineText = msLine.Q<TextElement>("msLineText");
            msLineText.text = msLineHeight + " ms";


            onSelectionChange += SelectionChanged;
        }

        protected override void Reset()
        {
            graphClickableColumns.Clear();
            selectionColumns.Clear();
        }

        protected override void SetupDatasetSelection()
        {
            selectDataset = component.Q<ToolbarMenu>("selectDataset");

            if (activeDataset == null)
            {
                selectedDataID = currentSlice.timerData.Keys.First();
                activeDataset = currentSlice.timerData[selectedDataID];
                selectDataset.text = activeDataset.name;
            }

            foreach (var item in currentSlice.timerData)
            {
                selectDataset.menu.AppendAction(item.Key.name, a => { SelectNewDataset(item.Key); }, a => DropdownMenuAction.Status.Normal);
            }

        }

        void SelectNewDataset(DataID dataID)
        {
            selectedDataID = dataID;
            activeDataset = currentSlice.timerData[dataID];

            selectDataset.text = dataID.name;
            if (currentSlice != null)
                AfterLayoutInitUpdate();
        }

        protected override void UpdateSelectedDataset()
        {
            activeDataset = currentSlice.timerData[selectedDataID];
        }

        public void SetMsLineHeight(int newHeight)
        {
            msLineHeight = newHeight;
            msLineText.text = newHeight + " ms";

            Update();
        }

        public void SetMsCap(int newCap)
        {
            msCap = newCap;

            Update();
        }

        void SelectionChanged(int oldS, int newS)
        {
            if (selectionIndicator != null)
            {
                if (ColumnExists(oldS))
                    selectionIndicator.style.backgroundColor = GetColorFromAverage(oldS);
                selectionIndicator = null;
            }
            if (newS != -1 && graphClickableColumns.Count > newS)
            {
                selectionIndicator = GetGraphColumn(newS);
                selectionIndicator.style.backgroundColor = (Color)new Color32(131, 171, 211, 255);
            }
        }

        Color GetColorFromAverage(int columnID)
        {
            return activeDataset.columns[columnID].max < msLineHeight - 1 ?
                    Color.green :
                activeDataset.columns[columnID].max > msLineHeight + 1 ?
                    Color.red :
                    Color.yellow;
        }

        protected override void AfterLayoutInitUpdate()
        {
            float max = Mathf.Max(msCap, msLineHeight + 2);

            float h = graphBox.contentRect.height;

            float columnWidth = DataMinerEditorPrefs.GetColumnWidth();
            UpdateTooltip();

            for (int i = 0; i < dataPointsCount; i++)
            {
                float height = activeDataset.columns[i].withinRecordingRange ? Mathf.Max((Mathf.Clamp(activeDataset.columns[i].max, 1, max) * h) / max, 3) : 1;

                VisualElement column = GetGraphColumn(i, "graph-column-border");
                column.visible = true;

                column.style.left = i * columnWidth;
                column.style.width = columnWidth;
                column.style.height = height;
                //column.style.borderTopWidth = h - height; //TODO

                if (column != selectionIndicator)
                    column.style.backgroundColor = GetColorFromAverage(i);

                if (columnWidth <= 3)
                {
                    column.style.borderLeftWidth = 0;
                    column.style.borderRightWidth = 0;
                }
                else
                {
                    column.style.borderLeftWidth = 1;
                    column.style.borderRightWidth = 1;
                }
            }

            msLine.style.bottom = (msLineHeight * h) / max;

            if (ActiveColumnSelection != -1)
                SelectionChanged(ActiveColumnSelection, ActiveColumnSelection);


            for (int i = dataPointsCount; i < graphClickableColumns.Count; i++)
            {
                graphClickableColumns[i].visible = false;
            }
        }

        protected bool ColumnExists(int toCheck)
        {
            return !activeDataset.Equals(default(DataID)) && activeDataset.columns.Length > toCheck;
        }

        public override void UpdateTooltip()
        {
            if (hoveredColumn != -1 && ColumnExists(hoveredColumn))
            {
                StringBuilder sb = new StringBuilder();

                ColumnTimer selected = activeDataset.columns[hoveredColumn];

                int totalSelected = selected.framesTo - selected.framesFrom;

                if (totalSelected > 1)
                {
                    sb.AppendLine($"Frames {selected.framesFrom} to {selected.framesTo} count {totalSelected}");
                    sb.Append($"Max { selected.max} ms @ frame { selected.maxAt}");

                }
                else
                {
                    sb.AppendLine($"Frame {selected.framesFrom} count {totalSelected}");
                    sb.Append($"Max {selected.max} ms");

                }

                tooltip.Update(sb.ToString());

            }
        }

        protected override Column GetColumn(int id)
        {
            if (ColumnExists(id))
                return activeDataset.columns[id];
            else
                return null;
        }

        protected override List<DataID> GetSelectedDataIDs()
        {
            return new List<DataID>() { selectedDataID };
        }

        protected override bool IsSliceViable()
        {
            if (currentSlice.timerData.Count == 0)
                return false;

            return true;
        }  
    }
}
