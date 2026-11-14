using System;

/// <summary>Teams of the match. None marks an entity without a team.</summary>
public enum MilitaryTeam
{
    Blue,
    Red,
    None
}

/// <summary>Combat unit of a team.</summary>
public interface IMilitary
{
    /// <summary>Team of the unit.</summary>
    MilitaryTeam Team { get; }

    /// <summary>Damage the unit has dealt.</summary>
    public int TotalDamageDealt { get; }

    /// <summary>Raised when the unit dies.</summary>
    public event Action OnDeathInCombat;
}

/// <summary>Entity that a CapturePoint counts while it is inside the point.</summary>
public interface ICapturePointEntity
{
    /// <summary>Called by the CapturePoint when the entity enters it.</summary>
    void PointEnter(CapturePoint point);

    /// <summary>Called by the CapturePoint while the entity stays inside it.</summary>
    void PointStay();

    /// <summary>Called by the CapturePoint when the entity leaves it.</summary>
    void PointExit(CapturePoint point);

    /// <summary>true while the entity counts for the capture.</summary>
    public bool CanCapture { get; }

    /// <summary>The CapturePoint the entity is inside of.</summary>
    CapturePoint Point { get; }

    /// <summary>true while Point is set.</summary>
    bool IsInZone => Point != null;
}
