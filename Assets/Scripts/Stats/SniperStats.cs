using UnityEngine;

/// <summary>Stats of Sniper.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Sniper", fileName = "SniperStats")]
public class SniperStats : UnitStats
{
    /// <summary>Weapon of the sniper.</summary>
    public WeaponStats Weapon = new WeaponStats();

    /// <summary>Field of view of the sniper.</summary>
    public VisionStats Vision = new VisionStats();

    /// <summary>Frames between two enemy searches.</summary>
    [Min(1)] public int FramesBetweenEnemySearch = 4;

    /// <summary>Speed of the aim rotation.</summary>
    [Min(0)] public float AimSpeed = 1f;

    /// <summary>Focus gained per second for each shot in a row.</summary>
    [Min(0)] public float AddPerTimesFocused = 1f;

    /// <summary>Focus needed for each shot.</summary>
    [Min(0)] public float RequiredFocusTime = 1f;

    /// <summary>Shots in a row before the sniper aims again.</summary>
    [Min(1)] public float MaxShootsInRow = 1f;
}
