using System;
using UnityEngine;
[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public class Bunker : MonoBehaviour
{
    Civilian[] _refugees;
    [SerializeField,Range(1,30)]int _bunkerCapacity;
    DebugableObject _debug;
    public event Action onBunkerDestroyed;

    private void Awake()
    {
        _debug = GetComponent<DebugableObject>();
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
            _debug.Log($"El civil {civilian} puede entrar al bunker, quedan {_refugees.Length - i} espacios disponibles ");
            return true;
        }
        _debug.Log($"El civil {civilian} no puede entrar al bunker ");
        return false;
    }



    
}
