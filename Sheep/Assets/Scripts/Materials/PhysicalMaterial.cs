using NBG.Water;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PhysicalMaterial", menuName = "ScriptableObjects/PhysicalMaterial", order = 1)]
public class PhysicalMaterial : ScriptableObject, IFloatingMeshSettings
{
    //Nailing & puncturing
    [SerializeField]
    bool canBeNailedByHand = true;
    public bool CanBeNailedByHand { get { return canBeNailedByHand; } }
    [SerializeField]
    bool canBeAxed = false;
    public bool CanBeAxed { get { return canBeAxed; } }
    [SerializeField]
    bool granular;
    public bool Granular { get { return granular; } }

    //Water
    [SerializeField]
    FloatingMeshSimulationData floatingMeshData = FloatingMeshSimulationData.CreateDefault();
    public ref FloatingMeshSimulationData SimulationData => ref floatingMeshData;

    //Fire
    [SerializeField]
    bool flammable = false;
    public bool Flammable { get { return flammable; } }
    [SerializeField]
    float timeToIgnite = 2f;
    public float TimeToIgnite { get { return timeToIgnite; } }
    [SerializeField]
    bool burnable = false;
    public bool Burnable { get { return burnable; } }
    [SerializeField]
    float burnDuration = 5f;
    public float BurnDuration { get { return burnDuration; } }
    [SerializeField]
    bool canExtinguishWithMovement = false;
    public bool CanExtinguishWithMovement { get { return canExtinguishWithMovement; } }
    [SerializeField]
    bool canExtinguishWithWind = false;
    public bool CanExtinguishWithWind { get { return canExtinguishWithWind; } }
    [SerializeField]
    float velocityToExtinguishFire = 100;
    public float VelocityToExtinguishFire { get { return velocityToExtinguishFire; } }
    [SerializeField]
    bool canBurnout = false;
    public bool CanBurnout { get { return canBurnout; } }
    [SerializeField]
    bool despawnAfterBurnout = false;
    public bool DespawnAfterBurnout { get { return despawnAfterBurnout; } }
    [SerializeField]
    float timeUntilBurnout = 5;
    public float TimeUntilSelfExtinguish { get { return timeUntilBurnout; } }
    [SerializeField]
    bool spawnFireParticles = true;
    public bool SpawnFireParticles { get { return spawnFireParticles; } }
    [SerializeField]
    bool darkenColorWhenOnFire = true;
    public bool DarkenColorWhenOnFire { get { return darkenColorWhenOnFire; } }
    [SerializeField]
    Color maxColorTintFromFire = Color.black;
    public Color MaxColorTintFromFire { get { return maxColorTintFromFire; } }
    [SerializeField]
    float timeForFullTintFromFire = 5f;
    public float TimeForFullTintFromFire { get { return timeForFullTintFromFire; } }
    [SerializeField]
    bool createFireSourceWhenOnFire = true;
    public bool CreateFireSourceWhenOnFire { get { return createFireSourceWhenOnFire; } }

    //Magnets
    [SerializeField]
    bool magnetic = false;
    public bool Magnetic { get { return magnetic; } }

    //Eletricity
    [SerializeField]
    bool transfersElectricCurrent = false;
    public bool TransfersElectricCurrent { get { return transfersElectricCurrent; } }

}
