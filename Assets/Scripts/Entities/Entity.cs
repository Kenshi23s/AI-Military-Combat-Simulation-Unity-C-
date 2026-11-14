using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Base class of every unit of the simulation.
/// Forwards the ILifeObject members to its LifeComponent and gives the GameObject a random name.
/// </summary>
[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
[SelectionBase]
public abstract class Entity : MonoBehaviour, ILifeObject
{
    [SerializeField, Header("Entity")] private Transform AimingPoint;

    /// <summary>The LifeComponent of the entity.</summary>
    public LifeComponent Health { get; private set; }

    /// <summary>The DebugableObject of the entity.</summary>
    public DebugableObject DebugEntity { get; private set; }

    /// <summary>true while the entity captures a CapturePoint.</summary>
    public bool IsCapturing { get; private set; }

    /// <summary>Point other units aim at: AimingPoint, or the transform position.</summary>
    /// <remarks>Some models have their pivot away from the center.</remarks>
    public Vector3 AimPoint => AimingPoint != null
        ? AimingPoint.position
        : transform.position;

    #region Redirect To Health Component

    /// <summary>Raised when Health takes damage.</summary>
    public event Action OnTakeDamage;

    /// <summary>Raised when Health receives healing.</summary>
    public event Action OnHeal;

    /// <summary>Returns Health.IsAlive.</summary>
    public bool IsAlive => Health.IsAlive;

    /// <summary>Returns Health.MaxLife.</summary>
    public int MaxLife => Health.MaxLife;

    /// <summary>Returns Health.Life.</summary>
    public int Life => Health.Life;

    /// <summary>Life divided by MaxLife.</summary>
    public float NormalizedLife => (float)Life / MaxLife;

    /// <summary>Sets IsCapturing.</summary>
    public void SetCaptureState(bool arg)
    {
        IsCapturing = arg;
    }

    protected virtual void Awake()
    {
        Health = GetComponent<LifeComponent>();
        DebugEntity = GetComponent<DebugableObject>();
        IsCapturing = false;
        gameObject.name = GetType().Name + " - " + ColomboMethods.GenerateName(Random.Range(3,7));
        Health.OnTakeDamage += _ => OnTakeDamage?.Invoke();
        Health.OnHeal += () => OnHeal?.Invoke();
    }

    /// <summary>Calls Health.TakeDamage.</summary>
    public DamageData TakeDamage(int dmgToDeal) => Health.TakeDamage(dmgToDeal);

    /// <summary>Calls Health.TakeDamage with hitPoint.</summary>
    public DamageData TakeDamage(int dmgToDeal, Vector3 hitPoint) => Health.TakeDamage(dmgToDeal, hitPoint);

    /// <summary>Calls Health.AddKnockBack.</summary>
    public void AddKnockBack(Vector3 force) => Health.AddKnockBack(force);

    /// <summary>Calls Health.Position.</summary>
    public Vector3 Position() => Health.Position();

    /// <summary>Calls Health.Heal.</summary>
    public int Heal(int HealAmount) => Health.Heal(HealAmount);
    #endregion;
}

