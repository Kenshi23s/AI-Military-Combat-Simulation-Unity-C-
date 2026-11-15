using UnityEngine;

/// <summary>Stats of Plane.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Plane", fileName = "PlaneStats")]
public class PlaneStats : UnitStats
{
    /// <summary>Machine gun of the plane.</summary>
    public WeaponStats Weapon = new WeaponStats();

    /// <summary>Movement of the plane. DestinationArriveDistance is unused.</summary>
    public MovementStats Movement = new MovementStats();

    /// <summary>Field of view of the plane. ViewRadius is also the flocking radius.</summary>
    public VisionStats Vision = new VisionStats();

    /// <summary>Distance at which a pursued plane escapes.</summary>
    [Min(0)] public float LoseSightRadius = 100f;

    /// <summary>Flocking weights with the allied planes.</summary>
    [Range(0f, 3f)] public float AlignmentForce = 1f;
    [Range(0f, 3f)] public float SeparationForce = 1f;
    [Range(0f, 3f)] public float CohesionForce = 1f;

    /// <summary>Launch settings of the missiles. owner and Team are set by the plane.</summary>
    public Misile.MisileStats Misile;

    /// <summary>Seconds between two missiles while pursuing.</summary>
    [Min(0)] public float MisileCooldown = 2f;

    /// <summary>Horizontal distance to the air strike point at which the plane launches the missile.</summary>
    [Min(0)] public float MinimumDistanceForStrike = 23f;

    /// <summary>Damage of the explosion when the plane crashes.</summary>
    [Min(0)] public int ExplosionDamageOnDeath = 50;

    /// <summary>Distance behind the target the plane pursues from.</summary>
    [Min(0)] public float UnitsBehindPlane = 30f;

    /// <summary>Base length of the ground check in front of the plane.</summary>
    [Min(0)] public float CollisionCheckDistance = 30f;

    /// <summary>Maximum seconds between two direction changes while fleeing.</summary>
    [Min(0)] public float RandomDirectionCooldown = 1f;
}
