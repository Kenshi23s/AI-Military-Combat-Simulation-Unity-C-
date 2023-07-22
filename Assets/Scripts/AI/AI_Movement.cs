using FacundoColomboMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
#region ComponentsRequired
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(DebugableObject))]
[RequireComponent(typeof(Physics_Movement))]

#endregion

public class AI_Movement : MonoBehaviour
{
    DebugableObject _debug;
    public FOVAgent FOV { get; private set; }
    public Physics_Movement Movement { get; private set; }

    public Vector3 Destination { get; private set; } 

    public Vector3 Velocity => Movement._velocity;

    [Header("Flocking Forces")]

    public float DestinationArriveDistance = 0.1f;
    public float NodeArriveDistance = 1f;

    public LayerMask WhatIsGround;
    public LayerMask ObstacleMask;
    Action _fixedUpdate;

    public event Action OnDestinationReached, OnDestinationChanged, OnMovementCanceled;

    Vector3 _destination;
    List<Vector3> _waypoints = new List<Vector3>();

    #region Flocking
    [SerializeField]
    public bool Flocking {get; private set;}
    FlockingParameters _flockingParameters = new FlockingParameters();
    [SerializeField]
    IEnumerable<FlockableEntity> _flockingTargets = default;
    #endregion

    private void Awake()
    {
        FOV = GetComponent<FOVAgent>();
        Movement = GetComponent<Physics_Movement>();
        _debug = GetComponent<DebugableObject>();    

       
       
        _flockingParameters.myTransform = transform;
        _flockingParameters.viewRadius = FOV.ViewRadius;
    }

    public void SetTargets(IEnumerable<FlockableEntity> targets)
    {
        //_debug.Log("Targets para flocking Seteados");
        _flockingTargets = targets;
    }

    private void FixedUpdate() => _fixedUpdate?.Invoke();

    public void SetMaxSpeed(float newSpeed) =>  Movement.MaxSpeed = newSpeed;

    Transform _lookAtTarget;

    #region Pathfinding Methods
    /// <summary>
    /// Se mueve hasta esa ubicacion usando Theta* (aplicando flocking y obstacle avoidance)
    /// Si la ubicacion esta a la vista no calcula el camino y va directo
    /// </summary>
    /// <param name="destination"></param>
    public void SetDestination(Vector3 destination)
    {
        if (_waypoints.Any() && destination != _destination) OnDestinationChanged?.Invoke();

        _destination = destination;        

        if (transform.position.InLineOffSight(destination,AI_Manager.instance.WallMask))
        {
            // Conseguir la posicion en el piso
            if (Physics.Raycast(destination, Vector3.down, out RaycastHit hitInfo, 10f, AI_Manager.instance.WallMask))
            {
                destination = hitInfo.point;
                _destination = destination;
            }

            _fixedUpdate = () => 
            {
                _debug.Log("Veo el destino, voy directo.");
                MoveTowards(destination);
                if (Vector3.Distance(destination, transform.position) < DestinationArriveDistance) { 
                    OnDestinationReached?.Invoke();
                    ClearPath();
                }
            };
        }
        else
        {
            _debug.Log("No veo el destino, calculo el camino.");

            if (TryGetPath(destination, out _waypoints))
            {
                _fixedUpdate = PlayPath;
                _debug.Log("puedo calcular el camino.");
            }
            else
            {
                _debug.Log("No puedo calcular el camino.");
            }
        }
    }



   

    public void SetDestinationButLookAt(Vector3 destination, Transform lookAtTarget)
    {
        _lookAtTarget = lookAtTarget;

        if (_waypoints.Any() && destination != _destination) OnDestinationChanged?.Invoke();

        _destination = destination;

        if (transform.position.InLineOffSight(destination, AI_Manager.instance.WallMask))
        {
            // Conseguir la posicion en el piso
            if (Physics.Raycast(destination, Vector3.down, out RaycastHit hitInfo, 10f, AI_Manager.instance.WallMask)) 
            {
                destination = hitInfo.point;
                _destination = destination;
            }

            _fixedUpdate = () =>
            {
                if (_lookAtTarget == null)
                {
                    OnMovementCanceled?.Invoke();
                    ClearPath();
                    return;
                }

                //_debug.Log("Veo el destino, voy directo.");
                MoveTowardsButLookAt(destination, _lookAtTarget.transform.position);
                if (Vector3.Distance(destination, transform.position) < DestinationArriveDistance)
                {
                    OnDestinationReached?.Invoke();
                    ClearPath();
                }
            };
        }
        else
        {
            _debug.Log("No veo el destino, calculo el camino.");

            if (TryGetPath(destination,out _waypoints))
            {
                _fixedUpdate = PlayPathButLookAtTarget;
            }
            else
            {
                _debug.Log("No veo el camino");
            }
            
            
        }
    }

    public void MoveTowardsButLookAt(Vector3 destination, Vector3 lookAtTarget) 
    {
        MoveTowards(destination);

        Movement.LookAt(lookAtTarget);
    }

    void MoveTowards(Vector3 destination)
    {
        Vector3 actualForce = Vector3.zero;
        actualForce += Movement.Seek(destination);
        if (Flocking) actualForce += _flockingTargets.Flocking(_flockingParameters);
        actualForce += ObstacleAvoidance(transform);
        actualForce = ProjectAlongSlope(actualForce);
        
        Movement.SteerTowards(Velocity.CalculateSteering(actualForce, Movement.MaxSpeed));
        Movement.LookTowardsVelocity();
    }

    public Vector3 ProjectAlongSlope(Vector3 force) 
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.5f, WhatIsGround))
        {
            force = Vector3.ProjectOnPlane(force, hit.normal);
        }

        return force;
    }

    /// <summary>
    /// Consigue un camino hacia un objetivo usando Theta*
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    bool TryGetPath(Vector3 target,out List<Vector3> path)
    {
        path = new List<Vector3>();
        AI_Manager I = AI_Manager.instance;
        
        Tuple<Node, Node> keyNodes = Tuple.Create(I.GetNearestNode(transform.position), I.GetNearestNode(target));

        if (keyNodes.Item1!=null && keyNodes.Item2 !=null)
        {
            path = keyNodes.CalculateThetaStar(I.WallMask, target);
            return path.Any();
        }
        return false;
    }

    /// <summary>
    /// Reproduce el camino con Theta*
    /// </summary>
    /// <param name="waypoints"></param>
    void PlayPath()
    {
        if (!_waypoints.Any()) 
        {
            _debug.Log("no hay mas nodos, corto pathfinding");
             OnDestinationReached?.Invoke();
            ClearPath();
            return;
        }

        _debug.Log("Se mueve hacia el siguiente nodo, me faltan " + _waypoints.Count);

        MoveTowards(_waypoints[0]);

        if (Vector3.Distance(_waypoints[0], transform.position) < NodeArriveDistance) 
            _waypoints.RemoveAt(0);
    }

    void PlayPathButLookAtTarget() 
    {

        if (!_waypoints.Any())
        {
            OnDestinationReached?.Invoke();
            ClearPath();
            return;
        }

        if (_lookAtTarget == null)
        {
            OnMovementCanceled?.Invoke();
            ClearPath();
            return;
        }

        _debug.Log("Se mueve hacia el siguiente nodo, me faltan " + _waypoints.Count);

        MoveTowardsButLookAt(_waypoints[0], _lookAtTarget.transform.position);

        if (Vector3.Distance(_waypoints[0], transform.position) < NodeArriveDistance) 
            _waypoints.RemoveAt(0);
    }


    void ClearPath()
    {
        _fixedUpdate = null;
        _waypoints.Clear();
    }

    public void CancelMovement() 
    {
        ClearPath();
        OnMovementCanceled?.Invoke();
    }
    #endregion

    #region Movement Updates 

    Vector3 ObstacleAvoidance(Transform transform)
    {

        float dist = Velocity.magnitude;

        if (Physics.SphereCast(transform.position, 1.5f, transform.forward, out RaycastHit hit, dist, ObstacleMask))
        {
            //_debug.Log("ESTOY HACIENDO OBSTACLE AVOIDANCE!");
            Vector3 obstacle = hit.transform.position;
            Vector3 dirToObject = obstacle - transform.position;
            float angleInBetween = Vector3.SignedAngle(transform.forward, dirToObject, Vector3.up);

            Vector3 desired = angleInBetween >= 0 ? -transform.right : transform.right;

            return Velocity.CalculateSteering(desired,Movement.MaxSpeed);
        }

        return Vector3.zero;
    }

    #endregion

    
}
