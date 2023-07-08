using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
[RequireComponent(typeof(Rigidbody))]
public abstract class Entity : MonoBehaviour
{
    public Team myTeam { get; protected set; }
    public LifeComponent health { get; private set; }
    protected DebugableObject _debug;

    private void Awake()
    {
        health = GetComponent<LifeComponent>();
        _debug = GetComponent<DebugableObject>();

        EntityAwake();
    }

    protected virtual void EntityAwake() { }
}
