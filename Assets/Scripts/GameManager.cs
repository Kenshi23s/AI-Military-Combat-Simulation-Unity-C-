using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    List<Bunker> _bunkers;
    public ReadOnlyCollection<List<Bunker>> bunkers;
    protected override void SingletonAwake()
    {

      
    }

    public void AddBunker(Bunker newBunker)
    {
        _bunkers.Add(newBunker);
        newBunker.onBunkerDestroyed += () =>
        {
            _bunkers.Remove(newBunker);
        };
    }

 
}
