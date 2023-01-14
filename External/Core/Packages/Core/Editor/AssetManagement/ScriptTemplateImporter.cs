using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using System;

namespace NBG.Core.Editor
{
    /// <summary>
    /// Script template importer adds a menu item for manual activation.
    /// </summary>
    internal static class ScriptTemplateImporter
    {
        private const string kAssets = "Assets";
        private const string kTemplateFolder = "ScriptTemplates";

        private const string templateSearchQuery = "t:textasset";
        private const string kTemplateFileExtension = ".txt";

        private static ListRequest request;

        private static bool progressBarVisible = false;
        private static bool listeningActive = false;

        [MenuItem("No Brakes Games/Development/Import script templates")]
        private static void ParsePacakgesForScriptTemplates()
        {
            if (!listeningActive)
            {
                request = Client.List(true, false); // List packages installed for the project
                listeningActive = true;
                EditorApplication.update += OnRequestFinishCopyScriptTemplatesFromPackages;
            }
        }

        private static void CleanupWaitingOnRequestFinish()
        {
            if (progressBarVisible)
            {
                EditorUtility.ClearProgressBar();
                progressBarVisible = false;
            }

            if (listeningActive)
            {
                EditorApplication.update -= OnRequestFinishCopyScriptTemplatesFromPackages;
                listeningActive = false;
            }
        }

        private static void OnRequestFinishCopyScriptTemplatesFromPackages()
        {
            try
            {
                if (request.IsCompleted)
                {
                    if (request.Status == StatusCode.Success)
                    {
                        List<string> potentialTemplateFolders = new List<string>();
                        foreach (var package in request.Result)
                        {
                            potentialTemplateFolders.Add($"{package.assetPath}/{kTemplateFolder}");
                        }
                        HashSet<string> templatesInPackages = new HashSet<string>();
                        FindTemplatesInFolders(potentialTemplateFolders, templatesInPackages);


                        if (templatesInPackages.Count > 0)
                        {
                            if (!AssetDatabase.IsValidFolder($"{kAssets}/{kTemplateFolder}"))
                                AssetDatabase.CreateFolder(kAssets, kTemplateFolder);

                            foreach (string template in templatesInPackages)
                            {
                                string assetDestinationPath = $"{kAssets}/{kTemplateFolder}/{ Path.GetFileName(template)}";
                                if (File.Exists(assetDestinationPath))
                                {
                                    File.Copy(template, assetDestinationPath, true); // When asset exists, we use filesystem since AssetDatabase regenerates ".meta" with file copy.
                                }
                                else
                                {
                                    AssetDatabase.CopyAsset(template, assetDestinationPath);
                                }
                            }

                            AssetDatabase.Refresh();
                        }
                    }
                    else if (request.Status >= StatusCode.Failure)
                    {
                        Debug.Log(request.Error.message);
                    }

                    CleanupWaitingOnRequestFinish();
                }
                else
                {
                    progressBarVisible = true;
                    EditorUtility.DisplayProgressBar("Getting templates", $"Parsing packages to copy script templates: {request.Status}", -1f);
                }
            }
            catch (Exception e)
            {
                CleanupWaitingOnRequestFinish();
                throw e;
            }
        }

        private static void FindTemplatesInFolders(List<string> folderLocations, HashSet<string> outTemplatePaths)
        {
            string[] validFolders = folderLocations.Where(entry => AssetDatabase.IsValidFolder(entry)).ToArray();

            string[] assetsInValidFolderGuids = AssetDatabase.FindAssets(templateSearchQuery, validFolders);
            for (int i = 0; i < assetsInValidFolderGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetsInValidFolderGuids[i]);
                if (Path.GetExtension(assetPath).Equals(kTemplateFileExtension, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    outTemplatePaths.Add(assetPath);
                }
            }
        }
    }
}
