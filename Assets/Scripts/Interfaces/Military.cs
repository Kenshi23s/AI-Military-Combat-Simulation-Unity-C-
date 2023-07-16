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
}

public interface IZoneEntity 
{
    void ZoneEnter(CapturePoint zone);
    void ZoneStay(CapturePoint zone);
    void ZoneExit(CapturePoint zone);

    public bool CanCapture { get; }
    CapturePoint Zone { get; }
    bool IsInZone => Zone != null;
}

