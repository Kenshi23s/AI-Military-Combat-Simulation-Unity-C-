using System;
using UnityEngine;

/// <summary>Health values applied to a LifeComponent.</summary>
[Serializable]
public class HealthStats
{
    /// <summary>Maximum health points.</summary>
    [Min(1)] public int MaxLife = 100;

    /// <summary>Multiplier of the damage taken.</summary>
    [Range(0.1f, 2f)] public float DamageMultiplier = 1f;

    /// <summary>Divisor of the damage taken.</summary>
    [Min(1)] public int DamageResist = 1;
}

/// <summary>Weapon values applied to a ShootComponent.</summary>
[Serializable]
public class WeaponStats
{
    /// <summary>Damage of each bullet.</summary>
    [Min(0)] public int BulletDamage = 10;

    /// <summary>Maximum spread of each bullet, in degrees.</summary>
    [Range(0f, 180f)] public float BulletSpread = 2f;

    /// <summary>Bullets in each burst.</summary>
    [Min(1)] public float BulletsPerBurst = 3f;

    /// <summary>Seconds between two bullets of a burst.</summary>
    [Min(0)] public float BulletCooldown = 0.3f;

    /// <summary>Seconds between two bursts.</summary>
    [Min(0)] public float BurstCooldown = 3f;
}

/// <summary>Movement values applied to NewPhysicsMovement and NewAIMovement.</summary>
[Serializable]
public class MovementStats
{
    /// <summary>Velocity change per second.</summary>
    [Min(0)] public float Acceleration = 37.5f;

    /// <summary>Maximum speed.</summary>
    [Min(0)] public float MaxSpeed = 8f;

    /// <summary>Distance to a point at which the unit counts it as reached.</summary>
    [Min(0)] public float DestinationArriveDistance = 2f;
}

/// <summary>Field of view values applied to a FOVAgent.</summary>
[Serializable]
public class VisionStats
{
    /// <summary>Maximum distance of the field of view.</summary>
    [Min(0)] public float ViewRadius = 30f;

    /// <summary>Angle of the field of view, in degrees.</summary>
    [Range(0f, 360f)] public float ViewAngle = 120f;
}
