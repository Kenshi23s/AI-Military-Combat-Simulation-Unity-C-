using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Entity : MonoBehaviour
{

    public LifeComponent health { get; private set; }
    protected DebugableObject _debug;


    private void Awake()
    {
        health = GetComponent<LifeComponent>();
        _debug = GetComponent<DebugableObject>();
        EntityAwake();
    }
    protected abstract void EntityAwake();
}
