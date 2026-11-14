using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Sets the time scale and frame rate of the match and keeps the list of Bunkers.
/// Logs the damage feed of the match.
/// </summary>
[RequireComponent(typeof(DebugableObject))]
public class GameManager : MonoSingleton<GameManager>
{
    /// <summary>Read-only view of the Bunkers that are standing.</summary>
    public ReadOnlyCollection<Bunker> Bunkers;

    /// <summary>Time.timeScale set on Awake.</summary>
    public int timeScale = 1;
    private List<Bunker> _bunkers=new List<Bunker>();
    private DebugableObject DebugGM;

    /// <summary>Application.targetFrameRate set on Awake.</summary>
    public const int targetFrameRate = 144;

    protected override void SingletonAwake()
    {
        Time.timeScale = timeScale;
        Application.targetFrameRate = targetFrameRate;
        Bunkers = new ReadOnlyCollection<Bunker>(_bunkers);
        DebugGM = GetComponent<DebugableObject>();
    }

    /// <summary>Adds newBunker to Bunkers. The bunker leaves the list when it is destroyed.</summary>
    public void AddBunker(Bunker newBunker)
    {
        _bunkers.Add(newBunker);
        newBunker.onBunkerDestroyed += () =>
        {
            _bunkers.Remove(newBunker);
        };
    }

    /// <summary>Logs that From damaged or killed Victim.</summary>
    public void DebugDamageFeed(GameObject From,IDamageable Victim)
    {
        DebugDamageFeed(From.gameObject.name,Victim);
    }

    /// <summary>Logs that name damaged or killed Victim.</summary>
    public void DebugDamageFeed(string name, IDamageable Victim)
    {
        string Text = "Le hizo daño a";
        if (!Victim.IsAlive)
        {
            Text = "Mato a";
        }
        DebugGM.Log($"{name} " + Text + $" {Victim}");
    }
}
