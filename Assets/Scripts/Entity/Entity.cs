using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Entity : MonoBehaviour
{

    [field:SerializeField] public Team MyTeam { get; protected set; }
    public LifeComponent Health { get; private set; }
    public DebugableObject DebugEntity { get; private set; }

    private void Awake()
    {
        Health = GetComponent<LifeComponent>();
        DebugEntity = GetComponent<DebugableObject>();

        EntityAwake();
    }

    protected virtual void EntityAwake() { }
}
