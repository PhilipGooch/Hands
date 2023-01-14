using NBG.Core;
using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NonReadableMeshTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

    public override string Name => "There are no meshes which should be marked as readable";

    public override string Category => "Scene";

    List<GameObject> violations = new List<GameObject>();

    protected override Result OnRun(ILevel level)
    {
        FindViolations(level);

        var result = new Result();
        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    /// <summary>
    /// Currently only interested in flammable interactable entities IF level contains a fire source
    /// </summary>
    /// <param name="level"></param>
    void FindViolations(ILevel level)
    {
        violations.Clear();

        bool containsFireSource = false;
        foreach (var root in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
        {
            if (root.GetComponentInChildren<FireSource>() != null)
            {
                containsFireSource = true;
                break;
            }
        }

        if (containsFireSource)
        {
            foreach (var root in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                //currently we are only interested in burnable interactable entities.
                var entities = root.GetComponentsInChildren<InteractableEntity>().Where(x => x.physicalMaterial && x.physicalMaterial.Flammable == true);

                foreach (var entity in entities)
                {
                    var meshFilters = entity.GetComponentsInChildren<MeshFilter>();

                    foreach (var filter in meshFilters)
                    {
                        if (filter.sharedMesh && !filter.sharedMesh.isReadable)
                        {
                            string meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
                            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);

                            if (violations.Contains(asset))
                                continue;

                            violations.Add(asset);

                            PrintLog($"Non readable mesh {filter.sharedMesh.name}", asset);
                        }
                    }
                }
            }
        }
    }

}
