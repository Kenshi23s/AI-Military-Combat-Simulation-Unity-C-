using UnityEngine;
using System;

/// <summary>Object that takes damage and knockback.</summary>
public interface IDamageable
{
    /// <summary>true while the object has health left.</summary>
    public bool IsAlive { get; }

    /// <summary>Applies dmgToDeal and returns the result of the hit.</summary>
    DamageData TakeDamage(int dmgToDeal);

    /// <summary>Applies dmgToDeal at hitPoint and returns the result of the hit.</summary>
    DamageData TakeDamage(int dmgToDeal, Vector3 hitPoint);

    /// <summary>Applies a knockback force.</summary>
    void AddKnockBack(Vector3 force);

    /// <summary>Returns the position of the object.</summary>
    Vector3 Position();
}

/// <summary>Result of a TakeDamage call.</summary>
public struct DamageData
{
    public int damageDealt;
    public bool wasKilled;
    public bool wasCrit;
    public IDamageable victim;
}

/// <summary>Object that receives healing.</summary>
public interface IHealable
{
    /// <summary>Adds HealAmount of health and returns the amount healed.</summary>
    int Heal(int HealAmount);
}

/// <summary>Object with health that takes damage and receives healing.</summary>
public interface ILifeObject : IDamageable, IHealable
{
    /// <summary>Life divided by MaxLife.</summary>
    public float NormalizedLife => (float)Life / MaxLife;

    /// <summary>Maximum health points.</summary>
    public int MaxLife { get; }

    /// <summary>Current health points.</summary>
    public int Life { get; }

    /// <summary>Raised when the object takes damage.</summary>
    public event Action OnTakeDamage;

    /// <summary>Raised when the object receives healing.</summary>
    public event Action OnHeal;
}
