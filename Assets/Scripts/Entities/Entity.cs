using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Entity : MonoBehaviour, IDamagable, IHealable
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

    private void Awake()
    {
        Health = GetComponent<LifeComponent>();
        DebugEntity = GetComponent<DebugableObject>();
        IsCapturing = false;
        gameObject.name = GetType().Name + " - " + ColomboMethods.GenerateName(6);
        EntityAwake();
    }

    protected virtual void EntityAwake() { }

    #region Redirect To Health Component
    public bool IsAlive => Health.IsAlive;

    public DamageData TakeDamage(int dmgToDeal) => Health.TakeDamage(dmgToDeal);

    public DamageData TakeDamage(int dmgToDeal, Vector3 hitPoint) => Health.TakeDamage(dmgToDeal, hitPoint);

    public void AddKnockBack(Vector3 force) => Health.AddKnockBack(force);

    public Vector3 Position() => Health.Position();

    public int Heal(int HealAmount) => Health.Heal(HealAmount);
    #endregion;
}

