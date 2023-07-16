using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Entity : MonoBehaviour
{
    public LifeComponent Health { get; private set; }
    public DebugableObject DebugEntity { get; private set; }
    public bool IsCapturing { get; private set; }


    public void SetCaptureState(bool arg)
    {
        IsCapturing = arg;
    }

    private void Awake()
    {
        Health = GetComponent<LifeComponent>();
        DebugEntity = GetComponent<DebugableObject>();
        IsCapturing = false;

        EntityAwake();
    }

    protected virtual void EntityAwake() { }
}
