using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(DebugableObject))]
public class GameManager : MonoSingleton<GameManager>
{
    List<Bunker> _bunkers=new List<Bunker>();
    public ReadOnlyCollection<Bunker> Bunkers;
    DebugableObject DebugGM;

    public const int targetFrameRate = 144;

    protected override void SingletonAwake()
    {
        Application.targetFrameRate = targetFrameRate;
        Bunkers = new ReadOnlyCollection<Bunker>(_bunkers);
        DebugGM = GetComponent<DebugableObject>();
    }

    public void AddBunker(Bunker newBunker)
    {
        _bunkers.Add(newBunker);
        newBunker.onBunkerDestroyed += () =>
        {
            _bunkers.Remove(newBunker);
        };
    }

    public void DebugDamageFeed(GameObject From,IDamagable Victim)
    {
        DebugDamageFeed(From.gameObject.name,Victim);
    }

    public void DebugDamageFeed(string name, IDamagable Victim)
    {
        string Text = "Le hizo da�o a";
        if (!Victim.IsAlive)
        {
            Text = "Mato a";
        }
        DebugGM.Log($"{name} " + Text + $" {Victim}");
    }

}
