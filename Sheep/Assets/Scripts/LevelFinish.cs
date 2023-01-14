using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelFinish : MonoBehaviour
{
    int targetSheepCount;
    int targetOptionalSheepCount;
    List<Sheep> savedSheep = new List<Sheep>();
    List<Sheep> savedOptionalSheep = new List<Sheep>();
    int sheepLayer;
    public static bool levelWon = false;
    int maxCount = 0;
    int optionalSheepMaxCount = 0;
    SheepManager sheepManager;

    // Start is called before the first frame update
    void Start()
    {
        sheepManager = FindObjectOfType<SheepManager>();
        targetSheepCount = sheepManager.count;
        targetOptionalSheepCount = sheepManager.blackSheepCount;
        sheepLayer = (int)Layers.SheepHead;
        levelWon = false;
    }
    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if(LayerUtils.IsPartOfLayer(other.gameObject.layer, sheepLayer))
        {
            var sheep = other.GetComponentInParent<Sheep>();
            if (!sheep.Optional)
            {
                savedSheep.Add(sheep);
                if (savedSheep.Count > maxCount)
                {
                    maxCount = savedSheep.Count;
                    AudioManager.instance.PlayPass(other.transform.position, savedSheep.Count);
                }
                if (maxCount == targetSheepCount)
                {
                    Victory();
                }
            }
            else
            {
                savedOptionalSheep.Add(sheep);
                optionalSheepMaxCount = Mathf.Max(optionalSheepMaxCount, savedOptionalSheep.Count);
                if (optionalSheepMaxCount == targetOptionalSheepCount)
                {
                    Debug.Log("You get an achievement for rescuing this creature!");
                }
            }
        }
    }

    private void Victory()
    {
        if (!levelWon)
        {
            levelWon = true;

            AudioManager.instance.PlaySuccess();
            SaveLevelCompleted();
            DoLevelEnd();
        }
    }
    public void SaveLevelCompleted()
    {
        var chapterId = LevelManager.Instance.GetCurrentChapter().id;
        var levelId = LevelManager.Instance.GetCurrentLevel().id;

        DataManager.Instance.SaveLevelComplete(Time.timeSinceLevelLoad, chapterId, levelId);
    }
    void DoLevelEnd()
    {
        LevelDoneScreen panel = PlayerUIManager.Instance.ShowPanel(MenuState.LEVEL_DONE) as LevelDoneScreen;
        panel.SetTimeSpent(Time.timeSinceLevelLoad);
    }

    private void OnTriggerExit(Collider other)
    {
        if (LayerUtils.IsPartOfLayer(other.gameObject.layer, sheepLayer))
        {
            var sheep = other.GetComponentInParent<Sheep>();
            if (!sheep.Optional)
            {
                savedSheep.Remove(sheep);
            }
            else
            {
                savedOptionalSheep.Remove(sheep);
            }
        }
    }
}
