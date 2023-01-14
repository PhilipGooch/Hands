using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Displays GroupView(s)
    /// </summary>
    public class InspectorView : VisualElement
    {
        private const string k_UXMLGUID = "3fa8ab890513238469f0431bce00c076";
        public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> { }

        private AnimatorWindowAnimationData data;

        private readonly ScrollView elementsScrollView;

        internal event Action<float> onScroll;

        private readonly List<GroupView> groupViews;

        private readonly VisualElement topSpacer;

        public InspectorView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            groupViews = new List<GroupView>();

            elementsScrollView = this.Q<ScrollView>();
            elementsScrollView.contentContainer.Clear();

            elementsScrollView.verticalScroller.valueChanged += OnScroll;

            topSpacer = GetSpacer();
            elementsScrollView.Add(topSpacer);
        }

        //is this the best way of doing this??
        private VisualElement GetSpacer()
        {
            VisualElement spacer = new VisualElement();
            spacer.style.height = NoodleAnimatorParameters.rowHeight;
            return spacer;
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;

            ClearAll();


            foreach (var group in groupViews)
            {
                group.SetNewDataFile(data);
            }

            Update();
        }

        void ClearAll()
        {
            foreach (var item in groupViews)
            {
                elementsScrollView.Remove(item);
            }

            groupViews.Clear();
        }

        internal void Update()
        {
            CreateGroups();
        }

        private void OnScroll(float value)
        {
            onScroll?.Invoke(value);
        }

        internal void SetHorizontalScrollVisibility(bool state)
        {
            //need to force visibility
            elementsScrollView.horizontalScrollerVisibility = state ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Hidden;
        }

        internal float GetVerticalScrollValue()
        {
            return elementsScrollView.verticalScroller.value;
        }

        internal void SetVerticalScrollValue(float value)
        {
            elementsScrollView.verticalScroller.value = value;
        }

        internal void AddToVerticalScrollValue(float value)
        {
            elementsScrollView.verticalScroller.value += value;
        }

        private void CreateGroups()
        {
            if (data == null)
                return;

            int id = 0;

            foreach (var group in data.groupedTracks)
            {
                GroupView current = GetGroupView(id);

                current.Update(group.Key);

                id++;
            }
        }

        private GroupView GetGroupView(int id)
        {
            if (groupViews.Count > id)
                return groupViews[id];

            var groupView = new GroupView();
            groupView.SetNewDataFile(data);

            groupViews.Add(groupView);
            elementsScrollView.Add(groupView);

            return groupView;
        }
    }
}
