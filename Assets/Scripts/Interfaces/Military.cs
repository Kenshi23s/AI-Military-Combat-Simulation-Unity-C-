using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.Mathematics;

public enum MilitaryTeam
{
    Blue,
    Red,
    None
}

public interface IMilitary 
{
    MilitaryTeam Team { get; }
    public int TotalDamageDealt { get; }
    public event Action OnDeathInCombat;
    
}

public interface ILifeObject
{
    public float NormalizedLife => (Life*1f) / (MaxLife*1f);
    public int MaxLife { get; }
    public int Life { get; }
    public event Action OnTakeDamage;
}

public interface ICapturePointEntity 
{
    void ZoneEnter(CapturePoint zone);
    void ZoneStay();
    void ZoneExit(CapturePoint zone);

    public bool CanCapture { get; }
    CapturePoint Zone { get; }
    bool IsInZone => Zone != null;
}

