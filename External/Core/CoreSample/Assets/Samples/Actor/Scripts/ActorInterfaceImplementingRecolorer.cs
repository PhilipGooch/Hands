using NBG.Actor;
using UnityEngine;


[RequireComponent(typeof(MeshRenderer))]
public class ActorInterfaceImplementingRecolorer : MonoBehaviour, ActorSystem.IActorCallbacks
{
    void ActorSystem.IActorCallbacks.OnAfterDespawn()
    {
        Color randomColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        GetComponent<MeshRenderer>().material.SetColor("_Color", randomColor);
    }

    void ActorSystem.IActorCallbacks.OnAfterSpawn() { }
}
