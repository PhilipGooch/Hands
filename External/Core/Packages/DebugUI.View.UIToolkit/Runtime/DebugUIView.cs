using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;

namespace NBG.DebugUI.View.UIToolkit
{
    public enum CanvasType
    {
        ScreenOverlay,
        RenderTexture
    }
    
    [RequireComponent(typeof(UIDocument))]

    internal class DebugUIView : MonoBehaviour, IView
    {
        private VisualElement root;
        private VisualElement catParent;

        private TextElement catName;
        private TextElement extraInfo;

        bool hidden;

        Dictionary<string, DebugUIViewCategory> categories = new Dictionary<string, DebugUIViewCategory>();
        DebugUILog debugUILog;

        string lastCatSelection;
        int lastItemSelection;

        StringBuilder categoryName = new StringBuilder(100, 100);

        public void Setup()
        {
            if (root == null)
            {
                root = GetComponent<UIDocument>().rootVisualElement;
                root = root.Q<VisualElement>("root");

                catName = root.Q<TextElement>("catName");
                extraInfo = root.Q<TextElement>("extraInfo");
                catParent = root.Q<VisualElement>("catParent");

                extraInfo.text = string.Empty;

                debugUILog = new DebugUILog(root);

                Hide();
            }
        }
        void Update()
        {
            if (!hidden)
                debugUILog.CheckForOutOfDateMessages();
        }

        public void UpdateExtraInfo(string text)
        {
            extraInfo.text = text;
        }
        public void UpdateView(IDebugTree tree)
        {
            int id = 0;

            foreach (var category in tree.Categories)
            {
                DebugUIViewCategory viewCategory;
                if (!categories.TryGetValue(category, out viewCategory))
                {
                    viewCategory = new DebugUIViewCategory(catParent, id == 0);
                    categories.Add(category, viewCategory);
                }

                viewCategory.RewriteText(tree.GetItems(category));

                id++;
            }

        }

        public void UpdateSelection(string category, int itemID)
        {
            if (category != lastCatSelection || lastItemSelection != itemID)
            {
                if (category != lastCatSelection)
                {
                    categoryName.Clear();
                    categoryName.Append("<b><<");
                    categoryName.Append(category);
                    categoryName.Append(">></b>");

                    catName.text = categoryName.ToString();

                    if (!string.IsNullOrWhiteSpace(lastCatSelection))
                        categories[lastCatSelection].Hide();

                    categories[category].Show();
                }

                categories[category].UpdateSelection(itemID);

                lastCatSelection = category;
                lastItemSelection = itemID;
            }
        }

        public void PushLog(string message, Verbosity verbosity)
        {
            debugUILog.UpdateLog(message, verbosity);
        }

        public void Show()
        {
            hidden = false;
            //root.visible = open; // Performance: it is more efficient to move the UI out of view than hide it
            root.transform.position = Vector3.zero;
        }

        public void Hide()
        {
            hidden = true;
            //root.visible = open; // Performance: it is more efficient to move the UI out of view than hide it
            root.transform.position = new Vector3(-10000, -10000, -10000);
        }
    }

    public static class UIToolkitView
    {
        [NBG.Core.ClearOnReload]
        private static DebugUIView instance;

        const string kGameObjectName = "NBG_DEBUG_UI_VIEW_UITOOLKIT";

        public static IView Get(CanvasType canvasType)
        {
            if (instance == null)
            {
                Assert.IsNull(GameObject.Find(kGameObjectName));

                GameObject uiPref = Resources.Load<GameObject>($"NBG.DebugUI/DebugUIPrefab{canvasType}");

                var go = UnityEngine.Object.Instantiate(uiPref);
                go.name = kGameObjectName;
                go.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(go);

                instance = go.GetComponent<DebugUIView>();
                instance.Setup();
            }
            
            return instance;
        }
    }
}
