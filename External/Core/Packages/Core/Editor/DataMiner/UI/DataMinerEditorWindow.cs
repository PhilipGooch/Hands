using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public struct MouseEventData
    {
        public List<DataID> dataIDs;
        public int columnID;
        public int framesFrom;
        public int framesTo;

        public MouseEventData(List<DataID> dataIDs, int columnID, int framesFrom, int framesTo)
        {
            this.dataIDs = dataIDs;
            this.columnID = columnID;
            this.framesFrom = framesFrom;
            this.framesTo = framesTo;
        }

        public void Reset()
        {
            dataIDs = new List<DataID>();
            columnID = -1;
            framesFrom = -1;
            framesTo = -1;
        }
    }
    enum Components
    {
        COLUMN_GRAPH,
        LINE_GRAPH,
        SELECTION_DIAGNOSTICS
    }
    public class DataMinerEditorWindow : EditorWindow
    {
        VisualElement root;
        MinMaxSlider frameSelector;
        TextElement sliceDetails;
        ScrollView scroller;
        SettingsPopup settingsPopup;

        DataReader reader;
        Slice currentSlice;

        Vector2Int selection;
        List<DataMinerWindowComponent> active = new List<DataMinerWindowComponent>();

        int dataPointsCount;

        MouseEventData hoveredData;
        MouseEventData clickedData;

        int selectedFrame;

        [MenuItem("No Brakes Games/Data Miner/Viewer...", priority = 1)]
        public static void OpenWindow()
        {
            DataMinerEditorWindow wnd = GetWindow<DataMinerEditorWindow>();
            wnd.titleContent = new GUIContent("Data Miner Viewer");
            wnd.minSize = new Vector2(500, 300);
        }

        private void OnDestroy()
        {
            if (selectedFrame != -1)
                currentSlice?.ResetSelectionHandlers();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("da483cfa0ece2de4e82e427c54298372"));
            visualTree.CloneTree(root);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("3d8613bf7adcff94fabb6d285214d5a8"));
            root.styleSheets.Add(styleSheet);

            scroller = root.Q<ScrollView>("scroller");
            sliceDetails = root.Q<TextElement>("sliceDetails");
            sliceDetails.style.height = 23;
            frameSelector = root.Q<MinMaxSlider>("frameSelector");

            settingsPopup = new SettingsPopup(root, SetGraphsHeight, SetMsLineHeight, SetColumnWidth, SetMsCap);


            SetupWindow();
        }

        #region Setup
        void SetupWindow()
        {
            selection = Vector2Int.zero;
            dataPointsCount = 1;
            selectedFrame = -1;
            hoveredData = new MouseEventData(default, -1, -1, -1);
            clickedData = new MouseEventData(default, -1, -1, -1);
            //not dynamic parts
            {
                SetupAddComponentsMenu();
                SetupFileLoader();
                SetupOpenSettingsBtn();
            }


            root.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                clickedData.Reset();
                hoveredData.Reset();

                UpdateDataPointsCountFromWinWidth();
                UpdateComponents();
                UpdateComponentSelection();
            });


            AddComponent(Components.COLUMN_GRAPH);
            AddComponent(Components.LINE_GRAPH);
            AddComponent(Components.SELECTION_DIAGNOSTICS);
        }
        void SetupOpenSettingsBtn()
        {
            Button settingsBtn = root.Q<Button>("settingsBtn");

            settingsBtn.clickable.clicked += () =>
            {
                settingsPopup.Show();
            };
        }
        void SetupAddComponentsMenu()
        {
            ToolbarMenu addComponent = root.Q<ToolbarMenu>("addComponent");

            addComponent.menu.AppendAction("Timing", a => { AddComponent(Components.COLUMN_GRAPH); }, a => DropdownMenuAction.Status.Normal);
            addComponent.menu.AppendAction("Counters", a => { AddComponent(Components.LINE_GRAPH); }, a => DropdownMenuAction.Status.Normal);
            addComponent.menu.AppendAction("Selection Diagnostics", a => { AddComponent(Components.SELECTION_DIAGNOSTICS); }, a => DropdownMenuAction.Status.Normal);

        }
        void SetupSlider()
        {
            uint first = reader.FirstFrameNo;
            uint last = reader.LastFrameNo;

            //reset values
            {
                frameSelector.lowLimit = 0;
                frameSelector.highLimit = 0;
                frameSelector.minValue = 0;
                frameSelector.maxValue = 0;
            }

            //set new values
            {
                frameSelector.highLimit = last;
                frameSelector.lowLimit = first;

                frameSelector.maxValue = last;
                frameSelector.minValue = first;
            }

            frameSelector.value = new Vector2(first, last);

            frameSelector.RegisterValueChangedCallback((evt) => { SliderValueChanged(evt.newValue); });
        }

        void SetupFileLoader()
        {
            var _iconFolderOpened = EditorGUIUtility.IconContent("d_FolderOpened Icon");
            Button fileLoader = root.Q<Button>("fileLoader");
            fileLoader.style.backgroundImage = new StyleBackground((Texture2D)_iconFolderOpened.image);
            fileLoader.clickable.clicked += () =>
            {

                string path = EditorUtility.OpenFilePanel("Load Recording", DataMiner.DefaultFolder, DataMiner.FileExtension);
                Load(path);
            };

        }

        #endregion

        #region Events
        private void MouseEnterEvent(MouseEnterEvent evt, MouseEventData mouseEventData)
        {

            hoveredData = mouseEventData;
        }

        private void MouseLeaveEvent(MouseLeaveEvent evt, MouseEventData mouseEventData)
        {
            hoveredData.Reset();
        }

        private void MouseUpEvent(MouseUpEvent evt, MouseEventData mouseEventData)
        {
            if (mouseEventData.columnID == clickedData.columnID)
                clickedData.Reset();
            else
            {
                clickedData = mouseEventData;

            }
            UpdateSelectedFrame();
            UpdateComponentSelection();

        }

        private void WheelEvent(WheelEvent evt)
        {
            if (evt.delta.magnitude != 0)
            {
                Zoom(evt);
            }
        }
        #endregion

        void UpdateSelectedFrame()
        {
            if (clickedData.columnID != -1 && clickedData.dataIDs.Count > 0)
            {
                //if timer data exists, take maximum at selected column value
                if (currentSlice.timerData.Count > 0)
                {
                    if (clickedData.dataIDs.First().dataSource is ITimingProvider)
                    {
                        selectedFrame = (int)currentSlice.timerData[clickedData.dataIDs.First()].columns[clickedData.columnID].maxAt;
                    }
                    else
                    {
                        selectedFrame = (int)currentSlice.timerData.First().Value.columns[clickedData.columnID].maxAt;
                    }
                }
                //if timer data doesnt exit, take first unique frame in selected column OR first unique frame going back to first column;
                else
                {
                    selectedFrame = currentSlice.GetFirstUniqueFrameInCountersColumn(clickedData.columnID);
                    Debug.Log(selectedFrame);
                }


            }
            else
            {
                selectedFrame = -1;
            }

            if (selectedFrame != -1)
                currentSlice.SelectFrame((uint)selectedFrame);
        }

        void SliderValueChanged(Vector2 newValue)
        {
            if (root != null)
            {
                // selection = Vector2Int.RoundToInt( newValue);
                if (UpdateSelectedSlice())
                {

                    UpdateComponents();
                    UpdateComponentSelection();
                }
            }
        }

        void SetGraphsHeight(int newHeight)
        {
            foreach (var item in active)
            {
                IResizable resizable = item as IResizable;

                if (resizable != null)
                    resizable.SetHeight(newHeight);
            }
        }

        void SetMsLineHeight(int newHeight)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].GetType() == typeof(ColumnGraph))
                {
                    ((ColumnGraph)active[i]).SetMsLineHeight(newHeight);
                }
            }
        }

        void SetMsCap(int newCap)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].GetType() == typeof(ColumnGraph))
                {
                    ((ColumnGraph)active[i]).SetMsCap(newCap);
                }
            }
        }

        void SetColumnWidth(int newWidth)
        {
            UpdateDataPointsCountFromWinWidth();
            UpdateComponents();
        }

        void UpdateDataPointsCountFromWinWidth()
        {
            float w = root.resolvedStyle.width;
            w -= 40; //hardcoded a bit
            int c = (int)w / DataMinerEditorPrefs.GetColumnWidth();

            if (c != dataPointsCount)
            {
                dataPointsCount = c;
                SetNewSlice();
            }

        }

        void NewDataSetRead()
        {
            selection = Vector2Int.zero;

            SetupSlider();

            UpdateSelectedSlice();
            UpdateComponents();
        }

        void Zoom(WheelEvent evt)
        {
            Vector2Int selection = Vector2Int.RoundToInt(frameSelector.value);

            int zoomAt = clickedData.columnID != -1 ? clickedData.columnID : hoveredData.columnID;
            if (zoomAt != -1 && currentSlice != null)
            {
                evt.PreventDefault();
                evt.StopPropagation();

                Column columnToZoomAt = currentSlice.GetColumnFromDataID(clickedData.columnID != -1 ? clickedData.dataIDs[0] : hoveredData.dataIDs[0], zoomAt);
                if (columnToZoomAt == null)
                    return;

                int framesToTheLeft = columnToZoomAt.framesFrom - currentSlice.framesFrom;
                int framesToTheRight = currentSlice.framesTo - columnToZoomAt.framesTo;

                int changeLeft = Mathf.Max((int)(framesToTheLeft * 0.02f), zoomAt);
                int changeRight = Mathf.Max((int)(framesToTheRight * 0.02f), dataPointsCount - zoomAt);


                if (evt.delta.y > 0) //zoom out
                {
                    selection = new Vector2Int(selection.x - changeLeft, selection.y + changeRight);
                    ClampSelection(selection, selection.y - selection.x);

                }
                else if (evt.delta.y < 0) //zoom in
                {
                    if (selection.y - selection.x > dataPointsCount)
                    {
                        selection = new Vector2Int(selection.x + changeLeft, selection.y - changeRight);
                        ClampSelection(selection, selection.y - selection.x);
                    }
                }
            }
        }

        void ClampSelection(Vector2Int newSelection, int fullRange)
        {
            int globalMin = (int)reader.FirstFrameNo;
            int globalMax = (int)reader.LastFrameNo;
            int globalRange = (globalMax - globalMin);

            if (fullRange < dataPointsCount)
            {
                int dif = dataPointsCount - fullRange;

                newSelection.y += dif / 2;
                newSelection.x -= dif - dif / 2;
            }

            int actualRange = Mathf.Min(newSelection.y, globalMax) - Mathf.Max(newSelection.x, globalMin);

            if (actualRange < fullRange && globalRange >= fullRange) //new range has less data points then it should AND it is less than available range
            {
                Vector2Int orig = newSelection;

                int dif = fullRange - actualRange;

                int x = Mathf.Max(newSelection.x - dif / 2, globalMin);
                int y = Mathf.Min(newSelection.y + (dif - dif / 2), globalMax);

                int changeX = x - orig.x;
                int changeY = y - orig.y;

                if (newSelection.x < globalMin)
                {
                    y += changeX;
                }

                if (newSelection.y > globalMax)
                {
                    x += changeY;
                }

                frameSelector.value = new Vector2(x, y);
            }
            else
            {
                frameSelector.value = newSelection;
            }
        }

        private void UpdateSelectionDetails()
        {
            sliceDetails.text = $"Recorded frames {reader.FirstFrameNo} - {reader.LastFrameNo} | Frames visible {selection.y - selection.x} from {selection.x} - to {selection.y}";

        }

        void UpdateComponents()
        {
            if (currentSlice != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    active[i].UpdateWithNewSlice(currentSlice);
                }
            }

        }

        void UpdateComponentSelection()
        {
            if (currentSlice != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    ISelectable selectable = active[i] as ISelectable;

                    if (selectable != null)
                        selectable.UpdateSelection(clickedData.columnID);
                }
            }
        }

        bool UpdateSelectedSlice()
        {
            Vector2Int lastSelection = selection;
            selection = new Vector2Int((int)frameSelector.value.x, (int)frameSelector.value.y);

            if (lastSelection != selection || currentSlice == null)
            {
                SetNewSlice();

                return true;
            }
            else
                return false;
        }

        void SetNewSlice()
        {
            if (reader != null)
            {
                currentSlice = new Slice(reader, selection.x, selection.y, dataPointsCount);

                UpdateSelectionDetails();
            }
        }

        void AddComponent(Components toAdd)
        {
            if (active.OfType<SelectionDiagnostics>().Any() && toAdd == Components.SELECTION_DIAGNOSTICS)
                return;


            DataMinerWindowComponent newComponent;
            switch (toAdd)
            {
                case Components.COLUMN_GRAPH:
                    newComponent = new ColumnGraph(scroller, dataPointsCount, RemoveComponent);
                    break;
                case Components.LINE_GRAPH:
                    newComponent = new LineGraph(scroller, dataPointsCount, RemoveComponent);
                    break;
                case Components.SELECTION_DIAGNOSTICS:
                    newComponent = new SelectionDiagnostics(scroller, RemoveComponent);
                    break;
                default:
                    newComponent = new ColumnGraph(scroller, dataPointsCount, RemoveComponent);
                    break;
            }

            newComponent.RegisterOnMouseEnter(MouseEnterEvent);
            newComponent.RegisterOnMouseLeave(MouseLeaveEvent);
            newComponent.RegisterOnMouseUp(MouseUpEvent);

            newComponent.component.RegisterCallback<WheelEvent>(WheelEvent);
            active.Add(newComponent);

            if (currentSlice != null)
            {
                newComponent.UpdateWithNewSlice(currentSlice);

                ISelectable selectable = newComponent as ISelectable;
                if (selectable != null)
                    selectable.UpdateSelection(clickedData.columnID);
            }

        }

        void RemoveComponent(DataMinerWindowComponent toRemove)
        {
            active.Remove(toRemove);
            scroller.Remove(toRemove.component);
        }

        void Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                byte[] _loadedData = File.ReadAllBytes(filePath);
                reader = new DataReader(new BinaryReader(new MemoryStream(_loadedData)));
                reader.Read();

                NewDataSetRead();
            }
        }
    }
}

