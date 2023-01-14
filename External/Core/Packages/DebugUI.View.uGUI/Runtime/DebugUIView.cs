using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Text;
using UnityEngine.Assertions;

namespace NBG.DebugUI.View.uGUI
{
    internal class DebugUIView : MonoBehaviour, IView
    {
        [SerializeField]
        TMP_Text catNameTxt;
        [SerializeField]
        TMP_Text extraInfoTxt;
        [SerializeField]
        DebugUILog debugUILog;
        [SerializeField]
        DebugUIUGUICategory categoryPrefab;
        [SerializeField]
        Transform catParent;

        StringBuilder categoryName = new StringBuilder(100, 100);

        Dictionary<string, DebugUIUGUICategory> categories = new Dictionary<string, DebugUIUGUICategory>();

        string lastCatSelection;
        int lastItemSelection;

        public void Setup() {}

        public void UpdateExtraInfo(string text)
        {
            extraInfoTxt.text = text;
        }

        public void UpdateView(IDebugTree tree)
        {
            int id = 0;

            foreach (var category in tree.Categories)
            {
                DebugUIUGUICategory viewCategory;
                if (!categories.TryGetValue(category, out viewCategory))
                {
                    viewCategory = Instantiate(categoryPrefab, catParent);
                    if (id == 0)
                        viewCategory.Show();
                    else
                        viewCategory.Hide();

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

                    catNameTxt.text = categoryName.ToString();

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

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);

        }
    }

    public static class UGUIView
    {
        [NBG.Core.ClearOnReload]
        private static DebugUIView instance;

        const string kGameObjectName = "NBG_DEBUG_UI_VIEW_UGUI";

        //screen space canvas
        public static IView GetScreenSpace()
        {
            if (instance == null)
            {
                GameObject.Destroy(GameObject.Find(kGameObjectName));

                instance = CreateNewInstance("DebugUIUGUIScreenSpacePrefab");
            }

            return instance;
        }

        public static IView GetWorldSpace(Transform attachTo, Camera cam, float followDistance)
        {
            if (instance == null)
            {
                GameObject.Destroy(GameObject.Find(kGameObjectName));

                instance = CreateNewInstance("DebugUIUGUIWorldSpacePrefab");

                instance.GetComponent<Canvas>().worldCamera = cam;
                instance.GetComponent<FollowObject>().Setup(attachTo, followDistance);
            }

            return instance;
        }

        static DebugUIView CreateNewInstance(string prefabName)
        {
            GameObject uiPref = Resources.Load<GameObject>($"NBG.DebugUI/{prefabName}");

            var go = UnityEngine.Object.Instantiate(uiPref);
            go.name = kGameObjectName;
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            GameObject.DontDestroyOnLoad(go);

            var instance = go.GetComponent<DebugUIView>();
            instance.Setup();
            instance.Hide();

            return instance;
        }
    }
}
