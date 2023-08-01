using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Human : Entity
{
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
