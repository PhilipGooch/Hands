using NBG.Entities;
using Noodles;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public interface IPlayerControls<TDevice>
{
    void AddDevice(TDevice device);
    bool ContainsDevice(TDevice device);
    void RemoveDevice(TDevice device);
}
public abstract class PlayerControllerBase : MonoBehaviour
{
    [NonSerialized]
    public Entity entity;

    public void OnCreate(Entity entity)
    {
        this.entity = entity;

    }
    public abstract void OnCreate();
    public abstract void Dispose();
}
public abstract class PlayerManagerBase<TPlayerController, TControls, TDevice> : MonoBehaviour
    where TPlayerController : PlayerControllerBase
    where TControls : IPlayerControls<TDevice>
{
    public static PlayerManagerBase<TPlayerController, TControls, TDevice> instance;

    [SerializeField] internal bool m_AllowJoining = true;

    public TPlayerController playerPrefab;
    public int maxPlayers = 4;
    public List<TPlayerController> players = new List<TPlayerController>();

    public abstract TControls CreateControls(bool mainPlayer);
    public virtual void OnCreate()
    {
        if (instance == null)
            instance = this;
        else
        {
            Debug.LogWarning("Multiple PlayerManagers in the game. There should only be one PlayerManager", this);
            return;
        }


        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
        //maxPlayers = cameras.Length;

        // Create default player
        var player0 = AddPlayer(new float3(0, 2, 0));

        // Assign all joysticks to a Player 0
        var controls = CreateControls(mainPlayer: true);

        EntityStore.AddComponentObject(player0.entity, controls);

        CameraManagerBase.GetOrCreateInstance().AddCamera(player0.entity);
    }



    public virtual void Dispose()
    {
        if (instance == this)
            instance = null;

        for (int i = players.Count - 1; i >= 0; i--)
            RemovePlayer(i);
    }

    public void OnDeviceAdded(TDevice device)
    {
        if (players.Count > 0)
        {
            var controls0 = EntityStore.GetComponentObject<TControls>(players[0].entity);
            controls0.AddDevice(device); // just add to default player
        }
    }
    public void OnDeviceRemoved(TDevice device)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var controls = EntityStore.GetComponentObject<TControls>(player.entity, optional: true);
            if (controls != null && controls.ContainsDevice(device))
            {
                controls.RemoveDevice(device);
                if (i > 0)
                    RemovePlayer(i);
            }
        }


    }


    public void OnJoinPressed(TDevice device)
    {
        for (int i = 1; i < players.Count; i++)
        {
            var player = players[i];
            var controls = EntityStore.GetComponentObject<TControls>(player.entity, optional: true);
            if (controls != null && controls.ContainsDevice(device))
            {
                RemovePlayer(i);
                EntityStore.GetComponentObject<TControls>(players[0].entity).AddDevice(device);// add to player0
                return;
            }
        }
        {
            var player = AddPlayer(new float3(0, 2, 0));
            if (player != null)
            {
                // also create camera
                CameraManagerBase.GetOrCreateInstance().AddCamera(player.entity);

                EntityStore.GetComponentObject<TControls>(players[0].entity).RemoveDevice(device);// remove from player0

                // and give to this one
                var controls = CreateControls(false);
                controls.AddDevice(device);
                EntityStore.AddComponentObject(player.entity, controls);
            }
        }
    }

    public TPlayerController AddPlayer(float3 pos)
    {
        if (players.Count >= maxPlayers)
            return null;

        var player = playerPrefab;
        if (players.Count > 0 || !player.gameObject.scene.IsValid())
            player = Instantiate(playerPrefab, pos, Quaternion.identity);
        else
        {
            // ALT1. make a copy if in scene, somewhat broken on copying
            //playerPrefab = Instantiate(playerPrefab);
            // ALT2. just instantiate at same place as prefab
            player = Instantiate(playerPrefab, playerPrefab.transform.position, Quaternion.identity);

            playerPrefab.gameObject.SetActive(false);
        }
        if (!player.gameObject.activeSelf)
            player.gameObject.SetActive(true);
        player.OnCreate();
        players.Add(player);

        return player;
    }
    public void RemovePlayer(int i)
    {
        CameraManagerBase.GetOrCreateInstance().RemoveCamera(players[i].entity);
        players[i].Dispose();
        Destroy(players[i].gameObject);
        players.RemoveAt(i);
    }
}

