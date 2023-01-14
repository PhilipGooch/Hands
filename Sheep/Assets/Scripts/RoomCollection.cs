using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public class RoomCollection : MonoBehaviour
{
    public static bool? beginAtStart;
    public Transform startingPuzzle;
    public static RoomCollection instance;
    SheepManager sheepManager;
    Transform[] rooms;
    int currentPuzzle = 0;
    private void OnEnable()
    {
        instance = this;
        rooms = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            rooms[i] = transform.GetChild(i);
            rooms[i].gameObject.SetActive(false);
            if (!beginAtStart.HasValue && startingPuzzle == rooms[i])
                currentPuzzle = i;
        }
        if (beginAtStart.HasValue)
            currentPuzzle = beginAtStart.Value ? 0 : rooms.Length - 1;

        rooms[currentPuzzle].gameObject.SetActive(true);

        var vr = FindObjectOfType<VRLocomotion>();
        vr.transform.position = rooms[currentPuzzle].transform.position.SetY(vr.transform.position.y);


    }
    private void OnDestroy()
    {
        instance = null;
    }



    public bool NextPuzzle()
    {
        if (currentPuzzle == rooms.Length - 1) return false;
        var vr = FindObjectOfType<VRLocomotion>();
        var oldStart = rooms[currentPuzzle].GetComponentInChildren<SheepManager>().transform.position;
        rooms[currentPuzzle].gameObject.SetActive(false);
        currentPuzzle = (currentPuzzle + 1) % rooms.Length;
        rooms[currentPuzzle].gameObject.SetActive(true);

        var newStart = rooms[currentPuzzle].GetComponentInChildren<SheepManager>().transform.position;
        vr.transform.position += (newStart - oldStart).ZeroY();
        //vr.transform.position = rooms[currentPuzzle].transform.position.SetY(vr.transform.position.y);
        return true;
    }

    public bool PreviousPuzzle()
    {
        if (currentPuzzle == 0) return false;
        var vr = FindObjectOfType<VRLocomotion>();
        var oldStart = rooms[currentPuzzle].GetComponentInChildren<SheepManager>().transform.position;
        rooms[currentPuzzle].gameObject.SetActive(false);
        currentPuzzle = (currentPuzzle + rooms.Length-1) % rooms.Length;
        rooms[currentPuzzle].gameObject.SetActive(true);
        var newStart = rooms[currentPuzzle].GetComponentInChildren<SheepManager>().transform.position;
        vr.transform.position += (newStart - oldStart).ZeroY();
        //vr.transform.position = rooms[currentPuzzle].transform.position.SetY(vr.transform.position.y);
        //return currentPuzzle != rooms.Length - 1;
        return true;
    }
}
