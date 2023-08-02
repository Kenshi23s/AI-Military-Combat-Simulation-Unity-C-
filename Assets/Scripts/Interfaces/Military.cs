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

public interface ICapturePointEntity 
{
    void PointEnter(CapturePoint point);
    void PointStay();
    void PointExit(CapturePoint point);

    public bool CanCapture { get; }
    CapturePoint Point { get; }
    bool IsInZone => Point != null;
}