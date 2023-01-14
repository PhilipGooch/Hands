using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using NBG.Core.Editor;
using System.Linq;
using UnityEditor;

public class LightmapUVsGeneratedForBakedObjectsTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.Strict;
    public override string Name => "There are no baked meshes that don't have lightmap UVs generated.";
    public override string Category => "Scene";

    public override bool CanFix { get; } = true;

    protected override Result OnRun(ILevel level)
    {
        List<MeshFilter> violations = FindViolations(level);

        var result = new Result();

        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    List<MeshFilter> FindViolations(ILevel level)
    {
        List<MeshFilter> violations = new List<MeshFilter>();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            var filters = root.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh != null && IsGIStatic(x.gameObject));
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                var uvs = mesh.uv2;
                if (uvs == null || uvs.Length == 0)
                {
                    PrintLog("Baked mesh does not have lightmap UVs", filter);
                    violations.Add(filter);
                }
            }
        }

        return violations;
    }

    bool IsGIStatic(GameObject go)
    {
        return (GameObjectUtility.GetStaticEditorFlags(go.gameObject) & StaticEditorFlags.ContributeGI) > 0;
    }

    protected override void OnFix(ILevel level)
    {
        var violatingObjects = FindViolations(level);

        if (violatingObjects.Count > 0)
        {
            Fix(violatingObjects);
        }

    }

    void Fix(List<MeshFilter> violatingObjects)
    {
        AssetDatabase.Refresh();
        Debug.Log("----[Fixing lightmap UV generation test]----");

        foreach(var violation in violatingObjects)
        {
            var path = AssetDatabase.GetAssetPath(violation.sharedMesh);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
                PrintLog($"Fixed {violation.sharedMesh.name}", importer);
            }
            else
            {
                PrintError($"Failed to fix {violation.sharedMesh.name}", violation.sharedMesh);
            }
        }
    }
}
