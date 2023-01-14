using NBG.Core;
using NBG.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NBG.XPBDRope;

/// <summary>
/// Check for invalid components, currently only for MeshFilter and MeshRenderer
/// </summary>
public class UnusableComponentsTest : ValidationTest
{
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes;

    public override string Name => "There are no unusable components";

    public override string Category => "Scene";

    List<Component> violations = new List<Component>();

    protected override Result OnRun(ILevel level)
    {
        FindViolations(level);

        var result = new Result();
        result.Count = violations.Count;
        result.Status = violations.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }

    private void FindViolations(ILevel level)
    {
        violations.Clear();
        foreach (var root in ValidationTests.GetAllRootsFromAllScenes(level))
        {
            //mesh filter may not have a mesh if its part of cloud system
            var meshFilters = root.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh == null && x.GetComponent<CloudSystem>() == null && x.GetComponent<RopeRenderer>() == null);
            IEnumerable<MeshRenderer> renderers;
            foreach (var meshFilter in meshFilters)
            {
                violations.Add(meshFilter);
                PrintLog("Mesh Filter has no mesh", meshFilter);
                renderers = meshFilter.GetComponents<MeshRenderer>();
                violations.AddRange(renderers);

                foreach (var renderer in renderers)
                {
                    PrintLog("Mesh Renderer on object with no mesh", renderer);
                }
            }

            renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials.Count() == 0)
                {
                    violations.Add(renderer);
                    PrintLog("Mesh Renderer with no materials", renderer);
                }

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        violations.Add(renderer);
                        PrintLog("Mesh Renderer with invalid material(s)", renderer);
                    }
                }

            }
        }
    }
}
