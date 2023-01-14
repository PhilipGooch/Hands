using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public interface IResizable
    {
        public void SetHeight(int newHeight);
    }
    public interface ISelectable
    {
        public void UpdateSelection(int newSelection);
    }
    interface ITooltip
    {
        void UpdateTooltip();
    }

    public abstract class DataMinerWindowComponent
    {
        public VisualElement component;

        protected VisualTreeAsset visualTreeAsset;
        protected VisualElement root;
        protected Slice currentSlice;

        protected Action initialLayoutSetup;
        protected bool initialLayoutSetupCompleted;

        protected Action<MouseEnterEvent, MouseEventData> onMouseEnter;
        protected Action<MouseLeaveEvent, MouseEventData> onMouseLeave;
        protected Action<MouseUpEvent, MouseEventData> onMouseUp;

        protected Action<int, int> onSelectionChange; //only when new values differs from old
        protected Action<int, int> onSelection; //any time selection is set

        protected int activeColumnSelection;
        protected int ActiveColumnSelection
        {
            get
            {
                return activeColumnSelection;
            }
            set
            {
                int oldValue = activeColumnSelection;
                activeColumnSelection = value;

                if (oldValue != activeColumnSelection)
                {
                    onSelectionChange?.Invoke(oldValue, activeColumnSelection);
                }
                onSelection?.Invoke(oldValue, activeColumnSelection);

            }
        }

        protected int dataPointsCount;

        protected abstract void SetupDatasetSelection();
        protected abstract void AfterLayoutInitUpdate();
        protected abstract void UpdateSelectedDataset();
        protected abstract bool IsSliceViable();
        protected abstract void Reset();


        protected ToolbarMenu selectDataset;

        public void AddToWindow()
        {
            root.Add(component);
        }

        protected void SetupDeleteButton(Action<DataMinerWindowComponent> onRemove)
        {
            Button delete = component.Q<Button>("delete");
            delete.clickable.clicked += () => { onRemove(this); };
        }

        //only used when creating component or layout changed and need to wait for it to finish recalculating
        protected void Repaint()
        {
            initialLayoutSetupCompleted = false;

            if (currentSlice != null)
                initialLayoutSetup += AfterLayoutInitUpdate;

            component.RegisterCallback<GeometryChangedEvent>(LayoutSetupCompleted);
        }

        void LayoutSetupCompleted(GeometryChangedEvent evt)
        {
            initialLayoutSetupCompleted = true;
            initialLayoutSetup?.Invoke();

            component.UnregisterCallback<GeometryChangedEvent>(LayoutSetupCompleted);
        }

        //use when no need to wait for layout changes
        protected void Update()
        {
            if (currentSlice != null)
            {
                if (!initialLayoutSetupCompleted)
                    initialLayoutSetup += AfterLayoutInitUpdate;
                else
                    AfterLayoutInitUpdate();
            }
        }

        public void UpdateWithNewSlice(Slice slice)
        {
            if (currentSlice != slice)
            {
                currentSlice = slice;

                if (!IsSliceViable()) //TODO clear if false
                {
                    Reset();
                    return;
                }
                
                dataPointsCount = currentSlice.dataPointsCount;

                SetupDatasetSelection();
                UpdateSelectedDataset();

                Update();
            }
        }

        #region Events
        public void RegisterOnMouseEnter(Action<MouseEnterEvent, MouseEventData> onMouseEnter)
        {
            this.onMouseEnter += onMouseEnter;
        }
        public void RegisterOnMouseLeave(Action<MouseLeaveEvent, MouseEventData> onMouseLeave)
        {
            this.onMouseLeave += onMouseLeave;
        }
        public void RegisterOnMouseUp(Action<MouseUpEvent, MouseEventData> onMouseUp)
        {
            this.onMouseUp += onMouseUp;
        }

        public void RemoveOnMouseEnter(Action<MouseEnterEvent, MouseEventData> onMouseEnter)
        {
            this.onMouseEnter -= onMouseEnter;
        }
        public void RemoveOnMouseLeave(Action<MouseLeaveEvent, MouseEventData> onMouseLeave)
        {
            this.onMouseLeave -= onMouseLeave;
        }
        public void RemoveOnMouseUp(Action<MouseUpEvent, MouseEventData> onMouseUp)
        {
            this.onMouseUp -= onMouseUp;
        }
        #endregion
    }
}
