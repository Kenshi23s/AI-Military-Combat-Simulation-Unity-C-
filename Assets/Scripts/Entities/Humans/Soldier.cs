using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public abstract class Soldier : Human, IMilitary , IZoneEntity
{
    [field : SerializeField]public MilitaryTeam Team { get; protected set; }

    protected GridEntity _gridEntity;

    public bool InCombat { get; protected set; }

    public bool CanCapture => Health.isAlive;

    public CapturePoint Zone { get; protected set; }
 
    public event Action onZoneEnter;
    public event Action onZoneStay;
    public event Action onZoneExit;

    protected override void EntityAwake()
    {
        _gridEntity = GetComponent<GridEntity>();

        SoldierAwake();
    }

    protected abstract void SoldierAwake();


    public void ZoneEnter(CapturePoint _zone)
    {
        Zone = _zone;

        onZoneEnter?.Invoke();
        DebugEntity.Log("ZoneEnter");
    }

    public void ZoneStay(CapturePoint _zone)
    {
        if (Zone != _zone) return;
        DebugEntity.Log("ZoneStay");
        onZoneStay?.Invoke();
    }

    public void ZoneExit(CapturePoint _zone)
    {
        if (Zone != _zone) return;        
            Zone = null;
        DebugEntity.Log("ZoneExit");

        onZoneExit?.Invoke();
    }
}
