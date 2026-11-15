using UnityEngine;

/// <summary>Stats of Turret.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Turret", fileName = "TurretStats")]
public class TurretStats : UnitStats
{
    /// <summary>Weapon of the turret. BulletCooldown is the time between shots.</summary>
    public WeaponStats Weapon = new WeaponStats();

    /// <summary>Field of view of the turret.</summary>
    public VisionStats Vision = new VisionStats();

    /// <summary>Speed of the base and canon rotation.</summary>
    [Min(0)] public float RotationSpeed = 5f;

    /// <summary>Angle at which the base counts as aligned.</summary>
    [Min(0)] public float BaseMinAngle = 5f;

    /// <summary>Angle at which the canon counts as aligned.</summary>
    [Min(0)] public float CanonMinAngle = 5f;

    /// <summary>Seconds between two searches of enemy planes.</summary>
    [Min(0)] public float SearchCooldown = 0.5f;
}
