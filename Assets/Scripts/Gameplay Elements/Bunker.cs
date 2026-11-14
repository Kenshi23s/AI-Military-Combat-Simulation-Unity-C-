using System;
using UnityEngine;
/// <summary>
/// Shelter with room for _bunkerCapacity civilians.
/// Raises onBunkerDestroyed and destroys itself when its LifeComponent dies.
/// </summary>
[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public class Bunker : MonoBehaviour
{
    [SerializeField,Range(1,30)]private int _bunkerCapacity;
    private Civilian[] _refugees;
    private DebugableObject _debug;

    /// <summary>Raised when the bunker is destroyed.</summary>
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

    /// <summary>Places civilian in the first free spot. Returns true when the bunker had room.</summary>
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
