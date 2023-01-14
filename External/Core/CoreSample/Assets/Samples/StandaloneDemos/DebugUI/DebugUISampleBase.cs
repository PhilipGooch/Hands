#define ENABLE_COUNTER_TEST
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using NBG.DebugUI;
using System;

public abstract class DebugUISampleBase : MonoBehaviour
{
    public enum Enum1
    {
        Value1,
        Value2,
        Value3,
        Value4
    }

    IDebugUI _debugUI;

    public Enum1 TestEnum = Enum1.Value1;
    public bool TestBool = true;
    public float TestFloat = 13.37f;
    public int TestInt = 1337;

    public int TestIntCounter = 0;

    protected abstract IView CreateView();

    static LogType LogTypeFromVerbosity(Verbosity verbosity)
    {
        switch (verbosity)
        {
            case Verbosity.Info: return LogType.Log;
            case Verbosity.Warning: return LogType.Warning;
            case Verbosity.Error: return LogType.Error;
            default:
                throw new System.NotImplementedException();
        }
    }

    void Awake()
    {
        _debugUI = NBG.DebugUI.DebugUI.Get();
        _debugUI.SetView(CreateView());
        _debugUI.OnPrint += (message, verbosity) =>
        {
            Debug.LogFormat(LogTypeFromVerbosity(verbosity), LogOption.NoStacktrace, null, message);
        };

        _debugUI.RegisterAction("Test Print", "Sample A", () =>
        {
            _debugUI.Print(Guid.NewGuid().ToString(), (Verbosity)UnityEngine.Random.Range(0, 3));
        });

        _debugUI.RegisterEnum("Test Enum", "Sample A", typeof(Enum1),
            () => { return TestEnum; },
            (value) =>
            {
                // Debug.Log($"Change TestBool {TestBool}");
                TestEnum = (Enum1)value;
            }
        );
        _debugUI.RegisterObject("Show object (TestEnum)", "Sample A", () =>
        {
            return TestEnum;
        });

        _debugUI.RegisterBool("Show TestBool", "Sample A", () =>
        {
            //  Debug.Log($"TestBool {TestBool}");
            return TestBool;
        });
        _debugUI.RegisterFloat("Show TestFloat", "Sample A", () =>
        {
            //       Debug.Log($"TestFloat {TestFloat}");
            return TestFloat;
        });
        _debugUI.RegisterInt("Show TestInt", "Sample A", () =>
        {
            //  Debug.Log($"TestInt {TestInt}");
            return TestInt;
        });
        _debugUI.RegisterInt("Show TestIntCounter", "Sample A", () =>
        {
            return TestIntCounter;
        });

        _debugUI.RegisterBool("Change TestBool", "Sample A",
            () => { return TestBool; },
            (value) =>
            {
                // Debug.Log($"Change TestBool {TestBool}");
                TestBool = value;
            }
        );
        _debugUI.RegisterFloat("Change TestFloat (step 0.01)", "Sample A",
            () => { return TestFloat; },
            (value) =>
            {
                // Debug.Log($"Change TestFloat {TestFloat}");
                TestFloat = (float)value;
            },
            step: 0.01,
            minValue: 10.0,
            maxValue: 20.0
        );
        _debugUI.RegisterFloat("Change TestFloat (step 0.000001)", "Sample A",
            () => { return TestFloat; },
            (value) =>
            {
                // Debug.Log($"Change TestFloat {TestFloat}");
                TestFloat = (float)value;
            },
            step: 0.000001,
            minValue: 10.0,
            maxValue: 20.0
        );
        _debugUI.RegisterFloat("Change TestFloat (step 0.9)", "Sample A",
            () => { return TestFloat; },
            (value) =>
            {
                // Debug.Log($"Change TestFloat {TestFloat}");
                TestFloat = (float)value;
            },
            step: 0.9,
            minValue: 10.0,
            maxValue: 20.0
        );
        _debugUI.RegisterFloat("Change TestFloat (step 1.0)", "Sample A",
            () => { return TestFloat; },
            (value) =>
            {
                // Debug.Log($"Change TestFloat {TestFloat}");
                TestFloat = (float)value;
            },
            step: 1.0,
            minValue: 10.0,
            maxValue: 20.0
        );
        _debugUI.RegisterInt("Change TestInt", "Sample A",
            () => { return TestInt; },
            (value) =>
            {
                //  Debug.Log($"Change TestInt {TestInt}");
                TestInt = value;
            },
            step: 1,
            minValue: 0,
            maxValue: 200000
        );


        // Another test category
        _debugUI.RegisterAction("Test Print 1b (info)", "Sample B", () =>
        {
            _debugUI.Print("Test Print 1b", Verbosity.Info);
        });
        _debugUI.RegisterAction("Test Print 2b (warning)", "Sample B", () =>
        {
            _debugUI.Print("Test Print 2b", Verbosity.Warning);
        });
        _debugUI.RegisterAction("Test Print 3b (error)", "Sample B", () =>
        {
            _debugUI.Print("Test Print 3b", Verbosity.Error);
        });
        _debugUI.RegisterAction("Test Print 3b", "Sample B", () =>
        {
            _debugUI.Print("Test Print 3b");
        }); // Test identical name
        _debugUI.RegisterAction("Test Print 3b", "Sample B", () =>
        {
            _debugUI.Print("Test Print 3b");
        }); // Test identical name

        // Category with no interactive items
        _debugUI.RegisterBool("Show TestBool", "Non-interactives", () =>
        {
            //  Debug.Log($"TestBool {TestBool}");
            return TestBool;
        });
        _debugUI.RegisterInt("Show TestIntCounter", "Non-interactives", () =>
        {
            return TestIntCounter;
        });

        _debugUI.SetExtraInfoText("Super extra info v0.1");

     //   StartCoroutine(DynamicElementAddRemove());
    }

    IEnumerator DynamicElementAddRemove()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            var item = _debugUI.RegisterAction("Test Print 1b", "Sample B", () =>
              {
                  Debug.Log("Test Print 1b");
              });
            yield return new WaitForSeconds(3f);
            _debugUI.Unregister(item);
        }
    }
}
