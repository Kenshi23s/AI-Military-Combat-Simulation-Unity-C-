using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Entity with an Animator on itself or on a child.</summary>
public abstract class Human : Entity
{
    /// <summary>The Animator of the human.</summary>
    [field: SerializeField] public Animator Anim { get; protected set; }

    protected override void Awake()
    {
        base.Awake();

        if (TryGetComponent<Animator>(out var anim))
        {
            Anim = anim;
            return;
        }

        Anim = GetComponentInChildren<Animator>();
    }
}
