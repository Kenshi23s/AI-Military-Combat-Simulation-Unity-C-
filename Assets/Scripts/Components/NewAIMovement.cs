using System;
using System.Collections.Generic;
using FacundoColomboMethods;
using UnityEngine;

/// <summary>
/// Moves the agent to a destination with NewPhysicsMovement.
/// The agent moves straight to a destination in line of sight and follows a Theta* path to any other destination.
/// </summary>
[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewAIMovement : MonoBehaviour
{
    // Maximum distance for the straight movement without a path.
    private const float DirectPathMaxDistance = 20f;

    // Nodes that CalculateLazyThetaStar expands per frame.
    private const int PathStepsPerFrame = 200;

    /// <summary>The NewPhysicsMovement that applies the forces.</summary>
    public NewPhysicsMovement ManualMovement { get; private set; }

    /// <summary>The GridEntity on the same GameObject.</summary>
    public GridEntity Owner { get; private set; }

    /// <summary>Raised when the agent reaches any destination.</summary>
    public event Action OnAnyDestinationReached;

    /// <summary>Raised when SetDestination receives a destination different from Destination.</summary>
    public event Action OnDestinationChanged;

    /// <summary>Raised by CancelMovement.</summary>
    public event Action OnMovementCanceled;

    // Callback of the current destination. SetDestination replaces it.
    private Action OnCurrentDestinationReached;

    private DebugableObject _debug;

    /// <summary>Distance to a point at which the agent counts it as reached.</summary>
    public float DestinationArriveDistance;

    private Action _fixedUpdate;

    private readonly List<Vector3> _path = new List<Vector3>();

    // Index in _path of the next waypoint.
    private int _pathIndex;

    // Coroutine of the path calculation in progress.
    private Coroutine _pathCalculation;

    [SerializeField] private LayerMask obstacleMask;

    /// <summary>The last destination given to SetDestination.</summary>
    public Vector3 Destination { get; private set; }

    private void Awake()
    {
        ManualMovement = GetComponent<NewPhysicsMovement>();
        Owner = GetComponent<GridEntity>();
        _debug = GetComponent<DebugableObject>();

        _debug.AddGizmoAction(DrawPath); _debug.AddGizmoAction(DrawDestination);

        OnDestinationChanged += () => OnCurrentDestinationReached = null;
        OnMovementCanceled += () => OnCurrentDestinationReached = null;
    }

    private void FixedUpdate() => _fixedUpdate?.Invoke();

    /// <summary>
    /// Moves the agent to newDestination.
    /// The agent moves straight when it has line of sight to newDestination within DirectPathMaxDistance.
    /// The agent follows a Theta* path in every other case.
    /// </summary>
    /// <param name="newDestination">Point to reach.</param>
    /// <param name="onDestinationReached">Called once when the agent reaches newDestination.</param>
    /// <param name="onFinishCalculating">
    /// Called once with true when the agent starts moving.
    /// Called once with false when the path calculation fails.
    /// </param>
    public void SetDestination(Vector3 newDestination, Action onDestinationReached = null, Action<bool> onFinishCalculating = null)
    {
        StopPathCalculation();

        if (newDestination != Destination) OnDestinationChanged?.Invoke();

        Destination = newDestination;
        OnCurrentDestinationReached = onDestinationReached;

        if (transform.position.InLineOffSight(newDestination, AI_Manager.instance.WallMask, DirectPathMaxDistance))
        {
            onFinishCalculating?.Invoke(true);
            OnDesinationAtSight(newDestination);
        }
        else
        {
            CalculatePath(newDestination, onFinishCalculating);
        }
    }

    /// <summary>Stops the agent and clears the current destination callback.</summary>
    public void CancelMovement()
    {
        StopPathCalculation();
        ClearPath();
        OnMovementCanceled?.Invoke();
    }

    private void OnDesinationAtSight(Vector3 newDestination)
    {
        // Conseguir la posicion en el piso
        if (Physics.Raycast(newDestination, Vector3.down, out RaycastHit hitInfo, 10f, AI_Manager.instance.WallMask))
        {
            newDestination = hitInfo.point;
            Destination = newDestination;
        }

        if (_debug.canDebug)
            _debug.Log("Veo el destino, voy directo a " + newDestination);

        _fixedUpdate = () =>
        {
            if (Vector3.Distance(newDestination, transform.position) < DestinationArriveDistance)
            {
                Action onReached = OnCurrentDestinationReached;

                // The path is cleared before the callbacks, so a callback can set a new destination.
                ClearPath();
                onReached?.Invoke();
                OnAnyDestinationReached?.Invoke();
            }
            else
            {
                Vector3 dir = newDestination - transform.position;
                ManualMovement.AccelerateTowards(dir + ObstacleAvoidance());
            }
        };
    }

    #region CalculatePath

    private void CalculatePath(Vector3 newDestination, Action<bool> onFinishCalculating)
    {
        _debug.Log("No veo el destino, calculo el camino.");

        AI_Manager manager = AI_Manager.instance;

        Tuple<Node, Node> keyNodes = Tuple.Create(manager.GetNearestNode(transform.position), manager.GetNearestNode(newDestination));

        if (keyNodes.Item1 == null || keyNodes.Item2 == null)
        {
            if (_debug.canDebug)
            {
                string node1 = keyNodes.Item1 != null ? " NO es null " : "ES null ";
                string node2 = keyNodes.Item2 != null ? " NO es null " : "ES null ";
                _debug.Log("El nodo INICIAL" + node1 + " y El nodo FINAL" + node2);
            }

            onFinishCalculating?.Invoke(false);
            return;
        }

        Action<bool, List<Vector3>> onPathCalculated = (pathMade, newPath) =>
        {
            _pathCalculation = null;
            onFinishCalculating?.Invoke(pathMade);
            CanPlayPath(pathMade, newPath);
        };

        _pathCalculation = StartCoroutine(keyNodes.CalculateLazyThetaStar(manager.WallMask, onPathCalculated, Destination, PathStepsPerFrame));
    }

    private void StopPathCalculation()
    {
        if (_pathCalculation == null)
            return;

        StopCoroutine(_pathCalculation);
        _pathCalculation = null;
    }

    #endregion

    #region PathPlay

    private void CanPlayPath(bool pathmade, List<Vector3> newPath)
    {
        if (!pathmade)
        {
            _debug.Log("No se pudo armar el camino");
            return;
        }

        _path.Clear();
        _path.AddRange(newPath);
        _pathIndex = 0;

        _debug.Log("Arme el camino, lo reproduzo ");
        _fixedUpdate = PlayPath;
    }

    private void PlayPath()
    {
        // Si llegamos al waypoint mas cercano, pasar al siguiente
        if (_pathIndex < _path.Count && Vector3.Distance(_path[_pathIndex], transform.position) < DestinationArriveDistance)
            _pathIndex++;

        // Mientras queden waypoints seguir avanzando
        if (_pathIndex < _path.Count)
        {
            if (_debug.canDebug)
                _debug.Log("Se mueve hacia el siguiente nodo, me faltan " + (_path.Count - _pathIndex));

            var dir = _path[_pathIndex] - transform.position;
            dir += ObstacleAvoidance();
            ManualMovement.AccelerateTowards(dir);
            return;
        }

        // Si no quedan, finalizar el recorrido
        _debug.Log("no hay mas nodos, corto pathfinding");

        Action onReached = OnCurrentDestinationReached;

        // The path is cleared before the callbacks, so a callback can set a new destination.
        ClearPath();
        OnAnyDestinationReached?.Invoke();
        onReached?.Invoke();
    }

    private void ClearPath()
    {
        _fixedUpdate = null;
        ManualMovement.ClearForces();
        ManualMovement.AccelerateTowards(Vector3.zero);
        _path.Clear();
        _pathIndex = 0;
    }

    #endregion

    private Vector3 ObstacleAvoidance()
    {
        float dist = ManualMovement.CurrentSpeed;

        if (Physics.SphereCast(transform.position, 1.5f, ManualMovement.Velocity, out RaycastHit hit, dist, obstacleMask))
        {
            Vector3 obstacle = hit.transform.position;
            Vector3 dirToObject = obstacle - transform.position;
            float angleInBetween = Vector3.SignedAngle(transform.forward, dirToObject, Vector3.up);

            return angleInBetween >= 0 ? -transform.right : transform.right;
        }

        return Vector3.zero;
    }

    #region Gizmos

    private void DrawPath()
    {
        if (_pathIndex < _path.Count)
            Gizmos.DrawLine(transform.position, _path[_pathIndex]);

        for (int i = _pathIndex; i < _path.Count - 1; i++)
            Gizmos.DrawLine(_path[i], _path[i + 1]);
    }

    private void DrawDestination()
    {
        if (Destination != Vector3.zero)
            Gizmos.DrawWireSphere(Destination, 3f);
    }

    #endregion
}
