using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Entity : MonoBehaviour
{
    public LifeComponent Health { get; private set; }
    public DebugableObject DebugEntity { get; private set; }
    public bool IsCapturing { get; private set; }


    // Donde es mi punto donde deben apuntar las otras unidades
    // (porque algunas cosas no tienen el pivote en el centro)
    public Vector3 AimPoint => AimingPoint != null 
        ? AimingPoint.position 
        : transform.position;
    [SerializeField,Header("Entity")] Transform AimingPoint;

    public void SetCaptureState(bool arg)
    {
        IsCapturing = arg;
    }

    private void Awake()
    {
        Health = GetComponent<LifeComponent>();
        DebugEntity = GetComponent<DebugableObject>();
        IsCapturing = false;
        gameObject.name = GetType().Name + ColomboMethods.GenerateName(6);
        EntityAwake();
    }

    protected virtual void EntityAwake() { }
}
