using System;
using UnityEngine;

/// <summary>Quantities of one team at the start of the match.</summary>
[Serializable]
public class TeamMatchSettings
{
    /// <summary>Fireteams of the team.</summary>
    [Range(0, MatchSettings.MaxFireteams)] public int Fireteams = 5;

    /// <summary>Soldiers in each fireteam.</summary>
    [Range(1, MatchSettings.MaxMembersPerFireteam)] public int MembersPerFireteam = 4;

    /// <summary>Planes of the team.</summary>
    [Range(0, MatchSettings.MaxPlanes)] public int Planes = 3;
}

/// <summary>
/// Settings chosen before the match starts: units of each team, civilians and simulation speed.
/// Saved in PlayerPrefs as JSON.
/// </summary>
[Serializable]
public class MatchSettings
{
    /// <summary>Units of the Blue team.</summary>
    public TeamMatchSettings Blue = new TeamMatchSettings();

    /// <summary>Units of the Red team.</summary>
    public TeamMatchSettings Red = new TeamMatchSettings();

    /// <summary>Civilians that run to the bunkers.</summary>
    [Range(0, MaxCivilians)] public int Civilians = 10;

    /// <summary>Time.timeScale of the match.</summary>
    [Range(MinTimeScale, MaxTimeScale)] public float TimeScale = 1f;

    /// <summary>Maximum fireteams per team.</summary>
    public const int MaxFireteams = 50;

    /// <summary>Maximum soldiers per fireteam.</summary>
    public const int MaxMembersPerFireteam = 5;

    /// <summary>Maximum planes per team.</summary>
    public const int MaxPlanes = 50;

    /// <summary>Maximum civilians.</summary>
    public const int MaxCivilians = 100;

    /// <summary>Slowest simulation speed.</summary>
    public const float MinTimeScale = 0.25f;

    /// <summary>Fastest simulation speed.</summary>
    public const float MaxTimeScale = 4f;

    private const string PrefsKey = "MatchSettings";

    /// <summary>Returns the settings of team. None returns null.</summary>
    public TeamMatchSettings ForTeam(MilitaryTeam team)
    {
        switch (team)
        {
            case MilitaryTeam.Blue: return Blue;
            case MilitaryTeam.Red: return Red;
            default: return null;
        }
    }

    /// <summary>Returns a copy of these settings.</summary>
    public MatchSettings Clone()
    {
        return JsonUtility.FromJson<MatchSettings>(JsonUtility.ToJson(this));
    }

    /// <summary>Saves the settings in PlayerPrefs.</summary>
    public void Save()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    /// <summary>Returns the saved settings, or a copy of fallback when there are none.</summary>
    public static MatchSettings LoadOrDefault(MatchSettings fallback)
    {
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            try
            {
                MatchSettings saved = JsonUtility.FromJson<MatchSettings>(PlayerPrefs.GetString(PrefsKey));
                if (saved != null)
                    return saved;
            }
            catch (ArgumentException)
            {
                // Unreadable JSON from an older version: the defaults are used.
            }
        }

        return fallback.Clone();
    }
}
