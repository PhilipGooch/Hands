using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameParameters", menuName = "ScriptableObjects/Game Parameters", order = 1)]
public class GameParameters : ScriptableObject
{
    static GameParameters instance;
    public static GameParameters Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameParameters>("GameParameters");
            }
            return instance;
        }
    }

    //[Header("Particles")]
    //public MeshParticleEffect meshFireParticles;
    //[Tooltip("Does not require mesh to work, simply follows the assigned object")]
    //public ContinuousParticleEffect continuousFireParticles;
    //public SingleShotParticleEffect waterParticles;
    //public SingleShotParticleEffect sheepSpawnEffect;
    //public SingleShotParticleEffect despawnEffect;
    //public SingleShotParticleEffect respawnEffect;

    [Header("Others")]
    public Color disabledUIElementsColor = new Color32(66, 111, 142, 255);

    //public Sheep sheepPrefab;
    public Mesh quadMesh;
    public Material objectOutlineMaterial;

    [Header("Threats")]
    public float objectSpeedToBecomeThreat = 1f;

    [Header("Place in front of player")]
    public float distanceFromCamera = 20f;
    public float positionChangeToMove = 12f;
    public float followSpeed = 2;
    public float firstMoveSpeed = 10;

    [Header("Haptics")]
    public float destructibleHitDuration = 0.1f;
    public int destructibleHitFrequency = 50;
    public float destructibleHitAmplitude = 0.1f;

}
