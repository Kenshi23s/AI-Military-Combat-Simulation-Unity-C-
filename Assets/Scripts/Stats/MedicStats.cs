using UnityEngine;

/// <summary>Stats of MedicInfantry.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Medic", fileName = "MedicStats")]
public class MedicStats : UnitStats
{
    /// <summary>Movement of the medic.</summary>
    public MovementStats Movement = new MovementStats();

    /// <summary>Field of view of the medic. ViewRadius is the search radius of wounded allies.</summary>
    public VisionStats Vision = new VisionStats();

    /// <summary>Distance to the Leader or the destination at which the medic stops.</summary>
    [Min(0)] public float MinDistanceFromDestination = 3f;

    /// <summary>Health points healed per second.</summary>
    [Min(0)] public float HealSpeed = 40f;

    /// <summary>Distance at which healing stops and the medic runs to the patient again.</summary>
    [Min(0)] public float MaxHealDistance = 2.5f;

    /// <summary>Preferred distance to the patient while healing.</summary>
    [Min(0)] public float IdealHealDistance = 1.5f;
}
