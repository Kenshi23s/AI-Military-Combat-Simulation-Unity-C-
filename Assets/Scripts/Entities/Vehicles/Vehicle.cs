using System;
using UnityEngine;

/// <summary>
/// Base class of the military vehicles.
/// Holds the movement, field of view, weapon and flocking parameters of the vehicle.
/// </summary>
[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(ShootComponent))]
public abstract class Vehicle : Entity, IMilitary, IFlockableEntity
{
    [SerializeField, Header("Vehicle Variables")]
    protected FlockingParameters _flockingParameters;
    [SerializeField] protected float _sightRadius;
    [SerializeField] protected float _loseSightRadius;
    protected FOVAgent _fov;
    protected NewPhysicsMovement _movement;
    protected ShootComponent _shootComponent;

    protected GridEntity _gridEntity;

    /// <summary>Team of the vehicle.</summary>
    public MilitaryTeam Team { get; private set; }

    /// <summary>true while the vehicle fights.</summary>
    public virtual bool InCombat => false;

    /// <summary>Damage dealt by the weapons of the vehicle.</summary>
    public int TotalDamageDealt { get; protected set; }

    /// <summary>Raised when the vehicle dies.</summary>
    public event Action OnDeathInCombat;

    /// <summary>Initializes the vehicle at the end of Awake.</summary>
    public abstract void VehicleAwake();

    protected override void Awake()
    {
        base.Awake();

        Health.OnKilled += () => OnDeathInCombat?.Invoke();

        _gridEntity = GetComponent<GridEntity>();
        _movement = GetComponent<NewPhysicsMovement>();
        _fov = GetComponent<FOVAgent>();
        _shootComponent = GetComponent<ShootComponent>();
        _shootComponent.onHit += _ => TotalDamageDealt += _shootComponent.BulletDamage;

        _flockingParameters.myTransform = transform;
        VehicleAwake();
    }

    private void OnValidate()
    {
        _sightRadius = Mathf.Clamp(_sightRadius, 0, Mathf.Infinity);
        _loseSightRadius = Mathf.Clamp(_loseSightRadius, _sightRadius, Mathf.Infinity);
        GetComponent<FOVAgent>().SetFov(_sightRadius);
    }

    /// <summary>Sets the Team.</summary>
    public void Initialize(MilitaryTeam newTeam)
    {
        Team = newTeam;
    }

    /// <summary>Returns the position of the vehicle.</summary>
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>Returns the velocity of the vehicle.</summary>
    public Vector3 GetVelocity()
    {
        return _movement.Velocity;
    }
}
