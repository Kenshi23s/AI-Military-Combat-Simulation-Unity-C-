using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public abstract class Soldier : Human, IMilitary, ICapturePointEntity
{
    [field : SerializeField, Header("Soldier")] public MilitaryTeam Team { get; protected set; }

    protected GridEntity _gridEntity;

    public bool InCombat { get; protected set; }

    public bool CanCapture => Health.IsAlive;

    public CapturePoint Zone { get; protected set; }
 
    public event Action OnZoneEnter = delegate { };
    public event Action OnZoneStay = delegate { };
    public event Action OnZoneExit = delegate { };

    protected override void EntityAwake()
    {
        _gridEntity = GetComponent<GridEntity>();
        SoldierAwake();
    }

    protected abstract void SoldierAwake();


    public void ZoneEnter(CapturePoint zone)
    {
        Zone = zone;
        DebugEntity.Log("ZoneEnter");
        OnZoneEnter();
    }

    public void ZoneStay()
    {
        DebugEntity.Log("ZoneStay");
        OnZoneStay();        
    }

    public void ZoneExit(CapturePoint zone)
    {
        Zone = null;
        DebugEntity.Log("ZoneExit");
        OnZoneExit();
    }
}
