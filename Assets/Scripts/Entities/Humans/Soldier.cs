using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(ShootComponent))]
[RequireComponent(typeof(FOVAgent))]
public abstract class Soldier : Human, IMilitary
{
    [field: SerializeField, Header("Soldier")]
    public MilitaryTeam Team { get; protected set; }

    protected GridEntity _gridEntity;
    protected ShootComponent ShootComponent;
    protected FOVAgent _fovAgent;

    [SerializeField] protected Transform _shootPos;


    public bool InCombat { get; protected set; }

    public int TotalDamageDealt { get; protected set; }

    public event Action OnDeathInCombat;

    protected override void Awake()
    {
        base.Awake();

        Health.OnKilled += () => OnDeathInCombat?.Invoke();

        _gridEntity = GetComponent<GridEntity>();
        ShootComponent = GetComponent<ShootComponent>();
        _fovAgent = GetComponent<FOVAgent>();
    }
}
