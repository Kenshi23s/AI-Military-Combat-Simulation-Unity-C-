using IA2;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public enum PlaneStates
{
    FlyAround,
    PursuitTarget,
    BeingChased,
    Abandoned
}
[RequireComponent(typeof(GridEntity))]

public class Plane : Vehicle
{

    EventFSM<PlaneStates> _planeFSM;
    public Plane targetPlane;
    public Plane BeingChasedBy;

    public override void VehicleAwake()
    {
        _movement = GetComponent<Physics_Movement>();      
    }

    // Start is called before the first frame update
    void Start()
    {
       
    }

    State<PlaneStates> FlyAround()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");


        state.OnUpdate += () =>
        {
            var col = GetNearbyPlanes().Where(x => x.myTeam != myTeam).Where(x=>_fov.IN_FOV(x.transform.position));
            if (col.Any())
            {
                targetPlane = col.Minimum((x) => Vector3.Distance(x.transform.position, transform.position));
                _planeFSM.SendInput(PlaneStates.PursuitTarget);
            }
        };
       
        state.OnFixedUpdate += () =>
        {
            Vector3 force = transform.forward;

            var col = GetNearbyPlanes().Where(x=> x.myTeam == myTeam);
            if (col.Any())           
                force += col.Flocking(flockingParameters);        
            else if(!PlanesManager.instance.InCombatZone())         
                force = -_movement._velocity;

            if (Physics.Raycast(transform.position, transform.forward, 30f, PlanesManager.instance.ground))
                force += Vector3.up;

            _movement.AddForce(force);
        };
        return state;
    }

    IEnumerable<Plane> GetNearbyPlanes()
    {
        return Queries.Query().Where(x => x != this).OfType<Plane>().Where(x=>x._planeFSM.CurrentKey != PlaneStates.Abandoned);
    }


    State<PlaneStates> DogFight()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");

        state.OnEnter += (x) =>
        {
            if (targetPlane==null)
            {
                _planeFSM.SendInput(PlaneStates.FlyAround);
                return;
            }
            else
            {
                targetPlane.BeingChasedBy = this;
                targetPlane._planeFSM.SendInput(PlaneStates.BeingChased);
            }
        };
        state.OnFixedUpdate += () =>
        {
            Vector3 force = transform.forward;
            if (!PlanesManager.instance.InCombatZone())
                force += -_movement._velocity;
            if (Physics.Raycast(transform.position, transform.forward, 30f, PlanesManager.instance.ground))
                force += Vector3.up;

            _movement.AddForce(force);
        };
        return state;
    }
}
