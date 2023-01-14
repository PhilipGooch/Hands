using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Extended foldout to handle custom selection
    /// </summary>
    public class SearcherFoldoutView : Foldout, ISearcherSelectable
    {
        Toggle toggle;
        Toggle Toggle
        {
            get
            {
                if (toggle == null)
                    toggle = this.Q<Toggle>();

                return toggle;
            }
        }

        private VisualElement iconContainer = new VisualElement();

        private VisualElement parentElement;
        internal SearcherItemView ParentItemView { get; private set; }

        public bool IsVissible
        {
            get
            {
                var foldout = parentElement as SearcherFoldoutView;
                if (foldout != null)
                {
                    if (!foldout.value)
                    {
                        return false;
                    }
                    else
                    {
                        return foldout.IsVissible;
                    }
                }

                return true;
            }
        }

        public string UniqueID { get; private set; }

        public bool Selected { get; private set; }

        public bool Hovered { get; private set; }

        public int OrderIndex { get; set; }
        
        string nameForFiltering;

        private Action<ISearcherSelectable, bool> onSelect;
        //needed for milkshake only
        private int depth;
        private UnityEngine.Object relativeObj;
        
        internal void Initialize(
            VisualElement parentElement,
            SearcherItemView parentItemView,
            List<(string segment, UnityEngine.Object relativeObj)> path,
            int depth,
            bool defaultToggleValue,
            Action<ISearcherSelectable, bool> onSelect)
        {
            this.parentElement = parentElement;
            this.ParentItemView = parentItemView;
            this.onSelect = onSelect;
            this.depth = depth;
            this.relativeObj = path[depth].relativeObj;
            text = path[depth].segment;
            nameForFiltering = text.ReplaceWhitespace("").ToLower();

            UniqueID = SearcherUtils.PathToID(path, depth);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            SetValueWithoutNotify(EditorStateManager.GetSearcherFoldoutState(UniqueID, defaultToggleValue));

            this.RegisterValueChangedCallback(OnFoldoutValueChanged);

            focusable = true;

            Toggle.focusable = false;
            //need to register these callbacks on toggle and not on this foldout because hovering on child foldout also triggers hover on parents;
            Toggle.RegisterCallback<MouseEnterEvent>((_) => OnHoverStart());
            Toggle.RegisterCallback<MouseLeaveEvent>((_) => OnHoverEnd());

            if (relativeObj != null)
            {
                AddIcon((Texture2D)EditorGUIUtility.ObjectContent(relativeObj, relativeObj.GetType()).image);
            }
        }

        private void AddIcon(Texture2D image)
        {
            if (iconContainer == null)
                return;

            if (!iconContainer.ClassListContains("searcher-node-group-icon"))
                iconContainer.AddToClassList("searcher-node-group-icon");

            iconContainer.style.backgroundImage = image;
        }

        private void OnFoldoutValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.target == this)
            {
                EditorStateManager.SetSearcherFoldoutState(UniqueID, evt.newValue);
                onSelect?.Invoke(this, false);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            //needed because milkshake uses an ancient unity version without bugfixes
#if UNITY_2021_1_OR_NEWER
            if (depth == 1)
                style.left = (depth - 2) * -Parameters.foldoutLayerWidth;
#endif
            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
                Toggle.Q<Label>().FixLightSkinLabel();

            var iconParent = Toggle.Children().First();
            if (iconParent != null)
            {
                iconParent.Add(iconContainer);

                if (iconParent.childCount >= 2)
                    iconParent.Children().ToList()[iconParent.childCount - 2].PlaceInFront(iconContainer);
                else
                    Debug.LogError("Foldout layout changed!!!");
            }
            else
            {
                Debug.LogError("Foldout layout changed!!!");
            }

            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void Select()
        {
            Selected = true;
            Toggle.style.backgroundColor = Parameters.seacherSelectableSelectedColor;
            LogicGraphUtils.Ping(relativeObj);
        }

        public void Deselect()
        {
            Selected = false;
            if (!Hovered)
                Toggle.style.backgroundColor = Color.clear;
            else
                Toggle.style.backgroundColor = Parameters.seacherSelectableHoverOnColor;
        }

        public void OnHoverStart()
        {
            Hovered = true;
            if (!Selected)
                Toggle.style.backgroundColor = Parameters.seacherSelectableHoverOnColor;
        }

        public void OnHoverEnd()
        {
            Hovered = false;
            if (!Selected)
                Toggle.style.backgroundColor = Color.clear;
        }

        internal bool Filter(string query)
        {
            if (nameForFiltering.Contains(query))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
