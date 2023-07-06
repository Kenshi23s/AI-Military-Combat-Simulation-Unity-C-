using IA2;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum PlaneStates
{
    FlyAround,
    PursuitTarget,
    BeingChased,
    Abandoned
}
[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(ShootComponent))]
public class Plane : Vehicle
{

    EventFSM<PlaneStates> _planeFSM;
    public Plane targetPlane;
    public Plane beingChasedBy;
    [SerializeField] float _loseSightRadius;
    [SerializeField] float _spreadRadius;
    ShootComponent shootComponent;

    //para que el avion que es perseguido se mueva de manera "dinamica" en la dogfight
    //estaria bueno cambiarlo cada x seg con una corrutina cuando se esta en ese estado
    Vector3 evadingDir;

    #region ShootingLogic
    [Header("Shooting Parameters")]
    [SerializeField] bool isShooting;
    [SerializeField] float bulletsPerBurst;
    [SerializeField] float burstCD = 3;
    [SerializeField] float BulletCD = 0.3f;
    [SerializeField] Transform shootPos;

    IEnumerator ShootCoroutine()
    {
        isShooting = true;
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            shootComponent.Shoot(shootPos);
            yield return new WaitForSeconds(BulletCD);
        }
        yield return new WaitForSeconds(burstCD);
        isShooting = false;
    }
    #endregion

    public override void VehicleAwake()
    {
        _movement = GetComponent<Physics_Movement>();
        shootComponent = GetComponent<ShootComponent>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    //jocha me va a matar cuando vea este quilombo :C C:
    State<PlaneStates> FlyAround()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");

        state.OnUpdate += () =>
        {
            //obtengo los aviones cercanos que no sean de mi equipo y esten en mi fov
            var col = GetNearbyPlanes().Where(x => x.myTeam != myTeam).Where(x => _fov.IN_FOV(x.transform.position));
            if (col.Any())
            {
                //si encuentro alguno, obtengo el mas cercano
                targetPlane = col.Minimum((x) => Vector3.Distance(x.transform.position, transform.position));
                //y empiezo la persecucion
                _planeFSM.SendInput(PlaneStates.PursuitTarget);
            }
        };

        state.OnFixedUpdate += () =>
        {
            Vector3 force = transform.forward;

            //agarro los aviones mas cercanos de mi equipo
            var col = GetNearbyPlanes().Where(x => x.myTeam == myTeam);

            //si hay alguno y estoy en zona de combate, hago flocking
            if (col.Any() && PlanesManager.instance.InCombatZone(this))
                force += col.Flocking(flockingParameters);
            else if (!PlanesManager.instance.InCombatZone(this))
            {
                //sino estoy en zona de combate me pego la vuelta
                force += -_movement._velocity;
            }

            //si estoy cerca del suelo levanto hacia arriba
            if (Physics.Raycast(transform.position, transform.forward, 30f, PlanesManager.instance.ground))
                force += Vector3.up;

            //sumo estas fuerzas C:
            _movement.AddForce(force);
        };
        return state;
    }

    /// <summary>
    /// obtengo los aviones cercanos que no sean yo y que no esten "abandonados"
    /// </summary>
    /// <returns></returns>
    IEnumerable<Plane> GetNearbyPlanes()
    {
        return Queries.Query().Where(x => x != this).OfType<Plane>().Where(x => x._planeFSM.CurrentKey != PlaneStates.Abandoned);
    }


    State<PlaneStates> DogFight()
    {
        State<PlaneStates> state = new State<PlaneStates>("DogFight");

        state.OnEnter += (x) =>
        {
            if (targetPlane == null)
            {
                _planeFSM.SendInput(PlaneStates.FlyAround);
                return;
            }
            else
            {
                targetPlane.beingChasedBy = this;
                targetPlane._planeFSM.SendInput(PlaneStates.BeingChased);
            }
        };

        //decision
        state.OnUpdate += () =>
        {
            if (targetPlane == null)
            {
                _planeFSM.SendInput(PlaneStates.FlyAround); return;
            }

            if (!_fov.IN_FOV(targetPlane.transform.position, _loseSightRadius))
            {
                targetPlane.beingChasedBy = null;
                _planeFSM.SendInput(PlaneStates.FlyAround);
                return;
            }
        };

        state.OnUpdate += () =>
        {
            if (targetPlane == null)
            {
                _planeFSM.SendInput(PlaneStates.FlyAround);
                return;
            }

            if (!_fov.IN_FOV(targetPlane.transform.position, _loseSightRadius))
            {
                targetPlane.beingChasedBy = null;
                _planeFSM.SendInput(PlaneStates.FlyAround);
            }
        };

        //fisica avion
        state.OnFixedUpdate += () =>
        {
            Vector3 force = transform.forward;

            force += targetPlane.Pursuit();

            if (!PlanesManager.instance.InCombatZone(this))
                force += -_movement._velocity;
            //despues tener una variable para la "distancia del suelo"
            if (Physics.Raycast(transform.position, transform.forward, 30f, PlanesManager.instance.ground))
                force += Vector3.up;

            _movement.AddForce(force);
        };
        return state;
    }

    State<PlaneStates> FleeFromDogFight()
    {
        State<PlaneStates> state = new State<PlaneStates>("FleeFromFight");
        state.OnEnter += (x) =>
        {
            if (beingChasedBy == null)            
                _planeFSM.SendInput(PlaneStates.FlyAround);
             
            
           
           
        };



        return state;
    }
   
}
