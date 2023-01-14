using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public abstract class GraphComponentBase : DataMinerWindowComponent, IResizable, ITooltip, ISelectable
    {
        protected List<VisualElement> graphClickableColumns;

        protected VisualElement graphBox;
        protected VisualElement selectionColumns;

        protected Box detailsParent;
        protected Tooltip tooltip;

        protected int hoveredColumn;

        protected abstract Column GetColumn(int id);
        protected abstract List<DataID> GetSelectedDataIDs();
        public abstract void UpdateTooltip();
   
        protected void Setup(VisualElement root, int columnCount, Action<DataMinerWindowComponent> onRemove, string uxmlGUID)
        {
            activeColumnSelection = -1;

            this.dataPointsCount = columnCount;
            this.root = root;

            graphClickableColumns = new List<VisualElement>();

            visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGUID));
            component = visualTreeAsset.Instantiate();
            AddToWindow();

            SetupDeleteButton(onRemove);

            graphBox = component.Q<VisualElement>("graphBox");
            graphBox.style.height = DataMinerEditorPrefs.GetGraphsHeight();
            detailsParent = component.Q<Box>("graphDetailsBox");
            selectionColumns = component.Q<VisualElement>("selectionColumns");

            tooltip = new Tooltip(component);
            onMouseEnter += ShowTooltip;
            onMouseLeave += HideTooltip;

        }

        protected VisualElement GetGraphColumn(int id, string aditionalClass = "")
        {

            if (graphClickableColumns.Count > id)
                return graphClickableColumns[id];

            //create new
            float columnWidth = DataMinerEditorPrefs.GetColumnWidth();

            VisualElement column = new VisualElement();
            column.style.left = id * columnWidth;
            column.style.width = columnWidth;
            column.style.height = 1;
           
            column.RegisterCallback<MouseEnterEvent>((evt) => { OnMouseEnter(evt, column); });
            column.RegisterCallback<MouseLeaveEvent>((evt) => { OnMouseLeave(evt, column); });
            column.RegisterCallback<MouseUpEvent>((evt) => { OnMouseUp(evt, column); });

            column.AddToClassList("graph-column");

            if (!string.IsNullOrEmpty(aditionalClass))
                column.AddToClassList(aditionalClass);


            graphClickableColumns.Add(column);
            selectionColumns.Add(column);

            return column;
        }

        public void UpdateSelection(int newSelection)
        {
            ActiveColumnSelection = newSelection;
        }

        public void SetHeight(int newHeight)
        {
            graphBox.style.height = newHeight;
            Repaint();
        }

        void ShowTooltip(MouseEnterEvent evt,MouseEventData data)
        {
            if (data.columnID != -1)
            {
                hoveredColumn = data.columnID;
                tooltip.Move(evt.localMousePosition, GetGraphColumn(data.columnID));
                UpdateTooltip();
            }
        }

        void HideTooltip(MouseLeaveEvent evt, MouseEventData data)
        {
            hoveredColumn = -1;
            tooltip.Hide();
        }

        protected void OnMouseEnter(MouseEnterEvent evt, VisualElement element)
        {
            int id = IDFromButton(element);
            Column clm = GetColumn(id);
            onMouseEnter?.Invoke(evt, new MouseEventData(GetSelectedDataIDs(),id, clm != null? clm.framesFrom:-1, clm != null ? clm.framesTo:-1));
        }

        protected void OnMouseLeave(MouseLeaveEvent evt, VisualElement element)
        {
            int id = IDFromButton(element);
            Column clm = GetColumn(id);
            onMouseLeave?.Invoke(evt, new MouseEventData(GetSelectedDataIDs(), id, clm != null ? clm.framesFrom : -1, clm != null ? clm.framesTo : -1));
        }

        protected void OnMouseUp(MouseUpEvent evt, VisualElement element)
        {
            int id = IDFromButton(element);
            Column clm = GetColumn(id);
            onMouseUp?.Invoke(evt, new MouseEventData(GetSelectedDataIDs(), id, clm != null ? clm.framesFrom : -1, clm != null ? clm.framesTo : -1));
        }

        protected int IDFromButton(VisualElement button)
        {
            return graphClickableColumns.FindIndex(x => x == button);
        }
    }
}
