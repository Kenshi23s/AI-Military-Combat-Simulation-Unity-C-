using System;
using UnityEngine;

/// <summary>
/// Base asset of the stats of a unit type. Every unit of that type reads the same asset.
/// Changed is raised when the asset is edited in the Inspector, so the units in Play mode apply the new values.
/// </summary>
public abstract class UnitStats : ScriptableObject
{
    /// <summary>Health of the unit.</summary>
    public HealthStats Health = new HealthStats();

    /// <summary>Raised when the asset is edited in the Inspector.</summary>
    public event Action Changed;

    private void OnValidate()
    {
        Changed?.Invoke();
    }
}
