using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
[SelectionBase]
public abstract class Entity : MonoBehaviour, ILifeObject
{
    public LifeComponent Health { get; private set; }
    public DebugableObject DebugEntity { get; private set; }
    public bool IsCapturing { get; private set; }


    // Donde es mi punto donde deben apuntar las otras unidades
    // (porque algunas cosas no tienen el pivote en el centro)
    public Vector3 AimPoint => AimingPoint != null
        ? AimingPoint.position
        : transform.position;  

    [SerializeField, Header("Entity")] Transform AimingPoint; 

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

    #region Redirect To Health Component

    public event Action OnTakeDamage;
    public event Action OnHeal;

    public bool IsAlive => Health.IsAlive;

    public int MaxLife => Health.MaxLife;

    public int Life => Health.Life;

    public float NormalizedLife => (float)Life / MaxLife;

    public DamageData TakeDamage(int dmgToDeal) => Health.TakeDamage(dmgToDeal);

    public DamageData TakeDamage(int dmgToDeal, Vector3 hitPoint) => Health.TakeDamage(dmgToDeal, hitPoint);

    public void AddKnockBack(Vector3 force) => Health.AddKnockBack(force);

    public Vector3 Position() => Health.Position();

    public int Heal(int HealAmount) => Health.Heal(HealAmount);
    #endregion;
}

