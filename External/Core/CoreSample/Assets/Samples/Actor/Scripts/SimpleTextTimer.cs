using NBG.LogicGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SimpleTextTimer : MonoBehaviour
{
    private Text myTimeText;
    private float timeSoFar = 0;

    private void Awake()
    {
        myTimeText = GetComponent<Text>();
    }

    [NodeAPI("ResetTimer")]
    public void ResetTimer()
    {
        timeSoFar = 0;
        myTimeText.text = Mathf.FloorToInt(timeSoFar).ToString();
    }

    private void Update()
    {
        timeSoFar += Time.deltaTime;
        myTimeText.text = Mathf.FloorToInt(timeSoFar).ToString();
    }
}
