using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LifeComponent))]
public class Bunker : MonoBehaviour
{
    Civilian[] Refugees;
    [SerializeField,Range(1,30)]int _bunkerCapacity;
    public event Action onBunkerDestroyed;
    [SerializeField] float life;

    private void Awake()
    {
        Refugees = new Civilian[_bunkerCapacity];
        var health = GetComponent<LifeComponent>();
        health.OnKilled += () =>
        {
            onBunkerDestroyed?.Invoke();
            Destroy(gameObject);
        };
    }


    public bool EnterBunker(Civilian civilian)
    {
        for (int i = 0; i < Refugees.Length; i++)
        {
            if (Refugees[i] != null) continue;

            Refugees[i]=civilian;
            return true;
        }
        return false;
    }



    
}
