using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System.Linq;
using System;
[System.Serializable]
public struct Seat
{
    public bool Available;
    public int seatPriority;
    public Infantry passenger;  
    public Transform seatPos;
}

[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(GridEntity))]
public abstract class Vehicle : Entity, IMilitary, FlockableEntity
{

    [SerializeField,Header("Vehicle Variables")] 
    protected FlockingParameters _flockingParameters;
    [SerializeField] protected float _sightRadius;
    [SerializeField] protected float _loseSightRadius;
    protected FOVAgent _fov;
    protected NewPhysicsMovement _movement;

    public MilitaryTeam Team { get; private set; }

    public bool InCombat => throw new NotImplementedException();

    protected GridEntity _gridEntity;

    public event Action OnDeathInCombat;

    public abstract void VehicleAwake();

    
    protected override void EntityAwake()
    {
        Health.OnKilled += () => OnDeathInCombat();
        _gridEntity = GetComponent<GridEntity>();
        _movement = GetComponent<NewPhysicsMovement>();
        _fov = GetComponent<FOVAgent>();
        _flockingParameters.myTransform = transform;
        VehicleAwake();
    }

    //#region Ya no va en los vehiculos :C
    //public List<Seat> vehicleSeats = new List<Seat>();

    //public event Action OnEngineTurnOff;
    //public event Action OnEngineTurnOn;
    //public event Action whileEngineOn;

    //public bool EngineOn;

    ///// <summary>
    ///// Subirse al vehiculo, requiere que le pasen un pasajero, solo se subira si hay asientos disponibles,
    ///// devuelve una booleana para chequear eso
    ///// </summary>
    ///// <param name="NewPassenger"></param>
    ///// <returns></returns>
    ////public bool GetInVehicle(Infantry NewPassenger)
    ////{
    ////    var col = vehicleSeats.Where(x => x.Available);
    ////    if (!col.Any()) return false;
      
    ////    var seat = col.Maximum(x => x.seatPriority);
    ////    seat.Available = false;
    ////    seat.passenger = NewPassenger;
    ////    NewPassenger.transform.position = seat.seatPos.position;
    ////    NewPassenger.transform.parent = seat.seatPos;

    ////    if (!EngineOn) TurnOnEngine();


    ////    return true;
    ////}
  
    ////public void GetOffVehicle(Infantry removePassenger)
    ////{
    ////    var col = vehicleSeats.Where((x) => x.passenger == removePassenger);
    ////    if (!col.Any()) return;       
            
    ////    var seat = col.First();
    ////    seat.Available = true;
    ////    seat.passenger = null;
    ////    removePassenger.transform.parent = null;
    ////    if (vehicleSeats.Where(x => !x.Available).Any())
    ////    {
    ////        TurnOffEngine();
    ////    }
    ////}

    //////private void Update()
    //////{
    //////    //si el motor esta prendido se ejecuta el evento
    //////    //me la estoy complicando talvez¿?
    //////    if (EngineOn) whileEngineOn?.Invoke();
    //////}

    ////void TurnOffEngine()
    ////{
    ////    EngineOn = false;
    ////    OnEngineTurnOff?.Invoke();
    ////}

    ////void TurnOnEngine()
    ////{
    ////    EngineOn=true;
    ////    OnEngineTurnOn?.Invoke();

    ////}
    //#endregion

    private void OnValidate()
    {
        _sightRadius = Mathf.Clamp(_sightRadius, 0, Mathf.Infinity);
        _loseSightRadius = Mathf.Clamp(_loseSightRadius, _sightRadius, Mathf.Infinity);
        GetComponent<FOVAgent>().SetFov(_sightRadius);
    }
    public void Initialize(MilitaryTeam newTeam)
    {
        Team = newTeam;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Vector3 GetVelocity()
    {
        return _movement.Velocity;
    }
}
