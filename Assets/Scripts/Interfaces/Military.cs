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
    void ZoneEnter() { }
    void ZoneStay() { }
    void ZoneExit() { }

    CapturePoint Zone { get; set; }
    bool IsInZone => Zone != null;
}

