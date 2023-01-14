using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class LineGraph : GraphComponentBase
    {
        List<VisualElement> gridLines;
        List<IMGUIContainer> lineGraphs;

        const string uxmlGUID = "046b87b41aa11644a92b115f8d7c6b3f";

        VisualElement selectionIndicator;

        VisualElement gridLinesParent;
        VisualElement graphLinesParent;
        Box toggleDatasets;

        Dictionary<DataID, DatasetCounters> activeDatasets;

        List<Color32> graphsColors = new List<Color32>() {
            new Color32(0, 221, 255, 255),
            new Color32(161, 58,45, 255),
            new Color32(87, 45, 161, 255),
            new Color32(161, 95, 45, 255),

        };

        public LineGraph(VisualElement root, int pointsCount, Action<DataMinerWindowComponent> onRemove)
        {
            gridLines = new List<VisualElement>();
            lineGraphs = new List<IMGUIContainer>();

            Setup(root, pointsCount, onRemove, uxmlGUID);

            gridLinesParent = component.Q<VisualElement>("gridLines");
            graphLinesParent = component.Q<VisualElement>("graphLines");
            toggleDatasets = component.Q<Box>("toggleDatasets");

            Repaint();

            onSelectionChange += SelectionChanged;
        }

        protected override void SetupDatasetSelection()
        {
            toggleDatasets.Clear();

            if (activeDatasets == null)
            {
                activeDatasets = new Dictionary<DataID, DatasetCounters>();
                foreach (var item in currentSlice.counterData)
                {
                    activeDatasets.Add(item.Key, item.Value);
                }
            }

            foreach (var item in currentSlice.counterData)
            {
                CheckboxReverse checkboxReverse = new CheckboxReverse(item.Key.name, toggleDatasets);
                checkboxReverse.label.style.color = GetGraphColor(GetDatasetID(item.Key));
                if (activeDatasets.ContainsKey(item.Key))
                    checkboxReverse.toggle.SetValueWithoutNotify(true);

                checkboxReverse.toggle.RegisterValueChangedCallback<bool>((evt) => { SelectNewDataset(evt.newValue, item.Key); });
            }
        }


        void SelectNewDataset(bool state, DataID dataID)
        {
            if (!state)
            {
                activeDatasets.Remove(dataID);
            }
            else
            {
                activeDatasets.Add(dataID, currentSlice.counterData[dataID]);

            }

            if (currentSlice != null)
                AfterLayoutInitUpdate();
        }
        int GetDatasetID(DataID dataID)
        {
            int id = 0;
            foreach (var item in currentSlice.counterData)
            {
                if (item.Key.Equals(dataID))
                    return id;

                id++;
            }

            return id;
        }
        Color GetGraphColor(int id)
        {
            return graphsColors[id % graphsColors.Count];

        }
        protected override void UpdateSelectedDataset()
        {
            foreach (var item in currentSlice.counterData)
            {
                if (activeDatasets.ContainsKey(item.Key))
                    activeDatasets[item.Key] = currentSlice.counterData[item.Key];

            }
        }

        VisualElement GetGridLine(int id)
        {

            if (gridLines.Count > id)
                return gridLines[id];

            //create new
            VisualElement line = new VisualElement();
            line.style.bottom = 0;
            line.AddToClassList("graph-grid-lines");
            line.pickingMode = PickingMode.Ignore;

            if (EditorGUIUtility.isProSkin) //dark mode
            {
                line.AddToClassList("graph-dark-grid-lines");
            }
            else //light mode
            {
                line.AddToClassList("graph-light-grid-lines");
            }

            gridLines.Add(line);
            gridLinesParent.Add(line);

            return line;

        }

        IMGUIContainer GetLineGraph(int id)
        {
            if (lineGraphs.Count > id)
                return lineGraphs[id];

            IMGUIContainer lines = new IMGUIContainer();
            lines.AddToClassList("module-viewport");
            lineGraphs.Add(lines);
            graphLinesParent.Add(lines);

            return lines;
        }

        void SelectionChanged(int oldS, int newS)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.RemoveFromClassList("graph-grid-lines-selected");
                selectionIndicator = null;
            }
            if (newS != -1 && gridLines.Count > newS)
            {
                selectionIndicator = gridLines[newS];
                selectionIndicator.AddToClassList("graph-grid-lines-selected");
            }
        }

        protected override void AfterLayoutInitUpdate()
        {
            float max = 0;
            float min = 0;

            float w = graphBox.contentRect.width;
            float h = graphBox.contentRect.height;
            float bot = graphBox.resolvedStyle.paddingBottom;

            gridLinesParent.style.bottom = bot;
            graphLinesParent.style.bottom = bot;
            selectionColumns.style.bottom = bot;

            float columnWidth = DataMinerEditorPrefs.GetColumnWidth();

            for (int j = 0; j < dataPointsCount; j++)
            {
                VisualElement column = GetGraphColumn(j, "graph-bg");

                column.style.left = j * columnWidth + columnWidth / 2 - columnWidth / 2;
                column.style.height = h;
                column.style.width = columnWidth;
                column.visible = true;

                VisualElement line = GetGridLine(j);
                line.style.height = h;
                line.style.left = j * columnWidth + columnWidth / 2;
                line.visible = true;

            }

            foreach (var item in activeDatasets)
            {
                max = item.Value.max;
                min = item.Value.min;

                List<Vector2> pos = RecalculatePointsPos(columnWidth, h, max, item.Value);

                UpdateTooltip();
                int id = GetDatasetID(item.Key);
                IMGUIContainer graph = GetLineGraph(id);
                Color color = GetGraphColor(id);
                graph.onGUIHandler = () =>
                {
                    OnDrawTimeView(h, pos, color);
                };
                graph.visible = true;

                if (ActiveColumnSelection != -1)
                    SelectionChanged(ActiveColumnSelection, ActiveColumnSelection);
            }

            foreach (var item in currentSlice.counterData)
            {
                if (!activeDatasets.ContainsKey(item.Key))
                    GetLineGraph(GetDatasetID(item.Key)).visible = false;
            }

            for (int i = dataPointsCount; i < gridLines.Count; i++)
            {
                gridLines[i].visible = false;
            }

            for (int i = dataPointsCount; i < graphClickableColumns.Count; i++)
            {
                graphClickableColumns[i].visible = false;
            }

        }

        private void OnDrawTimeView(float h, List<Vector2> pointsPos, Color32 color)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(h));
            {
                Handles.BeginGUI();
                Handles.color = color;

                for (int i = 0; i < pointsPos.Count; i++)
                {
                    if (i + 1 < pointsPos.Count)
                    {
                        Vector2 pos1 = new Vector2(pointsPos[i].x, h - pointsPos[i].y);
                        Vector2 pos2 = new Vector2(pointsPos[i + 1].x, h - pointsPos[i + 1].y);

                        Vector3 midPointLow = new Vector3((pos1.x + pos2.x) / 2, pos1.y);
                        Vector3 midPointHigh = new Vector3(midPointLow.x, pos2.y);


                        Handles.DrawLine(pos1, midPointLow);

                        Handles.DrawLine(midPointLow, midPointHigh);
                        Handles.DrawLine(midPointHigh, pos2);

                    }
                }

                Handles.EndGUI();
            }
            GUILayout.EndHorizontal();
        }

        List<Vector2> RecalculatePointsPos(float columnWidth, float h, float max, DatasetCounters dataset)
        {
            List<Vector2> pos = new List<Vector2>();

            for (int i = 0; i < dataPointsCount; i++)
            {
                var x = i * columnWidth + columnWidth / 2;
                var y = dataset.columns[i].withinRecordingRange ? (dataset.columns[i].max * h) / max : 1;
                Vector2 newPos = new Vector2(x, y);
                pos.Add(newPos);
            }

            return pos;
        }

        protected bool ColumnExists(int toCheck)
        {
            return activeDatasets != null && activeDatasets.Count > 0 && activeDatasets.First().Value.columns.Length > toCheck;
        }

        public override void UpdateTooltip()
        {
            if (hoveredColumn != -1 && ColumnExists(hoveredColumn))
            {
                StringBuilder sb = new StringBuilder();

                var first = GetColumn(hoveredColumn);
                int totalSelected = first.framesTo - first.framesFrom;
                if (totalSelected > 1)
                    sb.AppendLine($"Frames {first.framesFrom} to {first.framesTo}");
                else
                    sb.AppendLine($"Frame {first.framesFrom}");


                foreach (var item in activeDatasets)
                {
                    ColumnCounters selected = item.Value.columns[hoveredColumn];

                    sb.AppendLine($"{item.Value.name} max {selected.max} @ {selected.maxAt}");
                }

                tooltip.Update(sb.ToString());
            }
        }

        protected override Column GetColumn(int id)
        {
            if (ColumnExists(id))
                return activeDatasets.First().Value.columns[id];
            else
                return null;
        }

        protected override List<DataID> GetSelectedDataIDs()
        {
            List<DataID> selectedDataIDs = new List<DataID>();
            foreach (var item in activeDatasets)
            {
                selectedDataIDs.Add(item.Key);
            }
            return selectedDataIDs;
        }
        
        protected override bool IsSliceViable() 
        { 

            if (currentSlice.counterData.Count == 0)
                return false;

            return true;
        }

        protected override void Reset()
        {
            lineGraphs.Clear();
            graphLinesParent.Clear();
            toggleDatasets.Clear();
            gridLines.Clear();
            gridLinesParent.Clear();
            graphClickableColumns.Clear();
            selectionColumns.Clear();
        }

    }
}
