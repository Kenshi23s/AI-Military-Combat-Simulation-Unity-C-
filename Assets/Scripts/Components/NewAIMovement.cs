using System;
using System.Collections.Generic;
using FacundoColomboMethods;
using UnityEngine;

/// <summary>
/// Moves the agent to a destination with NewPhysicsMovement.
/// The agent moves straight to a destination with clear line of sight and follows a Theta* path to any other destination.
/// The agent slides along walls and recovers when it stops making progress.
/// </summary>
/// <remarks>
/// <para>The line of sight checks use a sphere of the radius of the CapsuleCollider, so the shortcuts fit the body of the agent.</para>
/// <para>
/// Every StuckCheckInterval seconds the agent compares its position with the previous check.
/// The first time it is stuck it recalculates the path through the nodes.
/// The next times it backs away from the wall and recalculates the path.
/// After MaxStuckAttempts it gives up and raises OnMovementFailed.
/// </para>
/// </remarks>
[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(DebugableObject))]
[DisallowMultipleComponent]
public class NewAIMovement : MonoBehaviour
{
    /// <summary>Distance to a point at which the agent counts it as reached.</summary>
    public float DestinationArriveDistance;

    [SerializeField] private LayerMask obstacleMask;

    // Callbacks of the current destination. SetDestination replaces them.
    private Action OnCurrentDestinationReached;
    private Action OnCurrentDestinationFailed;

    private DebugableObject _debug;

    private Action _fixedUpdate;

    private readonly List<Vector3> _path = new List<Vector3>();

    // Index in _path of the next waypoint.
    private int _pathIndex;

    // Coroutine of the path calculation in progress.
    private Coroutine _pathCalculation;

    // Radius of the body and center of the body relative to the transform.
    private float _agentRadius;
    private Vector3 _bodyCenterLocal;

    // Stuck detection.
    private Vector3 _progressCheckPosition;
    private float _progressTimer;
    private int _stuckAttempts;
    private bool _isBackingOff;

    // Flat normal of the last wall the agent touched or was about to hit.
    private Vector3 _lastWallNormal;

    // Maximum distance for the straight movement without a path.
    private const float DirectPathMaxDistance = 20f;

    // Length of the downward raycast that snaps a destination to the ground.
    private const float GroundSnapDistance = 10f;

    // Nodes that CalculateLazyThetaStar expands per frame.
    private const int PathStepsPerFrame = 200;

    // Radius used when the agent has no CapsuleCollider.
    private const float DefaultAgentRadius = 0.5f;

    // The avoidance looks ahead the distance covered in this many seconds, with AvoidanceMinLookAhead as the minimum.
    private const float AvoidanceLookAheadTime = 0.5f;
    private const float AvoidanceMinLookAhead = 1.5f;

    // Weight of the push away from the wall, added to the slide direction.
    private const float WallPushWeight = 0.5f;

    // Seconds between two progress checks and minimum distance per check.
    private const float StuckCheckInterval = 1f;
    private const float StuckMinProgress = 0.75f;

    // Stuck checks before the agent gives up.
    private const int MaxStuckAttempts = 3;

    // Seconds the agent backs away from the wall.
    private const float BackoffDuration = 0.5f;

    /// <summary>The NewPhysicsMovement that applies the forces.</summary>
    public NewPhysicsMovement ManualMovement { get; private set; }

    /// <summary>The GridEntity on the same GameObject.</summary>
    public GridEntity Owner { get; private set; }

    /// <summary>The last destination given to SetDestination.</summary>
    public Vector3 Destination { get; private set; }

    /// <summary>Raised when the agent reaches any destination.</summary>
    public event Action OnAnyDestinationReached;

    /// <summary>Raised when SetDestination receives a destination different from Destination.</summary>
    public event Action OnDestinationChanged;

    /// <summary>Raised by CancelMovement.</summary>
    public event Action OnMovementCanceled;

    /// <summary>Raised when the agent gives up on a destination after MaxStuckAttempts.</summary>
    public event Action OnMovementFailed;

    // Layers the avoidance slides along: obstacleMask and the walls of AI_Manager.
    private LayerMask AvoidanceMask => obstacleMask | AI_Manager.instance.WallMask;

    // World position of the center of the body.
    private Vector3 BodyCenter => transform.TransformPoint(_bodyCenterLocal);

    private void Awake()
    {
        ManualMovement = GetComponent<NewPhysicsMovement>();
        Owner = GetComponent<GridEntity>();
        _debug = GetComponent<DebugableObject>();

        if (TryGetComponent(out CapsuleCollider capsule))
        {
            Vector3 scale = transform.lossyScale;
            _agentRadius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            _bodyCenterLocal = capsule.center;
        }
        else
        {
            _agentRadius = DefaultAgentRadius;
            _bodyCenterLocal = Vector3.zero;
        }

        _debug.AddGizmoAction(DrawPath); _debug.AddGizmoAction(DrawDestination);

        OnDestinationChanged += ClearDestinationCallbacks;
        OnMovementCanceled += ClearDestinationCallbacks;
    }

    private void FixedUpdate()
    {
        if (_fixedUpdate == null)
            return;

        _fixedUpdate();

        if (_fixedUpdate != null && !_isBackingOff)
            CheckProgress();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_fixedUpdate == null || (AvoidanceMask.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector3 normal = collision.GetContact(0).normal;
        normal.y = 0;
        if (normal.sqrMagnitude > 0.001f)
            _lastWallNormal = normal.normalized;
    }

    /// <summary>
    /// Moves the agent to newDestination.
    /// The agent moves straight when a sphere of its radius reaches newDestination within DirectPathMaxDistance.
    /// The agent follows a Theta* path in every other case.
    /// </summary>
    /// <param name="newDestination">Point to reach.</param>
    /// <param name="onDestinationReached">Called once when the agent reaches newDestination.</param>
    /// <param name="onFinishCalculating">
    /// Called once with true when the agent starts moving.
    /// Called once with false when the path calculation fails.
    /// </param>
    /// <param name="onFailed">Called once when the agent gives up on newDestination after MaxStuckAttempts.</param>
    public void SetDestination(Vector3 newDestination, Action onDestinationReached = null, Action<bool> onFinishCalculating = null, Action onFailed = null)
    {
        StopPathCalculation();

        if (newDestination != Destination) OnDestinationChanged?.Invoke();

        Destination = newDestination;
        OnCurrentDestinationReached = onDestinationReached;
        OnCurrentDestinationFailed = onFailed;

        _stuckAttempts = 0;
        _isBackingOff = false;
        ResetProgress();

        if (HasClearPathTo(newDestination))
        {
            onFinishCalculating?.Invoke(true);
            OnDesinationAtSight(newDestination);
        }
        else
        {
            CalculatePath(newDestination, onFinishCalculating);
        }
    }

    /// <summary>Stops the agent and clears the current destination callbacks.</summary>
    public void CancelMovement()
    {
        StopPathCalculation();
        ClearPath();
        OnMovementCanceled?.Invoke();
    }

    // true when a sphere of the agent radius goes from the body center to the destination without hitting a wall.
    private bool HasClearPathTo(Vector3 destination)
    {
        Vector3 start = BodyCenter;
        Vector3 end = destination + (start - transform.position);
        return start.InLineOfSightWide(end, AI_Manager.instance.WallMask, _agentRadius, DirectPathMaxDistance);
    }

    private void OnDesinationAtSight(Vector3 newDestination)
    {
        // Conseguir la posicion en el piso
        if (Physics.Raycast(newDestination, Vector3.down, out RaycastHit hitInfo, GroundSnapDistance, AI_Manager.instance.GroundMask))
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
                ArriveAtDestination();
                return;
            }

            ManualMovement.AccelerateTowards(Steer(newDestination - transform.position));
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

        _pathCalculation = StartCoroutine(keyNodes.CalculateLazyThetaStar(manager.WallMask, onPathCalculated, Destination, PathStepsPerFrame, DirectPathMaxDistance, _agentRadius));
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
        _isBackingOff = false;
        ResetProgress();

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

            ManualMovement.AccelerateTowards(Steer(_path[_pathIndex] - transform.position));
            return;
        }

        // Si no quedan, finalizar el recorrido
        _debug.Log("no hay mas nodos, corto pathfinding");
        ArriveAtDestination();
    }

    private void ArriveAtDestination()
    {
        Action onReached = OnCurrentDestinationReached;

        // The path is cleared before the callbacks, so a callback can set a new destination.
        ClearPath();
        OnCurrentDestinationFailed = null;
        OnAnyDestinationReached?.Invoke();
        onReached?.Invoke();
    }

    private void ClearPath()
    {
        _fixedUpdate = null;
        _isBackingOff = false;
        ManualMovement.ClearForces();
        ManualMovement.AccelerateTowards(Vector3.zero);
        _path.Clear();
        _pathIndex = 0;
    }

    private void ClearDestinationCallbacks()
    {
        OnCurrentDestinationReached = null;
        OnCurrentDestinationFailed = null;
    }

    #endregion

    #region Wall avoidance

    // Returns desired turned to slide along the wall ahead, with a push away from it.
    private Vector3 Steer(Vector3 desired)
    {
        Vector3 flat = new Vector3(desired.x, 0, desired.z);
        float length = flat.magnitude;
        if (length < 0.001f)
            return desired;

        Vector3 dir = flat / length;
        float lookAhead = Mathf.Max(ManualMovement.CurrentSpeed * AvoidanceLookAheadTime, AvoidanceMinLookAhead);

        // The cast starts one radius behind the body, so a wall the body already touches is detected.
        Vector3 origin = BodyCenter - dir * _agentRadius;
        if (!Physics.SphereCast(origin, _agentRadius * 0.9f, dir, out RaycastHit hit, lookAhead + _agentRadius, AvoidanceMask))
            return desired;

        Vector3 normal = new Vector3(hit.normal.x, 0, hit.normal.z);
        if (normal.sqrMagnitude < 0.001f)
            return desired;

        normal.Normalize();
        _lastWallNormal = normal;

        // Moving away from the wall: nothing to correct.
        if (Vector3.Dot(dir, normal) >= 0)
            return desired;

        Vector3 slide = Vector3.ProjectOnPlane(dir, normal);

        // Head-on hit: slide toward the side of the agent's right, so the choice stays stable between frames.
        if (slide.sqrMagnitude < 0.01f)
        {
            Vector3 side = Vector3.Cross(Vector3.up, normal);
            slide = Vector3.Dot(side, transform.right) >= 0 ? side : -side;
        }

        Vector3 steered = (slide.normalized + normal * WallPushWeight).normalized * length;
        steered.y = desired.y;
        return steered;
    }

    #endregion

    #region Stuck recovery

    private void ResetProgress()
    {
        _progressTimer = 0f;
        _progressCheckPosition = transform.position;
    }

    private void CheckProgress()
    {
        _progressTimer += Time.fixedDeltaTime;
        if (_progressTimer < StuckCheckInterval)
            return;

        Vector3 moved = transform.position - _progressCheckPosition;
        moved.y = 0;
        ResetProgress();

        if (moved.sqrMagnitude >= StuckMinProgress * StuckMinProgress)
        {
            _stuckAttempts = 0;
            return;
        }

        OnStuck();
    }

    private void OnStuck()
    {
        _stuckAttempts++;

        if (_stuckAttempts > MaxStuckAttempts)
        {
            FailMovement();
            return;
        }

        if (_debug.canDebug)
            _debug.Log($"Estoy trabado, intento {_stuckAttempts} de {MaxStuckAttempts}");

        if (_stuckAttempts == 1)
            RecalculatePath();
        else
            StartBackoff();
    }

    // Recalculates the path through the nodes, without the straight shortcut.
    private void RecalculatePath()
    {
        StopPathCalculation();
        CalculatePath(Destination, null);
    }

    // Moves away from the last wall for BackoffDuration seconds and then recalculates the path.
    private void StartBackoff()
    {
        Vector3 away = _lastWallNormal != Vector3.zero ? _lastWallNormal : -transform.forward;

        // A sideways component keeps the agent from backing into the same spot.
        Vector3 side = Vector3.Cross(Vector3.up, away);
        Vector3 backoffDir = (away + side * 0.5f).normalized;

        float timer = 0f;
        _isBackingOff = true;

        _fixedUpdate = () =>
        {
            timer += Time.fixedDeltaTime;
            ManualMovement.AccelerateTowards(backoffDir);

            if (timer < BackoffDuration)
                return;

            _isBackingOff = false;
            ResetProgress();

            // The agent waits in place until the new path is ready.
            _fixedUpdate = () => ManualMovement.AccelerateTowards(Vector3.zero);
            RecalculatePath();
        };
    }

    private void FailMovement()
    {
        Action onFailed = OnCurrentDestinationFailed;

        _debug.Log("Me trabe, no puedo llegar al destino");

        StopPathCalculation();
        ClearPath();
        ClearDestinationCallbacks();
        _stuckAttempts = 0;

        onFailed?.Invoke();
        OnMovementFailed?.Invoke();
    }

    #endregion

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
