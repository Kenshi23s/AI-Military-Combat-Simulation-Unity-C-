using UnityEngine;

/// <summary>Stats of AssaultInfantry.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Assault", fileName = "AssaultStats")]
public class AssaultStats : UnitStats
{
    /// <summary>Weapon of the soldier.</summary>
    public WeaponStats Weapon = new WeaponStats();

    /// <summary>Movement of the soldier.</summary>
    public MovementStats Movement = new MovementStats();

    /// <summary>Field of view of the soldier.</summary>
    public VisionStats Vision = new VisionStats();

    /// <summary>Distance to the Leader or the destination at which the soldier stops.</summary>
    [Min(0)] public float MinDistanceFromDestination = 3f;

    /// <summary>Seconds between two target selections while firing.</summary>
    [Min(0.05f)] public float TimeBeforeSelectingTarget = 0.5f;
}
