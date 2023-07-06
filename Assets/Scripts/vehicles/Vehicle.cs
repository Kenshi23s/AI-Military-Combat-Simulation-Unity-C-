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
[RequireComponent(typeof(Physics_Movement))]
[RequireComponent(typeof(FOVAgent))]
public abstract class Vehicle : Entity,FlockableEntity
{
    [SerializeField] 
    protected FlockingParameters flockingParameters;
    [SerializeField] protected float sightRadius;
    [SerializeField] protected float _loseSightRadius;
    protected FOVAgent _fov;
    protected Physics_Movement _movement;
    public Team myTeam;
    public List<Seat> vehicleSeats = new List<Seat>();

    public event Action OnEngineTurnOff;
    public event Action OnEngineTurnOn;
    public event Action whileEngineOn;

    public bool EngineOn;

    public abstract void VehicleAwake();

    protected override void EntityAwake()
    {
        _movement = GetComponent<Physics_Movement>();
        _fov=GetComponent<FOVAgent>();
        vehicleSeats = vehicleSeats.OrderBy(x => x.seatPriority).ToList();
        VehicleAwake();
    }
  
    /// <summary>
    /// Subirse al vehiculo, requiere que le pasen un pasajero, solo se subira si hay asientos disponibles,
    /// devuelve una booleana para chequear eso
    /// </summary>
    /// <param name="NewPassenger"></param>
    /// <returns></returns>
    public bool GetInVehicle(Infantry NewPassenger)
    {
        var col = vehicleSeats.Where(x => x.Available);
        if (!col.Any()) return false;
      
        var seat = col.Maximum(x=>x.seatPriority);
        seat.Available = false;
        seat.passenger = NewPassenger;
        NewPassenger.transform.position = seat.seatPos.position;
        NewPassenger.transform.parent = seat.seatPos;

        if (!EngineOn) TurnOnEngine();
       

        return true;      
    }
  
    public void GetOffVehicle(Infantry removePassenger)
    {
        var col = vehicleSeats.Where((x) => x.passenger == removePassenger);
        if (!col.Any()) return;       
            
        
        var seat = col.First();
        seat.Available = true;
        seat.passenger = null;
        removePassenger.transform.parent = null;
        if (vehicleSeats.Where(x=>!x.Available).Any())
        {
            TurnOffEngine();
        }
    }

    private void Update()
    {
        //si el motor esta prendido se ejecuta el evento
        //me la estoy complicando talvez¿?
        if (EngineOn) whileEngineOn?.Invoke();
    }
    void TurnOffEngine()
    {
        EngineOn = false;
        OnEngineTurnOff?.Invoke();
    }

    void TurnOnEngine()
    {
        EngineOn=true;
        OnEngineTurnOn?.Invoke();

    }
    private void OnValidate()
    {
        sightRadius = Mathf.Clamp(sightRadius, 0, Mathf.Infinity);
        _loseSightRadius = Mathf.Clamp(_loseSightRadius, sightRadius, Mathf.Infinity);
    }
    public void Initialize(Team newTeam)
    {
        myTeam = newTeam;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Vector3 GetVelocity()
    {
        return _movement._velocity;
    }
}
