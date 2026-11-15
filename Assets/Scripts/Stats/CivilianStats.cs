using UnityEngine;

/// <summary>Stats of Civilian.</summary>
[CreateAssetMenu(menuName = "Unit Stats/Civilian", fileName = "CivilianStats")]
public class CivilianStats : UnitStats
{
    /// <summary>Movement of the civilian.</summary>
    public MovementStats Movement = new MovementStats();
}
