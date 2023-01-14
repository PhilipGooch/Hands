using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{

    // The SplitterPanel manages two rectangular panes (A and B) separated by a draggable divider (D), that all together occupy a nominated
    // rectangle (Bounds) within the parent container's coordinate system. The divider controls the size of pane A directly, whilst pane B gets
    // whatever space is left over. The default configuration is a vertical divider with pane A on the left and pane B to the right, but this can
    // be changed. (Switching between vertical and horizontal modes requires reinitialisation, but swapping over the panels across the divider can
    // be done dynamically.)
    //   Minimum sizes can be set for the two panes to prevent either being made too small. An option allows pane A to be dragged to zero size
    // despite the minimum size, so it can be dragged closed (snapping). Additionally, there are two 'collapsed' states, where one of the panes
    // and the divider are hidden, with the remaining pane expanded to fill the entire Bounds.
    public abstract class SplitterPanelBase
    {
        public SplitterPanelBase()
        {
            // Need to call InitState() to do late config. Also need to set up width of the divider (sub-class normally does this)
        }

        public void CompleteInit()
        {
            // call when all initial geometry is set up (bounds, minima, configs, etc) to regenerate the auxilliary variables
            UpdateMinBounds();
            UpdateRange();
            RefreshOutputs();
            dragging = false;
        }

        public enum CollapseMode
        {
            Normal,         // panes A and B are both enabled (if size>0) and divider is visible and draggable
            CollapsedA,     // pane A is collapsed; pane A and divider are not drawn and pane B expands to fill the bounds
            CollapsedB,     // pane B is collapsed; pane B and divider are not drawn and pane A expands to fill the bounds
        }

        public struct InitialConfig
        {
            public bool UpDownMode;     // if true, divider is horizontal, with A above and B below; else divider is vertical with A left and B right
            public bool SwitchPanels;   // if true, locations of A and B are swapped over (across the divider)
            public bool CanSnapAClosed; // if true, pane A can be dragged to zero size despite MinSizeA (ie. will snap from MinSizeA to zero if dragged enough)
            public CollapseMode Collapse;
            public float PaneASize;     // initial size of pane A (when not collapsed)
            public Rect InitialBounds;  // initial container rectangle
            public float MinSizeA;      // 
            public float MinSizeB;
        }

        public void InitState(InitialConfig config, bool deferRecalc = false)
        {
            // reinitialises everything except the width of the divider (which needs setting up before calling CompleteInit())
            UpDownMode = config.UpDownMode;
            _SwitchPanels = config.SwitchPanels;
            _CanSnapAClosed = config.CanSnapAClosed;
            _CollapseMode = config.Collapse;
            SizeA = config.PaneASize;
            _Bounds = config.InitialBounds;
            _MinSizeA = config.MinSizeA;
            _MinSizeB = config.MinSizeB;

            if (!deferRecalc)
                CompleteInit();
        }

        // these are the output dimensions in the parent container's coordinate space
        public Rect RectPaneA { get; protected set; }
        public Rect RectPaneB { get; protected set; }
        public Rect RectDivider { get; protected set; }

        public bool DrawPaneA { get; protected set; } // true if pane A has non-zero area and is enabled
        public bool DrawPaneB { get; protected set; } // true if pane B has non-zero area and is enabled
        public bool DrawDivider { get; protected set; } // true if divider has non-zero area and is enabled

        public bool UpDownMode { get; protected set; }

        public bool SwitchPanels
        {
            get { return _SwitchPanels; }
            set
            {
                if (_SwitchPanels != value)
                {
                    _SwitchPanels = value;
                    dragging = false;
                    RefreshOutputs();
                }
            }
        }
        public CollapseMode CollapseState
        {
            get { return _CollapseMode; }
            set
            {
                if (_CollapseMode != value)
                {
                    _CollapseMode = value;
                    dragging = false;
                    UpdateRange();
                    RefreshOutputs();
                }
            }
        }
        public bool CanSnapAClosed
        {
            get { return _CanSnapAClosed; }
            set
            {
                if (_CanSnapAClosed != value)
                {
                    _CanSnapAClosed = value;
                    UpdateRange();
                    RefreshOutputs();
                }
            }
        }
        public float MinSizeA
        {
            get { return _MinSizeA; }
            set
            {
                if (_MinSizeA != value)
                {
                    _MinSizeA = value;
                    UpdateMinBounds();
                    UpdateRange();
                    RefreshOutputs();
                }
            }
        }
        public float MinSizeB
        {
            get { return _MinSizeB; }
            set
            {
                if (_MinSizeB != value)
                {
                    _MinSizeB = value;
                    UpdateMinBounds();
                    UpdateRange();
                    RefreshOutputs();
                }
            }
        }
        public Rect Bounds
        {
            get { return _Bounds; }
            set
            {
                if ((_Bounds.x != value.x) || (_Bounds.y != value.y) || (_Bounds.width != value.width) || (_Bounds.height != value.height))
                {
                    _Bounds = value;
                    UpdateMinBounds();
                    UpdateRange();
                    RefreshOutputs();
                }
            }
        }


        protected bool _SwitchPanels = false;
        protected bool _CanSnapAClosed;
        protected bool _IsSnappedClosed = false;
        protected CollapseMode _CollapseMode = CollapseMode.Normal;
        protected float _MinSizeA = 0.0f;
        protected float _MinSizeB = 0.0f;
        protected float SizeA = 150.0f; // controlled by divider (SizeB is derived implicitly)
        protected float SizeD = 10.0f; // controlled by config
        protected float ActiveSizeA, ActiveSizeB, ActiveSizeD; // derived from Size* and CollapseMode

        public bool debugRender = false;

        protected Rect _Bounds;

        public float MinBoundsX { get; protected set; } // smallest Bounds.xMax permitted
        public float MinBoundsY { get; protected set; }

        protected void UpdateMinBounds()
        {
            if (UpDownMode)
            {
                MinBoundsX = _Bounds.xMax;
                MinBoundsY = _Bounds.yMin + _MinSizeA + _MinSizeB + SizeD;
            }
            else
            {
                MinBoundsX = _Bounds.xMin + _MinSizeA + _MinSizeB + SizeD;
                MinBoundsY = _Bounds.yMax;
            }
        }

        protected void UpdateRange()
        {
            // SizeA and SizeD are internal data that only changes when absolutely necessary.
            float bounds = (UpDownMode ? _Bounds.height : _Bounds.width);
            if (bounds < 0.0f)
                bounds = 0.0f;

            switch (_CollapseMode)
            {
                case CollapseMode.CollapsedA:
                    ActiveSizeA = 0.0f;
                    ActiveSizeD = 0.0f;
                    ActiveSizeB = bounds;
                    //_IsSnappedClosed = false;
                    return;
                case CollapseMode.CollapsedB:
                    ActiveSizeB = 0.0f;
                    ActiveSizeD = 0.0f;
                    ActiveSizeA = bounds;
                    //_IsSnappedClosed = false;
                    return;
            }

            ActiveSizeA = SizeA;
            ActiveSizeD = SizeD;

            float overflow = ClampActiveA(bounds);
            if (overflow > 0.0f)
            {
                // this will only happen if clamping ActiveSizeB to _MinSizeB made a change
                // steal pixels from A
                ActiveSizeA -= overflow;
                overflow = ClampActiveA(bounds);

                if (overflow > 0.0f)
                {
                    // can A be snapped shut?
                    ActiveSizeA = 0.0f;
                    overflow = ClampActiveA(bounds);

                    if (overflow > 0.0f)
                    {
                        // constaints failed
                        //if (overflow > 0.5f)
                        //    Debug.LogError("Divider bounds violated");

                        // remove the side panel (A) completely
                        ActiveSizeA = 0.0f;
                        ActiveSizeD = 0.0f;
                        ActiveSizeB = bounds;
                    }
                }
            }

            //SizeA = ActiveSizeA;
            _IsSnappedClosed = _CanSnapAClosed && (ActiveSizeA <= 0.0f);
            // called RefreshOutputs() to apply new settings
        }

        float ClampActiveA(float bounds)
        {
            // handles snapping closed/open
            if (_CanSnapAClosed)
            {
                if ((ActiveSizeA <= 0.0f) || ((ActiveSizeA < _MinSizeA * 0.5f) && _IsSnappedClosed))
                    ActiveSizeA = 0.0f;
                else if (ActiveSizeA < _MinSizeA)
                    ActiveSizeA = _MinSizeA;
            }
            else if (ActiveSizeA < _MinSizeA)
                ActiveSizeA = _MinSizeA;

            ActiveSizeB = bounds - ActiveSizeA - ActiveSizeD;
            if (ActiveSizeB < _MinSizeB)
                ActiveSizeB = _MinSizeB;
            float overflow = ActiveSizeA + ActiveSizeB + ActiveSizeD - bounds;
            return overflow;
        }


        void MoveDivider(float newSizeA)
        {
            SizeA = newSizeA;
            UpdateRange();
            RefreshOutputs();
        }

        bool dragging = false;
        float dragSize; // SizeA at start of drag
        float dragRefPt; // mouse pos at start of drag

        protected void RefreshOutputs()
        {
            // Call UpdateRange() to refresh ActiveSize*
            if (UpDownMode)
            {
                if (SwitchPanels)
                {
                    // B|D|A vertical
                    RectPaneB = new Rect(_Bounds.x, _Bounds.y, _Bounds.width, ActiveSizeB);
                    RectDivider = new Rect(_Bounds.x, _Bounds.y + ActiveSizeB, _Bounds.width, ActiveSizeD);
                    RectPaneA = new Rect(_Bounds.x, _Bounds.y + ActiveSizeB + ActiveSizeD, _Bounds.width, ActiveSizeA);
                }
                else
                {
                    // A|D|B vertical
                    RectPaneA = new Rect(_Bounds.x, _Bounds.y, _Bounds.width, ActiveSizeA);
                    RectDivider = new Rect(_Bounds.x, _Bounds.y + ActiveSizeA, _Bounds.width, ActiveSizeD);
                    RectPaneB = new Rect(_Bounds.x, _Bounds.y + ActiveSizeA + ActiveSizeD, _Bounds.width, ActiveSizeB);
                }
            }
            else
            {
                if (SwitchPanels)
                {
                    // B|D|A horizontal
                    RectPaneB = new Rect(_Bounds.x, _Bounds.y, ActiveSizeB, _Bounds.height);
                    RectDivider = new Rect(_Bounds.x + ActiveSizeB, _Bounds.y, ActiveSizeD, _Bounds.height);
                    RectPaneA = new Rect(_Bounds.x + ActiveSizeB + ActiveSizeD, _Bounds.y, ActiveSizeA, _Bounds.height);
                }
                else
                {
                    // A|D|B horizontal
                    RectPaneA = new Rect(_Bounds.x, _Bounds.y, ActiveSizeA, _Bounds.height);
                    RectDivider = new Rect(_Bounds.x + ActiveSizeA, _Bounds.y, ActiveSizeD, _Bounds.height);
                    RectPaneB = new Rect(_Bounds.x + ActiveSizeA + ActiveSizeD, _Bounds.y, ActiveSizeB, _Bounds.height);
                }
            }

            DrawPaneA = (RectPaneA.width > 0.0f) && (RectPaneA.height > 0.0f) && (_CollapseMode != CollapseMode.CollapsedA);
            DrawPaneB = (RectPaneB.width > 0.0f) && (RectPaneB.height > 0.0f) && (_CollapseMode != CollapseMode.CollapsedB);
            DrawDivider = (RectDivider.width > 0.0f) && (RectDivider.height > 0.0f) && (_CollapseMode == CollapseMode.Normal);
            if (!DrawDivider)
                dragging = false;
        }

        protected abstract void RenderDivider();

        public bool OnGUI(int dividerControlHint = -99)
        {
            // returns true if the window needs a repaint (because of an input change)
            bool needsRepaint = false;

            Event evt = Event.current;

            int id = GUIUtility.GetControlID(dividerControlHint, FocusType.Passive, RectDivider);
            if (GUIUtility.hotControl == id)
            {
                if (!dragging)
                {
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
            }
            else
            {
                dragging = false;
            }

            switch (evt.type)
            {
                case EventType.MouseUp:
                    if ((evt.button == 0) && (GUIUtility.hotControl == id))
                    {
                        dragging = false;
                        GUIUtility.hotControl = 0;
                    }
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                        if (!dragging)
                            GUIUtility.hotControl = 0;
                    }
                    break;
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    {
                        // allow only left when starting drag, but any button may continue drag (until left is released)
                        if ((evt.button != 0) && !dragging)
                            break;
                        if (!dragging && (GUIUtility.hotControl == 0))
                        {
                            if (!DrawDivider || !RectDivider.Contains(evt.mousePosition))
                                break;
                            GUIUtility.hotControl = id;
                            dragging = true;
                            dragSize = ActiveSizeA;
                            dragRefPt = (UpDownMode ? evt.mousePosition.y : evt.mousePosition.x);
                        }
                        if (GUIUtility.hotControl != id)
                        {
                            dragging = false;
                            break;
                        }
                        // Make sure no one uses the event after us
                        evt.Use();

                        float movement = (UpDownMode ? evt.mousePosition.y : evt.mousePosition.x) - dragRefPt;
                        if (_SwitchPanels)
                            movement = -movement;
                        MoveDivider(dragSize + movement);

                        needsRepaint = true;
                        break;
                    }
            }

#if true
            if (debugRender)
            {
                // for some reason, DrawRect seems to be a pixel out. Not sure why. Window border perhaps?
                Rect r1 = RectPaneA; //r1.width -= 1;
                Rect r2 = RectDivider;// r2.x -= 1;
                Rect r3 = RectPaneB;// r3.x -= 1; r3.width += 1;
                if (DrawPaneA)
                    EditorGUI.DrawRect(r1, Color.red);
                if (DrawPaneB)
                    EditorGUI.DrawRect(r3, Color.green);
                if (DrawDivider)
                    EditorGUI.DrawRect(r2, Color.blue);
            }
#endif

            // draw proper divider
            if (DrawDivider)
                RenderDivider();

            EditorGUIUtility.AddCursorRect(RectDivider, (UpDownMode ? MouseCursor.SplitResizeUpDown : MouseCursor.SplitResizeLeftRight), id);

            return needsRepaint;
        }
    }

    public class SplitterPanel : SplitterPanelBase
    {
        public SplitterPanel() : base()
        {
            InitDividerShape();
        }

        public void InitDividerShape(float divW = 6.0f, float lineW = 3.0f, float marginY = 15.0f)
        {
            SizeD = divW;
            LineW = lineW;
            MarginY = marginY;

            CompleteInit();
        }

        protected float LineW = 2.0f;
        protected float MarginY = 0.0f;
        public Color LeftEdgeColor = new Color(0.25f, 0.25f, 0.25f, 1.0f); // or top
        public Color RightEdgeColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // or bottom

        protected override void RenderDivider()
        {
            // draw proper divider
            if (UpDownMode)
            {
                float offset = (RectDivider.height - LineW) * 0.5f;
                //offset -= 1.0f; //// dunno why, but seems necessary
                Rect bar = new Rect(RectDivider.x + MarginY, RectDivider.y + offset, RectDivider.width - MarginY * 2.0f, LineW * 0.5f);
                EditorGUI.DrawRect(bar, LeftEdgeColor);
                bar.y += bar.height;
                EditorGUI.DrawRect(bar, RightEdgeColor);
            }
            else
            {
                float offset = (RectDivider.width - LineW) * 0.5f;
                //offset -= 1.0f; //// dunno why, but seems necessary
                Rect bar = new Rect(RectDivider.x + offset, RectDivider.y + MarginY, LineW * 0.5f, RectDivider.height - MarginY * 2.0f);
                EditorGUI.DrawRect(bar, LeftEdgeColor);
                bar.x += bar.width;
                EditorGUI.DrawRect(bar, RightEdgeColor);
            }
        }

        // direction: 0=right,1=left,2=down,3=up
        public static readonly string[] CollapseSymbols = { "\u25ba", "\u25c4", "\u25bc", "\u25b2" };
        public string DetermineCollapseTip(bool right)
        {
            if (CollapseState == SplitterPanelBase.CollapseMode.CollapsedA)
                return "Open side panel";
            if (right ? !SwitchPanels : SwitchPanels)
                return "Move side panel to this side";
            return "Close side panel";
        }
        public int DetermineCollapseSymbol(bool right)
        {
            bool doOpen = (CollapseState == SplitterPanelBase.CollapseMode.CollapsedA) || (right ? !SwitchPanels : SwitchPanels);
            if (right)
                doOpen = !doOpen;
            if (UpDownMode)
                return doOpen ? 2 : 3;
            else
                return doOpen ? 0 : 1;
        }
        public void ProcessCollapseButton(bool right)
        {
            bool dir = SwitchPanels;
            if (right)
                dir = !dir;
            if (dir)
            {
                SwitchPanels = !SwitchPanels;
                CollapseState = SplitterPanelBase.CollapseMode.Normal;
            }
            else if (CollapseState != SplitterPanelBase.CollapseMode.Normal)
                CollapseState = SplitterPanelBase.CollapseMode.Normal;
            else
                CollapseState = SplitterPanelBase.CollapseMode.CollapsedA;
        }

        static GUIStyle style_Button = null;
        public bool DefaultCollapeButtons(float size = 16.0f, float margin1 = 4.0f, float margin2 = 4.0f)
        {
            if (!DrawPaneB)
                return false;
            if (style_Button == null)
            {
                style_Button = new GUIStyle(EditorStyles.miniButton);
                style_Button.fontSize = 7;
            }

            bool needsRepaint = false;
            Rect where = new Rect(RectPaneB.x + margin1, RectPaneB.y + margin1, size, size);
            if (UpDownMode)
                where.x = RectPaneB.xMax - margin2 - size;
            GUIContent content = new GUIContent(CollapseSymbols[DetermineCollapseSymbol(false)], DetermineCollapseTip(false));
            if (GUI.Button(where, content, style_Button))
            {
                ProcessCollapseButton(false);
                needsRepaint = true;
            }
            if (UpDownMode)
                where.y = RectPaneB.yMax - margin2 - size;
            else
                where.x = RectPaneB.xMax - margin2 - size;
            content = new GUIContent(CollapseSymbols[DetermineCollapseSymbol(true)], DetermineCollapseTip(true));
            if (GUI.Button(where, content, style_Button))
            {
                ProcessCollapseButton(true);
                needsRepaint = true;
            }
            return needsRepaint;
        }
    }

}
