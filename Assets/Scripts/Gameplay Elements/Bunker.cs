using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LifeComponent))]
public class Bunker : MonoBehaviour
{
    Civilian[] _refugees;
    [SerializeField,Range(1,30)]int _bunkerCapacity;
    public event Action onBunkerDestroyed;

    private void Awake()
    {
        _refugees = new Civilian[_bunkerCapacity];
        var health = GetComponent<LifeComponent>();
        health.OnKilled += () =>
        {
            onBunkerDestroyed?.Invoke();
            Destroy(gameObject);
        };
    }

    private void Start()
    {
        GameManager.instance.AddBunker(this);
    }
    public bool EnterBunker(Civilian civilian)
    {
        for (int i = 0; i < _refugees.Length; i++)
        {
            if (_refugees[i] != null) continue;

            _refugees[i]=civilian;
            return true;
        }
        return false;
    }



    
}
