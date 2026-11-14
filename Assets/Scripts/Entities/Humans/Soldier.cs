using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Human of a team with a GridEntity, a ShootComponent and a FOVAgent.</summary>
[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(ShootComponent))]
[RequireComponent(typeof(FOVAgent))]
public abstract class Soldier : Human, IMilitary
{
    /// <summary>Team of the soldier.</summary>
    [field: SerializeField, Header("Soldier")]
    public MilitaryTeam Team { get; protected set; }

    [SerializeField] protected Transform _shootPos;

    protected GridEntity _gridEntity;
    protected ShootComponent ShootComponent;
    protected FOVAgent _fovAgent;

    /// <summary>Damage the soldier has dealt.</summary>
    public int TotalDamageDealt { get; protected set; }

    /// <summary>Raised when the soldier dies.</summary>
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
