using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using TMPro;

public class LoadingScreen : MenuScreen
{
    [SerializeField]
    PlaceInfrontOfPlayer placeInFront;
    [SerializeField]
    TextMeshProUGUI loadingText;
    [SerializeField]
    float textChangeFrequency = 0.5f;

    int currentText = 0;
    float timer = 0f;

    string[] loadingTexts = new string[]
    {
        "Loading.",
        "Loading..",
        "Loading..."
    };

    private void OnEnable()
    {
        currentText = 0;
        timer = 0f;
        loadingText.text = loadingTexts[currentText];
    }

    private async void Start()
    {
        await UICamera.Instance.FadeToBlack();
        placeInFront.PlaceInFrontOfPlayer();
        await LevelManager.Instance.LoadMainMenuAsync(false);
        await UICamera.Instance.FadeFromBlack();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > textChangeFrequency)
        {
            timer = 0;
            currentText = (currentText + 1) % loadingTexts.Length;
            loadingText.text = loadingTexts[currentText];
        }
    }

    private void OnValidate()
    {
        if (!placeInFront)
        {
            placeInFront = GetComponent<PlaceInfrontOfPlayer>();
        }
    }
}
