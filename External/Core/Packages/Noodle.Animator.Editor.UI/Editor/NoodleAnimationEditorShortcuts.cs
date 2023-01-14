using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Hotkeys actions
    /// </summary>
    internal class NoodleAnimationEditorShortcuts
    {
        private AnimatorWindowAnimationData data;

        private Action<int> verticalArrowKeysScroll;
        private Action<int> horizontalArrowKeysScroll;

        internal void Initialize(AnimatorWindowAnimationData data, Action<int> verticalArrowKeysScroll, Action<int> horizontalArrowKeysScroll)
        {
            this.data = data;
            this.verticalArrowKeysScroll = verticalArrowKeysScroll;
            this.horizontalArrowKeysScroll = horizontalArrowKeysScroll;
        }

        internal void OnKeyDownInspector(KeyDownEvent ev)
        {
            switch (ev.keyCode)
            {
                case KeyCode.UpArrow:

                    verticalArrowKeysScroll?.Invoke(1);
                    data.selectionController.SelectUp(!ev.shiftKey);
                    data.StateChanged();

                    break;
                case KeyCode.DownArrow:

                    verticalArrowKeysScroll?.Invoke(-1);
                    data.selectionController.SelectDown(!ev.shiftKey);
                    data.StateChanged();

                    break;
                case KeyCode.RightArrow:
                    if (ev.ctrlKey)
                    {
                        int change = data.selectionController.SelectNext(!ev.shiftKey);
                        horizontalArrowKeysScroll?.Invoke(-change);
                        data.StateChanged();
                    }
                    break;
                case KeyCode.LeftArrow:
                    if (ev.ctrlKey)
                    {
                        int change = data.selectionController.SelectPrevious(!ev.shiftKey);
                        horizontalArrowKeysScroll?.Invoke(-change);
                        data.StateChanged();
                    }
                    break;
            }
        }

        internal void OnKeyDownTimeline(KeyDownEvent ev)
        {
            switch (ev.keyCode)
            {
                case KeyCode.M:
                    if (ev.ctrlKey)
                    {
                        data.operationsController.FlipSelectionHorizontaly();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.A:
                    if (ev.ctrlKey)
                    {
                        data.selectionController.SelectAll();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.C:
                    if (ev.ctrlKey)
                    {
                        data.operationsController.Copy();
                    }
                    break;
                case KeyCode.X:
                    if (ev.ctrlKey)
                    {
                        data.operationsController.Cut();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.V:
                    if (ev.ctrlKey)
                    {
                        data.operationsController.Paste(deleteKeysInArea:false);
                        data.refreshSource = RefreshSource.KeyboardOperation;
                        data.selectionController.CalculatePivot();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.Z:
                    if (ev.ctrlKey)
                    {
                        data.undoController.Undo();
                        data.refreshSource = RefreshSource.KeyboardOperation;
                        data.selectionController.CalculatePivot();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.Y:
                    if (ev.ctrlKey)
                    {
                        data.undoController.Redo();
                        data.refreshSource = RefreshSource.KeyboardOperation;
                        data.selectionController.CalculatePivot();
                        data.StateChanged();
                    }
                    break;
                case KeyCode.UpArrow:

                    verticalArrowKeysScroll?.Invoke(1);
                    data.selectionController.SelectUp(!ev.shiftKey);
                    data.StateChanged();

                    break;
                case KeyCode.DownArrow:

                    verticalArrowKeysScroll?.Invoke(-1);
                    data.selectionController.SelectDown(!ev.shiftKey);
                    data.StateChanged();

                    break;
                case KeyCode.RightArrow:
                    if (ev.ctrlKey)
                    {
                        int change = data.selectionController.SelectNext(!ev.shiftKey);
                        horizontalArrowKeysScroll?.Invoke(-change);
                        data.StateChanged();
                    }
                    break;
                case KeyCode.LeftArrow:
                    if (ev.ctrlKey)
                    {
                        int change = data.selectionController.SelectPrevious(!ev.shiftKey);
                        horizontalArrowKeysScroll?.Invoke(-change);
                        data.StateChanged();
                    }
                    break;
                case KeyCode.Delete:
                    data.operationsController.DeleteSelection();
                    data.StateChanged();
                    break;
                case KeyCode.K:
                    data.operationsController.CreateKey();
                    data.StateChanged();
                    break;
            }
        }
    }
}
