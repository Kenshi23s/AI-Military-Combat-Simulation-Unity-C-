using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GridEntity))]
public abstract class Entity : MonoBehaviour
{
    public Team myTeam { get; protected set; }
    public LifeComponent health { get; private set; }
    protected DebugableObject _debug;
    public GridEntity gridEntity { get; private set; }

    private void Awake()
    {
        health = GetComponent<LifeComponent>();
        _debug = GetComponent<DebugableObject>();
        gridEntity = GetComponent<GridEntity>();
        EntityAwake();
    }

    protected abstract void EntityAwake();
}
