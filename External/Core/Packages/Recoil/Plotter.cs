using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plotter : MonoBehaviour
{
    static Plotter instance;
    Camera cam;

    public int numberOfInstances = 0;

    private Vector2 size = new Vector2(0.4f, 0.2f);

    Dictionary<string, int> indexes;

    List<PlotGraph> graphs;

    public static bool commonGraph = false;

    public static float commonMax = -9999999.0f;
    public static float commonMin = 9999999.0f;

    private class PlotGraph
    {
        public float[] samples;
        public LineRenderer lineRenderer;
        public Material material;
        public float max = -9999999.0f;
        public float min = 9999999.0f;
    }

    public static void Plot(string key, float value, int sampleCount)
    {
        if (instance == null)
        {
            CreateInstance(key);
        }

        instance.PlotInternal(key, value, sampleCount);
    }

    public static void Plot(string key, float value, int sampleCount, Color color)
    {
        if (instance == null)
        {
            CreateInstance(key);
        }

        instance.PlotInternal(key, value, sampleCount, color);
    }

    private static void CreateInstance(string key)
    {
        GameObject go = new GameObject("Plotter");
        instance = go.AddComponent<Plotter>();
        instance.Initialize(key);
    }

    private void Initialize(string key)
    {
        indexes = new Dictionary<string, int>();
        graphs = new List<PlotGraph>();
        cam = Camera.main;

        NewGraph(key);
    }

    private void CheckKey(string key)
    {
        if (!indexes.ContainsKey(key))
        {
            NewGraph(key);
        }
    }

    public void NewGraph(string key)
    {
        indexes.Add(key, numberOfInstances);

        PlotGraph newGraph = new PlotGraph();

        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = Color.green;
        newGraph.material = material;


        GameObject newGo = new GameObject("Sub Plotter");
        newGo.transform.parent = transform;

        LineRenderer lineRenderer = newGo.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.002f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.material = material;

        newGraph.lineRenderer = lineRenderer;

        newGraph.samples = null;

        graphs.Add(newGraph);

        numberOfInstances++;
    }

    private void PlotInternal(string key, float value, int sampleCount, Color color)
    {
        CheckKey(key);

        Material material = graphs[indexes[key]].material;
        if (material.color != color)
            material.color = color;

        PlotInternal(key, value, sampleCount);
    }

    private void PlotInternal(string key, float value, int sampleCount)
    {
        CheckKey(key);

        int index = indexes[key];
        float[] samples = graphs[index].samples;

        if (samples == null || samples.Length != sampleCount)
        {
            samples = new float[sampleCount];
            graphs[index].samples = samples;
            graphs[index].lineRenderer.positionCount = sampleCount;
        }

        for (int i = 0; i < (sampleCount - 1); i++)
        {
            samples[i] = samples[i + 1];
        }

        samples[sampleCount - 1] = value;

        if (commonGraph)
        {
            commonMax = Mathf.Max(value, commonMax);
            commonMin = Mathf.Min(value, commonMin);
        }
        else
        {
            graphs[index].max = Mathf.Max(value, graphs[index].max);
            graphs[index].min = Mathf.Min(value, graphs[index].min);
        }
    }


    void Update()
    {
        Vector3 screenPos = new Vector3(0.0f, 0.0f, 1.0f);

        float screenHeight = Screen.height;

        if (cam == null)
        {
            Camera[] cams = FindObjectsOfType<Camera>();

            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i].name.Contains("PlayerFollowCam"))
                    cam = cams[i];
            }
            if (cam == null)
            {
                Debug.Log("Camera not found");
                return;
            }
        }
        for (int j = 0; j < numberOfInstances; j++)
        {
            float xIncrement = Screen.width / (float)graphs[j].samples.Length;

            LineRenderer lineRenderer = graphs[j].lineRenderer;
            float[] samples = graphs[j].samples;

            float max, min;

            if (commonGraph)
            {
                max = commonMax;
                min = commonMin;
            }
            else
            {
                max = graphs[j].max;
                min = graphs[j].min;
            }

            for (int i = 0; i < samples.Length; i++)
            {
                screenPos.x = i * xIncrement * size.x;

                float value = 0.0f;
                if (max != min)
                    value = (samples[i] - min) / (max - min);

                float verticalOffset = commonGraph ? 0.0f : screenHeight * size.y * j;
                screenPos.y = value * screenHeight * size.y + verticalOffset + screenHeight * 0.01f;

                lineRenderer.SetPosition(i, cam.ScreenToWorldPoint(screenPos));
            }
        }
    }

}
