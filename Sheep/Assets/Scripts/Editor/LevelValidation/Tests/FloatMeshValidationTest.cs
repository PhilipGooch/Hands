using NBG.Core.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using NBG.Core;
using NBG.Water;

public class FloatMeshValidationTest : ValidationTest
{
    enum ViolationType
    {
        NO_ENTITY,
        NO_PHYS_MAT,
        NO_MESH,
    }
    struct Violation
    {
        public GameObject obj;
        public ViolationType type;

        public Violation(GameObject obj, ViolationType type)
        {
            this.obj = obj;
            this.type = type;
        }
    }
    public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes;
    public override string Name => "There are no badly configured float meshes in a scene with water";
    public override bool CanFix { get; } = true;
    public override bool AutoRerunAfterFix { get; } = false;
    public override string Category => "Water";


    List<Violation> FindViolations(ILevel level)
    {
        var violatingObjects = new List<Violation>();

        bool isWaterLevel = false;

        List<GameObject> roots = ValidationTests.GetAllRootsFromAllScenes(level).ToList();

        foreach (var root in roots)
        {
            if (!isWaterLevel && root.GetComponentInChildren<BodyOfWater>())
            {
                isWaterLevel = true;
            }
        }

        if (!isWaterLevel)
        {
            Debug.Log($"NOT A WATER LEVEL");
            return violatingObjects;
        }

        foreach (var root in roots)
        {
            foreach (var grab in root.GetComponentsInChildren<GrabParamsBinding>())
            {
                var entity = grab.gameObject.GetComponentInChildren<InteractableEntity>();
                //has entity and buoyancy is set either in physical mat or in entity
                if (entity != null)
                {
                    if (entity.physicalMaterial != null)
                    {
                        if (entity.useWaterSystem && ((IFloatingMesh)(entity.floatingSystem )).HullGameObject == null)
                        {
                            PrintLog($"No Hull GameObject!", entity.gameObject);
                            violatingObjects.Add(new Violation(entity.gameObject, ViolationType.NO_MESH));
                        }
                    }
                }
            }
        }
        return violatingObjects;
    }

    protected override Result OnRun(ILevel level)
    {
        var violatingObjects = FindViolations(level);

        Result result = new Result();
        result.Count = violatingObjects.Count;
        result.Status = violatingObjects.Count > 0 ? ValidationTestStatus.Failure : ValidationTestStatus.OK;

        return result;
    }


    protected override void OnFix(ILevel level)
    {
        var violatingObjects = FindViolations(level);

        if (violatingObjects.Count > 0)
        {
            Fix(violatingObjects);
        }

    }

    void Fix(List<Violation> violatingObjects)
    {
        int group = -1;
        Debug.Log("----[Fixing floating mesh validation test]----");


        foreach (var violation in violatingObjects)
        {
            if (violation.type == ViolationType.NO_ENTITY)
            {
                Undo.AddComponent<InteractableEntity>(violation.obj);
                if (group == -1)
                    group = Undo.GetCurrentGroup();

                PrintLog($"[ADDED ENTITY]", violation.obj);

                if (PrefabUtility.IsPartOfPrefabInstance(violation.obj))
                {
                    GameObject origin = PrefabUtility.GetCorrespondingObjectFromOriginalSource(violation.obj);

                    Undo.AddComponent<InteractableEntity>(origin);


                }
            }
        }

        if (group != -1)
            Undo.CollapseUndoOperations(group);
    }
}
