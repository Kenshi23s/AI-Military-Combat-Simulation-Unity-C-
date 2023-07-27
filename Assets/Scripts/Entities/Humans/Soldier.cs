using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public abstract class Soldier : Human, IMilitary
{
    [field: SerializeField, Header("Soldier")]
    public MilitaryTeam Team { get; protected set; }

    protected GridEntity _gridEntity;

    public bool InCombat { get; protected set; }

    public int TotalDamageDealt { get; protected set; }

    public event Action OnDeathInCombat;

    protected abstract void SoldierAwake();

    protected override void EntityAwake()
    {
        Health.OnKilled += () => OnDeathInCombat?.Invoke();
        _gridEntity = GetComponent<GridEntity>();
        SoldierAwake();
    }




    
}
