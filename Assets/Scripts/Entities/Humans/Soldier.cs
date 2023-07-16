using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public abstract class Soldier : Human, IMilitary
{
    [field : SerializeField]public MilitaryTeam Team { get; protected set; }

    protected GridEntity _gridEntity;

    protected override void EntityAwake()
    {
        _gridEntity = GetComponent<GridEntity>();

        SoldierAwake();
    }

    protected virtual void SoldierAwake() { }
}
